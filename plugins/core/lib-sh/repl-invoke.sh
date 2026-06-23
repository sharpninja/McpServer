#!/usr/bin/env bash
set -uo pipefail

# repl_invoke <method> [params_yaml]
# Wrapper accepts YAML/JSON params. Direct mcpserver-repl --agent-stdio callers
# should send single-line JSON request envelopes.
# Workflow-prefixed methods are plugin-local shims that translate to either:
# - local cache mutations under cache/
# - the real client.* MCP methods exposed by mcpserver-repl

REPL_INVOKE_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPL_INVOKE_PLUGIN_ROOT="${PLUGIN_ROOT_OVERRIDE:-$(cd "$REPL_INVOKE_SCRIPT_DIR/.." && pwd)}"
REPL_INVOKE_CACHE_DIR="${REPL_INVOKE_PLUGIN_ROOT}/cache"

# shellcheck source=./cache-scope.sh
source "${REPL_INVOKE_SCRIPT_DIR}/cache-scope.sh"
cache_scope_init "$REPL_INVOKE_PLUGIN_ROOT" "$(pwd)"

# Agent for per-agent REPL cache and isolation. Plugins must pass --agent on every invocation.
# Preferred: MCP_AGENT_NAME (set by plugin-env), fallback to PLUGIN_AGENT_DEFAULT.
AGENT_NAME="${MCP_AGENT_NAME:-${PLUGIN_AGENT_DEFAULT:-${MCP_SESSION_AGENT:-default}}}"

_repl_now_iso() {
    date -u +%Y-%m-%dT%H:%M:%SZ
}

_repl_now_compact() {
    date -u +%Y%m%dT%H%M%SZ
}

_repl_slugify() {
    local value="${1:-}"
    printf '%s' "$value" \
        | tr '[:upper:]' '[:lower:]' \
        | sed 's/[^a-z0-9]/-/g; s/-\{2,\}/-/g; s/^-//; s/-$//' \
        | cut -c1-48
}

_repl_unquote() {
    local value="${1:-}"
    value="$(printf '%s' "$value" | sed 's/^"\(.*\)"$/\1/; s/^'\''\(.*\)'\''$/\1/')"
    printf '%s' "$value"
}

_repl_yaml_get() {
    # _repl_yaml_get <yaml_text> <key>
    local text="$1"
    local key="$2"
    local value
    if printf '%s' "$text" | grep -q '^[[:space:]]*[{[]' && command -v node >/dev/null 2>&1; then
        value="$(printf '%s' "$text" | node -e '
const fs = require("fs");
const key = process.argv[1];
const input = fs.readFileSync(0, "utf8").trim();
if (!input) process.exit(0);
let root;
try { root = JSON.parse(input); } catch { process.exit(0); }
let value = root && Object.prototype.hasOwnProperty.call(root, key) ? root[key] : undefined;
if (value === undefined && root && root.request && typeof root.request === "object" && !Array.isArray(root.request) && Object.prototype.hasOwnProperty.call(root.request, key)) {
  value = root.request[key];
}
if (value === undefined || value === null) process.exit(0);
if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
  process.stdout.write(String(value));
} else {
  process.stdout.write(JSON.stringify(value));
}
' "$key" 2>/dev/null || true)"
        if [ -n "$value" ]; then
            printf '%s\n' "$value"
            return 0
        fi
    fi
    printf '%s\n' "$text" | grep "^[[:space:]]*$key:" | head -1 | sed "s/^[[:space:]]*$key:[[:space:]]*//"
}

_repl_yaml_get_root() {
    # _repl_yaml_get_root <yaml_text> <key>
    # Reads only root-level YAML scalar fields. This intentionally excludes
    # nested fields such as implementationTasks[].done from parent TODO fields.
    local text="$1"
    local key="$2"
    printf '%s\n' "$text" | awk -v key="$key" '
        $0 ~ "^" key ":[[:space:]]*" {
            sub("^" key ":[[:space:]]*", "")
            print
            exit
        }
    '
}

_repl_yaml_block_get() {
    # _repl_yaml_block_get <yaml_text> <key>
    printf '%s\n' "$1" | awk -v key="$2" '
        $0 ~ "^[[:space:]]*" key ":[[:space:]]*[|>][-+]?[[:space:]]*$" {
            capture = 1
            folded = ($0 ~ ":[[:space:]]*>")
            next
        }
        capture {
            if ($0 ~ "^[^[:space:]]") {
                exit
            }
            sub(/^[[:space:]][[:space:]]/, "")
            if (folded) {
                if ($0 == "") {
                    if (value != "") {
                        value = value "\n"
                    }
                    sep = ""
                    next
                }
                value = value sep $0
                sep = " "
                next
            }
            print
        }
        END {
            if (folded && value != "") {
                print value
            }
        }
    '
}

_repl_list_block_get() {
    # _repl_list_block_get <yaml_text> <key>
    printf '%s\n' "$1" | awk -v key="$2" '
        function indent(line) {
            match(line, /^[[:space:]]*/)
            return RLENGTH
        }
        $0 ~ "^[[:space:]]*" key ":[[:space:]]*$" {
            capture = 1
            key_indent = indent($0)
            strip_indent = -1
            next
        }
        capture {
            if ($0 ~ "^[[:space:]]*$") {
                print
                next
            }

            line_indent = indent($0)
            if (line_indent <= key_indent && $0 !~ "^[[:space:]]*-") {
                exit
            }

            if (strip_indent < 0) {
                strip_indent = line_indent
            }

            if (strip_indent > 0 && length($0) >= strip_indent) {
                print substr($0, strip_indent + 1)
            } else {
                print
            }
        }
    '
}

_repl_json_array_block_get() {
    local text="$1"
    local key="$2"
    if ! printf '%s' "$text" | grep -q '^[[:space:]]*[{[]' || ! command -v node >/dev/null 2>&1; then
        return 0
    fi

    printf '%s' "$text" | node -e '
const fs = require("fs");
const key = process.argv[1];
const input = fs.readFileSync(0, "utf8").trim();
if (!input) process.exit(0);
let root;
try { root = JSON.parse(input); } catch { process.exit(0); }
let value = root && Object.prototype.hasOwnProperty.call(root, key) ? root[key] : undefined;
if (value === undefined && root && root.request && typeof root.request === "object" && !Array.isArray(root.request) && Object.prototype.hasOwnProperty.call(root.request, key)) {
  value = root.request[key];
}
if (!Array.isArray(value) || value.length === 0) process.exit(0);
function scalar(value) {
  if (value === null || value === undefined) return "null";
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  return JSON.stringify(String(value));
}
for (const item of value) {
  if (!item || typeof item !== "object" || Array.isArray(item)) {
    console.log(`- ${scalar(item)}`);
    continue;
  }
  const entries = Object.entries(item);
  if (entries.length === 0) {
    console.log("- {}");
    continue;
  }
  entries.forEach(([entryKey, entryValue], index) => {
    const rendered = entryValue && typeof entryValue === "object" ? JSON.stringify(entryValue) : scalar(entryValue);
    console.log(`${index === 0 ? "- " : "  "}${entryKey}: ${rendered}`);
  });
}
' "$key" 2>/dev/null || true
}

_repl_records_block_get() {
    local params_yaml="$1"
    local records_value records_block
    records_value="$(_repl_yaml_get "$params_yaml" "records" 2>/dev/null || true)"
    records_value="$(_repl_unquote "$records_value")"
    if printf '%s' "$records_value" | grep -Eq '^\[[[:space:]]*\{'; then
        printf '%s\n' "$records_value"
        return 0
    fi

    records_block="$(_repl_list_block_get "$params_yaml" "records")"
    _repl_records_block_normalize "$records_block"
}

_repl_records_block_normalize() {
    printf '%s\n' "$1" | awk '
        function indent(line) {
            match(line, /^[[:space:]]*/)
            return RLENGTH
        }
        {
            line_indent = indent($0)
            if (in_acceptance_criteria) {
                if ($0 ~ "^[[:space:]]*$") {
                    print
                    next
                }
                if (line_indent > acceptance_criteria_indent || (line_indent == acceptance_criteria_indent && $0 ~ "^[[:space:]]*-")) {
                    print "  " $0
                    next
                }
                in_acceptance_criteria = 0
            }

            print
            if ($0 ~ "^[[:space:]]*acceptanceCriteria:[[:space:]]*$") {
                in_acceptance_criteria = 1
                acceptance_criteria_indent = line_indent
            }
        }
    '
}

_repl_schema_error() {
    local method="$1"
    local message="$2"
    printf 'type: error\npayload:\n'
    printf '  code: schema_validation_failed\n'
    printf '  message: %s\n' "$message"
    printf '  details:\n'
    printf '    methodName: %s\n' "$method"
}

_repl_schema_has_text() {
    local params_yaml="$1"
    local key="$2"
    local value
    value="$(_repl_param_text "$params_yaml" "$key" 2>/dev/null || true)"
    value="$(_repl_unquote "$value")"
    if [ -n "$value" ]; then
        return 0
    fi

    printf '%s\n' "$params_yaml" | grep -Eq "^[[:space:]]*$key:[[:space:]]*(#.*)?$"
}

_repl_schema_require_text() {
    local method="$1"
    local params_yaml="$2"
    local key="$3"
    if ! _repl_schema_has_text "$params_yaml" "$key"; then
        _repl_schema_error "$method" "payload.params.${key} is required."
        return 1
    fi
}

_repl_schema_require_any_text() {
    local method="$1"
    local params_yaml="$2"
    shift 2
    local key
    for key in "$@"; do
        if _repl_schema_has_text "$params_yaml" "$key"; then
            return 0
        fi
    done
    _repl_schema_error "$method" "payload.params must include at least one of: $*."
    return 1
}

_repl_schema_has_records() {
    local params_yaml="$1"
    local records_block
    records_block="$(_repl_records_block_get "$params_yaml" 2>/dev/null || true)"
    if [ -n "$records_block" ] && printf '%s\n' "$records_block" | grep -Eq '^[[:space:]]*-|^\[[[:space:]]*\{' ; then
        return 0
    fi
    return 1
}

_repl_schema_require_records() {
    local method="$1"
    local params_yaml="$2"
    if ! _repl_schema_has_records "$params_yaml"; then
        _repl_schema_error "$method" "payload.params.records must be a non-empty array."
        return 1
    fi
}

_repl_schema_validate_method() {
    local method="$1"
    local params_yaml="${2:-}"
    case "$method" in
        workflow.sessionlog.openSession)
            # sessionId is intentionally NOT required: the openSession handler
            # generates a canonical <Agent>-<timestamp>-<slug> id when the caller
            # omits it (see _repl_workflow_open_session), and begin_turn auto-opens
            # a session with empty params. Requiring sessionId here breaks both
            # paths and contradicts the workspace rule that ids are plugin-generated.
            _repl_schema_require_text "$method" "$params_yaml" "title" || return 1
            ;;
        workflow.sessionlog.beginTurn)
            _repl_schema_require_text "$method" "$params_yaml" "requestId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "queryTitle" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "queryText" || return 1
            ;;
        workflow.sessionlog.failTurn)
            _repl_schema_require_text "$method" "$params_yaml" "errorMessage" || return 1
            ;;
        workflow.sessionlog.appendDialog)
            _repl_schema_require_text "$method" "$params_yaml" "dialogItems" || return 1
            ;;
        workflow.sessionlog.appendActions)
            _repl_schema_require_text "$method" "$params_yaml" "actions" || return 1
            ;;
        workflow.sessionlog.importRecovery|client.SessionLog.SubmitAsync)
            _repl_schema_require_text "$method" "$params_yaml" "sessionLog" || return 1
            ;;
        workflow.todo.get|workflow.todo.select|workflow.todo.delete|workflow.todo.analyzeRequirements|workflow.todo.streamStatus|workflow.todo.streamPlan|workflow.todo.streamImplement|workflow.todo.getProjectionStatus|workflow.todo.repairProjection)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.todo.create|client.Todo.CreateAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "title" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "section" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "priority" || return 1
            ;;
        workflow.todo.update|client.Todo.UpdateAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.memory.get|workflow.memory.remove)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.memory.add)
            _repl_schema_require_text "$method" "$params_yaml" "category" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "text" || return 1
            ;;
        workflow.memory.update)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.requirements.getFr|workflow.requirements.deleteFr|workflow.requirements.getTr|workflow.requirements.deleteTr|workflow.requirements.getTest|workflow.requirements.deleteTest|client.Requirements.GetFrAsync|client.Requirements.DeleteFrAsync|client.Requirements.GetTrAsync|client.Requirements.DeleteTrAsync|client.Requirements.GetTestAsync|client.Requirements.DeleteTestAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.requirements.createFr|client.Requirements.CreateFrAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "title" || return 1
            _repl_schema_require_any_text "$method" "$params_yaml" "description" "body" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "priority" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "area" || return 1
            ;;
        workflow.requirements.createTr|client.Requirements.CreateTrAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "title" || return 1
            _repl_schema_require_any_text "$method" "$params_yaml" "description" "body" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "priority" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "area" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "subarea" || return 1
            ;;
        workflow.requirements.createTest|client.Requirements.CreateTestAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "title" || return 1
            _repl_schema_require_any_text "$method" "$params_yaml" "description" "condition" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "priority" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "area" || return 1
            ;;
        workflow.requirements.updateFr|workflow.requirements.updateTr|workflow.requirements.updateTest|client.Requirements.UpdateFrAsync|client.Requirements.UpdateTrAsync|client.Requirements.UpdateTestAsync)
            _repl_schema_require_text "$method" "$params_yaml" "id" || return 1
            ;;
        workflow.requirements.createFrBatch|workflow.requirements.updateFrBatch|workflow.requirements.createTrBatch|workflow.requirements.updateTrBatch|workflow.requirements.createTestBatch|workflow.requirements.updateTestBatch|workflow.requirements.createBatch|workflow.requirements.updateBatch|client.Requirements.CreateFrBatchAsync|client.Requirements.UpdateFrBatchAsync|client.Requirements.CreateTrBatchAsync|client.Requirements.UpdateTrBatchAsync|client.Requirements.CreateTestBatchAsync|client.Requirements.UpdateTestBatchAsync|client.Requirements.CreateBatchAsync|client.Requirements.UpdateBatchAsync)
            _repl_schema_require_records "$method" "$params_yaml" || return 1
            ;;
        workflow.requirements.createMapping|client.Requirements.UpsertMappingAsync)
            _repl_schema_require_text "$method" "$params_yaml" "frId" || return 1
            _repl_schema_require_any_text "$method" "$params_yaml" "trId" "trIds" "testId" "testIds" || return 1
            ;;
        workflow.requirements.deleteMapping|client.Requirements.DeleteMappingAsync)
            _repl_schema_require_any_text "$method" "$params_yaml" "frId" "trId" "testId" || return 1
            ;;
        workflow.requirements.ingestDocument|client.Requirements.IngestAsync)
            _repl_schema_require_any_text "$method" "$params_yaml" "content" "documents" "functionalMarkdown" "technicalMarkdown" "testingMarkdown" "mappingMarkdown" || return 1
            ;;
        workflow.graphrag.status|workflow.graphrag.index|workflow.graphrag.documents.list|workflow.graphrag.entities.list|workflow.graphrag.relationships.list)
            ;;
        workflow.graphrag.query)
            _repl_schema_require_text "$method" "$params_yaml" "query" || return 1
            ;;
        workflow.graphrag.ingest)
            _repl_schema_require_text "$method" "$params_yaml" "content" || return 1
            ;;
        workflow.graphrag.documents.chunks|workflow.graphrag.documents.delete)
            _repl_schema_require_text "$method" "$params_yaml" "documentId" || return 1
            ;;
        workflow.graphrag.entities.get|workflow.graphrag.entities.delete)
            _repl_schema_require_text "$method" "$params_yaml" "entityId" || return 1
            ;;
        workflow.graphrag.entities.create)
            _repl_schema_require_text "$method" "$params_yaml" "name" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "entityType" || return 1
            ;;
        workflow.graphrag.entities.update)
            _repl_schema_require_text "$method" "$params_yaml" "entityId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "name" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "entityType" || return 1
            ;;
        workflow.graphrag.relationships.get|workflow.graphrag.relationships.delete)
            _repl_schema_require_text "$method" "$params_yaml" "relationshipId" || return 1
            ;;
        workflow.graphrag.relationships.create)
            _repl_schema_require_text "$method" "$params_yaml" "sourceEntityId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "targetEntityId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "relationshipType" || return 1
            ;;
        workflow.graphrag.relationships.update)
            _repl_schema_require_text "$method" "$params_yaml" "relationshipId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "sourceEntityId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "targetEntityId" || return 1
            _repl_schema_require_text "$method" "$params_yaml" "relationshipType" || return 1
            ;;
    esac
}

_repl_state_value() {
    local file="$1"
    local key="$2"
    [ -f "$file" ] || return 1
    grep "^${key}:" "$file" 2>/dev/null | head -1 | sed "s/^${key}:[[:space:]]*//"
}

_repl_session_state_value() {
    _repl_state_value "${REPL_INVOKE_CACHE_DIR}/session-state.yaml" "$1"
}

_repl_current_turn_value() {
    _repl_state_value "${REPL_INVOKE_CACHE_DIR}/current-turn.yaml" "$1"
}

_repl_session_meta() {
    local f="${REPL_INVOKE_CACHE_DIR}/session-state.yaml"
    [ -f "$f" ] || return 1

    local sid source_type
    sid="$(_repl_session_state_value "sessionId")"
    [ -z "$sid" ] && return 1

    source_type="$(_repl_session_state_value "sourceType")"
    if [ -z "$source_type" ]; then
        source_type="${sid%%-*}"
    fi

    printf '%s %s' "$source_type" "$sid"
}

_repl_emit_response() {
    local body="${1:-  ok: true}"
    printf 'type: response\npayload:\n%s\n' "$body"
}

_repl_response_is_error() {
    printf '%s\n' "${1:-}" | grep -q 'type: error'
}

_repl_response_has_empty_result() {
    printf '%s\n' "${1:-}" | awk '
        /^[[:space:]]*result:[[:space:]]*$/ { in_result = 1; next }
        in_result && /^[[:space:]]*$/ { next }
        in_result && /^    [^[:space:]]/ { found_child = 1; exit }
        in_result && /^[^[:space:]]/ { empty_result = 1; exit }
        END { exit((in_result && !found_child) || empty_result ? 0 : 1) }
    '
}

_repl_response_is_nonempty_success() {
    local response="${1:-}"
    ! _repl_response_is_error "$response" && ! _repl_response_has_empty_result "$response"
}

_repl_json_escape() {
    printf '%s' "${1:-}" | awk '
        BEGIN { ORS = "" }
        {
            gsub(/\\/, "\\\\")
            gsub(/"/, "\\\"")
            gsub(/\r/, "\\r")
            gsub(/\t/, "\\t")
            if (NR > 1) {
                printf "\\n"
            }
            printf "%s", $0
        }
    '
}

_repl_run_repl_with_timeout() {
    local timeout_seconds="${1:-30}"
    shift

    local pwsh_candidate=""
    pwsh_candidate="$(command -v pwsh.exe 2>/dev/null || true)"
    if [ -n "$pwsh_candidate" ] &&
       uname -s 2>/dev/null | grep -Eqi 'mingw|msys|cygwin' &&
       { [ ! -f "$pwsh_candidate" ] || ! head -c 2 "$pwsh_candidate" 2>/dev/null | grep -q '^#!'; }; then
        local input_file output_file error_file args_file input_path output_path error_path args_path executable_path
        input_file="${REPL_INVOKE_CACHE_DIR}/repl-input.$$.$RANDOM.yaml"
        output_file="${REPL_INVOKE_CACHE_DIR}/repl-output.$$.$RANDOM.yaml"
        error_file="${REPL_INVOKE_CACHE_DIR}/repl-error.$$.$RANDOM.log"
        args_file="${REPL_INVOKE_CACHE_DIR}/repl-args.$$.$RANDOM.txt"
        mkdir -p "$REPL_INVOKE_CACHE_DIR"
        cat > "$input_file"
        : > "$args_file"
        local repl_argument executable_raw executable_kind
        executable_raw="$(command -v "$1" 2>/dev/null || printf '%s' "$1")"
        executable_kind="native"
        if [ -f "$executable_raw" ] && head -c 2 "$executable_raw" 2>/dev/null | grep -q '^#!'; then
            executable_kind="bash-script"
            printf '%s\n' "$(cygpath -w "$executable_raw" 2>/dev/null || printf '%s' "$executable_raw")" >> "$args_file"
        fi
        for repl_argument in "${@:2}"; do
            printf '%s\n' "$repl_argument" >> "$args_file"
        done
        input_path="$(cygpath -w "$input_file" 2>/dev/null || printf '%s' "$input_file")"
        output_path="$(cygpath -w "$output_file" 2>/dev/null || printf '%s' "$output_file")"
        error_path="$(cygpath -w "$error_file" 2>/dev/null || printf '%s' "$error_file")"
        args_path="$(cygpath -w "$args_file" 2>/dev/null || printf '%s' "$args_file")"
        if [ "$executable_kind" = "bash-script" ]; then
            executable_path="$(command -v bash.exe 2>/dev/null || command -v bash 2>/dev/null || printf '%s' bash)"
        else
            executable_path="$executable_raw"
        fi
        executable_path="$(cygpath -w "$executable_path" 2>/dev/null || printf '%s' "$executable_path")"

        REPL_TIMEOUT_SECONDS="$timeout_seconds" \
        REPL_INPUT_PATH="$input_path" \
        REPL_OUTPUT_PATH="$output_path" \
        REPL_ERROR_PATH="$error_path" \
        REPL_FILE_NAME="$executable_path" \
        REPL_ARGS_PATH="$args_path" \
            pwsh.exe -NoLogo -NoProfile -NonInteractive -Command - <<'PWSH'
$TimeoutSeconds = [int]$env:REPL_TIMEOUT_SECONDS
$InputPath = $env:REPL_INPUT_PATH
$OutputPath = $env:REPL_OUTPUT_PATH
$ErrorPath = $env:REPL_ERROR_PATH
$FileName = $env:REPL_FILE_NAME
$ArgumentList = @()
if ($env:REPL_ARGS_PATH -and [System.IO.File]::Exists($env:REPL_ARGS_PATH)) {
    $ArgumentList = [System.IO.File]::ReadAllLines($env:REPL_ARGS_PATH)
}

$startInfo = @{
    FilePath = $FileName
    ArgumentList = $ArgumentList
    RedirectStandardInput = $InputPath
    RedirectStandardOutput = $OutputPath
    RedirectStandardError = $ErrorPath
    NoNewWindow = $true
    PassThru = $true
}

try {
    $process = Start-Process @startInfo
} catch {
    [System.IO.File]::WriteAllText($ErrorPath, $_.Exception.Message)
    exit 1
}

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    try {
        $process.Kill($true)
    } catch {
        try { $process.Kill() } catch { }
    }
    exit 124
}

exit $process.ExitCode
PWSH
        local exit_code=$?
        [ -f "$output_file" ] && cat "$output_file"
        [ -s "$error_file" ] && cat "$error_file" >&2
        rm -f "$input_file" "$output_file" "$error_file" "$args_file"
        return $exit_code
    fi

    if command -v timeout >/dev/null 2>&1; then
        timeout --kill-after=2s "$timeout_seconds" "$@"
        return $?
    fi

    "$@"
}

_repl_failsafe_plugin_name() {
    local root name
    root="$(cd "$REPL_INVOKE_SCRIPT_DIR/.." && pwd)"
    name="$(basename "$root" | sed 's/^mcpserver-//; s/-plugin$//; s/[^A-Za-z0-9._-]/-/g')"
    [ -z "$name" ] && name="${PLUGIN_AGENT_NAME:-plugin}"
    printf '%s' "$name"
}

_repl_failsafe_workspace_root() {
    local root
    root="${MCPSERVER_WORKSPACE_PATH:-${MCP_WORKSPACE_PATH:-}}"
    [ -z "$root" ] && root="$(_repl_unquote "$(_repl_session_state_value "workspacePath" 2>/dev/null || true)")"
    [ -z "$root" ] && root="$(pwd)"
    _repl_path_for_bash "$root" 2>/dev/null || printf '%s' "$root"
}

_repl_failsafe_dir() {
    printf '%s/.mcpServer/failsafe/%s' "$(_repl_failsafe_workspace_root)" "$(_repl_failsafe_plugin_name)"
}

_repl_failsafe_write() {
    # _repl_failsafe_write <method> <params_yaml> [reason]
    local method="$1"
    local params_yaml="${2:-}"
    local reason="${3:-write_ahead}"
    local dir op_id file tmp params_b64 workspace_path

    dir="$(_repl_failsafe_dir)"
    mkdir -p "$dir" || return 0

    op_id="$(_repl_now_compact)-$$-$RANDOM"
    file="${dir}/${op_id}.json"
    tmp="${file}.tmp"
    params_b64="$(printf '%s' "$params_yaml" | base64 | tr -d '\r\n')"
    workspace_path="$(_repl_failsafe_workspace_root)"

    cat > "$tmp" <<EOF
{
  "schemaVersion": 1,
  "kind": "mcpserver-plugin-failsafe",
  "plugin": "$(_repl_json_escape "$(_repl_failsafe_plugin_name)")",
  "createdAtUtc": "$(_repl_now_iso)",
  "workspacePath": "$(_repl_json_escape "$workspace_path")",
  "operations": [
    {
      "id": "$(_repl_json_escape "$op_id")",
      "method": "$(_repl_json_escape "$method")",
      "paramsYamlBase64": "$params_b64",
      "status": "pending",
      "reason": "$(_repl_json_escape "$reason")"
    }
  ]
}
EOF
    mv "$tmp" "$file" && printf '%s' "$file"
}

_repl_failsafe_clear() {
    local file="${1:-}"
    [ -n "$file" ] && [ -f "$file" ] && rm -f "$file"
}

_repl_workflow_todo_is_mutation() {
    case "${1:-}" in
        create|update|delete) return 0 ;;
        *) return 1 ;;
    esac
}

_repl_workflow_memory_is_mutation() {
    case "${1:-}" in
        workflow.memory.add|workflow.memory.update|workflow.memory.remove) return 0 ;;
        *) return 1 ;;
    esac
}

_repl_workflow_memory_append_action() {
    local method="$1"
    local params_yaml="${2:-}"
    local operation id description

    _repl_workflow_memory_is_mutation "$method" || return 0
    operation="${method##*.}"
    description="Memory ${operation}"
    id="$(_repl_yaml_get "$params_yaml" "id" 2>/dev/null || true)"
    id="$(_repl_unquote "$id")"
    case "$id" in
        MEMORY-*) description="${description} ${id}" ;;
    esac

    _repl_workflow_append_actions "actions:
  - description: ${description}
    type: edit
    status: completed" >/dev/null 2>&1 || true
}

_repl_workflow_memory() {
    local method="$1"
    local params_yaml="${2:-}"
    local response status original_timeout memory_timeout failsafe_file=""

    _repl_bootstrap_state "$(pwd)" >/dev/null 2>&1 || true
    if _repl_workflow_memory_is_mutation "$method"; then
        failsafe_file="$(_repl_failsafe_write "$method" "$params_yaml" "memory_${method##*.}")"
    fi

    original_timeout="${REPL_TIMEOUT:-}"
    memory_timeout="${REPL_MEMORY_REPL_TIMEOUT:-8}"
    export REPL_TIMEOUT="$memory_timeout"

    response="$(_repl_invoke_raw_in_workspace "$method" "$params_yaml" "compat" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        _repl_workflow_memory_append_action "$method" "$params_yaml"
        printf '%s\n' "$response"
        return 0
    fi

    original_timeout="${REPL_TIMEOUT:-}"
    export REPL_TIMEOUT="$memory_timeout"
    response="$(_repl_invoke_raw_in_workspace "$method" "$params_yaml" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi

    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        _repl_workflow_memory_append_action "$method" "$params_yaml"
        printf '%s\n' "$response"
        return 0
    fi

    original_timeout="${REPL_TIMEOUT:-}"
    export REPL_TIMEOUT="$memory_timeout"
    response="$(_repl_invoke_raw "$method" "$params_yaml" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi

    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        _repl_workflow_memory_append_action "$method" "$params_yaml"
    fi
    printf '%s\n' "$response"
    return $status
}

_repl_bool_to_enabled() {
    local value="${1:-}"
    value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]' | sed 's/^"\(.*\)"$/\1/; s/^'\''\(.*\)'\''$/\1/')"
    case "$value" in
        1|true|yes|on|enabled|enable|mcp|mcpserver) printf 'true' ;;
        0|false|no|off|disabled|disable|codex|local) printf 'false' ;;
        *) return 1 ;;
    esac
}

_repl_internal_todo_state_file() {
    printf '%s/internal-todo.yaml' "$REPL_INVOKE_CACHE_DIR"
}

_repl_internal_todo_mode_value() {
    local value mode state_file

    value="${MCPSERVER_INTERNAL_TODO:-${MCP_CODEX_INTERNAL_TODO:-${MCPSERVER_CODEX_INTERNAL_TODO:-${CODEX_MCP_TODO:-}}}}"
    if [ -n "$value" ]; then
        mode="$(_repl_bool_to_enabled "$value" 2>/dev/null || true)"
        if [ -n "$mode" ]; then
            printf '%s environment\n' "$mode"
            return 0
        fi
    fi

    state_file="$(_repl_internal_todo_state_file)"
    value="$(_repl_state_value "$state_file" "enabled" 2>/dev/null || true)"
    if [ -n "$value" ]; then
        mode="$(_repl_bool_to_enabled "$value" 2>/dev/null || true)"
        if [ -n "$mode" ]; then
            printf '%s cache\n' "$mode"
            return 0
        fi
    fi

    printf 'false default\n'
}

_repl_internal_todo_is_enabled() {
    local mode
    mode="$(_repl_internal_todo_mode_value | awk '{print $1}')"
    [ "$mode" = "true" ]
}

_repl_workflow_requirements_is_mutation() {
    case "${1:-}" in
        createFr|createFrBatch|updateFr|updateFrBatch|deleteFr|createTr|createTrBatch|updateTr|updateTrBatch|deleteTr|createTest|createTestBatch|updateTest|updateTestBatch|deleteTest|createBatch|updateBatch|createMapping|deleteMapping|generateDocument|ingestDocument|copyAcceptanceCriteriaFromTodo) return 0 ;;
        *) return 1 ;;
    esac
}

_repl_workflow_requirements_prefers_typed() {
    if [ "${MCPSERVER_REQUIREMENTS_PREFER_WORKFLOW_CREATE:-${MCP_CODEX_REQUIREMENTS_PREFER_WORKFLOW_CREATE:-}}" = "1" ]; then
        case "${1:-}" in
            createTr|createTest) return 1 ;;
        esac
    fi

    case "${1:-}" in
        createFr|createFrBatch|updateFr|updateFrBatch|createTr|createTrBatch|updateTr|updateTrBatch|createTest|createTestBatch|updateTest|updateTestBatch|createBatch|updateBatch) return 0 ;;
        *) return 1 ;;
    esac
}

_repl_url_path_segment() {
    printf '%s' "${1:-}" \
        | sed 's/%/%25/g; s/ /%20/g; s#/#%2F#g; s/?/%3F/g; s/#/%23/g; s/&/%26/g'
}

_repl_write_session_state() {
    local status="$1"
    local source_type="$2"
    local session_id="$3"
    local title="$4"
    local model="$5"
    local started="$6"
    local last_updated="$7"
    local workspace_path="$8"
    local workspace="$9"
    local base_url="${10}"

    source_type="$(_repl_unquote "$source_type")"
    session_id="$(_repl_unquote "$session_id")"
    title="$(_repl_unquote "$title")"
    model="$(_repl_unquote "$model")"
    started="$(_repl_unquote "$started")"
    last_updated="$(_repl_unquote "$last_updated")"
    workspace_path="$(_repl_unquote "$workspace_path")"
    workspace="$(_repl_unquote "$workspace")"
    base_url="$(_repl_unquote "$base_url")"

    if declare -F cache_scope_init >/dev/null 2>&1; then
        cache_scope_init "$REPL_INVOKE_PLUGIN_ROOT" "${workspace_path:-$(pwd)}"
        if [ -n "$session_id" ]; then
            cache_scope_select_session "$session_id"
        fi
    fi

    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    local session_file="${REPL_INVOKE_CACHE_DIR}/session-state.yaml"
    local tmp="${session_file}.tmp.$$"

    cat > "$tmp" <<EOF
status: ${status}
EOF

    if [ -n "$source_type" ]; then
        printf 'sourceType: %s\n' "$source_type" >> "$tmp"
    fi
    if [ -n "$session_id" ]; then
        printf 'sessionId: %s\n' "$session_id" >> "$tmp"
    fi
    if [ -n "$title" ]; then
        printf 'title: %s\n' "$title" >> "$tmp"
    fi
    if [ -n "$model" ]; then
        printf 'model: %s\n' "$model" >> "$tmp"
    fi
    if [ -n "$started" ]; then
        printf 'started: %s\n' "$started" >> "$tmp"
    fi
    if [ -n "$last_updated" ]; then
        printf 'lastUpdated: %s\n' "$last_updated" >> "$tmp"
    fi
    printf 'workspacePath: "%s"\n' "$workspace_path" >> "$tmp"
    printf 'workspace: "%s"\n' "$workspace" >> "$tmp"
    printf 'baseUrl: "%s"\n' "$base_url" >> "$tmp"
    printf 'timestamp: "%s"\n' "$(_repl_now_iso)" >> "$tmp"

    mv "$tmp" "$session_file"
}

_repl_bootstrap_state() {
    local start_dir="${1:-$(pwd)}"
    if declare -F cache_scope_init >/dev/null 2>&1; then
        cache_scope_init "$REPL_INVOKE_PLUGIN_ROOT" "$start_dir"
    fi
    local session_file="${REPL_INVOKE_CACHE_DIR}/session-state.yaml"

    if [ -f "$session_file" ]; then
        local existing_status
        existing_status="$(_repl_session_state_value "status")"
        if [ "$existing_status" = "verified" ]; then
            # Do not reuse a verified cache for a different workspace. Marker
            # state is per-workspace and the session id/source context must
            # follow the marker discovered from this start directory.
            # shellcheck source=./marker-resolver.sh
            source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
            set +e
            local marker_file marker_workspace existing_workspace
            marker_file=$(find_marker_file "$start_dir" 2>/dev/null || true)
            marker_workspace=""
            if [ -n "$marker_file" ]; then
                marker_workspace="$(parse_marker_field "$marker_file" "workspacePath" 2>/dev/null || true)"
            fi
            existing_workspace="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
            if [ -z "$marker_file" ]; then
                return 0
            fi
            if [ -n "$marker_workspace" ] && [ "$existing_workspace" = "$marker_workspace" ]; then
                return 0
            fi
        fi
    fi

    # shellcheck source=./marker-resolver.sh
    source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
    set +e

    local env_workspace_path env_workspace env_base_url env_api_key marker_file marker_workspace marker_base_url marker_api_key
    env_workspace_path="${MCPSERVER_WORKSPACE_PATH:-}"
    env_workspace="${MCPSERVER_WORKSPACE:-}"
    env_base_url="${MCPSERVER_BASE_URL:-}"
    env_api_key="${MCPSERVER_API_KEY:-}"
    if [ -n "$env_workspace_path" ] && [ -n "$env_base_url" ] && [ -n "$env_api_key" ]; then
        marker_file="$(find_marker_file "$start_dir" 2>/dev/null || true)"
        if [ -n "$marker_file" ]; then
            marker_workspace="$(_repl_unquote "$(parse_marker_field "$marker_file" "workspacePath" 2>/dev/null || true)")"
            marker_base_url="$(_repl_unquote "$(parse_marker_field "$marker_file" "baseUrl" 2>/dev/null || true)")"
            marker_api_key="$(_repl_unquote "$(parse_marker_field "$marker_file" "apiKey" 2>/dev/null || true)")"
            if [ "$marker_workspace" = "$env_workspace_path" ] &&
               [ "$marker_base_url" = "$env_base_url" ] &&
               [ "$marker_api_key" = "$env_api_key" ]; then
                [ -z "$env_workspace" ] && env_workspace="$(basename "$env_workspace_path")"
                if declare -F cache_scope_init >/dev/null 2>&1; then
                    cache_scope_init "$REPL_INVOKE_PLUGIN_ROOT" "$env_workspace_path"
                fi
                _repl_write_session_state "verified" "" "" "" "" "" "" "$env_workspace_path" "$env_workspace" "$env_base_url"
                return 0
            fi
        fi
    fi

    full_bootstrap "$start_dir" || return 1
    set +e

    local workspace_path workspace base_url
    workspace_path="${MCPSERVER_WORKSPACE_PATH:-$start_dir}"
    workspace="${MCPSERVER_WORKSPACE:-$(basename "$workspace_path")}"
    base_url="${MCPSERVER_BASE_URL:-}"

    if declare -F cache_scope_init >/dev/null 2>&1; then
        cache_scope_init "$REPL_INVOKE_PLUGIN_ROOT" "$workspace_path"
    fi

    _repl_write_session_state "verified" "" "" "" "" "" "" "$workspace_path" "$workspace" "$base_url"
}

_repl_generate_session_id() {
    local agent="$1"
    local title="$2"
    local workspace="$3"
    local slug
    slug="$(_repl_slugify "$title")"
    [ -z "$slug" ] && slug="$(_repl_slugify "$workspace")"
    [ -z "$slug" ] && slug="session"
    printf '%s-%s-%s' "$agent" "$(_repl_now_compact)" "$slug"
}

_repl_build_session_submit_params() {
    local source_type="$1"
    local session_id="$2"
    local title="$3"
    local model="$4"
    local started="$5"
    local status="$6"
    local turns_block="${7:-}"
    local turn_count="${8:-0}"

    local params="sessionLog:
  sourceType: ${source_type}
  sessionId: ${session_id}
  title: ${title}
  model: ${model}
  started: ${started}
  lastUpdated: $(_repl_now_iso)
  status: ${status}
  turnCount: !!int ${turn_count}
  totalTokens: !!int 0"

    if [ -n "$turns_block" ]; then
        params="${params}
  turns:
${turns_block}"
    else
        params="${params}
  turns: []"
    fi

    printf '%s' "$params"
}

_repl_invoke_raw() {
    local method="$1"
    local params_yaml="${2:-}"
    local request_id="req-$(_repl_now_compact)-$(printf '%04x' $RANDOM)"
    local timeout="${REPL_TIMEOUT:-30}"

    if ! command -v mcpserver-repl >/dev/null 2>&1; then
        echo "ERROR: mcpserver-repl not found on PATH" >&2
        return 1
    fi

    # Always build envelope as object and serialize to JSON (preferred for complex params).
    # This eliminates all manual YAML text construction and indentation errors.
    # The repl supports JSON request envelopes.
    local params_arg="null"
    if [ -n "$params_yaml" ]; then
        if [[ "$params_yaml" == \{* ]] || [[ "$params_yaml" == \[* ]]; then
            params_arg="$params_yaml"
        else
            # legacy YAML params: keep as string under params for now (will be phased)
            # but wrap properly
            params_arg="\"$params_yaml\""  # simplistic, better to use node for all
        fi
    fi

    # Build envelope as object in node, serialize to JSON. No text YAML.
    # params may be JSON string or legacy YAML (for legacy, we stringify the string as params for compatibility; complex paths should pass JSON).
    local params_json_for_node="null"
    if [ -n "$params_yaml" ]; then
        if [[ "$params_yaml" == \{* ]] || [[ "$params_yaml" == \[* ]]; then
            params_json_for_node="$params_yaml"
        else
            # legacy YAML string: wrap as string param (rare for complex); prefer callers pass JSON now
            params_json_for_node=$(node -e 'console.log(JSON.stringify(process.argv[1]))' "$params_yaml" 2>/dev/null || echo "null")
        fi
    fi

    local envelope_json
    envelope_json=$(node -e '
      const payload = { requestId: process.argv[1], method: process.argv[2] };
      if (process.argv[3] && process.argv[3] !== "null") {
        try { payload.params = JSON.parse(process.argv[3]); } catch(e) { payload.params = process.argv[3]; }
      }
      console.log(JSON.stringify({ type: "request", payload }));
    ' "$request_id" "$method" "$params_json_for_node" )

    local tmp_env="${REPL_INVOKE_CACHE_DIR}/envelope-${request_id}.$$.json"
    mkdir -p "$(dirname "$tmp_env")" 2>/dev/null || true
    printf '%s\n' "$envelope_json" > "$tmp_env"
    local response
    response="$(cat "$tmp_env" | _repl_run_repl_with_timeout "$timeout" mcpserver-repl --agent-stdio --agent "$AGENT_NAME" 2>/dev/null)"
    rm -f "$tmp_env" 2>/dev/null || true

    local exit_code=$?
    if [ $exit_code -eq 0 ]; then
        printf '%s\n' "$response"
        if _repl_response_is_error "$response"; then
            return 1
        fi
        return 0
    fi

    echo "ERROR: mcpserver-repl invocation failed for method ${method}" >&2
    return 1
}

_repl_invoke_with_fallback() {
    local primary="$1"
    local fallback="$2"
    local params_yaml="${3:-}"
    local response

    response="$(_repl_invoke_raw "$primary" "$params_yaml" 2>&1)"
    local status=$?
    if [ $status -eq 0 ]; then
        printf '%s\n' "$response"
        return 0
    fi

    if [ -n "$fallback" ] && printf '%s\n' "$response" | grep -q 'method_not_found'; then
        _repl_invoke_raw "$fallback" "$params_yaml"
        return $?
    fi

    printf '%s\n' "$response"
    return $status
}

_repl_path_for_bash() {
    local path_value="$(_repl_unquote "${1:-}")"
    if [ -z "$path_value" ]; then
        return 1
    fi

    if command -v cygpath >/dev/null 2>&1 && printf '%s' "$path_value" | grep -Eq '^[A-Za-z]:[\\/]'; then
        cygpath -u "$path_value"
        return $?
    fi

    printf '%s' "$path_value"
}

_repl_path_for_repl() {
    local path_value="$(_repl_unquote "${1:-}")"
    if [ -z "$path_value" ]; then
        return 1
    fi

    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$path_value"
        return $?
    fi

    printf '%s' "$path_value"
}

_repl_compat_marker_field() {
    local marker_file="$1"
    local field="$2"
    local default_value="${3:-}"

    if [ -n "$marker_file" ] && declare -F parse_marker_field >/dev/null 2>&1; then
        parse_marker_field "$marker_file" "$field" 2>/dev/null || printf '%s' "$default_value"
        return 0
    fi

    printf '%s' "$default_value"
}

_repl_compat_marker_endpoint_field() {
    local marker_file="$1"
    local field="$2"
    local default_value="${3:-}"
    local value=""

    if [ -n "$marker_file" ]; then
        value="$(sed -n '/^endpoints:/,/^[^ ]/p' "$marker_file" 2>/dev/null \
            | grep "^[[:space:]]*${field}:" \
            | head -1 \
            | tr -d '\r' \
            | sed "s/^[[:space:]]*${field}:[[:space:]]*//" \
            | sed 's/^"\(.*\)"$/\1/' \
            | sed "s/^'\(.*\)'$/\1/")"
    fi

    if [ -n "$value" ]; then
        printf '%s' "$value"
    else
        printf '%s' "$default_value"
    fi
}

_repl_create_compat_marker() {
    local workspace_path workspace_path_bash workspace base_url marker_file api_key
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    workspace="${MCPSERVER_WORKSPACE:-$(_repl_unquote "$(_repl_session_state_value "workspace")")}"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$workspace" ] && workspace="$(_repl_compat_marker_field "$marker_file" "workspace" "$(basename "$workspace_path")")"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$base_url" ] && return 1

    local port pid started marker_written server_started
    port="$(_repl_compat_marker_field "$marker_file" "port" "")"
    if [ -z "$port" ]; then
        port="$(printf '%s' "$base_url" | sed -n 's#^[a-zA-Z][a-zA-Z]*://[^:/]*:\([0-9][0-9]*\).*#\1#p')"
    fi
    [ -z "$port" ] && port="7147"
    pid="$(_repl_compat_marker_field "$marker_file" "pid" "$$")"
    started="$(_repl_compat_marker_field "$marker_file" "startedAt" "$(_repl_now_iso)")"
    marker_written="$(_repl_compat_marker_field "$marker_file" "markerWrittenAtUtc" "$started")"
    server_started="$(_repl_compat_marker_field "$marker_file" "serverStartedAtUtc" "$started")"

    local health swagger swagger_ui mcp_transport session_log session_log_dialog context_search context_pack context_sources todo repo desktop github tools workspace_endpoint server_startup marker_timestamp
    health="$(_repl_compat_marker_endpoint_field "$marker_file" "health" "/health")"
    swagger="$(_repl_compat_marker_endpoint_field "$marker_file" "swagger" "/swagger/v1/swagger.json")"
    swagger_ui="$(_repl_compat_marker_endpoint_field "$marker_file" "swaggerUi" "/swagger")"
    mcp_transport="$(_repl_compat_marker_endpoint_field "$marker_file" "mcpTransport" "/mcp-transport")"
    session_log="$(_repl_compat_marker_endpoint_field "$marker_file" "sessionLog" "/mcpserver/sessionlog")"
    session_log_dialog="$(_repl_compat_marker_endpoint_field "$marker_file" "sessionLogDialog" "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog")"
    context_search="$(_repl_compat_marker_endpoint_field "$marker_file" "contextSearch" "/mcpserver/context/search")"
    context_pack="$(_repl_compat_marker_endpoint_field "$marker_file" "contextPack" "/mcpserver/context/pack")"
    context_sources="$(_repl_compat_marker_endpoint_field "$marker_file" "contextSources" "/mcpserver/context/sources")"
    todo="$(_repl_compat_marker_endpoint_field "$marker_file" "todo" "/mcpserver/todo")"
    repo="$(_repl_compat_marker_endpoint_field "$marker_file" "repo" "/mcpserver/repo")"
    desktop="$(_repl_compat_marker_endpoint_field "$marker_file" "desktop" "/mcpserver/desktop")"
    github="$(_repl_compat_marker_endpoint_field "$marker_file" "gitHub" "/mcpserver/gh")"
    tools="$(_repl_compat_marker_endpoint_field "$marker_file" "tools" "/mcpserver/tools")"
    workspace_endpoint="$(_repl_compat_marker_endpoint_field "$marker_file" "workspace" "/mcpserver/workspace")"
    server_startup="$(_repl_compat_marker_endpoint_field "$marker_file" "serverStartupUtc" "/server-startup-utc")"
    marker_timestamp="$(_repl_compat_marker_endpoint_field "$marker_file" "markerFileTimestamp" "/marker-file-timestamp?repoPath={workspacePath}")"

    local payload signature compat_dir compat_marker
    payload="canonicalization=marker-v1"$'\n'
    payload+="port=${port}"$'\n'
    payload+="baseUrl=${base_url}"$'\n'
    payload+="apiKey=${api_key}"$'\n'
    payload+="workspace=${workspace}"$'\n'
    payload+="workspacePath=${workspace_path}"$'\n'
    payload+="pid=${pid}"$'\n'
    payload+="startedAt=${started}"$'\n'
    payload+="markerWrittenAtUtc=${marker_written}"$'\n'
    payload+="serverStartedAtUtc=${server_started}"$'\n'
    payload+="endpoints.health=${health}"$'\n'
    payload+="endpoints.swagger=${swagger}"$'\n'
    payload+="endpoints.swaggerUi=${swagger_ui}"$'\n'
    payload+="endpoints.mcpTransport=${mcp_transport}"$'\n'
    payload+="endpoints.sessionLog=${session_log}"$'\n'
    payload+="endpoints.sessionLogDialog=${session_log_dialog}"$'\n'
    payload+="endpoints.contextSearch=${context_search}"$'\n'
    payload+="endpoints.contextPack=${context_pack}"$'\n'
    payload+="endpoints.contextSources=${context_sources}"$'\n'
    payload+="endpoints.todo=${todo}"$'\n'
    payload+="endpoints.repo=${repo}"$'\n'
    payload+="endpoints.desktop=${desktop}"$'\n'
    payload+="endpoints.gitHub=${github}"$'\n'
    payload+="endpoints.tools=${tools}"$'\n'
    payload+="endpoints.workspace=${workspace_endpoint}"$'\n'
    payload+="endpoints.serverStartupUtc=${server_startup}"$'\n'
    payload+="endpoints.markerFileTimestamp=${marker_timestamp}"$'\n'

    local payload_b64 api_key_b64
    payload_b64="$(printf '%s' "$payload" | base64 | tr -d '\r\n')"
    api_key_b64="$(printf '%s' "$api_key" | base64 | tr -d '\r\n')"
    signature="$(printf '%s' "$payload" | openssl dgst -sha256 -hmac "$api_key" -hex 2>/dev/null | awk '{print toupper($NF)}')"
    export PAYLOAD_B64="$payload_b64"
    export API_KEY_B64="$api_key_b64"
    if [ -z "$signature" ] && command -v pwsh.exe >/dev/null 2>&1; then
        local hmac_script="${REPL_INVOKE_CACHE_DIR}/hmac-marker.$$.$RANDOM.ps1"
        cat > "$hmac_script" <<'PS1'
$payload = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:PAYLOAD_B64))
$apiKey = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:API_KEY_B64))
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($apiKey))
try { [Convert]::ToHexString($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload))) } finally { $hmac.Dispose() }
PS1
        signature="$(pwsh.exe -NoLogo -NoProfile -File "$(_repl_path_for_repl "$hmac_script" 2>/dev/null || printf '%s' "$hmac_script")" 2>/dev/null | tr -d '\r\n')"
        rm -f "$hmac_script"
    elif [ -z "$signature" ] && command -v powershell.exe >/dev/null 2>&1; then
        local hmac_script="${REPL_INVOKE_CACHE_DIR}/hmac-marker.$$.$RANDOM.ps1"
        cat > "$hmac_script" <<'PS1'
$payload = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:PAYLOAD_B64))
$apiKey = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:API_KEY_B64))
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($apiKey))
try { [BitConverter]::ToString($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload))).Replace('-', '') } finally { $hmac.Dispose() }
PS1
        signature="$(powershell.exe -NoLogo -NoProfile -File "$(_repl_path_for_repl "$hmac_script" 2>/dev/null || printf '%s' "$hmac_script")" 2>/dev/null | tr -d '\r\n')"
        rm -f "$hmac_script"
    fi
    unset PAYLOAD_B64 API_KEY_B64
    [ -z "$signature" ] && return 1

    compat_dir="${REPL_INVOKE_CACHE_DIR}/repl-marker.$$.$RANDOM"
    compat_marker="${compat_dir}/AGENTS-README-FIRST.yaml"
    mkdir -p "$compat_dir" || return 1
    cat > "$compat_marker" <<EOF
port: ${port}
baseUrl: ${base_url}
apiKey: ${api_key}
endpoints:
  health: ${health}
  swagger: ${swagger}
  swaggerUi: ${swagger_ui}
  mcpTransport: ${mcp_transport}
  sessionLog: ${session_log}
  sessionLogDialog: ${session_log_dialog}
  contextSearch: ${context_search}
  contextPack: ${context_pack}
  contextSources: ${context_sources}
  todo: ${todo}
  repo: ${repo}
  desktop: ${desktop}
  gitHub: ${github}
  tools: ${tools}
  workspace: ${workspace_endpoint}
  serverStartupUtc: ${server_startup}
  markerFileTimestamp: ${marker_timestamp}
workspace: ${workspace}
workspacePath: ${workspace_path}
pid: ${pid}
startedAt: ${started}
markerWrittenAtUtc: ${marker_written}
serverStartedAtUtc: ${server_started}
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: ${signature}
EOF

    printf '%s' "$compat_dir"
}

_repl_requirements_bootstrap_state() {
    local params_yaml="${1:-}"
    local start_dir
    start_dir="$(_repl_yaml_get "$params_yaml" "workspacePath")"
    start_dir="$(_repl_unquote "$start_dir")"
    [ -z "$start_dir" ] && start_dir="$(pwd)"

    local bootstrap_dir
    bootstrap_dir="$(_repl_path_for_bash "$start_dir" 2>/dev/null || printf '%s' "$start_dir")"
    _repl_bootstrap_state "$bootstrap_dir" || return 1

    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi

    local marker_file marker_workspace existing_workspace marker_workspace_name marker_base_url
    marker_file="$(find_marker_file "$bootstrap_dir" 2>/dev/null || true)"
    [ -n "$marker_file" ] || return 0
    marker_workspace="$(_repl_unquote "$(parse_marker_field "$marker_file" "workspacePath" 2>/dev/null || true)")"
    existing_workspace="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    if [ -n "$marker_workspace" ] && [ "$existing_workspace" != "$marker_workspace" ]; then
        marker_workspace_name="$(_repl_unquote "$(parse_marker_field "$marker_file" "workspace" 2>/dev/null || true)")"
        marker_base_url="$(_repl_unquote "$(parse_marker_field "$marker_file" "baseUrl" 2>/dev/null || true)")"
        [ -z "$marker_workspace_name" ] && marker_workspace_name="$(basename "$marker_workspace")"
        _repl_write_session_state "verified" "" "" "" "" "" "" "$marker_workspace" "$marker_workspace_name" "$marker_base_url"
    fi
}

_repl_invoke_raw_in_workspace() {
    local method="$1"
    local params_yaml="${2:-}"
    local marker_mode="${3:-workspace}"
    local request_id="req-$(_repl_now_compact)-$(printf '%04x' $RANDOM)"
    local timeout="${REPL_TIMEOUT:-30}"

    if ! command -v mcpserver-repl >/dev/null 2>&1; then
        echo "ERROR: mcpserver-repl not found on PATH" >&2
        return 1
    fi

    local workspace_path workspace_env_path workspace_cwd base_url cleanup_dir
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    workspace_env_path="$workspace_path"
    base_url="$(_repl_unquote "$(_repl_session_state_value "baseUrl")")"
    workspace_cwd="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || printf '%s' "$(pwd)")"
    [ -z "$workspace_cwd" ] && workspace_cwd="$(pwd)"
    cleanup_dir=""
    if [ "$marker_mode" = "compat" ]; then
        cleanup_dir="$(_repl_create_compat_marker)" || return 1
        workspace_cwd="$cleanup_dir"
        workspace_env_path=""
    fi

    # Object + JSON serialize (no manual YAML text)
    local pjson="null"
    if [ -n "$params_yaml" ]; then
        if [[ "$params_yaml" == \{* ]] || [[ "$params_yaml" == \[* ]]; then pjson="$params_yaml"; else pjson=$(node -e 'console.log(JSON.stringify(process.argv[1]))' "$params_yaml" 2>/dev/null || echo "null"); fi
    fi
    local envelope_json=$(node -e '
      const p = process.argv[3] && process.argv[3]!=="null" ? JSON.parse(process.argv[3]) : undefined;
      console.log(JSON.stringify({type:"request", payload:{requestId:process.argv[1], method:process.argv[2], ...(p?{params:p}:{}) }}));
    ' "$request_id" "$method" "$pjson")

    local response stderr_file
    stderr_file="${REPL_INVOKE_CACHE_DIR}/repl-${request_id}.$$.$RANDOM.stderr"
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    local tmp_env="${REPL_INVOKE_CACHE_DIR}/envelope-${request_id}.$$.json"
    mkdir -p "$(dirname "$tmp_env")" 2>/dev/null || true
    printf '%s\n' "$envelope_json" > "$tmp_env"
    response="$(
        cat "$tmp_env" | (
            cd "$workspace_cwd" || exit 1
            if [ -n "$workspace_env_path" ]; then
                export MCP_WORKSPACE_PATH="$workspace_env_path"
                export MCP_WORKSPACE="$workspace_env_path"
                export MCPSERVER_WORKSPACE_PATH="$workspace_env_path"
            else
                unset MCP_WORKSPACE_PATH MCP_WORKSPACE MCPSERVER_WORKSPACE_PATH
            fi
            if [ -n "$base_url" ]; then
                export MCP_SERVER_URL="$base_url"
                export MCPSERVER_BASE_URL="$base_url"
            fi
            _repl_run_repl_with_timeout "$timeout" mcpserver-repl --agent-stdio --agent "$AGENT_NAME"
        ) 2>"$stderr_file"
    )"
    rm -f "$tmp_env" 2>/dev/null || true

    local exit_code=$?
    if [ -n "$cleanup_dir" ]; then
        rm -rf "$cleanup_dir"
    fi
    if [ $exit_code -eq 0 ]; then
        printf '%s\n' "$response"
        rm -f "$stderr_file"
        if _repl_response_is_error "$response"; then
            return 1
        fi
        return 0
    fi

    if [ $exit_code -eq 124 ]; then
        echo "ERROR: mcpserver-repl timed out after ${timeout}s for method ${method}" >&2
    else
        echo "ERROR: mcpserver-repl invocation failed for method ${method} (exit ${exit_code})" >&2
    fi
    if [ -s "$stderr_file" ]; then
        sed 's/^/stderr: /' "$stderr_file" >&2
    fi
    rm -f "$stderr_file"
    return 1
}

_repl_param_text() {
    local params_yaml="$1"
    local key="$2"
    local value
    value="$(_repl_yaml_block_get "$params_yaml" "$key")"
    if [ -z "$value" ]; then
        value="$(_repl_yaml_get "$params_yaml" "$key")"
    fi
    printf '%s' "$value"
}

_repl_first_param_text() {
    local params_yaml="$1"
    shift
    local key value
    for key in "$@"; do
        value="$(_repl_param_text "$params_yaml" "$key")"
        if [ -n "$value" ]; then
            printf '%s' "$value"
            return 0
        fi
    done
    return 0
}

_repl_yaml_field() {
    local indent="$1"
    local key="$2"
    local value="${3:-}"

    if [[ "$value" == *$'\n'* ]]; then
        printf '%s%s: |\n' "$indent" "$key"
        printf '%s\n' "$value" | sed "s/^/${indent}  /"
    elif [ -n "$value" ]; then
        printf '%s%s: %s\n' "$indent" "$key" "$value"
    else
        printf '%s%s: \"\"\n' "$indent" "$key"
    fi
}

# FR-MCP-REQACPLUGIN-001 / TR-MCP-REQACPLUGIN-001: emit the acceptanceCriteria YAML block
# under the given indent when one is present in the source YAML. Supports the canonical
# server shape:
#   acceptanceCriteria:
#     - id: ac-1
#       text: ...
#       isSatisfied: true
#       evidence: ...
# Returns 0 (no output) when no block is present, so call sites can use it unconditionally.
_repl_emit_acceptance_criteria_block() {
    local indent="$1"
    local source_yaml="$2"
    local block
    block="$(_repl_list_block_get "$source_yaml" "acceptanceCriteria" 2>/dev/null || true)"
    if [ -z "$block" ]; then
        block="$(_repl_json_array_block_get "$source_yaml" "acceptanceCriteria" 2>/dev/null || true)"
    fi
    [ -z "$block" ] && return 0
    printf '%sacceptanceCriteria:\n' "$indent"
    printf '%s\n' "$block" | sed "s/^/${indent}  /"
    return 0
}

_repl_has_acceptance_criteria_block() {
    local source_yaml="$1"
    local block
    block="$(_repl_list_block_get "$source_yaml" "acceptanceCriteria" 2>/dev/null || true)"
    if [ -z "$block" ]; then
        block="$(_repl_json_array_block_get "$source_yaml" "acceptanceCriteria" 2>/dev/null || true)"
    fi
    [ -n "$block" ]
}

# FR-MCP-REQACPLUGIN-001: hydration helper for partial updates. Emits acceptanceCriteria
# from $params_yaml when present, else from $existing_yaml when present, else nothing.
# Always returns 0 so the call site under set -e / strict-mode test runners is not aborted
# by the trailing predicate's exit status.
_repl_emit_acceptance_criteria_hydrate() {
    local indent="$1"
    local params_yaml="$2"
    local existing_yaml="$3"
    if _repl_has_acceptance_criteria_block "$params_yaml"; then
        _repl_emit_acceptance_criteria_block "$indent" "$params_yaml"
    elif [ -n "$existing_yaml" ]; then
        _repl_emit_acceptance_criteria_block "$indent" "$existing_yaml"
    fi
    return 0
}

_repl_requirements_acceptance_criteria_result_ok() {
    local operation="$1"
    local params_yaml="$2"
    local response="$3"

    case "$operation" in
        createFr|updateFr|createTr|updateTr|createTest|updateTest)
            ;;
        *)
            return 0
            ;;
    esac

    if ! _repl_has_acceptance_criteria_block "$params_yaml"; then
        return 0
    fi

    if ! printf '%s\n' "$response" | grep -qE '^[[:space:]]*acceptanceCriteria:[[:space:]]*\[[[:space:]]*\][[:space:]]*$'; then
        return 0
    fi

    printf 'type: error\npayload:\n'
    printf '  code: requirements_acceptance_criteria_not_captured\n'
    printf '  message: acceptanceCriteria was supplied but the mutation response returned an empty acceptanceCriteria list.\n'
    printf '  details:\n'
    printf '    operation: %s\n' "$operation"
    return 1
}

_repl_requirement_list_field() {
    local params_yaml="$1"
    local key="$2"
    local single_key="$3"
    local indent="$4"
    local list_block single_value

    list_block="$(_repl_list_block_get "$params_yaml" "$key")"
    if [ -n "$list_block" ]; then
        printf '%s%s:\n' "$indent" "$key"
        printf '%s\n' "$list_block" | sed "s/^/${indent}  /"
        return 0
    fi

    single_value="$(_repl_yaml_get "$params_yaml" "$single_key")"
    if [ -n "$single_value" ]; then
        printf '%s%s:\n' "$indent" "$key"
        printf '%s  - %s\n' "$indent" "$single_value"
        return 0
    fi

    printf '%s%s: []\n' "$indent" "$key"
}

_repl_requirements_workflow_doc_type() {
    case "$(_repl_unquote "${1:-}")" in
        functional) printf 'fr' ;;
        technical) printf 'tr' ;;
        testing) printf 'test' ;;
        mapping) printf 'matrix' ;;
        "") printf 'all' ;;
        *) printf '%s' "$(_repl_unquote "${1:-}")" ;;
    esac
}

_repl_requirements_typed_doc_type() {
    case "$(_repl_unquote "${1:-}")" in
        fr) printf 'functional' ;;
        tr) printf 'technical' ;;
        test) printf 'testing' ;;
        matrix) printf 'mapping' ;;
        "") printf 'all' ;;
        *) printf '%s' "$(_repl_unquote "${1:-}")" ;;
    esac
}

_repl_requirements_workflow_params() {
    local operation="$1"
    local params_yaml="${2:-}"
    if [ "$operation" != "generateDocument" ]; then
        printf '%s' "$params_yaml"
        return 0
    fi

    local format doc_type
    format="$(_repl_yaml_get "$params_yaml" "format")"
    [ -z "$format" ] && format="markdown"
    doc_type="$(_repl_requirements_workflow_doc_type "$(_repl_yaml_get "$params_yaml" "docType")")"

    printf 'format: %s\n' "$format"
    printf 'docType: %s\n' "$doc_type"
}

_repl_requirements_typed_method() {
    case "$1" in
        listFr) printf 'client.Requirements.ListFrAsync' ;;
        getFr) printf 'client.Requirements.GetFrAsync' ;;
        createFr) printf 'client.Requirements.CreateFrAsync' ;;
        createFrBatch) printf 'client.Requirements.CreateFrBatchAsync' ;;
        updateFr) printf 'client.Requirements.UpdateFrAsync' ;;
        updateFrBatch) printf 'client.Requirements.UpdateFrBatchAsync' ;;
        deleteFr) printf 'client.Requirements.DeleteFrAsync' ;;
        listTr) printf 'client.Requirements.ListTrAsync' ;;
        getTr) printf 'client.Requirements.GetTrAsync' ;;
        createTr) printf 'client.Requirements.CreateTrAsync' ;;
        createTrBatch) printf 'client.Requirements.CreateTrBatchAsync' ;;
        updateTr) printf 'client.Requirements.UpdateTrAsync' ;;
        updateTrBatch) printf 'client.Requirements.UpdateTrBatchAsync' ;;
        deleteTr) printf 'client.Requirements.DeleteTrAsync' ;;
        listTest) printf 'client.Requirements.ListTestAsync' ;;
        getTest) printf 'client.Requirements.GetTestAsync' ;;
        createTest) printf 'client.Requirements.CreateTestAsync' ;;
        createTestBatch) printf 'client.Requirements.CreateTestBatchAsync' ;;
        updateTest) printf 'client.Requirements.UpdateTestAsync' ;;
        updateTestBatch) printf 'client.Requirements.UpdateTestBatchAsync' ;;
        deleteTest) printf 'client.Requirements.DeleteTestAsync' ;;
        createBatch) printf 'client.Requirements.CreateBatchAsync' ;;
        updateBatch) printf 'client.Requirements.UpdateBatchAsync' ;;
        listMappings) printf 'client.Requirements.ListMappingsAsync' ;;
        createMapping) printf 'client.Requirements.UpsertMappingAsync' ;;
        deleteMapping) printf 'client.Requirements.DeleteMappingAsync' ;;
        generateDocument) printf 'client.Requirements.GenerateAsync' ;;
        ingestDocument) printf 'client.Requirements.IngestAsync' ;;
        *) return 1 ;;
    esac
}

# FR-MCP-REQACPLUGIN-001: map an update operation to the matching typed GET client method.
_repl_requirements_update_get_method() {
    case "${1:-}" in
        updateFr) printf 'client.Requirements.GetFrAsync' ;;
        updateTr) printf 'client.Requirements.GetTrAsync' ;;
        updateTest) printf 'client.Requirements.GetTestAsync' ;;
        *) return 1 ;;
    esac
}

_repl_requirements_create_get_method() {
    case "${1:-}" in
        createFr) printf 'client.Requirements.GetFrAsync' ;;
        createTr) printf 'client.Requirements.GetTrAsync' ;;
        createTest) printf 'client.Requirements.GetTestAsync' ;;
        *) return 1 ;;
    esac
}

_repl_requirements_create_workflow_get_method() {
    case "${1:-}" in
        createFr) printf 'workflow.requirements.getFr' ;;
        createTr) printf 'workflow.requirements.getTr' ;;
        createTest) printf 'workflow.requirements.getTest' ;;
        *) return 1 ;;
    esac
}

_repl_requirements_created_response_after_empty_success() {
    local operation="$1"
    local params_yaml="${2:-}"
    local response="${3:-}"
    local id get_method workflow_get_method get_params get_response status

    _repl_response_is_error "$response" && return 1
    _repl_response_has_empty_result "$response" || return 1

    id="$(_repl_yaml_get "$params_yaml" "id")"
    [ -n "$id" ] || return 1

    workflow_get_method="$(_repl_requirements_create_workflow_get_method "$operation" 2>/dev/null || true)"
    get_method="$(_repl_requirements_create_get_method "$operation")" || return 1
    get_params="$(printf 'id: %s\n' "$id")"

    if [ -n "$workflow_get_method" ]; then
        get_response="$(_repl_invoke_raw_in_workspace "$workflow_get_method" "$get_params" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$get_response"; then
            printf '%s\n' "$get_response"
            return 0
        fi

        get_response="$(_repl_invoke_raw_in_workspace "$workflow_get_method" "$get_params" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$get_response"; then
            printf '%s\n' "$get_response"
            return 0
        fi
    fi

    get_response="$(_repl_invoke_raw_in_workspace "$get_method" "$get_params" "compat" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$get_response"; then
        printf '%s\n' "$get_response"
        return 0
    fi

    get_response="$(_repl_invoke_raw_in_workspace "$get_method" "$get_params" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$get_response"; then
        printf '%s\n' "$get_response"
        return 0
    fi

    return 1
}

# FR-MCP-REQACPLUGIN-001: map an update operation to the matching workflow GET method.
_repl_requirements_update_workflow_get_method() {
    case "${1:-}" in
        updateFr) printf 'workflow.requirements.getFr' ;;
        updateTr) printf 'workflow.requirements.getTr' ;;
        updateTest) printf 'workflow.requirements.getTest' ;;
        *) return 1 ;;
    esac
}

# FR-MCP-REQACPLUGIN-001: fetch the stored representation of a requirement so partial
# update calls can hydrate fields the caller omitted (notably acceptanceCriteria).
# Tries the workflow.requirements.get* path first, falls back to the typed client get.
_repl_requirements_existing_for_update() {
    local operation="$1"
    local id="$2"
    local get_method workflow_get_method get_params response status

    [ -n "$id" ] || return 1
    get_method="$(_repl_requirements_update_get_method "$operation")" || return 1
    get_params="$(printf 'id: %s\n' "$id")"

    workflow_get_method="$(_repl_requirements_update_workflow_get_method "$operation" 2>/dev/null || true)"
    if [ -n "$workflow_get_method" ]; then
        response="$(_repl_invoke_raw_in_workspace "$workflow_get_method" "$get_params" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            printf '%s\n' "$response"
            return 0
        fi

        response="$(_repl_invoke_raw_in_workspace "$workflow_get_method" "$get_params" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            printf '%s\n' "$response"
            return 0
        fi
    fi

    response="$(_repl_invoke_raw_in_workspace "$get_method" "$get_params" "compat" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    response="$(_repl_invoke_raw_in_workspace "$get_method" "$get_params" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    return 1
}

_repl_requirements_typed_params() {
    local operation="$1"
    local params_yaml="${2:-}"
    local id title body fr_id doc_type format content documents_block records_block source_format preferred_wiki_format
    local priority area subarea status notes test_type existing

    case "$operation" in
        listFr|listTr|listTest|listMappings)
            return 0
            ;;
        createFrBatch|updateFrBatch|createTrBatch|updateTrBatch|createTestBatch|updateTestBatch|createBatch|updateBatch)
            records_block="$(_repl_records_block_get "$params_yaml")"
            printf 'request:\n'
            if [ -n "$records_block" ]; then
                if printf '%s' "$records_block" | grep -Eq '^\[[[:space:]]*\{'; then
                    printf '  records: %s\n' "$records_block"
                else
                    printf '  records:\n'
                    printf '%s\n' "$records_block" | sed 's/^/    /'
                fi
            else
                printf '  records: []\n'
            fi
            ;;
        getFr|getTr|getTest|deleteFr|deleteTr|deleteTest)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            printf 'id: %s\n' "$id"
            ;;
        createFr)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "body")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            area="$(_repl_yaml_get "$params_yaml" "area")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            printf 'request:\n'
            _repl_yaml_field "  " "id" "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "body" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "area" "$area"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_block "  " "$params_yaml"
            ;;
        updateFr)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "body")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            status="$(_repl_yaml_get "$params_yaml" "status")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            existing="$(_repl_requirements_existing_for_update "$operation" "$id" 2>/dev/null || true)"
            [ -z "$title" ] && title="$(_repl_yaml_get "$existing" "title")"
            [ -z "$body" ] && body="$(_repl_first_param_text "$existing" "description" "body")"
            [ -z "$priority" ] && priority="$(_repl_yaml_get "$existing" "priority")"
            [ -z "$status" ] && status="$(_repl_yaml_get "$existing" "status")"
            [ -z "$notes" ] && notes="$(_repl_yaml_get "$existing" "notes")"
            printf 'id: %s\nrequest:\n' "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "body" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "status" "$status"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_hydrate "  " "$params_yaml" "$existing"
            ;;
        createTr)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "body")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            area="$(_repl_yaml_get "$params_yaml" "area")"
            subarea="$(_repl_yaml_get "$params_yaml" "subarea")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            printf 'request:\n'
            _repl_yaml_field "  " "id" "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "body" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "area" "$area"
            _repl_yaml_field "  " "subarea" "$subarea"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_block "  " "$params_yaml"
            ;;
        updateTr)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "body")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            status="$(_repl_yaml_get "$params_yaml" "status")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            existing="$(_repl_requirements_existing_for_update "$operation" "$id" 2>/dev/null || true)"
            [ -z "$title" ] && title="$(_repl_yaml_get "$existing" "title")"
            [ -z "$body" ] && body="$(_repl_first_param_text "$existing" "description" "body")"
            [ -z "$priority" ] && priority="$(_repl_yaml_get "$existing" "priority")"
            [ -z "$status" ] && status="$(_repl_yaml_get "$existing" "status")"
            [ -z "$notes" ] && notes="$(_repl_yaml_get "$existing" "notes")"
            printf 'id: %s\nrequest:\n' "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "body" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "status" "$status"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_hydrate "  " "$params_yaml" "$existing"
            ;;
        createTest)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "condition")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            area="$(_repl_yaml_get "$params_yaml" "area")"
            test_type="$(_repl_yaml_get "$params_yaml" "testType")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            printf 'request:\n'
            _repl_yaml_field "  " "id" "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "condition" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "area" "$area"
            _repl_yaml_field "  " "testType" "$test_type"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_block "  " "$params_yaml"
            ;;
        updateTest)
            id="$(_repl_yaml_get "$params_yaml" "id")"
            title="$(_repl_yaml_get "$params_yaml" "title")"
            body="$(_repl_first_param_text "$params_yaml" "description" "condition")"
            priority="$(_repl_yaml_get "$params_yaml" "priority")"
            status="$(_repl_yaml_get "$params_yaml" "status")"
            notes="$(_repl_yaml_get "$params_yaml" "notes")"
            existing="$(_repl_requirements_existing_for_update "$operation" "$id" 2>/dev/null || true)"
            [ -z "$title" ] && title="$(_repl_yaml_get "$existing" "title")"
            [ -z "$body" ] && body="$(_repl_first_param_text "$existing" "description" "condition" "body")"
            [ -z "$priority" ] && priority="$(_repl_yaml_get "$existing" "priority")"
            [ -z "$status" ] && status="$(_repl_yaml_get "$existing" "status")"
            [ -z "$notes" ] && notes="$(_repl_yaml_get "$existing" "notes")"
            printf 'id: %s\nrequest:\n' "$id"
            _repl_yaml_field "  " "title" "$title"
            _repl_yaml_field "  " "condition" "$body"
            _repl_yaml_field "  " "priority" "$priority"
            _repl_yaml_field "  " "status" "$status"
            _repl_yaml_field "  " "notes" "$notes"
            _repl_emit_acceptance_criteria_hydrate "  " "$params_yaml" "$existing"
            ;;
        createMapping)
            fr_id="$(_repl_yaml_get "$params_yaml" "frId")"
            printf 'frId: %s\nrequest:\n' "$fr_id"
            _repl_requirement_list_field "$params_yaml" "trIds" "trId" "  "
            _repl_requirement_list_field "$params_yaml" "testIds" "testId" "  "
            ;;
        deleteMapping)
            fr_id="$(_repl_yaml_get "$params_yaml" "frId")"
            printf 'frId: %s\n' "$fr_id"
            ;;
        generateDocument)
            doc_type="$(_repl_requirements_typed_doc_type "$(_repl_yaml_get "$params_yaml" "docType")")"
            format="$(_repl_yaml_get "$params_yaml" "format")"
            [ -z "$format" ] && format="markdown"
            printf 'doc: %s\n' "$doc_type"
            printf 'format: %s\n' "$format"
            ;;
        ingestDocument)
            content="$(_repl_param_text "$params_yaml" "content")"
            documents_block="$(_repl_list_block_get "$params_yaml" "documents")"
            source_format="$(_repl_yaml_get "$params_yaml" "sourceFormat")"
            preferred_wiki_format="$(_repl_yaml_get "$params_yaml" "preferredWikiFormat")"
            printf 'request:\n'
            if [ -n "$documents_block" ]; then
                [ -z "$source_format" ] && source_format="wiki"
                _repl_yaml_field "  " "sourceFormat" "$source_format"
                if [ -n "$preferred_wiki_format" ]; then
                    _repl_yaml_field "  " "preferredWikiFormat" "$preferred_wiki_format"
                fi
                printf '  documents:\n'
                printf '%s\n' "$documents_block" | sed 's/^/    /'
            else
                _repl_yaml_field "  " "functionalMarkdown" "$content"
                _repl_yaml_field "  " "technicalMarkdown" "$content"
                _repl_yaml_field "  " "testingMarkdown" "$content"
                _repl_yaml_field "  " "mappingMarkdown" "$content"
            fi
            ;;
        *)
            return 1
            ;;
    esac
}

_repl_requirements_normalize_generate_response() {
    local response="${1:-}"
    local request_id content_type file_name format doc_type generated_at decoded content_base64

    if ! printf '%s\n' "$response" | awk '
        /^[[:space:]]*content:[[:space:]]*$/ { in_content = 1; next }
        in_content && /^[[:space:]]*-[[:space:]]*[0-9]+[[:space:]]*$/ { found = 1; exit }
        in_content && /^[^[:space:]]|^[[:space:]]*[[:alpha:]_][[:alnum:]_]*:/ { in_content = 0 }
        END { exit(found ? 0 : 1) }
    '; then
        printf '%s\n' "$response"
        return 0
    fi

    request_id="$(printf '%s\n' "$response" | grep '^[[:space:]]*requestId:' | head -1 | sed 's/^[[:space:]]*requestId:[[:space:]]*//')"
    content_type="$(printf '%s\n' "$response" | grep '^[[:space:]]*contentType:' | head -1 | sed 's/^[[:space:]]*contentType:[[:space:]]*//')"
    file_name="$(printf '%s\n' "$response" | grep '^[[:space:]]*fileName:' | head -1 | sed 's/^[[:space:]]*fileName:[[:space:]]*//')"
    format="$(printf '%s\n' "$response" | grep '^[[:space:]]*format:' | head -1 | sed 's/^[[:space:]]*format:[[:space:]]*//')"
    doc_type="$(printf '%s\n' "$response" | grep '^[[:space:]]*docType:' | head -1 | sed 's/^[[:space:]]*docType:[[:space:]]*//')"
    generated_at="$(printf '%s\n' "$response" | grep '^[[:space:]]*generatedAt:' | head -1 | sed 's/^[[:space:]]*generatedAt:[[:space:]]*//')"
    [ -z "$content_type" ] && content_type="text/markdown"

    if printf '%s\n' "$content_type" | grep -qi 'zip' || printf '%s\n' "$file_name" | grep -qi '\.zip$'; then
        content_base64="$(printf '%s\n' "$response" | awk '
            /^[[:space:]]*content:[[:space:]]*$/ { in_content = 1; next }
            in_content && /^[[:space:]]*-[[:space:]]*[0-9]+[[:space:]]*$/ {
                value = $0
                sub(/^[[:space:]]*-[[:space:]]*/, "", value)
                sub(/[[:space:]]*$/, "", value)
                print value
                next
            }
            in_content && /^[^[:space:]]|^[[:space:]]*[[:alpha:]_][[:alnum:]_]*:/ { in_content = 0 }
        ' | while IFS= read -r byte_value; do
            printf "\\$(printf '%03o' "$byte_value")"
        done | base64 | tr -d '\r\n')"

        [ -z "$file_name" ] && file_name="requirements-documents.zip"
        [ -z "$format" ] && format="markdown"
        [ -z "$doc_type" ] && doc_type="all"
        [ -z "$generated_at" ] && generated_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

        printf 'type: result\npayload:\n'
        if [ -n "$request_id" ]; then
            printf '  requestId: %s\n' "$request_id"
        fi
        printf '  result:\n'
        printf '    contentBase64: %s\n' "$content_base64"
        printf '    contentType: %s\n' "$content_type"
        printf '    fileName: %s\n' "$file_name"
        printf '    format: %s\n' "$format"
        printf '    docType: %s\n' "$doc_type"
        printf '    generatedAt: %s\n' "$generated_at"
        return 0
    fi

    decoded="$(printf '%s\n' "$response" | awk '
        /^[[:space:]]*content:[[:space:]]*$/ { in_content = 1; next }
        in_content && /^[[:space:]]*-[[:space:]]*[0-9]+[[:space:]]*$/ {
            value = $0
            sub(/^[[:space:]]*-[[:space:]]*/, "", value)
            sub(/[[:space:]]*$/, "", value)
            print value
            next
        }
        in_content && /^[^[:space:]]|^[[:space:]]*[[:alpha:]_][[:alnum:]_]*:/ { in_content = 0 }
    ' | while IFS= read -r byte_value; do
        printf "\\$(printf '%03o' "$byte_value")"
    done)"

    printf 'type: result\npayload:\n'
    if [ -n "$request_id" ]; then
        printf '  requestId: %s\n' "$request_id"
    fi
    printf '  result:\n'
    printf '    content: |\n'
    printf '%s\n' "$decoded" | sed 's/^/      /'
    printf '    contentType: %s\n' "$content_type"
    if [ -n "$format" ]; then
        printf '    format: %s\n' "$format"
    fi
    if [ -n "$doc_type" ]; then
        printf '    docType: %s\n' "$doc_type"
    fi
}

_repl_requirements_generate_response_has_content_base64() {
    printf '%s\n' "${1:-}" | grep -q '^[[:space:]]*contentBase64:[[:space:]]*[^[:space:]]'
}

_repl_requirements_generate_response_is_zip() {
    local response="${1:-}"
    _repl_requirements_generate_response_has_content_base64 "$response" && {
        printf '%s\n' "$response" | grep -qi '^[[:space:]]*contentType:[[:space:]]*.*zip' ||
            printf '%s\n' "$response" | grep -qi '^[[:space:]]*fileName:[[:space:]]*.*\.zip'
    }
}

_repl_requirements_emit_generate_response() {
    local response="${1:-}"
    local params_yaml="${2:-}"
    local format normalized
    format="$(_repl_yaml_get "$params_yaml" "format")"
    [ -z "$format" ] && format="markdown"
    normalized="$(_repl_requirements_normalize_generate_response "$response")"
    if [ "$format" = "wiki" ]; then
        _repl_requirements_generate_response_is_zip "$normalized" || return 1
    fi
    printf '%s\n' "$normalized"
}

_repl_requirements_generate_http_fallback() {
    local params_yaml="${1:-}"
    local format doc_type workspace_path workspace_path_bash base_url marker_file api_key

    format="$(_repl_yaml_get "$params_yaml" "format")"
    [ -z "$format" ] && format="markdown"
    [ "$format" = "wiki" ] || return 1

    if ! command -v curl >/dev/null 2>&1 || ! command -v base64 >/dev/null 2>&1; then
        return 1
    fi

    doc_type="$(_repl_requirements_typed_doc_type "$(_repl_yaml_get "$params_yaml" "docType")")"
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    local tmp_body tmp_headers url content_type content_base64 curl_status
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/requirements-generate.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/requirements-generate.$$.$RANDOM.headers"
    url="${base_url%/}/mcpserver/requirements/generate?doc=${doc_type}&format=${format}"

    curl -sSL \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        "$url" >/dev/null 2>&1
    curl_status=$?
    if [ $curl_status -ne 0 ]; then
        if [ -s "$tmp_body" ]; then
            printf 'type: error\npayload:\n'
            printf '  code: http_error\n'
            printf '  message: requirements generate HTTP fallback failed with curl exit %s\n' "$curl_status"
            printf '  details:\n'
            printf '    responseBody: |\n'
            sed 's/^/      /' "$tmp_body"
            printf '\n'
        fi
        rm -f "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    local http_status
    http_status="$(awk 'toupper($0) ~ /^HTTP\// { code = $2 } END { print code }' "$tmp_headers" 2>/dev/null)"
    if [ -n "$http_status" ] && [ "$http_status" -ge 400 ] 2>/dev/null; then
        printf 'type: error\npayload:\n'
        printf '  code: http_error\n'
        printf '  message: requirements generate HTTP fallback returned HTTP %s\n' "$http_status"
        printf '  details:\n'
        printf '    responseBody: |\n'
        sed 's/^/      /' "$tmp_body"
        printf '\n'
        rm -f "$tmp_body" "$tmp_headers"
        return 1
    fi

    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/zip"
    content_base64="$(base64 < "$tmp_body" | tr -d '\r\n')"
    rm -f "$tmp_body" "$tmp_headers"

    printf 'type: result\npayload:\n'
    printf '  result:\n'
    printf '    contentBase64: %s\n' "$content_base64"
    printf '    contentType: %s\n' "$content_type"
    if printf '%s\n' "$content_type" | grep -qi 'zip'; then
        printf '    fileName: requirements-wiki-documents.zip\n'
    fi
    printf '    format: %s\n' "$format"
    printf '    docType: %s\n' "$doc_type"
    printf '    generatedAt: %s\n' "$(_repl_now_iso)"
}

_repl_sessionlog_query_http_fallback() {
    local params_yaml="${1:-}"
    local workspace_path workspace_path_bash base_url marker_file api_key
    local requested_workspace_path
    local agent model text from to limit offset

    if ! command -v curl >/dev/null 2>&1; then
        return 1
    fi

    requested_workspace_path="$(_repl_yaml_get "$params_yaml" "workspacePath" 2>/dev/null || true)"
    requested_workspace_path="$(_repl_unquote "$requested_workspace_path")"
    if [ -n "$requested_workspace_path" ]; then
        workspace_path="$requested_workspace_path"
    else
        workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    fi
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    if [ -n "$requested_workspace_path" ]; then
        api_key="$(_repl_compat_marker_field "$marker_file" "apiKey" "")"
        base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
        [ -z "$api_key" ] && api_key="${MCPSERVER_API_KEY:-}"
        [ -z "$base_url" ] && base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"
    else
        api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
        [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
        [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    fi
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    agent="$(_repl_yaml_get "$params_yaml" "agent" 2>/dev/null || true)"
    model="$(_repl_yaml_get "$params_yaml" "model" 2>/dev/null || true)"
    text="$(_repl_yaml_get "$params_yaml" "text" 2>/dev/null || true)"
    from="$(_repl_yaml_get "$params_yaml" "from" 2>/dev/null || true)"
    to="$(_repl_yaml_get "$params_yaml" "to" 2>/dev/null || true)"
    limit="$(_repl_yaml_get "$params_yaml" "limit" 2>/dev/null || true)"
    offset="$(_repl_yaml_get "$params_yaml" "offset" 2>/dev/null || true)"

    local tmp_body tmp_headers curl_status
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/sessionlog-query.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/sessionlog-query.$$.$RANDOM.headers"

    curl -fsSL \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        --get \
        ${agent:+--data-urlencode "agent=${agent}"} \
        ${model:+--data-urlencode "model=${model}"} \
        ${text:+--data-urlencode "text=${text}"} \
        ${from:+--data-urlencode "from=${from}"} \
        ${to:+--data-urlencode "to=${to}"} \
        ${limit:+--data-urlencode "limit=${limit}"} \
        ${offset:+--data-urlencode "offset=${offset}"} \
        "${base_url%/}/mcpserver/sessionlog" >/dev/null 2>&1
    curl_status=$?
    if [ $curl_status -ne 0 ]; then
        rm -f "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    local content_type
    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/json"
    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    printf '  contentType: %s\n' "$content_type"
    rm -f "$tmp_body" "$tmp_headers"
}

_repl_sessionlog_submit_http_fallback() {
    local source_type="$1"
    local session_id="$2"
    local title="$3"
    local model="$4"
    local started="$5"
    local status="$6"
    local has_turn="${7:-0}"
    local turn_request_id="${8:-}"
    local turn_title="${9:-}"
    local turn_status="${10:-in_progress}"
    local response_text="${11:-}"
    local actions_block="${12:-}"

    if ! command -v curl >/dev/null 2>&1 || ! command -v node >/dev/null 2>&1; then
        return 1
    fi

    local workspace_path workspace_path_bash base_url marker_file api_key
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    local query_text turn_timestamp
    query_text="$(_repl_yaml_block_get "$(cat "${REPL_INVOKE_CACHE_DIR}/current-turn.yaml" 2>/dev/null)" "queryText")"
    [ -z "$query_text" ] && query_text="$turn_title"
    turn_timestamp="$(_repl_current_turn_value "openedAt" 2>/dev/null || true)"
    [ -z "$turn_timestamp" ] && turn_timestamp="$(_repl_now_iso)"

    local tmp_existing tmp_incoming tmp_merged tmp_body tmp_headers timeout_seconds
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_existing="${REPL_INVOKE_CACHE_DIR}/sessionlog-existing.$$.$RANDOM.json"
    tmp_incoming="${REPL_INVOKE_CACHE_DIR}/sessionlog-incoming.$$.$RANDOM.json"
    tmp_merged="${REPL_INVOKE_CACHE_DIR}/sessionlog-merged.$$.$RANDOM.json"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/sessionlog-submit.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/sessionlog-submit.$$.$RANDOM.headers"
    timeout_seconds="${REPL_SESSIONLOG_HTTP_TIMEOUT:-20}"

    curl -sS \
        --max-time "$timeout_seconds" \
        -o "$tmp_existing" \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        --get \
        --data-urlencode "agent=${source_type}" \
        --data-urlencode "limit=1000" \
        "${base_url%/}/mcpserver/sessionlog" >/dev/null 2>&1 || {
            rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
            return 1
        }

    SESSION_SOURCE_TYPE="$source_type" \
    SESSION_ID="$session_id" \
    SESSION_TITLE="$title" \
    SESSION_MODEL="$model" \
    SESSION_STARTED="$started" \
    SESSION_LAST_UPDATED="$(_repl_now_iso)" \
    SESSION_STATUS="$status" \
    SESSION_HAS_TURN="$has_turn" \
    SESSION_TURN_REQUEST_ID="$turn_request_id" \
    SESSION_TURN_TIMESTAMP="$turn_timestamp" \
    SESSION_QUERY_TITLE="$turn_title" \
    SESSION_TURN_STATUS="$turn_status" \
    SESSION_QUERY_TEXT_B64="$(printf '%s' "$query_text" | base64 | tr -d '\r\n')" \
    SESSION_RESPONSE_B64="$(printf '%s' "$response_text" | base64 | tr -d '\r\n')" \
    SESSION_ACTIONS_B64="$(printf '%s' "$actions_block" | base64 | tr -d '\r\n')" \
        node "${REPL_INVOKE_SCRIPT_DIR}/sessionlog-submit-body.js" build > "$tmp_incoming" || {
            rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
            return 1
        }

    node "${REPL_INVOKE_SCRIPT_DIR}/sessionlog-submit-body.js" merge "$tmp_existing" "$tmp_incoming" > "$tmp_merged" || {
        rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
        return 1
    }

    curl -sS \
        --max-time "$timeout_seconds" \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -X POST \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        -H "Content-Type: application/json" \
        --data-binary "@${tmp_merged}" \
        "${base_url%/}/mcpserver/sessionlog" >/dev/null 2>&1
    local curl_status=$?
    if [ $curl_status -ne 0 ]; then
        if [ -s "$tmp_body" ]; then
            printf 'type: error\npayload:\n'
            printf '  code: http_error\n'
            printf '  message: session log submit HTTP fallback failed with curl exit %s\n' "$curl_status"
            printf '  details:\n'
            printf '    responseBody: |\n'
            sed 's/^/      /' "$tmp_body"
            printf '\n'
        fi
        rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    local http_status
    http_status="$(awk 'toupper($0) ~ /^HTTP\// { code = $2 } END { print code }' "$tmp_headers" 2>/dev/null)"
    if [ -n "$http_status" ] && [ "$http_status" -ge 400 ] 2>/dev/null; then
        printf 'type: error\npayload:\n'
        printf '  code: http_error\n'
        printf '  message: session log submit HTTP fallback returned HTTP %s\n' "$http_status"
        printf '  details:\n'
        printf '    responseBody: |\n'
        sed 's/^/      /' "$tmp_body"
        printf '\n'
        rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
        return 1
    fi

    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    rm -f "$tmp_existing" "$tmp_incoming" "$tmp_merged" "$tmp_body" "$tmp_headers"
    return 0
}

_repl_requirements_list_http_fallback() {
    local operation="$1"
    local workspace_path workspace_path_bash base_url marker_file api_key route

    if ! command -v curl >/dev/null 2>&1; then
        return 1
    fi

    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    case "$operation" in
        listFr) route="mcpserver/requirements/fr" ;;
        listTr) route="mcpserver/requirements/tr" ;;
        listTest) route="mcpserver/requirements/test" ;;
        listMappings) route="mcpserver/requirements/mapping" ;;
        *) return 1 ;;
    esac

    local tmp_body tmp_headers curl_status content_type
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/requirements-list.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/requirements-list.$$.$RANDOM.headers"

    curl -fsSL \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        "${base_url%/}/${route}" >/dev/null 2>&1
    curl_status=$?
    if [ $curl_status -ne 0 ]; then
        rm -f "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/json"
    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    printf '  contentType: %s\n' "$content_type"
    rm -f "$tmp_body" "$tmp_headers"
}

_repl_requirements_copy_acceptance_http_fallback() {
    local params_yaml="${1:-}"
    local workspace_path workspace_path_bash base_url marker_file api_key kind id todo_id route body

    if ! command -v curl >/dev/null 2>&1; then
        return 1
    fi

    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    kind="$(_repl_unquote "$(_repl_yaml_get "$params_yaml" "kind")")"
    id="$(_repl_unquote "$(_repl_yaml_get "$params_yaml" "id")")"
    todo_id="$(_repl_unquote "$(_repl_yaml_get "$params_yaml" "todoId")")"
    [ -z "$kind" ] && return 1
    [ -z "$id" ] && return 1
    [ -z "$todo_id" ] && return 1

    case "$kind" in
        fr|functional) kind="fr" ;;
        tr|technical) kind="tr" ;;
        test|testing) kind="test" ;;
        *) return 1 ;;
    esac

    route="mcpserver/requirements/${kind}/$(_repl_url_path_segment "$id")/acceptance-criteria/copy-from-todo"
    body="$(printf '{"todoId":"%s"}' "$(_repl_json_escape "$todo_id")")"

    local tmp_body tmp_headers curl_status content_type
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/requirements-copy-ac.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/requirements-copy-ac.$$.$RANDOM.headers"

    curl -fsSL \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -X POST \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        -H "Content-Type: application/json" \
        --data-binary "$body" \
        "${base_url%/}/${route}" >/dev/null 2>&1
    curl_status=$?
    if [ $curl_status -ne 0 ]; then
        rm -f "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/json"
    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    printf '  contentType: %s\n' "$content_type"
    rm -f "$tmp_body" "$tmp_headers"
}

_repl_todo_json_body() {
    local operation="$1"
    local params_yaml="${2:-}"
    local body="" sep="" value

    if printf '%s' "$params_yaml" | grep -q '^[[:space:]]*{' && command -v node >/dev/null 2>&1; then
        printf '%s' "$params_yaml" | node -e '
const fs = require("fs");
const input = fs.readFileSync(0, "utf8").trim();
let root = {};
try { root = JSON.parse(input); } catch { root = {}; }
let body = root && root.request && typeof root.request === "object" && !Array.isArray(root.request)
  ? { ...root.request }
  : { ...root };
if (body.section) {
  const normalized = String(body.section).toLowerCase();
  body.section = normalized === "ui" ? "UI" : "Backlog";
}
process.stdout.write(JSON.stringify(body));
' 2>/dev/null && return 0
    fi

    _repl_todo_json_add_raw() {
        local key="$1"
        local json_value="$2"
        [ -z "$json_value" ] && return 0
        body="${body}${sep}\"${key}\":${json_value}"
        sep=","
    }

    _repl_todo_json_add_string() {
        local key="$1"
        value="$(_repl_param_text "$params_yaml" "$key")"
        [ -z "$value" ] && return 0
        value="$(_repl_unquote "$value")"
        _repl_todo_json_add_raw "$key" "\"$(_repl_json_escape "$value")\""
    }

    _repl_todo_json_add_bool() {
        local key="$1" normalized
        value="$(_repl_yaml_get_root "$params_yaml" "$key" 2>/dev/null || true)"
        [ -z "$value" ] && return 0
        value="$(_repl_unquote "$value")"
        normalized="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
        case "$normalized" in
            true|yes|1) _repl_todo_json_add_raw "$key" "true" ;;
            false|no|0) _repl_todo_json_add_raw "$key" "false" ;;
        esac
    }

    _repl_todo_json_add_section() {
        local section normalized
        section="$(_repl_yaml_get "$params_yaml" "section" 2>/dev/null || true)"
        [ -z "$section" ] && return 0
        section="$(_repl_unquote "$section")"
        normalized="$(printf '%s' "$section" | tr '[:upper:]' '[:lower:]')"
        case "$normalized" in
            backlog) section="Backlog" ;;
            ui) section="UI" ;;
            *) section="Backlog" ;;
        esac
        _repl_todo_json_add_raw "section" "\"$(_repl_json_escape "$section")\""
    }

    _repl_todo_json_add_array() {
        local key="$1" list_block single item items="" item_sep=""
        list_block="$(_repl_list_block_get "$params_yaml" "$key")"
        if [ -n "$list_block" ]; then
            while IFS= read -r item; do
                case "$item" in
                    *-*)
                        item="$(printf '%s' "$item" | sed 's/^[[:space:]]*-[[:space:]]*//')"
                        [ -z "$item" ] && continue
                        item="$(_repl_unquote "$item")"
                        items="${items}${item_sep}\"$(_repl_json_escape "$item")\""
                        item_sep=","
                        ;;
                esac
            done <<EOF
${list_block}
EOF
        else
            single="$(_repl_param_text "$params_yaml" "$key")"
            if [ -n "$single" ]; then
                single="$(_repl_unquote "$single")"
                items="\"$(_repl_json_escape "$single")\""
            fi
        fi
        [ -z "$items" ] && return 0
        _repl_todo_json_add_raw "$key" "[${items}]"
    }

    _repl_todo_json_add_tasks() {
        local key="$1" list_block single item items="" item_sep="" current_task="" current_done="false" trimmed

        _repl_todo_json_flush_task() {
            [ -z "$current_task" ] && return 0
            items="${items}${item_sep}{\"task\":\"$(_repl_json_escape "$current_task")\",\"done\":${current_done}}"
            item_sep=","
            current_task=""
            current_done="false"
        }

        list_block="$(_repl_list_block_get "$params_yaml" "$key")"
        if [ -n "$list_block" ]; then
            while IFS= read -r item; do
                trimmed="$(printf '%s' "$item" | sed 's/^[[:space:]]*//; s/[[:space:]]*$//')"
                case "$trimmed" in
                    -\ task:*)
                        _repl_todo_json_flush_task
                        current_task="$(_repl_unquote "${trimmed#- task:}")"
                        current_task="$(printf '%s' "$current_task" | sed 's/^[[:space:]]*//')"
                        ;;
                    task:*)
                        current_task="$(_repl_unquote "${trimmed#task:}")"
                        current_task="$(printf '%s' "$current_task" | sed 's/^[[:space:]]*//')"
                        ;;
                    done:*)
                        current_done="$(_repl_unquote "${trimmed#done:}")"
                        current_done="$(printf '%s' "$current_done" | sed 's/^[[:space:]]*//' | tr '[:upper:]' '[:lower:]')"
                        case "$current_done" in
                            true|yes|1) current_done="true" ;;
                            *) current_done="false" ;;
                        esac
                        ;;
                    -*)
                        _repl_todo_json_flush_task
                        current_task="$(_repl_unquote "${trimmed#-}")"
                        current_task="$(printf '%s' "$current_task" | sed 's/^[[:space:]]*//')"
                        ;;
                esac
            done <<EOF
${list_block}
EOF
            _repl_todo_json_flush_task
        else
            single="$(_repl_param_text "$params_yaml" "$key")"
            if [ -n "$single" ]; then
                single="$(_repl_unquote "$single")"
                items="{\"task\":\"$(_repl_json_escape "$single")\",\"done\":false}"
            fi
        fi
        [ -z "$items" ] && return 0
        _repl_todo_json_add_raw "$key" "[${items}]"
    }

    [ "$operation" = "create" ] && _repl_todo_json_add_string "id"
    _repl_todo_json_add_string "title"
    _repl_todo_json_add_string "priority"
    _repl_todo_json_add_section
    _repl_todo_json_add_string "estimate"
    _repl_todo_json_add_string "note"
    _repl_todo_json_add_string "completedDate"
    _repl_todo_json_add_string "doneSummary"
    _repl_todo_json_add_string "remaining"
    _repl_todo_json_add_string "reference"
    _repl_todo_json_add_string "phase"
    _repl_todo_json_add_bool "done"
    _repl_todo_json_add_array "description"
    _repl_todo_json_add_array "technicalDetails"
    _repl_todo_json_add_tasks "implementationTasks"
    _repl_todo_json_add_array "dependsOn"
    _repl_todo_json_add_array "functionalRequirements"
    _repl_todo_json_add_array "technicalRequirements"

    printf '{%s}' "$body"
}

_repl_todo_has_request_wrapper() {
    printf '%s\n' "${1:-}" | grep -Eq '^[[:space:]]*request:[[:space:]]*$'
}

_repl_todo_typed_params() {
    local operation="$1"
    local params_yaml="${2:-}"
    local todo_id

    case "$operation" in
        create)
            if _repl_todo_has_request_wrapper "$params_yaml"; then
                printf '%s\n' "$params_yaml"
            else
                printf 'request:\n'
                printf '%s\n' "$params_yaml" | sed 's/^/  /'
            fi
            ;;
        update)
            if _repl_todo_has_request_wrapper "$params_yaml"; then
                printf '%s\n' "$params_yaml"
            else
                todo_id="$(_repl_yaml_get "$params_yaml" "id" 2>/dev/null || true)"
                todo_id="$(_repl_unquote "$todo_id")"
                [ -n "$todo_id" ] && printf 'id: %s\n' "$todo_id"
                printf 'request:\n'
                printf '%s\n' "$params_yaml" | awk '
                    /^[[:space:]]*id:[[:space:]]*/ { next }
                    { print "  " $0 }
                '
            fi
            ;;
        *)
            printf '%s\n' "$params_yaml"
            ;;
    esac
}

_repl_todo_http_fallback() {
    local operation="$1"
    local params_yaml="${2:-}"
    local workspace_path workspace_path_bash base_url marker_file api_key

    if ! command -v curl >/dev/null 2>&1; then
        return 1
    fi

    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    local tmp_body tmp_headers tmp_request curl_status content_type todo_id todo_id_path
    local keyword priority section done status_value title
    local -a curl_args
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/todo-${operation}.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/todo-${operation}.$$.$RANDOM.headers"
    tmp_request=""

    curl_args=(-sSL -D "$tmp_headers" -o "$tmp_body" -H "X-Api-Key: ${api_key}" -H "X-Workspace-Path: ${workspace_path}")

    case "$operation" in
        query)
            keyword="$(_repl_yaml_get "$params_yaml" "keyword" 2>/dev/null || true)"
            title="$(_repl_yaml_get "$params_yaml" "title" 2>/dev/null || true)"
            priority="$(_repl_yaml_get "$params_yaml" "priority" 2>/dev/null || true)"
            section="$(_repl_yaml_get "$params_yaml" "section" 2>/dev/null || true)"
            todo_id="$(_repl_yaml_get "$params_yaml" "id" 2>/dev/null || true)"
            done="$(_repl_yaml_get "$params_yaml" "done" 2>/dev/null || true)"
            status_value="$(_repl_yaml_get "$params_yaml" "status" 2>/dev/null || true)"
            [ -z "$keyword" ] && keyword="$title"
            if [ -z "$done" ] && [ -n "$status_value" ]; then
                case "$(printf '%s' "$(_repl_unquote "$status_value")" | tr '[:upper:]' '[:lower:]')" in
                    open|active|pending|in_progress|in-progress) done="false" ;;
                    closed|complete|completed|done) done="true" ;;
                esac
            fi
            keyword="$(_repl_unquote "$keyword")"
            priority="$(_repl_unquote "$priority")"
            section="$(_repl_unquote "$section")"
            todo_id="$(_repl_unquote "$todo_id")"
            done="$(_repl_unquote "$done")"
            curl_args+=(--get)
            [ -n "$keyword" ] && curl_args+=(--data-urlencode "keyword=${keyword}")
            [ -n "$priority" ] && curl_args+=(--data-urlencode "priority=${priority}")
            [ -n "$section" ] && curl_args+=(--data-urlencode "section=${section}")
            [ -n "$todo_id" ] && curl_args+=(--data-urlencode "id=${todo_id}")
            [ -n "$done" ] && curl_args+=(--data-urlencode "done=${done}")
            curl_args+=("${base_url%/}/mcpserver/todo")
            ;;
        get|delete|update|analyzeRequirements)
            todo_id="$(_repl_yaml_get "$params_yaml" "id" 2>/dev/null || true)"
            todo_id="$(_repl_unquote "$todo_id")"
            if [ -z "$todo_id" ]; then
                rm -f "$tmp_body" "$tmp_headers" "$tmp_request"
                return 1
            fi
            todo_id_path="$(_repl_url_path_segment "$todo_id")"
            case "$operation" in
                get)
                    curl_args+=("${base_url%/}/mcpserver/todo/${todo_id_path}")
                    ;;
                delete)
                    curl_args+=(-X DELETE "${base_url%/}/mcpserver/todo/${todo_id_path}")
                    ;;
                update)
                    tmp_request="${REPL_INVOKE_CACHE_DIR}/todo-${operation}.$$.$RANDOM.json"
                    _repl_todo_json_body "$operation" "$params_yaml" > "$tmp_request"
                    curl_args+=(-H "Content-Type: application/json" -X PUT --data-binary "@${tmp_request}" "${base_url%/}/mcpserver/todo/${todo_id_path}")
                    ;;
                analyzeRequirements)
                    tmp_request="${REPL_INVOKE_CACHE_DIR}/todo-${operation}.$$.$RANDOM.json"
                    _repl_todo_json_body "$operation" "$params_yaml" > "$tmp_request"
                    curl_args+=(-H "Content-Type: application/json" -X POST --data-binary "@${tmp_request}" "${base_url%/}/mcpserver/todo/${todo_id_path}/requirements")
                    ;;
            esac
            ;;
        create)
            tmp_request="${REPL_INVOKE_CACHE_DIR}/todo-${operation}.$$.$RANDOM.json"
            _repl_todo_json_body "$operation" "$params_yaml" > "$tmp_request"
            curl_args+=(-H "Content-Type: application/json" -X POST --data-binary "@${tmp_request}" "${base_url%/}/mcpserver/todo")
            ;;
        *)
            rm -f "$tmp_body" "$tmp_headers" "$tmp_request"
            return 1
            ;;
    esac

    curl "${curl_args[@]}" >/dev/null 2>&1
    curl_status=$?
    if [ $curl_status -ne 0 ]; then
        if [ -s "$tmp_body" ]; then
            printf 'type: error\npayload:\n'
            printf '  code: http_error\n'
            printf '  message: TODO HTTP fallback failed with curl exit %s\n' "$curl_status"
            printf '  details:\n'
            printf '    operation: %s\n' "$operation"
            printf '    responseBody: |\n'
            sed 's/^/      /' "$tmp_body"
            printf '\n'
        fi
        rm -f "$tmp_body" "$tmp_headers" "$tmp_request"
        return $curl_status
    fi

    local http_status
    http_status="$(awk 'toupper($0) ~ /^HTTP\// { code = $2 } END { print code }' "$tmp_headers" 2>/dev/null)"
    if [ -n "$http_status" ] && [ "$http_status" -ge 400 ] 2>/dev/null; then
        printf 'type: error\npayload:\n'
        printf '  code: http_error\n'
        printf '  message: TODO HTTP fallback returned HTTP %s for %s\n' "$http_status" "$operation"
        printf '  details:\n'
        printf '    operation: %s\n' "$operation"
        printf '    responseBody: |\n'
        sed 's/^/      /' "$tmp_body"
        printf '\n'
        rm -f "$tmp_body" "$tmp_headers" "$tmp_request"
        return 1
    fi

    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/json"
    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    printf '  contentType: %s\n' "$content_type"
    rm -f "$tmp_body" "$tmp_headers" "$tmp_request"
}

_repl_workflow_todo() {
    local method="$1"
    local params_yaml="${2:-}"
    local operation="${method#workflow.todo.}"
    local response status typed_method typed_params fallback_method="" original_timeout todo_timeout
    local http_response="" http_status=1
    local failsafe_file=""

    case "$operation" in
        query) typed_method="client.Todo.QueryAsync" ;;
        get) typed_method="client.Todo.GetAsync" ;;
        create) typed_method="client.Todo.CreateAsync" ;;
        update) typed_method="client.Todo.UpdateAsync" ;;
        delete) typed_method="client.Todo.DeleteAsync" ;;
        analyzeRequirements) typed_method="client.Todo.AnalyzeRequirementsAsync" ;;
        *)
            _repl_invoke_raw_in_workspace "$method" "$params_yaml"
            return $?
            ;;
    esac

    _repl_bootstrap_state "$(pwd)" >/dev/null 2>&1 || true
    if _repl_workflow_todo_is_mutation "$operation"; then
        failsafe_file="$(_repl_failsafe_write "$method" "$params_yaml" "todo_${operation}")"
    fi

    http_response="$(_repl_todo_http_fallback "$operation" "$params_yaml" 2>&1)"
    http_status=$?
    if [ $http_status -eq 0 ] && _repl_response_is_nonempty_success "$http_response"; then
        _repl_failsafe_clear "$failsafe_file"
        printf '%s\n' "$http_response"
        return 0
    fi

    original_timeout="${REPL_TIMEOUT:-}"
    todo_timeout="${REPL_TODO_REPL_TIMEOUT:-8}"
    export REPL_TIMEOUT="$todo_timeout"

    response="$(_repl_invoke_raw_in_workspace "$method" "$params_yaml" "compat" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        printf '%s\n' "$response"
        return 0
    fi

    typed_params="$(_repl_todo_typed_params "$operation" "$params_yaml")"
    original_timeout="${REPL_TIMEOUT:-}"
    export REPL_TIMEOUT="$todo_timeout"
    response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" "compat" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        printf '%s\n' "$response"
        return 0
    fi

    if [ -z "$http_response" ]; then
        http_response="$(_repl_todo_http_fallback "$operation" "$params_yaml" 2>&1)"
        http_status=$?
        if [ $http_status -eq 0 ] && _repl_response_is_nonempty_success "$http_response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$http_response"
            return 0
        fi
    fi

    if [ -n "$fallback_method" ]; then
        response="$(_repl_invoke_raw_in_workspace "$fallback_method" "$params_yaml" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi

    original_timeout="${REPL_TIMEOUT:-}"
    export REPL_TIMEOUT="$todo_timeout"
    response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" 2>&1)"
    status=$?
    if [ -n "$original_timeout" ]; then export REPL_TIMEOUT="$original_timeout"; else unset REPL_TIMEOUT; fi
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        printf '%s\n' "$response"
        return 0
    fi

    if [ -n "$fallback_method" ]; then
        response="$(_repl_invoke_raw_in_workspace "$fallback_method" "$params_yaml" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi

    if [ -n "$http_response" ]; then
        printf '%s\n' "$http_response"
        return $http_status
    fi

    printf '%s\n' "$response"
    return $status
}

_repl_workflow_requirements() {
    local method="$1"
    local params_yaml="${2:-}"
    local operation="${method#workflow.requirements.}"
    local workflow_params typed_method typed_params response status created_response
    local failsafe_file=""
    local typed_tried=0

    if [ "$operation" = "copyAcceptanceCriteriaFromTodo" ]; then
        _repl_requirements_bootstrap_state "$params_yaml" || return 1
        failsafe_file="$(_repl_failsafe_write "$method" "$params_yaml" "requirements_${operation}")"
        response="$(_repl_requirements_copy_acceptance_http_fallback "$params_yaml" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi

        response="$(_repl_invoke_raw_in_workspace "$method" "$params_yaml" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi

        printf '%s\n' "$response"
        return $status
    fi

    _repl_requirements_typed_method "$operation" >/dev/null || {
        _repl_invoke_raw_in_workspace "$method" "$params_yaml"
        return $?
    }

    _repl_requirements_bootstrap_state "$params_yaml" || return 1
    if _repl_workflow_requirements_is_mutation "$operation"; then
        failsafe_file="$(_repl_failsafe_write "$method" "$params_yaml" "requirements_${operation}")"
    fi

    typed_method="$(_repl_requirements_typed_method "$operation")"
    typed_params="$(_repl_requirements_typed_params "$operation" "$params_yaml")"

    if _repl_workflow_requirements_prefers_typed "$operation"; then
        typed_tried=1
        response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
        if [ $status -eq 0 ]; then
            created_response="$(_repl_requirements_created_response_after_empty_success "$operation" "$params_yaml" "$response" 2>&1)"
            if [ $? -eq 0 ]; then
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$created_response"
                return 0
            fi
        fi

        response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
        if [ $status -eq 0 ]; then
            created_response="$(_repl_requirements_created_response_after_empty_success "$operation" "$params_yaml" "$response" 2>&1)"
            if [ $? -eq 0 ]; then
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$created_response"
                return 0
            fi
        fi
    fi

    workflow_params="$(_repl_requirements_workflow_params "$operation" "$params_yaml")"
    response="$(_repl_invoke_raw_in_workspace "$method" "$workflow_params" "compat" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        if [ "$operation" = "generateDocument" ]; then
            if _repl_requirements_emit_generate_response "$response" "$params_yaml"; then
                _repl_failsafe_clear "$failsafe_file"
                return 0
            fi
        else
            _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi

    response="$(_repl_invoke_raw_in_workspace "$method" "$workflow_params" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && ! _repl_response_is_error "$response"; then
        if [ "$operation" = "generateDocument" ]; then
            if _repl_requirements_emit_generate_response "$response" "$params_yaml"; then
                _repl_failsafe_clear "$failsafe_file"
                return 0
            fi
        else
            _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi

    if [ "$typed_tried" -eq 0 ]; then
        response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" "compat" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            if [ "$operation" = "generateDocument" ]; then
                if _repl_requirements_emit_generate_response "$response" "$params_yaml"; then
                    _repl_failsafe_clear "$failsafe_file"
                    return 0
                fi
            else
                _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$response"
                return 0
            fi
        fi
        if [ $status -eq 0 ]; then
            created_response="$(_repl_requirements_created_response_after_empty_success "$operation" "$params_yaml" "$response" 2>&1)"
            if [ $? -eq 0 ]; then
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$created_response"
                return 0
            fi
        fi

        response="$(_repl_invoke_raw_in_workspace "$typed_method" "$typed_params" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            if [ "$operation" = "generateDocument" ]; then
                if _repl_requirements_emit_generate_response "$response" "$params_yaml"; then
                    _repl_failsafe_clear "$failsafe_file"
                    return 0
                fi
            else
                _repl_requirements_acceptance_criteria_result_ok "$operation" "$params_yaml" "$response" || return 1
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$response"
                return 0
            fi
        fi
        if [ $status -eq 0 ]; then
            created_response="$(_repl_requirements_created_response_after_empty_success "$operation" "$params_yaml" "$response" 2>&1)"
            if [ $? -eq 0 ]; then
                _repl_failsafe_clear "$failsafe_file"
                printf '%s\n' "$created_response"
                return 0
            fi
        fi
    fi
    if [ "$operation" = "listFr" ] || [ "$operation" = "listTr" ] || [ "$operation" = "listTest" ] || [ "$operation" = "listMappings" ]; then
        response="$(_repl_requirements_list_http_fallback "$operation" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi
    if [ "$operation" = "generateDocument" ]; then
        response="$(_repl_requirements_generate_http_fallback "$params_yaml" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            _repl_failsafe_clear "$failsafe_file"
            printf '%s\n' "$response"
            return 0
        fi
    fi
    printf '%s\n' "$response"
    return $status
}

_repl_submit_session() {
    local source_type="$1"
    local session_id="$2"
    local title="$3"
    local model="$4"
    local started="$5"
    local status="$6"
    local turns_block="${7:-}"
    local turn_count="${8:-0}"
    local turn_request_id="${9:-}"
    local turn_title="${10:-}"
    local turn_status="${11:-in_progress}"
    local response_text="${12:-}"
    local actions_block="${13:-}"

    local params
    params="$(_repl_build_session_submit_params "$source_type" "$session_id" "$title" "$model" "$started" "$status" "$turns_block" "$turn_count")"
    local failsafe_file submit_status
    failsafe_file="$(_repl_failsafe_write "workflow.sessionlog.importRecovery" "$params" "session_submit")"
    if [ -n "$turn_request_id" ]; then
        _repl_sessionlog_submit_http_fallback "$source_type" "$session_id" "$title" "$model" "$started" "$status" "1" "$turn_request_id" "$turn_title" "$turn_status" "$response_text" "$actions_block" >/dev/null 2>&1
        submit_status=$?
    else
        _repl_sessionlog_submit_http_fallback "$source_type" "$session_id" "$title" "$model" "$started" "$status" "0" "" "" "" "" "" >/dev/null 2>&1
        submit_status=$?
    fi
    if [ $submit_status -eq 0 ]; then
        _repl_failsafe_clear "$failsafe_file"
        return 0
    fi

    _repl_invoke_raw_in_workspace "client.SessionLog.SubmitAsync" "$params" "compat" >/dev/null 2>&1
    submit_status=$?
    if [ $submit_status -eq 0 ]; then
        _repl_failsafe_clear "$failsafe_file"
    fi
    return $submit_status
}

_repl_normalized_actions_block() {
    local block
    block="$(_repl_json_array_block_get "$1" "actions" 2>/dev/null || true)"
    if [ -n "$block" ]; then
        printf '%s\n' "$block"
        return 0
    fi
    _repl_list_block_get "$1" "actions"
}

_repl_normalized_dialog_items_block() {
    local block
    block="$(_repl_json_array_block_get "$1" "dialogItems" 2>/dev/null || true)"
    if [ -n "$block" ]; then
        printf '%s\n' "$block"
        return 0
    fi
    _repl_list_block_get "$1" "dialogItems"
}

_repl_turns_block() {
    local req_id="$1"
    local title="$2"
    local status="$3"
    local response_text="$4"
    local actions_block="${5:-}"

    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    local query_text timestamp model
    query_text="$(_repl_yaml_block_get "$(cat "$turn_file" 2>/dev/null)" "queryText")"
    [ -z "$query_text" ] && query_text="$title"
    timestamp="$(_repl_current_turn_value "openedAt")"
    [ -z "$timestamp" ] && timestamp="$(_repl_now_iso)"
    model="$(_repl_session_state_value "model")"
    [ -z "$model" ] && model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-codex}}"

    local query_text_indented response_indented
    query_text_indented="$(printf '%s\n' "$query_text" | sed 's/^/        /')"
    response_indented="$(printf '%s\n' "$response_text" | sed 's/^/        /')"

    local files_modified_block=""
    local file_paths
    file_paths="$(printf '%s\n' "$actions_block" | grep '^[[:space:]]*filePath:' | sed 's/^[[:space:]]*filePath:[[:space:]]*//')"
    if [ -n "$file_paths" ]; then
        files_modified_block="      filesModified:
$(printf '%s\n' "$file_paths" | sed 's/^/        - /')"
    else
        files_modified_block="      filesModified: []"
    fi

    # Return JSON string for the turns block entry (object serialized).
    local files_json='[]'
    if [ -n "$file_paths" ]; then
        files_json=$(printf '%s\n' "$file_paths" | node -e 'console.log(JSON.stringify(fs.readFileSync(0,"utf8").trim().split(/\r?\n/).filter(Boolean)))' 2>/dev/null || echo '[]')
    fi
    local actions_json='[]'
    if [ -n "$actions_block" ]; then
        actions_json=$(printf '%s\n' "$actions_block" | node -e "$(cat <<'NODEEOF'
const fs=require("fs"); const txt=fs.readFileSync(0,"utf8"); const lines=txt.split(/\r?\n/); let cur=null,arr=[];
for(const raw of lines){const t=raw.trim();if(!t)continue;
  if(t.startsWith("- ")){if(cur)arr.push(cur);cur={};const r=t.slice(2).trim();if(r.includes(":")){const[k,...v]=r.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}}
  else if(cur&&t.includes(":")){const[k,...v]=t.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}
}if(cur)arr.push(cur);console.log(JSON.stringify(arr));
NODEEOF
)" 2>/dev/null || echo '[]')
    fi
    node -e '
      console.log(JSON.stringify({
        requestId: process.argv[1], timestamp: process.argv[2], queryText: process.argv[3],
        queryTitle: process.argv[4], response: process.argv[5], interpretation: "",
        status: process.argv[6], model: process.argv[7], modelProvider: "", tokenCount: 0,
        tags: [], contextList: [], designDecisions: [], requirementsDiscovered: [],
        filesModified: JSON.parse(process.argv[8]), blockers: [], actions: JSON.parse(process.argv[9]), processingDialog: []
      }));
    ' "$req_id" "$timestamp" "$query_text" "$title" "$response_text" "$status" "$model" "$files_json" "$actions_json"
}

_repl_turn_upsert_params() {
    local source_type="$1"
    local session_id="$2"
    local req_id="$3"
    local title="$4"
    local status="$5"
    local response_text="$6"
    local actions_block="${7:-}"

    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    local query_text timestamp model
    query_text="$(_repl_yaml_block_get "$(cat "$turn_file" 2>/dev/null)" "queryText")"
    [ -z "$query_text" ] && query_text="$title"
    timestamp="$(_repl_current_turn_value "openedAt")"
    [ -z "$timestamp" ] && timestamp="$(_repl_now_iso)"
    model="$(_repl_session_state_value "model")"
    [ -z "$model" ] && model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-codex}}"

    local query_text_indented response_indented
    query_text_indented="$(printf '%s\n' "$query_text" | sed 's/^/    /')"
    response_indented="$(printf '%s\n' "$response_text" | sed 's/^/    /')"

    local files_modified_block=""
    local file_paths
    file_paths="$(printf '%s\n' "$actions_block" | grep '^[[:space:]]*filePath:' | sed 's/^[[:space:]]*filePath:[[:space:]]*//')"
    if [ -n "$file_paths" ]; then
        files_modified_block="  filesModified:
$(printf '%s\n' "$file_paths" | sed 's/^/    - /')"
    fi

    # Build as object, return as JSON string. Caller will use with JSON envelope.
    # No more text YAML for turn documents.
    local files_json="[]"
    if [ -n "$files_modified_block" ] && printf '%s\n' "$files_modified_block" | grep -q 'filesModified:'; then
        # simple extraction for list, or build in node
        files_json=$(printf '%s\n' "$files_modified_block" | node -e '
          const fs=require("fs"); let t=fs.readFileSync(0,"utf8");
          let m = t.match(/filesModified:[\s\S]*?(\[[\s\S]*?\]|\n(  - .+)+)/);
          if(m) { try{console.log(JSON.stringify(JSON.parse(m[1]||m[0].replace(/  - /g,"").split("\n").filter(Boolean).map(s=>s.trim()))));}catch(e){} }
        ' 2>/dev/null || echo "[]")
    fi

    local actions_json="[]"
    if [ -n "$actions_block" ]; then
        actions_json=$(printf '%s\n' "$actions_block" | node -e "$(cat <<'NODEEOF'
const fs=require("fs"); const txt=fs.readFileSync(0,"utf8");
const lines=txt.split(/\r?\n/); let cur=null, arr=[];
for(const l of lines){ const t=l.trim(); if(!t)continue;
  if(t.startsWith("- ")){if(cur)arr.push(cur);cur={}; const r=t.slice(2).trim(); if(r.includes(":")){const[k,...v]=r.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}}
  else if(cur && t.includes(":")){const[k,...v]=t.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}
}if(cur)arr.push(cur); console.log(JSON.stringify(arr));
NODEEOF
)" 2>/dev/null || echo "[]")
    fi

    node -e '
      const turn = {
        requestId: process.argv[1],
        timestamp: process.argv[2],
        queryText: process.argv[3],
        queryTitle: process.argv[4],
        response: process.argv[5],
        status: process.argv[6],
        model: process.argv[7],
        tokenCount: 0,
        ...(process.argv[8] !== "[]" ? {filesModified: JSON.parse(process.argv[8])} : {}),
        ...(process.argv[9] !== "[]" ? {actions: JSON.parse(process.argv[9])} : {})
      };
      const doc = { agent: process.argv[10], sessionId: process.argv[11], turn };
      console.log(JSON.stringify(doc));
    ' \
      "$req_id" "$timestamp" "$query_text" "$title" "$response_text" "$status" "$model" \
      "$files_json" "$actions_json" \
      "$source_type" "$session_id"
}

_repl_persist_turn() {
    # _repl_persist_turn <requestId> <queryTitle> <status> <responseText> [actionsYamlList]
    local req_id="$1"
    local title="$2"
    local status="$3"
    local response_text="$4"
    local actions_block="${5:-}"

    local meta source_type session_id
    if ! meta="$(_repl_session_meta)"; then
        return 1
    fi
    source_type="${meta%% *}"
    session_id="${meta##* }"

    local title_state model started
    title_state="$(_repl_session_state_value "title")"
    model="$(_repl_session_state_value "model")"
    started="$(_repl_session_state_value "started")"
    [ -z "$title_state" ] && title_state="$title"
    [ -z "$model" ] && model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-codex}}"
    [ -z "$started" ] && started="$(_repl_now_iso)"

    # Object + JSON serialization for the envelope (no hand-built YAML text).
    # Fixes "Malformed YAML envelope" for turns with actions lists + multiline responses.
    local response_b64 actions_b64
    actions_b64=""
    response_b64=$(printf '%s' "$response_text" | base64 | tr -d '\r\n')
    [ -n "$actions_block" ] && actions_b64=$(printf '%s' "$actions_block" | base64 | tr -d '\r\n')

    local json_envelope
    json_envelope=$( \
      SOURCE_TYPE="$source_type" SESSION_ID="$session_id" REQ_ID="$req_id" \
      TITLE="$title" STATUS="$status" RESPONSE_B64="$response_b64" \
      ACTIONS_B64="$actions_b64" MODEL="$model" TIMESTAMP="$started" \
      node <<'NODEEOF'
const src=process.env.SOURCE_TYPE, sid=process.env.SESSION_ID, rid=process.env.REQ_ID;
const title=process.env.TITLE||"", status=process.env.STATUS||"in_progress";
const model=process.env.MODEL||"codex", ts=process.env.TIMESTAMP||new Date().toISOString();
const resp=Buffer.from(process.env.RESPONSE_B64||"","base64").toString("utf8");
let actions=[];
if(process.env.ACTIONS_B64){
  const txt=Buffer.from(process.env.ACTIONS_B64,"base64").toString("utf8");
  const lines=txt.split(/\r?\n/); let cur=null;
  for(const raw of lines){const line=raw.trim();if(!line)continue;
    if(line.startsWith("- ")){if(cur)actions.push(cur);cur={};
      const rest=line.slice(2).trim();if(rest.includes(":")){const[k,...v]=rest.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}}
    else if(cur&&line.includes(":")){const[k,...v]=line.split(":");cur[k.trim()]=v.join(":").trim().replace(/^["']|["']$/g,"");}
  }if(cur)actions.push(cur);
}
const turn={agent:src,sessionId:sid,turn:{requestId:rid,timestamp:ts,queryTitle:title,response:resp,status,model,tokenCount:0,actions}};
const env={type:"request",payload:{requestId:"req-"+Date.now().toString(16),method:"client.SessionLog.UpsertTurnAsync",params:turn}};
console.log(JSON.stringify(env));
NODEEOF
    )

    local failsafe_file
    failsafe_file="$(_repl_failsafe_write "client.SessionLog.UpsertTurnAsync" "$json_envelope" "session_upsertTurn")"

    local tmp_env="${REPL_INVOKE_CACHE_DIR}/envelope-${req_id}.$$.json"
    mkdir -p "$(dirname "$tmp_env")" 2>/dev/null || true
    printf '%s\n' "$json_envelope" > "$tmp_env"
    local response
    response="$(cat "$tmp_env" | _repl_run_repl_with_timeout "${REPL_TIMEOUT:-30}" mcpserver-repl --agent-stdio --agent "$AGENT_NAME" 2>/dev/null || true)"
    rm -f "$tmp_env" 2>/dev/null || true

    if [ $? -eq 0 ] && ! _repl_response_is_error "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        return 0
    fi

    printf '%s\n' "$response" >&2
    return 1
}

_repl_workflow_bootstrap() {
    local start_dir
    start_dir="$(_repl_yaml_get "$1" "workspacePath")"
    [ -z "$start_dir" ] && start_dir="$(pwd)"

    if ! _repl_bootstrap_state "$start_dir"; then
        return 1
    fi

    _repl_emit_response "  initialized: true"
}

_repl_workflow_open_session() {
    local params="$1"
    local start_dir
    start_dir="$(_repl_yaml_get "$params" "workspacePath")"
    [ -z "$start_dir" ] && start_dir="$(pwd)"

    _repl_bootstrap_state "$start_dir" || return 1

    local workspace workspace_path base_url
    workspace="$(_repl_unquote "$(_repl_session_state_value "workspace")")"
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="$(_repl_unquote "$(_repl_session_state_value "baseUrl")")"

    local source_type model title session_id started last_updated
    source_type="$(_repl_yaml_get "$params" "agent")"
    [ -z "$source_type" ] && source_type="$(_repl_yaml_get "$params" "sourceType")"
    [ -z "$source_type" ] && source_type="$(_repl_session_state_value "sourceType")"
    [ -z "$source_type" ] && source_type="${MCP_SESSION_AGENT:-${PLUGIN_AGENT_DEFAULT:-Codex}}"

    model="$(_repl_yaml_get "$params" "model")"
    [ -z "$model" ] && model="$(_repl_session_state_value "model")"
    [ -z "$model" ] && model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-codex}}"

    title="$(_repl_yaml_get "$params" "title")"
    [ -z "$title" ] && title="$(_repl_session_state_value "title")"
    [ -z "$title" ] && title="${workspace} session"

    session_id="$(_repl_yaml_get "$params" "sessionId")"
    [ -z "$session_id" ] && session_id="$(_repl_session_state_value "sessionId")"
    [ -z "$session_id" ] && session_id="$(_repl_generate_session_id "$source_type" "$title" "$workspace")"

    started="$(_repl_session_state_value "started")"
    [ -z "$started" ] && started="$(_repl_now_iso)"
    last_updated="$(_repl_now_iso)"

    _repl_write_session_state "verified" "$source_type" "$session_id" "$title" "$model" "$started" "$last_updated" "$workspace_path" "$workspace" "$base_url"
    _repl_submit_session "$source_type" "$session_id" "$title" "$model" "$started" "in_progress" "" "0" || true

    _repl_emit_response "  sessionId: ${session_id}
  started: ${started}"
}

# PLAN-SESSIONLOGENFORCEMENT-001: increment (or create) a numeric audit counter in
# current-turn.yaml. Adds the field at the end of the file when it is missing so turns
# created before the audit schema existed are upgraded in place on first append.
_repl_turn_bump() {
    local turn_file="$1" field="$2" delta="$3" current new tmp
    [ -f "$turn_file" ] || return 0
    [ -n "$delta" ] || delta=0
    case "$delta" in (*[!0-9]*) delta=0 ;; esac
    [ "$delta" -eq 0 ] && return 0
    current="$(grep "^${field}:" "$turn_file" 2>/dev/null | head -1 | sed "s/^${field}:[[:space:]]*//" || true)"
    current="${current:-0}"
    case "$current" in (*[!0-9]*) current=0 ;; esac
    new=$((current + delta))
    tmp="${turn_file}.tmp.$$"
    if grep -q "^${field}:" "$turn_file"; then
        awk -v f="$field" -v n="$new" '
            index($0, f ":") == 1 { print f ": " n; next }
            { print }
        ' "$turn_file" > "$tmp" && mv "$tmp" "$turn_file"
    else
        cp "$turn_file" "$tmp" && printf '%s: %s\n' "$field" "$new" >> "$tmp" && mv "$tmp" "$turn_file"
    fi
}

# PLAN-SESSIONLOGENFORCEMENT-001: count occurrences of a fixed-string pattern in the params.
_repl_count_lines() {
    local params="$1" pattern="$2" count
    count="$(printf '%s\n' "$params" | grep -c "$pattern" 2>/dev/null || true)"
    count="${count:-0}"
    case "$count" in (*[!0-9]*) count=0 ;; esac
    printf '%s' "$count"
}

# PLAN-SESSIONLOGENFORCEMENT-001: a turn carries the audit schema once begin_turn (or a
# prior append) has written the auditActions counter. Used to keep enforcement backward
# compatible with legacy turn caches that predate the audit fields.
_repl_turn_has_audit_schema() {
    local turn_file="$1"
    [ -f "$turn_file" ] && grep -q '^auditActions:' "$turn_file"
}

# PLAN-SESSIONLOGENFORCEMENT-001: emit `complete: true/false` plus the missing required
# fields for the current turn. A turn with no code edits is complete by definition; a
# code-edit turn must record at least one action, one modified file, and one reasoning or
# decision dialog entry before it can finalize.
_repl_turn_audit_missing() {
    local turn_file="$1" code_edits actions files dialog decisions missing=""
    code_edits="$(grep '^codeEdits:' "$turn_file" 2>/dev/null | head -1 | sed 's/^codeEdits:[[:space:]]*//' || true)"
    code_edits="${code_edits:-0}"
    case "$code_edits" in (*[!0-9]*) code_edits=0 ;; esac
    [ "$code_edits" -eq 0 ] && return 0
    actions="$(grep '^auditActions:' "$turn_file" 2>/dev/null | head -1 | sed 's/^auditActions:[[:space:]]*//' || true)"
    files="$(grep '^auditFiles:' "$turn_file" 2>/dev/null | head -1 | sed 's/^auditFiles:[[:space:]]*//' || true)"
    dialog="$(grep '^auditDialog:' "$turn_file" 2>/dev/null | head -1 | sed 's/^auditDialog:[[:space:]]*//' || true)"
    decisions="$(grep '^auditDecisions:' "$turn_file" 2>/dev/null | head -1 | sed 's/^auditDecisions:[[:space:]]*//' || true)"
    [ "${actions:-0}" -ge 1 ] 2>/dev/null || missing="${missing} actions"
    [ "${files:-0}" -ge 1 ] 2>/dev/null || missing="${missing} filesModified"
    if ! { [ "${dialog:-0}" -ge 1 ] 2>/dev/null || [ "${decisions:-0}" -ge 1 ] 2>/dev/null; }; then
        missing="${missing} processingDialog/designDecisions"
    fi
    printf '%s' "${missing# }"
}

_repl_workflow_begin_turn() {
    local params="$1"
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    local session_id

    session_id="$(_repl_session_state_value "sessionId")"
    if [ -z "$session_id" ]; then
        _repl_workflow_open_session "" >/dev/null
        session_id="$(_repl_session_state_value "sessionId")"
    fi
    [ -z "$session_id" ] && return 1

    local turn_request_id query_title query_text opened_at
    turn_request_id="$(_repl_yaml_get "$params" "requestId")"
    [ -z "$turn_request_id" ] && turn_request_id="req-$(_repl_now_compact)-turn-$(_repl_slugify "$(_repl_yaml_get "$params" "queryTitle")")"
    query_title="$(_repl_yaml_get "$params" "queryTitle")"
    [ -z "$query_title" ] && query_title="User prompt"
    query_text="$(_repl_yaml_block_get "$params" "queryText")"
    [ -z "$query_text" ] && query_text="$(_repl_yaml_get "$params" "queryText")"
    [ -z "$query_text" ] && query_text="$query_title"
    opened_at="$(_repl_now_iso)"

    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    cat > "$turn_file" <<EOF
turnRequestId: ${turn_request_id}
queryTitle: ${query_title}
openedAt: ${opened_at}
status: in_progress
codeEdits: 0
lastBuildStatus: unknown
auditActions: 0
auditDialog: 0
auditDecisions: 0
auditFiles: 0
auditCommits: 0
queryText: |
$(printf '%s\n' "$query_text" | sed 's/^/  /')
EOF

    _repl_persist_turn "$turn_request_id" "$query_title" "in_progress" "(turn opened)" "" || true
    _repl_emit_response "  turnRequestId: ${turn_request_id}
  status: in_progress
  timestamp: ${opened_at}"
}

_repl_workflow_update_turn() {
    local params="$1"
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    [ -f "$turn_file" ] || return 0

    local req_id title response_text status
    req_id="$(_repl_current_turn_value "turnRequestId")"
    title="$(_repl_current_turn_value "queryTitle")"
    status="$(_repl_yaml_get "$params" "status")"
    [ -z "$status" ] && status="$(_repl_current_turn_value "status")"
    [ -z "$status" ] && status="in_progress"
    response_text="$(_repl_yaml_block_get "$params" "response")"
    [ -z "$response_text" ] && response_text="$(_repl_yaml_get "$params" "response")"
    [ -z "$response_text" ] && response_text="Turn updated."

    _repl_persist_turn "$req_id" "$title" "$status" "$response_text" "" || true
    _repl_emit_response "  turnRequestId: ${req_id}
  status: ${status}"
}

_repl_workflow_append_dialog() {
    local params="$1"
    local meta source_type session_id request_id items_block
    if ! meta="$(_repl_session_meta)"; then
        return 1
    fi
    source_type="${meta%% *}"
    session_id="${meta##* }"

    request_id="$(_repl_yaml_get "$params" "requestId")"
    [ -z "$request_id" ] && request_id="$(_repl_current_turn_value "turnRequestId")"
    [ -z "$request_id" ] && return 1

    items_block="$(_repl_normalized_dialog_items_block "$params")"
    [ -z "$items_block" ] && return 1

    # PLAN-SESSIONLOGENFORCEMENT-001: record dialog and decision counts locally so the turn
    # audit reflects reasoning even when the remote submit is unavailable.
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    if [ -f "$turn_file" ]; then
        local dialog_count decision_count
        dialog_count="$(_repl_count_lines "$params" '^[[:space:]]*-\{0,1\}[[:space:]]*role:')"
        [ "$dialog_count" -eq 0 ] 2>/dev/null && dialog_count="$(_repl_count_lines "$params" '^[[:space:]]*content:')"
        decision_count="$(_repl_count_lines "$params" '^[[:space:]]*category:[[:space:]]*decision')"
        _repl_turn_bump "$turn_file" "auditDialog" "$dialog_count"
        _repl_turn_bump "$turn_file" "auditDecisions" "$decision_count"
    fi

    local invoke_params="agent: ${source_type}
sessionId: ${session_id}
requestId: ${request_id}
items:
$(printf '%s\n' "$items_block" | sed 's/^/  /')"

    local response status failsafe_file
    failsafe_file="$(_repl_failsafe_write "workflow.sessionlog.appendDialog" "$params" "session_appendDialog")"
    response="$(_repl_invoke_raw_in_workspace "client.SessionLog.AppendDialogAsync" "$invoke_params" "compat" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && ! _repl_response_is_error "$response"; then
        _repl_failsafe_clear "$failsafe_file"
        printf '%s\n' "$response"
        return 0
    fi

    response="$(_repl_invoke_raw_in_workspace "client.SessionLog.AppendDialogAsync" "$invoke_params" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && ! _repl_response_is_error "$response"; then
        _repl_failsafe_clear "$failsafe_file"
    fi
    printf '%s\n' "$response"
    return $status
}

_repl_workflow_append_actions() {
    local params="$1"
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    [ -f "$turn_file" ] || return 0

    local actions_block
    actions_block="$(_repl_normalized_actions_block "$params")"

    local added current new tmp
    # Count non-empty filePath: from the (possibly JSON-normalized) block only.
    added="$(printf '%s\n' "$actions_block" | grep -E '^[[:space:]]*filePath:[[:space:]]*("?)[^"[:space:]]' | grep -c . || true)"
    added="${added:-0}"

    current="$(_repl_current_turn_value "codeEdits")"
    current="${current:-0}"
    new=$((current + added))

    tmp="${turn_file}.tmp.$$"
    awk -v n="$new" '
        /^codeEdits:/ { print "codeEdits: " n; next }
        { print }
    ' "$turn_file" > "$tmp" && mv "$tmp" "$turn_file"

    # PLAN-SESSIONLOGENFORCEMENT-001: keep per-turn audit counters in sync
    local action_count decision_count commit_count
    action_count="$(_repl_count_lines "$actions_block" '^[[:space:]]*type:')"
    decision_count="$(_repl_count_lines "$actions_block" '^[[:space:]]*type:[[:space:]]*design_decision')"
    commit_count="$(_repl_count_lines "$actions_block" '^[[:space:]]*type:[[:space:]]*commit')"
    _repl_turn_bump "$turn_file" "auditActions" "$action_count"
    _repl_turn_bump "$turn_file" "auditFiles" "$added"
    _repl_turn_bump "$turn_file" "auditDecisions" "$decision_count"
    _repl_turn_bump "$turn_file" "auditCommits" "$commit_count"

    local req_id title status
    req_id="$(_repl_current_turn_value "turnRequestId")"
    title="$(_repl_current_turn_value "queryTitle")"
    status="$(_repl_current_turn_value "status")"
    [ -z "$status" ] && status="in_progress"
    _repl_persist_turn "$req_id" "$title" "$status" "Actions appended." "$actions_block" || true

    _repl_emit_response "  ok: true
  codeEdits: ${new}"
}

_repl_workflow_complete_turn() {
    local params="$1"
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    [ -f "$turn_file" ] || {
        _repl_emit_response "  ok: true"
        return 0
    }

    local req_id title response_text tmp
    req_id="$(_repl_current_turn_value "turnRequestId")"
    title="$(_repl_current_turn_value "queryTitle")"
    response_text="$(_repl_yaml_block_get "$params" "response")"
    [ -z "$response_text" ] && response_text="$(_repl_yaml_get "$params" "response")"
    [ -z "$response_text" ] && response_text="(no response provided)"

    tmp="${turn_file}.tmp.$$"
    awk '
        /^status:/ { print "status: completed"; next }
        { print }
    ' "$turn_file" > "$tmp" && mv "$tmp" "$turn_file"

    # When rich params (interpretation field) are present, route to importRecovery
    # so all JSONL-extracted fields reach the server instead of the minimal turn block.
    if printf '%s\n' "$params" | grep -q '^interpretation:' 2>/dev/null && command -v node >/dev/null 2>&1; then
        local opened_at source_type session_id model started import_yaml
        opened_at="$(_repl_current_turn_value "openedAt")"
        [ -z "$opened_at" ] && opened_at="$(_repl_now_iso)"
        source_type="$(_repl_session_state_value "sourceType")"
        [ -z "$source_type" ] && source_type="${CT2R_SOURCE_TYPE:-${PLUGIN_AGENT_DEFAULT:-Codex}}"
        session_id="$(_repl_session_state_value "sessionId")"
        model="$(_repl_session_state_value "model")"
        [ -z "$model" ] && model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-codex}}"
        started="$(_repl_session_state_value "started")"
        [ -z "$started" ] && started="$(_repl_now_iso)"

        import_yaml="$(printf '%s\n' "$params" | node "${REPL_INVOKE_SCRIPT_DIR}/complete-turn-to-recovery.js" \
            "$session_id" "$source_type" "$model" "$started" "$req_id" "$title" "$opened_at" 2>/dev/null || true)"

        if [ -n "$import_yaml" ]; then
            _repl_workflow_import_recovery "$import_yaml" >/dev/null 2>&1 || true
            _repl_emit_response "  ok: true
  status: completed"
            return 0
        fi
    fi

    _repl_persist_turn "$req_id" "$title" "completed" "$response_text" "" || true
    _repl_emit_response "  ok: true
  status: completed"
}

# PLAN-SESSIONLOGENFORCEMENT-001: single close-turn helper. Appends any supplied actions
# and dialog items (so their audit counters are recorded) and then completes the turn in
# one call, giving agents a way to finalize with full audit data instead of a bare
# completeTurn that leaves actions/dialog/decisions empty.
_repl_workflow_close_turn() {
    local params="$1"
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"

    if printf '%s\n' "$params" | grep -q '^[[:space:]]*actions:'; then
        _repl_workflow_append_actions "$params" >/dev/null 2>&1 || true
    fi
    if printf '%s\n' "$params" | grep -q '^[[:space:]]*dialogItems:'; then
        _repl_workflow_append_dialog "$params" >/dev/null 2>&1 || true
    fi

    _repl_workflow_complete_turn "$params"
}

# PLAN-SESSIONLOGENFORCEMENT-001: report per-turn audit completeness so an agent (or the
# stop-gate) can detect a completed turn that is missing actions, modified files, or
# reasoning. Emits the counters plus a `complete` verdict and the list of missing fields.
_repl_workflow_audit() {
    local turn_file="${REPL_INVOKE_CACHE_DIR}/current-turn.yaml"
    if [ ! -f "$turn_file" ]; then
        _repl_emit_response "  complete: false
  reason: no active turn"
        return 0
    fi

    local status code_edits actions files dialog decisions commits missing complete
    status="$(_repl_current_turn_value "status")"
    code_edits="$(_repl_current_turn_value "codeEdits")"; code_edits="${code_edits:-0}"
    actions="$(_repl_current_turn_value "auditActions")"; actions="${actions:-0}"
    files="$(_repl_current_turn_value "auditFiles")"; files="${files:-0}"
    dialog="$(_repl_current_turn_value "auditDialog")"; dialog="${dialog:-0}"
    decisions="$(_repl_current_turn_value "auditDecisions")"; decisions="${decisions:-0}"
    commits="$(_repl_current_turn_value "auditCommits")"; commits="${commits:-0}"
    missing="$(_repl_turn_audit_missing "$turn_file")"
    if [ -z "$missing" ]; then complete="true"; else complete="false"; fi

    _repl_emit_response "  complete: ${complete}
  status: ${status:-unknown}
  codeEdits: ${code_edits}
  auditActions: ${actions}
  auditFiles: ${files}
  auditDialog: ${dialog}
  auditDecisions: ${decisions}
  auditCommits: ${commits}
  missing: ${missing:-none}"
}

_repl_workflow_query_history() {
    local params_yaml="${1:-}"
    local explicit_workspace_path response status

    explicit_workspace_path="$(_repl_yaml_get "$params_yaml" "workspacePath" 2>/dev/null || true)"
    explicit_workspace_path="$(_repl_unquote "$explicit_workspace_path")"
    if [ -n "$explicit_workspace_path" ]; then
        response="$(_repl_sessionlog_query_http_fallback "$params_yaml" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            printf '%s\n' "$response"
            return 0
        fi
    fi

    response="$(_repl_invoke_raw_in_workspace "client.SessionLog.QueryAsync" "$params_yaml" "compat" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    response="$(_repl_invoke_raw_in_workspace "client.SessionLog.QueryAsync" "$params_yaml" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    response="$(_repl_sessionlog_query_http_fallback "$params_yaml" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    printf '%s\n' "$response"
    return $status
}

_repl_sessionlog_import_recovery_http_fallback() {
    local params_yaml="${1:-}"

    if ! command -v curl >/dev/null 2>&1 || ! command -v node >/dev/null 2>&1; then
        return 1
    fi

    local workspace_path workspace_path_bash base_url marker_file api_key
    workspace_path="$(_repl_unquote "$(_repl_session_state_value "workspacePath")")"
    base_url="${MCPSERVER_BASE_URL:-$(_repl_unquote "$(_repl_session_state_value "baseUrl")")}"

    workspace_path_bash="$(_repl_path_for_bash "$workspace_path" 2>/dev/null || true)"
    if ! declare -F find_marker_file >/dev/null 2>&1 || ! declare -F parse_marker_field >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "${REPL_INVOKE_SCRIPT_DIR}/marker-resolver.sh" || return 1
        set +e
    fi
    marker_file=""
    if [ -n "$workspace_path_bash" ] && declare -F find_marker_file >/dev/null 2>&1; then
        marker_file="$(find_marker_file "$workspace_path_bash" 2>/dev/null || true)"
    fi

    api_key="${MCPSERVER_API_KEY:-$(_repl_compat_marker_field "$marker_file" "apiKey" "")}"
    [ -z "$workspace_path" ] && workspace_path="${MCPSERVER_WORKSPACE_PATH:-$(_repl_compat_marker_field "$marker_file" "workspacePath" "")}"
    [ -z "$base_url" ] && base_url="$(_repl_compat_marker_field "$marker_file" "baseUrl" "")"
    [ -z "$api_key" ] && return 1
    [ -z "$workspace_path" ] && return 1
    [ -z "$base_url" ] && return 1

    local tmp_request tmp_body tmp_headers timeout_seconds
    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    tmp_request="${REPL_INVOKE_CACHE_DIR}/sessionlog-import.$$.$RANDOM.json"
    tmp_body="${REPL_INVOKE_CACHE_DIR}/sessionlog-import.$$.$RANDOM.body"
    tmp_headers="${REPL_INVOKE_CACHE_DIR}/sessionlog-import.$$.$RANDOM.headers"
    timeout_seconds="${REPL_SESSIONLOG_HTTP_TIMEOUT:-20}"

    printf '%s' "$params_yaml" | node "${REPL_INVOKE_SCRIPT_DIR}/sessionlog-recovery-body.js" > "$tmp_request" 2>/dev/null || {
        rm -f "$tmp_request" "$tmp_body" "$tmp_headers"
        return 1
    }

    curl -sS \
        --max-time "$timeout_seconds" \
        -D "$tmp_headers" \
        -o "$tmp_body" \
        -X POST \
        -H "X-Api-Key: ${api_key}" \
        -H "X-Workspace-Path: ${workspace_path}" \
        -H "Content-Type: application/json" \
        --data-binary "@${tmp_request}" \
        "${base_url%/}/mcpserver/sessionlog" >/dev/null 2>&1
    local curl_status=$?
    if [ $curl_status -ne 0 ]; then
        if [ -s "$tmp_body" ]; then
            printf 'type: error\npayload:\n'
            printf '  code: http_error\n'
            printf '  message: session log import HTTP fallback failed with curl exit %s\n' "$curl_status"
            printf '  details:\n'
            printf '    responseBody: |\n'
            sed 's/^/      /' "$tmp_body"
            printf '\n'
        fi
        rm -f "$tmp_request" "$tmp_body" "$tmp_headers"
        return $curl_status
    fi

    local http_status content_type
    http_status="$(awk 'toupper($0) ~ /^HTTP\// { code = $2 } END { print code }' "$tmp_headers" 2>/dev/null)"
    if [ -n "$http_status" ] && [ "$http_status" -ge 400 ] 2>/dev/null; then
        printf 'type: error\npayload:\n'
        printf '  code: http_error\n'
        printf '  message: session log import HTTP fallback returned HTTP %s\n' "$http_status"
        printf '  details:\n'
        printf '    responseBody: |\n'
        sed 's/^/      /' "$tmp_body"
        printf '\n'
        rm -f "$tmp_request" "$tmp_body" "$tmp_headers"
        return 1
    fi

    content_type="$(grep -i '^content-type:' "$tmp_headers" 2>/dev/null | head -1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//' | tr -d '\r')"
    content_type="${content_type%%;*}"
    [ -z "$content_type" ] && content_type="application/json"
    printf 'type: result\npayload:\n'
    printf '  result: |\n'
    sed 's/^/    /' "$tmp_body"
    printf '\n'
    printf '  contentType: %s\n' "$content_type"
    rm -f "$tmp_request" "$tmp_body" "$tmp_headers"
    return 0
}

_repl_workflow_import_recovery() {
    local params_yaml="${1:-}"
    local response status previous_timeout="${REPL_TIMEOUT:-}"

    response="$(_repl_sessionlog_import_recovery_http_fallback "$params_yaml" 2>&1)"
    status=$?
    if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
        printf '%s\n' "$response"
        return 0
    fi

    export REPL_TIMEOUT="${REPL_SESSIONLOG_REPL_TIMEOUT:-8}"
    response="$(_repl_invoke_raw_in_workspace "workflow.sessionlog.importRecovery" "$params_yaml" "compat" 2>&1)"
    status=$?
    if [ $status -ne 0 ] || ! _repl_response_is_nonempty_success "$response"; then
        response="$(_repl_invoke_raw_in_workspace "workflow.sessionlog.importRecovery" "$params_yaml" 2>&1)"
        status=$?
    fi
    if [ -n "$previous_timeout" ]; then
        export REPL_TIMEOUT="$previous_timeout"
    else
        unset REPL_TIMEOUT
    fi

    printf '%s\n' "$response"
    return $status
}

_repl_pending_import_todo_exists() {
    local todo_id="$1"
    local response
    [ -z "$todo_id" ] && return 1

    response="$(repl_invoke "workflow.todo.query" "id: ${todo_id}" 2>/dev/null || true)"
    printf '%s\n' "$response" | grep -Eq "\"id\"[[:space:]]*:[[:space:]]*\"${todo_id}\"|id:[[:space:]]*${todo_id}"
}

_repl_pending_import_file() {
    local import_file="$1"
    local plan status method params_b64 label params response todo_id
    local imported=0 skipped=0 failed=0 details=""

    [ -f "$import_file" ] || {
        printf 'type: error\npayload:\n'
        printf '  code: import_file_not_found\n'
        printf '  message: pending import file not found: %s\n' "$import_file"
        return 1
    }

    if ! command -v node >/dev/null 2>&1; then
        printf 'type: error\npayload:\n'
        printf '  code: node_not_found\n'
        printf '  message: node is required to normalize pending MCP import JSON into YAML REPL commands\n'
        return 1
    fi

    plan="$(node "${REPL_INVOKE_SCRIPT_DIR}/pending-import-to-yaml.js" "$import_file" 2>&1)"
    status=$?
    if [ $status -ne 0 ]; then
        printf 'type: error\npayload:\n'
        printf '  code: import_plan_failed\n'
        printf '  message: pending import normalization failed\n'
        printf '  details:\n'
        printf '    stderr: |\n'
        printf '%s\n' "$plan" | sed 's/^/      /'
        return $status
    fi

    while IFS=$'\t' read -r method params_b64 label; do
        [ -z "$method" ] && continue
        params="$(printf '%s' "$params_b64" | base64 -d 2>/dev/null || true)"
        if [ -z "$params" ]; then
            failed=$((failed + 1))
            details="${details}
    - ${label:-$method}: failed to decode YAML params"
            continue
        fi

        if [ "$method" = "workflow.todo.create" ]; then
            todo_id="$(_repl_yaml_get "$params" "id" 2>/dev/null || true)"
            if _repl_pending_import_todo_exists "$todo_id"; then
                skipped=$((skipped + 1))
                details="${details}
    - ${label:-$method}: skipped existing TODO ${todo_id}"
                continue
            fi
        fi

        response="$(repl_invoke "$method" "$params" 2>&1)"
        status=$?
        if [ $status -eq 0 ] && _repl_response_is_nonempty_success "$response"; then
            imported=$((imported + 1))
            details="${details}
    - ${label:-$method}: imported via ${method}"
        else
            failed=$((failed + 1))
            details="${details}
    - ${label:-$method}: failed via ${method}: $(printf '%s' "$response" | tr '\n' ' ' | cut -c1-240)"
        fi
    done <<EOF
${plan}
EOF

    if [ $failed -gt 0 ]; then
        printf 'type: error\npayload:\n'
        printf '  code: pending_import_failed\n'
        printf '  message: pending import replay failed for %s command(s)\n' "$failed"
    else
        printf 'type: result\npayload:\n'
    fi
    printf '  result:\n'
    printf '    file: %s\n' "$import_file"
    printf '    imported: !!int %s\n' "$imported"
    printf '    skipped: !!int %s\n' "$skipped"
    printf '    failed: !!int %s\n' "$failed"
    if [ -n "$details" ]; then
        printf '    details:\n'
        printf '%s\n' "$details"
    fi

    [ $failed -eq 0 ]
}

_repl_workflow_import_pending() {
    local params_yaml="${1:-}"
    local import_path import_dir status response
    local imported=0 skipped=0 failed=0 details=""

    import_path="$(_repl_yaml_get "$params_yaml" "file" 2>/dev/null || true)"
    [ -z "$import_path" ] && import_path="$(_repl_yaml_get "$params_yaml" "path" 2>/dev/null || true)"
    import_path="$(_repl_unquote "$import_path")"

    if [ -n "$import_path" ] && [ -f "$import_path" ]; then
        _repl_pending_import_file "$import_path"
        return $?
    fi

    import_dir="$(_repl_yaml_get "$params_yaml" "directory" 2>/dev/null || true)"
    import_dir="$(_repl_unquote "$import_dir")"
    [ -z "$import_dir" ] && import_dir=".mcpServer"
    [ -d "$import_dir" ] || {
        printf 'type: error\npayload:\n'
        printf '  code: import_directory_not_found\n'
        printf '  message: pending import directory not found: %s\n' "$import_dir"
        return 1
    }

    while IFS= read -r import_path; do
        [ -z "$import_path" ] && continue
        response="$(_repl_pending_import_file "$import_path" 2>&1)"
        status=$?
        if [ $status -eq 0 ]; then
            imported=$((imported + 1))
        else
            failed=$((failed + 1))
        fi
        details="${details}
    - ${import_path}: $(printf '%s' "$response" | tr '\n' ' ' | cut -c1-240)"
    done < <(find "$import_dir" -type f -name '*.json' | sort)

    if [ $failed -gt 0 ]; then
        printf 'type: error\npayload:\n'
        printf '  code: pending_import_failed\n'
        printf '  message: pending import replay failed for %s file(s)\n' "$failed"
    else
        printf 'type: result\npayload:\n'
    fi
    printf '  result:\n'
    printf '    directory: %s\n' "$import_dir"
    printf '    importedFiles: !!int %s\n' "$imported"
    printf '    skippedFiles: !!int %s\n' "$skipped"
    printf '    failedFiles: !!int %s\n' "$failed"
    if [ -n "$details" ]; then
        printf '    details:\n'
        printf '%s\n' "$details"
    fi

    [ $failed -eq 0 ]
}

_repl_workflow_todo_select() {
    local params="$1"
    local todo_id state_file
    todo_id="$(_repl_yaml_get "$params" "id")"
    [ -z "$todo_id" ] && return 1

    mkdir -p "$REPL_INVOKE_CACHE_DIR"
    state_file="${REPL_INVOKE_CACHE_DIR}/todo-state.yaml"
    printf 'selectedTodoId: %s\n' "$todo_id" > "$state_file"
    _repl_emit_response "  id: ${todo_id}"
}

_repl_workflow_todo_update_selected() {
    local params="$1"
    local state_file="${REPL_INVOKE_CACHE_DIR}/todo-state.yaml"
    local todo_id
    todo_id="$(_repl_state_value "$state_file" "selectedTodoId")"
    [ -z "$todo_id" ] && return 1

    local combined="id: ${todo_id}"
    if [ -n "$params" ]; then
        combined="${combined}
${params}"
    fi

    _repl_workflow_todo "workflow.todo.update" "$combined"
}

_repl_workflow_todo_internal_tracking() {
    local params="$1"
    local requested="" mode source state_file tmp

    if [ -n "$params" ]; then
        requested="$(_repl_yaml_get "$params" "enabled" 2>/dev/null || true)"
        [ -z "$requested" ] && requested="$(_repl_yaml_get "$params" "mode" 2>/dev/null || true)"
        [ -z "$requested" ] && requested="$(_repl_yaml_get "$params" "mcpTodo" 2>/dev/null || true)"
        [ -z "$requested" ] && requested="$(_repl_yaml_get "$params" "mcpBacked" 2>/dev/null || true)"
    fi

    state_file="$(_repl_internal_todo_state_file)"
    if [ -n "$requested" ]; then
        mode="$(_repl_bool_to_enabled "$requested" 2>/dev/null || true)"
        if [ -z "$mode" ]; then
            printf 'type: error\npayload:\n'
            printf '  code: invalid_internal_todo_mode\n'
            printf '  message: internal TODO tracking mode must be enabled/disabled or true/false\n'
            return 1
        fi

        mkdir -p "$REPL_INVOKE_CACHE_DIR"
        tmp="${state_file}.tmp.$$"
        cat > "$tmp" <<EOF
enabled: ${mode}
updatedAt: $(_repl_now_iso)
EOF
        mv "$tmp" "$state_file"
    fi

    read -r mode source < <(_repl_internal_todo_mode_value)
    _repl_emit_response "  enabled: ${mode}
  source: ${source}
  stateFile: ${state_file}"
}

repl_invoke() {
    local method="$1"
    local params_yaml="${2:-}"

    _repl_schema_validate_method "$method" "$params_yaml" || return $?

    case "$method" in
        workflow.sessionlog.bootstrap)
            _repl_workflow_bootstrap "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.openSession)
            _repl_workflow_open_session "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.beginTurn)
            _repl_workflow_begin_turn "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.updateTurn)
            _repl_workflow_update_turn "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.appendDialog)
            _repl_workflow_append_dialog "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.appendActions)
            _repl_workflow_append_actions "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.completeTurn|workflow.sessionlog.failTurn)
            _repl_workflow_complete_turn "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.closeTurn)
            _repl_workflow_close_turn "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.audit)
            _repl_workflow_audit
            return $?
            ;;
        workflow.sessionlog.queryHistory|workflow.sessionlog.getHistory)
            _repl_workflow_query_history "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.closeSession)
            _repl_workflow_complete_turn "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.importRecovery)
            _repl_workflow_import_recovery "$params_yaml"
            return $?
            ;;
        workflow.sessionlog.importPending|workflow.pendingImport.replay)
            _repl_workflow_import_pending "$params_yaml"
            return $?
            ;;
        workflow.todo.query)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.todo.get)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.todo.create)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.todo.update)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.todo.delete)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.memory.list|workflow.memory.get|workflow.memory.add|workflow.memory.update|workflow.memory.remove)
            _repl_workflow_memory "$method" "$params_yaml"
            return $?
            ;;
        todo.query|todo.get|todo.create|todo.update|todo.delete)
            _repl_workflow_todo "workflow.${method}" "$params_yaml"
            return $?
            ;;
        workflow.todo.analyzeRequirements)
            _repl_workflow_todo "$method" "$params_yaml"
            return $?
            ;;
        workflow.todo.select)
            _repl_workflow_todo_select "$params_yaml"
            return $?
            ;;
        workflow.todo.updateSelected)
            _repl_workflow_todo_update_selected "$params_yaml"
            return $?
            ;;
        workflow.todo.internalTracking|workflow.todo.internal.status)
            _repl_workflow_todo_internal_tracking "$params_yaml"
            return $?
            ;;
        workflow.todo.internal.enable)
            _repl_workflow_todo_internal_tracking "enabled: true"
            return $?
            ;;
        workflow.todo.internal.disable)
            _repl_workflow_todo_internal_tracking "enabled: false"
            return $?
            ;;
        workflow.requirements.*)
            _repl_workflow_requirements "$method" "$params_yaml"
            return $?
            ;;
        workflow.graphrag.*)
            _repl_invoke_raw_in_workspace "$method" "$params_yaml"
            return $?
            ;;
    esac

    _repl_invoke_raw "$method" "$params_yaml"
}

repl_build_envelope() {
    local method="$1"
    local params_yaml="${2:-}"
    local request_id="req-$(_repl_now_compact)-$(printf '%04x' $RANDOM)"

    local pjson="null"
    if [ -n "$params_yaml" ]; then
        if [[ "$params_yaml" == \{* ]] || [[ "$params_yaml" == \[* ]]; then pjson="$params_yaml"; else pjson=$(node -e 'console.log(JSON.stringify(process.argv[1]))' "$params_yaml" 2>/dev/null || echo "null"); fi
    fi
    node -e '
      const p = process.argv[3] && process.argv[3]!=="null" ? JSON.parse(process.argv[3]) : undefined;
      console.log(JSON.stringify({type:"request", payload:{requestId:process.argv[1], method:process.argv[2], ...(p?{params:p}:{}) }}));
    ' "$request_id" "$method" "$pjson"
}

_repl_read_stdin_if_ready() {
    if [ -t 0 ]; then
        return 0
    fi

    if read -r -t 0; then
        cat 2>/dev/null | tr -d '\r' || true
    fi
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    method="${1:-}"
    if [ -z "$method" ]; then
        echo "usage: repl-invoke.sh <method> [params_yaml_from_stdin]" >&2
        exit 64
    fi

    params_yaml="$(_repl_read_stdin_if_ready)"
    if [ -z "$params_yaml" ] && [ $# -ge 2 ]; then
        params_yaml="$2"
    fi
    repl_invoke "$method" "$params_yaml"
    # Exit 0 so that type: error envelopes (which are valid responses) are not masked by
    # wrapper callers that throw on non-zero. Usage error (64) is handled before.
    exit 0
fi

export -f _repl_read_stdin_if_ready 2>/dev/null || true
export -f _repl_turn_upsert_params 2>/dev/null || true

export -f repl_invoke repl_build_envelope _repl_bool_to_enabled _repl_compat_marker_endpoint_field _repl_compat_marker_field _repl_create_compat_marker _repl_failsafe_clear _repl_failsafe_dir _repl_failsafe_plugin_name _repl_failsafe_workspace_root _repl_failsafe_write _repl_first_param_text _repl_internal_todo_is_enabled _repl_internal_todo_mode_value _repl_internal_todo_state_file _repl_invoke_raw _repl_invoke_raw_in_workspace _repl_invoke_with_fallback _repl_bootstrap_state _repl_emit_response _repl_generate_session_id _repl_json_array_block_get _repl_json_escape _repl_normalized_actions_block _repl_normalized_dialog_items_block _repl_param_text _repl_path_for_bash _repl_path_for_repl _repl_pending_import_file _repl_pending_import_todo_exists _repl_persist_turn _repl_records_block_get _repl_records_block_normalize _repl_requirements_acceptance_criteria_result_ok _repl_requirements_bootstrap_state _repl_requirements_copy_acceptance_http_fallback _repl_requirements_existing_for_update _repl_requirements_generate_http_fallback _repl_requirements_normalize_generate_response _repl_requirements_typed_doc_type _repl_requirements_typed_method _repl_requirements_typed_params _repl_requirements_update_get_method _repl_requirements_update_workflow_get_method _repl_requirements_workflow_doc_type _repl_requirements_workflow_params _repl_requirement_list_field _repl_response_has_empty_result _repl_response_is_error _repl_response_is_nonempty_success _repl_run_repl_with_timeout _repl_session_meta _repl_session_state_value _repl_sessionlog_import_recovery_http_fallback _repl_sessionlog_submit_http_fallback _repl_state_value _repl_submit_session _repl_todo_http_fallback _repl_todo_json_body _repl_turns_block _repl_url_path_segment _repl_turn_bump _repl_count_lines _repl_turn_has_audit_schema _repl_turn_audit_missing _repl_workflow_audit _repl_workflow_close_turn _repl_workflow_append_actions _repl_workflow_append_dialog _repl_workflow_begin_turn _repl_workflow_bootstrap _repl_workflow_complete_turn _repl_workflow_import_pending _repl_workflow_import_recovery _repl_workflow_memory _repl_workflow_memory_is_mutation _repl_workflow_memory_append_action _repl_workflow_open_session _repl_workflow_query_history _repl_workflow_requirements _repl_workflow_requirements_is_mutation _repl_workflow_requirements_prefers_typed _repl_workflow_todo _repl_workflow_todo_internal_tracking _repl_workflow_todo_is_mutation _repl_workflow_todo_select _repl_workflow_todo_update_selected _repl_workflow_update_turn _repl_emit_acceptance_criteria_block _repl_emit_acceptance_criteria_hydrate _repl_has_acceptance_criteria_block _repl_yaml_block_get _repl_yaml_field _repl_yaml_get 2>/dev/null || true
