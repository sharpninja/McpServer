#!/usr/bin/env node
"use strict";
/**
 * codex-jsonl.js - Parse Codex CLI JSONL session transcripts into MCP session-log turn data.
 *
 * Modes:
 *   parse     <jsonl-path>                              -> JSON array of rich turn objects
 *   subagents <parent-jsonl-path>                       -> JSON array of subagent file descriptors
 *   import    <jsonl-path> <session-id> [parent-req-id] -> tab-delimited import commands
 */

const fs = require("fs");
const path = require("path");
const os = require("os");

const mode = process.argv[2] || "parse";
const jsonlPath = process.argv[3] || "";
const sessionId = process.argv[4] || "";
const parentReqId = process.argv[5] || "";

// ---------------------------------------------------------------------------
// Secret redaction
// ---------------------------------------------------------------------------

const REDACT_PATTERNS = [
  [/X-Api-Key:\s*[^\s"'\r\n]{8,}/gi, "X-Api-Key: [REDACTED]"],
  [/"X-Api-Key":\s*"[^"]{8,}"/gi, '"X-Api-Key": "[REDACTED]"'],
  [/apiKey['":\s]+["']?[A-Za-z0-9_\-]{20,}["']?/gi, "apiKey: [REDACTED]"],
  [/Bearer\s+[A-Za-z0-9_.\-]{20,}/gi, "Bearer [REDACTED]"],
  [/Authorization:\s*[^\s"'\r\n]{8,}/gi, "Authorization: [REDACTED]"],
  [/signature:\s*\n\s+value:\s*[A-Fa-f0-9]{40,}/gi, "signature:\n  value: [REDACTED]"],
];

function redact(text) {
  if (typeof text !== "string") return text;
  let out = text;
  for (const [pattern, replacement] of REDACT_PATTERNS) {
    out = out.replace(pattern, replacement);
  }
  return out;
}

function redactObj(obj) {
  if (typeof obj === "string") return redact(obj);
  if (!obj || typeof obj !== "object") return obj;
  if (Array.isArray(obj)) return obj.map(redactObj);
  const result = {};
  for (const [k, v] of Object.entries(obj)) {
    const lk = k.toLowerCase();
    if (lk === "apikey" || lk === "api_key" || lk === "value" || lk === "authorization") {
      result[k] = typeof v === "string" && v.length > 8 ? "[REDACTED]" : v;
    } else {
      result[k] = redactObj(v);
    }
  }
  return result;
}

// ---------------------------------------------------------------------------
// JSONL reading
// ---------------------------------------------------------------------------

function readJsonl(filePath) {
  const content = fs.readFileSync(filePath, "utf8");
  const events = [];
  for (const line of content.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    try {
      events.push(JSON.parse(trimmed));
    } catch {
      // skip malformed lines
    }
  }
  return events;
}

// ---------------------------------------------------------------------------
// Session metadata extraction
// ---------------------------------------------------------------------------

function extractSessionMeta(events) {
  const metas = events.filter(e => e.type === "session_meta");
  if (!metas.length) return null;
  const first = metas[0];
  const p = first.payload || {};
  return {
    id: p.id || "",
    cwd: p.cwd || "",
    timestamp: p.timestamp || first.timestamp || "",
    threadSource: p.thread_source || "user",
    originator: p.originator || "",
    cliVersion: p.cli_version || "",
    source: p.source || null,
    parentThreadId: p.source?.subagent?.thread_spawn?.parent_thread_id || null,
    agentNickname: p.source?.subagent?.thread_spawn?.agent_nickname || null,
    agentRole: p.source?.subagent?.thread_spawn?.agent_role || null,
    depth: p.source?.subagent?.thread_spawn?.depth || 0,
  };
}

// ---------------------------------------------------------------------------
// Turn boundary grouping
// ---------------------------------------------------------------------------

function groupByTurns(events) {
  const turns = [];
  let current = null;

  for (const event of events) {
    const payload = event.payload || {};
    const subType = payload.type;

    if (event.type === "event_msg" && subType === "task_started") {
      if (current) turns.push(current);
      current = {
        turnId: payload.turn_id || "",
        startedAt: event.timestamp || "",
        startedAtEpoch: payload.started_at || 0,
        completedAt: "",
        status: "in_progress",
        reason: "",
        events: [event],
      };
    } else if (current) {
      current.events.push(event);
      if (event.type === "event_msg" && subType === "task_complete") {
        current.completedAt = event.timestamp || "";
        current.status = "completed";
        current.lastAgentMessage = redact(payload.last_agent_message || "");
      } else if (event.type === "event_msg" && subType === "turn_aborted") {
        current.completedAt = event.timestamp || "";
        current.status = "failed";
        current.reason = payload.reason || "aborted";
      }
    }
  }

  if (current) turns.push(current);
  return turns;
}

// ---------------------------------------------------------------------------
// Per-turn rich field extraction
// ---------------------------------------------------------------------------

const FILE_READ_TOOLS = new Set([
  "read_file", "cat", "head", "tail", "nl", "sed",
  "get_file_info", "read", "ReadFile",
]);

const FILE_WRITE_TOOLS = new Set([
  "write_file", "create_file", "overwrite_file", "WriteFile",
  "edit_block", "patch_apply", "apply_patch",
]);

const GIT_TOOLS = new Set([
  "git_commit", "git_push", "commit", "push",
]);

function extractTurnFields(turn) {
  const events = turn.events || [];
  let queryText = "";
  let interpretation = "";
  const processingDialog = [];
  const actions = [];
  const filesModified = [];
  const contextList = [];
  const blockers = [];
  const designDecisions = [];
  const requirementsDiscovered = [];

  let actionOrder = 1;

  for (const event of events) {
    const payload = event.payload || {};
    const subType = payload.type;
    const ts = event.timestamp || turn.startedAt;

    // User message -> queryText
    if (event.type === "event_msg" && subType === "user_message") {
      const msg = redact(payload.message || "");
      if (!queryText) queryText = msg;
    }

    // Agent message -> interpretation + processingDialog
    if (event.type === "event_msg" && subType === "agent_message") {
      const msg = redact(payload.message || "");
      const phase = payload.phase || "completion";
      if (!interpretation && msg) {
        interpretation = msg.slice(0, 500);
      }
      if (msg) {
        processingDialog.push({
          timestamp: ts,
          role: "model",
          category: determineCategory(msg, phase),
          content: msg,
        });
        // Extract design decisions
        if (/decision:|chose |approach:|rationale:/i.test(msg)) {
          designDecisions.push(msg.slice(0, 200));
        }
        // Extract requirement IDs
        const reqIds = msg.match(/\b(?:FR|TR|TEST)-[A-Z]+-\d+\b/g);
        if (reqIds) {
          for (const id of reqIds) {
            if (!requirementsDiscovered.includes(id)) {
              requirementsDiscovered.push(id);
            }
          }
        }
      }
    }

    // Turn aborted -> blocker
    if (event.type === "event_msg" && subType === "turn_aborted") {
      const reason = payload.reason || "interrupted";
      blockers.push(`Turn aborted: ${reason}`);
    }

    // Patch apply end -> file edits
    if (event.type === "event_msg" && subType === "patch_apply_end") {
      const stdout = payload.stdout || "";
      const patchedFiles = parsePatchedFiles(stdout);
      for (const filePath of patchedFiles) {
        const clean = redact(filePath);
        if (!filesModified.includes(clean)) filesModified.push(clean);
        actions.push({
          order: actionOrder++,
          type: "edit",
          status: "completed",
          description: `Applied patch to ${path.basename(clean)}`,
          filePath: clean,
        });
      }
      // Add to contextList if reading
      if (stdout.includes("Read:") || stdout.includes("read")) {
        const readFiles = parsePatchedFiles(stdout.replace(/^.*?read.*?:/im, ""));
        for (const f of readFiles) {
          const clean = redact(f);
          if (!contextList.includes(clean)) contextList.push(clean);
        }
      }
    }

    // Function call -> tool call action + context
    if (event.type === "response_item" && subType === "function_call") {
      const toolName = payload.name || "";
      let argsObj = {};
      try { argsObj = JSON.parse(payload.arguments || "{}"); } catch {}

      const argsRedacted = redactObj(argsObj);

      if (FILE_WRITE_TOOLS.has(toolName)) {
        const filePath = redact(argsRedacted.path || argsRedacted.file || argsRedacted.filename || "");
        if (filePath) {
          if (!filesModified.includes(filePath)) filesModified.push(filePath);
          actions.push({
            order: actionOrder++,
            type: "edit",
            status: "completed",
            description: `${toolName}: ${path.basename(filePath)}`,
            filePath,
          });
        }
      } else if (FILE_READ_TOOLS.has(toolName)) {
        const filePath = redact(argsRedacted.path || argsRedacted.file || argsRedacted.filename || "");
        if (filePath && !contextList.includes(filePath)) contextList.push(filePath);
      } else if (GIT_TOOLS.has(toolName)) {
        const msg = argsRedacted.message || argsRedacted.commit_message || toolName;
        actions.push({
          order: actionOrder++,
          type: "commit",
          status: "completed",
          description: `git: ${String(msg).slice(0, 120)}`,
          filePath: "",
        });
      } else {
        // Generic tool call -> processing dialog
        const cmdArg = argsRedacted.command || argsRedacted.cmd || "";
        if (cmdArg) {
          processingDialog.push({
            timestamp: ts,
            role: "tool",
            category: "tool_call",
            content: redact(`${toolName}: ${String(cmdArg).slice(0, 200)}`),
          });
          // Check for file references in command
          const fileRefs = String(cmdArg).match(/([A-Za-z]:[\\/][^\s"']+\.[A-Za-z]{1,6}|\/[^\s"']+\.[A-Za-z]{1,6})/g) || [];
          for (const f of fileRefs) {
            const clean = redact(f);
            if (looksLikeCodeFile(f) && !contextList.includes(clean)) contextList.push(clean);
          }
        }
      }
    }

    // Custom tool call (patch_apply, etc.)
    if (event.type === "response_item" && subType === "custom_tool_call") {
      const toolName = payload.name || "";
      let inputObj = payload.input || {};
      if (typeof inputObj === "string") {
        try { inputObj = JSON.parse(inputObj); } catch {}
      }
      inputObj = redactObj(inputObj);
      const filePath = redact(inputObj.path || inputObj.file || "");
      if (filePath) {
        if (!filesModified.includes(filePath)) filesModified.push(filePath);
        actions.push({
          order: actionOrder++,
          type: "edit",
          status: "completed",
          description: `${toolName}: ${path.basename(filePath)}`,
          filePath,
        });
      }
    }
  }

  // Ensure minimum required action when files were modified
  if (filesModified.length > 0 && actions.length === 0) {
    actions.push({
      order: 1,
      type: "edit",
      status: "completed",
      description: `Modified ${filesModified.length} file(s)`,
      filePath: filesModified[0] || "",
    });
  }

  // Always have at least one session_log action
  if (actions.length === 0) {
    actions.push({
      order: 1,
      type: "session_log",
      status: "completed",
      description: "Session turn captured from Codex JSONL",
      filePath: "",
    });
  }

  return {
    queryText: queryText || turn.lastAgentMessage || "Codex turn",
    interpretation: interpretation || "Turn captured from Codex JSONL transcript",
    processingDialog: processingDialog.length > 0 ? processingDialog : [
      { timestamp: turn.startedAt, role: "model", category: "observation", content: "Turn captured from Codex JSONL transcript" },
    ],
    actions,
    filesModified,
    contextList,
    blockers,
    designDecisions,
    requirementsDiscovered,
  };
}

function determineCategory(msg, phase) {
  if (/decision:|chose |approach:|rationale:|trade-?off/i.test(msg)) return "decision";
  if (/error:|fail|exception:|warning:/i.test(msg)) return "observation";
  if (phase === "context") return "reasoning";
  return "observation";
}

function parsePatchedFiles(stdout) {
  const files = [];
  for (const line of String(stdout).split(/\r?\n/)) {
    // "A path/to/file.cs" or "M path/to/file.cs" or "Updated path/to/file.cs"
    const m = line.match(/^[AMRDC\s]*([A-Za-z]:[\\/][^\r\n]+\.[A-Za-z]{1,10}|\/[^\r\n\s]+\.[A-Za-z]{1,10})/);
    if (m) {
      const f = m[1].trim();
      if (f && !files.includes(f)) files.push(f);
    }
    // Windows paths without drive letter prefix
    const m2 = line.match(/(?:Updated|Created|Modified|Deleted|New file:)\s+(.+)/i);
    if (m2) {
      const f = m2[1].trim();
      if (f && !files.includes(f)) files.push(f);
    }
  }
  return files;
}

function looksLikeCodeFile(filePath) {
  return /\.(cs|ts|js|py|sh|yaml|yml|json|md|sql|csproj|sln|props|targets)$/i.test(filePath);
}

// ---------------------------------------------------------------------------
// Request ID generation
// ---------------------------------------------------------------------------

function compactIso(isoStr) {
  return (isoStr || new Date().toISOString())
    .replace(/[-:]/g, "")
    .replace(/\.\d+Z$/, "Z")
    .replace(/Z$/, "Z");
}

function slugify(text) {
  return String(text || "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 40);
}

function makeTurnRequestId(turn) {
  const ts = compactIso(turn.startedAt);
  const slug = slugify(turn.turnId.slice(0, 8) || "codex");
  return `req-${ts}-${slug}`;
}

function makeSubagentRequestId(meta, sessionMeta) {
  const ts = compactIso(sessionMeta.timestamp);
  const nick = slugify(meta.agentNickname || meta.agentRole || "subagent");
  return `req-${ts}-subagent-${nick}`;
}

// ---------------------------------------------------------------------------
// Mode: parse
// ---------------------------------------------------------------------------

function parse(jsonlPath) {
  const events = readJsonl(jsonlPath);
  const sessionMeta = extractSessionMeta(events);
  const turns = groupByTurns(events);

  return turns.map(turn => {
    const fields = extractTurnFields(turn);
    const requestId = makeTurnRequestId(turn);
    const queryTitle = fields.queryText
      ? fields.queryText.split("\n")[0].slice(0, 80)
      : `Turn ${turn.turnId.slice(0, 8)}`;

    return {
      requestId,
      timestamp: turn.startedAt,
      queryTitle,
      queryText: fields.queryText,
      interpretation: fields.interpretation,
      status: turn.status === "failed" ? "failed" : "completed",
      actions: fields.actions,
      filesModified: fields.filesModified,
      contextList: fields.contextList,
      processingDialog: fields.processingDialog,
      blockers: fields.blockers,
      designDecisions: fields.designDecisions,
      requirementsDiscovered: fields.requirementsDiscovered,
      tags: sessionMeta?.threadSource === "subagent"
        ? ["subagent", sessionMeta.agentNickname || "codex"].filter(Boolean)
        : ["codex"],
      model: "codex",
      tokenCount: 0,
      turnId: turn.turnId,
      _meta: { sessionMeta, turnStatus: turn.status },
    };
  });
}

// ---------------------------------------------------------------------------
// Mode: subagents
// ---------------------------------------------------------------------------

function findSubagentTranscripts(parentSessionId) {
  const sessionsRoot = process.env.CODEX_SESSION_DIR || path.join(os.homedir(), ".codex", "sessions");
  const found = [];

  if (!fs.existsSync(sessionsRoot)) return found;

  // Scan year/month/day directories
  const scanDir = (dir) => {
    let entries;
    try { entries = fs.readdirSync(dir, { withFileTypes: true }); } catch { return; }
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        scanDir(full);
      } else if (entry.isFile() && entry.name.endsWith(".jsonl")) {
        try {
          // Only read the first line for speed
          const head = readFirstLines(full, 2);
          for (const event of head) {
            if (event.type === "session_meta") {
              const p = event.payload || {};
              const pid = p.source?.subagent?.thread_spawn?.parent_thread_id;
              if (pid === parentSessionId) {
                found.push({
                  path: full,
                  sessionId: p.id || "",
                  parentThreadId: pid,
                  agentNickname: p.source?.subagent?.thread_spawn?.agent_nickname || null,
                  agentRole: p.source?.subagent?.thread_spawn?.agent_role || null,
                  startedAt: p.timestamp || event.timestamp || "",
                  cwd: p.cwd || "",
                });
              }
              break;
            }
          }
        } catch {
          // skip unreadable files
        }
      }
    }
  };

  scanDir(sessionsRoot);
  return found;
}

function readFirstLines(filePath, count) {
  const buf = Buffer.alloc(8192);
  let fd;
  try {
    fd = fs.openSync(filePath, "r");
    const bytesRead = fs.readSync(fd, buf, 0, 8192, 0);
    const text = buf.slice(0, bytesRead).toString("utf8");
    const lines = text.split("\n").filter(Boolean).slice(0, count);
    return lines.map(l => { try { return JSON.parse(l); } catch { return null; } }).filter(Boolean);
  } finally {
    if (fd !== undefined) try { fs.closeSync(fd); } catch {}
  }
}

function subagents(parentJsonlPath) {
  const events = readJsonl(parentJsonlPath);
  const sessionMeta = extractSessionMeta(events);
  if (!sessionMeta || !sessionMeta.id) {
    return [];
  }
  return findSubagentTranscripts(sessionMeta.id);
}

// ---------------------------------------------------------------------------
// Mode: import
// ---------------------------------------------------------------------------

function buildImportLines(jsonlPath, targetSessionId, parentReqId) {
  const events = readJsonl(jsonlPath);
  const sessionMeta = extractSessionMeta(events);
  const turns = groupByTurns(events);
  const lines = [];

  const isSubagent = sessionMeta?.threadSource === "subagent";
  const nickname = sessionMeta?.agentNickname || sessionMeta?.agentRole || "subagent";
  const subagentTs = compactIso(sessionMeta?.timestamp || "");

  for (const turn of turns) {
    const fields = extractTurnFields(turn);
    const requestId = isSubagent
      ? `req-${subagentTs}-subagent-${slugify(nickname)}-${slugify(turn.turnId.slice(0, 8))}`
      : makeTurnRequestId(turn);
    const queryTitle = fields.queryText
      ? fields.queryText.split("\n")[0].slice(0, 80)
      : `Turn ${turn.turnId.slice(0, 8)}`;

    const turnData = {
      requestId,
      timestamp: turn.startedAt,
      queryTitle,
      queryText: fields.queryText,
      response: fields.interpretation,
      interpretation: fields.interpretation,
      status: turn.status === "failed" ? "failed" : "completed",
      actions: fields.actions,
      filesModified: fields.filesModified,
      contextList: fields.contextList,
      processingDialog: fields.processingDialog,
      blockers: fields.blockers,
      designDecisions: fields.designDecisions,
      requirementsDiscovered: fields.requirementsDiscovered,
      tags: isSubagent ? ["subagent", nickname] : ["codex"],
      model: "codex",
      tokenCount: 0,
    };

    const sessionLog = {
      sourceType: "Codex",
      sessionId: targetSessionId,
      title: isSubagent ? `Subagent: ${nickname}` : "Codex session",
      model: "codex",
      started: sessionMeta?.timestamp || turn.startedAt,
      lastUpdated: turn.completedAt || turn.startedAt || sessionMeta?.timestamp || "",
      status: "completed",
      turns: [turnData],
    };

    // If subagent and parentReqId given, link back to parent
    if (isSubagent && parentReqId) {
      turnData.tags = [...(turnData.tags || []), `parent:${parentReqId}`];
    }

    const paramsJson = JSON.stringify({ sessionLog });
    const paramsB64 = Buffer.from(paramsJson).toString("base64").replace(/[\r\n]/g, "");
    const label = isSubagent
      ? `subagent-${nickname}-${turn.turnId.slice(0, 8)}`
      : `parent-${turn.turnId.slice(0, 8)}`;

    lines.push(`workflow.sessionlog.importRecovery\t${paramsB64}\t${label}`);
  }

  return lines;
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

function main() {
  if (!jsonlPath) {
    process.stderr.write(`usage: codex-jsonl.js <parse|subagents|import> <jsonl-path> [session-id] [parent-req-id]\n`);
    process.exit(2);
  }

  if (!fs.existsSync(jsonlPath)) {
    process.stderr.write(`codex-jsonl.js: file not found: ${jsonlPath}\n`);
    process.exit(2);
  }

  switch (mode) {
    case "parse": {
      const turns = parse(jsonlPath);
      process.stdout.write(JSON.stringify(turns, null, 2));
      break;
    }
    case "subagents": {
      const subs = subagents(jsonlPath);
      process.stdout.write(JSON.stringify(subs, null, 2));
      break;
    }
    case "import": {
      if (!sessionId) {
        process.stderr.write("codex-jsonl.js import: session-id required\n");
        process.exit(2);
      }
      const importLines = buildImportLines(jsonlPath, sessionId, parentReqId);
      for (const line of importLines) {
        process.stdout.write(line + "\n");
      }
      break;
    }
    default:
      process.stderr.write(`codex-jsonl.js: unknown mode: ${mode}\n`);
      process.exit(2);
  }
}

main();
