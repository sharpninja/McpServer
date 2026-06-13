#!/usr/bin/env bash
# plugin-env.template.sh - canonical host-knob surface for the McpServer
# plugin core (Phase 2, plugins/core). Each plugin repo ships lib/plugin-env.sh
# generated from this template (set MCP_PLUGIN_HOST before sourcing, or copy
# the matching case branch). Core libraries read ONLY the neutral names
# exported here; host-specific env vars are mapped in this file and nowhere
# else.
#
# Per-plugin values (from the Phase 2 reconciliation report):
#
#   Knob                      claude-code   cowork         codex    copilot   grok
#   PLUGIN_AGENT_DEFAULT      ClaudeCode    ClaudeCowork   Codex    Copilot   GrokCode
#   PLUGIN_MODEL_DEFAULT      claude        claude         codex    copilot   grok
#   PLUGIN_TAG                claude-code   claude-cowork  codex    copilot   grok
#   MCP_HOOK_OUTPUT_MODE      hook          hook           cli      hook      hook
#   MCP_PLUGIN_ROOT chain     CLAUDE_       CLAUDE_        CODEX_   PLUGIN_   GROK_PLUGIN_ROOT >
#                             PLUGIN_ROOT   PLUGIN_ROOT    PLUGIN_  ROOT >    PLUGIN_ROOT >
#                                                          ROOT     CLAUDE_   CLAUDE_PLUGIN_ROOT
#                                                                   PLUGIN_
#                                                                   ROOT
#   MCP_WORKSPACE_START_DIR   CLAUDE_       COWORK_WORKSPACE_PATH > pwd       pwd      CLAUDE_
#   chain                     PROJECT_DIR   MCPSERVER_WORKSPACE_PATH >                 PROJECT_DIR
#                             > pwd         MCP_WORKSPACE_PATH >                       > pwd
#                                           CLAUDE_COWORK_WORKSPACE_PATH >
#                                           CLAUDE_PROJECT_DIR > pwd
#   Wrapper script depth      hooks/scripts (../..) for claude-code, cowork,
#                             copilot, grok; lib (..) for codex
#
# Pass-through knobs (no per-host values; core reads them directly):
#   REPL_TIMEOUT, REPL_SESSIONLOG_REPL_TIMEOUT (default 8),
#   MCP_PLUGIN_STALE_LOCK_SECONDS (default 120),
#   MCP_CACHE_DIR_OVERRIDE (precedence 1), PLUGIN_ROOT_OVERRIDE (test hook),
#   MCP_SESSION_ID, MCP_SESSION_TITLE,
#   MCPSERVER_WORKSPACE_PATH / MCP_WORKSPACE_PATH,
#   MCPSERVER_REQUIREMENTS_PREFER_WORKFLOW_CREATE
#     (alias: MCP_CODEX_REQUIREMENTS_PREFER_WORKFLOW_CREATE),
#   MCPSERVER_INTERNAL_TODO
#     (aliases: MCPSERVER_CODEX_INTERNAL_TODO, MCP_CODEX_INTERNAL_TODO),
#   CODEX_SESSION_FILE / CODEX_ROLLOUT_FILE / CODEX_SESSION_DIR
#     (JSONL discovery gates; the blocks self-disable when unset),
#   CLAUDE_STOP_HOOK_ACTIVE (read by that name on all hosts),
#   PLUGIN_AGENT_NAME (failsafe plugin name; auto-derives from repo dirname),
#   SESSION_* contract vars consumed by the lib JS toolkit.

# Idempotence guard.
if [ -n "${MCP_PLUGIN_ENV_LOADED:-}" ]; then
    return 0 2>/dev/null || true
fi
MCP_PLUGIN_ENV_LOADED=1

_PLUGIN_ENV_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MCP_PLUGIN_HOST="${MCP_PLUGIN_HOST:-claude-code}"

_plugin_env_first_dir() {
    # Echo the first argument that names an existing directory; empty otherwise.
    local candidate
    for candidate in "$@"; do
        if [ -n "$candidate" ] && [ -d "$candidate" ]; then
            printf '%s' "$candidate"
            return 0
        fi
    done
    return 1
}

# Reminder body shared by the Claude-family + copilot + grok hosts.
_PLUGIN_ENV_REMINDER_CLAUDE='session log turn __TURN_REQUEST_ID__ is now active. __INTERNAL_TODO_REMINDER__ The stop-gate hook will auto-close the turn on finalize. PostToolUse/Write|Edit hooks auto-log actions. If you want richer action metadata, POST /mcpserver/sessionlog directly with the workspace API key from AGENTS-README-FIRST.yaml.'

_PLUGIN_ENV_REMINDER_COWORK='session log turn __TURN_REQUEST_ID__ is now active. Use the installed mcpserver connector/skills for MCP work. __INTERNAL_TODO_REMINDER__ The stop-gate hook will auto-close the turn when hooks are available; if MCP is unavailable, preserve local handoff/failsafe files for later import.'

_PLUGIN_ENV_REMINDER_CODEX='A session log turn is active. Use McpServer as the default source of task continuity:
1. Prefer session/task state and recent checkpoints over asking the user for context.
2. __INTERNAL_TODO_REMINDER__
3. For attached Android validation, use adb_step for screenshot -> inspect -> act -> screenshot loops.
4. After meaningful progress or a failed validation cycle, record/update the session log.
5. Run code-verify.sh after source edits and stop-gate.sh before the final response.'

case "$MCP_PLUGIN_HOST" in
    claude-code)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-ClaudeCode}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-claude}"
        PLUGIN_TAG="${PLUGIN_TAG:-claude-code}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-hook}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-}}"
        _start_dir="$(_plugin_env_first_dir "${CLAUDE_PROJECT_DIR:-}" 2>/dev/null || true)"
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_CLAUDE}"
        ;;
    cowork|claude-cowork)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-ClaudeCowork}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-claude}"
        PLUGIN_TAG="${PLUGIN_TAG:-claude-cowork}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-hook}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-}}"
        _start_dir="$(_plugin_env_first_dir "${COWORK_WORKSPACE_PATH:-}" "${MCPSERVER_WORKSPACE_PATH:-}" "${MCP_WORKSPACE_PATH:-}" "${CLAUDE_COWORK_WORKSPACE_PATH:-}" "${CLAUDE_PROJECT_DIR:-}" 2>/dev/null || true)"
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_COWORK}"
        ;;
    codex)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-Codex}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-codex}"
        PLUGIN_TAG="${PLUGIN_TAG:-codex}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-cli}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${CODEX_PLUGIN_ROOT:-}}"
        _start_dir=""
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_CODEX}"
        _codex_todo_enabled="MCP-backed Codex internal TODO tracking is enabled. Mirror durable plan items through workflow.todo.* and keep only transient execution details in Codex's local checklist."
        MCP_INTERNAL_TODO_REMINDER_ENABLED="${MCP_INTERNAL_TODO_REMINDER_ENABLED:-$_codex_todo_enabled}"
        unset _codex_todo_enabled
        ;;
    copilot)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-Copilot}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-copilot}"
        PLUGIN_TAG="${PLUGIN_TAG:-copilot}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-hook}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-}}}"
        _start_dir=""
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_CLAUDE}"
        ;;
    grok)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-GrokCode}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-grok}"
        PLUGIN_TAG="${PLUGIN_TAG:-grok}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-hook}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-${GROK_PLUGIN_ROOT:-${PLUGIN_ROOT:-${CLAUDE_PLUGIN_ROOT:-}}}}"
        _start_dir="$(_plugin_env_first_dir "${CLAUDE_PROJECT_DIR:-}" 2>/dev/null || true)"
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_CLAUDE}"
        ;;
    *)
        PLUGIN_AGENT_DEFAULT="${PLUGIN_AGENT_DEFAULT:-Codex}"
        PLUGIN_MODEL_DEFAULT="${PLUGIN_MODEL_DEFAULT:-codex}"
        PLUGIN_TAG="${PLUGIN_TAG:-mcpserver}"
        MCP_HOOK_OUTPUT_MODE="${MCP_HOOK_OUTPUT_MODE:-hook}"
        MCP_PLUGIN_ROOT="${MCP_PLUGIN_ROOT:-}"
        _start_dir=""
        MCP_PROMPT_REMINDER_BODY="${MCP_PROMPT_REMINDER_BODY:-$_PLUGIN_ENV_REMINDER_CLAUDE}"
        ;;
esac

# Script-relative plugin-root fallback: lib/ is one level below the root.
if [ -z "$MCP_PLUGIN_ROOT" ]; then
    MCP_PLUGIN_ROOT="$(cd "$_PLUGIN_ENV_SCRIPT_DIR/.." && pwd)"
fi

# Only pin the start dir when a host env var supplied one; otherwise the
# core libs default to pwd at call time.
if [ -n "${_start_dir:-}" ]; then
    MCP_WORKSPACE_START_DIR="${MCP_WORKSPACE_START_DIR:-$_start_dir}"
    export MCP_WORKSPACE_START_DIR
fi
unset _start_dir

# Unified identity trio (collapses MCP_AGENT_NAME / PLUGIN_AGENT_NAME /
# MCP_SESSION_AGENT spellings) + model/title defaults.
MCP_AGENT_NAME="${MCP_AGENT_NAME:-$PLUGIN_AGENT_DEFAULT}"
MCP_AGENT_ID="${MCP_AGENT_ID:-$PLUGIN_AGENT_DEFAULT}"
MCP_SESSION_AGENT="${MCP_SESSION_AGENT:-$PLUGIN_AGENT_DEFAULT}"
MCP_SESSION_MODEL="${MCP_SESSION_MODEL:-$PLUGIN_MODEL_DEFAULT}"
MCP_SESSION_TITLE="${MCP_SESSION_TITLE:-${PLUGIN_AGENT_DEFAULT} plugin session}"

# complete-turn-to-recovery.js host identity.
CT2R_SOURCE_TYPE="${CT2R_SOURCE_TYPE:-$PLUGIN_AGENT_DEFAULT}"
CT2R_MODEL="${CT2R_MODEL:-$PLUGIN_MODEL_DEFAULT}"
CT2R_TITLE="${CT2R_TITLE:-${PLUGIN_AGENT_DEFAULT} turn}"
CT2R_TAGS="${CT2R_TAGS:-$PLUGIN_TAG}"

# Internal-TODO reminder strings consumed by hook-lib begin_turn.
MCP_INTERNAL_TODO_REMINDER_DEFAULT="${MCP_INTERNAL_TODO_REMINDER_DEFAULT:-Use TODO and requirements tools only as needed.}"
MCP_INTERNAL_TODO_REMINDER_ENABLED="${MCP_INTERNAL_TODO_REMINDER_ENABLED:-MCP-backed internal TODO tracking is enabled. Mirror durable plan items through workflow.todo.* and keep only transient execution details in the local checklist.}"

export MCP_PLUGIN_HOST PLUGIN_AGENT_DEFAULT PLUGIN_MODEL_DEFAULT PLUGIN_TAG
export MCP_HOOK_OUTPUT_MODE MCP_PLUGIN_ROOT
export MCP_AGENT_NAME MCP_AGENT_ID MCP_SESSION_AGENT MCP_SESSION_MODEL MCP_SESSION_TITLE
export CT2R_SOURCE_TYPE CT2R_MODEL CT2R_TITLE CT2R_TAGS
export MCP_PROMPT_REMINDER_BODY
export MCP_INTERNAL_TODO_REMINDER_DEFAULT MCP_INTERNAL_TODO_REMINDER_ENABLED
