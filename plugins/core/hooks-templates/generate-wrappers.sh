#!/usr/bin/env bash
# generate-wrappers.sh - emit per-host hook wrappers + lib/plugin-env.sh from
# the shared templates.
#
# Usage: generate-wrappers.sh <host> <plugin-root>
#   host        claude-code | cowork | codex | copilot | grok
#   plugin-root destination plugin repo root (lib/ must already hold the
#               synced core: hook-lib.sh, plugin-env.template.sh, ...)
#
# Wrapper placement follows the host convention: hooks/scripts/ (depth ../..)
# for claude-code, cowork, copilot, grok; lib/ (depth ..) for codex. Codex
# receives only its 5 hook families. hooks.json is emitted for claude-code
# (the canonical Claude-family registration); other hosts keep their per-repo
# hooks.json (schema and root env var differ per host).
set -euo pipefail

TEMPLATE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

HOST="${1:?usage: generate-wrappers.sh <host> <plugin-root>}"
PLUGIN_ROOT="${2:?usage: generate-wrappers.sh <host> <plugin-root>}"

case "$HOST" in
    codex)
        DEPTH=".."
        WRAPPER_DIR="$PLUGIN_ROOT/lib"
        HOOKS="session-start user-prompt-submit stop-gate code-verify subagent-import"
        ;;
    claude-code|cowork|claude-cowork|copilot|grok)
        DEPTH="../.."
        WRAPPER_DIR="$PLUGIN_ROOT/hooks/scripts"
        HOOKS="session-start session-end pre-compact post-compact user-prompt-submit stop-gate code-verify plan-approved plan-modified cache-flush health-check subagent-import"
        ;;
    *)
        echo "error: unknown host '$HOST'" >&2
        exit 1
        ;;
esac

# hook name -> entry function + cache mode
entry_for() {
    case "$1" in
        session-start)      printf 'session_start_main flat' ;;
        session-end)        printf 'session_end_main flat' ;;
        pre-compact)        printf 'pre_compact_main flat' ;;
        post-compact)       printf 'post_compact_main flat' ;;
        user-prompt-submit) printf 'user_prompt_submit_main scoped' ;;
        stop-gate)          printf 'stop_gate_main scoped' ;;
        code-verify)        printf 'code_verify_main scoped' ;;
        plan-approved)      printf 'plan_approved_main flat' ;;
        plan-modified)      printf 'plan_modified_main flat' ;;
        cache-flush)        printf 'cache_flush_main flat' ;;
        health-check)       printf 'health_check_main flat' ;;
        subagent-import)    printf 'subagent_import_main flat' ;;
        *) return 1 ;;
    esac
}

mkdir -p "$WRAPPER_DIR"

for hook in $HOOKS; do
    read -r entry mode <<<"$(entry_for "$hook")"
    out="$WRAPPER_DIR/${hook}.sh"
    sed -e "s/__HOOK_NAME__/${hook}/g" \
        -e "s/__HOST__/${HOST}/g" \
        -e "s|__DEPTH__|${DEPTH}|g" \
        -e "s/__CACHE_MODE__/${mode}/g" \
        -e "s/__ENTRY__/${entry}/g" \
        "$TEMPLATE_DIR/wrapper.sh.template" > "$out"
    chmod +x "$out"
done

# lib/plugin-env.sh instantiation (idempotent two-liner over the template).
ENV_OUT="$PLUGIN_ROOT/lib/plugin-env.sh"
if [ ! -f "$ENV_OUT" ]; then
    cat > "$ENV_OUT" <<EOF
#!/usr/bin/env bash
# plugin-env.sh - host knob defaults for the ${HOST} plugin (generated).
MCP_PLUGIN_HOST="\${MCP_PLUGIN_HOST:-${HOST}}"
# shellcheck source=./plugin-env.template.sh
source "\$(cd "\$(dirname "\${BASH_SOURCE[0]}")" && pwd)/plugin-env.template.sh"
EOF
    chmod +x "$ENV_OUT"
fi

# hooks.json for the canonical Claude-family registration.
if [ "$HOST" = "claude-code" ]; then
    mkdir -p "$PLUGIN_ROOT/hooks"
    cp "$TEMPLATE_DIR/hooks.claude-code.json" "$PLUGIN_ROOT/hooks/hooks.json"
fi

echo "generated ${HOST} wrappers in $WRAPPER_DIR"
