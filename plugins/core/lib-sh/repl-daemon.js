#!/usr/bin/env node
"use strict";
/**
 * FR-MCP-PLUGINCORE-003: persistent REPL daemon.
 *
 * Replaces spawn-per-call: one long-lived `mcpserver-repl --agent-stdio`
 * child serves many requests over the NDJSON framing (FR-MCP-REPL-005).
 * A tiny localhost TCP server brokers requests from short-lived hook
 * processes to the child; responses are byte streams terminated by a lone
 * `---` line.
 *
 * Modes:
 *   --serve   run the broker (spawned detached by --send on demand)
 *   --send    read ONE single-line JSON envelope from stdin, deliver via the
 *             daemon (auto-starting it), print the response, exit
 *
 * Environment:
 *   MCPSERVER_REPL_BIN          repl binary (default: mcpserver-repl)
 *   MCPSERVER_REPL_DAEMON_DIR   state dir for daemon.json (default: $TMPDIR)
 *   MCPSERVER_REPL_IDLE_SECONDS daemon exits after idle (default: 300)
 *   MCPSERVER_WORKSPACE_PATH    forwarded to the repl child
 */

const net = require("net");
const fs = require("fs");
const path = require("path");
const os = require("os");
const { spawn } = require("child_process");

const REPL_BIN = process.env.MCPSERVER_REPL_BIN || "mcpserver-repl";
const STATE_DIR = process.env.MCPSERVER_REPL_DAEMON_DIR || os.tmpdir();
const STATE_FILE = path.join(STATE_DIR, "mcpserver-repl-daemon.json");
const IDLE_SECONDS = parseInt(process.env.MCPSERVER_REPL_IDLE_SECONDS || "300", 10);
const TERMINATOR = "---";

function readState() {
  try {
    return JSON.parse(fs.readFileSync(STATE_FILE, "utf8"));
  } catch {
    return null;
  }
}

function spawnRepl() {
  // A .js repl (used by tests / future node transports) runs under this same
  // node; anything else is treated as a native executable.
  if (REPL_BIN.endsWith(".js")) {
    return spawn(process.execPath, [REPL_BIN, "--agent-stdio"], {
      stdio: ["pipe", "pipe", "ignore"],
      env: process.env,
    });
  }
  return spawn(REPL_BIN, ["--agent-stdio"], {
    stdio: ["pipe", "pipe", "ignore"],
    env: process.env,
  });
}

function serve() {
  const child = spawnRepl();
  child.on("error", () => {
    try { fs.unlinkSync(STATE_FILE); } catch { /* not written yet */ }
    process.exit(1);
  });

  let queue = Promise.resolve();
  let idleTimer = null;

  function resetIdle() {
    if (idleTimer) clearTimeout(idleTimer);
    idleTimer = setTimeout(() => shutdown(0), IDLE_SECONDS * 1000);
    if (idleTimer.unref) idleTimer.unref();
    // An unref'd timer will not keep the process alive; re-ref so idle
    // shutdown actually fires while the server keeps the loop running.
    if (idleTimer.ref) idleTimer.ref();
  }

  function shutdown(code) {
    try { fs.unlinkSync(STATE_FILE); } catch { /* already gone */ }
    try { child.stdin.end(); } catch { /* child gone */ }
    try { child.kill(); } catch { /* child gone */ }
    process.exit(code);
  }

  child.on("exit", () => shutdown(1));

  // Collector for child stdout: resolves one pending request at a time with
  // the bytes up to and including the lone '---' terminator line.
  let buffer = "";
  let pendingResolve = null;
  child.stdout.on("data", (chunk) => {
    buffer += chunk.toString("utf8");
    if (!pendingResolve) return;
    const lines = buffer.split(/\r?\n/);
    for (let i = 0; i < lines.length; i++) {
      if (lines[i].trimEnd() === TERMINATOR) {
        const upto = lines.slice(0, i + 1).join("\n") + "\n";
        buffer = lines.slice(i + 1).join("\n");
        const resolve = pendingResolve;
        pendingResolve = null;
        resolve(upto);
        return;
      }
    }
  });

  function request(line) {
    const run = () => new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        pendingResolve = null;
        reject(new Error("repl response timeout"));
      }, 170000);
      pendingResolve = (response) => {
        clearTimeout(timer);
        resolve(response);
      };
      child.stdin.write(line.trimEnd() + "\n");
      // Drain anything already buffered (response may have raced the write).
      child.stdout.emit("data", Buffer.alloc(0));
    });
    const result = queue.then(run, run);
    queue = result.catch(() => undefined);
    return result;
  }

  const server = net.createServer((socket) => {
    resetIdle();
    let inbound = "";
    socket.on("data", (chunk) => {
      inbound += chunk.toString("utf8");
      const newline = inbound.indexOf("\n");
      if (newline < 0) return;
      const line = inbound.slice(0, newline);
      inbound = inbound.slice(newline + 1);
      request(line)
        .then((response) => socket.end(response))
        .catch((err) => socket.end(
          `type: error\npayload:\n  requestId: unknown\n  code: daemon_error\n  message: ${JSON.stringify(String(err.message || err))}\n\n${TERMINATOR}\n`));
    });
    socket.on("error", () => { /* client went away */ });
  });

  server.listen(0, "127.0.0.1", () => {
    const { port } = server.address();
    fs.mkdirSync(STATE_DIR, { recursive: true });
    // The state file IS the readiness signal: --send polls for it. No stdio
    // coupling to the spawning process, so the daemon survives its exit
    // (required on Windows, where an inherited pipe ties child to parent).
    fs.writeFileSync(STATE_FILE, JSON.stringify({ port, pid: process.pid, replBin: REPL_BIN }));
    resetIdle();
  });
}

function connectOnce(port) {
  return new Promise((resolve, reject) => {
    const socket = net.connect({ port, host: "127.0.0.1" }, () => resolve(socket));
    socket.on("error", reject);
  });
}

async function ensureDaemon() {
  const state = readState();
  if (state) {
    try {
      return await connectOnce(state.port);
    } catch {
      try { fs.unlinkSync(STATE_FILE); } catch { /* stale */ }
    }
  }

  const daemon = spawn(process.execPath, [__filename, "--serve"], {
    detached: true,
    stdio: ["ignore", "ignore", "ignore"],
    env: process.env,
  });
  let startupFailed = false;
  daemon.on("exit", () => { startupFailed = true; });
  daemon.unref();

  // Readiness = state file appears and accepts a connection. Poll up to 15s.
  const deadline = Date.now() + 15000;
  for (;;) {
    const state2 = readState();
    if (state2) {
      try {
        return await connectOnce(state2.port);
      } catch { /* daemon still binding or stale file - keep polling */ }
    }
    if (startupFailed && !state2) throw new Error("daemon exited during startup");
    if (Date.now() > deadline) throw new Error("daemon failed to start within 15s");
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
}

async function send() {
  const line = fs.readFileSync(0, "utf8").split(/\r?\n/).find((l) => l.trim().length > 0);
  if (!line) {
    process.stderr.write("error: no envelope on stdin\n");
    process.exit(2);
  }

  const socket = await ensureDaemon();
  socket.write(line.trim() + "\n");
  let out = "";
  socket.on("data", (chunk) => { out += chunk.toString("utf8"); });
  await new Promise((resolve) => socket.on("end", resolve));
  process.stdout.write(out);
}

const mode = process.argv[2];
if (mode === "--serve") {
  serve();
} else if (mode === "--send") {
  send().catch((err) => {
    process.stderr.write(`error: ${err.message || err}\n`);
    process.exit(1);
  });
} else {
  process.stderr.write("usage: repl-daemon.js --serve | --send\n");
  process.exit(2);
}
