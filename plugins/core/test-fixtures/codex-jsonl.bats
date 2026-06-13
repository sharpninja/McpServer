#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.
# codex-jsonl.bats — Tests for lib/codex-jsonl.js and lib/codex-jsonl-enrich.js.
#
# Covers:
#   - Parsing parent JSONL: rich field extraction (interpretation, actions, etc.)
#   - Subagent discovery via parent_thread_id
#   - Import mode: tab-delimited output for repl dispatch
#   - Idempotency: duplicate subagent entries not created
#   - Secret redaction: API keys and auth tokens scrubbed from output

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
CODEX_JSONL="$PLUGIN_ROOT/lib/codex-jsonl.js"
CODEX_ENRICH="$PLUGIN_ROOT/lib/codex-jsonl-enrich.js"
FIXTURES="$PLUGIN_ROOT/tests/fixtures"

setup() {
    command -v node >/dev/null 2>&1 || skip "node not available"
    SANDBOX="$(mktemp -d)"
}

teardown() {
    rm -rf "$SANDBOX"
}

# ---------------------------------------------------------------------------
# parse mode
# ---------------------------------------------------------------------------

@test "parse: extracts turns from parent rollout fixture" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    # Should return a JSON array
    echo "$output" | node -e "const d=require('fs').readFileSync(0,'utf8'); const a=JSON.parse(d); if(!Array.isArray(a)) throw new Error('not array'); process.exit(0);"
}

@test "parse: parent fixture yields 2 turns" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    count="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(String(a.length));")"
    [ "$count" -eq 2 ]
}

@test "parse: first turn has non-empty interpretation" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    interp="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(a[0].interpretation||'');")"
    [ -n "$interp" ]
}

@test "parse: first turn has non-empty queryText" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    qt="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(a[0].queryText||'');")"
    [ -n "$qt" ]
}

@test "parse: first turn has non-empty actions array" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    len="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(String(a[0].actions.length));")"
    [ "$len" -gt 0 ]
}

@test "parse: first turn captures file edit from patch_apply_end" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    fm="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].filesModified));")"
    echo "$fm" | grep -q "SessionLogService.cs"
}

@test "parse: first turn has non-empty processingDialog" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    len="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(String(a[0].processingDialog.length));")"
    [ "$len" -gt 0 ]
}

@test "parse: extracts requirement ID FR-MCP-007 from agent message" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    reqs="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].requirementsDiscovered||[]));")"
    echo "$reqs" | grep -q "FR-MCP-007"
}

@test "parse: extracts design decision from decision-phrased agent message" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    dd="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].designDecisions||[]));")"
    echo "$dd" | grep -qi "OrdinalIgnoreCase\|case.*insensitive\|rationale\|decision"
}

@test "parse: empty blockers array when no turn_aborted events" {
    run node "$CODEX_JSONL" parse "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    blockers="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].blockers||[]));")"
    [ "$blockers" = "[]" ]
}

@test "parse: subagent fixture yields 1 turn with subagent tag" {
    run node "$CODEX_JSONL" parse "$FIXTURES/subagent-rollout-1.jsonl"
    [ "$status" -eq 0 ]
    tags="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].tags||[]));")"
    echo "$tags" | grep -q "subagent"
}

@test "parse: subagent-2 fixture captures filesModified from patch_apply_end" {
    run node "$CODEX_JSONL" parse "$FIXTURES/subagent-rollout-2.jsonl"
    [ "$status" -eq 0 ]
    fm="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(JSON.stringify(a[0].filesModified||[]));")"
    echo "$fm" | grep -q "SessionLogServiceTests.cs"
}

@test "parse: exits non-zero for missing file" {
    run node "$CODEX_JSONL" parse "$SANDBOX/nonexistent.jsonl"
    [ "$status" -ne 0 ]
}

# ---------------------------------------------------------------------------
# subagents mode
# ---------------------------------------------------------------------------

@test "subagents: discovers subagent transcripts by parent_thread_id" {
    # Copy fixtures into a temp sessions dir; set CODEX_SESSION_DIR (works on Windows where HOME is ignored by os.homedir())
    FAKE_SESSIONS="$SANDBOX/sessions/2026/05/25"
    mkdir -p "$FAKE_SESSIONS"
    cp "$FIXTURES/subagent-rollout-1.jsonl" "$FAKE_SESSIONS/rollout-subagent-1.jsonl"
    cp "$FIXTURES/subagent-rollout-2.jsonl" "$FAKE_SESSIONS/rollout-subagent-2.jsonl"

    CODEX_SESSION_DIR="$SANDBOX/sessions" run node "$CODEX_JSONL" subagents "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    # Should return array of 2 subagent descriptors
    count="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(String(a.length));")"
    [ "$count" -eq 2 ]
}

@test "subagents: each descriptor has path, agentNickname, parentThreadId" {
    FAKE_SESSIONS="$SANDBOX/sessions/2026/05/25"
    mkdir -p "$FAKE_SESSIONS"
    cp "$FIXTURES/subagent-rollout-1.jsonl" "$FAKE_SESSIONS/rollout-subagent-1.jsonl"

    CODEX_SESSION_DIR="$SANDBOX/sessions" run node "$CODEX_JSONL" subagents "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    # Descriptor must have agentNickname
    nick="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(a[0].agentNickname||'');")"
    [ -n "$nick" ]
    # parentThreadId must match parent session
    pid="$(echo "$output" | node -e "const a=JSON.parse(require('fs').readFileSync(0,'utf8')); process.stdout.write(a[0].parentThreadId||'');")"
    [ "$pid" = "test-parent-uuid-0001" ]
}

@test "subagents: returns empty array when no subagents exist" {
    CODEX_SESSION_DIR="$SANDBOX/sessions" run node "$CODEX_JSONL" subagents "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -eq 0 ]
    [ "$output" = "[]" ]
}

# ---------------------------------------------------------------------------
# import mode
# ---------------------------------------------------------------------------

@test "import: emits tab-delimited lines for parent transcript" {
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl" "ClaudeCode-20260525T100000Z-test"
    [ "$status" -eq 0 ]
    # Should have one line per turn (2 turns in parent fixture)
    line_count="$(echo "$output" | grep -c "workflow.sessionlog.importRecovery" || true)"
    [ "$line_count" -ge 1 ]
}

@test "import: each line has method, base64 params, and label fields" {
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl" "ClaudeCode-20260525T100000Z-test"
    [ "$status" -eq 0 ]
    # First line should have 3 tab-separated fields
    first_line="$(echo "$output" | head -1)"
    field_count="$(echo "$first_line" | awk -F'\t' '{print NF}')"
    [ "$field_count" -eq 3 ]
}

@test "import: base64 params decode to valid JSON with non-empty interpretation" {
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl" "ClaudeCode-20260525T100000Z-test"
    [ "$status" -eq 0 ]
    first_b64="$(echo "$output" | head -1 | cut -f2)"
    decoded="$(printf '%s' "$first_b64" | base64 --decode 2>/dev/null)"
    echo "$decoded" | node -e "
const d = require('fs').readFileSync(0,'utf8');
const obj = JSON.parse(d);
const turn = obj.sessionLog.turns[0];
if (!turn.interpretation || turn.interpretation.trim() === '') {
  process.stderr.write('interpretation is empty\n'); process.exit(1);
}
if (!turn.processingDialog || turn.processingDialog.length === 0) {
  process.stderr.write('processingDialog is empty\n'); process.exit(1);
}
"
}

@test "import: subagent transcript includes subagent tag and parent link" {
    run node "$CODEX_JSONL" import "$FIXTURES/subagent-rollout-1.jsonl" "ClaudeCode-20260525T100000Z-test" "req-20260525T100001Z-parent"
    [ "$status" -eq 0 ]
    first_b64="$(echo "$output" | head -1 | cut -f2)"
    decoded="$(printf '%s' "$first_b64" | base64 --decode 2>/dev/null)"
    echo "$decoded" | node -e "
const d = require('fs').readFileSync(0,'utf8');
const obj = JSON.parse(d);
const turn = obj.sessionLog.turns[0];
const tags = turn.tags || [];
if (!tags.includes('subagent')) { process.stderr.write('missing subagent tag\n'); process.exit(1); }
"
}

@test "import: subagent request IDs include nickname slug" {
    run node "$CODEX_JSONL" import "$FIXTURES/subagent-rollout-1.jsonl" "ClaudeCode-20260525T100000Z-test"
    [ "$status" -eq 0 ]
    first_b64="$(echo "$output" | head -1 | cut -f2)"
    decoded="$(printf '%s' "$first_b64" | base64 --decode 2>/dev/null)"
    reqId="$(echo "$decoded" | node -e "const d=require('fs').readFileSync(0,'utf8'); const obj=JSON.parse(d); process.stdout.write(obj.sessionLog.turns[0].requestId||'');")"
    echo "$reqId" | grep -qi "subagent\|dalton"
}

@test "import: requires session-id argument" {
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl"
    [ "$status" -ne 0 ]
}

# ---------------------------------------------------------------------------
# Idempotency
# ---------------------------------------------------------------------------

@test "import: running twice produces same request IDs (idempotent)" {
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl" "ClaudeCode-20260525T100000Z-test"
    out1="$output"
    run node "$CODEX_JSONL" import "$FIXTURES/parent-rollout.jsonl" "ClaudeCode-20260525T100000Z-test"
    out2="$output"
    [ "$out1" = "$out2" ]
}

# ---------------------------------------------------------------------------
# Secret redaction
# ---------------------------------------------------------------------------

@test "redaction: API key values are scrubbed from parsed output" {
    # Create a fixture with an embedded API key
    cat > "$SANDBOX/secret-rollout.jsonl" <<'JSONL'
{"timestamp":"2026-05-25T10:00:00.000Z","type":"session_meta","payload":{"id":"test-secret-uuid","timestamp":"2026-05-25T10:00:00.000Z","cwd":"/tmp","thread_source":"user"}}
{"timestamp":"2026-05-25T10:00:01.000Z","type":"event_msg","payload":{"type":"task_started","turn_id":"turn-secret-001","started_at":1779784800}}
{"timestamp":"2026-05-25T10:00:02.000Z","type":"event_msg","payload":{"type":"user_message","message":"Check the API key"}}
{"timestamp":"2026-05-25T10:00:03.000Z","type":"response_item","payload":{"type":"function_call","name":"shell_command","arguments":"{\"command\":\"curl -H 'X-Api-Key: abc123supersecretkey9876543210' http://localhost:7147/health\"}","call_id":"call_secret"}}
{"timestamp":"2026-05-25T10:00:04.000Z","type":"event_msg","payload":{"type":"agent_message","message":"Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ0ZXN0In0.fake will be redacted here","phase":"completion"}}
{"timestamp":"2026-05-25T10:00:05.000Z","type":"event_msg","payload":{"type":"task_complete","turn_id":"turn-secret-001","last_agent_message":"Done"}}
JSONL

    run node "$CODEX_JSONL" parse "$SANDBOX/secret-rollout.jsonl"
    [ "$status" -eq 0 ]
    # API key must not appear in output
    echo "$output" | grep -qv "abc123supersecretkey9876543210"
    # Bearer token must not appear
    echo "$output" | grep -qv "eyJhbGciOiJIUzI1NiJ9"
    # Redaction placeholders must appear
    echo "$output" | grep -q "REDACTED"
}

@test "redaction: X-Api-Key header value is scrubbed" {
    cat > "$SANDBOX/xapikey-rollout.jsonl" <<'JSONL'
{"timestamp":"2026-05-25T10:00:00.000Z","type":"session_meta","payload":{"id":"test-xapi-uuid","timestamp":"2026-05-25T10:00:00.000Z","cwd":"/tmp","thread_source":"user"}}
{"timestamp":"2026-05-25T10:00:01.000Z","type":"event_msg","payload":{"type":"task_started","turn_id":"turn-xapi-001","started_at":1779784800}}
{"timestamp":"2026-05-25T10:00:02.000Z","type":"event_msg","payload":{"type":"agent_message","message":"Calling API with X-Api-Key: mysupersecrettoken12345678901234","phase":"completion"}}
{"timestamp":"2026-05-25T10:00:03.000Z","type":"event_msg","payload":{"type":"task_complete","turn_id":"turn-xapi-001","last_agent_message":"Done"}}
JSONL

    run node "$CODEX_JSONL" parse "$SANDBOX/xapikey-rollout.jsonl"
    [ "$status" -eq 0 ]
    echo "$output" | grep -qv "mysupersecrettoken12345678901234"
    echo "$output" | grep -q "REDACTED"
}

# ---------------------------------------------------------------------------
# codex-jsonl-enrich.js
# ---------------------------------------------------------------------------

@test "enrich: emits valid YAML with response field" {
    run node "$CODEX_ENRICH" "$FIXTURES/parent-rollout.jsonl" "Test response text"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "^response:"
}

@test "enrich: emits non-empty interpretation" {
    run node "$CODEX_ENRICH" "$FIXTURES/parent-rollout.jsonl" "Test response"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "^interpretation:"
    # interpretation value must not be empty
    interp_val="$(echo "$output" | awk '/^interpretation:/{found=1; next} found && /^[^ ]/{exit} found{print}' | head -1)"
    [ -n "${interp_val}$(echo "$output" | grep '^interpretation:' | sed 's/^interpretation:[[:space:]]*//')" ]
}

@test "enrich: emits actions array" {
    run node "$CODEX_ENRICH" "$FIXTURES/parent-rollout.jsonl" "Test response"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "^actions:"
}

@test "enrich: emits processingDialog array" {
    run node "$CODEX_ENRICH" "$FIXTURES/parent-rollout.jsonl" "Test response"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "^processingDialog:"
}

@test "enrich: exits non-zero for missing JSONL file" {
    run node "$CODEX_ENRICH" "$SANDBOX/missing.jsonl" "Response"
    [ "$status" -ne 0 ]
}
