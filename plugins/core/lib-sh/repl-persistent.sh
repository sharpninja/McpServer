#!/usr/bin/env bash
# FR-MCP-PLUGINCORE-003: shell entry point for the persistent REPL daemon.
#
# repl_invoke_persistent <method> [paramsJson]
#   Sends one request through the long-lived repl daemon (repl-daemon.js) and
#   prints the '---'-terminated YAML response. Falls back to spawn-per-call
#   when MCPSERVER_REPL_PERSISTENT=0 or node is unavailable.
#
# The request id is generated in the canonical req-<utc>-<slug> format.

_repl_persistent_script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

repl_persistent_enabled() {
    [ "${MCPSERVER_REPL_PERSISTENT:-1}" != "0" ] && command -v node >/dev/null 2>&1
}

_repl_persistent_native_path() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s\n' "$1"
    fi
}

repl_invoke_persistent() {
    local method="${1:?usage: repl_invoke_persistent <method> [paramsJson]}"
    local params_json="${2:-}"
    local request_id="req-$(date -u +%Y%m%dT%H%M%SZ)-persist-$RANDOM"

    local envelope
    if [ -n "$params_json" ]; then
        envelope="$(node -e '
const [rid, method, params] = process.argv.slice(1);
process.stdout.write(JSON.stringify({type:"request",payload:{requestId:rid,method,params:JSON.parse(params)}}));
' "$request_id" "$method" "$params_json")"
    else
        envelope="$(node -e '
const [rid, method] = process.argv.slice(1);
process.stdout.write(JSON.stringify({type:"request",payload:{requestId:rid,method}}));
' "$request_id" "$method")"
    fi

    if repl_persistent_enabled; then
        printf '%s\n' "$envelope" | node "$(_repl_persistent_native_path "$_repl_persistent_script_dir/repl-daemon.js")" --send
    else
        # Fallback: classic one-process-per-request invocation.
        printf '%s\n' "$envelope" | "${MCPSERVER_REPL_BIN:-mcpserver-repl}" --agent-stdio
    fi
}
