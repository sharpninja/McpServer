#!/usr/bin/env bash
# core-staging.bash - stage a plugin-shaped root from the canonical core so
# the shared bats suites exercise plugins/core/lib-sh instead of a plugin
# repo's lib/. Sourced at the top of every ported suite.
#
# The staged root mirrors the claude-code layout (the suites' origin):
#   lib/            <- lib-sh/* + lib-ps/* (+ host-named aliases)
#   hooks/scripts/  <- generated wrappers (hooks-templates, claude-code)
#   hooks/hooks.json
#   tests/          <- cache-scope-helper.bash + fixtures/
#   skills/         <- borrowed from a host plugin checkout (host-bound
#                      content; MCP_PLUGIN_SKILLS_SOURCE overrides discovery)
#
# Knobs:
#   CORE_STAGED_PLUGIN_ROOT  - override the staging location.
#   MCP_PLUGIN_SKILLS_SOURCE - skills/ source dir (default: sibling
#                              mcpserver-claude-code-plugin checkout).
#   CORE_STAGE_FORCE=1       - force re-staging.

CORE_STAGING_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CORE_ROOT="$(cd "$CORE_STAGING_DIR/.." && pwd)"
CORE_STAGED_PLUGIN_ROOT="${CORE_STAGED_PLUGIN_ROOT:-$CORE_ROOT/.staged-plugin}"

_core_stage_is_stale() {
    local stamp="$CORE_STAGED_PLUGIN_ROOT/.stamp"
    [ -f "$stamp" ] || return 0
    [ -n "${CORE_STAGE_FORCE:-}" ] && return 0
    local newer
    newer="$(find "$CORE_ROOT/lib-sh" "$CORE_ROOT/lib-ps" "$CORE_ROOT/hooks-templates" \
        -type f -newer "$stamp" 2>/dev/null | head -1)"
    [ -n "$newer" ]
}

core_stage() {
    if ! _core_stage_is_stale; then
        printf '%s\n' "$CORE_STAGED_PLUGIN_ROOT"
        return 0
    fi

    local stage="$CORE_STAGED_PLUGIN_ROOT"
    rm -rf "$stage"
    mkdir -p "$stage/lib" "$stage/tests" "$stage/cache"

    # Canonical sh + js library.
    cp "$CORE_ROOT"/lib-sh/*.sh "$CORE_ROOT"/lib-sh/*.js "$stage/lib/" 2>/dev/null

    # PowerShell twins + host-named aliases used by the claude-code suites.
    cp "$CORE_ROOT"/lib-ps/*.ps1 "$stage/lib/" 2>/dev/null || true
    cp "$CORE_ROOT/lib-ps/Invoke-McpPlugin.ps1" "$stage/lib/Invoke-ClaudeMcpPlugin.ps1" 2>/dev/null || true

    # Host-named status script (label and wrapper name derive from filename).
    cp "$CORE_ROOT/lib-sh/mcp-status.sh" "$stage/lib/mcp.claude.status.sh"

    # Hook wrappers + plugin-env + hooks.json (claude-code flavor).
    bash "$CORE_ROOT/hooks-templates/generate-wrappers.sh" claude-code "$stage" >/dev/null

    # Test support files.
    cp "$CORE_STAGING_DIR/cache-scope-helper.bash" "$stage/tests/cache-scope-helper.bash"
    if [ -d "$CORE_STAGING_DIR/fixtures" ]; then
        mkdir -p "$stage/tests/fixtures"
        cp "$CORE_STAGING_DIR"/fixtures/* "$stage/tests/fixtures/" 2>/dev/null || true
    fi

    # Host-bound skills content (borrowed; suites skip when unavailable).
    local skills_src="${MCP_PLUGIN_SKILLS_SOURCE:-}"
    if [ -z "$skills_src" ]; then
        local sibling
        for sibling in \
            "$CORE_ROOT/../../../mcpserver-claude-code-plugin/skills" \
            "$HOME/GitHub/mcpserver-claude-code-plugin/skills"; do
            if [ -d "$sibling" ]; then
                skills_src="$sibling"
                break
            fi
        done
    fi
    if [ -n "$skills_src" ] && [ -d "$skills_src" ]; then
        mkdir -p "$stage/skills"
        cp -R "$skills_src"/. "$stage/skills/"
    fi

    chmod +x "$stage"/lib/*.sh "$stage"/hooks/scripts/*.sh 2>/dev/null || true
    date -u +%Y-%m-%dT%H:%M:%SZ > "$stage/.stamp"
    printf '%s\n' "$stage"
}

export CORE_ROOT CORE_STAGED_PLUGIN_ROOT
