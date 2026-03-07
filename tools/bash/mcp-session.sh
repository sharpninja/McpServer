#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# MCP Session Log — Bash helper functions for the /mcpserver/sessionlog API.
#
# Usage:
#   source ./mcp-session.sh
#   mcp_session_init                                  # reads marker, sets vars
#   mcp_session_create "Copilot" "My session" "claude-sonnet-4"  # creates session
#   mcp_session_add_turn "req-001" "Fix bug" "Fix the auth bug" "in_progress"
#   mcp_session_send_dialog "req-001" "Analyzing the issue..." "reasoning"
#   mcp_session_update                                # pushes to server
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

# ─── State ───────────────────────────────────────────────────────────────────
MCP_BASE_URL=""
MCP_API_KEY=""
MCP_WORKSPACE_PATH=""
MCP_SESSION_FILE=""   # temp file holding the session JSON

# ─── Connection ──────────────────────────────────────────────────────────────

mcp_session_init() {
    # Usage: mcp_session_init [marker_path]
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

    MCP_BASE_URL=$(grep -oP 'baseUrl:\s*\K\S+' "$marker")
    MCP_API_KEY=$(grep -oP 'apiKey:\s*\K\S+' "$marker")
    MCP_WORKSPACE_PATH=$(grep -oP 'workspacePath:\s*\K.+' "$marker" | sed 's/[[:space:]]*$//' || true)
    if [[ -z "$MCP_WORKSPACE_PATH" ]]; then
        MCP_WORKSPACE_PATH=$(dirname "$marker")
    fi
    MCP_SESSION_FILE=$(mktemp /tmp/mcp-session-XXXXXX.json)

    # Verify connectivity
    local health
    if health=$(curl -sf "${MCP_BASE_URL}/health" 2>/dev/null); then
        echo "Connected to MCP server at ${MCP_BASE_URL}"
    else
        echo "WARNING: MCP server at ${MCP_BASE_URL} is not responding" >&2
    fi
}

# ─── Session CRUD ────────────────────────────────────────────────────────────

mcp_session_create() {
    # Usage: mcp_session_create <sourceType> <title> <model> [sessionId]
    local source_type="$1" title="$2" model="$3"
    local session_id="${4:-${source_type}-$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid)}"
    local now; now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    cat > "$MCP_SESSION_FILE" <<EOF
{
  "sourceType": "${source_type}",
  "sessionId": "${session_id}",
  "title": "${title}",
  "model": "${model}",
  "started": "${now}",
  "lastUpdated": "${now}",
  "status": "in_progress",
  "entryCount": 0,
  "totalTokens": 0,
  "entries": []
}
EOF

    _mcp_session_push
    echo "$session_id"
}

mcp_session_update() {
    # Usage: mcp_session_update [status] [title]
    local status="${1:-}" title="${2:-}"
    local now; now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    local tmp; tmp=$(mktemp)
    jq --arg now "$now" \
       --arg status "$status" \
       --arg title "$title" \
       '
        .lastUpdated = $now
        | if $status != "" then .status = $status else . end
        | if $title != "" then .title = $title else . end
       ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push

    if [[ "$status" == "completed" ]]; then
        _mcp_session_delete_state
    fi
}

mcp_session_complete() {
    # Usage: mcp_session_complete [title]
    mcp_session_update "completed" "${1:-}"
}

mcp_session_query() {
    # Usage: mcp_session_query [limit]
    local limit="${1:-5}"
    curl -sf -H "X-Api-Key: ${MCP_API_KEY}" \
        -H "X-Workspace-Path: ${MCP_WORKSPACE_PATH}" \
        "${MCP_BASE_URL}/mcpserver/sessionlog?limit=${limit}"
}

# ─── Turns ───────────────────────────────────────────────────────────────────

mcp_session_add_turn() {
    # Usage: mcp_session_add_turn <requestId> <queryTitle> <queryText> <status> [response] [interpretation]
    local req_id="$1" query_title="$2" query_text="$3" status="$4"
    local response="${5:-}" interpretation="${6:-}"
    local now; now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    local model; model=$(jq -r '.model' "$MCP_SESSION_FILE")

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" \
       --arg ts "$now" \
       --arg qt "$query_text" \
       --arg qtt "$query_title" \
       --arg resp "$response" \
       --arg interp "$interpretation" \
       --arg st "$status" \
       --arg mdl "$model" \
       '
        .entries += [{
          requestId: $rid,
          timestamp: $ts,
          queryText: $qt,
          queryTitle: $qtt,
          response: $resp,
          interpretation: $interp,
          status: $st,
          model: $mdl,
          tags: [],
          contextList: [],
          designDecisions: [],
          requirementsDiscovered: [],
          filesModified: [],
          blockers: [],
          actions: [],
          processingDialog: []
        }]
       ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_update_turn() {
    # Usage: mcp_session_update_turn <requestId> <field> <value>
    # Fields: response, status, interpretation
    local req_id="$1" field="$2" value="$3"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" \
       --arg field "$field" \
       --arg value "$value" \
       '
        .entries |= map(
          if .requestId == $rid then .[$field] = $value else . end
        )
       ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_action() {
    # Usage: mcp_session_add_action <requestId> <description> <type> [filePath] [status]
    local req_id="$1" description="$2" action_type="$3"
    local file_path="${4:-}" status="${5:-completed}"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" \
       --arg desc "$description" \
       --arg atype "$action_type" \
       --arg fp "$file_path" \
       --arg st "$status" \
       '
        .entries |= map(
          if .requestId == $rid then
            .actions += [{
              order: ((.actions | length) + 1),
              description: $desc,
              type: $atype,
              status: $st,
              filePath: $fp
            }]
          else . end
        )
       ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_file() {
    # Usage: mcp_session_add_file <requestId> <filePath>
    local req_id="$1" file_path="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg fp "$file_path" \
       '.entries |= map(if .requestId == $rid then .filesModified += [$fp] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_tag() {
    # Usage: mcp_session_add_tag <requestId> <tag>
    local req_id="$1" tag="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg tag "$tag" \
       '.entries |= map(if .requestId == $rid then .tags += [$tag] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_context() {
    # Usage: mcp_session_add_context <requestId> <contextItem>
    local req_id="$1" context_item="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg item "$context_item" \
       '.entries |= map(if .requestId == $rid then .contextList += [$item] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_decision() {
    # Usage: mcp_session_add_decision <requestId> <decisionText>
    local req_id="$1" decision="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg decision "$decision" \
       '.entries |= map(if .requestId == $rid then .designDecisions += [$decision] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_requirement() {
    # Usage: mcp_session_add_requirement <requestId> <requirementIdOrText>
    local req_id="$1" requirement="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg requirement "$requirement" \
       '.entries |= map(if .requestId == $rid then .requirementsDiscovered += [$requirement] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

mcp_session_add_blocker() {
    # Usage: mcp_session_add_blocker <requestId> <blockerText>
    local req_id="$1" blocker="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg blocker "$blocker" \
       '.entries |= map(if .requestId == $rid then .blockers += [$blocker] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_push
}

# ─── Dialog ──────────────────────────────────────────────────────────────────

mcp_session_send_dialog() {
    # Usage: mcp_session_send_dialog <requestId> <content> [category] [role]
    local req_id="$1" content="$2"
    local category="${3:-reasoning}" role="${4:-model}"
    local now; now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    local source_type; source_type=$(jq -r '.sourceType' "$MCP_SESSION_FILE")
    local session_id; session_id=$(jq -r '.sessionId' "$MCP_SESSION_FILE")

    local body
    body=$(jq -n --arg ts "$now" --arg r "$role" --arg c "$content" --arg cat "$category" \
           '[{ timestamp: $ts, role: $r, content: $c, category: $cat }]')

    curl -sf -X POST \
        -H "X-Api-Key: ${MCP_API_KEY}" \
        -H "X-Workspace-Path: ${MCP_WORKSPACE_PATH}" \
        -H "Content-Type: application/json" \
        -d "$body" \
        "${MCP_BASE_URL}/mcpserver/sessionlog/${source_type}/${session_id}/${req_id}/dialog" \
        > /dev/null
}

# ─── Internal ────────────────────────────────────────────────────────────────

_mcp_session_push() {
    _mcp_session_normalize
    curl -sf -X POST \
        -H "X-Api-Key: ${MCP_API_KEY}" \
        -H "X-Workspace-Path: ${MCP_WORKSPACE_PATH}" \
        -H "Content-Type: application/json" \
        -d @"$MCP_SESSION_FILE" \
        "${MCP_BASE_URL}/mcpserver/sessionlog" \
        > /dev/null
}

_mcp_session_normalize() {
    local tmp; tmp=$(mktemp)
    jq '
        if (.entries | type) == "array" then .
        elif (.turns | type) == "array" then . + { entries: .turns }
        else . + { entries: [] }
        end
        | del(.turns)
        | .entryCount = (.entries | length)
        | .totalTokens = (([.entries[]?.tokenCount // 0] | add) // 0)
    ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"
}

_mcp_session_delete_state() {
    local workspace_path="${MCP_WORKSPACE_PATH:-$(pwd)}"
    local state_file="${workspace_path}/.mcpServer/session.yaml"
    rm -f "$state_file"
}
