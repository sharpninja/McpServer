#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# MCP Todo — Bash helper functions for the /mcp/todo API.
#
# Usage:
#   source ./mcp-todo.sh
#   mcp_todo_init                                     # reads marker, sets vars
#   mcp_todo_list                                     # list all todos
#   mcp_todo_get "fix-auth"                           # get specific todo
#   mcp_todo_create "fix-auth" "Fix auth" "Backend" "high"
#   mcp_todo_update "fix-auth" '{"remaining":"Need tests"}'
#   mcp_todo_complete "fix-auth" "Auth fixed with JWT"
#   mcp_todo_delete "fix-auth"
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ─── State ───────────────────────────────────────────────────────────────────
MCP_TODO_BASE_URL=""
MCP_TODO_API_KEY=""

# ─── Connection ──────────────────────────────────────────────────────────────

mcp_todo_init() {
    # Usage: mcp_todo_init [marker_path]
    local marker="${1:-}"
    if [[ -z "$marker" ]]; then
        local dir; dir="$(pwd)"
        while [[ "$dir" != "/" ]]; do
            if [[ -f "$dir/AGENTS-README-FIRST.yaml" ]]; then
                marker="$dir/AGENTS-README-FIRST.yaml"
                break
            fi
            dir="$(dirname "$dir")"
        done
    fi

    if [[ -z "$marker" || ! -f "$marker" ]]; then
        echo "ERROR: AGENTS-README-FIRST.yaml not found." >&2
        return 1
    fi

    MCP_TODO_BASE_URL=$(grep -oP 'baseUrl:\s*\K\S+' "$marker")
    MCP_TODO_API_KEY=$(grep -oP 'apiKey:\s*\K\S+' "$marker")

    # Verify connectivity
    if curl -sf "${MCP_TODO_BASE_URL}/health" > /dev/null 2>&1; then
        echo "Connected to MCP server at ${MCP_TODO_BASE_URL}"
    else
        echo "WARNING: MCP server at ${MCP_TODO_BASE_URL} is not responding" >&2
    fi
}

# ─── Read ────────────────────────────────────────────────────────────────────

mcp_todo_list() {
    # Usage: mcp_todo_list
    curl -sf -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        "${MCP_TODO_BASE_URL}/mcp/todo"
}

mcp_todo_get() {
    # Usage: mcp_todo_get <id>
    local id="$1"
    curl -sf -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}"
}

mcp_todo_prompt() {
    # Usage: mcp_todo_prompt <id> <type>  (type: implement, plan, status)
    local id="$1" ptype="$2"
    curl -sf -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}/prompt/${ptype}"
}

# ─── Create ──────────────────────────────────────────────────────────────────

mcp_todo_create() {
    # Usage: mcp_todo_create <id> <title> <section> <priority> [description_json]
    # description_json: optional JSON string for additional fields, merged into request
    local id="$1" title="$2" section="$3" priority="$4"
    local extra="${5:-}"
    if [[ -z "$extra" ]]; then extra='{}'; fi

    local base
    base=$(jq -n --arg id "$id" --arg t "$title" --arg s "$section" --arg p "$priority" \
           '{ id: $id, title: $t, section: $s, priority: $p }')

    local body
    body=$(echo "$base" "$extra" | jq -s '.[0] * .[1]')

    curl -sf -X POST \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_TODO_BASE_URL}/mcp/todo"
}

mcp_todo_create_full() {
    # Usage: mcp_todo_create_full <json_body>
    # Pass complete TodoCreateRequest JSON
    local body="$1"
    curl -sf -X POST \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_TODO_BASE_URL}/mcp/todo"
}

# ─── Update ──────────────────────────────────────────────────────────────────

mcp_todo_update() {
    # Usage: mcp_todo_update <id> <json_body>
    # json_body: TodoUpdateRequest JSON with only the fields to change
    local id="$1" body="$2"
    curl -sf -X PUT \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}"
}

# ─── Complete ────────────────────────────────────────────────────────────────

mcp_todo_complete() {
    # Usage: mcp_todo_complete <id> <done_summary>
    local id="$1" summary="$2"
    local now; now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    local body
    body=$(jq -n --arg s "$summary" --arg d "$now" \
           '{ done: true, completedDate: $d, doneSummary: $s }')

    curl -sf -X PUT \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}"
}

# ─── Delete ──────────────────────────────────────────────────────────────────

mcp_todo_delete() {
    # Usage: mcp_todo_delete <id>
    local id="$1"
    curl -sf -X DELETE \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}"
    echo "Deleted todo: ${id}"
}

# ─── Requirements ────────────────────────────────────────────────────────────

mcp_todo_add_requirements() {
    # Usage: mcp_todo_add_requirements <id> <json_body>
    # json_body: { "functionalRequirements": [...], "technicalRequirements": [...] }
    local id="$1" body="$2"
    curl -sf -X POST \
        -H "X-Api-Key: ${MCP_TODO_API_KEY}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_TODO_BASE_URL}/mcp/todo/${id}/requirements"
}
