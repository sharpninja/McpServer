#!/usr/bin/env bash
# FR-MCP-PLUGINCORE-001: Sync the canonical plugin core into a plugin repo.
# Copies lib-sh/ (and lib-ps/ when present) into <plugin>/lib/ and writes
# CORE-MANIFEST.yaml with per-file sha256 hashes so CI can detect local edits.
#
# Usage: sync-plugin-core.sh <plugin-repo-root> [--include-ps]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CORE_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PLUGIN_ROOT="${1:?usage: sync-plugin-core.sh <plugin-repo-root> [--include-ps]}"
INCLUDE_PS="${2:-}"

if [ ! -d "$PLUGIN_ROOT" ]; then
    echo "error: plugin root not found: $PLUGIN_ROOT" >&2
    exit 1
fi

core_version="$(git -C "$CORE_ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)"
synced_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
manifest="$PLUGIN_ROOT/CORE-MANIFEST.yaml"

hash_file() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | cut -d' ' -f1
    else
        shasum -a 256 "$1" | cut -d' ' -f1
    fi
}

mkdir -p "$PLUGIN_ROOT/lib"

{
    printf 'coreVersion: %s\n' "$core_version"
    printf 'syncedAtUtc: %s\n' "$synced_at"
    printf 'files:\n'
} > "$manifest"

sync_tree() {
    local source_dir="$1"
    [ -d "$source_dir" ] || return 0
    local file rel dest
    while IFS= read -r -d '' file; do
        rel="${file#"$source_dir"/}"
        dest="$PLUGIN_ROOT/lib/$rel"
        mkdir -p "$(dirname "$dest")"
        cp "$file" "$dest"
        printf '  lib/%s: %s\n' "$rel" "$(hash_file "$dest")" >> "$manifest"
    done < <(find "$source_dir" -type f -print0 | sort -z)
}

sync_tree "$CORE_ROOT/lib-sh"
if [ "$INCLUDE_PS" = "--include-ps" ]; then
    sync_tree "$CORE_ROOT/lib-ps"
fi

count="$(grep -c '^  lib/' "$manifest" || true)"
echo "synced $count core files into $PLUGIN_ROOT/lib (core $core_version)"
