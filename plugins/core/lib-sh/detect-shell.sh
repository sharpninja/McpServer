#!/usr/bin/env bash
# Dispatch to bash or PowerShell variant of a script
# Usage: detect-shell.sh <script-basename> [args...]
# Example: detect-shell.sh marker-resolver [args]
# Will run lib/marker-resolver.sh if bash available, else lib/marker-resolver.ps1 via pwsh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT_NAME="$1"
shift

if [ -f "$SCRIPT_DIR/${SCRIPT_NAME}.sh" ]; then
    exec bash "$SCRIPT_DIR/${SCRIPT_NAME}.sh" "$@"
elif command -v pwsh >/dev/null 2>&1 && [ -f "$SCRIPT_DIR/${SCRIPT_NAME}.ps1" ]; then
    exec pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$SCRIPT_DIR/${SCRIPT_NAME}.ps1" "$@"
else
    echo "ERROR: Neither bash script nor pwsh available for ${SCRIPT_NAME}" >&2
    exit 1
fi
