#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
SCRIPT_DIR="$(core_stage)"

setup() {
    # Override PLUGIN_ROOT to a temp directory so cache ops use isolated storage
    TEST_PLUGIN_ROOT="$(mktemp -d)"
    export PLUGIN_ROOT_OVERRIDE="$TEST_PLUGIN_ROOT"
    source "$SCRIPT_DIR/lib/cache-manager.sh"
}

teardown() {
    rm -rf "$TEST_PLUGIN_ROOT"
}

@test "cache_write creates a YAML file in cache/pending/ directory" {
    result=$(cache_write "sessionlog.addTurn" "sessionId: test-123")
    [ -f "$result" ]
    [[ "$result" == *"/cache/pending/"* ]]
    [[ "$result" == *".yaml" ]]
}

@test "cache_write uses monotonic sequence numbering (001, 002, 003...)" {
    file1=$(cache_write "method.one" "key: val1")
    file2=$(cache_write "method.two" "key: val2")
    file3=$(cache_write "method.three" "key: val3")

    # Extract sequence from filename
    basename1=$(basename "$file1")
    basename2=$(basename "$file2")
    basename3=$(basename "$file3")

    [[ "$basename1" == 001-* ]]
    [[ "$basename2" == 002-* ]]
    [[ "$basename3" == 003-* ]]
}

@test "cache_write stores method, params, and timestamp in the YAML file" {
    file=$(cache_write "todo.create" "title: Buy milk
priority: high")

    grep -q "^method: todo.create" "$file"
    grep -q "title: Buy milk" "$file"
    grep -q "priority: high" "$file"
    grep -q "^timestamp:" "$file"
    grep -q "^retryCount: 0" "$file"
}

@test "cache_status returns 0 when no pending items" {
    count=$(cache_status)
    [ "$count" -eq 0 ]
}

@test "cache_status returns correct count of pending items" {
    cache_write "m1" "p: 1" >/dev/null
    cache_write "m2" "p: 2" >/dev/null
    cache_write "m3" "p: 3" >/dev/null

    count=$(cache_status)
    [ "$count" -eq 3 ]
}

@test "cache_flush removes items from pending/ after successful replay" {
    # Create a mock repl_invoke that always succeeds
    repl_invoke() { return 0; }
    export -f repl_invoke

    cache_write "ok.method" "data: yes" >/dev/null
    cache_write "ok.method2" "data: also" >/dev/null

    [ "$(cache_status)" -eq 2 ]

    result=$(cache_flush)
    [ "$(cache_status)" -eq 0 ]
    [[ "$result" == *"flushed=2"* ]]
}

@test "cache_flush increments retryCount on failure" {
    # Create a mock repl_invoke that always fails
    repl_invoke() { return 1; }
    export -f repl_invoke

    file=$(cache_write "fail.method" "data: nope")

    cache_flush >/dev/null

    retry_count=$(grep "^retryCount:" "$file" | sed 's/retryCount: *//')
    [ "$retry_count" -eq 1 ]

    cache_flush >/dev/null

    retry_count=$(grep "^retryCount:" "$file" | sed 's/retryCount: *//')
    [ "$retry_count" -eq 2 ]
}

@test "cache_flush skips items with retryCount >= 3" {
    # Create a mock repl_invoke that always fails
    repl_invoke() { return 1; }
    export -f repl_invoke

    file=$(cache_write "doomed.method" "data: hopeless")

    # Manually set retryCount to 3
    sed -i "s/^retryCount: .*/retryCount: 3/" "$file"

    result=$(cache_flush)
    # Should report 0 flushed and 0 failed (skipped entirely)
    [[ "$result" == *"flushed=0"* ]]
    [[ "$result" == *"failed=0"* ]]

    # File should still exist with retryCount=3 (unchanged)
    retry_count=$(grep "^retryCount:" "$file" | sed 's/retryCount: *//')
    [ "$retry_count" -eq 3 ]
}

@test "cache_flush processes items in order" {
    # Track invocation order via a temp file
    ORDER_FILE="$(mktemp)"
    repl_invoke() {
        echo "$1" >> "$ORDER_FILE"
        return 0
    }
    export -f repl_invoke
    export ORDER_FILE

    cache_write "first.method" "seq: 1" >/dev/null
    cache_write "second.method" "seq: 2" >/dev/null
    cache_write "third.method" "seq: 3" >/dev/null

    cache_flush >/dev/null

    # Verify methods were invoked in order
    line1=$(sed -n '1p' "$ORDER_FILE")
    line2=$(sed -n '2p' "$ORDER_FILE")
    line3=$(sed -n '3p' "$ORDER_FILE")

    [ "$line1" = "first.method" ]
    [ "$line2" = "second.method" ]
    [ "$line3" = "third.method" ]

    rm -f "$ORDER_FILE"
}

@test "cache-manager.sh is syntactically valid bash" {
    run bash -n "$SCRIPT_DIR/lib/cache-manager.sh"
    [ "$status" -eq 0 ]
}
