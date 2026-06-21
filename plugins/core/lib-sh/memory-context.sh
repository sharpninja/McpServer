#!/usr/bin/env bash
# Shared required-memory context helpers for user prompt hooks.

mcp_json_escape() {
    awk '
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
    ' <<<"$1"
}

mcp_required_memory_items_from_response() {
    local response="${1:-}"
    local python_bin=""

    for candidate in python3 python; do
        if command -v "$candidate" >/dev/null 2>&1 && "$candidate" -c 'import yaml' >/dev/null 2>&1; then
            python_bin="$candidate"
            break
        fi
    done

    if [ -n "$python_bin" ]; then
        MCP_MEMORY_RESPONSE="$response" "$python_bin" - <<'PY' 2>/dev/null && return 0
import json
import os
import sys
import yaml

text = os.environ.get("MCP_MEMORY_RESPONSE", "")
try:
    data = yaml.safe_load(text) if text.strip() else {}
except Exception:
    sys.exit(1)

payload = data.get("payload", {}) if isinstance(data, dict) else {}
result = payload.get("result", payload) if isinstance(payload, dict) else payload
if isinstance(result, str):
    stripped = result.strip()
    if stripped:
        try:
            result = json.loads(stripped)
        except Exception:
            try:
                result = yaml.safe_load(stripped)
            except Exception:
                result = {}
items = []
if isinstance(result, dict):
    items = result.get("items") or result.get("Items") or []
elif isinstance(result, list):
    items = result

for item in items:
    if not isinstance(item, dict):
        continue
    memory_id = item.get("id") or item.get("Id")
    text_value = item.get("text") or item.get("Text")
    if not memory_id or text_value is None:
        continue
    lines = str(text_value).replace("\r\n", "\n").replace("\r", "\n").split("\n")
    print(f"- {memory_id}: {lines[0] if lines else ''}")
    for line in lines[1:]:
        print(f"  {line}")
PY
    fi

    awk '
        function trim(value) {
            sub(/^[[:space:]]+/, "", value)
            sub(/[[:space:]]+$/, "", value)
            return value
        }
        function unquote(value) {
            value = trim(value)
            if (value ~ /^".*"$/ || value ~ /^\047.*\047$/) {
                value = substr(value, 2, length(value) - 2)
            }
            return value
        }
        function flush() {
            if (id != "") {
                print "- " id ": " text
            }
            id = ""
            text = ""
        }
        /^[[:space:]]*-?[[:space:]]*[Ii]d:[[:space:]]*/ {
            flush()
            line = $0
            sub(/^[[:space:]]*-?[[:space:]]*[Ii]d:[[:space:]]*/, "", line)
            id = unquote(line)
            next
        }
        /^[[:space:]]*[Tt]ext:[[:space:]]*/ {
            line = $0
            sub(/^[[:space:]]*[Tt]ext:[[:space:]]*/, "", line)
            text = unquote(line)
            next
        }
        END { flush() }
    ' <<<"$response"
}

mcp_required_memory_context() {
    local response="" items="" previous_timeout

    if type repl_invoke >/dev/null 2>&1; then
        previous_timeout="${REPL_TIMEOUT:-}"
        export REPL_TIMEOUT="${REPL_MEMORY_REPL_TIMEOUT:-8}"
        response="$(repl_invoke "workflow.memory.list" "scope: Effective" 2>/dev/null || true)"
        if [ -n "$previous_timeout" ]; then
            export REPL_TIMEOUT="$previous_timeout"
        else
            unset REPL_TIMEOUT
        fi
    fi

    items="$(mcp_required_memory_items_from_response "$response" 2>/dev/null || true)"
    printf 'REQUIRED MEMORIES\n'
    if [ -n "$items" ]; then
        printf '%s\n' "$items"
    else
        printf -- '- None.\n'
    fi
}
