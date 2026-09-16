# P0-A Bounded Re-review: DISAGREE

## Reviewed Artifact and Boundary

- Reviewed all 403 lines of `F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001-completion-20260906.md`.
- SHA-256: `A488D3AC9CA50C78F504B42184BFC3D00733E86F716C9CBF0DD16CC74F03B4A7`.
- Incorporated normative catalog: `F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md`, independently verified SHA-256 `7D83528F9097AFBB894AB76545F06B2D702D0A16BD2F5D7FB3FBA8BE65E4698E`.
- Preserved prior review: `F:\GitHub\McpServer\docs\receipts\completion-program-20260906\p0a-hostile-20260906T185500Z-01a07803.md`.
- Scope: the original seven findings and consistency of their remedies, including the parent-supplied C10 precision concerns and live TEST requirement mutation evidence. This is plan review, not implementation or execution approval.
- No product, TODO, requirement, mapping, commit, merge, deployment, parent-console, or parent-session changes. No Linux validation, recovery, installation, or service restart attempted. No additional program inventory performed.

## Concrete Residual Blockers

### 1. P1: Amendment F Cannot Match the Live TEST Criterion

Location: corrected plan line 127; affected gate G0, lines 295-300. This is the remaining portion of original finding 6.

The plan requires exactly one live AC whose whole text or linked assertion is exactly `Import_RemapsWorkspaceIdAndPaths_OldIdAbsent`. A fresh supported `workflow.requirements.effective` read returned only AC1 for `TEST-MCP-WIKIEXPORT-004`, with `isSatisfied=false` and this complete text:

```text
Named tests exist: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_RemapsWorkspaceIdAndPaths_OldIdAbsent; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos.
```

Its top-level `condition` is:

```text
AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_RemapsWorkspaceIdAndPaths_OldIdAbsent; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos.
```

No separate linked assertion object was exposed. The prescribed exact whole-criterion predicate matches zero records and therefore must fail without mutation. Altering only AC text would also leave `condition` describing the superseded whole-store assertion.

Minimum correction: target stable AC ID `AC1`; guard the complete current AC1 text and current `condition`; replace only the exact `Import_RemapsWorkspaceIdAndPaths_OldIdAbsent` token with `Import_OperationalColumnsContainOnlyDestinationIdsAndPaths` in both fields. Preserve the other three test names, punctuation/order, AC ID, satisfaction state, evidence/history, and superseded text. Use supported requirement operations with immediate re-query and fail-closed guards. State that G0 verifies unchanged retained criteria plus the approved replacements and appendages, rather than retaining contradictory old active meanings. Update the corresponding normative named-test assertion through explicit supersession. No requirement mutation was performed in this review.

The separate FR/TR AC2 exact-text predicates at lines 123 and 125 match the freshly queried live store. They do not need a new design decision.

### 2. P2: C10's Exact SHA Assertion Is Impossible and Its Later Binding Is Ambiguous

Location: corrected plan line 276 and its reuse at line 375. This is a command-precision remainder of original finding 1.

`git rev-parse HEAD` returns the full object ID, but C10 requires it to equal the eight-character `808ec049`. Thus an otherwise correct checkout cannot satisfy the written assertion. G8 subsequently requires C10 on the final integrated candidate; an unconditional BUG139 SHA assertion cannot also verify that later candidate after program changes.

Minimum correction: freeze the BUG139 expected full SHA as `808ec049d56daf214c391beccd6b9d69bb9867c6` and compare full output from `git rev-parse --verify HEAD` to that exact value. Bind C10 explicitly to an immutable expected candidate SHA/tree supplied by its caller: G1 uses this BUG139 SHA; G8 uses the independently frozen integrated candidate SHA/tree. Record the full observed values, compare them before discovery/execution, and invalidate downstream evidence after any source change. Do not use abbreviation/prefix matching or silently keep validating the historical BUG139 checkout at G8.

### 3. P2: C10 Assumes the Linux PowerShell Executor Before Verifying It

Location: corrected plan line 276. This is the remaining executor-precision portion of original finding 1, not a finding that native Linux must run during P0-A.

The command is fixed as `wsl.exe --exec pwsh -NoProfile -NonInteractive`, while preflight explicitly proves ext4, SHA, non-root identity, and mount capability but does not require resolving and verifying that Linux `pwsh` or its selected distro/user execution path. This review has no evidence that this executor is available or approved; it does not assert that the executable is missing.

Minimum correction: require runtime discovery and verification of the approved executor before any C10 command. Record the selected distro, explicit executable path/version, noninteractive arguments, working directory, non-root identity, and separately authorized mount-capable context. If Linux `pwsh` is selected, prove it exists and works in that exact context. Keep launch through the owned PowerShell.Mcp 1.14 console. If the approved executor is unavailable, stop at the prerequisite gate and obtain authorization for any installation or disruptive repair; do not assume a shell or substitute Windows execution. This is a future execution prerequisite, not a reason to wait for Linux recovery before deciding plan completeness.

## Original Finding Dispositions

1. Partially resolved. Incorporated named catalog, G0, named bounded RED/GREEN gates, cumulative scopes, and post-requirements hostile approval are now explicit. Residuals 2 and 3 above remain.
2. Resolved at plan level. Actual in-memory AgentPool, distinct durable request/attempt identities, process-local correlation, bounded at-least-once execution, restart orphaning, fenced single terminal result, cleanup, four public operations, and read-only status are specified at lines 98-104 and 131-163.
3. Resolved at plan level. Authorized re-resolution, protected one-shot context, bounded inputs/output, transient-only raw prompts, and sanitized non-echoing results are specified at lines 91-95 and 165-175.
4. Resolved at plan level. Receipt-first replay, preserved nonempty checkout, transactional registration/hydration, owned generated paths, and honest recoverable post-commit activation are specified at lines 114-118 and 177-192.
5. Resolved at plan level. Complete DbSet policy registry, inert replayable work, additive schema/selector gates, source-owned Product remap, and authorized externally-owned Product mapping are specified at lines 194-245 and 364-368.
6. Partially resolved. Guarded FR/TR AC2 replacement correctly separates operational identity from immutable historical provenance. Residual 1 remains for the actual TEST record shape.
7. Resolved at plan level. Outbound-only sanitization of scalar, nested, encoded, and diagnostic text with explicit loss policy and hashes over final bytes is specified at lines 107-111 and 247-255.

Dirty-develop preservation, truthful historical model attribution, real plugin/aiUnit execution, the hygiene keep-open hold, migration compatibility, and approval-gated release boundaries remain intact. No historical green receipt is treated as newly executed evidence. The historical provider manifest remains an explicit 107-test inventory, not an invented target or current rerun.

## Runtime Identity and Session Proof

- Reviewer native task ID: `01a07803-5454-7123-a1ee-e4304a59a588`.
- Verified native `turn_context`: timestamp `2026-09-06T19:44:12.051Z`, turn `01a0783b-6564-73b3-b950-8f12dda0f521`, model `gpt-6-astra`, effort `xhigh`.
- Source: `C:\Users\kingd\.codex\sessions\2026\09\06\rollout-2026-09-06T13-37-59-01a07803-5454-7123-a1ee-e4304a59a588.jsonl`. Read-only shared access was used after exclusive `ReadLines` encountered the active writer's sharing restriction.
- Commands used only PowerShell.Mcp 1.14 `execute_command` with isolated `agent_id=sa-7186f21d`. Manual receipt creation uses `apply_patch` as required.
- Installed wrapper: `C:\Users\kingd\.codex\plugins\cache\mcpserver-codex-plugin\mcpserver\1.106.0\Invoke-CodexMcpPlugin.ps1`.
- Isolated cache: `F:\GitHub\McpServer\.mcpServer\codex\reviews\01a07803-5454-7123-a1ee-e4304a59a588`.
- Generated MCP session: `Codex-20260906T184446Z-plugin-session`.
- Current review request: `req-20260906T194057Z-prompt-5986`.
- Model-authored interpretation, four review actions, two decision dialogs, and final verdict were submitted through supported plugin operations. The plugin's generic `codex` model label was not rewritten or represented as verified Astra metadata; the native context above is the actual model/effort proof.
- `CompleteTurn` returned without stderr rejection. Independent server proof was obtained at `2026-09-06T19:48:46.0004464Z` through `client.SessionLog.QueryAsync` with TOP-LEVEL parameters `agent='Codex', limit=5`, followed by selection of `items.sessionId` and `turns.requestId`.
- Server returned the exact session and native `agentSessionId`, the exact review request, `status=completed`, four actions, two processing-dialog entries, and the DISAGREE response describing these three corrections and this receipt target. This is server persistence proof, not an inference from wrapper exit status or text-search absence.

## Verdict Boundary

**DISAGREE.** Correct only these residual plan defects, then obtain bounded P0-A re-review before persistent-plan P0-B. Do not begin product implementation on the strength of this review. Linux execution success remains a later gate. Stop after this bounded review.
