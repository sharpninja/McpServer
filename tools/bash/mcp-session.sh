#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# MCP Session Log — Bash helper functions for the /mcpserver/sessionlog API.
#
# Usage:
#   source ./mcp-session.sh
#   mcp_session_init "Copilotcli" "gpt-5.3-codex"      # reads marker, sets vars, persists/reuses session slug
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
MCP_SESSION_STATE_FILE=""
MCP_SESSION_AGENT=""
MCP_SESSION_MODEL=""
MCP_SESSION_SLUG=""

# ─── Connection ──────────────────────────────────────────────────────────────

mcp_session_init() {
    # Usage: mcp_session_init <agent> <model> [marker_path]
    local agent="${1:-}"
    local model="${2:-}"
    local marker="${3:-}"

    if [[ -z "$agent" || -z "$model" ]]; then
        echo "ERROR: mcp_session_init requires <agent> and <model>." >&2
        return 1
    fi

    MCP_SESSION_AGENT="$agent"
    MCP_SESSION_MODEL="$model"

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
    MCP_SESSION_STATE_FILE="${MCP_WORKSPACE_PATH}/.mcpServer/session.yaml"

    # Verify connectivity
    local health
    if health=$(curl -sf "${MCP_BASE_URL}/health" 2>/dev/null); then
        echo "Connected to MCP server at ${MCP_BASE_URL}"
    else
        echo "WARNING: MCP server at ${MCP_BASE_URL} is not responding" >&2
    fi

    MCP_SESSION_SLUG=$(_mcp_session_resolve_slug)
    _mcp_session_load_from_state
    echo "$MCP_SESSION_SLUG"
}

# ─── Session CRUD ────────────────────────────────────────────────────────────

mcp_session_create() {
    # Usage: mcp_session_create <sourceType> <title> <model> [sessionId]
    local source_type="$1" title="$2" model="$3"
    local session_id="${4:-${MCP_SESSION_SLUG:-${source_type}-$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid)}}"
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
  "turnCount": 0,
  "totalTokens": 0,
  "turns": []
}
EOF

    _mcp_session_persist_state
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

    _mcp_session_persist_state
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
    _mcp_session_require_local
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
        .turns += [{
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

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_update_turn() {
    # Usage: mcp_session_update_turn <requestId> <field> <value>
    # Fields: response, status, interpretation
    _mcp_session_require_local
    local req_id="$1" field="$2" value="$3"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" \
       --arg field "$field" \
       --arg value "$value" \
       '
        .turns |= map(
          if .requestId == $rid then .[$field] = $value else . end
        )
       ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_action() {
    # Usage: mcp_session_add_action <requestId> <description> <type> [filePath] [status]
    _mcp_session_require_local
    local req_id="$1" description="$2" action_type="$3"
    local file_path="${4:-}" status="${5:-completed}"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" \
       --arg desc "$description" \
       --arg atype "$action_type" \
       --arg fp "$file_path" \
       --arg st "$status" \
       '
        .turns |= map(
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

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_file() {
    # Usage: mcp_session_add_file <requestId> <filePath>
    _mcp_session_require_local
    local req_id="$1" file_path="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg fp "$file_path" \
       '.turns |= map(if .requestId == $rid then .filesModified += [$fp] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_tag() {
    # Usage: mcp_session_add_tag <requestId> <tag>
    _mcp_session_require_local
    local req_id="$1" tag="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg tag "$tag" \
       '.turns |= map(if .requestId == $rid then .tags += [$tag] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_context() {
    # Usage: mcp_session_add_context <requestId> <contextItem>
    _mcp_session_require_local
    local req_id="$1" context_item="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg item "$context_item" \
       '.turns |= map(if .requestId == $rid then .contextList += [$item] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_decision() {
    # Usage: mcp_session_add_decision <requestId> <decisionText>
    _mcp_session_require_local
    local req_id="$1" decision="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg decision "$decision" \
       '.turns |= map(if .requestId == $rid then .designDecisions += [$decision] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_requirement() {
    # Usage: mcp_session_add_requirement <requestId> <requirementIdOrText>
    _mcp_session_require_local
    local req_id="$1" requirement="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg requirement "$requirement" \
       '.turns |= map(if .requestId == $rid then .requirementsDiscovered += [$requirement] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

mcp_session_add_blocker() {
    # Usage: mcp_session_add_blocker <requestId> <blockerText>
    _mcp_session_require_local
    local req_id="$1" blocker="$2"

    local tmp; tmp=$(mktemp)
    jq --arg rid "$req_id" --arg blocker "$blocker" \
       '.turns |= map(if .requestId == $rid then .blockers += [$blocker] else . end)' \
       "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"

    _mcp_session_persist_state
    _mcp_session_push
}

# ─── Dialog ──────────────────────────────────────────────────────────────────

mcp_session_send_dialog() {
    # Usage: mcp_session_send_dialog <requestId> <content> [category] [role]
    _mcp_session_require_local
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
    _mcp_session_require_local
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
        if (.turns | type) == "array" then .
        elif (.entries | type) == "array" then . + { turns: .entries }
        else . + { turns: [] }
        end
        | del(.entries)
        | del(.entryCount)
        | .turnCount = (.turns | length)
        | .totalTokens = (([.turns[]?.tokenCount // 0] | add) // 0)
    ' "$MCP_SESSION_FILE" > "$tmp" && mv "$tmp" "$MCP_SESSION_FILE"
}

_mcp_session_delete_state() {
    local workspace_path="${MCP_WORKSPACE_PATH:-$(pwd)}"
    local state_file="${workspace_path}/.mcpServer/session.yaml"
    rm -f "$state_file"
}

_mcp_session_require_local() {
    if [[ -z "$MCP_SESSION_FILE" ]]; then
        echo "ERROR: Session helper not initialized. Call mcp_session_init first." >&2
        return 1
    fi

    if [[ ! -f "$MCP_SESSION_FILE" ]]; then
        _mcp_session_load_from_state
    fi

    if [[ ! -f "$MCP_SESSION_FILE" ]]; then
        echo "ERROR: No session found. Create one with mcp_session_create first." >&2
        return 1
    fi
}

_mcp_session_resolve_slug() {
    local now_ts now_epoch
    now_ts=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    now_epoch=$(date -u +"%s")

    if [[ -f "$MCP_SESSION_STATE_FILE" ]]; then
        local existing_slug existing_agent existing_model existing_key slug_generated_at reuse
        existing_slug=$(jq -r '.slug // ""' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)
        existing_agent=$(jq -r '.agent // ""' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)
        existing_model=$(jq -r '.model // ""' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)
        existing_key=$(jq -r '.apiKey // ""' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)
        slug_generated_at=$(jq -r '.slugGeneratedAt // ""' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)
        reuse="false"

        if [[ -n "$existing_slug" && "$existing_agent" == "$MCP_SESSION_AGENT" && "$existing_model" == "$MCP_SESSION_MODEL" ]]; then
            if [[ "$existing_key" == "$MCP_API_KEY" ]]; then
                reuse="true"
            elif [[ -n "$slug_generated_at" ]]; then
                local slug_epoch age_seconds
                slug_epoch=$(date -u -d "$slug_generated_at" +"%s" 2>/dev/null || echo 0)
                age_seconds=$((now_epoch - slug_epoch))
                if [[ $slug_epoch -gt 0 && $age_seconds -lt 3600 ]]; then
                    reuse="true"
                fi
            fi
        fi

        if [[ "$reuse" == "true" ]]; then
            jq -n \
                --arg apiKey "$MCP_API_KEY" \
                --arg agent "$MCP_SESSION_AGENT" \
                --arg model "$MCP_SESSION_MODEL" \
                --arg slug "$existing_slug" \
                --arg slugGeneratedAt "${slug_generated_at:-$now_ts}" \
                --argjson session "$(jq '.session // null' "$MCP_SESSION_STATE_FILE")" \
                '{ apiKey: $apiKey, agent: $agent, model: $model, slug: $slug, slugGeneratedAt: $slugGeneratedAt, session: $session }' \
                > "$MCP_SESSION_STATE_FILE"
            echo "$existing_slug"
            return 0
        fi
    fi

    local model_token generated_slug
    model_token=$(echo "$MCP_SESSION_MODEL" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//')
    if [[ -z "$model_token" ]]; then
        echo "ERROR: model '$MCP_SESSION_MODEL' did not produce a valid slug token." >&2
        return 1
    fi

    generated_slug="${MCP_SESSION_AGENT}-$(date -u +"%Y%m%dT%H%M%SZ")-${model_token}"
    mkdir -p "$(dirname "$MCP_SESSION_STATE_FILE")"
    jq -n \
        --arg apiKey "$MCP_API_KEY" \
        --arg agent "$MCP_SESSION_AGENT" \
        --arg model "$MCP_SESSION_MODEL" \
        --arg slug "$generated_slug" \
        --arg slugGeneratedAt "$now_ts" \
        '{ apiKey: $apiKey, agent: $agent, model: $model, slug: $slug, slugGeneratedAt: $slugGeneratedAt, session: null }' \
        > "$MCP_SESSION_STATE_FILE"
    echo "$generated_slug"
}

_mcp_session_load_from_state() {
    if [[ -f "$MCP_SESSION_STATE_FILE" ]]; then
        local has_session
        has_session=$(jq -r 'has("session") and (.session != null)' "$MCP_SESSION_STATE_FILE" 2>/dev/null || echo "false")
        if [[ "$has_session" == "true" ]]; then
            jq '.session' "$MCP_SESSION_STATE_FILE" > "$MCP_SESSION_FILE"
        fi
    fi
}

_mcp_session_persist_state() {
    if [[ -z "$MCP_SESSION_STATE_FILE" || ! -f "$MCP_SESSION_FILE" ]]; then
        return 0
    fi

    _mcp_session_normalize

    local now_ts
    now_ts=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    mkdir -p "$(dirname "$MCP_SESSION_STATE_FILE")"
    jq -n \
        --arg apiKey "$MCP_API_KEY" \
        --arg agent "$MCP_SESSION_AGENT" \
        --arg model "$MCP_SESSION_MODEL" \
        --arg slug "$MCP_SESSION_SLUG" \
        --arg slugGeneratedAt "$(jq -r '.slugGeneratedAt // empty' "$MCP_SESSION_STATE_FILE" 2>/dev/null || true)" \
        --arg now "$now_ts" \
        --argjson session "$(cat "$MCP_SESSION_FILE")" \
        '{ apiKey: $apiKey, agent: $agent, model: $model, slug: $slug, slugGeneratedAt: (if $slugGeneratedAt == "" then $now else $slugGeneratedAt end), session: $session }' \
        > "$MCP_SESSION_STATE_FILE"
}
