#!/usr/bin/env bash
# TR-MCP-QBRAIN-007: Resolve the installed REPL command.
# Prefer qbrain-ai-repl when it is already on PATH, otherwise the 1.x mcpserver-repl command.
# Does not download QBrainAI.Repl when either command is already usable.

resolve_repl_bin() {
    if [ -n "${MCPSERVER_REPL_BIN:-}" ]; then
        if [ -x "${MCPSERVER_REPL_BIN}" ] || command -v "${MCPSERVER_REPL_BIN}" >/dev/null 2>&1; then
            printf '%s\n' "${MCPSERVER_REPL_BIN}"
            return 0
        fi
    fi
    if command -v qbrain-ai-repl >/dev/null 2>&1; then
        command -v qbrain-ai-repl
        return 0
    fi
    if command -v mcpserver-repl >/dev/null 2>&1; then
        command -v mcpserver-repl
        return 0
    fi
    return 1
}

repl_bin_installed() {
    resolve_repl_bin >/dev/null 2>&1
}
