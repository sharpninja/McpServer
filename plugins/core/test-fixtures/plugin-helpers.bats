#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
source "$PLUGIN_ROOT/tests/cache-scope-helper.bash"

to_host_path() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s' "$1"
    fi
}

find_pwsh() {
    if command -v pwsh >/dev/null 2>&1; then
        command -v pwsh
        return 0
    fi

    command -v pwsh.exe 2>/dev/null || true
}

setup() {
    SANDBOX="$(mktemp -d)"
    mkdir -p "$SANDBOX/bin" "$SANDBOX/workspace"
    export PATH="$SANDBOX/bin:$PATH"
    export CLAUDE_PLUGIN_ROOT="$PLUGIN_ROOT"
    export PLUGIN_ROOT_OVERRIDE="$SANDBOX"
    export STUB_LOG="$SANDBOX/repl-calls.log"
    init_test_cache "$SANDBOX/workspace" "ClaudeCode-20260521T000000Z-helper"

    cat > "$SANDBOX/bin/mcpserver-repl" <<'STUB'
#!/usr/bin/env bash
input="$(cat)"
{
    printf '%s\n' "$input"
    printf '%s\n' '---'
} >> "${STUB_LOG:-/dev/null}"
printf 'type: response\npayload:\n  ok: true\n'
STUB
    chmod +x "$SANDBOX/bin/mcpserver-repl"
}

teardown() {
    rm -rf "$SANDBOX"
}

write_turn_state() {
    cat > "$(test_cache_file session-state.yaml)" <<EOF
sourceType: ClaudeCode
sessionId: $TEST_SESSION_ID
status: verified
EOF

    cat > "$(test_cache_file current-turn.yaml)" <<EOF
sourceType: ClaudeCode
sessionId: $TEST_SESSION_ID
turnRequestId: req-helper-001
queryTitle: helper test
status: in_progress
codeEdits: 0
lastBuildStatus: unknown
EOF
}

@test "mcp.claude.status reports cache, marker, session, turn, namespaces, and wrappers" {
    write_turn_state

    run bash -c 'cd "$1" && bash "$2/lib/mcp.claude.status.sh"' _ "$TEST_WORKSPACE" "$PLUGIN_ROOT"

    [ "$status" -eq 0 ]
    grep -Fq "mcp.claude.status:" <<<"$output"
    grep -Fq "pluginRoot:" <<<"$output"
    grep -Fq "cacheDir:" <<<"$output"
    grep -Fq "trust: 'missing'" <<<"$output"
    grep -Fq "sessionId: '$TEST_SESSION_ID'" <<<"$output"
    grep -Fq "turnRequestId: 'req-helper-001'" <<<"$output"
    grep -Fq "workflow.sessionlog" <<<"$output"
    grep -Fq "workflow.todo" <<<"$output"
    grep -Fq "workflow.requirements" <<<"$output"
    grep -Fq "workflow.graphrag" <<<"$output"
    grep -Fq "Invoke-ClaudeMcpPlugin.ps1" <<<"$output"
}

@test "final-response helper completes the scoped current turn" {
    write_turn_state

    run bash "$PLUGIN_ROOT/lib/final-response.sh" "completed by helper"

    [ "$status" -eq 0 ]
    grep -q '^status: completed' "$(test_cache_file current-turn.yaml)"
}

@test "PowerShell wrapper passes params through native parameter without shell expansion" {
    write_turn_state
    pwsh_bin="$(find_pwsh)"
    [ -n "$pwsh_bin" ] || skip "pwsh is not available"

    run "$pwsh_bin" -NoLogo -NoProfile -File "$(to_host_path "$PLUGIN_ROOT/lib/Invoke-ClaudeMcpPlugin.ps1")" \
        -Command Invoke \
        -Method client.Todo.QueryAsync \
        -Params 'keyword: $(cat /should-not-run)' \
        -PluginRoot "$(to_host_path "$PLUGIN_ROOT")" \
        -CacheRoot "$(to_host_path "$SANDBOX")" \
        -WorkspacePath "$(to_host_path "$TEST_WORKSPACE")" \
        -BashPath "$(to_host_path "$BASH")"

    [ "$status" -eq 0 ]
    grep -Fq 'keyword: $(cat /should-not-run)' "$STUB_LOG"
}

@test "PowerShell wrapper passes params through stdin without shell expansion" {
    write_turn_state
    pwsh_bin="$(find_pwsh)"
    [ -n "$pwsh_bin" ] || skip "pwsh is not available"

    run bash -c '
        printf "%s\n" "keyword: from-stdin" "literal: \$(cat /should-not-run)" |
            "$1" -NoLogo -NoProfile -File "$2" \
                -Command Invoke \
                -Method client.Todo.QueryAsync \
                -PluginRoot "$3" \
                -CacheRoot "$4" \
                -WorkspacePath "$5" \
                -BashPath "$6"
    ' _ \
        "$pwsh_bin" \
        "$(to_host_path "$PLUGIN_ROOT/lib/Invoke-ClaudeMcpPlugin.ps1")" \
        "$(to_host_path "$PLUGIN_ROOT")" \
        "$(to_host_path "$SANDBOX")" \
        "$(to_host_path "$TEST_WORKSPACE")" \
        "$(to_host_path "$BASH")"

    [ "$status" -eq 0 ]
    grep -Fq 'keyword: from-stdin' "$STUB_LOG"
    grep -Fq 'literal: $(cat /should-not-run)' "$STUB_LOG"
}

@test "PowerShell wrapper status uses Claude helper and scoped cache" {
    write_turn_state
    pwsh_bin="$(find_pwsh)"
    [ -n "$pwsh_bin" ] || skip "pwsh is not available"

    run "$pwsh_bin" -NoLogo -NoProfile -File "$(to_host_path "$PLUGIN_ROOT/lib/Invoke-ClaudeMcpPlugin.ps1")" \
        -Command Status \
        -PluginRoot "$(to_host_path "$PLUGIN_ROOT")" \
        -CacheRoot "$(to_host_path "$SANDBOX")" \
        -WorkspacePath "$(to_host_path "$TEST_WORKSPACE")" \
        -BashPath "$(to_host_path "$BASH")"

    [ "$status" -eq 0 ]
    grep -Fq "mcp.claude.status:" <<<"$output"
    grep -Fq "turnRequestId: 'req-helper-001'" <<<"$output"
}
