init_test_cache() {
    TEST_WORKSPACE="${1:-$SANDBOX/workspace}"
    TEST_SESSION_ID="${2:-Codex-20260419T000000Z-test}"
    mkdir -p "$TEST_WORKSPACE"
    export MCP_WORKSPACE_PATH="$TEST_WORKSPACE"
    export MCPSERVER_WORKSPACE_PATH="$TEST_WORKSPACE"

    # shellcheck source=../lib/cache-scope.sh
    source "$PLUGIN_ROOT/lib/cache-scope.sh"
    cache_scope_init "$SANDBOX" "$TEST_WORKSPACE"
    cache_scope_select_session "$TEST_SESSION_ID"
    TEST_CACHE_DIR="$CACHE_DIR"
    export TEST_WORKSPACE TEST_SESSION_ID TEST_CACHE_DIR
}

refresh_test_cache() {
    # shellcheck source=../lib/cache-scope.sh
    source "$PLUGIN_ROOT/lib/cache-scope.sh"
    cache_scope_init "$SANDBOX" "${TEST_WORKSPACE:-$SANDBOX/workspace}"
    TEST_CACHE_DIR="$CACHE_DIR"
    export TEST_CACHE_DIR
}

test_cache_file() {
    refresh_test_cache
    printf '%s/%s' "$TEST_CACHE_DIR" "$1"
}
