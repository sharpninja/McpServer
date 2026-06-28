#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
LIB="$PLUGIN_ROOT/lib/repl-invoke.sh"
source "$LIB"

setup() {
    SANDBOX="$(mktemp -d)"
    export REPL_INVOKE_CACHE_DIR="$SANDBOX/cache"
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    cat > "$REPL_INVOKE_CACHE_DIR/current-turn.yaml" <<'YAML'
requestId: req-upsert-001
openedAt: 2026-06-11T00:00:00Z
queryText: |
  structured query
YAML
}

teardown() {
    rm -rf "$SANDBOX"
}

install_success_stubs() {
    _repl_session_meta() { printf '%s\n' 'TestAgent TestAgent-session'; }
    _repl_session_state_value() {
        case "$1" in
            title) printf '%s\n' 'Session Title' ;;
            model) printf '%s\n' 'test-model' ;;
            started) printf '%s\n' '2026-06-11T00:00:00Z' ;;
        esac
    }
    _repl_failsafe_write() {
        printf '%s\n' "$1" > "$SANDBOX/failsafe-method"
        printf '%s\n' "$2" > "$SANDBOX/failsafe-params"
        printf '%s\n' "$SANDBOX/failsafe.yaml"
    }
    _repl_failsafe_clear() { printf '%s\n' "$1" > "$SANDBOX/cleared"; }
    _repl_invoke_raw_in_workspace() {
        printf '%s\n' "$1" >> "$SANDBOX/methods"
        printf '%s\n' "$2" > "$SANDBOX/upsert-params"
        printf 'type: response\npayload:\n  ok: true\n'
        return 0
    }
    _repl_response_is_error() { return 1; }
    _repl_submit_session() {
        printf 'submit called\n' >> "$SANDBOX/submit"
        return 0
    }
}

install_method_missing_stubs() {
    install_success_stubs
    _repl_invoke_raw_in_workspace() {
        printf '%s\n' "$1" >> "$SANDBOX/methods"
        printf 'type: error\npayload:\n  code: method_not_found\n  message: missing\n'
        return 0
    }
    _repl_response_is_error() { printf '%s\n' "${1:-}" | grep -q 'type: error'; }
    _repl_submit_session() {
        printf '%s %s\n' "$1" "$2" > "$SANDBOX/submit"
        return 0
    }
}

@test "turn upsert payload omits absent structured collections" {
    run _repl_turn_upsert_params 'TestAgent' 'TestAgent-session' 'req-upsert-001' 'Upsert Title' 'completed' 'Done' ''

    [ "$status" -eq 0 ]
    [[ "$output" == *"agent: TestAgent"* ]]
    [[ "$output" == *"sessionId: TestAgent-session"* ]]
    [[ "$output" == *"requestId: req-upsert-001"* ]]
    [[ "$output" == *"structured query"* ]]
    [[ "$output" != *"actions:"* ]]
    [[ "$output" != *"filesModified:"* ]]
}

@test "turn upsert payload includes supplied actions and file paths" {
    actions=$'  - type: file_change\n    status: completed\n    filePath: src/example.cs'

    run _repl_turn_upsert_params 'TestAgent' 'TestAgent-session' 'req-upsert-001' 'Upsert Title' 'completed' 'Done' "$actions"

    [ "$status" -eq 0 ]
    [[ "$output" == *"actions:"* ]]
    [[ "$output" == *"filePath: src/example.cs"* ]]
    [[ "$output" == *"filesModified:"* ]]
    [[ "$output" == *"- src/example.cs"* ]]
}

@test "persist turn uses UpsertTurnAsync without full session submit on success" {
    install_success_stubs

    run _repl_persist_turn 'req-upsert-001' 'Upsert Title' 'completed' 'Done' ''

    [ "$status" -eq 0 ]
    grep -q 'client.SessionLog.UpsertTurnAsync' "$SANDBOX/methods"
    grep -q '^client.SessionLog.UpsertTurnAsync$' "$SANDBOX/failsafe-method"
    ! test -e "$SANDBOX/submit"
    ! grep -q '^  actions:' "$SANDBOX/upsert-params"
}

@test "persist turn falls back to SubmitAsync only when UpsertTurnAsync is missing" {
    install_method_missing_stubs

    run _repl_persist_turn 'req-upsert-001' 'Upsert Title' 'completed' 'Done' ''

    [ "$status" -eq 0 ]
    [ "$(grep -c 'client.SessionLog.UpsertTurnAsync' "$SANDBOX/methods")" -eq 2 ]
    grep -q 'TestAgent TestAgent-session' "$SANDBOX/submit"
}
