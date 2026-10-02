# Handback: Overlay implementer resume after QuadBrain/QBAgent interruption

Prepared by GrokCode (Grok Build TUI, grok-4.6) on 2026-09-10 UTC.
Workspace for overlay work: `F:\GitHub\McpServer`.
This file is a continuity handback to the overlay/completion-program implementer that was interrupted. It is not a Handoff ingest source.

## 0. How to use this file

Read it. Resume overlay work from Section 8.

Do **not** ingest this file through `workflow.handoff.ingest`, `handoff_ingest`, DraftOnly, RequireReview, or CreateWhenConfident. Ingest would mutate TODO state. The HANDOFF skill is for TODO extraction from a bounded sample such as `docs/handoffs/example.md`, not for this continuity record.

Do **not** copy live API keys, marker HMAC values, or ProgramData secrets into receipts, commits, or further handoffs. Re-read `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml` after every service restart. Keys rotated during QuadBrain `Update-McpService -SkipVersionBump` publishes.

## 1. Who you are

You are the overlay implementer for `PLAN-PLUGINHANDOFF-001`.

- Code: Grok `grok-5.6` xhigh, identity `GrokCode`, plugin `mcpserver-grok-plugin`.
- Hostile gates: separate Codex CLI process, `gpt-5.6-sol` extra-high (`xhigh`), launched only from PowerShell.Mcp `execute_command`. Not the Grok hostile-validator default. Not Astra, except the in-product G5 HostileReviewWorkerOptions policy.
- Grok TUI session: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a08640-ef93-7340-92b9-45f2cf9234ad`
- MCP session: `GrokCode-20260909T130459Z-plugin-session` (workspace `F:\GitHub\McpServer`)
- Open overlay turn at interruption: `req-20260910T094800Z-041-lift-hygiene-hold-close` (in_progress). Recover it; do not open a duplicate session for the same work.
- Goal scratch: `C:\Users\kingd\AppData\Local\Temp\grok-goal-aea91f6548f9\implementer`
- Last overlay assistant line before interruption: Codex blocked store-close on hygiene gaps. Overlay was fixing triage status sourcing, batched reads, and parity tests on the existing console, then planned a rereview. Hygiene and the umbrella stay open until that AGREE.

The interrupting agent is this QuadBrain/QBAgent session:

- Grok TUI: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServerManager\01a08c4b-9d37-7633-a249-c5f2366b8061`
- MCP session: `GrokCode-20260910T185134Z-plugin-session` (workspace `F:\GitHub\McpServerManager`)
- Earlier QuadBrain config session: `GrokCode-20260910T171054Z-quadbrain-config`

Overlay previously wrote a one-way brief to QuadBrain at `C:\Users\kingd\AppData\Local\Temp\grok-goal-aea91f6548f9\implementer\quadbrain-handoff-prompt.md`. This document is the return trip.

## 2. Authorization

Operator Payton previously said `AGREE` with the meaning: finish the work, lift the hygiene keep-open hold, then store-close hygiene and the umbrella after Codex extra-high AGREE.

That administrative lift still stands. Codex extra-high at `2026-09-10T17:31:00Z` returned **DISAGREE. Do not store-close either TODO.** Overlay remediations for that DISAGREE are on disk but were not rebuilt or rereviewed because QuadBrain compile errors blocked `McpServer.Support.Mcp.Tests`.

You are authorized to resume overlay close work. You are not authorized to:

- Store-close `MCP-WORKSPACEHYGIENE-002` or `PLAN-PLUGINHANDOFF-001` without a fresh Codex extra-high AGREE on the close.
- Reopen or close `MCP-PLUGININT-001` unless a new Codex extra-high review of the hygiene close explicitly requires a store change, and then only after stating that requirement to the operator if it conflicts with prior overlay close shape (Done=true, P20 false, staging gated).
- Mark `PLAN-LLMSTRATEGY-001` done. Richer-than-CompleteAsync strategy remains remaining.
- Ingest this handback as a TODO.
- Revert QuadBrain CLI persistence, Grok isolation, Codex resume argv, QBAgent timeout, or live ProgramData BrainSlots unless the operator asks.
- Run staging/production `UpdateService`. Development `C11` is overlay-owned. QuadBrain already ran `gsudo pwsh -File F:\GitHub\McpServer\scripts\Update-McpService.ps1 -SkipVersionBump` several times. Do not run another publish unless the operator asks or a proven overlay C11 gap requires a development UpdateService with explicit approval.

## 3. Mandatory instruction recovery

Read these in full. This handback does not replace them.

- `F:\GitHub\McpServer\AGENTS.md`
- `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml` (re-read now; pid and key changed)
- `F:\GitHub\McpServer\.github\copilot-instructions.md`
- `F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`
- Overlay goal plan: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a08640-ef93-7340-92b9-45f2cf9234ad\goal\plan.md`
- Completion draft: `F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001-completion-20260906.md` (SHA-256 `5241F3B97B0BEE1453880FECFA622EFEEEAF425361C4F82D0E07CFE1629229CC`)
- Catalog: `F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md` (SHA-256 `7D83528F9097AFBB894AB76545F06B2D702D0A16BD2F5D7FB3FBA8BE65E4698E`)
- Prior pause handoff: `F:\GitHub\McpServer\docs\handoffs\grok-completion-program-20260909.md`
- After any compaction: complete `add-profile` before the next user request.

YAML mutations still require deserialize, mutate, serialize through `plugins/core/lib-ps/yaml-object-mutation.ps1`. Never edit YAML as text.

MCP is the only TODO/session/requirements interface. Never edit `todo.yaml` or session-log files on disk.

No em-dashes. `pwsh.exe` only. No Python for JSON/YAML construction.

## 4. Where overlay stopped

Last overlay compaction: session `01a08640` segment 008 at `2026-09-10T17:42:48Z`.

Codex extra-high hygiene-hold-close verdict (do not store-close):

- Path: `C:\Users\kingd\AppData\Local\Temp\grok-goal-aea91f6548f9\implementer\hygiene-hold-close-verdict.md`
- Stamp: `2026-09-10T17:31:00Z`
- Thread cited in overlay notes: `01a08c43-81be-7cc2-ae41-52b5b245eb5e`
- Overall: DISAGREE

PASS items from that verdict (still true unless you re-verify otherwise):

- Operator AGREE persisted as `req-20260910T094800Z-041-lift-hygiene-hold-close` is sufficient to lift the administrative hold.
- Pre-remediation G5 TRX `g5-closeout-overlay.trx` SHA-256 `E66629F2A59A413401F72A75B2B59DB72AEE12A0A04E209AEED4B306E4F7CBCD`: Total 24, Executed 24, Passed 24, Failed 0, Skipped/NotExecuted 0.
- Eligible children remain store-closed with G8 SHA-256 `B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6` in doneSummary.
- Hygiene and umbrella were still Done=false at the review snapshot.

BLOCKING items from that verdict, and current disk vs that review:

1. **Triage domain and batched reads.** Review said `WorkspaceValidationService` copied triage statuses into a private HashSet and used whole-table `ToListAsync`. Current disk (unverified by a new G5 TRX):
   - `src/McpServer.Services/Services/TriageService.cs`: public `IsFailedStatus` / `IsNonTerminalStatus` from live triage constants.
   - `src/McpServer.Services/Services/WorkspaceValidationService.cs`: `QueryBatchSize = 256`, `LoadBatchesAsync`, triage via those helpers. Dirty as untracked `??`.
2. **G5 parity.** Review said counts/rule-codes only and a test-only `ServiceBackedWorkspaceValidationWorkflow`. Current disk:
   - `tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs` `AssertParity` compares threshold plus `(RuleCode, RecordId, Severity)`.
   - REPL path is `WorkspaceValidationWorkflow` + `ServiceForwardingValidationHandler`.
   - Source-assert `ValidationService_UsesLiveTriageDomainAndBatchedQueries`.
   - `ServiceBackedWorkspaceValidationWorkflow` is gone from that file.
3. **Development C11.** Review said live plugin `method_not_found` for `workflow.workspace.validate` and authenticated REST hygiene HTTP 404 on a 2026-09-08 process. That probe is stale. Service was republished. Re-probe against current pid after re-reading the marker. Do not treat the old 404 as current evidence. Overlay Non-goal remains staging/production UpdateService, not development C11.
4. **PLUGININT P20.** Live `MCP-PLUGININT-001` is still Done=true with P20 false. Hygiene rule `todo_done_incomplete_tasks` will flag that as Error. Do not silently reopen or close PLUGININT. Decide on rereview with Codex; prior overlay close shape treated P20 staging as Non-goal and P1-P19 plus development C11 as in-scope.

Compile blocker that stopped the G5 rebuild:

- First: CS0051 public `BrainSlotChatClientFactory` ctor took `ILogger<CliBrainSlotChatClient>?` while `CliBrainSlotChatClient` is `internal`.
- Then: CS1503 `NullLogger` vs `ILogger<BrainSlotChatClientFactory>?` in `CliBrainSlotStrategyTests`.
- Overlay handed that compile/DI slice to QuadBrain via `quadbrain-handoff-prompt.md` and was interrupted from overlay close.

## 5. What QuadBrain changed (do not revert)

QuadBrain work was operator-directed: bind persistent Grok/Codex CLI strategies to live BrainSlots, then make QBAgent usable. PLAN-LLMSTRATEGY-001 remains Done=false (strategy still poorer than the remaining richer-than-CompleteAsync requirement).

### Live Windows service (verified 2026-09-10 after last SkipVersionBump)

- Service name `McpServer`, LocalSystem, `C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147`
- pid **90560**
- `serverStartedAtUtc` `2026-09-10T19:28:46.1772029+00:00`
- Health: Healthy, version `1.0.0+08eaf2a506a0aa2db89766e6a547d9ae1c85f681`, nonce echo verified
- HEAD still `08eaf2a506a0aa2db89766e6a547d9ae1c85f681` on `develop` (dirty tree; publish used SkipVersionBump)

Prior pids this QuadBrain session (all dead): 15980, then 38768, then 90560. Re-read the marker after any future restart.

Live ProgramData (not source `appsettings`):

- `Mcp:BrainSlots:ExecutionEnabled` true
- `Mcp:TurnTransactions` Enabled and RequiredForMutations true (needed for QuadBrain; currently breaks some `sessionlog_begin_turn` via keyserver/subscriber)
- Four slots: ArbiterOfTruth and Creativity = grok-4.6 `cli://grok-cli`; CuriosityEngine and Logic = gpt-5.6-sol `cli://codex-cli`
- TimeoutSeconds 180; CliRunAs `kingd`; configured CliWorkingDirectory historically `F:\GitHub\McpServer` but Grok QuadBrain cwd is isolated (below)
- Source repo `appsettings` keeps BrainSlots ExecutionEnabled **false**. Do not flip source to match live unless the operator asks.

CLI persistence is in-process `CliBrainSlotSessionStore`. It clears on service restart.

Prompt/temp dir: `C:\ProgramData\McpServer\Temp\quadbrain-cli` with BuiltinUsers Modify; TEMP/TMP pointed there. Grok work cwd: `C:\ProgramData\McpServer\Temp\quadbrain-cli\grok-work`. Env strips `GROK_PLUGIN_ROOT` and sets `GROK_AGENT_DASHBOARD=0`.

Grok persistent QuadBrain argv (not Agent Help one-shot): `--prompt-file`, `--session-id`/`--resume`, `--effort`/`--reasoning-effort` xhigh, `--always-approve`, `--output-format plain`, `--no-plan --no-subagents --verbatim --max-turns 1`. Agent Help one-shot still uses `--permission-mode plan`.

Codex resume argv must be `codex exec --json ... -o FILE resume SESSION_ID -` (exec OPTIONS before the `resume` subcommand). Stdin `-` is required because the 39KB tool payload exceeds Windows argv. Sandbox `read-only`.

Live proofs on pid 90560 (do not treat as overlay G5/C11 proof):

- Raw POST `/v1/chat/completions` model `quadbrain` without tools: HTTP 200, content PONG
- Rebuilt QBAgent text PONG: stdout `qbagent> PONG`, ~81s
- QBAgent tool loop `list_files` on `docs`: returned that `docs` contains directory `Operations`, ~137s, exit 0

### Compile/DI unblock (your CS0051/CS1503)

Current disk (re-read; this is what QuadBrain left):

`src/McpServer.Support.Mcp/Services/BrainSlotChatClientFactory.cs`

- Field: `ILogger<BrainSlotChatClientFactory> _cliLogger`
- Public ctor arg 5: `ILogger<BrainSlotChatClientFactory>? cliLogger`
- Fallback: `NullLogger<BrainSlotChatClientFactory>.Instance`
- `Create` constructs `new CliBrainSlotChatClient(..., _cliLogger)` (`ILogger<T>` implements `ILogger`)

`src/McpServer.Support.Mcp/Services/CliBrainSlotChatClient.cs`

- `internal sealed class CliBrainSlotChatClient(..., ILogger logger)`

`tests/McpServer.Support.Mcp.Tests/Services/CliBrainSlotStrategyTests.cs`

- Factory create uses `NullLogger<BrainSlotChatClientFactory>.Instance`
- Direct `CliBrainSlotChatClient` constructions still use non-generic `NullLogger.Instance` (valid: ctor takes `ILogger`)

Do **not** switch the public factory ctor back to `ILogger<CliBrainSlotChatClient>`. That is the CS0051 leak.

QuadBrain unit receipts from this session (not a substitute for your G5 rebuild): CliBrainSlotStrategyTests Passed (22 then 25 after resume/cwd tests); QuadBrainLiveOrchestrationTests 6 passed; QBAgentChatClientFactoryTests 7 passed. Overlay must rebuild `WorkspaceHygieneG5OverlayTests` itself on Baltic.

### QuadBrain-owned dirty paths (leave them)

Treat these as QuadBrain-owned unless a proven overlay compile/test failure requires a minimal surgical fix, and then do not change CLI resume/cwd/tool-loop behavior:

- `src/McpServer.Support.Mcp/Services/CliBrainSlotChatClient.cs` (untracked)
- `src/McpServer.Support.Mcp/Services/CliBrainSlotEndpoint.cs` (untracked)
- `src/McpServer.Support.Mcp/Services/CliBrainSlotSessionStore.cs` (untracked)
- `src/McpServer.Support.Mcp/Services/BrainSlotChatClientFactory.cs`
- `src/McpServer.Support.Mcp/Services/BrainSlotContracts.cs`
- `src/McpServer.Support.Mcp/Services/BrainSlotCredentialResolver.cs`
- `src/McpServer.Support.Mcp/Services/BrainSlotRegistryService.cs`
- `src/McpServer.Support.Mcp/Services/BrainSlotValidation.cs`
- `src/McpServer.Support.Mcp/Services/QuadBrainOpenAiChatService.cs`
- `src/McpServer.Support.Mcp/Services/QuadBrainOrchestrationService.cs`
- `src/McpServer.Services/Services/CodexCliAgentExecutionStrategy.cs`
- `src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs`
- `src/McpServer.QBAgent/QBAgentChatClientFactory.cs`
- `tests/McpServer.QBAgent.Tests/QBAgentChatClientFactoryTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/CliBrainSlotStrategyTests.cs` (untracked)
- `tests/McpServer.Support.Mcp.Tests/Services/QuadBrainLiveOrchestrationTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs`
- `tests/McpServer.Support.Mcp.IntegrationTests/Controllers/QuadBrainLiveEndpointIntegrationTests.cs`
- `config/brain-slots/quad-brain-slot-assignments.yaml`
- `docs/QUADBRAIN.md`

Isolated DbContext per `InvokeRoleAsync` via `IServiceScopeFactory` is required. Do not put parallel role invokes back on one DbContext.

QBAgent `QBAgentChatClientFactory.MinimumNetworkTimeout` is 10 minutes. Do not drop it back to the SDK default (~100s). Empty orchestration now returns `QuadBrain returned no decision ({reason})` instead of an empty 200.

## 6. Mixed dirty develop tree

HEAD: `08eaf2a506a0aa2db89766e6a547d9ae1c85f681` on `develop`.
Porcelain at handback write: **3908** paths (`git status --porcelain=v1 --untracked-files=all`). This is mixed overlay + QuadBrain + older receipts. Not a clean checkout. Do not reset, stash-drop, or restore overlay/QuadBrain files to match HEAD.

Overlay-owned hygiene close files (finish these; do not revert):

- `src/McpServer.Services/Services/TriageService.cs` (modified)
- `src/McpServer.Services/Services/WorkspaceValidationService.cs` (untracked)
- `tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs` (untracked)
- Related overlay tests still untracked: `DocsSyncG8OverlayTests.cs`, `HandoffD3D5OverlayTests.cs`, `HostileReviewG4OverlayTests.cs`, `WikiDumpG6OverlayTests.cs`, `WikiDumpG7OverlayTests.cs`

The tree also contains overlay G1 sixteenth sources, ProcessRunner job containment, wiki dump/import, hostile-review, plugin integration, docs/wiki projections, and a large receipts tree. Those were overlay work before this interruption. Do not treat porcelain count as QuadBrain ownership.

Non-goals from overlay plan remain: FILETOOLS, Octopus, TR-AUDIT, avalonia-remote, `PLAN-REDDITFEATURES-001`. QuadBrain/QBCODE was an overlay Non-goal that the operator later explicitly assigned to the interrupting agent. Leave the resulting files. Do not expand QuadBrain further unless asked.

## 7. Live MCP TODO snapshot (re-query before mutating)

Verified via `mcpserver__todo_get` on `F:\GitHub\McpServer` at handback time:

| Id | Done | Note |
|---|---|---|
| PLAN-PLUGINHANDOFF-001 | false | Umbrella. Keep open until Codex extra-high AGREE on hygiene close. |
| MCP-WORKSPACEHYGIENE-002 | false | Keep open until that same AGREE. Tasks still 0/10 Done in store even though G5 code exists. |
| MCP-PLUGININT-001 | true | P1-P19 true, P20 false. Do not reopen/close unless rereview requires it. |
| MCP-HANDOFF-001 | true | G3 D3-D5 + G8 SHA in doneSummary |
| MCP-HANDOFFPLAN-001 | true | same |
| MCP-HANDOFFREVIEW-001 | true | same |
| MCP-HOSTILEREVIEW-001 | true | G4 + G8 SHA |
| MCP-WIKIEXPORT-001 | true | G6/G7/G8 SHA |
| BUG-TRIAGE-139 | false | Sixteenth tasks 1-7 still false in store. C10 fail-closed. Do not check G1 complete. |
| PLAN-WARNREMEDIATION-001 | false | W18 still false |
| PLAN-LLMSTRATEGY-001 | false | Do not store-close. QuadBrain CLI hop shipped; richer strategy remaining. |

Native `mcpserver__todo_update` cannot set doneSummary. Overlay used GET-then-PUT `/mcpserver/todo/{id}` for child close. Keep that if you close hygiene later.

## 8. Immediate resume steps (ordered)

1. Re-read `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml`. Verify marker HMAC and `/health?nonce=`. Stop MCP usage on mismatch.
2. Recover overlay session `GrokCode-20260909T130459Z-plugin-session` and turn `req-20260910T094800Z-041-lift-hygiene-hold-close`. Query history first. Do not duplicate the session.
3. Reuse PowerShell.Mcp console **Baltic `#69712`** (still alive at handback: started 2026-09-10 11:59:09). Do not pass `reason` to `start_console`. If Busy, `wait_for_completion`. Do not open a new console for each command.
4. From `F:\GitHub\McpServer` on Baltic, rebuild G5 (QuadBrain CS0051/CS1503 should be gone):

```
dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj -c Debug --filter FullyQualifiedName~WorkspaceHygieneG5OverlayTests --nologo
```

Required: Failed 0, Skipped 0, and a new TRX. The old `g5-closeout-overlay.trx` (SHA `E66629F2...`) is pre-remediation and does not prove the DISAGREE fixes.

5. Re-probe development C11 against **current** pid 90560 (or whatever the marker pid is after you re-read it): plugin `workflow.workspace.validate` and authenticated REST hygiene. Old 404/method_not_found was a different process. Live TurnTransactions may affect sessionlog; hygiene REST is a different surface.
6. Codex extra-high rereview of hygiene-hold close from `execute_command` on Baltic (`codex exec --json`, `-c model_reasoning_effort=xhigh`, `--dangerously-bypass-approvals-and-sandbox`, `-o` verdict file, ISO-8601 UTC stamps). Prompt still forbids product/TODO/git writes by the reviewer.
7. Store-close hygiene then umbrella **only** on OverallVerdict AGREE, with hostile receipt paths in doneSummary, completedDate set, Remaining not `Do not store-close`, and implementation tasks truthful. If DISAGREE, remediate and rereview. Do not close on operator AGREE alone.

## 9. PowerShell consoles (operator standing order)

Overlay standing order: STOP OPENING NEW CONSOLES. Reuse Baltic `#69712`.

This QuadBrain session violated that order: it used Disco `#64132` (gone at handback) and Pop `#55448` (alive; this handback used Pop for git/status). Extra consoles exist on the machine. Do not close independently user-started consoles. Do not treat Pop as the overlay console. Resume overlay commands on Baltic `#69712`.

If Baltic is dead when you resume, tell the operator before allocating a replacement. Do not silently `start_console` with `reason`.

## 10. Service-publish side effects you inherit

QuadBrain ran official `Update-McpService.ps1 -SkipVersionBump` more than once. Consequences:

- Workspace API key rotated. Overlay's older HMAC `2AF568AF...` and nonce `g78-closeout-77cdb602` are not current trust proofs.
- Live binary is SkipVersionBump of dirty develop, version still `1.0.0+08eaf2a5`, pid 90560.
- Live ProgramData now has BrainSlots + TurnTransactions on. Source appsettings ExecutionEnabled stays false.
- In-process CLI session store was cleared at each restart.
- Overlay C11 evidence taken against an older process is invalid until re-probed.
- Do not copy live keys into `docs/receipts`, this file, or git.

If `/mcpserver/*` returns 401: re-read the marker, re-verify nonce, resume through the Grok plugin. Do not hand-roll REST for session/TODO/requirements except the documented GET-then-PUT doneSummary path overlay already used.

## 11. Receipts and paths

- Hygiene DISAGREE: `C:\Users\kingd\AppData\Local\Temp\grok-goal-aea91f6548f9\implementer\hygiene-hold-close-verdict.md`
- Pre-remediation G5 TRX: `...\implementer\g5-closeout-overlay.trx` SHA-256 `E66629F2A59A413401F72A75B2B59DB72AEE12A0A04E209AEED4B306E4F7CBCD`
- Overlay-to-QuadBrain brief: `...\implementer\quadbrain-handoff-prompt.md`
- G8 AGREE SHA-256: `B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6` path `g8-verdict.md` thread `01a08a51-2f37-7723-bc35-c31160e794b8`
- G1 sixteenth workspace TRX SHA-256: `66C76C2C11F363789328647BA4EA6B0B12D3FB588D041296E1F1AA8479C1F6D8` (22/22)
- W18 workspace TRX SHA-256: `BDFE39902ABE2D67F43754B1B0B696AA1446DF614A05F4F42FA47BC4BE1AF952`
- Overlay compaction: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a08640-ef93-7340-92b9-45f2cf9234ad\compaction\INDEX.md` (segment 008 is the interruption snapshot)
- This handback: `F:\GitHub\McpServer\docs\handoffs\handback-overlay-from-quadbrain-qbagent-20260910.md`

## 12. Constraints checklist (do not drop)

- Do not store-close hygiene or umbrella without fresh Codex extra-high AGREE.
- Do not ingest this file as a Handoff TODO.
- Do not reopen/close PLUGININT unless the new close review requires it and you record that decision.
- Do not close PLAN-LLMSTRATEGY-001.
- Do not revert QuadBrain CLI/QBAgent files listed in Section 5.
- Do not put `ILogger<CliBrainSlotChatClient>` back on the public factory ctor.
- Do not open extra PowerShell consoles. Baltic `#69712` only.
- Do not run staging/production UpdateService. Do not SkipVersionBump again unless operator-approved for overlay C11.
- Do not leak live API keys.
- C10 remains fail-closed until an operator-approved native Linux executor exists.
- Byrd tests-first still binds overlay slices. Current-plus-prior Failed 0 Skipped 0.

## 13. One-sentence resume

Rebuild G5 on Baltic `#69712` against the already-written hygiene remediations plus the QuadBrain logger fix, re-probe live C11 on pid 90560 after re-reading the marker, then Codex extra-high rereview the hygiene-hold close; store-close hygiene and the umbrella only on AGREE.
