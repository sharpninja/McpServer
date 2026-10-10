# Hostile validation receipt

TimestampUtc: 2026-09-17T17:41:43Z
LaunchTimestampUtc: 2026-09-17T17:27:02Z
ValidatorIdentity: GrokSubagentHostile
WorkspacePath: F:\GitHub\McpServer
Gate: txnkey-all-adapters
TodoId: PLAN-TXNKEYSERVER-001
WorkClass: 1 (project implementation: PLAN-TXNKEYSERVER-001 all non-QuadBrain first-party mutation adapters)
AddProfileExecuted: true
AddProfileFileCount: 19
AddProfileExcludedSkillPort: C:\Users\kingd\.claude\profile\add-profile.grok.md
ProfileFilesRead:
- PROFILE.md
- user-payton-byrd.md
- accuracy-first-verify-sources.md
- approve-before-execute.md
- philosophical-dialogue-mode.md
- log-decisions-as-conclusions.md
- session-turn-title-summary.md
- never-skip-explicit-actions.md
- adversarial-review-global.md
- hv-jsonl-and-session-log.md
- bring-the-receipts.md
- hostile-on-goal-state.md
- hostile-ops-vs-requirements.md
- hostile-phase-gates.md
- lab-authorization.md
- no-attitude-honesty-tell.md
- no-python-lab.md
- no-shortcuts-precision-over-convenience.md
- requirement-change-plan-first.md

HvSessionId: GrokSubagentHostile-20260917T172702Z-txnkey-all-adapters
HvRequestId: req-20260917T172702Z-001-hostile-validate-txnkey-adapters
HvTurnId: 45218
RequestJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T172702Z-txnkey-all-adapters.request.jsonl
ResponseJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T172702Z-txnkey-all-adapters.response.jsonl
ReceiptMarkdown: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T172702Z.md
ReceiptJson: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T172702Z.json

OverallVerdict: DISAGREE
Accuracy: 99
Completeness: 99
PASS: 16
FAIL: 1
UNKNOWN: 0

Do not mark PLAN-TXNKEYSERVER-001 done. Hostile AGREE is required before done. This review is DISAGREE.

## Classification

Class 1 project implementation. Surface C applies. Byrd phase-order is not scored from FR-vs-file timestamps. Implementer does not claim the TODO is done.

## FAIL list

C-TEST161-not-retargeted: TEST-MCP-161 remains mapped to FR-MCP-120 and its store Condition still requires federation apply/control-plane gating, memory add/update/delete rollback, TODO CRUD rollback, repo/template/requirements/session/tool registry compensation, and GraphRAG/GitHub/context/voice/agent-pool fail-closed gates. FR-MCP-120 / FR-MCP-173 now require those same first-party mutations (including QBAgent session-log) to persist without coordinator/keyserver. Product TransactionGatedSessionLogService.ExecuteMutationAsync now bypasses on operation name for every source type, including QBAgent. TEST-MCP-161 was not retargeted to brain-slot.invoke / brain-slot.weight-update remaining scope. One structured AC on TEST-MCP-161 is still isSatisfied=true with generic "transaction gating and fail-closed" text.

## UNKNOWN list

none.

## Surface A (requested claims)

### A1. PASS. Operator rejected session-log-only carve-out. Store FR-MCP-173, FR-MCP-120 (structured AC), TR-MCP-TXNKEY-001, TEST-MCP-221 were amended to all non-QuadBrain first-party mutations. Markdown regenerated via requirements_generate doc=all.

Evidence:
- REST GET `/mcpserver/requirements/fr/FR-MCP-173`: title "Keyserver signs QuadBrain transactions only", notes "Amended 2026-09-17: operator rejected session-log-only carve-out. Scope is every non-QuadBrain first-party gated adapter, not session-log only.", 5 structured ACs (ac-fr173-001..005, all isSatisfied=false). Body lists TODO, requirements, session-log including QBAgent, memory, repo, tools, GitHub, GraphRAG, voice, agent pool, REPL TODO workflow, ingest, context, federation apply/control.
- REST GET `/mcpserver/requirements/fr/FR-MCP-120`: notes "Amended 2026-09-17 for FR-MCP-173: coordinator/keyserver applies to QuadBrain brain-slot transactions only. Operator rejected session-log-only carve-out." ac-fr120-001..006 structured; ac-fr120-001 and ac-fr120-004 include session-log for every source type including QBAgent under FR-MCP-173 persist-without-coordinator.
- REST GET `/mcpserver/requirements/tr/TR-MCP-TXNKEY-001`: title now "Keyserver gate is QuadBrain/brain-slot only" (was session-log/QBAgent-only in the 14:33 HV). notes "Amended 2026-09-17: all non-QuadBrain first-party gated adapters, not session-log/QBAgent-only." 3 structured ACs ac-trtxnkey-001..003 isSatisfied=false.
- REST GET `/mcpserver/requirements/test/TEST-MCP-221`: title "Non-QuadBrain gated adapters skip coordinator and keyserver", notes "Amended 2026-09-17: all non-QuadBrain adapters, not session-log only." Condition length 914 names the expanded operation list and TransactionGated* / TransactionalTodoWorkflowTests / coordinator SignManifest bypass.
- Mapping GET FR-MCP-173 -> TR-MCP-TXNKEY-001 + TEST-MCP-221. FR-MCP-120 still TR-MCP-TXN-001 + TEST-MCP-161,TEST-MCP-168.
- context_search product-requirements FR-MCP-173 and FR-MCP-120 match store bodies.
- Markdown LastWriteTimeUtc 2026-09-17T17:24:41Z for Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, TR-per-FR-Mapping.md, and Requirements-Matrix.md (same second: generate doc=all).

### A2. PASS. TurnTransactionKeyserverScope.ShouldBypassCoordinator is used by the named adapters/controllers. Coordinator ExecuteAsync bypasses SignManifestAsync unless RequiresKeyserver.

Evidence:
- `TurnTransactionKeyserverScope.cs` RequiresKeyserver true only for `brain-slot:` party prefix or `brain-slot.` / `quadbrain.` operation prefixes. ShouldBypassCoordinator = coordinator is null OR !RequiresKeyserver.
- ShouldBypassCoordinator call sites: TransactionGatedTodoMutationService, TodoExecutionService, SessionLogService, RequirementsDocumentService, RequirementsAnalysisService, RepoFileService, MemoryService, PromptTemplateService, ToolRegistryService, ToolBucketService, GraphRagService, GitHubCliService, GitHubWorkspaceTokenStore, IssueTodoSyncService, VoiceConversationService, AgentPoolService, TurnTransactionFederationOperationApplyService, TransactionalTodoWorkflow, RequirementsController.ShouldDeferIngest, ContextController.ShouldDeferContextMutation, FederationController.ShouldDeferFederationControlMutation, FwhMcpTools/McpServerMcpTools.ShouldDeferContextMutation (partial class).
- TransactionGatedTriageTodoCreator has no direct call; it delegates to ITransactionGatedTodoMutationService (bypass inherited).
- RepairWorkspaceStampsAsync does not call ShouldBypassCoordinator; it fail-closes via ThrowIfUncompensatedRepairBlocked (matches TR-MCP-TXNKEY-001 last sentence).
- TurnTransactionCoordinator.ExecuteAsync lines 311-327: if !Mutating or !Enabled or !RequiredForMutations or !RequiresKeyserver, run mutation and return Status=bypassed without SignManifestAsync. SignManifestAsync is only after that gate (line 343).

### A3. PASS. HV re-ran the claimed focused filters. Counts match.

Evidence (HV, not implementer memory):
- Support.Mcp.Tests filter FullyQualifiedName~TransactionGated|TurnTransactionKeyserverScope|TurnTransactionCoordinatorTests|BrainSlotInvocationTransaction|RequirementsControllerTransactionGate|ContextControllerTransactionGate|TransactionGatedStdioRouting|FederationController: Failed 0, Passed 227, Skipped 0, EXIT=0, duration 4 m 55 s. Log: `docs/receipts/_hv-txnkey-all-adapters-20260917T172702Z-focused.txt` line 25. TRX: `docs/receipts/_hv-txnkey-all-adapters-20260917T172702Z-focused.trx`.
- FederationOperationApplyServiceTests: Failed 0, Passed 9, Skipped 0, EXIT=0. Log: `docs/receipts/_hv-txnkey-all-adapters-20260917T172702Z-fedapply.txt` line 25.
- TransactionalTodoWorkflowTests (tests/McpServer.Repl.IntegrationTests): Failed 0, Passed 10, Skipped 0, EXIT=0. Log: `docs/receipts/_hv-txnkey-all-adapters-20260917T172702Z-todowf.txt` line 12.

Residual (not FAIL): TEST-MCP-221 says "Unit tests SHALL prove" and names TransactionalTodoWorkflowTests, which lives in the IntegrationTests project with Trait Integration. The 10 tests exist and pass; placement is hygiene.

### A4. PASS. PLAN-TXNKEYSERVER-001 Done=false. Live Windows service was not redeployed (pid 138544). Live todo.update still hits coordinator on the 9/16 binary.

Evidence:
- MCP todo_get: Id=PLAN-TXNKEYSERVER-001 Done=false CompletedDate=null. Title still "Keyserver signs QuadBrain/QBAgent session-log only". Note still says "FR-MCP-173/FR-MCP-120 to be amended".
- Marker pid 138544. Win32_Process Name=McpServer.Support.Mcp.exe CreationDate 2026-09-16 17:28:11 local (22:28:11 UTC). Live exe C:\ProgramData\McpServer\McpServer.Support.Mcp.exe LastWriteTimeUtc 2026-09-16T22:28:06Z. Working-tree TurnTransactionKeyserverScope.cs LastWriteTimeUtc 2026-09-17T15:20:44Z.
- Live binary Unicode/UTF8 scan: ShouldBypassCoordinator=false, TurnTransactionKeyserverScope=false, IsQuadBrainSessionLogSource=false. brain-slot: present.
- Live C:\ProgramData\McpServer\appsettings.yaml TurnTransactions.Enabled=true RequiredForMutations=true LastWriteTimeUtc 2026-09-16T15:21:04Z. Repo src/McpServer.Support.Mcp/appsettings.yaml Enabled=false RequiredForMutations=true (unchanged 2026-09-10).
- Live log C:\ProgramData\McpServer\logs\mcp-20260917.log line 74020: coordinator did not commit todo.update for PLAN-TXNKEYSERVER-001 txn-bae6a4baebd14260ac835ef0958edb78 Subscriber commit failed. Independently confirms implementer observation on the live 9/16 binary.
- Residual: HV MCP todo_update of the same note at 2026-09-17T17:34:27 local returned success=true. That does not redeploy the binary; it shows live coordinator commit is not a 100 percent fail. The failure the implementer cited is in the live log.

### A5. PASS. RepairWorkspaceStampsAsync remains fail-closed. Full unit suite was not run; only the focused gated-adapter slice.

Evidence:
- TransactionGatedSessionLogService.RepairWorkspaceStampsAsync dryRun skips the gate; non-dryRun calls ThrowIfUncompensatedRepairBlocked which throws when coordinator.GetStatus().Enabled and RequiredForMutations, or when degraded. No ShouldBypassCoordinator on this path.
- Tests RepairWorkspaceStampsAsync_WhenTransactionsRequired_FailsClosedWithoutCoordinatorExecute and WhenCoordinatorDegraded_FailsClosedWithoutCoordinatorExecute are in the 227-run class.
- HV ran only the claimed focused filters. No `./build.ps1 Test` / full unit suite in this review. Implementer did not claim otherwise.

### A6. PASS. Implementer does not claim done, deploy, or live-service green.

Evidence: todo_get Done=false; live exe still 9/16; FR-MCP-173 structured ACs isSatisfied=false; TR-MCP-TXNKEY-001 ACs isSatisfied=false; markdown FR-MCP-173 checkboxes are `[ ]`.

## Surface B (workspace rules)

### B1. PASS. Honesty.

No done/green/deploy claim. Stale TODO title/description/note ("to be amended") after the store amendment is residual metadata lag, not a false done-state.

### B2. PASS. Receipts.

HV re-verified store, product, live process, and tests. Did not accept plan checkboxes.

### B3. PASS. MCP-only storage.

Requirements were read from MCP store (context_search + REST GET after TR/TEST context_search returned empty). TODO via todo_get. HV did not edit todo.yaml or session-log files.

### B4. PASS. PowerShell only. No Python in this review.

### B5. PASS (N/A). No deletes.

### B6. PASS. Byrd v4 phase-order not failed from post-hoc timestamps. This is a post-slice review of in-progress work, not a claimed phase-complete with missing inter-phase HV.

## Surface C (requirements)

### C1. PASS. FR-MCP-173, FR-MCP-120, TR-MCP-TXNKEY-001, TEST-MCP-221 exist, are amended to the all-adapter carve-out, and FR-MCP-173 maps to TR-MCP-TXNKEY-001 + TEST-MCP-221.

### C2. PASS. TEST-MCP-221 coverage exists in TurnTransactionKeyserverScopeTests, TransactionGated*Tests (Request stays null / persist on reject-or-degraded coordinator), BrainSlotInvocationTransactionTests (coordinator still used for invoke), TurnTransactionCoordinatorTests.ExecuteAsync_WhenEnabledForGeneralAgentMutation_BypassesKeyserver (DidNotReceive SignManifestAsync for todo.update).

### C3. FAIL. TEST-MCP-161 not retargeted. See FAIL list.

### C4. Residual, not FAIL. TR-MCP-TXN-001 body still says mutation paths SHALL use compensation-capable coordinator execution or fail closed, Status Complete, mapped from FR-MCP-120. Prior 14:33 HV treated this as residual. Conflict is larger now that FR-MCP-120 carves out those adapters, but TR-MCP-TXNKEY-001 is the specific new TR.

Other residuals (not FAIL): FR-MCP-173 structured ACs isSatisfied=false while FR-MCP-120 ACs isSatisfied=true for overlapping carve-out text; TransactionGatedSessionLogServiceTests class comments still say TEST-MCP-161 QBAgent goes through the coordinator while the facts in that file now Assert.Null(coordinator.Request) for QBAgent submits; TEST-MCP-221 Condition rather than structured AC array.

## Surface D (plan holistically)

### D1. PASS. PLAN-TXNKEYSERVER-001 is not claimed complete. Done=false. FR-173 ACs unsatisfied.

### D2. PASS. Remaining open work is honestly stated as live Windows-service deploy (Nuke), HV AGREE before done, and full unit suite. Those three are still open. HV AGREE is not obtained on this run.

Residual: remaining-work inventory does not mention TEST-MCP-161 retarget or TODO title/description refresh. Not scored as D FAIL because the named remaining gates are still actually open and were not hidden.

## Verdict

OverallVerdict=DISAGREE because C-TEST161-not-retargeted FAILs. Accuracy 99 Completeness 99. One FAIL blocks AGREE even with scores at 99. Do not change PLAN-TXNKEYSERVER-001 to done.

=== VERDICT JSON ===
{"OverallVerdict":"DISAGREE","Accuracy":99,"Completeness":99,"PassCount":16,"FailCount":1,"UnknownCount":0,"FailList":["C-TEST161-not-retargeted: TEST-MCP-161 still mapped to FR-MCP-120 and still requires adapter compensation/fail-closed for memory/TODO/session/repo/tools/GraphRAG/GitHub/context/voice/agent-pool while FR-MCP-173/FR-MCP-120/product now persist those paths without coordinator, including QBAgent session-log."],"UnknownList":[],"WorkClass":1,"AddProfileFileCount":19,"HvSessionId":"GrokSubagentHostile-20260917T172702Z-txnkey-all-adapters","HvRequestId":"req-20260917T172702Z-001-hostile-validate-txnkey-adapters","HvTurnId":45218}
