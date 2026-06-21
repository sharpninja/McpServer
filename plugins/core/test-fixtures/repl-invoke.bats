#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"

@test "repl_invoke constructs valid YAML envelope with type, method, params, and requestId" {
    # Create a mock mcpserver-repl that echoes stdin so we can inspect the envelope
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat  # echo stdin to stdout
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "sessionlog.addTurn" "sessionId: abc-123
turnIndex: 1")

    # Verify envelope structure
    echo "$output" | grep -q "^type: request"
    echo "$output" | grep -q "^  method: sessionlog.addTurn"
    echo "$output" | grep -q "^  requestId: req-"
    echo "$output" | grep -q "^  params:"
    echo "$output" | grep -q "sessionId: abc-123"

    rm -rf "$TMPBIN"
}

@test "repl_invoke outputs the constructed YAML envelope correctly" {
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "todo.list")

    # Without params, the envelope should still have type, payload.requestId, and payload.method
    echo "$output" | grep -q "^type: request"
    echo "$output" | grep -q "method: todo.list"

    # Should NOT contain a params section when no params given
    if echo "$output" | grep -q "^  params:"; then
        false  # fail: params should not appear when empty
    fi

    rm -rf "$TMPBIN"
}

@test "repl_invoke requestId contains a timestamp in ISO format" {
    TMPBIN="$(mktemp -d)"
    cat > "$TMPBIN/mcpserver-repl" << 'MOCK'
#!/bin/bash
cat
MOCK
    chmod +x "$TMPBIN/mcpserver-repl"
    export PATH="$TMPBIN:$PATH"

    source "$SCRIPT_DIR/lib/repl-invoke.sh"
    output=$(repl_invoke "health.check")

    # Extract requestId value
    request_id=$(echo "$output" | grep "requestId:" | sed 's/.*requestId: //')

    # Should match pattern: req-YYYYMMDDTHHMMSSz-XXXX
    [[ "$request_id" =~ ^req-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{4}$ ]]

    rm -rf "$TMPBIN"
}

@test "repl_invoke returns exit 1 when mcpserver-repl is not available" {
    # Run in a subshell with restricted PATH containing only essential tools
    run bash -c '
        export PATH="/usr/bin:/bin"
        # Remove any mcpserver-repl from discoverable locations
        hash -r 2>/dev/null
        source "'"$SCRIPT_DIR"'/lib/repl-invoke.sh" 2>/dev/null
        repl_invoke "test.method" 2>&1
    '
    # Should fail because mcpserver-repl is not in /usr/bin or /bin
    if command -v mcpserver-repl >/dev/null 2>&1; then
        skip "mcpserver-repl is installed globally — cannot test unavailable path"
    fi
    [ "$status" -eq 1 ]
}

@test "repl-invoke.sh is syntactically valid bash" {
    run bash -n "$SCRIPT_DIR/lib/repl-invoke.sh"
    [ "$status" -eq 0 ]
}
