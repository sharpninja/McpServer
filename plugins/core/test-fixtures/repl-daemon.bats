#!/usr/bin/env bats
# FR-MCP-PLUGINCORE-003: persistent REPL daemon contract.
# Uses a fake repl (node script) so no server is needed: the fake answers every
# NDJSON request line with a '---'-terminated result envelope and records each
# process start so the suite can prove ONE child served N requests.
#
# Windows note: bats runs under MSYS bash while node is a native Windows
# binary, so every path handed to node goes through `cygpath -w` (no-op
# wrapper on real POSIX systems).

CORE_ROOT="$(cd "$(dirname "$BATS_TEST_FILENAME")/.." && pwd)"
DAEMON="$CORE_ROOT/lib-sh/repl-daemon.js"

native_path() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s\n' "$1"
    fi
}

setup() {
    SANDBOX="$(mktemp -d)"
    mkdir -p "$SANDBOX/state"
    export MCPSERVER_REPL_DAEMON_DIR="$(native_path "$SANDBOX/state")"
    export MCPSERVER_REPL_IDLE_SECONDS=60
    FAKE_LOG_POSIX="$SANDBOX/fake-starts.log"
    export FAKE_LOG="$(native_path "$FAKE_LOG_POSIX")"

    FAKE="$SANDBOX/fake-repl.js"
    cat > "$FAKE" <<'FAKE_EOF'
const fs = require("fs");
const readline = require("readline");
fs.appendFileSync(process.env.FAKE_LOG, `started ${process.pid}\n`);
const rl = readline.createInterface({ input: process.stdin });
rl.on("line", (line) => {
    const match = /"requestId":"([^"]+)"/.exec(line);
    const rid = match ? match[1] : "unknown";
    process.stdout.write(`type: result\npayload:\n  requestId: ${rid}\n  result:\n    ok: true\n\n---\n`);
});
FAKE_EOF
    export MCPSERVER_REPL_BIN="$(native_path "$FAKE")"
    DAEMON_NATIVE="$(native_path "$DAEMON")"
    STATE_JSON="$SANDBOX/state/mcpserver-repl-daemon.json"
}

teardown() {
    # Kill any daemon left behind so suites stay independent. Use node's
    # process.kill - portable across MSYS (where `kill` only knows MSYS pids)
    # and POSIX.
    if [ -f "$STATE_JSON" ]; then
        node -e "try{process.kill(JSON.parse(require('fs').readFileSync(process.argv[1],'utf8')).pid)}catch{}" "$(native_path "$STATE_JSON")" || true
        sleep 0.3
    fi
    rm -rf "$SANDBOX"
}

kill_daemon() {
    node -e "process.kill(JSON.parse(require('fs').readFileSync(process.argv[1],'utf8')).pid)" "$(native_path "$STATE_JSON")"
}

send() {
    printf '%s\n' "$1" | node "$DAEMON_NATIVE" --send
}

@test "send auto-starts the daemon and returns a terminated response" {
    run send '{"type":"request","payload":{"requestId":"req-20260612T000001Z-daemon-1","method":"client.x.Y"}}'
    [ "$status" -eq 0 ]
    [[ "$output" == *"requestId: req-20260612T000001Z-daemon-1"* ]]
    [[ "$output" == *"---"* ]]
}

@test "two sends are served by ONE repl child process" {
    send '{"type":"request","payload":{"requestId":"req-20260612T000002Z-daemon-2","method":"client.x.Y"}}' > /dev/null
    send '{"type":"request","payload":{"requestId":"req-20260612T000003Z-daemon-3","method":"client.x.Y"}}' > /dev/null
    [ -f "$FAKE_LOG_POSIX" ]
    [ "$(grep -c '^started ' "$FAKE_LOG_POSIX")" -eq 1 ]
}

@test "daemon restarts automatically after being killed" {
    send '{"type":"request","payload":{"requestId":"req-20260612T000004Z-daemon-4","method":"client.x.Y"}}' > /dev/null
    kill_daemon
    sleep 1
    run send '{"type":"request","payload":{"requestId":"req-20260612T000005Z-daemon-5","method":"client.x.Y"}}'
    [ "$status" -eq 0 ]
    [[ "$output" == *"requestId: req-20260612T000005Z-daemon-5"* ]]
    [ "$(grep -c '^started ' "$FAKE_LOG_POSIX")" -eq 2 ]
}

@test "concurrent sends both complete" {
    send '{"type":"request","payload":{"requestId":"req-20260612T000006Z-daemon-6","method":"client.x.Y"}}' > "$SANDBOX/a.out" &
    send '{"type":"request","payload":{"requestId":"req-20260612T000007Z-daemon-7","method":"client.x.Y"}}' > "$SANDBOX/b.out" &
    wait
    grep -q 'req-20260612T000006Z-daemon-6' "$SANDBOX/a.out"
    grep -q 'req-20260612T000007Z-daemon-7' "$SANDBOX/b.out"
}
