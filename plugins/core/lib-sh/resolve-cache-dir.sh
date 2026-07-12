#!/usr/bin/env bash
# resolve-cache-dir.sh — workspace-aware cache path resolver.
#
# Cache state (session-state.yaml, current-turn.yaml, plan-todo-map.yaml,
# turn-accept-failure.marker, pending/, last-build.log) applies to the
# workspace the marker file is in, not the plugin install directory. This
# helper picks the right cache dir for the caller.
#
# Precedence:
#   1. $MCP_CACHE_DIR_OVERRIDE    - explicit override (any path).
#   2. $PLUGIN_ROOT_OVERRIDE/cache - legacy test hook (kept for bats suites).
#   3. workspace env/.mcpServer/<agent>        - $MCPSERVER_WORKSPACE_PATH or
#                                   $MCP_WORKSPACE_PATH when it names an
#                                   existing directory (host-neutral).
#   4. <markerDir>/.mcpServer/<agent>          - workspace resolved by walking up for
#                                   AGENTS-README-FIRST.yaml. Production path.
#   5. $MCP_PLUGIN_ROOT/cache     - last-resort fallback (legacy
#                                   $CLAUDE_PLUGIN_ROOT honored).
#
# Usage:
#   source "$(dirname "${BASH_SOURCE[0]}")/resolve-cache-dir.sh"
#   CACHE_DIR="$(resolve_cache_dir)"

# Guard: avoid re-defining if already sourced.
if type resolve_cache_dir >/dev/null 2>&1; then
    return 0 2>/dev/null || true
fi

_RESOLVE_CACHE_DIR_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

resolve_cache_agent_key() {
    local agent="${MCP_AGENT_NAME:-${PLUGIN_AGENT_NAME:-${PLUGIN_AGENT_DEFAULT:-${MCP_PLUGIN_HOST:-default}}}}"
    local normalized
    normalized="$(printf '%s' "$agent" | tr '[:upper:]' '[:lower:]')"

    case "$normalized" in
        claude|claudecode|claude-code) printf '%s' 'claude'; return 0 ;;
        claudecowork|claude-cowork) printf '%s' 'cowork'; return 0 ;;
        codex) printf '%s' 'codex'; return 0 ;;
        copilot) printf '%s' 'copilot'; return 0 ;;
        grok|grokcode|grok-code) printf '%s' 'grok'; return 0 ;;
        cline|cline-v2) printf '%s' 'cline'; return 0 ;;
        opencode|open-code) printf '%s' 'opencode'; return 0 ;;
    esac

    normalized="$(printf '%s' "$normalized" | sed 's/[^a-z0-9][^a-z0-9]*/-/g; s/^-//; s/-$//')"
    [ -n "$normalized" ] || normalized='default'
    printf '%s' "$normalized"
}

resolve_workspace_cache_dir() {
    local workspace_path="$1"
    printf '%s/.mcpServer/%s' "$workspace_path" "$(resolve_cache_agent_key)"
}

resolve_cache_dir() {
    if [ -n "${MCP_CACHE_DIR_OVERRIDE:-}" ]; then
        printf '%s' "$MCP_CACHE_DIR_OVERRIDE"
        return 0
    fi

    if [ -n "${PLUGIN_ROOT_OVERRIDE:-}" ]; then
        printf '%s/cache' "$PLUGIN_ROOT_OVERRIDE"
        return 0
    fi

    # Host-neutral configured-workspace short-circuit: when the workspace
    # path is supplied via env and exists, its cache dir wins over walking.
    local configured_workspace="${MCPSERVER_WORKSPACE_PATH:-${MCP_WORKSPACE_PATH:-}}"
    if [ -n "$configured_workspace" ] && [ -d "$configured_workspace" ]; then
        resolve_workspace_cache_dir "$configured_workspace"
        return 0
    fi

    # Walk up for the workspace marker. plugin-env.sh computes
    # MCP_WORKSPACE_START_DIR from the host's env chain; CLAUDE_PROJECT_DIR
    # is honored as a legacy fallback, then CWD.
    local start_dir="${MCP_WORKSPACE_START_DIR:-${CLAUDE_PROJECT_DIR:-$(pwd)}}"

    # The function may arrive in child processes via export -f without the
    # load-time script-dir variable; guard every reference (set -u safety).
    local resolver_dir="${_RESOLVE_CACHE_DIR_SCRIPT_DIR:-}"
    if ! type find_marker_file >/dev/null 2>&1 && [ -n "$resolver_dir" ]; then
        # Best-effort source. marker-resolver.sh lives alongside this file.
        # shellcheck source=./marker-resolver.sh
        source "$resolver_dir/marker-resolver.sh" 2>/dev/null || true
    fi

    if type find_marker_file >/dev/null 2>&1; then
        local marker_file
        if marker_file=$(find_marker_file "$start_dir" 2>/dev/null); then
            resolve_workspace_cache_dir "$(dirname "$marker_file")"
            return 0
        fi
    fi

    local plugin_root="${MCP_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-}}"
    if [ -z "$plugin_root" ] && [ -n "$resolver_dir" ]; then
        plugin_root="$(cd "$resolver_dir/.." && pwd)"
    fi
    [ -n "$plugin_root" ] || plugin_root="$(pwd)"
    printf '%s/cache' "$plugin_root"
}

export -f resolve_cache_dir 2>/dev/null || true
