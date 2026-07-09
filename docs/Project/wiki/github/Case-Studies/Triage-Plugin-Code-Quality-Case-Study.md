# Case Study Plugin Code Quality Stabilization Through Triage

## Summary

The MCP triage system became the stabilization path for plugin and agent-runtime defects that were discovered while agents were working on unrelated tasks. Instead of forcing the active task to stop for every incidental plugin failure, agents submitted detailed triage reports, continued the user-requested work when possible, and let the triage queue group related failures into research and remediation work.

That workflow exposed a class of issues that normal feature implementation rarely catches. Examples included stale plugin caches, mismatched marker metadata, hook installation drift, REPL wrapper schema drift, split cache roots, session-log append no-ops, missing plugin instruction coverage, and long-running AI triage failures with insufficient output capture. Each defect produced better operational rules, sharper requirements, stronger acceptance criteria, and targeted tests.

## Problem

The plugin ecosystem spans Codex, Claude Code, Claude Cowork, Cline, Cline v2, Copilot, Grok, and OpenCode. Each plugin has to preserve a consistent MCP-first workflow while running in a different host with different hook, cache, skill, and shell behavior.

Before triage was used as the default incidental-defect path, plugin failures tended to interrupt the active user request. Agents either tried to repair the plugin immediately, created manual TODOs with inconsistent detail, or worked around the failure through raw REST calls. That created two risks.

- The user’s active work was delayed by unrelated infrastructure repairs.
- Obscure plugin defects were not captured with enough context to reproduce and harden them later.

The triage system changed the failure mode. Agents were instructed to submit plugin and MCP Server failures through the triage tool, write failsafe YAML for the local failure record, and then continue the active task unless triage itself was unavailable.

## Triage Workflow

The effective workflow had four parts.

1. Detect an incidental MCP Server or plugin failure during normal work.
2. Submit a triage report with the failing command or endpoint, observed error, workspace path, component, and relevant plugin or agent identity.
3. Keep a local failsafe YAML record for the failure, regardless of whether triage submission succeeds.
4. Continue the active user request after successful triage submission. If triage submission fails, stop and notify the user because the reporting path is unavailable.

On the server side, reports were grouped into workspace-scoped triage groups. MCP Server related failures, including plugin failures, were grouped into the `McpServer` workspace when that workspace exists. Related reports reset the quiet window so the triage agent can process a batch rather than a single incomplete symptom.

## Edge Cases Surfaced

### Stale Plugin Cache And Marker Metadata

Agents reported cases where the active plugin version was correct, but the marker still advertised an old plugin version. That made agents distrust the wrong source of truth. The remediation was to make marker generation populate plugin versions from current plugin state and to make plugins watch the marker timestamp so changed markers are reprocessed before a request.

This became a requirement-level lesson. Marker fields that affect agent behavior cannot be static advisory text. They must be generated from the current server and plugin state, and plugin runtimes must treat marker changes as invalidating cached connection and identity data.

### Hook Installation Drift

Claude Code showed a particularly obscure failure mode. The plugin package included hooks, but Claude’s active user settings did not necessarily wire those hooks into the running session. That meant skills were installed, but enforcement still did not run consistently.

Triage turned that into explicit plugin guidance and validation work. Claude needed a hook-validation skill that could be triggered from the marker file, inspect the active settings, remove stale plugin cache entries, and install the required hooks into the active Claude configuration.

### Split Cache Roots

The plugin runtime exposed a split between flat session cache and workspace-scoped turn cache. One hook path wrote one cache shape while another wrapper path read another. The visible symptom was that `appendActions` could no-op because `current-turn.yaml` was missing from the resolved cache directory.

That edge case forced tighter requirements around cache identity. Cache resolution must be workspace-aware, marker-aware, and agent-aware. Wrapper calls must not silently succeed when no active turn exists.

### REPL Surface Drift

Triage also surfaced gaps where plugin wrappers, `client.*` passthrough, and typed `workflow.*` REPL surfaces did not expose the same capability. One reported caveat was that triage feature files existed in the cached plugin, but the status surface was not advertising `workflow.triage`.

The resulting acceptance criteria became more precise. Full REPL parity must be tested through client passthrough and typed workflow wrappers, and status or discovery surfaces must not lag the callable command surface.

### Shell Runtime Drift

The plugin stabilization effort also found that Bash and Node helper paths made behavior diverge across agents and Windows hosts. Triage reports and follow-up analysis drove the PowerShell-only plugin runtime plan. Normal plugin operations must use `pwsh` through `PowerShell.MCP`. Bash is allowed only to install PowerShell. JSON, YAML, and envelope construction must be PowerShell-native.

That led to stronger TDD expectations. Any behavior previously covered by Bats needed Pester parity before the Bash and Bats surfaces could be removed.

### Triage Agent Observability

When a triage AI run timed out, the stored run record initially showed only timeout status without enough stdout, raw output, or command-line detail to diagnose what the agent did. That was a triage-system quality defect, discovered through triage itself.

The improvement was to stream and append triage agent output like the existing Copilot plan, status, and implement actions. Complex reports also needed a longer runtime budget than the original ten-minute ceiling.

## Requirements Improvements

The triage feedback loop changed requirements from broad intent into enforceable contracts.

Examples of improved requirement shape.

- Plugin failure reporting must use triage and failsafe YAML, then continue current work after successful submission.
- If triage submission fails, the agent must stop and notify the user rather than silently falling back to raw REST or alternate reporting.
- Plugin version and marker metadata must be generated from current state, not stale template text.
- Plugins must reprocess marker files when marker timestamps change before a request.
- REPL parity must include REST, `McpServerClient`, `client.*` passthrough, typed `workflow.*` wrappers, YAML validation, error envelopes, and status discoverability.
- PowerShell-only plugin runtime behavior must be proven with Pester parity before Bash or Node paths are removed.
- Triage runs must capture exact command lines, streamed stdout or stderr, result JSON, status, and failure details.

The important shift was that each edge case became a requirement with observable acceptance criteria, not just an implementation note.

## Acceptance Criteria Improvements

Triage reports exposed where acceptance criteria had been too vague. The stabilization work made AC more concrete in these areas.

- A plugin cache refresh is not complete unless the active agent can prove it loaded the new plugin version and removed stale cache entries.
- Hook installation is not complete unless active host settings contain the required hook entries, not merely because the plugin package ships hook files.
- A session-log append path is not complete unless server history shows the turn or the wrapper reports an explicit failure.
- Triage dashboard work is not complete unless failed groups can be retried and triage-created TODOs are visible in the same user-facing queue as other open TODOs.
- A PowerShell-only plugin migration is not complete unless shipped plugin packages contain no normal-operation Bash, Node helper, or Bash command references.
- Warning remediation is not complete unless unapproved suppressions are fixed in code, approved suppressions are captured in a durable register, and aiUnit audits the suppression decisions.

These criteria are intentionally testable. They describe the externally visible result the agent or user can observe.

## Test Improvements

The triage-driven stabilization work added or strengthened tests in several categories.

- Pester tests for plugin runtime behavior that had previously been covered by Bats.
- Static package gates proving plugin distributions do not ship forbidden Bash or Node runtime files.
- Wrapper tests for REPL command shapes and parity across typed workflow and client passthrough paths.
- Plugin skill and marker tests proving agents receive correct triage, failsafe, hook-validation, and identity instructions.
- Triage UI and API tests for queue visibility, run history, retry behavior, and created TODO projection.
- aiUnit governance tests for warning suppression decisions and acceptance criteria coverage.

The tests were not only regression checks. They became the executable form of the newly discovered requirements.

## Outcome

Triage made plugin quality work less reactive. Instead of treating every plugin failure as an interruption, agents captured enough structured detail for the server to group, research, and convert the failure into durable remediation work.

The strongest result was not only the fixes themselves. The stronger result was the process change.

- Incidental plugin bugs are reported consistently.
- The active user task continues when reporting succeeds.
- Local failsafe evidence is always retained.
- Edge cases become requirements.
- Requirements receive acceptance criteria.
- Acceptance criteria receive tests.
- Plugin and MCP Server quality can improve without relying on memory of one-off failures.

This case study should be used as the model for future plugin hardening work. When a plugin defect appears obscure, intermittent, or host-specific, it belongs in triage with enough evidence to reproduce the edge case and enough follow-through to convert the lesson into requirements, acceptance criteria, and tests.
