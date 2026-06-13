#!/usr/bin/env bash
set -uo pipefail

# Shared cache scoping for plugin runtime state. Runtime files live under:
#   cache/workspaces/<workspace-key>/sessions/<session-key>/
# Bootstrap-only state uses:
#   cache/workspaces/<workspace-key>/bootstrap/

CACHE_SCOPE_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Unified resolver: load the workspace-aware cache resolver up front (best
# effort) so cache_scope_init can anchor through it.
if ! type resolve_cache_dir >/dev/null 2>&1 && [ -f "$CACHE_SCOPE_SCRIPT_DIR/resolve-cache-dir.sh" ]; then
    # shellcheck source=./resolve-cache-dir.sh
    source "$CACHE_SCOPE_SCRIPT_DIR/resolve-cache-dir.sh" 2>/dev/null || true
fi

_cache_scope_unquote() {
    local value="${1:-}"
    value="$(printf '%s' "$value" | sed 's/^"\(.*\)"$/\1/; s/^'\''\(.*\)'\''$/\1/')"
    printf '%s' "$value"
}

_cache_scope_slugify() {
    local value="${1:-}"
    printf '%s' "$value" \
        | tr '[:upper:]' '[:lower:]' \
        | sed 's#[\\/:\"]#-#g; s/[^a-z0-9._=-]/-/g; s/-\{2,\}/-/g; s/^-//; s/-$//' \
        | cut -c1-48
}

_cache_scope_path_for_bash() {
    local path_value="$(_cache_scope_unquote "${1:-}")"
    if [ -z "$path_value" ]; then
        return 1
    fi

    if command -v cygpath >/dev/null 2>&1 && printf '%s' "$path_value" | grep -Eq '^[A-Za-z]:[\\/]'; then
        cygpath -u "$path_value"
        return $?
    fi

    printf '%s' "$path_value"
}

_cache_scope_normalize_path() {
    local path_value="$(_cache_scope_unquote "${1:-}")"
    local bash_path
    bash_path="$(_cache_scope_path_for_bash "$path_value" 2>/dev/null || printf '%s' "$path_value")"

    if [ -d "$bash_path" ]; then
        (cd "$bash_path" 2>/dev/null && pwd -P) || printf '%s' "$bash_path"
    else
        printf '%s' "$bash_path"
    fi
}

_cache_scope_hash() {
    local value="${1:-}"
    if command -v sha1sum >/dev/null 2>&1; then
        printf '%s' "$value" | sha1sum | awk '{print substr($1,1,12)}'
    elif command -v shasum >/dev/null 2>&1; then
        printf '%s' "$value" | shasum -a 1 | awk '{print substr($1,1,12)}'
    elif command -v openssl >/dev/null 2>&1; then
        printf '%s' "$value" | openssl dgst -sha1 2>/dev/null | awk '{print substr($NF,1,12)}'
    else
        printf '%s' "$value" | cksum | awk '{printf "%s%s", $1, $2}' | cut -c1-12
    fi
}

_cache_scope_basename() {
    local value="${1:-workspace}"
    value="$(printf '%s' "$value" | tr '\\' '/')"
    value="${value%/}"
    value="${value##*/}"
    [ -n "$value" ] || value="workspace"
    printf '%s' "$value"
}

_cache_scope_workspace_path_from_marker() {
    local start_dir="${1:-$(pwd)}"
    local dir
    dir="$(_cache_scope_path_for_bash "$start_dir" 2>/dev/null || printf '%s' "$start_dir")"

    local depth=0
    while [ "$dir" != "/" ] && [ "$dir" != "" ] && [ "$depth" -lt 20 ]; do
        local marker="$dir/AGENTS-README-FIRST.yaml"
        if [ -f "$marker" ]; then
            grep '^workspacePath:' "$marker" 2>/dev/null \
                | head -1 \
                | tr -d '\r' \
                | sed 's/^workspacePath:[[:space:]]*//' \
                | sed 's/^"\(.*\)"$/\1/' \
                | sed "s/^'\(.*\)'$/\1/"
            return 0
        fi
        dir="$(dirname "$dir")"
        depth=$((depth + 1))
    done

    return 1
}

cache_scope_workspace_path() {
    local start_dir="${1:-$(pwd)}"
    local workspace_path=""

    workspace_path="${MCPSERVER_WORKSPACE_PATH:-${MCP_WORKSPACE_PATH:-}}"
    if [ -z "$workspace_path" ]; then
        workspace_path="$(_cache_scope_workspace_path_from_marker "$start_dir" 2>/dev/null || true)"
    fi
    [ -n "$workspace_path" ] || workspace_path="$start_dir"

    _cache_scope_normalize_path "$workspace_path"
}

cache_scope_workspace_key() {
    local workspace_path
    workspace_path="$(_cache_scope_normalize_path "${1:-$(pwd)}")"

    local base slug hash canonical
    base="$(_cache_scope_basename "$workspace_path")"
    slug="$(_cache_scope_slugify "$base")"
    [ -n "$slug" ] || slug="workspace"
    canonical="$(printf '%s' "$workspace_path" | tr '[:upper:]' '[:lower:]')"
    hash="$(_cache_scope_hash "$canonical")"
    printf '%s-%s' "$slug" "$hash"
}

cache_scope_session_key() {
    local session_id="$(_cache_scope_unquote "${1:-}")"
    local slug hash canonical
    slug="$(_cache_scope_slugify "$session_id")"
    [ -n "$slug" ] || slug="session"
    canonical="$(printf '%s' "$session_id" | tr '[:upper:]' '[:lower:]')"
    hash="$(_cache_scope_hash "$canonical")"
    printf '%s-%s' "$slug" "$hash"
}

cache_scope_init() {
    local plugin_root="${1:-$(cd "$CACHE_SCOPE_SCRIPT_DIR/.." && pwd)}"
    local start_dir="${2:-$(pwd)}"
    local storage_root="${PLUGIN_ROOT_OVERRIDE:-$plugin_root}"

    # Unified resolver (Phase 2 shared core): anchor the cache root through
    # resolve_cache_dir (override / marker / workspace aware) so scoped
    # runtime state and cache-manager pending flushes share one tree. Falls
    # back to <storage_root>/cache when the resolver is unavailable.
    local base_cache=""
    if type resolve_cache_dir >/dev/null 2>&1; then
        base_cache="$(MCP_WORKSPACE_START_DIR="${MCP_WORKSPACE_START_DIR:-$start_dir}" MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-$storage_root}" resolve_cache_dir 2>/dev/null; true)"
    fi
    [ -n "$base_cache" ] || base_cache="${storage_root}/cache"

    MCP_PLUGIN_CACHE_ROOT="${base_cache}"
    MCP_PLUGIN_WORKSPACE_PATH="$(cache_scope_workspace_path "$start_dir")"
    MCP_PLUGIN_WORKSPACE_KEY="$(cache_scope_workspace_key "$MCP_PLUGIN_WORKSPACE_PATH")"
    MCP_PLUGIN_WORKSPACE_CACHE_DIR="${MCP_PLUGIN_CACHE_ROOT}/workspaces/${MCP_PLUGIN_WORKSPACE_KEY}"

    local session_id="${MCP_SESSION_ID:-}"
    if [ -z "$session_id" ] && [ -f "${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/active-session" ]; then
        session_id="$(head -1 "${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/active-session" 2>/dev/null | tr -d '\r')"
    fi

    if [ -n "$session_id" ]; then
        MCP_PLUGIN_SESSION_ID="$session_id"
        MCP_PLUGIN_SESSION_KEY="$(cache_scope_session_key "$session_id")"
        MCP_PLUGIN_SESSION_CACHE_DIR="${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/sessions/${MCP_PLUGIN_SESSION_KEY}"
    else
        MCP_PLUGIN_SESSION_ID=""
        MCP_PLUGIN_SESSION_KEY="bootstrap"
        MCP_PLUGIN_SESSION_CACHE_DIR="${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/bootstrap"
    fi

    CACHE_DIR="$MCP_PLUGIN_SESSION_CACHE_DIR"
    REPL_INVOKE_CACHE_DIR="$MCP_PLUGIN_SESSION_CACHE_DIR"
    PENDING_DIR="${CACHE_DIR}/pending"

    mkdir -p "$CACHE_DIR"
    export MCP_PLUGIN_CACHE_ROOT MCP_PLUGIN_WORKSPACE_PATH MCP_PLUGIN_WORKSPACE_KEY MCP_PLUGIN_WORKSPACE_CACHE_DIR
    export MCP_PLUGIN_SESSION_ID MCP_PLUGIN_SESSION_KEY MCP_PLUGIN_SESSION_CACHE_DIR
    export CACHE_DIR REPL_INVOKE_CACHE_DIR PENDING_DIR
}

cache_scope_select_session() {
    local session_id="$(_cache_scope_unquote "${1:-}")"
    [ -n "$session_id" ] || return 1

    if [ -z "${MCP_PLUGIN_WORKSPACE_CACHE_DIR:-}" ]; then
        cache_scope_init "${2:-$(cd "$CACHE_SCOPE_SCRIPT_DIR/.." && pwd)}" "${3:-$(pwd)}"
    fi

    MCP_PLUGIN_SESSION_ID="$session_id"
    MCP_PLUGIN_SESSION_KEY="$(cache_scope_session_key "$session_id")"
    MCP_PLUGIN_SESSION_CACHE_DIR="${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/sessions/${MCP_PLUGIN_SESSION_KEY}"
    CACHE_DIR="$MCP_PLUGIN_SESSION_CACHE_DIR"
    REPL_INVOKE_CACHE_DIR="$MCP_PLUGIN_SESSION_CACHE_DIR"
    PENDING_DIR="${CACHE_DIR}/pending"

    mkdir -p "$CACHE_DIR"
    printf '%s\n' "$session_id" > "${MCP_PLUGIN_WORKSPACE_CACHE_DIR}/active-session"
    export MCP_PLUGIN_SESSION_ID MCP_PLUGIN_SESSION_KEY MCP_PLUGIN_SESSION_CACHE_DIR
    export CACHE_DIR REPL_INVOKE_CACHE_DIR PENDING_DIR
}

cache_scope_current_turn_file() {
    printf '%s/current-turn.yaml' "${CACHE_DIR:?CACHE_DIR is not initialized}"
}

cache_scope_session_state_file() {
    printf '%s/session-state.yaml' "${CACHE_DIR:?CACHE_DIR is not initialized}"
}

# =============================================================================
# V4 parity: base64url workspace key + .mcpServer/failsafe layout
# Matches V4CacheManager.GetScopedCachePath in @sharpninja/mcpserver-agent-core
# TR-MCP-AGENT-PARITY-013: cache scoped by workspace key (Base64URL) + agentId
# =============================================================================

# cache_scope_workspace_key_v4 [workspace_path]
# Returns Base64URL encoding of the workspace path, matching the TypeScript impl:
#   Buffer.from(workspaceKey).toString('base64').replace(/\+/g,'-').replace(/\//g,'_').replace(/=/g,'')
cache_scope_workspace_key_v4() {
    local workspace_path
    workspace_path="$(_cache_scope_normalize_path "${1:-${MCPSERVER_WORKSPACE_PATH:-${MCP_WORKSPACE_PATH:-$(pwd)}}}")"

    # Try node first (exact match to V4CacheManager TS impl)
    if command -v node >/dev/null 2>&1; then
        node -e "process.stdout.write(Buffer.from(process.argv[1]).toString('base64').replace(/\+/g,'-').replace(/\//g,'_').replace(/=/g,''))" \
            "$workspace_path" 2>/dev/null && return 0
    fi

    # Fallback: openssl base64 + tr for base64url conversion
    if command -v openssl >/dev/null 2>&1; then
        printf '%s' "$workspace_path" | openssl base64 -A 2>/dev/null | tr '+/' '-_' | tr -d '=' && return 0
    fi

    # Fallback: python3
    if command -v python3 >/dev/null 2>&1; then
        printf '%s' "$workspace_path" | python3 -c \
            "import sys,base64; print(base64.urlsafe_b64encode(sys.stdin.buffer.read()).decode().rstrip('='))" 2>/dev/null && return 0
    fi

    # Last resort: base64 with tr (may not be available on all systems)
    printf '%s' "$workspace_path" | base64 2>/dev/null | tr -d '\n' | tr '+/' '-_' | tr -d '='
}

# cache_scope_v4_failsafe_root [workspace_path] [agent_id]
# Returns the absolute path to the v4 failsafe root for this workspace + agent.
# Layout: <workspace_root>/.mcpServer/failsafe/<agentId>/workspaces/<base64url>
# Matches V4CacheManager.GetScopedCachePath (relative .mcpServer/failsafe/...) anchored to workspace root.
cache_scope_v4_failsafe_root() {
    local workspace_path="${1:-}"
    local agent_id="${2:-${MCP_AGENT_ID:-ClaudeCode}}"

    if [ -z "$workspace_path" ]; then
        workspace_path="$(cache_scope_workspace_path)"
    fi

    local bash_path
    bash_path="$(_cache_scope_path_for_bash "$workspace_path" 2>/dev/null || printf '%s' "$workspace_path")"
    local abs_path
    if [ -d "$bash_path" ]; then
        abs_path="$(cd "$bash_path" 2>/dev/null && pwd -P || printf '%s' "$bash_path")"
    else
        abs_path="$bash_path"
    fi

    local key
    key="$(cache_scope_workspace_key_v4 "$abs_path")"
    printf '%s/.mcpServer/failsafe/%s/workspaces/%s' "$abs_path" "$agent_id" "$key"
}

export -f cache_scope_current_turn_file cache_scope_init cache_scope_select_session \
    cache_scope_session_key cache_scope_session_state_file cache_scope_workspace_key \
    cache_scope_workspace_path cache_scope_workspace_key_v4 cache_scope_v4_failsafe_root \
    2>/dev/null || true
