#!/usr/bin/env bats
# Core analogue of the plugin repos' cache-ignore guard: mutable cache state
# under plugins/core must never be tracked by git.

source "$(dirname "$BATS_TEST_FILENAME")/core-staging.bash"

@test "mutable internal TODO cache state is ignored by git" {
    run git -C "$CORE_ROOT" check-ignore cache/internal-todo.yaml
    [ "$status" -eq 0 ]

    run git -C "$CORE_ROOT" ls-files --error-unmatch cache/internal-todo.yaml
    [ "$status" -ne 0 ]
}

@test "staged plugin root is ignored by git" {
    run git -C "$CORE_ROOT" check-ignore .staged-plugin/lib/repl-invoke.sh
    [ "$status" -eq 0 ]
}
