#!/usr/bin/env bats
# FR-MCP-PLUGINCORE-003: repl_invoke_persistent shell wrapper contract.

CORE_ROOT="$(cd "$(dirname "$BATS_TEST_FILENAME")/.." && pwd)"

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
    const m = /"requestId":"([^"]+)"/.exec(line);
    const km = /"keyword":"([^"]+)"/.exec(line);
    process.stdout.write(`type: result\npayload:\n  requestId: ${m ? m[1] : "unknown"}\n  result:\n    echo: ${km ? km[1] : "none"}\n\n---\n`);
});
FAKE_EOF
    export MCPSERVER_REPL_BIN="$(native_path "$FAKE")"
    STATE_JSON="$SANDBOX/state/mcpserver-repl-daemon.json"

    source "$CORE_ROOT/lib-sh/repl-persistent.sh"
}

teardown() {
    if [ -f "$STATE_JSON" ]; then
        node -e "try{process.kill(JSON.parse(require('fs').readFileSync(process.argv[1],'utf8')).pid)}catch{}" "$(native_path "$STATE_JSON")" || true
        sleep 0.3
    fi
    rm -rf "$SANDBOX"
}

@test "invoke without params returns a result envelope" {
    run repl_invoke_persistent "client.todo.QueryAsync"
    [ "$status" -eq 0 ]
    [[ "$output" == *"type: result"* ]]
    [[ "$output" == *"---"* ]]
}

@test "invoke with JSON params threads them through to the repl" {
    run repl_invoke_persistent "client.todo.QueryAsync" '{"keyword":"auth"}'
    [ "$status" -eq 0 ]
    [[ "$output" == *"echo: auth"* ]]
}

@test "consecutive invocations reuse one repl child" {
    repl_invoke_persistent "client.todo.QueryAsync" > /dev/null
    repl_invoke_persistent "client.todo.QueryAsync" > /dev/null
    [ "$(grep -c '^started ' "$FAKE_LOG_POSIX")" -eq 1 ]
}

@test "MCPSERVER_REPL_PERSISTENT=0 falls back to spawn-per-call" {
    export MCPSERVER_REPL_PERSISTENT=0
    # The fake is a .js file - the fallback path execs it directly, which only
    # works through node, so use a tiny native-shell fake for this test.
    FAKE_SH="$SANDBOX/fake-repl-sh"
    cat > "$FAKE_SH" <<'SH_EOF'
#!/usr/bin/env bash
echo "started-sh" >> "$FAKE_LOG"
IFS= read -r _line
printf 'type: result\npayload:\n  requestId: x\n  result: {}\n\n---\n'
SH_EOF
    chmod +x "$FAKE_SH"
    export MCPSERVER_REPL_BIN="$FAKE_SH"

    repl_invoke_persistent "client.todo.QueryAsync" > /dev/null
    repl_invoke_persistent "client.todo.QueryAsync" > /dev/null
    [ "$(grep -c 'started-sh' "$FAKE_LOG_POSIX")" -eq 2 ]
    [ ! -f "$STATE_JSON" ]
}
