#!/usr/bin/env bats
# Ported into plugins/core/test-fixtures (Phase 2 shared core): the suite
# runs against a staged plugin root built from lib-sh/lib-ps/hooks-templates.
# Regression suite for workflow shims added to lib/repl-invoke.sh.
#
# Original bug: every workflow.sessionlog.* call returned method_not_found
# from mcpserver-repl. mcpserver-repl exits 0 even on type:error, so callers
# saw "success" — but cache/current-turn.yaml never flipped to completed and
# the Stop hook blocked every turn.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"
PLUGIN_ROOT="$(core_stage)"
LIB="$PLUGIN_ROOT/lib/repl-invoke.sh"
source "$PLUGIN_ROOT/tests/cache-scope-helper.bash"

setup() {
    SANDBOX="$(mktemp -d)"
    mkdir -p "$SANDBOX/bin" "$SANDBOX/workspace"
    export REPL_TIMEOUT=2
    export REPL_TODO_REPL_TIMEOUT=2
    export STUB_LOG="$SANDBOX/repl-calls.log"
    export STUB_DB="$SANDBOX/requirements-fr.db"

# Stub mcpserver-repl: emulates the real dispatcher — success for valid
# client.* methods, type:error for the workflow.* fictions so any future
# shim-removal regresses through THIS test.
    cat > "$SANDBOX/bin/mcpserver-repl" <<'STUB'
#!/usr/bin/env bash
read_stdin_if_ready() {
    if [ -t 0 ]; then
        return 0
    fi

    timeout 0.5s cat 2>/dev/null || true
}
input="$(read_stdin_if_ready)"
json_value() {
    local key="$1"
    printf '%s\n' "$input" | node -e '
const fs = require("fs");
const key = process.argv[1];
try {
  const env = JSON.parse(fs.readFileSync(0, "utf8"));
  if (key === "__method") {
    const method = env?.payload?.method ?? env?.method;
    if (method !== undefined && method !== null) process.stdout.write(String(method));
    process.exit(0);
  }
  const params = env?.payload?.params ?? env?.params ?? {};
  const value = params?.[key];
  if (value === undefined || value === null) process.exit(0);
  process.stdout.write(Array.isArray(value) ? value.join(",") : String(value));
} catch {
  process.exit(0);
}
' "$key"
}
method="$(json_value __method)"
if [ -z "$method" ]; then
    method="$(printf '%s\n' "$input" | grep '^[[:space:]]*method:' | head -1 | sed 's/^[[:space:]]*method:[[:space:]]*//')"
fi
{
    printf 'method=%s\n' "$method"
    printf 'cwd=%s\n' "$(pwd)"
    printf 'MCP_WORKSPACE_PATH=%s\n' "${MCP_WORKSPACE_PATH:-}"
    printf 'MCP_SERVER_URL=%s\n' "${MCP_SERVER_URL:-}"
    printf '%s\n' "$input" | sed 's/^/input: /'
    printf '%s\n' '---'
} >> "${STUB_LOG:-/dev/null}"

extract_yaml_value() {
    local json
    json="$(json_value "$1")"
    if [ -n "$json" ]; then
        printf '%s' "$json"
        return 0
    fi
    printf '%s\n' "$input" | grep "^[[:space:]]*$1:" | tail -1 | sed "s/^[[:space:]]*$1:[[:space:]]*//"
}

if [[ "$method" == workflow.requirements.* ]] && [ "${STUB_WORKFLOW_REQUIREMENTS_AUTH_ERROR:-}" = "1" ]; then
    printf 'type: error\npayload:\n  code: method_invocation_error\n  message: Authentication required: no credential is configured on this client.\n'
    exit 0
fi

if [ "$method" = "workflow.requirements.generateDocument" ] && [ "${STUB_WORKFLOW_REQUIREMENTS_REJECT_WIKI:-}" = "1" ]; then
    format="$(extract_yaml_value format)"
    if [ "$format" = "wiki" ]; then
        printf 'type: error\npayload:\n  code: invalid_argument\n  message: "Invalid format: wiki. Valid values: markdown, yaml"\n'
        exit 0
    fi
fi

if [ "$method" = "workflow.requirements.createFr" ] && [ "${STUB_WORKFLOW_REQUIREMENTS_CREATE_IGNORES_PRIORITY:-}" = "1" ]; then
    id="$(extract_yaml_value id)"
    title="$(extract_yaml_value title)"
    body="$(extract_yaml_value description)"
    printf 'type: result\npayload:\n  result:\n    item:\n      id: %s\n      title: %s\n      description: %s\n      priority: medium\n' "$id" "$title" "$body"
    exit 0
fi

if [ "${STUB_WORKFLOW_REQUIREMENTS_GET_SUCCEEDS:-}" = "1" ]; then
    case "$method" in
        workflow.requirements.getFr)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Workflow FR\n    description: Workflow FR body\n    priority: medium\n    status: pending\n    notes: Workflow notes\n' "$id"
            exit 0
            ;;
        workflow.requirements.getTr)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Workflow TR\n    description: Workflow TR body\n    priority: medium\n    status: pending\n    notes: Workflow TR notes\n' "$id"
            exit 0
            ;;
        workflow.requirements.getTest)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Workflow TEST\n    condition: Workflow TEST condition\n    priority: medium\n    status: pending\n    notes: Workflow TEST notes\n' "$id"
            exit 0
            ;;
    esac
fi

if [ "${STUB_WORKFLOW_REQUIREMENTS_GET_WITH_AC:-}" = "1" ]; then
    case "$method" in
        workflow.requirements.getFr|client.Requirements.GetFrAsync)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Stored AC FR\n    description: Stored body\n    priority: medium\n    status: pending\n    notes: Stored notes\n    acceptanceCriteria:\n      - id: existing-ac-1\n        text: existing criterion text\n        isSatisfied: false\n' "$id"
            exit 0
            ;;
        workflow.requirements.getTr|client.Requirements.GetTrAsync)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Stored AC TR\n    description: Stored TR body\n    priority: medium\n    status: pending\n    notes: Stored TR notes\n    acceptanceCriteria:\n      - id: existing-ac-1\n        text: existing TR criterion\n        isSatisfied: false\n' "$id"
            exit 0
            ;;
        workflow.requirements.getTest|client.Requirements.GetTestAsync)
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    id: %s\n    title: Stored AC TEST\n    condition: Stored condition\n    priority: medium\n    status: pending\n    notes: Stored TEST notes\n    acceptanceCriteria:\n      - id: existing-ac-1\n        text: existing TEST criterion\n        isSatisfied: false\n' "$id"
            exit 0
            ;;
    esac
fi

if [[ "$method" == "client.SessionLog.QueryAsync" || "$method" == client.Todo.* ]] && [ "${STUB_REQUIRE_COMPAT_AUTH:-}" = "1" ] && [ "${MCP_WORKSPACE_PATH:-}" = "${STUB_AUTH_FAIL_WORKSPACE_PATH:-}" ]; then
    printf 'type: error\npayload:\n  code: method_invocation_error\n  message: Authentication required: no credential is configured on this client.\n'
    exit 0
fi

if [ "$method" = "client.SessionLog.QueryAsync" ] && [ "${STUB_SESSIONLOG_QUERY_ERROR:-}" = "1" ]; then
    printf 'type: error\npayload:\n  code: method_invocation_error\n  message: Session log query rejected by test stub.\n'
    exit 0
fi

if [[ "$method" == "client.Todo.GetAsync" || "$method" == "client.Todo.GetByIdAsync" ]] && [ "${STUB_TODO_GET_MISSING_ID:-}" = "1" ]; then
    printf 'type: error\npayload:\n  code: invalid_request\n  message: Missing required parameter: id (type: String)\n'
    exit 0
fi

if [ "$method" = "client.Todo.UpdateAsync" ] && [ "${STUB_TODO_UPDATE_REJECT:-}" = "1" ]; then
    printf 'type: error\npayload:\n  code: invalid_request\n  message: Missing required parameter: request (type: TodoUpdateRequest)\n'
    exit 0
fi

case "$method" in
    client.SessionLog.SubmitAsync|client.SessionLog.UpsertTurnAsync|client.SessionLog.QueryAsync|client.SessionLog.AppendDialogAsync|client.Todo.QueryAsync|client.Todo.UpdateAsync|client.Todo.GetAsync|client.Todo.GetByIdAsync|client.Todo.CreateAsync|client.Todo.DeleteAsync|client.Todo.AnalyzeRequirementsAsync)
        printf 'type: response\npayload:\n  ok: true\n'
        ;;
    workflow.sessionlog.importRecovery)
        printf 'type: result\npayload:\n  result:\n    importedTurns: 1\n    totalTurns: 1\n'
        ;;
    workflow.memory.list)
        printf 'type: result\npayload:\n  result:\n    items:\n      - id: MEMORY-REQ-001\n        text: Keep exact wording.\n    totalCount: 1\n'
        ;;
    workflow.memory.add|workflow.memory.update|workflow.memory.remove)
        id="$(extract_yaml_value id)"
        [ -z "$id" ] && id="MEMORY-REQ-001"
        printf 'type: result\npayload:\n  result:\n    success: true\n    id: %s\n' "$id"
        ;;
    workflow.graphrag.*)
        printf 'type: result\npayload:\n  result:\n    ok: true\n    method: %s\n' "$method"
        ;;
    workflow.sessionlog.*|workflow.requirements.*)
        printf 'type: error\npayload:\n  code: method_not_found\n  message: not routed\n'
        ;;
    client.Requirements.CreateFrAsync)
        id="$(extract_yaml_value id)"
        title="$(extract_yaml_value title)"
        body="$(extract_yaml_value body)"
        priority="$(extract_yaml_value priority)"
        printf '%s|%s|%s|%s\n' "$id" "$title" "$body" "$priority" >> "$STUB_DB"
        if [ "${STUB_TYPED_CREATE_EMPTY:-}" = "1" ]; then
            printf 'type: result\npayload:\n  result:\n'
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    item:\n      id: %s\n      title: %s\n      description: %s\n' "$id" "$title" "$body"
        [ -n "$priority" ] && printf '      priority: %s\n' "$priority"
        ;;
    client.Requirements.ListFrAsync)
        if [ "${STUB_REQUIRE_COMPAT_AUTH:-}" = "1" ] && [ "${MCP_WORKSPACE_PATH:-}" = "${STUB_AUTH_FAIL_WORKSPACE_PATH:-}" ]; then
            printf 'type: error\npayload:\n  code: method_invocation_error\n  message: Authentication required: no credential is configured on this client.\n'
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    items:\n'
        if [ -f "$STUB_DB" ]; then
            while IFS='|' read -r id title body priority; do
                [ -z "$id" ] && continue
                printf '      - id: %s\n        title: %s\n        description: %s\n' "$id" "$title" "$body"
                [ -n "$priority" ] && printf '        priority: %s\n' "$priority"
            done < "$STUB_DB"
        fi
        printf '    totalCount: %s\n' "$(wc -l < "$STUB_DB" 2>/dev/null || printf 0)"
        ;;
    client.Requirements.GetFrAsync)
        id="$(extract_yaml_value id)"
        if [ "${STUB_REQUIREMENTS_GET_NOT_FOUND:-}" = "1" ]; then
            printf 'type: error\npayload:\n  code: method_invocation_error\n  message: %s not found\n' "$id"
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    item:\n      id: %s\n      title: Stored FR\n      description: Stored body\n      priority: medium\n      status: pending\n      area: MCP\n      notes: Stored notes\n' "$id"
        ;;
    client.Requirements.GetTrAsync)
        id="$(extract_yaml_value id)"
        if [ "${STUB_REQUIREMENTS_GET_NOT_FOUND:-}" = "1" ]; then
            printf 'type: error\npayload:\n  code: method_invocation_error\n  message: %s not found\n' "$id"
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    item:\n      id: %s\n      title: Stored TR\n      description: Stored TR body\n      priority: medium\n      status: pending\n      area: MCP\n      subarea: Plugin\n      notes: Stored TR notes\n' "$id"
        ;;
    client.Requirements.GetTestAsync)
        id="$(extract_yaml_value id)"
        if [ "${STUB_REQUIREMENTS_GET_NOT_FOUND:-}" = "1" ]; then
            printf 'type: error\npayload:\n  code: method_invocation_error\n  message: %s not found\n' "$id"
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    item:\n      id: %s\n      title: Stored TEST\n      condition: Stored TEST condition\n      priority: medium\n      status: pending\n      area: MCP\n      testType: integration\n      notes: Stored TEST notes\n' "$id"
        ;;
    client.Requirements.CreateFrBatchAsync|client.Requirements.CreateTrBatchAsync|client.Requirements.CreateTestBatchAsync|client.Requirements.CreateBatchAsync|client.Requirements.UpdateFrBatchAsync|client.Requirements.UpdateTrBatchAsync|client.Requirements.UpdateTestBatchAsync|client.Requirements.UpdateBatchAsync)
        printf 'type: result\npayload:\n  result:\n    success: true\n    total: 1\n'
        ;;
    client.Requirements.GenerateAsync)
        doc="$(extract_yaml_value doc)"
        format="$(extract_yaml_value format)"
        if [ "${STUB_TYPED_GENERATE_EMPTY:-}" = "1" ]; then
            printf 'type: result\npayload:\n  result:\n'
            exit 0
        fi
        if [ "$format" = "wiki" ]; then
            printf 'type: result\npayload:\n  result:\n    success: true\n    format: wiki\n    docType: all\n    generatedAtUtc: 2026-05-08T12:00:00Z\n    outputRoot: /workspace/docs/Project/wiki\n    files:\n      - relativePath: azure/Home.md\n        fullPath: /workspace/docs/Project/wiki/azure/Home.md\n        contentType: text/markdown\n        lastModifiedUtc: 2026-05-08T12:00:00Z\n'
        else
            printf 'type: result\npayload:\n  result:\n    content: |\n      # Requirement Traceability Matrix\n      doc=%s\n    format: markdown\n' "$doc"
        fi
        ;;
    client.Requirements.CreateTrAsync|client.Requirements.CreateTestAsync)
        if [ "${STUB_TYPED_CREATE_EMPTY:-}" = "1" ]; then
            printf 'type: result\npayload:\n  result:\n'
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    ok: true\n'
        ;;
    client.Requirements.UpdateFrAsync)
        if [ "${STUB_REQUIREMENTS_EMPTY_AC_RESPONSE:-}" = "1" ]; then
            id="$(extract_yaml_value id)"
            printf 'type: result\npayload:\n  result:\n    success: true\n    item:\n      id: %s\n      acceptanceCriteria: []\n' "$id"
            exit 0
        fi
        printf 'type: result\npayload:\n  result:\n    ok: true\n'
        ;;
    client.Requirements.DeleteFrAsync|client.Requirements.DeleteTrAsync|client.Requirements.DeleteTestAsync|client.Requirements.ListTrAsync|client.Requirements.ListTestAsync|client.Requirements.ListMappingsAsync|client.Requirements.UpdateTrAsync|client.Requirements.UpdateTestAsync|client.Requirements.UpsertMappingAsync|client.Requirements.DeleteMappingAsync|client.Requirements.IngestAsync)
        printf 'type: result\npayload:\n  result:\n    ok: true\n'
        ;;
    *)
        printf 'type: error\npayload:\n  code: method_invocation_error\n  message: unknown\n'
        ;;
esac
STUB
    chmod +x "$SANDBOX/bin/mcpserver-repl"

    cat > "$SANDBOX/bin/curl" <<'STUB'
#!/usr/bin/env bash
headers_file=""
output_file=""
method="GET"
url=""
body_arg=""
headers=()
query_args=()
while [ "$#" -gt 0 ]; do
    case "$1" in
        -D)
            headers_file="$2"
            shift 2
            ;;
        -o)
            output_file="$2"
            shift 2
            ;;
        -H)
            headers+=("$2")
            shift 2
            ;;
        -X)
            method="$2"
            shift 2
            ;;
        --data-binary|--data|--data-raw|-d)
            body_arg="$2"
            [ "$method" = "GET" ] && method="POST"
            shift 2
            ;;
        --data-urlencode)
            query_args+=("$2")
            shift 2
            ;;
        --max-time)
            shift 2
            ;;
        --get)
            method="GET"
            shift
            ;;
        -fsSL|-f|-s|-S|-L)
            shift
            ;;
        -*)
            shift
            ;;
        *)
            url="$1"
            shift
            ;;
    esac
done
body=""
if [ -n "$body_arg" ]; then
    case "$body_arg" in
        @*) body="$(cat "${body_arg#@}")" ;;
        *) body="$body_arg" ;;
    esac
fi
{
    printf 'curl_method=%s\n' "$method"
    printf 'curl_url=%s\n' "$url"
    for header in "${headers[@]}"; do
        printf 'curl_header=%s\n' "$header"
    done
    for query in "${query_args[@]}"; do
        printf 'curl_query=%s\n' "$query"
    done
    [ -n "$body" ] && printf 'curl_body=%s\n' "$body"
    printf '%s\n' '---'
} >> "${STUB_LOG:-/dev/null}"

if [ "${STUB_TODO_HTTP_400:-}" = "1" ] && [[ "$url" == */mcpserver/todo* ]]; then
    [ -n "$headers_file" ] && printf 'HTTP/1.1 400 Bad Request\r\nContent-Type: application/json\r\n\r\n' > "$headers_file"
    [ -n "$output_file" ] && printf '{"error":"section Architecture is not valid"}' > "$output_file"
    exit 0
fi

content_type="application/zip"
payload=""
case "$url" in
    */mcpserver/sessionlog)
        content_type="application/json"
        payload='{"success":true,"sessionLogId":123}'
        ;;
    */mcpserver/todo)
        content_type="application/json"
        if [ "$method" = "POST" ]; then
            payload='{"id":"TODO-NEW","created":true}'
        else
            payload='[{"id":"RENDER-MAP3D-001","done":false}]'
        fi
        ;;
    */mcpserver/todo/*/requirements)
        content_type="application/json"
        payload='{"id":"RENDER-MAP3D-001","requirements":[]}'
        ;;
    */mcpserver/requirements/*/*/acceptance-criteria/copy-from-todo)
        content_type="application/json"
        payload='{"copied":true}'
        ;;
    */mcpserver/todo/*)
        content_type="application/json"
        case "$method" in
            DELETE) payload='{"deleted":true}' ;;
            PUT) payload='{"id":"RENDER-MAP3D-001","updated":true}' ;;
            *) payload='{"id":"RENDER-MAP3D-001","title":"Map 3D"}' ;;
        esac
        ;;
esac
[ -n "$headers_file" ] && printf 'HTTP/1.1 200 OK\r\nContent-Type: %s\r\n\r\n' "$content_type" > "$headers_file"
if [ -n "$output_file" ]; then
    if [ "$content_type" = "application/zip" ]; then
        printf 'PK\003\004' > "$output_file"
    else
        printf '%s' "$payload" > "$output_file"
    fi
fi
exit 0
STUB
    chmod +x "$SANDBOX/bin/curl"

    cat > "$SANDBOX/bin/pwsh.exe" <<'STUB'
#!/usr/bin/env bash
printf '0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF\n'
exit 0
STUB
    chmod +x "$SANDBOX/bin/pwsh.exe"

    cat > "$SANDBOX/bin/powershell.exe" <<'STUB'
#!/usr/bin/env bash
printf '0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF\n'
exit 0
STUB
    chmod +x "$SANDBOX/bin/powershell.exe"

    export PATH="$SANDBOX/bin:$PATH"
    export PLUGIN_ROOT_OVERRIDE="$SANDBOX"
    init_test_cache "$SANDBOX/workspace" "ClaudeCode-20260419T000000Z-test"

    cat > "$TEST_CACHE_DIR/session-state.yaml" <<EOF
status: verified
sessionId: ClaudeCode-20260419T000000Z-test
sourceType: ClaudeCode
title: Shim test session
model: codex
started: 2026-04-19T00:00:00Z
lastUpdated: 2026-04-19T00:00:00Z
workspacePath: "/tmp/ws"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "2026-04-19T00:00:00Z"
EOF
}

teardown() {
    rm -rf "$SANDBOX"
}

write_turn() {
    local status="${1:-in_progress}" edits="${2:-0}" build="${3:-unknown}"
    refresh_test_cache
    cat > "$TEST_CACHE_DIR/current-turn.yaml" <<EOF
turnRequestId: req-test-shim-001
queryTitle: Shim test
openedAt: 2026-04-19T00:00:00Z
status: ${status}
codeEdits: ${edits}
lastBuildStatus: ${build}
EOF
}

read_status() {
    grep '^status:' "$(test_cache_file current-turn.yaml)" | head -1 | sed 's/^status:[[:space:]]*//'
}

read_edits() {
    grep '^codeEdits:' "$(test_cache_file current-turn.yaml)" | head -1 | sed 's/^codeEdits:[[:space:]]*//'
}

# PLAN-SESSIONLOGENFORCEMENT-001 helpers: a turn cache carrying the audit-counter schema
# plus a reader for any audit field.
write_audit_turn() {
    local status="${1:-in_progress}" edits="${2:-0}"
    refresh_test_cache
    cat > "$TEST_CACHE_DIR/current-turn.yaml" <<EOF
turnRequestId: req-test-audit-001
queryTitle: Audit test
openedAt: 2026-04-19T00:00:00Z
status: ${status}
codeEdits: ${edits}
lastBuildStatus: unknown
auditActions: 0
auditDialog: 0
auditDecisions: 0
auditFiles: 0
auditCommits: 0
EOF
}

read_audit() {
    grep "^$1:" "$(test_cache_file current-turn.yaml)" | head -1 | sed "s/^$1:[[:space:]]*//"
}

@test "internal TODO tracking defaults off and can be toggled in cache" {
    source "$LIB"

    run repl_invoke "workflow.todo.internalTracking" ""
    [ "$status" -eq 0 ]
    grep -q 'enabled: false' <<<"$output"
    grep -q 'source: default' <<<"$output"

    run repl_invoke "workflow.todo.internal.enable" ""
    [ "$status" -eq 0 ]
    grep -q 'enabled: true' <<<"$output"
    grep -q 'source: cache' <<<"$output"
    grep -q '^enabled: true' "$(test_cache_file internal-todo.yaml)"

    run repl_invoke "workflow.todo.internal.status" ""
    [ "$status" -eq 0 ]
    grep -q 'enabled: true' <<<"$output"
    grep -q 'source: cache' <<<"$output"

    run repl_invoke "workflow.todo.internal.disable" ""
    [ "$status" -eq 0 ]
    grep -q 'enabled: false' <<<"$output"
    grep -q 'source: cache' <<<"$output"
    grep -q '^enabled: false' "$(test_cache_file internal-todo.yaml)"
}

@test "internal TODO tracking env override wins over cached setting" {
    source "$LIB"

    run repl_invoke "workflow.todo.internal.disable" ""
    [ "$status" -eq 0 ]

    export MCP_CODEX_INTERNAL_TODO=on
    run repl_invoke "workflow.todo.internal.status" ""
    unset MCP_CODEX_INTERNAL_TODO

    [ "$status" -eq 0 ]
    grep -q 'enabled: true' <<<"$output"
    grep -q 'source: environment' <<<"$output"
}

@test "GraphRAG workflow shim routes all 17 commands through workspace REPL" {
    source "$LIB"
    write_requirements_state

    methods=(
        "workflow.graphrag.status"
        "workflow.graphrag.index"
        "workflow.graphrag.query"
        "workflow.graphrag.ingest"
        "workflow.graphrag.documents.list"
        "workflow.graphrag.documents.chunks"
        "workflow.graphrag.documents.delete"
        "workflow.graphrag.entities.create"
        "workflow.graphrag.entities.list"
        "workflow.graphrag.entities.get"
        "workflow.graphrag.entities.update"
        "workflow.graphrag.entities.delete"
        "workflow.graphrag.relationships.create"
        "workflow.graphrag.relationships.list"
        "workflow.graphrag.relationships.get"
        "workflow.graphrag.relationships.update"
        "workflow.graphrag.relationships.delete"
    )

    for method in "${methods[@]}"; do
        params=""
        case "$method" in
            workflow.graphrag.index)
                params="force: true"
                ;;
            workflow.graphrag.query)
                params="query: Dispatcher parity?"
                ;;
            workflow.graphrag.ingest)
                params="content: GraphRAG shell parity."
                ;;
            workflow.graphrag.documents.list)
                params="skip: 0
take: 5
sourceType: repo"
                ;;
            workflow.graphrag.documents.chunks|workflow.graphrag.documents.delete)
                params="documentId: doc-001"
                ;;
            workflow.graphrag.entities.create)
                params="name: Dispatcher
entityType: component"
                ;;
            workflow.graphrag.entities.list)
                params="skip: 0
take: 5
entityType: component"
                ;;
            workflow.graphrag.entities.get|workflow.graphrag.entities.delete)
                params="entityId: entity-001"
                ;;
            workflow.graphrag.entities.update)
                params="entityId: entity-001
name: Dispatcher Updated
entityType: component"
                ;;
            workflow.graphrag.relationships.create)
                params="sourceEntityId: entity-001
targetEntityId: entity-002
relationshipType: routes-to"
                ;;
            workflow.graphrag.relationships.list)
                params="skip: 0
take: 5
entityId: entity-001
type: routes-to"
                ;;
            workflow.graphrag.relationships.get|workflow.graphrag.relationships.delete)
                params="relationshipId: rel-001"
                ;;
            workflow.graphrag.relationships.update)
                params="relationshipId: rel-001
sourceEntityId: entity-001
targetEntityId: entity-003
relationshipType: routes-to"
                ;;
        esac

        run repl_invoke "$method" "$params"
        if [ "$status" -ne 0 ]; then
            printf 'failed method=%s status=%s\n%s\n' "$method" "$status" "$output"
        fi
        [ "$status" -eq 0 ]
        grep -q "method: $method" <<<"$output"
        grep -q "method=$method" "$STUB_LOG"
    done
}

write_requirements_state() {
    refresh_test_cache
    cat > "$TEST_CACHE_DIR/session-state.yaml" <<EOF
status: verified
sessionId: Codex-20260419T000000Z-test
sourceType: Codex
title: Requirements shim test
model: codex
started: 2026-04-19T00:00:00Z
lastUpdated: 2026-04-19T00:00:00Z
workspacePath: "$SANDBOX/workspace"
workspace: "test"
baseUrl: "http://127.0.0.1:8765"
timestamp: "2026-04-19T00:00:00Z"
EOF
}

@test "completeTurn flips current-turn.yaml status from in_progress to completed" {
    write_turn "in_progress"
    source "$LIB"
    run repl_invoke "workflow.sessionlog.completeTurn" "requestId: req-test-shim-001
response: |
  Done."
    [ "$status" -eq 0 ]
    [ "$(read_status)" = "completed" ]
}

@test "completeTurn is idempotent on already-completed turns" {
    write_turn "completed"
    source "$LIB"
    run repl_invoke "workflow.sessionlog.completeTurn" "response: again"
    [ "$status" -eq 0 ]
    [ "$(read_status)" = "completed" ]
}

@test "completeTurn no-ops gracefully when current-turn.yaml is missing" {
    rm -f "$(test_cache_file current-turn.yaml)"
    source "$LIB"
    run repl_invoke "workflow.sessionlog.completeTurn" "response: x"
    [ "$status" -eq 0 ]
}

@test "appendActions bumps codeEdits once per filePath: in params" {
    write_turn "in_progress" 0
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendActions" "actions:
  - description: a
    type: edit
    filePath: src/a.cs
  - description: b
    type: edit
    filePath: src/b.cs"
    [ "$(read_edits)" = "2" ]
}

@test "appendActions with no filePath: leaves codeEdits unchanged" {
    write_turn "in_progress" 0
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendActions" "actions:
  - description: design only
    type: design_decision"
    [ "$(read_edits)" = "0" ]
}

@test "appendActions ignores substring 'filePath' appearing in description text (not a field)" {
    write_turn "in_progress" 0
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendActions" "actions:
  - description: 'talks about filePath in prose but provides no filePath: entry'
    type: design_decision
    status: completed"
    [ "$(read_edits)" = "0" ]
}

@test "appendActions accumulates across multiple invocations" {
    write_turn "in_progress" 1
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendActions" "actions:
  - description: c
    type: edit
    filePath: src/c.cs"
    [ "$(read_edits)" = "2" ]
}

@test "beginTurn initializes audit counters in current-turn.yaml" {
    source "$LIB"
    rm -f "$(test_cache_file current-turn.yaml)"
    run repl_invoke "workflow.sessionlog.beginTurn" "requestId: req-test-audit-init-001
queryTitle: Audit init
queryText: |
  Do the work."
    [ "$status" -eq 0 ]
    [ "$(read_audit auditActions)" = "0" ]
    [ "$(read_audit auditDialog)" = "0" ]
    [ "$(read_audit auditDecisions)" = "0" ]
    [ "$(read_audit auditFiles)" = "0" ]
    [ "$(read_audit auditCommits)" = "0" ]
}

@test "appendActions bumps audit counters for actions, files, decisions, commits" {
    write_audit_turn "in_progress" 0
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendActions" "actions:
  - description: edit a
    type: edit
    filePath: src/a.cs
  - description: decide b
    type: design_decision
  - description: commit c
    type: commit"
    [ "$(read_audit auditActions)" = "3" ]
    [ "$(read_audit auditFiles)" = "1" ]
    [ "$(read_audit auditDecisions)" = "1" ]
    [ "$(read_audit auditCommits)" = "1" ]
}

@test "appendDialog bumps audit dialog and decision counters" {
    write_audit_turn "in_progress" 0
    cat > "$(test_cache_file session-state.yaml)" <<EOF
status: verified
sessionId: Codex-20260419T000000Z-test
sourceType: Codex
workspacePath: "/tmp/ws"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "2026-04-19T00:00:00Z"
EOF
    source "$LIB"
    repl_invoke "workflow.sessionlog.appendDialog" "dialogItems:
  - timestamp: 2026-04-19T00:00:01Z
    role: model
    content: reasoning about the change
    category: reasoning
  - timestamp: 2026-04-19T00:00:02Z
    role: model
    content: chose approach X over Y
    category: decision" >/dev/null 2>&1 || true
    [ "$(read_audit auditDialog)" = "2" ]
    [ "$(read_audit auditDecisions)" -ge 1 ]
}

@test "workflow.sessionlog.audit reports incomplete for code-edit turn missing audit" {
    write_audit_turn "in_progress" 2
    source "$LIB"
    run repl_invoke "workflow.sessionlog.audit" ""
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "complete: false"
    echo "$output" | grep -q "actions"
}

@test "workflow.sessionlog.audit reports complete for a no-code-edit turn" {
    write_audit_turn "in_progress" 0
    source "$LIB"
    run repl_invoke "workflow.sessionlog.audit" ""
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "complete: true"
}

@test "workflow.sessionlog.closeTurn appends actions then completes with audit recorded" {
    write_audit_turn "in_progress" 0
    source "$LIB"
    run repl_invoke "workflow.sessionlog.closeTurn" "response: |
  Implemented and validated.
actions:
  - description: edit x
    type: edit
    filePath: src/x.cs"
    [ "$status" -eq 0 ]
    [ "$(read_status)" = "completed" ]
    [ "$(read_audit auditActions)" = "1" ]
    [ "$(read_audit auditFiles)" = "1" ]
}

@test "beginTurn creates current-turn.yaml when called directly" {
    source "$LIB"
    rm -f "$(test_cache_file current-turn.yaml)"
    run repl_invoke "workflow.sessionlog.beginTurn" "requestId: req-test-begin-001
queryTitle: Direct begin
queryText: |
  Start work now."
    [ "$status" -eq 0 ]
    turn_file="$(test_cache_file current-turn.yaml)"
    grep -q '^turnRequestId: req-test-begin-001' "$turn_file"
    grep -q '^status: in_progress' "$turn_file"
}

@test "openSession populates missing session metadata" {
    source "$LIB"
    cat > "$(test_cache_file session-state.yaml)" <<EOF
status: verified
workspacePath: "/tmp/ws"
workspace: "test"
baseUrl: "http://localhost:1"
timestamp: "2026-04-19T00:00:00Z"
EOF
    run repl_invoke "workflow.sessionlog.openSession" "agent: Codex
title: Fix the plugin
model: gpt-5.4"
    [ "$status" -eq 0 ]
    session_file="$(test_cache_file session-state.yaml)"
    grep -q '^sessionId: Codex-' "$session_file"
    grep -q '^sourceType: Codex' "$session_file"
    grep -q '^title: Fix the plugin' "$session_file"
}

@test "raw client.SessionLog.QueryAsync passes through to mcpserver-repl" {
    source "$LIB"
    run repl_invoke "client.SessionLog.QueryAsync" "limit: 1"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "type: response"
}

@test "workflow.sessionlog.queryHistory uses compatibility marker without workspace env override" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_REQUIRE_COMPAT_AUTH=1
    export STUB_AUTH_FAIL_WORKSPACE_PATH="$SANDBOX/workspace"
    source "$LIB"

    run repl_invoke "workflow.sessionlog.queryHistory" "agent: Codex
limit: 1"

    unset MCPSERVER_API_KEY
    unset STUB_REQUIRE_COMPAT_AUTH
    unset STUB_AUTH_FAIL_WORKSPACE_PATH
    [ "$status" -eq 0 ]
    [ "$(grep -c "method=client.SessionLog.QueryAsync" "$STUB_LOG")" -eq 1 ]
    grep -q "cwd=.*repl-marker" "$STUB_LOG"
    grep -q '^MCP_WORKSPACE_PATH=$' "$STUB_LOG"
    ! grep -q "MCP_WORKSPACE_PATH=$SANDBOX/workspace" "$STUB_LOG"
}

@test "workflow.sessionlog.queryHistory honors explicit workspacePath in HTTP query" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    other_workspace="$SANDBOX/other-workspace"
    mkdir -p "$other_workspace"
    source "$LIB"

    run repl_invoke "workflow.sessionlog.queryHistory" "workspacePath: \"$other_workspace\"
limit: 1"

    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    grep -q "curl_method=GET" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/sessionlog" "$STUB_LOG"
    grep -q "curl_header=X-Api-Key: test-api-key" "$STUB_LOG"
    grep -q "curl_header=X-Workspace-Path: $other_workspace" "$STUB_LOG"
    grep -q "curl_query=limit=1" "$STUB_LOG"
    ! grep -q "method=client.SessionLog.QueryAsync" "$STUB_LOG"
}

@test "workflow.todo.query uses authenticated HTTP fallback before REPL" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_REQUIRE_COMPAT_AUTH=1
    export STUB_AUTH_FAIL_WORKSPACE_PATH="$SANDBOX/workspace"
    source "$LIB"

    run repl_invoke "workflow.todo.query" "id: RENDER-MAP3D-001"

    unset MCPSERVER_API_KEY
    unset STUB_REQUIRE_COMPAT_AUTH
    unset STUB_AUTH_FAIL_WORKSPACE_PATH
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"id":"RENDER-MAP3D-001"'
    ! grep -q "method=client.Todo.QueryAsync" "$STUB_LOG"
    grep -q "curl_method=GET" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/todo" "$STUB_LOG"
    grep -q "curl_query=id=RENDER-MAP3D-001" "$STUB_LOG"
    grep -q "curl_header=X-Api-Key: test-api-key" "$STUB_LOG"
    grep -q "curl_header=X-Workspace-Path: $SANDBOX/workspace" "$STUB_LOG"
}

@test "workflow.todo.create typed fallback wraps flat YAML in request parameter" {
    write_requirements_state
    source "$LIB"

    typed_params="$(_repl_todo_typed_params "create" "id: MCP-TODO-CREATE-001
title: Create through workflow shim
section: Backlog
priority: medium
implementationTasks:
  - task: Normalize create request
    done: false")"

    printf '%s\n' "$typed_params" | grep -q "^request:"
    printf '%s\n' "$typed_params" | grep -q "^  id: MCP-TODO-CREATE-001"
    printf '%s\n' "$typed_params" | grep -q "^  implementationTasks:"
}

@test "workflow.todo.get falls back to authenticated HTTP when typed client rejects id" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_TODO_GET_MISSING_ID=1
    source "$LIB"

    run repl_invoke "workflow.todo.get" "id: RENDER-MAP3D-001"

    unset MCPSERVER_API_KEY
    unset STUB_TODO_GET_MISSING_ID
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"id":"RENDER-MAP3D-001"'
    ! grep -q "method=client.Todo.GetAsync" "$STUB_LOG"
    grep -q "curl_method=GET" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/todo/RENDER-MAP3D-001" "$STUB_LOG"
    grep -q "curl_header=X-Api-Key: test-api-key" "$STUB_LOG"
    grep -q "curl_header=X-Workspace-Path: $SANDBOX/workspace" "$STUB_LOG"
}

@test "workflow.todo.update typed fallback wraps flat YAML in request parameter" {
    write_requirements_state
    source "$LIB"

    typed_params="$(_repl_todo_typed_params "update" "id: MCP-TODO-CREATE-001
done: true
doneSummary: >-
  Finished through typed workflow
  without literal scalar markers.")"

    printf '%s\n' "$typed_params" | grep -q "^id: MCP-TODO-CREATE-001"
    printf '%s\n' "$typed_params" | grep -q "^request:"
    printf '%s\n' "$typed_params" | grep -q "^  done: true"
    printf '%s\n' "$typed_params" | grep -q "^  doneSummary: >-"
}

@test "workflow.todo.update falls back to authenticated HTTP with JSON body" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_TODO_UPDATE_REJECT=1
    source "$LIB"

    run repl_invoke "workflow.todo.update" "id: RENDER-MAP3D-001
done: true
note: finished"

    unset MCPSERVER_API_KEY
    unset STUB_TODO_UPDATE_REJECT
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"updated":true'
    ! grep -q "method=client.Todo.UpdateAsync" "$STUB_LOG"
    grep -q "curl_method=PUT" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/todo/RENDER-MAP3D-001" "$STUB_LOG"
    grep -q 'curl_body=.*"note":"finished"' "$STUB_LOG"
    grep -q 'curl_body=.*"done":true' "$STUB_LOG"
}

@test "TODO HTTP body folds doneSummary and maps implementationTasks objects" {
    write_requirements_state
    source "$LIB"

    body="$(_repl_todo_json_body "update" "section: Architecture
doneSummary: >-
  Completed the typed create path
  and preserved YAML semantics.
implementationTasks:
  - task: Normalize typed request
    done: true
  - Add fallback diagnostics")"

    printf '%s\n' "$body" | grep -q '"doneSummary":"Completed the typed create path and preserved YAML semantics."'
    printf '%s\n' "$body" | grep -q '"section":"Backlog"'
    printf '%s\n' "$body" | grep -q '"implementationTasks":\[{"task":"Normalize typed request","done":true},{"task":"Add fallback diagnostics","done":false}\]'
    ! printf '%s\n' "$body" | grep -q '>-'
}

@test "TODO HTTP body does not promote implementation task done to parent done" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    source "$LIB"

    body="$(_repl_todo_json_body "update" "implementationTasks:
  - task: Complete the focused regression
    done: true
  - task: Keep parent TODO open
    done: false")"

    printf '%s\n' "$body" | node -e '
const fs = require("fs");
const body = JSON.parse(fs.readFileSync(0, "utf8"));
if (Object.prototype.hasOwnProperty.call(body, "done")) process.exit(1);
if (!Array.isArray(body.implementationTasks)) process.exit(2);
if (body.implementationTasks[0].done !== true) process.exit(3);
if (body.implementationTasks[1].done !== false) process.exit(4);
'
}

@test "TODO HTTP body accepts JSON request wrapper because JSON is YAML" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    source "$LIB"

    body="$(_repl_todo_json_body "update" '{"id":"RENDER-MAP3D-001","request":{"done":true,"section":"Architecture","doneSummary":"Finished from JSON"}}')"

    printf '%s\n' "$body" | grep -q '"done":true'
    printf '%s\n' "$body" | grep -q '"section":"Backlog"'
    printf '%s\n' "$body" | grep -q '"doneSummary":"Finished from JSON"'
    ! printf '%s\n' "$body" | grep -q '"request"'
}

@test "TODO HTTP body preserves blank lines and indentation in description Markdown" {
    # ISS-TODO-001 (FR-MCP-108): description is formatted Markdown. The HTTP-fallback
    # array builder must keep blank-line entries and leading indentation so fenced code
    # blocks and paragraph spacing survive instead of being collapsed.
    write_requirements_state
    source "$LIB"

    body="$(_repl_todo_json_body "create" "id: MD-PLUGIN-001
title: Markdown body
section: Backlog
priority: low
description:
  - '# Heading'
  - ''
  - '    int y = 2;'")"

    printf '%s\n' "$body" | grep -q '"description":\["# Heading","","    int y = 2;"\]'
}

@test "TODO typed params preserve multi-line Markdown description verbatim" {
    # ISS-TODO-001 (FR-MCP-108): the typed-client path wraps raw YAML under request: and
    # must not strip blank-line list entries from the description before the server parses it.
    write_requirements_state
    source "$LIB"

    typed_params="$(_repl_todo_typed_params "create" "id: MD-PLUGIN-002
title: Markdown typed
section: Backlog
priority: low
description:
  - '# Heading'
  - ''
  - '    int y = 2;'")"

    printf '%s\n' "$typed_params" | grep -q "^request:"
    printf '%s\n' "$typed_params" | grep -q "^    - '# Heading'"
    printf '%s\n' "$typed_params" | grep -q "^    - ''"
    printf '%s\n' "$typed_params" | grep -q "^    - '    int y = 2;'"
}

@test "workflow.todo.create HTTP fallback preserves 4xx response body" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_TODO_HTTP_400=1
    source "$LIB"

    run _repl_todo_http_fallback "create" "id: MCP-TODO-CREATE-400
title: Invalid section
section: Architecture
priority: medium"

    unset MCPSERVER_API_KEY
    unset STUB_TODO_HTTP_400
    [ "$status" -eq 1 ]
    echo "$output" | grep -q "TODO HTTP fallback returned HTTP 400 for create"
    echo "$output" | grep -q "section Architecture is not valid"
}

@test "workflow.todo.updateSelected uses selected TODO through workflow fallback" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_TODO_UPDATE_REJECT=1
    source "$LIB"

    run repl_invoke "workflow.todo.select" "id: RENDER-MAP3D-001"
    [ "$status" -eq 0 ]
    run repl_invoke "workflow.todo.updateSelected" "done: true"

    unset MCPSERVER_API_KEY
    unset STUB_TODO_UPDATE_REJECT
    [ "$status" -eq 0 ]
    grep -q "curl_method=PUT" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/todo/RENDER-MAP3D-001" "$STUB_LOG"
    grep -q 'curl_body=.*"done":true' "$STUB_LOG"
}

@test "raw call returns exit 1 when mcpserver-repl emits type: error" {
    source "$LIB"
    run repl_invoke "client.SessionLog.NopeAsync" ""
    [ "$status" -eq 1 ]
    echo "$output" | grep -q "type: error"
}

@test "workflow verbs never fall through to raw error path" {
    write_turn "in_progress"
    source "$LIB"
    # The stub returns type:error for workflow.* methods. If shim were
    # removed, this assertion would catch the regression.
    run repl_invoke "workflow.sessionlog.completeTurn" "response: regression guard"
    [ "$status" -eq 0 ]
    [ "$(read_status)" = "completed" ]
}

@test "completeTurn fails closed when mcpserver-repl is unavailable" {
    write_turn "in_progress"
    # Strip our stub bin from PATH; coreutils stay reachable.
    PATH_BACKUP="$PATH"
    export PATH="$(printf '%s' "$PATH_BACKUP" | tr ':' '\n' | grep -vxF "$SANDBOX/bin" | paste -sd ':' -)"
    source "$LIB"
    run repl_invoke "workflow.sessionlog.completeTurn" "response: offline"
    export PATH="$PATH_BACKUP"
    [ "$status" -ne 0 ]
    [ "$(read_status)" = "in_progress" ]
    echo "$output" | grep -q "session_turn_persist_failed"
}

@test "workflow.memory.list is routed through the memory workflow shim" {
    source "$LIB"

    run repl_invoke "workflow.memory.list" "scope: Effective"

    [ "$status" -eq 0 ]
    grep -q "MEMORY-REQ-001" <<<"$output"
    grep -q "method=workflow.memory.list" "$STUB_LOG"
}

@test "workflow.memory.add appends audit action and clears failsafe after success" {
    source "$LIB"
    _repl_workflow_append_actions() {
        printf '%s\n' "$1" >> "$SANDBOX/memory-actions.log"
    }

    run repl_invoke "workflow.memory.add" "id: MEMORY-REQ-001
category: REQ
text: Keep exact wording.
scope: Workspace
updatedBy: Codex"

    [ "$status" -eq 0 ]
    grep -q "method=workflow.memory.add" "$STUB_LOG"
    grep -q "Memory add MEMORY-REQ-001" "$SANDBOX/memory-actions.log"
    [ "$(find "$SANDBOX" -path '*/failsafe/*' -type f | wc -l | tr -d ' ')" = "0" ]
}

@test "workflow.requirements.listFr falls back to typed client instead of method_not_found" {
    write_requirements_state
    source "$LIB"
    run repl_invoke "workflow.requirements.listFr" ""
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "type: result"
    ! echo "$output" | grep -q "method_not_found"
    grep -q "method=workflow.requirements.listFr" "$STUB_LOG"
    grep -q "method=client.Requirements.ListFrAsync" "$STUB_LOG"
}

@test "workflow.requirements.updateFr uses typed client even when workflow would emit auth error" {
    write_requirements_state
    export STUB_WORKFLOW_REQUIREMENTS_AUTH_ERROR=1
    source "$LIB"
    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-901
title: Requirements shim
description: Plugin wrapper must route update calls through typed fallback
priority: high
status: in_progress
notes: Preserve requirement metadata"
    unset STUB_WORKFLOW_REQUIREMENTS_AUTH_ERROR
    [ "$status" -eq 0 ]
    ! echo "$output" | grep -q "Authentication required"
    ! grep -q "method=workflow.requirements.updateFr" "$STUB_LOG"
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+status: in_progress" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+notes: Preserve requirement metadata" "$STUB_LOG"
}

@test "workflow.requirements.updateFr hydrates existing entry for priority-only update" {
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-901
priority: high
notes: Live priority repair"

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.GetFrAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+title: Stored FR" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+body: Stored body" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+status: pending" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+notes: Live priority repair" "$STUB_LOG"
}

@test "workflow.requirements.updateFr hydrates from workflow get before typed get" {
    write_requirements_state
    export STUB_WORKFLOW_REQUIREMENTS_GET_SUCCEEDS=1
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-903
priority: high"

    unset STUB_WORKFLOW_REQUIREMENTS_GET_SUCCEEDS
    [ "$status" -eq 0 ]
    grep -q "method=workflow.requirements.getFr" "$STUB_LOG"
    ! grep -q "method=client.Requirements.GetFrAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+title: Workflow FR" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+body: Workflow FR body" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
}

@test "workflow.requirements update typed fallbacks preserve TR and TEST metadata" {
    write_requirements_state
    export STUB_WORKFLOW_REQUIREMENTS_AUTH_ERROR=1
    source "$LIB"

    run repl_invoke "workflow.requirements.updateTr" "id: TR-MCP-PLUGIN-901
title: Requirements shim TR
description: Plugin wrapper must route TR metadata
priority: high
status: completed
notes: TR metadata"
    [ "$status" -eq 0 ]

    run repl_invoke "workflow.requirements.updateTest" "id: TEST-MCP-901
title: Requirements shim TEST
description: Plugin wrapper must route TEST metadata
priority: high
status: completed
notes: TEST metadata"
    unset STUB_WORKFLOW_REQUIREMENTS_AUTH_ERROR
    [ "$status" -eq 0 ]

    grep -q "method=client.Requirements.UpdateTrAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.UpdateTestAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+status: completed" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+notes: TR metadata" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+notes: TEST metadata" "$STUB_LOG"
}

@test "workflow.requirements update typed fallbacks hydrate TR and TEST priority-only updates" {
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.updateTr" "id: TR-MCP-PLUGIN-901
priority: high"
    [ "$status" -eq 0 ]

    run repl_invoke "workflow.requirements.updateTest" "id: TEST-MCP-901
priority: high"
    [ "$status" -eq 0 ]

    grep -q "method=client.Requirements.GetTrAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.GetTestAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+title: Stored TR" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+body: Stored TR body" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+title: Stored TEST" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+condition: Stored TEST condition" "$STUB_LOG"
    [ "$(grep -Ec "input:[[:space:]]+priority: high" "$STUB_LOG")" -ge 2 ]
}

@test "workflow.requirements.createFr uses typed client so workflow cannot swallow high priority" {
    write_requirements_state
    export STUB_WORKFLOW_REQUIREMENTS_CREATE_IGNORES_PRIORITY=1
    source "$LIB"

    run repl_invoke "workflow.requirements.createFr" "id: FR-MCP-902
title: High priority FR
description: High priority must survive create.
priority: high
area: MCP"

    unset STUB_WORKFLOW_REQUIREMENTS_CREATE_IGNORES_PRIORITY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "priority: high"
    grep -q "method=client.Requirements.CreateFrAsync" "$STUB_LOG"
    ! grep -q "method=workflow.requirements.createFr" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
}

@test "workflow.requirements.createFr hydrates empty typed create result" {
    write_requirements_state
    export STUB_TYPED_CREATE_EMPTY=1
    source "$LIB"

    run repl_invoke "workflow.requirements.createFr" "id: FR-MCP-904
title: Empty typed create result
description: Create returns an empty result but still persists.
priority: high
area: MCP"

    unset STUB_TYPED_CREATE_EMPTY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "FR-MCP-904"
    grep -q "method=client.Requirements.CreateFrAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.GetFrAsync" "$STUB_LOG"
    ! grep -q "method=workflow.requirements.createFr" "$STUB_LOG"
}

@test "workflow.requirements.createFrBatch verifies persisted records by readback" {
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.createFrBatch" "records:
  - id: FR-MCP-BATCH-001
    title: Batch verification
    description: Batch records must be read back.
    priority: high
    area: MCP"

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.CreateFrBatchAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.GetFrAsync" "$STUB_LOG"
    echo "$output" | grep -q "success: true"
}

@test "workflow.requirements.createTrBatch fails when readback cannot verify persistence" {
    write_requirements_state
    export STUB_REQUIREMENTS_GET_NOT_FOUND=1
    source "$LIB"

    run repl_invoke "workflow.requirements.createTrBatch" "records:
  - id: TR-MCP-BATCH-001
    title: Missing batch record
    description: False success must be rejected.
    priority: high
    area: MCP
    subarea: Requirements"

    unset STUB_REQUIREMENTS_GET_NOT_FOUND
    [ "$status" -ne 0 ]
    grep -q "method=client.Requirements.CreateTrBatchAsync" "$STUB_LOG"
    grep -q "method=client.Requirements.GetTrAsync" "$STUB_LOG"
    echo "$output" | grep -q "requirements_batch_persist_verification_failed"
    echo "$output" | grep -q "TR-MCP-BATCH-001"
}

@test "workflow.requirements.createFr listFr getFr works through typed fallback" {
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.createFr" "id: FR-MCP-901
title: Requirements shim
description: Plugin wrapper must route requirements calls
priority: high
area: MCP"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "FR-MCP-901"

    run repl_invoke "workflow.requirements.listFr" ""
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "FR-MCP-901"

    run repl_invoke "workflow.requirements.getFr" "id: FR-MCP-901"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "FR-MCP-901"
}

@test "workflow.requirements.createFr typed params include acceptanceCriteria block when provided" {
    # FR-MCP-REQACPLUGIN-001 / TEST-MCP-REQACPLUGIN-BASH: the typed-params builder must emit
    # the acceptanceCriteria YAML list so the typed-client CreateFrAsync call carries the
    # structured criteria through to the server.
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.createFr" "id: FR-MCP-AC-100
title: AC create
description: Plugin must thread acceptanceCriteria through createFr
priority: high
area: MCP
acceptanceCriteria:
  - id: ac-1
    text: 'First criterion'
    isSatisfied: false
  - id: ac-2
    text: 'Second criterion'
    isSatisfied: true
    evidence: 'unit'"

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.CreateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+acceptanceCriteria:" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+- id: ac-1" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+text: 'First criterion'" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+evidence: 'unit'" "$STUB_LOG"
}

@test "workflow.requirements.createTest typed params include acceptanceCriteria block when provided" {
    # FR-MCP-REQACPLUGIN-001 / TEST-MCP-REQACPLUGIN-BASH: same as createFr but for the TEST kind.
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.createTest" "id: TEST-MCP-AC-100
title: AC test create
description: Condition body
priority: high
area: MCP
acceptanceCriteria:
  - id: ac-1
    text: 'Test criterion'
    isSatisfied: false"

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.CreateTestAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+acceptanceCriteria:" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+- id: ac-1" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+text: 'Test criterion'" "$STUB_LOG"
}

@test "workflow.requirements.updateFr typed params hydrate acceptanceCriteria from existing on partial update" {
    # FR-MCP-REQACPLUGIN-001 / TEST-MCP-REQACPLUGIN-BASH: when the caller does not supply
    # acceptanceCriteria, the plugin re-emits the criteria carried by the hydration source
    # so a priority-only update does not wipe structured criteria.
    write_requirements_state
    export STUB_WORKFLOW_REQUIREMENTS_GET_WITH_AC=1
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-AC-200
priority: high"

    unset STUB_WORKFLOW_REQUIREMENTS_GET_WITH_AC
    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+priority: high" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+acceptanceCriteria:" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+- id: existing-ac-1" "$STUB_LOG"
}

@test "workflow.requirements.updateFr typed params prefer caller acceptanceCriteria on criteria-only update" {
    # FR-MCP-REQACPLUGIN-002: a criteria-only update must send the caller-supplied
    # acceptanceCriteria block instead of hydrating an empty/stale existing list.
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-AC-201
acceptanceCriteria:
  - id: caller-ac-1
    text: caller criterion text
    isSatisfied: false"

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+acceptanceCriteria:" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+- id: caller-ac-1" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+text: caller criterion text" "$STUB_LOG"
}

@test "workflow.requirements.updateFr typed params preserve JSON acceptanceCriteria on criteria-only update" {
    # FR-MCP-REQACPLUGIN-002: JSON params are accepted by the wrapper and must not
    # bypass the acceptanceCriteria shaper/guard path.
    write_requirements_state
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" '{"id":"FR-MCP-AC-JSON","acceptanceCriteria":[{"id":"json-ac-1","text":"json criterion text","isSatisfied":false}]}'

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.UpdateFrAsync" "$STUB_LOG"
    grep -Eq "input:[[:space:]]+acceptanceCriteria:" "$STUB_LOG"
    grep -Eq 'input:[[:space:]]+- id: "json-ac-1"' "$STUB_LOG"
    grep -Eq 'input:[[:space:]]+text: "json criterion text"' "$STUB_LOG"
}

@test "workflow.requirements.updateFr fails when supplied acceptanceCriteria returns empty" {
    # FR-MCP-REQACPLUGIN-002: do not report success when the server response proves
    # that supplied structured criteria were not captured.
    write_requirements_state
    export STUB_REQUIREMENTS_EMPTY_AC_RESPONSE=1
    source "$LIB"

    run repl_invoke "workflow.requirements.updateFr" "id: FR-MCP-AC-202
acceptanceCriteria:
  - id: caller-ac-1
    text: caller criterion text
    isSatisfied: false"

    unset STUB_REQUIREMENTS_EMPTY_AC_RESPONSE
    [ "$status" -ne 0 ]
    grep -q "requirements_acceptance_criteria_not_captured" <<<"$output"
    grep -q "acceptanceCriteria was supplied" <<<"$output"
}

@test "workflow.requirements.copyAcceptanceCriteriaFromTodo wrapper hits copy endpoint" {
    # FR-MCP-REQACPLUGIN-001 / TEST-MCP-REQACPLUGIN-BASH: the workflow wrapper exposes
    # the server copy operation so agents can copy TODO acceptance criteria without raw REST.
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"

    run repl_invoke "workflow.requirements.copyAcceptanceCriteriaFromTodo" "kind: fr
id: FR-MCP-AC-200
todoId: TODO-AC-1"

    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"copied":true'
    grep -q "curl_method=POST" "$STUB_LOG"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/requirements/fr/FR-MCP-AC-200/acceptance-criteria/copy-from-todo" "$STUB_LOG"
    grep -q "curl_header=X-Api-Key: test-api-key" "$STUB_LOG"
    grep -q "curl_header=X-Workspace-Path: $SANDBOX/workspace" "$STUB_LOG"
    grep -q 'curl_body={"todoId":"TODO-AC-1"}' "$STUB_LOG"
}

@test "workflow.requirements.generateDocument returns content through typed fallback" {
    write_requirements_state
    source "$LIB"
    run repl_invoke "workflow.requirements.generateDocument" "format: markdown
docType: matrix"
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "Requirement Traceability Matrix"
    grep -q "method=client.Requirements.GenerateAsync" "$STUB_LOG"
    grep -q "input:     doc: mapping" "$STUB_LOG"
    grep -q "input:     format: markdown" "$STUB_LOG"
}

@test "workflow.requirements.generateDocument returns wiki ZIP content instead of workspace metadata" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"
    run repl_invoke "workflow.requirements.generateDocument" "format: wiki
docType: all"
    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "contentBase64: UEsDBA=="
    echo "$output" | grep -q "contentType: application/zip"
    echo "$output" | grep -q "fileName: requirements-wiki-documents.zip"
    ! echo "$output" | grep -q "outputRoot: /workspace/docs/Project/wiki"
    grep -q "input:     doc: all" "$STUB_LOG"
    grep -q "input:     format: wiki" "$STUB_LOG"
}

@test "workflow.requirements.generateDocument falls back when workflow rejects wiki format" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_WORKFLOW_REQUIREMENTS_REJECT_WIKI=1
    source "$LIB"
    run repl_invoke "workflow.requirements.generateDocument" "format: wiki
docType: all"
    unset MCPSERVER_API_KEY
    unset STUB_WORKFLOW_REQUIREMENTS_REJECT_WIKI
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "contentBase64: UEsDBA=="
    ! echo "$output" | grep -q "Invalid format: wiki"
    grep -q "method=workflow.requirements.generateDocument" "$STUB_LOG"
    grep -q "method=client.Requirements.GenerateAsync" "$STUB_LOG"
}

@test "workflow.requirements.generateDocument HTTP fallback returns wiki ZIP bytes" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"
    run _repl_requirements_generate_http_fallback "format: wiki
docType: all"
    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "contentBase64: UEsDBA=="
    echo "$output" | grep -q "contentType: application/zip"
    echo "$output" | grep -q "fileName: requirements-wiki-documents.zip"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/requirements/generate?doc=all&format=wiki" "$STUB_LOG"
}

@test "workflow.requirements.generateDocument accepts JSON params because JSON is YAML" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"
    run repl_invoke "workflow.requirements.generateDocument" '{"format":"wiki","docType":"all"}'
    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q "contentBase64: UEsDBA=="
    echo "$output" | grep -q "contentType: application/zip"
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/requirements/generate?doc=all&format=wiki" "$STUB_LOG"
}

@test "workflow.requirements.ingestDocument passes wiki documents and timestamps to typed fallback" {
    write_requirements_state
    source "$LIB"
    run repl_invoke "workflow.requirements.ingestDocument" "format: wiki
sourceFormat: wiki
preferredWikiFormat: github
documents:
  github/Functional-Requirements.md:
    content: |
      # Functional Requirements (MCP Server)
    lastModifiedUtc: 2026-05-08T12:00:00Z"
    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.IngestAsync" "$STUB_LOG"
    grep -q "sourceFormat" "$STUB_LOG"
    grep -q "preferredWikiFormat" "$STUB_LOG"
    grep -q "documents" "$STUB_LOG"
    grep -q "github/Functional-Requirements.md" "$STUB_LOG"
    grep -q "lastModifiedUtc" "$STUB_LOG"
}

@test "workflow.requirements.ingestDocument passes inline content to typed ingest first" {
    write_requirements_state
    source "$LIB"
    run repl_invoke "workflow.requirements.ingestDocument" "sourceFormat: markdown
content: |
  # Functional Requirements
  - FR-MCP-INLINE-001: Inline ingest must not fall back to default files."

    [ "$status" -eq 0 ]
    grep -q "method=client.Requirements.IngestAsync" "$STUB_LOG"
    ! grep -q "method=workflow.requirements.ingestDocument" "$STUB_LOG"
    grep -q "functionalMarkdown" "$STUB_LOG"
    grep -q "FR-MCP-INLINE-001" "$STUB_LOG"
}

@test "workflow.requirements uses session-state workspace path and base URL for mcpserver-repl" {
    write_requirements_state
    source "$LIB"
    run repl_invoke "workflow.requirements.listFr" ""
    [ "$status" -eq 0 ]
    grep -q "cwd=$SANDBOX/workspace" "$STUB_LOG"
    grep -q "MCP_WORKSPACE_PATH=$SANDBOX/workspace" "$STUB_LOG"
    grep -q "MCP_SERVER_URL=http://127.0.0.1:8765" "$STUB_LOG"
}

@test "pending import transformer emits session recovery and TODO YAML commands" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    cat > "$SANDBOX/pending-import.json" <<'JSON'
{
  "createdAtUtc": "2026-05-14T05:12:23Z",
  "createdBy": "Codex",
  "targetMcpSession": {
    "sourceType": "Codex",
    "sessionId": "Codex-20260514T000000Z-import-test",
    "title": "Import test",
    "model": "gpt-5"
  },
  "turns": [
    {
      "requestId": "req-20260514T000100Z-imported",
      "queryTitle": "Imported turn",
      "queryText": "Import this turn",
      "status": "completed-pending-import",
      "actions": [
        { "type": "test", "status": "completed", "description": "validated" }
      ]
    }
  ],
  "operations": [
    {
      "id": "todo-test",
      "kind": "todo.create",
      "payload": {
        "id": "MCP-IMPORT-001",
        "title": "Import TODO",
        "priority": "high",
        "section": "Backlog",
        "implementationTasks": ["Replay through plugin"]
      }
    }
  ]
}
JSON

    run node "$PLUGIN_ROOT/lib/pending-import-to-yaml.js" "$SANDBOX/pending-import.json"

    [ "$status" -eq 0 ]
    echo "$output" | grep -q "workflow.sessionlog.importRecovery"
    echo "$output" | grep -q "workflow.todo.create"
}

@test "workflow.sessionlog.importRecovery uses marker-auth HTTP submit before REPL" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"

    run repl_invoke "workflow.sessionlog.importRecovery" 'sessionLog: {"sourceType":"Codex","sessionId":"Codex-20260514T000000Z-import-test","title":"Import test","status":"completed","turns":[{"requestId":"req-20260514T000100Z-imported","queryTitle":"Imported","queryText":"Import this","status":"completed","response":"done"}]}'

    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"success":true'
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/sessionlog" "$STUB_LOG"
    grep -q '"sessionId":"Codex-20260514T000000Z-import-test"' "$STUB_LOG"
    ! grep -q "method=workflow.sessionlog.importRecovery" "$STUB_LOG"
}

@test "workflow.sessionlog.importRecovery accepts nested YAML sessionLog payloads" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    source "$LIB"

    run repl_invoke "workflow.sessionlog.importRecovery" 'sessionLog:
  sourceType: Codex
  sessionId: Codex-20260515T000000Z-import-yaml-test
  title: Import YAML test
  status: completed
  turns:
    - requestId: req-20260515T000100Z-imported-yaml
      queryTitle: Imported YAML
      queryText: |
        Import this nested YAML recovery turn.
      status: completed
      response: >-
        folded response should become text, not the literal marker.
      actions:
        - type: test
          status: completed
          description: validated nested yaml'

    unset MCPSERVER_API_KEY
    [ "$status" -eq 0 ]
    echo "$output" | grep -q '"success":true'
    grep -q "curl_url=http://127.0.0.1:8765/mcpserver/sessionlog" "$STUB_LOG"
    grep -q '"sessionId":"Codex-20260515T000000Z-import-yaml-test"' "$STUB_LOG"
    grep -q '"queryTitle":"Imported YAML"' "$STUB_LOG"
    grep -q '"response":"folded response should become text, not the literal marker."' "$STUB_LOG"
    grep -q '"description":"validated nested yaml"' "$STUB_LOG"
    ! grep -q "method=workflow.sessionlog.importRecovery" "$STUB_LOG"
}

@test "failsafe write stores replayable generic operation under workspace .mcpServer" {
    command -v node >/dev/null 2>&1 || skip "node not available"
    write_requirements_state
    source "$LIB"

    failsafe_file="$(_repl_failsafe_write "workflow.todo.create" "id: MCP-FAILSAFE-001
title: Failsafe TODO
section: Backlog
priority: high" "test_failure")"

    [ -f "$failsafe_file" ]
    echo "$failsafe_file" | grep -q "$SANDBOX/workspace/.mcpServer/failsafe/codex"
    grep -q '"kind": "mcpserver-plugin-failsafe"' "$failsafe_file"
    grep -q '"method": "workflow.todo.create"' "$failsafe_file"

    run node "$PLUGIN_ROOT/lib/pending-import-to-yaml.js" "$failsafe_file"

    [ "$status" -eq 0 ]
    echo "$output" | grep -q "workflow.todo.create"
    printf '%s\n' "$output" | cut -f2 | base64 -d | grep -q "MCP-FAILSAFE-001"
}

@test "failsafe clear removes acknowledged operation" {
    write_requirements_state
    source "$LIB"

    failsafe_file="$(_repl_failsafe_write "workflow.requirements.createFr" "id: FR-FAILSAFE-001" "test_ack")"
    [ -f "$failsafe_file" ]

    _repl_failsafe_clear "$failsafe_file"

    [ ! -e "$failsafe_file" ]
}

@test "workflow.requirements prefers compatibility marker when direct marker auth would fail" {
    write_requirements_state
    export MCPSERVER_API_KEY="test-api-key"
    export STUB_REQUIRE_COMPAT_AUTH=1
    export STUB_AUTH_FAIL_WORKSPACE_PATH="$SANDBOX/workspace"
    source "$LIB"

    run repl_invoke "workflow.requirements.listFr" ""

    unset MCPSERVER_API_KEY
    unset STUB_REQUIRE_COMPAT_AUTH
    unset STUB_AUTH_FAIL_WORKSPACE_PATH
    [ "$status" -eq 0 ]
    [ "$(grep -c "method=client.Requirements.ListFrAsync" "$STUB_LOG")" -eq 1 ]
    awk '
        /method=client.Requirements.ListFrAsync/ { capture = 1 }
        capture && /cwd=.*repl-marker/ { saw_cwd = 1 }
        capture && /^MCP_WORKSPACE_PATH=$/ { saw_empty_workspace_env = 1 }
        /^---$/ { capture = 0 }
        END { exit((saw_cwd && saw_empty_workspace_env) ? 0 : 1) }
    ' "$STUB_LOG"
}
