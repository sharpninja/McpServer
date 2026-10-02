#!/usr/bin/env bats

PLUGIN_ROOT="$(cd "$(dirname "$BATS_TEST_FILENAME")/.." && pwd)"
SKILL="$PLUGIN_ROOT/skills/memory/SKILL.md"
DESCRIPTOR="$PLUGIN_ROOT/memory-descriptor.json"

@test "memory skill exists and loads" {
    [ -s "$SKILL" ]
    head -1 "$SKILL" | grep -q "^---$"
    grep -q "^name: memory" "$SKILL"
    grep -q "memory_remember" "$SKILL"
    grep -q "memory_recall" "$SKILL"
    grep -q "memory_explore" "$SKILL"
    grep -q "memory_consolidate" "$SKILL"
    grep -q "memory_promote" "$SKILL"
    grep -qi "injection" "$SKILL"
    grep -qi "fallback" "$SKILL"
}

@test "memory descriptor exists and lists required verbs" {
    [ -s "$DESCRIPTOR" ]
    grep -q '"host": "claude-cowork"' "$DESCRIPTOR"
    grep -q "memory_remember" "$DESCRIPTOR"
    grep -q "memory_recall" "$DESCRIPTOR"
    grep -q "memory_explore" "$DESCRIPTOR"
    grep -q "memory_consolidate" "$DESCRIPTOR"
    grep -q "memory_promote" "$DESCRIPTOR"
    grep -q "injection" "$DESCRIPTOR"
    grep -q "fallback" "$DESCRIPTOR"
}
