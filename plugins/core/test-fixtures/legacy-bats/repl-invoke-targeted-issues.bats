#!/usr/bin/env bats

LIB="${BATS_TEST_DIRNAME}/../lib-sh/repl-invoke.sh"

native_path() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s\n' "$1"
    fi
}

setup() {
    SANDBOX="$(mktemp -d)"
    export PLUGIN_ROOT_OVERRIDE="$SANDBOX/plugin-root"
    export MCP_AGENT_NAME=codex
    export REPL_TIMEOUT=5
    mkdir -p "$PLUGIN_ROOT_OVERRIDE"
}

teardown() {
    if [ -n "${MCPSERVER_REPL_DAEMON_DIR:-}" ]; then
        node -e 'try{process.kill(JSON.parse(require("fs").readFileSync(process.argv[1],"utf8")).pid)}catch{}' "${MCPSERVER_REPL_DAEMON_DIR}/mcpserver-repl-daemon.json" || true
    fi
    rm -rf "$SANDBOX"
    unset MCPSERVER_REPL_DAEMON_DIR MCPSERVER_REPL_BIN MCPSERVER_REPL_PERSISTENT
}

@test "ISSUE-18 raw calls use persistent daemon and reuse one repl process" {
    DAEMON_STATE="$SANDBOX/daemon-state"
    mkdir -p "$DAEMON_STATE"
    export MCPSERVER_REPL_DAEMON_DIR="$(native_path "$DAEMON_STATE")"
    export MCPSERVER_REPL_PERSISTENT=1
    FAKE_LOG_POSIX="$SANDBOX/daemon-starts.log"
    export FAKE_LOG="$(native_path "$FAKE_LOG_POSIX")"
    FAKE_JS="$SANDBOX/fake-persistent-repl.js"
    cat > "$FAKE_JS" <<'FAKE_EOF'
const fs = require("fs");
const readline = require("readline");
fs.appendFileSync(process.env.FAKE_LOG, `started ${process.pid}\n`);
const rl = readline.createInterface({ input: process.stdin });
rl.on("line", (line) => {
  const method = /"method":"([^"]+)"/.exec(line)?.[1] ?? "unknown";
  process.stdout.write(`type: result\npayload:\n  result:\n    method: ${method}\n\n---\n`);
});
FAKE_EOF
    export MCPSERVER_REPL_BIN="$(native_path "$FAKE_JS")"
    source "$LIB"

    run repl_invoke "client.SessionLog.QueryAsync" ""
    [ "$status" -eq 0 ]
    run repl_invoke "client.Todo.QueryAsync" ""
    [ "$status" -eq 0 ]

    [ "$(grep -c '^started ' "$FAKE_LOG_POSIX")" -eq 1 ]
}

@test "BUILD-REQPLUGIN-001 FR TR TEST typed params include acceptanceCriteria" {
    export MCPSERVER_REPL_PERSISTENT=0
    source "$LIB"

    for operation in createFr createTr createTest; do
        case "$operation" in
            createFr)
                expected_method="client.Requirements.CreateFrAsync"
                body_key="description"
                ;;
            createTr)
                expected_method="client.Requirements.CreateTrAsync"
                body_key="description"
                ;;
            createTest)
                expected_method="client.Requirements.CreateTestAsync"
                body_key="condition"
                ;;
        esac

        run _repl_requirements_typed_method "$operation"
        [ "$status" -eq 0 ]
        [ "$output" = "$expected_method" ]

        params="$(_repl_requirements_typed_params "$operation" "id: REQ-MCP-AC-100
title: AC create
${body_key}: Body text
priority: high
area: MCP
subarea: Requirements
acceptanceCriteria:
  - id: ac-1
    text: 'Criterion text'
    isSatisfied: false")"

        printf '%s\n' "$params" | grep -q 'acceptanceCriteria:'
        printf '%s\n' "$params" | grep -q 'id: ac-1'
        printf '%s\n' "$params" | grep -q "text: 'Criterion text'"
    done
}

@test "ISSUE-19 stale in-progress turn is superseded before beginTurn opens a new turn" {
    export MCPSERVER_REPL_PERSISTENT=0
    source "$LIB"
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    cat > "$REPL_INVOKE_CACHE_DIR/session-state.yaml" <<EOF
status: verified
sourceType: Codex
sessionId: Codex-20260626-issue19
title: ISSUE-19 test session
model: codex
started: 2026-06-26T00:00:00Z
workspacePath: "$SANDBOX/workspace"
workspace: "workspace"
baseUrl: "http://localhost:7147"
EOF
    cat > "$REPL_INVOKE_CACHE_DIR/current-turn.yaml" <<EOF
turnRequestId: req-old
queryTitle: Old turn
openedAt: 2026-06-26T00:00:00Z
status: in_progress
queryText: |
  old prompt
EOF
    PERSIST_LOG="$SANDBOX/persist.log"
    _repl_persist_turn() {
        printf '%s|%s|%s|%s\n' "$1" "$2" "$3" "$4" >> "$PERSIST_LOG"
        return 0
    }

    run _repl_workflow_begin_turn "requestId: req-new
queryTitle: New turn
queryText: |
  new prompt"

    [ "$status" -eq 0 ]
    grep -q '^req-old|Old turn|canceled|Superseded by req-new before it was completed\.$' "$PERSIST_LOG"
    grep -q '^req-new|New turn|in_progress|(turn opened)$' "$PERSIST_LOG"
    grep -q '^turnRequestId: req-new$' "$REPL_INVOKE_CACHE_DIR/current-turn.yaml"
}

@test "ISSUE-19 raw invocation propagates nonzero repl process exit" {
    export MCPSERVER_REPL_PERSISTENT=0
    source "$LIB"
    _repl_run_request_envelope() {
        printf 'not an error envelope\n'
        return 42
    }

    run _repl_invoke_raw "client.Todo.QueryAsync" ""

    [ "$status" -ne 0 ]
}

@test "ISSUE-19 createBatch typed params preserve root JSON records arrays" {
    export MCPSERVER_REPL_PERSISTENT=0
    source "$LIB"

    params="$(_repl_requirements_typed_params "createBatch" '{"records":[{"kind":"fr","id":"FR-MCP-JSON-001","title":"JSON FR","description":"JSON body"}]}')"

    printf '%s\n' "$params" | grep -q '^request:$'
    printf '%s\n' "$params" | grep -q '^  records:$'
    printf '%s\n' "$params" | grep -q 'FR-MCP-JSON-001'
    ! printf '%s\n' "$params" | grep -q '^  records: \[\]$'
}

@test "ISSUE-19 completeTurn recovery preserves relative indentation in multiline response" {
    run bash -c "printf '%s\n' 'response: |' '  summary' '    indented' 'interpretation: |' '  parsed' | node '${BATS_TEST_DIRNAME}/../lib-sh/complete-turn-to-recovery.js' 'Codex-20260626-issue19' 'Codex' 'codex' '2026-06-26T00:00:00Z' 'req-multiline' 'Multiline' '2026-06-26T00:01:00Z'"

    [ "$status" -eq 0 ]
    json="${output#sessionLog: }"
    printf '%s' "$json" | node -e 'const fs = require("fs"); const doc = JSON.parse(fs.readFileSync(0, "utf8")); if (doc.turns[0].response !== "summary\n  indented") process.exit(1);'
}
