# Handoff: QuadBrain-only keyserver, all-adapter bypass (stop 2026-09-19)

**Superseded as an active handoff.** PLAN-TXNKEYSERVER-001 is `Done=true` on the Linux box MCP after a publish/swap of `develop` `8f30caf` to `/opt/mcpserver`, live TODO/session-log/requirements proof with `TurnTransactions.Enabled=true`, requirements restore, and hostile AGREE Accuracy 99 Completeness 98. Keep this file for the operator decision and the pre-deploy remaining-work list. The close-out was not Windows Legion Nuke `UpdateService`. Current operator docs: `docs/USER-GUIDE.md` section 7f, `docs/MCP-SERVER.md` QuadBrain-only keyserver. Integration/Validation/Review remain unrun (operator policy).

Copy everything below the line into the next agent. Do not re-litigate the operator decision. Do not mark PLAN-TXNKEYSERVER-001 done until live Nuke deploy plus hostile AGREE on the done claim.

---

Operator stopped work 2026-09-19: write this hand-off, commit-sync, then stop. Do not continue the paused goal of running IntegrationTests / Validation / Review.Tests / Build.Tests / Category=AiReview or Category=Integration unless the operator asks again.

## Operator decision (locked)

Verbatim intent: "Keyserver is only supposed to be used in QuadBrain transactions." Then: "Yes, I approve the change." Then the session-log-only slice was rejected: "Other gated adapters (TODO, requirements, etc.) still use the coordinator; this slice is session-log only / NO! Not valid and unacceptable."

Keep live `Mcp:TurnTransactions:Enabled=true` for QuadBrain. Do not flip that flag off as the fix.

## What shipped in this worktree (not live)

`TurnTransactionKeyserverScope` (`src/McpServer.TransactionSecurity/TurnTransactionKeyserverScope.cs`):

- `RequiresKeyserver` is true only for publisher party prefix `brain-slot:` or operation prefix `brain-slot.` / `quadbrain.`
- `ShouldBypassCoordinator` is true when coordinator is null or `!RequiresKeyserver`

Wired into:

- All `TransactionGated*` adapters (TODO, requirements document/analysis, session-log including QBAgent, memory, repo, prompt templates, tool registry/bucket, GraphRAG, GitHub CLI, GitHub token store, issue-todo-sync, voice, agent pool)
- `TransactionalTodoWorkflow`
- `TurnTransactionFederationOperationApplyService`
- `RequirementsController.ShouldDeferIngest`
- `ContextController.ShouldDeferContextMutation`
- `FederationController.ShouldDeferFederationControlMutation`
- STDIO `FwhMcpTools` / `McpServerMcpTools.ShouldDeferContextMutation`
- `TurnTransactionCoordinator.ExecuteAsync` also skips `SignManifestAsync` unless `RequiresKeyserver`

Still fail-closed: `TransactionGatedSessionLogService.RepairWorkspaceStampsAsync` (uncompensated workspace-stamp repair).

QuadBrain still gated: `BrainSlotInvocationService` (`brain-slot.invoke`) and `QuadBrainOrchestrationService` (`brain-slot.weight-update`).

## Requirements (MCP store is source of truth)

Amended via plugin `workflow.requirements.updateFr/updateTr/updateTest`, then `requirements_generate` `doc=all` `format=markdown` (last generate 2026-09-17T17:47:50Z):

- FR-MCP-173: keyserver is QuadBrain/brain-slot only; all other first-party mutations persist without coordinator/keyserver
- FR-MCP-120: remaining gate is brain-slot.invoke / brain-slot.weight-update; FR-MCP-173 carve-out for the rest
- TR-MCP-TXNKEY-001: `TurnTransactionKeyserverScope` + adapter/controller bypass
- TEST-MCP-221: non-QuadBrain adapters skip coordinator
- TEST-MCP-161: retargeted after HV DISAGREE C-TEST161-not-retargeted to QuadBrain/brain-slot coordinator tests, not general-adapter fail-closed

Markdown: `docs/Project/Functional-Requirements.md`, `Technical-Requirements.md`, `Testing-Requirements.md`, `TR-per-FR-Mapping.md`, `Requirements-Matrix.md`

## Tests already run (do not claim more)

Nuke `.\build.ps1 Test` 2026-09-17 (excludes IntegrationTests, Validation, Review.Tests, Build.Tests; filter `Category!=AiReview&Category!=Integration`):

- Support.Mcp.Tests 2456
- Client.Tests 288
- Cqrs.Tests 33
- Launcher.Tests 20
- McpAgent.Tests 63
- Repl.Core.Tests 849
- QBAgent.Tests 90
- PluginIntegration.Tests 94
- Sum: Passed 3893, Failed 0, Skipped 0
- `NUKE_TEST_EXIT=0`
- Log: `docs/receipts/unit-suite-20260917T180358Z.log` (if present) and HV receipt below

Focused earlier: gated Support.Mcp.Tests 227/0/0, FederationOperationApplyServiceTests 9/0/0, TransactionalTodoWorkflowTests 10/0/0.

Not run: `*.IntegrationTests`, `*.Validation`, `*Review.Tests`, `Build.Tests`, `Category=AiReview`, `Category=Integration`. Operator started a goal to run those, then ordered stop before that work.

## Hostile validation

- `docs/receipts/hostile-validator-20260917T172702Z.md` DISAGREE (FAIL C-TEST161-not-retargeted)
- `docs/receipts/hostile-validator-20260917T174749Z.md` AGREE 99/99 PASS 16 FAIL 0 after TEST-MCP-161 retarget. Does not authorize `done: true`.
- `docs/receipts/hostile-validator-20260917T184144Z.md` AGREE 99/99 PASS 14 FAIL 0 on the Nuke Test count claim. Suite gate only. Does not authorize `done: true`.

jsonl under `docs/receipts/hv/`. HV sessions:

- `GrokSubagentHostile-20260917T172702Z-txnkey-all-adapters` turnId 45218
- `GrokSubagentHostile-20260917T174749Z-txnkey-test161-rereview` turnId 45219
- `GrokSubagentHostile-20260917T184144Z-nuke-test-unit-suite` turnId 45223

## PLAN TODO

`PLAN-TXNKEYSERVER-001` remains `Done=false`. Live `todo.update` on the Windows service still hits the old coordinator (observed: Subscriber commit failed, rollback completed). Do not mark done until:

1. `.\build.ps1 UpdateService` (Nuke only, never hand-copy)
2. Live proof that GrokCode/Codex TODO/requirements/session-log persist without keyserver
3. Hostile AGREE on the done claim (accuracy and completeness both >= 98, request+response jsonl, full verdict in MCP session log)

## Live service (not updated)

Marker `AGENTS-README-FIRST.yaml` at last check: pid 138544, started 2026-09-16T22:28:32Z, exe 1.4.38+6a72d445. Lacks `TurnTransactionKeyserverScope`. Do not treat live MCP mutations as proof of this slice.

## MCP session for this work

- Agent: GrokCode
- Session: `GrokCode-20260917T132412Z-new-mcp-session`
- Stop turn: `req-20260919T152210Z-003-stop-handoff-commit-sync`

## What you must still do (only if operator resumes)

1. Deploy via elevated `.\build.ps1 UpdateService`.
2. Prove live `todo.update` / `sessionlog_begin_turn` / requirements update succeed with `TurnTransactions.Enabled=true` and no keyserver error.
3. Hostile-validate the done claim. Then and only then set PLAN-TXNKEYSERVER-001 `done: true` with HV receipt paths in `doneSummary`.
4. Do not run IntegrationTests/Validation/Review/Build/AiReview unless the operator asks again.

## Out of scope

- Disabling live `TurnTransactions.Enabled`
- Session-log-only keyserver carve-out (operator rejected)
- Marking the PLAN done on repo tests alone
