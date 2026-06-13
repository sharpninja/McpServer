#!/usr/bin/env bash
# final-response.sh — Complete the active MCP session-log turn with rich fields.
#
# Reads the Codex JSONL transcript for the current session (if available) and
# extracts interpretation, processingDialog, actions, filesModified, contextList,
# blockers, designDecisions, and requirementsDiscovered before calling completeTurn.
#
# Usage:
#   final-response.sh [<response-text>]
#   echo "<response>" | final-response.sh
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

response="${1:-}"
if [ -z "$response" ] && [ ! -t 0 ]; then
    response="$(cat 2>/dev/null || true)"
fi
if [ -z "$response" ]; then
    response="Turn completed."
fi

# Try to locate the Codex JSONL for the current session and turn.
# The path may be stored in current-turn.yaml (written by user-prompt-submit.sh)
# or in environment variables set by the Codex CLI.
CODEX_JSONL_PATH="${CODEX_SESSION_FILE:-${CODEX_ROLLOUT_FILE:-}}"

if [ -z "$CODEX_JSONL_PATH" ] && command -v node >/dev/null 2>&1; then
    # Try to read from current-turn.yaml
    if ! type resolve_cache_dir >/dev/null 2>&1; then
        # shellcheck source=./resolve-cache-dir.sh
        source "${SCRIPT_DIR}/resolve-cache-dir.sh" 2>/dev/null || true
    fi
    if type resolve_cache_dir >/dev/null 2>&1; then
        CACHE_DIR_FR="$(resolve_cache_dir 2>/dev/null || true)"
        TURN_FILE="${CACHE_DIR_FR}/current-turn.yaml"
        if [ -f "$TURN_FILE" ]; then
            CODEX_JSONL_PATH="$(grep '^codexJsonlPath:' "$TURN_FILE" 2>/dev/null | head -1 | sed 's/^codexJsonlPath:[[:space:]]*//' | tr -d '"' || true)"
        fi
    fi
fi

_build_complete_params() {
    printf 'response: |\n'
    printf '%s\n' "$response" | sed 's/^/  /'
}

# If we have a JSONL file, extract rich fields and emit to completeTurn
if [ -n "$CODEX_JSONL_PATH" ] && [ -f "$CODEX_JSONL_PATH" ] && command -v node >/dev/null 2>&1; then
    JSONL_ENRICH="$(node "${SCRIPT_DIR}/codex-jsonl-enrich.js" "$CODEX_JSONL_PATH" "$response" 2>/dev/null || true)"
    if [ -n "$JSONL_ENRICH" ]; then
        printf '%s\n' "$JSONL_ENRICH" | "$SCRIPT_DIR/repl-invoke.sh" workflow.sessionlog.completeTurn
        exit $?
    fi
fi

# Fallback: plain completeTurn with just the response text
_build_complete_params | "$SCRIPT_DIR/repl-invoke.sh" workflow.sessionlog.completeTurn
