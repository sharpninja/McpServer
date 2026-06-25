#!/usr/bin/env bash
# hook-lib.sh - shared hook logic for every mcpserver-*-plugin host.
#
# Per-host wrappers (see plugins/core/hooks-templates/) are 5-10 lines: they
# source lib/plugin-env.sh (host knob defaults), source this file, call
# hook_env_init, then exactly one <hook>_main entry function.
#
# Assembled per the Phase 2 reconciliation report from:
#   - mcpserver-claude-code-plugin/hooks/scripts/*.sh (canonical wrapper set)
#   - mcpserver-codex-plugin/lib/*.sh logic fixes: stop-gate Gate 3 audit,
#     code-verify single-owner codeEdits rule, user-prompt-submit
#     self-bootstrap + queryText persistence, awk json escaper,
#     cli output mode for session-start
#   - mcpserver-claude-cowork-plugin: full_bootstrap start-dir argument
#
# The library reads only host-neutral knobs (MCP_*, PLUGIN_*); host env vars
# are mapped in plugin-env.sh. CLAUDE_STOP_HOOK_ACTIVE is read by that name
# on every host (codex deliberately retains it).

HOOK_LIB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ---------------------------------------------------------------------------
# Environment / cache-dir initialization
# ---------------------------------------------------------------------------

# hook_env_init [flat|scoped] [start_dir]
#   flat   - CACHE_DIR from resolve_cache_dir (workspace/override anchored,
#            no per-workspace/session subtree). Used by the session lifecycle
#            and plan hooks (hooks.bats contract).
#   scoped - CACHE_DIR from cache_scope_init (workspaces/<key>/sessions/<key>
#            beneath the unified resolver root). Used by user-prompt-submit,
#            stop-gate, and code-verify.
# Note: sourcing lib/repl-invoke.sh later re-runs cache_scope_init and
# re-exports CACHE_DIR; entry functions intentionally use the live value of
# $CACHE_DIR at write time, mirroring the original hook scripts.
hook_env_init() {
    local mode="${1:-flat}"
    local start_dir="${2:-${MCP_WORKSPACE_START_DIR:-$(pwd)}}"

    MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-$(cd "$HOOK_LIB_DIR/.." && pwd)}}"
    export MCP_PLUGIN_ROOT

    case "$mode" in
        scoped)
            # Source unconditionally: cache_scope_init may arrive via
            # export -f from a parent process WITHOUT its private helpers
            # (_cache_scope_*), so an inherited copy must never be trusted.
            # shellcheck source=./cache-scope.sh
            source "$HOOK_LIB_DIR/cache-scope.sh"
            cache_scope_init "$MCP_PLUGIN_ROOT" "$start_dir"
            ;;
        *)
            if ! type resolve_cache_dir >/dev/null 2>&1; then
                # shellcheck source=./resolve-cache-dir.sh
                source "$HOOK_LIB_DIR/resolve-cache-dir.sh"
            fi
            CACHE_DIR="$(resolve_cache_dir)"
            ;;
    esac
    export CACHE_DIR
}

# Conditional library loading: never stomp functions pre-defined by the
# caller (bats suites export mocks before sourcing the hook wrappers).
hook_require_repl_invoke() {
    if ! type repl_invoke >/dev/null 2>&1; then
        # shellcheck source=./repl-invoke.sh
        source "$HOOK_LIB_DIR/repl-invoke.sh" 2>/dev/null || true
    fi
}

hook_require_repl_invoke_strict() {
    if ! type repl_invoke >/dev/null 2>&1; then
        # shellcheck source=./repl-invoke.sh
        source "$HOOK_LIB_DIR/repl-invoke.sh"
    fi
}

hook_require_marker_resolver() {
    if ! type full_bootstrap >/dev/null 2>&1; then
        # shellcheck source=./marker-resolver.sh
        source "$HOOK_LIB_DIR/marker-resolver.sh"
    fi
}

hook_require_cache_manager() {
    if ! type cache_flush >/dev/null 2>&1; then
        # shellcheck source=./cache-manager.sh
        source "$HOOK_LIB_DIR/cache-manager.sh"
    fi
}

hook_require_memory_context() {
    if ! type mcp_required_memory_context >/dev/null 2>&1; then
        # shellcheck source=./memory-context.sh
        source "$HOOK_LIB_DIR/memory-context.sh" 2>/dev/null || true
    fi
}

# ---------------------------------------------------------------------------
# Small shared helpers
# ---------------------------------------------------------------------------

# run_with_timeout <seconds> <command...>
run_with_timeout() {
    local timeout_seconds="${1:-8}"
    shift

    if command -v timeout >/dev/null 2>&1; then
        timeout --kill-after=2s "$timeout_seconds" "$@"
        return $?
    fi

    "$@"
}

# acquire_hook_lock <name>
# Creates $CACHE_DIR/<name>.lock with stale-lock recovery. Returns 1 when
# another instance holds the lock; the caller emits its hook-specific output.
acquire_hook_lock() {
    local name="$1"
    mkdir -p "$CACHE_DIR"
    local lock_dir="$CACHE_DIR/${name}.lock"
    if [ -d "$lock_dir" ]; then
        local lock_age
        lock_age=$(( $(date +%s) - $(stat -c %Y "$lock_dir" 2>/dev/null || echo 0) ))
        if [ "$lock_age" -gt "${MCP_PLUGIN_STALE_LOCK_SECONDS:-120}" ]; then
            rm -rf "$lock_dir"
        fi
    fi
    if ! mkdir "$lock_dir" 2>/dev/null; then
        return 1
    fi
    # shellcheck disable=SC2064
    trap "rm -rf '$lock_dir'" EXIT
    return 0
}

# yaml_get <file> <key> - first scalar value for a top-level key.
yaml_get() {
    local file="$1" key="$2"
    [ -f "$file" ] || return 1
    grep "^${key}:" "$file" 2>/dev/null | head -1 | sed "s/^${key}:[[:space:]]*//"
}

# hook_json_escape <text> - escape arbitrary text for JSON string embedding
# without depending on jq (codex awk escaper; safer than emitting raw text).
hook_json_escape() {
    awk '
        BEGIN { ORS = "" }
        {
            gsub(/\\/, "\\\\")
            gsub(/"/, "\\\"")
            gsub(/\r/, "\\r")
            gsub(/\t/, "\\t")
            if (NR > 1) {
                printf "\\n"
            }
            printf "%s", $0
        }
    ' <<<"$1"
}

# payload_field <payload> <jq_path> <sed_key>
# Extract a field from a JSON hook payload. Prefers jq; grep/sed fallback.
payload_field() {
    local payload="$1" jq_path="$2" sed_key="$3"
    if command -v jq >/dev/null 2>&1; then
        printf '%s' "$payload" | jq -r "$jq_path // empty" 2>/dev/null
    else
        printf '%s' "$payload" | sed -n 's/.*"'"$sed_key"'"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1
    fi
}

# repl_invoke_timed <method> <params> - the PREVIOUS_REPL_TIMEOUT save/set/
# restore dance collapsed from ~20 copies across the hook scripts.
repl_invoke_timed() {
    local method="$1" params="${2:-}" rc
    local previous_timeout="${REPL_TIMEOUT:-}"
    export REPL_TIMEOUT="${REPL_SESSIONLOG_REPL_TIMEOUT:-8}"
    repl_invoke "$method" "$params"
    rc=$?
    if [ -n "$previous_timeout" ]; then
        export REPL_TIMEOUT="$previous_timeout"
    else
        unset REPL_TIMEOUT
    fi
    return $rc
}

# Output emitters selected by MCP_HOOK_OUTPUT_MODE (hook|cli).
hook_emit_noop() {
    printf '{}\n'
}

hook_emit_block() {
    printf '{"decision":"block","reason":"%s"}\n' "$1"
}

# hook_emit_event <eventName> <status> [extraJsonFields]
hook_emit_event() {
    local event="$1" status="$2" extra="${3:-}"
    if [ -n "$extra" ]; then
        printf '{"hookSpecificOutput":{"hookEventName":"%s","status":"%s",%s}}\n' "$event" "$status" "$extra"
    else
        printf '{"hookSpecificOutput":{"hookEventName":"%s","status":"%s"}}\n' "$event" "$status"
    fi
}

# cli_emit_status <status> [extraJsonFields] - codex CLI-mode status object.
cli_emit_status() {
    local status="$1" extra="${2:-}"
    if [ -n "$extra" ]; then
        printf '{"status":"%s",%s}\n' "$status" "$extra"
    else
        printf '{"status":"%s"}\n' "$status"
    fi
}

# ---------------------------------------------------------------------------
# session-start
# ---------------------------------------------------------------------------

_hook_write_untrusted() {
    mkdir -p "$CACHE_DIR"
    cat > "$CACHE_DIR/session-state.yaml" << EOF
status: MCP_UNTRUSTED
reason: "$1"
timestamp: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
EOF
}

_hook_epoch_from_iso() {
    local value="${1:-}"
    value="$(printf '%s' "$value" | tr -d '\r' | sed 's/^"\(.*\)"$/\1/; s/^'\''\(.*\)'\''$/\1/')"
    [ -n "$value" ] || return 1
    date -u -d "$value" +%s 2>/dev/null
}

_hook_file_mtime_epoch() {
    local file="$1"
    stat -c %Y "$file" 2>/dev/null || stat -f %m "$file" 2>/dev/null
}

_hook_session_state_last_seen_epoch() {
    local file="$1" value epoch key
    for key in lastUpdated timestamp started; do
        value="$(yaml_get "$file" "$key" 2>/dev/null || true)"
        epoch="$(_hook_epoch_from_iso "$value" 2>/dev/null || true)"
        if [ -n "$epoch" ]; then
            printf '%s' "$epoch"
            return 0
        fi
    done

    _hook_file_mtime_epoch "$file"
}

_hook_discard_stale_cached_session() {
    # Explicit MCP_SESSION_ID means the caller intentionally selected the session.
    [ -z "${MCP_SESSION_ID:-}" ] || return 0

    local session_file="$CACHE_DIR/session-state.yaml"
    [ -f "$session_file" ] || return 0

    local max_idle="${MCP_SESSION_CACHE_MAX_IDLE_SECONDS:-86400}"
    case "$max_idle" in
        ''|*[!0-9]*) max_idle=86400 ;;
    esac
    [ "$max_idle" -gt 0 ] || return 0

    local last_seen now age session_id active_file active_id
    last_seen="$(_hook_session_state_last_seen_epoch "$session_file" 2>/dev/null || true)"
    [ -n "$last_seen" ] || return 0
    now="$(date -u +%s)"
    age=$((now - last_seen))
    [ "$age" -gt "$max_idle" ] || return 0

    session_id="$(yaml_get "$session_file" sessionId 2>/dev/null || true)"
    active_file="${MCP_PLUGIN_WORKSPACE_CACHE_DIR:-}/active-session"
    if [ -n "$active_file" ] && [ -f "$active_file" ]; then
        active_id="$(head -1 "$active_file" 2>/dev/null | tr -d '\r')"
        if [ -z "$session_id" ] || [ "$active_id" = "$session_id" ]; then
            rm -f "$active_file"
        fi
    fi

    rm -f "$session_file" "$CACHE_DIR/current-turn.yaml"
    if type cache_scope_init >/dev/null 2>&1; then
        cache_scope_init "$MCP_PLUGIN_ROOT" "${1:-$(pwd)}"
    fi
}

_hook_cached_session_stale_reason() {
    # Stop-gate must not reuse a scoped cache that prompt submission would discard.
    # Explicit MCP_SESSION_ID means the caller intentionally selected the session.
    [ -z "${MCP_SESSION_ID:-}" ] || return 0

    local session_file="$CACHE_DIR/session-state.yaml"
    [ -f "$session_file" ] || return 0

    local max_idle="${MCP_SESSION_CACHE_MAX_IDLE_SECONDS:-86400}"
    case "$max_idle" in
        ''|*[!0-9]*) max_idle=86400 ;;
    esac
    [ "$max_idle" -gt 0 ] || return 0

    local last_seen now age session_id turn_file turn_id
    last_seen="$(_hook_session_state_last_seen_epoch "$session_file" 2>/dev/null || true)"
    [ -n "$last_seen" ] || return 0
    now="$(date -u +%s)"
    age=$((now - last_seen))
    [ "$age" -gt "$max_idle" ] || return 0

    session_id="$(yaml_get "$session_file" sessionId 2>/dev/null || true)"
    turn_file="${1:-$CACHE_DIR/current-turn.yaml}"
    turn_id=""
    if [ -f "$turn_file" ]; then
        turn_id="$(yaml_get "$turn_file" turnRequestId 2>/dev/null || true)"
    fi

    printf 'Cached MCP session %s is stale (%ss idle; limit %ss)' "${session_id:-unknown}" "$age" "$max_idle"
    if [ -n "$turn_id" ]; then
        printf ' and points at turn %s' "$turn_id"
    fi
    printf '. Start a fresh session before stop-gate can close this turn.'
}

session_start_main() {
    local output_mode="${MCP_HOOK_OUTPUT_MODE:-hook}"
    local start_dir="${1:-${MCP_WORKSPACE_START_DIR:-$(pwd)}}"

    hook_require_marker_resolver
    hook_require_repl_invoke_strict
    hook_require_cache_manager

    mkdir -p "$CACHE_DIR"
    if ! acquire_hook_lock "session-start"; then
        if [ "$output_mode" = "cli" ]; then
            cli_emit_status "no-session" '"error":"session-start already running"'
            exit 1
        fi
        hook_emit_noop
        exit 0
    fi

    # Ensure ensure-repl has run (install mcpserver-repl if missing)
    if ! command -v mcpserver-repl >/dev/null 2>&1; then
        bash "$HOOK_LIB_DIR/ensure-repl.sh" >&2 || true
    fi

    # Run bootstrap. full_bootstrap accepts an optional start dir (cowork
    # hosts do not run hooks in the workspace cwd; harmless elsewhere).
    if ! full_bootstrap "$start_dir" 2>/dev/null; then
        _hook_write_untrusted "Bootstrap failed"
        if [ "$output_mode" = "cli" ]; then
            cli_emit_status "untrusted" '"error":"marker bootstrap failed"'
            exit 1
        fi
        hook_emit_noop
        exit 0
    fi

    # Build session ID
    local session_agent session_model session_title session_id
    session_agent="${MCP_SESSION_AGENT:-${MCP_AGENT_NAME:-${PLUGIN_AGENT_DEFAULT:-ClaudeCode}}}"
    session_model="${MCP_SESSION_MODEL:-${PLUGIN_MODEL_DEFAULT:-}}"
    session_title="${MCP_SESSION_TITLE:-${session_agent} plugin session}"
    if [ -n "${MCP_SESSION_ID:-}" ]; then
        session_id="$MCP_SESSION_ID"
    elif type _repl_generate_session_id >/dev/null 2>&1; then
        session_id="$(_repl_generate_session_id "$session_agent" "$session_title" "${MCPSERVER_WORKSPACE:-$(basename "$start_dir")}")"
    else
        session_id="${session_agent}-$(date -u +%Y%m%dT%H%M%SZ)-plugin"
    fi

    if type cache_scope_select_session >/dev/null 2>&1; then
        cache_scope_select_session "$session_id" "$MCP_PLUGIN_ROOT" "$start_dir"
    fi

    local session_params="agent: ${session_agent}
sessionId: ${session_id}
title: ${session_title}"
    if [ -n "$session_model" ]; then
        session_params="${session_params}
model: ${session_model}"
    fi

    local status open_output open_status
    if open_output="$(repl_invoke_timed "workflow.sessionlog.openSession" "$session_params" 2>&1)"; then
        open_status=0
        status="verified"
    else
        open_status=$?
        status="degraded"
    fi

    if [ "$output_mode" = "cli" ]; then
        if [ "$open_status" -ne 0 ]; then
            cli_emit_status "no-session" "\"error\":\"$(hook_json_escape "$open_output")\",\"timeoutSeconds\":${REPL_SESSIONLOG_REPL_TIMEOUT:-8}"
            exit 1
        fi
        # The repl-invoke shim wrote session-state.yaml; read it back.
        local stored_id stored_status
        stored_id="$(yaml_get "$CACHE_DIR/session-state.yaml" sessionId || true)"
        stored_status="$(yaml_get "$CACHE_DIR/session-state.yaml" status || true)"
        cli_emit_status "${stored_status:-$status}" "\"sessionId\":\"${stored_id:-$session_id}\""
        exit 0
    fi

    # Write session state (hook mode keeps the claude-code contract).
    mkdir -p "$CACHE_DIR"
    cat > "$CACHE_DIR/session-state.yaml" << EOF
status: ${status}
sessionId: ${session_id}
workspacePath: "${MCPSERVER_WORKSPACE_PATH:-}"
workspace: "${MCPSERVER_WORKSPACE:-}"
baseUrl: "${MCPSERVER_BASE_URL:-}"
timestamp: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
EOF

    # Emit schema-valid no-op hook output.
    hook_emit_noop
}

# ---------------------------------------------------------------------------
# session-end
# ---------------------------------------------------------------------------

session_end_main() {
    local session_state="$CACHE_DIR/session-state.yaml"

    hook_require_repl_invoke_strict
    hook_require_cache_manager

    # Flush any pending cache entries
    FLUSH_RESULT=$(cache_flush 2>/dev/null || echo "flushed=0 failed=0 pending=0")

    # Read session state if it exists
    local session_id=""
    if [ -f "$session_state" ]; then
        session_id="$(yaml_get "$session_state" sessionId || true)"
    fi

    # Complete the session turn if we have a session ID
    if [ -n "$session_id" ]; then
        local close_params="agent: ${MCP_AGENT_NAME:-${PLUGIN_AGENT_DEFAULT:-ClaudeCode}}
sessionId: ${session_id}
status: completed"
        repl_invoke "workflow.sessionlog.closeSession" "$close_params" >/dev/null 2>&1 || true
    fi

    # Clean up session state
    rm -f "$session_state"

    hook_emit_noop
}

# ---------------------------------------------------------------------------
# pre-compact
# ---------------------------------------------------------------------------

pre_compact_main() {
    local session_state="$CACHE_DIR/session-state.yaml"

    hook_require_repl_invoke_strict
    hook_require_cache_manager

    # Read session state
    local session_id=""
    if [ -f "$session_state" ]; then
        session_id="$(yaml_get "$session_state" sessionId || true)"
    fi

    # Update the session turn with compaction tag before compacting
    if [ -n "$session_id" ]; then
        local update_params="agent: ${MCP_AGENT_NAME:-${PLUGIN_AGENT_DEFAULT:-ClaudeCode}}
sessionId: ${session_id}
tags:
  - pre-compact
status: persisting"
        repl_invoke "workflow.sessionlog.updateTurn" "$update_params" >/dev/null 2>&1 || true
    fi

    # Flush cache so no pending items are lost during compaction
    FLUSH_RESULT=$(cache_flush 2>/dev/null || echo "flushed=0 failed=0 pending=0")

    hook_emit_noop
}

# ---------------------------------------------------------------------------
# post-compact
# ---------------------------------------------------------------------------

post_compact_main() {
    local start_dir="${1:-${MCP_WORKSPACE_START_DIR:-$(pwd)}}"

    hook_require_marker_resolver

    # Re-verify the marker after compaction
    if ! full_bootstrap "$start_dir" 2>/dev/null; then
        hook_emit_noop
        exit 0
    fi

    # PostCompact cannot inject context; emit schema-valid no-op output.
    hook_emit_noop
}

# ---------------------------------------------------------------------------
# user-prompt-submit
# ---------------------------------------------------------------------------

user_prompt_submit_main() {
    if ! acquire_hook_lock "user-prompt-submit"; then
        hook_emit_event "UserPromptSubmit" "already-running"
        exit 0
    fi

    hook_require_repl_invoke
    hook_require_memory_context

    # Read stdin into PAYLOAD (may be empty)
    local payload
    payload="$(cat 2>/dev/null || true)"

    local user_prompt
    user_prompt="$(payload_field "$payload" '.prompt' 'prompt')"

    _hook_discard_stale_cached_session "$PWD"

    # Self-bootstrap (codex): when SessionStart never fired, run the
    # session-start wrapper so the hook is self-contained, then re-scope.
    if [ ! -f "$CACHE_DIR/session-state.yaml" ] || [ -z "$(yaml_get "$CACHE_DIR/session-state.yaml" sessionId 2>/dev/null)" ]; then
        local session_start_script="${MCP_SESSION_START_SCRIPT:-}"
        if [ -z "$session_start_script" ]; then
            for session_start_script in \
                "$MCP_PLUGIN_ROOT/hooks/scripts/session-start.sh" \
                "$MCP_PLUGIN_ROOT/lib/session-start.sh"; do
                [ -f "$session_start_script" ] && break
            done
        fi
        if [ -n "$session_start_script" ] && [ -f "$session_start_script" ]; then
            run_with_timeout "${REPL_SESSIONLOG_REPL_TIMEOUT:-8}" bash "$session_start_script" "$PWD" >/dev/null 2>&1 || true
            if type cache_scope_init >/dev/null 2>&1; then
                cache_scope_init "$MCP_PLUGIN_ROOT" "$PWD"
            fi
        fi
    fi

    # If no MCP session established, short-circuit. The agent can still respond.
    if [ ! -f "$CACHE_DIR/session-state.yaml" ]; then
        hook_emit_event "UserPromptSubmit" "no-session"
        exit 0
    fi

    local session_status
    session_status="$(yaml_get "$CACHE_DIR/session-state.yaml" status || true)"
    if [ "$session_status" != "verified" ]; then
        hook_emit_event "UserPromptSubmit" "$session_status"
        exit 0
    fi

    # Build a deterministic turn requestId
    local timestamp rand_suffix turn_request_id
    timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
    rand_suffix="$(printf '%04x' $RANDOM)"
    turn_request_id="req-${timestamp}-prompt-${rand_suffix}"

    # Derive a short queryTitle from the first line of the prompt (max 60 chars)
    local query_title
    query_title="$(printf '%s' "$user_prompt" | head -1 | cut -c1-60)"
    [ -z "$query_title" ] && query_title="User prompt"

    # Escape the prompt for YAML embedding (literal block)
    local query_text_block
    query_text_block="$(printf '%s' "$user_prompt" | sed 's/^/    /')"

    local turn_params="requestId: ${turn_request_id}
queryTitle: ${query_title}
queryText: |
${query_text_block}"

    # Open the turn. Graceful fallback to cache_write if REPL unavailable.
    if type repl_invoke >/dev/null 2>&1; then
        if ! repl_invoke_timed "workflow.sessionlog.beginTurn" "$turn_params" >/dev/null 2>&1; then
            if type cache_write >/dev/null 2>&1; then
                cache_write "workflow.sessionlog.beginTurn" "$turn_params" >/dev/null 2>&1 || true
            fi
        fi
    fi

    # Discover Codex JSONL transcript path for this session (env-gated;
    # self-disabling when the CODEX_* vars are unset).
    local codex_jsonl_path
    codex_jsonl_path="${CODEX_SESSION_FILE:-${CODEX_ROLLOUT_FILE:-}}"
    if [ -z "$codex_jsonl_path" ] && command -v node >/dev/null 2>&1; then
        local codex_session_dir today_dir
        codex_session_dir="${CODEX_SESSION_DIR:-${HOME}/.codex/sessions}"
        today_dir="${codex_session_dir}/$(date -u +%Y/%m/%d 2>/dev/null || true)"
        if [ -d "$today_dir" ]; then
            codex_jsonl_path="$(ls -t "${today_dir}"/rollout-*.jsonl 2>/dev/null | head -1 || true)"
        fi
    fi

    # Record the active turn so the Stop hook can verify completion.
    # queryText is persisted (codex fix) so later import/recovery keeps the
    # original prompt.
    mkdir -p "$CACHE_DIR"
    {
        printf 'turnRequestId: %s\n' "${turn_request_id}"
        printf 'queryTitle: %s\n' "${query_title}"
        printf 'openedAt: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
        printf 'status: in_progress\n'
        printf 'codeEdits: 0\n'
        printf 'lastBuildStatus: unknown\n'
        if [ -n "$codex_jsonl_path" ] && [ -f "$codex_jsonl_path" ]; then
            printf 'codexJsonlPath: "%s"\n' "$codex_jsonl_path"
        fi
        printf 'queryText: |\n'
        printf '%s\n' "${query_text_block}"
    } > "$CACHE_DIR/current-turn.yaml"

    local internal_todo_reminder="${MCP_INTERNAL_TODO_REMINDER_DEFAULT:-Use TODO and requirements tools only as needed.}"
    if type _repl_internal_todo_is_enabled >/dev/null 2>&1 && _repl_internal_todo_is_enabled; then
        internal_todo_reminder="${MCP_INTERNAL_TODO_REMINDER_ENABLED:-MCP-backed internal TODO tracking is enabled. Mirror durable plan items through workflow.todo.* and keep only transient execution details in the local checklist.}"
    fi

    local required_memory_context
    if type mcp_required_memory_context >/dev/null 2>&1; then
        required_memory_context="$(mcp_required_memory_context)"
    else
        required_memory_context="$(printf 'REQUIRED MEMORIES\n- None.\n')"
    fi

    # Per-turn reminder: REQUIRED MEMORIES + the host-supplied body
    # (MCP_PROMPT_REMINDER_BODY from plugin-env.sh) with placeholders
    # __TURN_REQUEST_ID__ and __INTERNAL_TODO_REMINDER__ substituted.
    local body reminder reminder_json
    body="${MCP_PROMPT_REMINDER_BODY:-session log turn __TURN_REQUEST_ID__ is now active. __INTERNAL_TODO_REMINDER__ The stop-gate hook will auto-close the turn on finalize. PostToolUse/Write|Edit hooks auto-log actions. If you want richer action metadata, POST /mcpserver/sessionlog directly with the workspace API key from AGENTS-README-FIRST.yaml.}"
    body="${body//__TURN_REQUEST_ID__/$turn_request_id}"
    body="${body//__INTERNAL_TODO_REMINDER__/$internal_todo_reminder}"
    reminder="$(printf '%s\n\n%s' "$required_memory_context" "$body")"

    if type mcp_json_escape >/dev/null 2>&1; then
        reminder_json="$(mcp_json_escape "$reminder")"
    else
        reminder_json="$(hook_json_escape "$reminder")"
    fi

    printf '{"hookSpecificOutput":{"hookEventName":"UserPromptSubmit","status":"turn-opened","turnRequestId":"%s","additionalContext":"%s"}}\n' \
        "$turn_request_id" \
        "$reminder_json"
    exit 0
}

# ---------------------------------------------------------------------------
# stop-gate
# ---------------------------------------------------------------------------

stop_gate_main() {
    local turn_file="$CACHE_DIR/current-turn.yaml"

    # Read stdin (may be empty) so the hook runtime doesn't complain.
    cat >/dev/null 2>&1 || true

    # Avoid re-prompting loops: if the runtime already set stop_hook_active
    # on a previous block, let this Stop through.
    local stop_hook_active="${CLAUDE_STOP_HOOK_ACTIVE:-false}"
    if [ "$stop_hook_active" = "true" ]; then
        # The Stop schema rejects custom hookSpecificOutput status fields;
        # an empty object is the canonical schema-valid allow output.
        hook_emit_noop
        exit 0
    fi

    # No turn file = no gate (e.g. MCP was unavailable).
    if [ ! -f "$turn_file" ]; then
        hook_emit_noop
        exit 0
    fi

    local stale_reason
    stale_reason="$(_hook_cached_session_stale_reason "$turn_file" 2>/dev/null || true)"
    if [ -n "$stale_reason" ]; then
        hook_emit_block "$stale_reason"
        exit 0
    fi

    local turn_status turn_id build_status code_edits
    turn_status="$(yaml_get "$turn_file" status || true)"
    turn_id="$(yaml_get "$turn_file" turnRequestId || true)"
    build_status="$(yaml_get "$turn_file" lastBuildStatus || true)"
    code_edits="$(yaml_get "$turn_file" codeEdits || true)"
    code_edits="${code_edits:-0}"

    # Gate 1 - turn not completed. Self-heal by auto-completing the turn via
    # the repl-invoke shim, enriched from the Codex JSONL when recorded.
    if [ "$turn_status" = "in_progress" ]; then
        if [ -f "$HOOK_LIB_DIR/repl-invoke.sh" ]; then
            local codex_jsonl_path_sg auto_params jsonl_params
            codex_jsonl_path_sg="$(yaml_get "$turn_file" codexJsonlPath 2>/dev/null | tr -d '"' || true)"
            auto_params="response: |
    Auto-closed by stop-gate.sh (turn self-heal). The agent cannot invoke workflow.sessionlog.* directly; the hook now finalizes the turn when the response finishes."
            if [ -n "$codex_jsonl_path_sg" ] && [ -f "$codex_jsonl_path_sg" ] && command -v node >/dev/null 2>&1; then
                jsonl_params="$(node "${HOOK_LIB_DIR}/codex-jsonl-enrich.js" "$codex_jsonl_path_sg" "Auto-closed by stop-gate.sh (turn self-heal; enriched from Codex JSONL)" 2>/dev/null || true)"
                [ -n "$jsonl_params" ] && auto_params="$jsonl_params"
            fi
            local complete_timeout complete_params_file
            complete_timeout="${MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS:-${REPL_SESSIONLOG_REPL_TIMEOUT:-8}}"
            case "$complete_timeout" in
                ''|*[!0-9]*) complete_timeout=8 ;;
            esac
            complete_params_file="${CACHE_DIR}/stop-gate-complete-$$.yaml"
            printf '%s\n' "$auto_params" > "$complete_params_file"
            if ! REPL_INVOKE_CACHE_DIR="$CACHE_DIR" REPL_TIMEOUT="$complete_timeout" \
                run_with_timeout "$complete_timeout" bash -c '
                    set -uo pipefail
                    # shellcheck source=/dev/null
                    source "$1"
                    _repl_workflow_complete_turn "$(cat "$2")"
                ' _ "$HOOK_LIB_DIR/repl-invoke.sh" "$complete_params_file" >/dev/null 2>&1; then
                rm -f "$complete_params_file" 2>/dev/null || true
                hook_emit_block "Session log turn ${turn_id} could not be auto-closed within ${complete_timeout}s. Check MCP server availability or repair the scoped cache."
                exit 0
            fi
            rm -f "$complete_params_file" 2>/dev/null || true
            turn_status="$(yaml_get "$turn_file" status || true)"
        fi
        if [ "$turn_status" = "in_progress" ]; then
            hook_emit_block "Session log turn ${turn_id} could not be auto-closed. Check plugin/lib/repl-invoke.sh or MCP server availability."
            exit 0
        fi
    fi

    # Gate 2 - build broken after a code edit.
    if [ "$code_edits" -gt 0 ] && [ "$build_status" = "failed" ]; then
        if [ -f "$CACHE_DIR/turn-accept-failure.marker" ]; then
            rm -f "$CACHE_DIR/turn-accept-failure.marker"
        else
            hook_emit_block "Last build in this turn failed after ${code_edits} code edit(s). Fix the build errors before claiming done, or explicitly accept failure by writing ${CACHE_DIR}/turn-accept-failure.marker."
            exit 0
        fi
    fi

    # Gate 3 - session-log audit completeness after code edits
    # (PLAN-SESSIONLOGENFORCEMENT-001, ported from codex). Enforced only when
    # the turn cache carries the audit schema (auditActions present); caches
    # that predate the audit fields remain exempt (backward compatible).
    if grep -q '^auditActions:' "$turn_file" 2>/dev/null && [ "$code_edits" -gt 0 ]; then
        local audit_actions audit_files audit_dialog audit_decisions missing
        audit_actions="$(yaml_get "$turn_file" auditActions || true)"
        audit_files="$(yaml_get "$turn_file" auditFiles || true)"
        audit_dialog="$(yaml_get "$turn_file" auditDialog || true)"
        audit_decisions="$(yaml_get "$turn_file" auditDecisions || true)"
        audit_actions="${audit_actions:-0}"
        audit_files="${audit_files:-0}"
        audit_dialog="${audit_dialog:-0}"
        audit_decisions="${audit_decisions:-0}"
        missing=""
        [ "$audit_actions" -ge 1 ] 2>/dev/null || missing="${missing} actions"
        [ "$audit_files" -ge 1 ] 2>/dev/null || missing="${missing} filesModified"
        if ! { [ "$audit_dialog" -ge 1 ] 2>/dev/null || [ "$audit_decisions" -ge 1 ] 2>/dev/null; }; then
            missing="${missing} processingDialog/designDecisions"
        fi
        if [ -n "$missing" ]; then
            if [ -f "$CACHE_DIR/turn-accept-incomplete-audit.marker" ]; then
                rm -f "$CACHE_DIR/turn-accept-incomplete-audit.marker"
            else
                hook_emit_block "Turn ${turn_id} made ${code_edits} code edit(s) but the session-log audit is incomplete (missing:${missing}). Record them with workflow.sessionlog.appendActions and appendDialog (or workflow.sessionlog.closeTurn), or write the scoped turn-accept-incomplete-audit.marker to accept."
                exit 0
            fi
        fi
    fi

    # All gates passed. Emit the canonical schema-valid no-op.
    hook_emit_noop
    exit 0
}

# ---------------------------------------------------------------------------
# code-verify
# ---------------------------------------------------------------------------

code_verify_main() {
    local turn_file="$CACHE_DIR/current-turn.yaml"

    if ! acquire_hook_lock "code-verify"; then
        hook_emit_event "PostToolUse" "already-running"
        exit 0
    fi

    hook_require_repl_invoke

    local payload
    payload="$(cat 2>/dev/null || true)"

    local file_path
    file_path="$(payload_field "$payload" '.tool_input.file_path' 'file_path')"
    if [ -z "$file_path" ] || [ ! -f "$file_path" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no file"'
        exit 0
    fi

    # Only verify source files.
    local ext verify_kind
    ext="${file_path##*.}"
    case "$ext" in
        cs|axaml|xaml|csproj|vbproj|fsproj|razor|cshtml)
            verify_kind="dotnet"
            ;;
        ts|tsx|js|jsx)
            verify_kind="node"
            ;;
        *)
            hook_emit_event "PostToolUse" "skipped" "\"reason\":\"unsupported-ext\",\"ext\":\"${ext}\""
            exit 0
            ;;
    esac

    _code_verify_find_nearest_project() {
        local start="$1"
        local dir
        dir="$(dirname "$start")"
        while [ "$dir" != "/" ] && [ "$dir" != "." ] && [ -n "$dir" ]; do
            case "$verify_kind" in
                dotnet)
                    local candidate
                    candidate="$(find "$dir" -maxdepth 1 -name '*.csproj' -o -maxdepth 1 -name '*.fsproj' -o -maxdepth 1 -name '*.vbproj' 2>/dev/null | head -1)"
                    if [ -n "$candidate" ]; then
                        printf '%s' "$candidate"
                        return 0
                    fi
                    ;;
                node)
                    if [ -f "$dir/package.json" ]; then
                        printf '%s' "$dir/package.json"
                        return 0
                    fi
                    ;;
            esac
            dir="$(dirname "$dir")"
        done
        return 1
    }

    local project
    project="$(_code_verify_find_nearest_project "$file_path" || true)"

    if [ -z "$project" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no-project-found"'
        exit 0
    fi

    local build_log="$CACHE_DIR/last-build.log"
    mkdir -p "$CACHE_DIR"

    _code_verify_run() {
        case "$verify_kind" in
            dotnet)
                # --nologo quiets header; -clp:NoSummary strips summary block
                dotnet build "$project" --nologo -clp:NoSummary 2>&1
                ;;
            node)
                # Prefer tsc --noEmit when tsconfig present, else skip
                local proj_dir
                proj_dir="$(dirname "$project")"
                if [ -f "$proj_dir/tsconfig.json" ] && command -v npx >/dev/null 2>&1; then
                    (cd "$proj_dir" && npx -y tsc --noEmit 2>&1)
                else
                    echo "skipped: no tsconfig.json or npx"
                    return 0
                fi
                ;;
        esac
    }

    local build_out build_status
    build_out="$(_code_verify_run || echo "__BUILD_FAILED_SENTINEL__")"

    if printf '%s' "$build_out" | grep -q '__BUILD_FAILED_SENTINEL__'; then
        build_status="failed"
    else
        # Dotnet returns 0 even when Build Succeeded; check for "Build FAILED"
        if printf '%s' "$build_out" | grep -qi "Build FAILED\|error CS\|error AVLN"; then
            build_status="failed"
        else
            build_status="succeeded"
        fi
    fi

    printf '%s\n' "$build_out" > "$build_log"

    # Update current-turn.yaml. Only set lastBuildStatus here: codeEdits are
    # owned by workflow.sessionlog.appendActions so one write/edit event is
    # counted exactly once (codex single-owner rule; fixes the double count).
    if [ -f "$turn_file" ]; then
        local tmp
        tmp="$(mktemp)"
        awk -v status="$build_status" '
            /^lastBuildStatus:/ { print "lastBuildStatus: " status; next }
            { print }
        ' "$turn_file" > "$tmp" && mv "$tmp" "$turn_file"
    fi

    # Append session log action
    if type repl_invoke >/dev/null 2>&1 && [ -f "$turn_file" ]; then
        local turn_id
        turn_id="$(yaml_get "$turn_file" turnRequestId || true)"
        if [ -n "$turn_id" ]; then
            local action_params="actions:
  - order: 1
    description: \"Auto-logged Edit/Write of ${file_path} (build ${build_status})\"
    type: edit
    status: completed
    filePath: \"${file_path}\""
            repl_invoke_timed "workflow.sessionlog.appendActions" "$action_params" >/dev/null 2>&1 || true
        fi
    fi

    # Output
    if [ "$build_status" = "failed" ]; then
        local errors errors_json msg
        errors="$(printf '%s' "$build_out" | grep -iE 'error (CS|AVLN|MSB)' | head -10)"
        [ -z "$errors" ] && errors="$(printf '%s' "$build_out" | tail -20)"
        errors_json="$(printf '%s' "$errors" | awk 'BEGIN{ORS="\\n"} {gsub(/"/, "\\\""); print}')"
        msg="Build FAILED after edit to ${file_path}. Fix before continuing:\\n${errors_json}"
        hook_emit_event "PostToolUse" "build-failed" "\"additionalContext\":\"${msg}\""
    else
        hook_emit_event "PostToolUse" "build-${build_status}" "\"project\":\"${project}\""
    fi
    exit 0
}

# ---------------------------------------------------------------------------
# plan-approved / plan-modified
# ---------------------------------------------------------------------------

plan_approved_main() {
    local plan_map="$CACHE_DIR/plan-todo-map.yaml"

    hook_require_repl_invoke_strict

    # Resolve plan file path from TOOL_INPUT or first argument
    local plan_file="${TOOL_INPUT:-${1:-}}"

    if [ -z "$plan_file" ] || [ ! -f "$plan_file" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no plan file"'
        exit 0
    fi

    # Extract title from first # heading
    local plan_title
    plan_title=$(grep -m1 '^# ' "$plan_file" 2>/dev/null | sed 's/^# //' || true)

    if [ -z "$plan_title" ]; then
        plan_title="$(basename "$plan_file" .md)"
    fi

    # Create TODO via REPL
    local todo_params="title: ${plan_title}
source: plan
planFile: ${plan_file}"

    local todo_response todo_id
    todo_response=$(repl_invoke "todo.create" "$todo_params" 2>/dev/null || echo "")
    todo_id=$(echo "$todo_response" | grep '^id:' | head -1 | sed 's/^id:[[:space:]]*//' || echo "")

    # Persist the plan -> todo mapping
    mkdir -p "$CACHE_DIR"
    if [ ! -f "$plan_map" ]; then
        cat > "$plan_map" << 'YAML'
entries: []
YAML
    fi

    cat >> "$plan_map" << YAML
  - planFile: ${plan_file}
    todoId: ${todo_id}
    title: ${plan_title}
    createdAt: "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
YAML

    hook_emit_event "PostToolUse" "created" "\"todoId\":\"${todo_id}\",\"title\":\"${plan_title}\""
}

plan_modified_main() {
    local plan_map="$CACHE_DIR/plan-todo-map.yaml"

    hook_require_repl_invoke_strict

    # Resolve file path from TOOL_INPUT or first argument
    local file_path="${TOOL_INPUT:-${1:-}}"

    if [ -z "$file_path" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no file path"'
        exit 0
    fi

    if [ ! -f "$plan_map" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no plan-todo-map"'
        exit 0
    fi

    # Look up the file in the mapping
    local todo_id
    todo_id=$(grep -A2 "planFile: ${file_path}" "$plan_map" 2>/dev/null \
        | grep 'todoId:' | head -1 | sed 's/.*todoId:[[:space:]]*//' || true)

    if [ -z "$todo_id" ]; then
        hook_emit_event "PostToolUse" "skipped" '"reason":"no mapping for file"'
        exit 0
    fi

    # Update the TODO
    local update_params="id: ${todo_id}
planFile: ${file_path}
status: modified"

    repl_invoke "todo.update" "$update_params" >/dev/null 2>&1 || true

    hook_emit_event "PostToolUse" "updated" "\"todoId\":\"${todo_id}\""
}

# ---------------------------------------------------------------------------
# cache-flush / health-check
# ---------------------------------------------------------------------------

cache_flush_main() {
    hook_require_cache_manager

    local result
    result=$(cache_flush 2>/dev/null || echo "flushed=0 failed=0 pending=0")
    echo "$result"
}

health_check_main() {
    hook_require_repl_invoke_strict

    if repl_invoke "workflow.sessionlog.bootstrap" "" >/dev/null 2>&1; then
        echo "health: ok"
        exit 0
    else
        echo "health: failed" >&2
        exit 1
    fi
}

# ---------------------------------------------------------------------------
# subagent-import
# ---------------------------------------------------------------------------

subagent_import_main() {
    if ! command -v node >/dev/null 2>&1; then
        exit 0
    fi

    local parent_jsonl="${CODEX_SESSION_FILE:-${CODEX_ROLLOUT_FILE:-}}"
    if [ -z "$parent_jsonl" ] && [ -f "$CACHE_DIR/current-turn.yaml" ]; then
        parent_jsonl="$(yaml_get "$CACHE_DIR/current-turn.yaml" codexJsonlPath 2>/dev/null | tr -d '"' || true)"
    fi

    if [ -z "$parent_jsonl" ] || [ ! -f "$parent_jsonl" ]; then
        exit 0
    fi

    # Load session state to get session ID
    local session_id
    session_id="$(yaml_get "$CACHE_DIR/session-state.yaml" sessionId 2>/dev/null || true)"
    if [ -z "$session_id" ]; then
        exit 0
    fi

    # Get parent turn request ID for linking
    local parent_turn_id
    parent_turn_id="$(yaml_get "$CACHE_DIR/current-turn.yaml" turnRequestId 2>/dev/null || true)"

    hook_require_repl_invoke

    # Discover subagent transcripts from the parent JSONL
    local subagents_json
    subagents_json="$(node "${HOOK_LIB_DIR}/codex-jsonl.js" subagents "$parent_jsonl" 2>/dev/null || true)"
    if [ -z "$subagents_json" ] || [ "$subagents_json" = "[]" ]; then
        exit 0
    fi

    # Track imported subagent sessions to prevent duplicates
    local imported_tracker="$CACHE_DIR/subagent-imported.txt"
    touch "$imported_tracker" 2>/dev/null || true

    # Process each subagent
    echo "$subagents_json" | node -e "
const data = require('fs').readFileSync(0,'utf8').trim();
let subs = [];
try { subs = JSON.parse(data); } catch {}
for (const sub of subs) {
  process.stdout.write(sub.path + '\t' + (sub.agentNickname || '') + '\t' + (sub.sessionId || '') + '\n');
}
" 2>/dev/null | while IFS=$'\t' read -r subagent_path nickname subagent_session_id; do
        [ -z "$subagent_path" ] || [ ! -f "$subagent_path" ] && continue

        # Skip if already imported
        local import_key="${subagent_session_id:-${subagent_path}}"
        if grep -qxF "$import_key" "$imported_tracker" 2>/dev/null; then
            continue
        fi

        # Import the subagent transcript
        local import_lines
        import_lines="$(node "${HOOK_LIB_DIR}/codex-jsonl.js" import "$subagent_path" "$session_id" "$parent_turn_id" 2>/dev/null || true)"
        [ -z "$import_lines" ] && continue

        local success=0
        while IFS=$'\t' read -r method params_b64 label; do
            [ -z "$method" ] || [ -z "$params_b64" ] && continue
            local params
            params="$(printf '%s' "$params_b64" | base64 --decode 2>/dev/null || true)"
            [ -z "$params" ] && continue
            if type repl_invoke >/dev/null 2>&1; then
                if repl_invoke "$method" "$params" >/dev/null 2>&1; then
                    success=1
                fi
            fi
        done <<< "$import_lines"

        # Add parent turn action linking to this subagent
        if [ "$success" = "1" ] && [ -n "$parent_turn_id" ] && type repl_invoke >/dev/null 2>&1; then
            local nick_label="${nickname:-subagent}"
            local append_params="actions:
  - order: 99
    type: session_log
    status: completed
    description: Subagent ${nick_label} transcript imported as MCP session-log turns: ${subagent_path}
    filePath: ${subagent_path}"
            repl_invoke "workflow.sessionlog.appendActions" "$append_params" >/dev/null 2>&1 || true
        fi

        # Mark as imported
        printf '%s\n' "$import_key" >> "$imported_tracker"
    done

    exit 0
}

export -f hook_env_init hook_require_repl_invoke hook_require_repl_invoke_strict \
    hook_require_marker_resolver hook_require_cache_manager hook_require_memory_context \
    run_with_timeout acquire_hook_lock yaml_get hook_json_escape payload_field \
    repl_invoke_timed hook_emit_noop hook_emit_block hook_emit_event cli_emit_status \
    _hook_write_untrusted session_start_main session_end_main pre_compact_main \
    post_compact_main user_prompt_submit_main stop_gate_main code_verify_main \
    plan_approved_main plan_modified_main cache_flush_main health_check_main \
    subagent_import_main 2>/dev/null || true
