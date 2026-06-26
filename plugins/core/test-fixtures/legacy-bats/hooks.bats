#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

# Tests for Phase 3 hook scripts.
# Each test validates syntax, shebang, or functional behavior with mocked dependencies.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"
HOOKS_DIR="$SCRIPT_DIR/hooks/scripts"

# Convert a MINGW/MSYS /x/ style path to a Node-compatible path.
# On Windows (MINGW), /f/foo -> F:/foo; on Linux/Mac, path is unchanged.
_node_path() {
    local p="$1"
    if [[ "$p" =~ ^/([a-zA-Z])/ ]]; then
        echo "${BASH_REMATCH[1]^^}:/${p:3}"
    else
        echo "$p"
    fi
}

# ---------------------------------------------------------------------------
# Syntax / shebang helpers
# ---------------------------------------------------------------------------

_assert_shebang() {
    local file="$1"
    run head -1 "$file"
    [ "$status" -eq 0 ]
    [[ "$output" == "#!/usr/bin/env bash" ]] || [[ "$output" == "#!/bin/bash" ]]
}

_assert_syntax() {
    local file="$1"
    run bash -n "$file"
    [ "$status" -eq 0 ]
}

_assert_executable() {
    local file="$1"
    [ -x "$file" ]
}

# ---------------------------------------------------------------------------
# hooks.json
# ---------------------------------------------------------------------------

@test "hooks.json exists and is valid JSON" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "JSON.parse(require('fs').readFileSync('${json_path}', 'utf8'));"
    [ "$status" -eq 0 ]
}

@test "hooks.json contains SessionStart hook" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "const d=JSON.parse(require('fs').readFileSync('${json_path}','utf8')); process.exit(d.hooks && 'SessionStart' in d.hooks ? 0 : 1);"
    [ "$status" -eq 0 ]
}

@test "hooks.json contains SessionEnd hook" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "const d=JSON.parse(require('fs').readFileSync('${json_path}','utf8')); process.exit(d.hooks && 'SessionEnd' in d.hooks ? 0 : 1);"
    [ "$status" -eq 0 ]
}

@test "hooks.json contains PreCompact hook" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "const d=JSON.parse(require('fs').readFileSync('${json_path}','utf8')); process.exit(d.hooks && 'PreCompact' in d.hooks ? 0 : 1);"
    [ "$status" -eq 0 ]
}

@test "hooks.json contains PostCompact hook" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "const d=JSON.parse(require('fs').readFileSync('${json_path}','utf8')); process.exit(d.hooks && 'PostCompact' in d.hooks ? 0 : 1);"
    [ "$status" -eq 0 ]
}

@test "hooks.json contains PostToolUse hook" {
    local json_path; json_path="$(_node_path "$SCRIPT_DIR/hooks/hooks.json")"
    run node -e "const d=JSON.parse(require('fs').readFileSync('${json_path}','utf8')); process.exit(d.hooks && 'PostToolUse' in d.hooks ? 0 : 1);"
    [ "$status" -eq 0 ]
}

# ---------------------------------------------------------------------------
# session-start.sh
# ---------------------------------------------------------------------------

@test "session-start.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/session-start.sh"
}

@test "session-start.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/session-start.sh"
}

@test "session-start.sh is executable" {
    _assert_executable "$HOOKS_DIR/session-start.sh"
}

@test "session-start.sh writes session-state.yaml on successful bootstrap" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    # Create stub for full_bootstrap that succeeds and exports required vars
    TMPBIN="$(mktemp -d)"
    # Stub repl_invoke to succeed and return a fake session id
    cat > "$TMPBIN/repl_invoke_stub.sh" << 'EOF'
repl_invoke() {
    echo "sessionId: stub-session-123"
    return 0
}
export -f repl_invoke
EOF

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        # Mock full_bootstrap
        full_bootstrap() {
            export MCPSERVER_BASE_URL='http://localhost:7147'
            export MCPSERVER_API_KEY='test-key'
            export MCPSERVER_WORKSPACE='TestWorkspace'
            export MCPSERVER_WORKSPACE_PATH='/tmp/test'
            return 0
        }
        export -f full_bootstrap

        # Mock find_marker_file
        find_marker_file() { echo '/tmp/fake-marker.yaml'; return 0; }
        export -f find_marker_file

        # Mock repl_invoke
        repl_invoke() { echo 'sessionId: stub-session-123'; return 0; }
        export -f repl_invoke

        # Run the script with mocked functions pre-exported
        source '$HOOKS_DIR/session-start.sh'
    "
    [ "$output" = "{}" ]
    # The session state file should have been created
    [ -f "$TEST_PLUGIN_ROOT/cache/session-state.yaml" ]
    rm -rf "$TEST_PLUGIN_ROOT" "$TMPBIN"
}

@test "session-start.sh writes MCP_UNTRUSTED to session-state.yaml when bootstrap fails" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        # Mock full_bootstrap to fail
        full_bootstrap() { return 1; }
        export -f full_bootstrap

        find_marker_file() { return 1; }
        export -f find_marker_file

        source '$HOOKS_DIR/session-start.sh'
    " || true

    [ "$output" = "{}" ]
    state_file="$(find "$TEST_PLUGIN_ROOT/cache" -name session-state.yaml | head -1)"
    [ -f "$state_file" ]
    grep -q "MCP_UNTRUSTED" "$state_file"
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# session-end.sh
# ---------------------------------------------------------------------------

@test "session-end.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/session-end.sh"
}

@test "session-end.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/session-end.sh"
}

@test "session-end.sh is executable" {
    _assert_executable "$HOOKS_DIR/session-end.sh"
}

@test "session-end.sh calls cache_flush" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"
    FLUSH_CALLED="$TEST_PLUGIN_ROOT/cache/flush_called"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        # Track cache_flush invocation
        cache_flush() { touch '$FLUSH_CALLED'; echo 'flushed=0 failed=0 pending=0'; }
        export -f cache_flush

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/session-end.sh'
    " || true

    [ "$output" = "{}" ]
    [ -f "$FLUSH_CALLED" ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# pre-compact.sh
# ---------------------------------------------------------------------------

@test "pre-compact.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/pre-compact.sh"
}

@test "pre-compact.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/pre-compact.sh"
}

@test "pre-compact.sh is executable" {
    _assert_executable "$HOOKS_DIR/pre-compact.sh"
}

@test "pre-compact.sh flushes cache before compaction" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"
    FLUSH_CALLED="$TEST_PLUGIN_ROOT/cache/flush_called"

    # Write a minimal session-state.yaml
    cat > "$TEST_PLUGIN_ROOT/cache/session-state.yaml" << 'EOF'
status: verified
sessionId: test-session-001
EOF

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        cache_flush() { touch '$FLUSH_CALLED'; echo 'flushed=0 failed=0 pending=0'; }
        export -f cache_flush

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/pre-compact.sh'
    " || true

    [ "$output" = "{}" ]
    [ -f "$FLUSH_CALLED" ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# post-compact.sh
# ---------------------------------------------------------------------------

@test "post-compact.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/post-compact.sh"
}

@test "post-compact.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/post-compact.sh"
}

@test "post-compact.sh is executable" {
    _assert_executable "$HOOKS_DIR/post-compact.sh"
}

@test "post-compact.sh emits schema-valid no-op JSON" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        full_bootstrap() {
            export MCPSERVER_BASE_URL='http://localhost:7147'
            export MCPSERVER_API_KEY='test-key'
            export MCPSERVER_WORKSPACE='TestWorkspace'
            export MCPSERVER_WORKSPACE_PATH='/tmp/test'
            return 0
        }
        export -f full_bootstrap

        find_marker_file() { echo '/tmp/fake.yaml'; return 0; }
        export -f find_marker_file

        repl_invoke() { echo 'history: []'; return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/post-compact.sh'
    "
    [ "$output" = "{}" ]
    [[ "$output" != *"additionalContext"* ]]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# plan-approved.sh
# ---------------------------------------------------------------------------

@test "plan-approved.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/plan-approved.sh"
}

@test "plan-approved.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/plan-approved.sh"
}

@test "plan-approved.sh is executable" {
    _assert_executable "$HOOKS_DIR/plan-approved.sh"
}

@test "plan-approved.sh extracts title from first # heading in plan file" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    # Create a fake plan file
    PLAN_FILE="$TEST_PLUGIN_ROOT/my-plan.md"
    cat > "$PLAN_FILE" << 'EOF'
# Implement User Authentication

## Overview
This plan covers OAuth2 integration.

## Tasks
- Add OAuth provider
EOF

    TODO_CREATED="$TEST_PLUGIN_ROOT/cache/todo_created"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='$PLAN_FILE'

        repl_invoke() {
            # Capture what was passed - look for the title
            echo \"\$@\" > '$TODO_CREATED'
            return 0
        }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-approved.sh'
    "

    # Verify the TODO was created and title was extracted
    [ -f "$TODO_CREATED" ]
    grep -q "Implement User Authentication" "$TODO_CREATED"
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-approved.sh writes to plan-todo-map.yaml after creating TODO" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    PLAN_FILE="$TEST_PLUGIN_ROOT/feature-plan.md"
    cat > "$PLAN_FILE" << 'EOF'
# Add Feature Flags

Simple plan.
EOF

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='$PLAN_FILE'

        repl_invoke() {
            echo 'id: TODO-FEAT-001'
            return 0
        }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-approved.sh'
    "

    [ -f "$TEST_PLUGIN_ROOT/cache/plan-todo-map.yaml" ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# plan-modified.sh
# ---------------------------------------------------------------------------

@test "plan-modified.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/plan-modified.sh"
}

@test "plan-modified.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/plan-modified.sh"
}

@test "plan-modified.sh is executable" {
    _assert_executable "$HOOKS_DIR/plan-modified.sh"
}

@test "plan-modified.sh skips silently when no plan-todo-map entry exists" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    # No plan-todo-map.yaml created
    INVOKE_CALLED="$TEST_PLUGIN_ROOT/cache/invoke_called"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='/some/plan/file.md'

        repl_invoke() { touch '$INVOKE_CALLED'; return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-modified.sh'
    "

    [ "$status" -eq 0 ]
    [ ! -f "$INVOKE_CALLED" ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-modified.sh calls repl_invoke when plan-todo-map entry exists" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    PLAN_FILE="/tmp/test-plans/my-plan.md"
    TODO_ID="PLAN-FEAT-001"

    # Create plan-todo-map with the entry
    cat > "$TEST_PLUGIN_ROOT/cache/plan-todo-map.yaml" << EOF
entries:
  - planFile: $PLAN_FILE
    todoId: $TODO_ID
EOF

    INVOKE_CALLED="$TEST_PLUGIN_ROOT/cache/invoke_called"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='$PLAN_FILE'

        repl_invoke() { touch '$INVOKE_CALLED'; return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-modified.sh'
    "

    [ "$status" -eq 0 ]
    [ -f "$INVOKE_CALLED" ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# Regression: PostToolUse hookSpecificOutput must carry hookEventName or Claude
# Code rejects it with "missing required field hookEventName" (non-blocking).
# Asserts every plan-modified.sh output path includes the PostToolUse event name.

@test "plan-modified.sh no-file-path output includes PostToolUse hookEventName" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        unset TOOL_INPUT

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-modified.sh'
    "

    [ "$status" -eq 0 ]
    echo "$output" | grep -qF '"hookEventName":"PostToolUse"'
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-modified.sh no-map skip output includes PostToolUse hookEventName" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='/some/plan/file.md'

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-modified.sh'
    "

    [ "$status" -eq 0 ]
    echo "$output" | grep -qF '"hookEventName":"PostToolUse"'
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-modified.sh updated output includes PostToolUse hookEventName" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    PLAN_FILE="/tmp/test-plans/my-plan.md"
    cat > "$TEST_PLUGIN_ROOT/cache/plan-todo-map.yaml" << EOF
entries:
  - planFile: $PLAN_FILE
    todoId: PLAN-FEAT-001
EOF

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='$PLAN_FILE'

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-modified.sh'
    "

    [ "$status" -eq 0 ]
    echo "$output" | grep -qF '"hookEventName":"PostToolUse"'
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-approved.sh no-plan skip output includes PostToolUse hookEventName" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        unset TOOL_INPUT

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-approved.sh'
    "

    [ "$status" -eq 0 ]
    echo "$output" | grep -qF '"hookEventName":"PostToolUse"'
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "plan-approved.sh created output includes PostToolUse hookEventName" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache"

    PLAN_FILE="$TEST_PLUGIN_ROOT/feature-plan.md"
    cat > "$PLAN_FILE" << 'EOF'
# Add Feature Flags

Simple plan.
EOF

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'
        export TOOL_INPUT='$PLAN_FILE'

        repl_invoke() { echo 'id: TODO-FEAT-001'; return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/plan-approved.sh'
    "

    [ "$status" -eq 0 ]
    echo "$output" | grep -qF '"hookEventName":"PostToolUse"'
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# cache-flush.sh
# ---------------------------------------------------------------------------

@test "cache-flush.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/cache-flush.sh"
}

@test "cache-flush.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/cache-flush.sh"
}

@test "cache-flush.sh is executable" {
    _assert_executable "$HOOKS_DIR/cache-flush.sh"
}

@test "cache-flush.sh outputs flush result summary" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    mkdir -p "$TEST_PLUGIN_ROOT/cache/pending"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        repl_invoke() { return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/cache-flush.sh'
    "

    [ "$status" -eq 0 ]
    [[ "$output" == *"flushed="* ]]
    rm -rf "$TEST_PLUGIN_ROOT"
}

# ---------------------------------------------------------------------------
# health-check.sh
# ---------------------------------------------------------------------------

@test "health-check.sh is syntactically valid bash" {
    _assert_syntax "$HOOKS_DIR/health-check.sh"
}

@test "health-check.sh has a shebang" {
    _assert_shebang "$HOOKS_DIR/health-check.sh"
}

@test "health-check.sh is executable" {
    _assert_executable "$HOOKS_DIR/health-check.sh"
}

@test "health-check.sh exits 0 when repl_invoke succeeds" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        repl_invoke() { echo 'status: ok'; return 0; }
        export -f repl_invoke

        source '$HOOKS_DIR/health-check.sh'
    "
    [ "$status" -eq 0 ]
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "health-check.sh exits 1 when repl_invoke fails" {
    TEST_PLUGIN_ROOT="$(mktemp -d)"

    run bash -c "
        export CLAUDE_PLUGIN_ROOT='$TEST_PLUGIN_ROOT'
        export PLUGIN_ROOT_OVERRIDE='$TEST_PLUGIN_ROOT'

        repl_invoke() { return 1; }
        export -f repl_invoke

        source '$HOOKS_DIR/health-check.sh'
    "
    [ "$status" -eq 1 ]
    rm -rf "$TEST_PLUGIN_ROOT"
}
