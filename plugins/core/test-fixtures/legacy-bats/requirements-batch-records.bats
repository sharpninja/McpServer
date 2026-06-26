#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
LIB="$PLUGIN_ROOT/lib/repl-invoke.sh"

setup() {
    SANDBOX="$(mktemp -d)"
    mkdir -p "$SANDBOX/cache" "$SANDBOX/workspace"
    export PLUGIN_ROOT_OVERRIDE="$SANDBOX/cache"
    export MCP_WORKSPACE_PATH="$SANDBOX/workspace"
    export MCPSERVER_WORKSPACE_PATH="$SANDBOX/workspace"
}

teardown() {
    rm -rf "$SANDBOX"
}

batch_operations() {
    printf '%s\n' \
        createFrBatch updateFrBatch \
        createTrBatch updateTrBatch \
        createTestBatch updateTestBatch \
        createBatch updateBatch
}

assert_batch_payload_preserved() {
    local payload="$1"
    local operation typed

    _repl_schema_has_records "$payload"

    while IFS= read -r operation; do
        typed="$(_repl_requirements_typed_params "$operation" "$payload")"
        grep -Fq 'request:' <<<"$typed"
        grep -Fq 'records:' <<<"$typed"
        grep -Fq 'FR-LOC-001' <<<"$typed"
        grep -Fq 'acceptanceCriteria:' <<<"$typed"
        grep -Fq '        - id: FR-LOC-001-AC001' <<<"$typed"
        grep -Fq 'isSatisfied: false' <<<"$typed"
    done < <(batch_operations)
}

@test "requirement batch parser accepts unindented PowerShell YAML records" {
    source "$LIB"
    payload='records:
- id: FR-LOC-001
  title: Monitor device location
  description: The system SHALL monitor the device location while tracking is enabled.
  priority: high
  status: pending
  area: LOC
  acceptanceCriteria:
  - id: FR-LOC-001-AC001
    text: Demonstrates behavior for FR-LOC-001.
    isSatisfied: false'

    assert_batch_payload_preserved "$payload"
}

@test "requirement batch parser accepts indented YAML and inline JSON records" {
    source "$LIB"
    indented_payload='records:
  - id: FR-LOC-001
    title: Monitor device location
    description: The system SHALL monitor the device location while tracking is enabled.
    priority: high
    status: pending
    area: LOC
    acceptanceCriteria:
      - id: FR-LOC-001-AC001
        text: Demonstrates behavior for FR-LOC-001.
        isSatisfied: false'
    json_payload='records: [{"id":"FR-LOC-001","title":"Monitor device location","description":"The system SHALL monitor the device location while tracking is enabled.","priority":"high","status":"pending","area":"LOC","acceptanceCriteria":[{"id":"FR-LOC-001-AC001","text":"Demonstrates behavior for FR-LOC-001.","isSatisfied":false}]}]'

    assert_batch_payload_preserved "$indented_payload"

    _repl_schema_has_records "$json_payload"
    typed="$(_repl_requirements_typed_params updateFrBatch "$json_payload")"
    grep -Fq 'records: [{' <<<"$typed"
    grep -Fq '"FR-LOC-001"' <<<"$typed"
    grep -Fq '"acceptanceCriteria"' <<<"$typed"
    grep -Fq '"isSatisfied":false' <<<"$typed"
}
