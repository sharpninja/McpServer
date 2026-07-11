#!/usr/bin/env bash
# final-response.sh - Complete the active MCP session-log turn with the model-authored response.
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

_build_complete_params() {
    printf 'response: |\n'
    printf '%s\n' "$response" | sed 's/^/  /'
}

_build_complete_params | "$SCRIPT_DIR/repl-invoke.sh" workflow.sessionlog.completeTurn
