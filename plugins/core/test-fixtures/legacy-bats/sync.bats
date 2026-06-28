#!/usr/bin/env bats
# FR-MCP-PLUGINCORE-001/002: sync-plugin-core + check-core-integrity contract.

CORE_ROOT="$(cd "$(dirname "$BATS_TEST_FILENAME")/.." && pwd)"

setup() {
    SANDBOX="$(mktemp -d)"
    PLUGIN="$SANDBOX/plugin"
    mkdir -p "$PLUGIN"
    # Stage a disposable helper so the suite is independent of real lib content.
    SMOKE="$CORE_ROOT/lib-sh/_bats-smoke-helper.sh"
    printf '#!/usr/bin/env bash\necho smoke\n' > "$SMOKE"
}

teardown() {
    rm -rf "$SANDBOX"
    rm -f "$SMOKE"
}

@test "sync copies lib-sh into plugin lib and writes manifest with sha256" {
    run bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    [ "$status" -eq 0 ]
    [ -f "$PLUGIN/lib/_bats-smoke-helper.sh" ]
    [ -f "$PLUGIN/CORE-MANIFEST.yaml" ]
    grep -q '^coreVersion: ' "$PLUGIN/CORE-MANIFEST.yaml"
    grep -Eq '^  lib/_bats-smoke-helper\.sh: [0-9a-f]{64}$' "$PLUGIN/CORE-MANIFEST.yaml"
}

@test "integrity guard passes immediately after sync" {
    bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    run bash "$CORE_ROOT/sync/check-core-integrity.sh" "$PLUGIN"
    [ "$status" -eq 0 ]
    [[ "$output" == *"core integrity OK"* ]]
}

@test "integrity guard fails when a synced file is edited locally" {
    bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    echo '# local edit' >> "$PLUGIN/lib/_bats-smoke-helper.sh"
    run bash "$CORE_ROOT/sync/check-core-integrity.sh" "$PLUGIN"
    [ "$status" -eq 1 ]
    [[ "$output" == *"MODIFIED: lib/_bats-smoke-helper.sh"* ]]
}

@test "integrity guard fails when a synced file is deleted" {
    bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    rm "$PLUGIN/lib/_bats-smoke-helper.sh"
    run bash "$CORE_ROOT/sync/check-core-integrity.sh" "$PLUGIN"
    [ "$status" -eq 1 ]
    [[ "$output" == *"MISSING: lib/_bats-smoke-helper.sh"* ]]
}

@test "integrity guard demands a manifest" {
    run bash "$CORE_ROOT/sync/check-core-integrity.sh" "$PLUGIN"
    [ "$status" -eq 1 ]
    [[ "$output" == *"no CORE-MANIFEST.yaml"* ]]
}

@test "re-sync repairs a tampered plugin copy" {
    bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    echo '# local edit' >> "$PLUGIN/lib/_bats-smoke-helper.sh"
    bash "$CORE_ROOT/sync/sync-plugin-core.sh" "$PLUGIN"
    run bash "$CORE_ROOT/sync/check-core-integrity.sh" "$PLUGIN"
    [ "$status" -eq 0 ]
}
