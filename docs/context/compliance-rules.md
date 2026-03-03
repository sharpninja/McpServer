# Compliance Rules Reference

Load this file when adding dependencies or using external code.

## License Compliance

Before adding any new dependency:

1. Verify its license is not in the workspace's banned list (check AGENTS-README-FIRST.yaml or workspace config).
2. Log the dependency name, version, and license in the session log as an action with type `dependency_add`.
3. If you cannot determine the license, do not add the dependency — flag it as a blocker.

If you discover an existing dependency uses a banned license, log it as a blocker with type `license_violation` and notify the user.

## Country of Origin Restrictions

Before adding any new dependency:

1. Verify the maintainer/organization's country of origin is not in the workspace's banned list.
2. If the country of origin cannot be determined, flag it as a blocker and ask the user.
3. Log any country-of-origin concerns as an action with type `origin_review`.

If you discover an existing dependency originates from a banned country, log it as a blocker with type `origin_violation` and notify the user.

## Banned Organizations

Do not use, recommend, or reference code maintained by banned organizations listed in the workspace configuration. Log any violations as an action with type `entity_violation`.

## Banned Individuals

Do not use, recommend, or reference code authored or primarily maintained by banned individuals listed in the workspace configuration. Log any violations as an action with type `entity_violation`.

## Where to Find Workspace Restrictions

Workspace-specific banned lists are in the `AGENTS-README-FIRST.yaml` marker file when applicable. If the marker does not list any restrictions, none apply to the workspace.
