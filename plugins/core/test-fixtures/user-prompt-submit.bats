#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
USER_PROMPT_SUBMIT="$PLUGIN_ROOT/hooks/scripts/user-prompt-submit.sh"
source "$PLUGIN_ROOT/tests/cache-scope-helper.bash"

setup() {
    SANDBOX="$(mktemp -d)"
    mkdir -p "$SANDBOX/bin" "$SANDBOX/workspace"
    export CLAUDE_PLUGIN_ROOT="$PLUGIN_ROOT"
    export PLUGIN_ROOT_OVERRIDE="$SANDBOX"
    init_test_cache "$SANDBOX/workspace" "ClaudeCode-20260423T000000Z-test"
    now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

    cat > "$TEST_CACHE_DIR/session-state.yaml" <<EOF
status: verified
sessionId: ClaudeCode-20260423T000000Z-test
sourceType: ClaudeCode
title: Prompt submit test
model: claude-code
started: $now
lastUpdated: $now
workspacePath: "/tmp/ws"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "$now"
EOF

    cat > "$SANDBOX/bin/mcpserver-repl" <<'EOF'
#!/usr/bin/env bash
payload="$(cat)"
if grep -q 'workflow.memory.list' <<<"$payload"; then
cat <<'YAML'
type: result
payload:
  result:
    items:
    - id: MEMORY-REQ-001
      text: Keep exact wording.
    - id: MEMORY-USER-002
      text: Preserve workspace preference.
YAML
exit 0
fi
printf 'type: response\npayload:\n  ok: true\n'
EOF
    chmod +x "$SANDBOX/bin/mcpserver-repl"

    export PATH="$SANDBOX/bin:$PATH"
}

teardown() {
    rm -rf "$SANDBOX"
}

@test "user-prompt-submit opens a turn, writes cache, and emits default TODO guidance" {
    payload='{"prompt":"Investigate the failing flow."}'

    run bash "$USER_PROMPT_SUBMIT" <<<"$payload"

    [ "$status" -eq 0 ]
    grep -q '"status":"turn-opened"' <<<"$output"
    turn_file="$(test_cache_file current-turn.yaml)"
    [ -f "$turn_file" ]
    grep -q '^status: in_progress' "$turn_file"
    grep -q '^turnRequestId: req-' "$turn_file"
    grep -Fq "REQUIRED MEMORIES" <<<"$output"
    grep -Fq "MEMORY-REQ-001: Keep exact wording." <<<"$output"
    grep -Fq "MEMORY-USER-002: Preserve workspace preference." <<<"$output"
    grep -Fq "Use TODO and requirements tools only as needed." <<<"$output"
}

@test "user-prompt-submit discards cached session idle more than 24 hours before opening turn" {
    stale_id="ClaudeCode-20260423T000000Z-stale"
    cache_scope_select_session "$stale_id"
    TEST_CACHE_DIR="$CACHE_DIR"
    export TEST_CACHE_DIR
    fresh_id="ClaudeCode-20260424T000000Z-fresh"
    cat > "$SANDBOX/session-start-stub.sh" <<EOF
#!/usr/bin/env bash
set -euo pipefail
source "$PLUGIN_ROOT/lib/cache-scope.sh"
cache_scope_init "$SANDBOX" "$TEST_WORKSPACE"
cache_scope_select_session "$fresh_id"
cat > "\$CACHE_DIR/session-state.yaml" <<STATE
status: verified
sessionId: $fresh_id
sourceType: ClaudeCode
title: Fresh prompt submit test
model: claude-code
started: $(date -u +%Y-%m-%dT%H:%M:%SZ)
lastUpdated: $(date -u +%Y-%m-%dT%H:%M:%SZ)
workspacePath: "$TEST_WORKSPACE"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
STATE
EOF
    chmod +x "$SANDBOX/session-start-stub.sh"
    export MCP_SESSION_START_SCRIPT="$SANDBOX/session-start-stub.sh"
    cat > "$TEST_CACHE_DIR/session-state.yaml" <<EOF
status: verified
sessionId: $stale_id
sourceType: ClaudeCode
title: Stale prompt submit test
model: claude-code
started: 1970-01-01T00:00:00Z
lastUpdated: 1970-01-01T00:00:00Z
workspacePath: "$TEST_WORKSPACE"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "1970-01-01T00:00:00Z"
EOF
    payload='{"prompt":"Open a new turn after stale cache."}'

    run bash "$USER_PROMPT_SUBMIT" <<<"$payload"

    [ "$status" -eq 0 ]
    grep -q '"status":"turn-opened"' <<<"$output"
    active_session="$(head -1 "$MCP_PLUGIN_WORKSPACE_CACHE_DIR/active-session")"
    [ "$active_session" = "$fresh_id" ]
    refresh_test_cache
    grep -q '^sessionId: ' "$TEST_CACHE_DIR/session-state.yaml"
    ! grep -q "$stale_id" "$TEST_CACHE_DIR/session-state.yaml"
    [ -f "$(test_cache_file current-turn.yaml)" ]
}

@test "user-prompt-submit emits MCP-backed internal TODO guidance when enabled" {
    export MCP_CODEX_INTERNAL_TODO=1
    payload='{"prompt":"Implement the next slice."}'

    run bash "$USER_PROMPT_SUBMIT" <<<"$payload"

    [ "$status" -eq 0 ]
    grep -q '"status":"turn-opened"' <<<"$output"
    grep -Fq "MCP-backed internal TODO tracking is enabled." <<<"$output"
    grep -Fq "workflow.todo.*" <<<"$output"
    ! grep -Fq "Use TODO and requirements tools only as needed." <<<"$output"
}
