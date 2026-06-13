#!/usr/bin/env bash
# FR-MCP-PLUGINCORE-002: CI checksum guard. Verifies every file listed in a
# plugin repo's CORE-MANIFEST.yaml still matches its synced sha256. A mismatch
# means someone edited a synced core file locally - the fix belongs in
# McpServer/plugins/core followed by a re-sync.
#
# Usage: check-core-integrity.sh <plugin-repo-root>
set -euo pipefail

PLUGIN_ROOT="${1:?usage: check-core-integrity.sh <plugin-repo-root>}"
manifest="$PLUGIN_ROOT/CORE-MANIFEST.yaml"

if [ ! -f "$manifest" ]; then
    echo "error: no CORE-MANIFEST.yaml in $PLUGIN_ROOT (run sync-plugin-core first)" >&2
    exit 1
fi

hash_file() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | cut -d' ' -f1
    else
        shasum -a 256 "$1" | cut -d' ' -f1
    fi
}

failures=0
checked=0
while IFS= read -r line; do
    case "$line" in
        "  lib/"*)
            rel="${line%%:*}"
            rel="${rel#"  "}"
            expected="${line##*: }"
            target="$PLUGIN_ROOT/$rel"
            checked=$((checked + 1))
            if [ ! -f "$target" ]; then
                echo "MISSING: $rel (listed in manifest, not on disk)" >&2
                failures=$((failures + 1))
                continue
            fi
            actual="$(hash_file "$target")"
            if [ "$actual" != "$expected" ]; then
                echo "MODIFIED: $rel (local edit detected - edit McpServer/plugins/core and re-sync)" >&2
                failures=$((failures + 1))
            fi
            ;;
    esac
done < "$manifest"

if [ "$checked" -eq 0 ]; then
    echo "error: manifest lists no files" >&2
    exit 1
fi

if [ "$failures" -gt 0 ]; then
    echo "core integrity check FAILED: $failures of $checked files diverged" >&2
    exit 1
fi

echo "core integrity OK: $checked files match"
