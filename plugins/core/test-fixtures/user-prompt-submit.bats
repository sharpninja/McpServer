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

    cat > "$TEST_CACHE_DIR/session-state.yaml" <<'EOF'
status: verified
sessionId: ClaudeCode-20260423T000000Z-test
sourceType: ClaudeCode
title: Prompt submit test
model: claude-code
started: 2026-04-23T00:00:00Z
lastUpdated: 2026-04-23T00:00:00Z
workspacePath: "/tmp/ws"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "2026-04-23T00:00:00Z"
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
