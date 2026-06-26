#!/usr/bin/env bats
# FR-MCP-REPL-005 contract fixtures: one repl process serves N requests in both
# NDJSON and ---separated YAML framing, and every response is terminated by a
# blank line AND a '---' separator line.
#
# Live tests run only when mcpserver-repl is on PATH and a workspace marker is
# reachable; otherwise they are skipped (static fixture validation always runs).

CORE_ROOT="$(cd "$(dirname "$BATS_TEST_FILENAME")/.." && pwd)"
FIXTURES="$CORE_ROOT/test-fixtures/repl-envelopes"
REPL_BIN="${MCPSERVER_REPL_BIN:-mcpserver-repl}"

require_live_repl() {
    command -v "$REPL_BIN" >/dev/null 2>&1 || [ -x "$REPL_BIN" ] || skip "repl binary not found: $REPL_BIN"
    [ -n "${MCPSERVER_REPL_CONTRACT_LIVE:-}" ] || skip "set MCPSERVER_REPL_CONTRACT_LIVE=1 to run live contract tests"
}

@test "fixture files are well-formed (2 NDJSON lines, 2 YAML documents)" {
    [ "$(grep -c '"type":"request"' "$FIXTURES/requests.ndjson")" -eq 2 ]
    [ "$(grep -c '^type: request$' "$FIXTURES/requests.yaml")" -eq 2 ]
    grep -q '^---$' "$FIXTURES/requests.yaml"
}

@test "live: one repl process answers both NDJSON requests with --- terminators" {
    require_live_repl
    run bash -c "'$REPL_BIN' --agent-stdio < '$FIXTURES/requests.ndjson'"
    [ "$status" -eq 0 ]
    [ "$(grep -c 'req-20260612T000001Z-fixture-json-1' <<<"$output")" -ge 1 ]
    [ "$(grep -c 'req-20260612T000002Z-fixture-json-2' <<<"$output")" -ge 1 ]
    [ "$(grep -c '^---$' <<<"$output")" -ge 2 ]
    [[ "$output" != *invalid_envelope* ]]
}

@test "live: one repl process answers both YAML documents with --- terminators" {
    require_live_repl
    run bash -c "'$REPL_BIN' --agent-stdio < '$FIXTURES/requests.yaml'"
    [ "$status" -eq 0 ]
    [ "$(grep -c 'req-20260612T000003Z-fixture-yaml-1' <<<"$output")" -ge 1 ]
    [ "$(grep -c 'req-20260612T000004Z-fixture-yaml-2' <<<"$output")" -ge 1 ]
    [ "$(grep -c '^---$' <<<"$output")" -ge 2 ]
}
