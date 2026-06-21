init_test_cache() {
    TEST_WORKSPACE="${1:-$SANDBOX/workspace}"
    TEST_SESSION_ID="${2:-Codex-20260419T000000Z-test}"
    mkdir -p "$TEST_WORKSPACE"
    export MCP_WORKSPACE_PATH="$TEST_WORKSPACE"
    export MCPSERVER_WORKSPACE_PATH="$TEST_WORKSPACE"

    # Determinism guard: pin the cache base via the highest-precedence knob
    # (MCP_CACHE_DIR_OVERRIDE) so the hook-under-test and this helper resolve
    # the SAME scoped cache dir regardless of WHEN the calling test exports
    # PLUGIN_ROOT_OVERRIDE. Without this, a test that sets PLUGIN_ROOT_OVERRIDE
    # AFTER init_test_cache flips resolve_cache_dir between its override and
    # workspace-env branches, so writer and reader disagree and the hook
    # reports no-session. resolve_cache_dir returns this value verbatim as the
    # base; cache_scope_init appends the workspace/session scope exactly once.
    export MCP_CACHE_DIR_OVERRIDE="$SANDBOX/cache"

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
