#!/usr/bin/env bash
if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    set -euo pipefail
fi

# Functions for marker file discovery and verification.
# Source this file: source "$(dirname "$0")/marker-resolver.sh"

MARKER_FILENAME="AGENTS-README-FIRST.yaml"
MAX_WALK_DEPTH=20

# find_marker_file [start_dir]
# Walk up from start_dir (default: CWD) looking for AGENTS-README-FIRST.yaml.
# Echoes the full path on success, returns 1 if not found.
find_marker_file() {
    local dir="${1:-$(pwd)}"
    local depth=0
    while [ "$dir" != "/" ] && [ "$dir" != "" ] && [ "$depth" -lt "$MAX_WALK_DEPTH" ]; do
        if [ -f "$dir/$MARKER_FILENAME" ]; then
            echo "$dir/$MARKER_FILENAME"
            return 0
        fi
        dir="$(dirname "$dir")"
        depth=$((depth + 1))
    done
    # Check root as well
    if [ -f "/$MARKER_FILENAME" ]; then
        echo "/$MARKER_FILENAME"
        return 0
    fi
    return 1
}

# parse_marker_field <marker_file> <field_name>
# Extract a top-level YAML field value using grep/sed (no yq dependency).
# Handles both top-level fields (apiKey: value) and nested endpoint fields (  health: /health).
parse_marker_field() {
    local marker_file="$1"
    local field_name="$2"

    # Try top-level field first. `tr -d '\r'` strips CR for CRLF marker files.
    local value
    value=$(grep "^${field_name}:" "$marker_file" 2>/dev/null | head -1 | tr -d '\r' | sed "s/^${field_name}:[[:space:]]*//" | sed 's/^"\(.*\)"$/\1/' | sed "s/^'\(.*\)'$/\1/")

    if [ -n "$value" ]; then
        echo "$value"
        return 0
    fi

    # Try nested endpoint field (indented under endpoints:)
    value=$(sed -n '/^endpoints:/,/^[^ ]/p' "$marker_file" 2>/dev/null \
        | grep "^[[:space:]]*${field_name}:" \
        | head -1 \
        | tr -d '\r' \
        | sed "s/^[[:space:]]*${field_name}:[[:space:]]*//" \
        | sed 's/^"\(.*\)"$/\1/' \
        | sed "s/^'\(.*\)'$/\1/")

    if [ -n "$value" ]; then
        echo "$value"
        return 0
    fi

    return 1
}

# verify_signature <marker_file>
# Compute HMAC-SHA256 of canonical payload, compare to marker signature value.
verify_signature() {
    local marker_file="$1"

    local api_key port base_url workspace workspace_path pid started_at marker_written server_started

    api_key=$(parse_marker_field "$marker_file" "apiKey")
    port=$(parse_marker_field "$marker_file" "port")
    base_url=$(parse_marker_field "$marker_file" "baseUrl")
    workspace=$(parse_marker_field "$marker_file" "workspace")
    workspace_path=$(parse_marker_field "$marker_file" "workspacePath")
    pid=$(parse_marker_field "$marker_file" "pid")
    started_at=$(parse_marker_field "$marker_file" "startedAt")
    marker_written=$(parse_marker_field "$marker_file" "markerWrittenAtUtc")
    server_started=$(parse_marker_field "$marker_file" "serverStartedAtUtc")

    # Build canonical payload (marker-v1 format)
    local payload=""
    payload+="canonicalization=marker-v1"$'\n'
    payload+="port=${port}"$'\n'
    payload+="baseUrl=${base_url}"$'\n'
    payload+="apiKey=${api_key}"$'\n'
    payload+="workspace=${workspace}"$'\n'
    payload+="workspacePath=${workspace_path}"$'\n'
    payload+="pid=${pid}"$'\n'
    payload+="startedAt=${started_at}"$'\n'
    payload+="markerWrittenAtUtc=${marker_written}"$'\n'
    payload+="serverStartedAtUtc=${server_started}"$'\n'

    # Extract endpoints section and build payload lines.
    # Strip CR before matching so CRLF marker files parse correctly.
    local in_endpoints=false
    while IFS= read -r line; do
        line="${line%$'\r'}"
        if [[ "$line" =~ ^endpoints: ]]; then
            in_endpoints=true
            continue
        fi
        if $in_endpoints && [[ "$line" =~ ^[^[:space:]] ]]; then
            break
        fi
        if $in_endpoints && [[ "$line" =~ ^[[:space:]]+([^:]+):[[:space:]]*(.*) ]]; then
            local key="${BASH_REMATCH[1]}"
            local val="${BASH_REMATCH[2]}"
            # Trim whitespace
            key=$(echo "$key" | sed 's/^[[:space:]]*//' | sed 's/[[:space:]]*$//')
            val=$(echo "$val" | sed 's/^[[:space:]]*//' | sed 's/[[:space:]]*$//')
    payload+="endpoints.${key}=${val}"$'\n'
        fi
    done < "$marker_file"

    # Include the signed agent plugin contract digest when present. Older
    # markers do not have agent_plugins and remain valid without these lines.
    local agent_plugins_policy agent_plugins_digest
    agent_plugins_policy=$(sed -n '/^agent_plugins:/,/^[^ ]/p' "$marker_file" 2>/dev/null \
        | grep "^[[:space:]]*policy:" \
        | head -1 \
        | tr -d '\r' \
        | sed "s/^[[:space:]]*policy:[[:space:]]*//" \
        | sed 's/^"\(.*\)"$/\1/' \
        | sed "s/^'\(.*\)'$/\1/")
    agent_plugins_digest=$(sed -n '/^agent_plugins:/,/^[^ ]/p' "$marker_file" 2>/dev/null \
        | grep "^[[:space:]]*contract_digest:" \
        | head -1 \
        | tr -d '\r' \
        | sed "s/^[[:space:]]*contract_digest:[[:space:]]*//" \
        | sed 's/^"\(.*\)"$/\1/' \
        | sed "s/^'\(.*\)'$/\1/")
    if [ -n "$agent_plugins_policy" ] || [ -n "$agent_plugins_digest" ]; then
        payload+="agentPlugins.policy=${agent_plugins_policy}"$'\n'
        payload+="agentPlugins.contractDigest=${agent_plugins_digest}"$'\n'
    fi

    # Extract stored signature value
    local stored_signature=""
    local in_signature=false
    while IFS= read -r line; do
        if [[ "$line" =~ ^signature: ]]; then
            in_signature=true
            continue
        fi
        if $in_signature && [[ "$line" =~ ^[^[:space:]] ]]; then
            break
        fi
        if $in_signature && [[ "$line" =~ ^[[:space:]]+value:[[:space:]]*(.*) ]]; then
            stored_signature="${BASH_REMATCH[1]}"
            # Strip trailing CR (CRLF marker files) and whitespace
            stored_signature="${stored_signature%$'\r'}"
            stored_signature="${stored_signature%%[[:space:]]}"
        fi
    done < "$marker_file"

    if [ -z "$stored_signature" ]; then
        echo "ERROR: No signature value found in marker file" >&2
        return 1
    fi

    # Compute HMAC-SHA256
    local computed
    computed=$(echo -n "$payload" | openssl dgst -sha256 -hmac "$api_key" -hex 2>/dev/null | awk '{print toupper($NF)}')

    if [ "$computed" = "$stored_signature" ]; then
        return 0
    else
        echo "ERROR: Signature mismatch (computed=$computed, stored=$stored_signature)" >&2
        return 1
    fi
}

# full_bootstrap [start_dir]
# Orchestrate: find marker -> parse -> verify signature -> health nonce check
full_bootstrap() {
    local start_dir="${1:-$(pwd)}"

    local marker_file
    marker_file=$(find_marker_file "$start_dir") || {
        echo "MCP_UNTRUSTED: No marker file found" >&2
        return 1
    }

    verify_signature "$marker_file" || {
        echo "MCP_UNTRUSTED: Signature verification failed" >&2
        return 1
    }

    # Parse base URL for health check
    local base_url
    base_url=$(parse_marker_field "$marker_file" "baseUrl")

    # Health nonce check
    local nonce="nonce-$(date +%s)-$$"
    local health_response
    health_response=$(curl -sf --max-time "${MCP_PLUGIN_HEALTH_TIMEOUT_SECONDS:-5}" "${base_url}/health?nonce=${nonce}" 2>/dev/null) || {
        echo "MCP_UNTRUSTED: Health check failed" >&2
        return 1
    }

    if ! echo "$health_response" | grep -q "\"nonce\":\"${nonce}\""; then
        echo "MCP_UNTRUSTED: Nonce verification failed" >&2
        return 1
    fi

    # Export resolved values for caller
    export MCPSERVER_BASE_URL="$base_url"
    export MCPSERVER_API_KEY="$(parse_marker_field "$marker_file" "apiKey")"
    export MCPSERVER_WORKSPACE="$(parse_marker_field "$marker_file" "workspace")"
    export MCPSERVER_WORKSPACE_PATH="$(parse_marker_field "$marker_file" "workspacePath")"

    return 0
}
