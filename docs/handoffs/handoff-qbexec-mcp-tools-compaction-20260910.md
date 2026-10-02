# Continuity handoff: QuadBrain mcp_* executor and Grok compact knobs

Prepared by GrokCode (Grok Build TUI, grok-4.6) on 2026-09-10 UTC.
Implementation workspace: `F:\GitHub\McpServer`.
Operator TUI workspace for this thread: `F:\GitHub\McpServerManager`.

This file is a continuity handoff. It is not a Handoff ingest source.

## 0. How to use this file

Read it. Resume from Section 8.

Do not ingest this file through `workflow.handoff.ingest`, `handoff_ingest`, DraftOnly, RequireReview, or CreateWhenConfident. Ingest would mutate TODO state. The HANDOFF skill is for TODO extraction from a bounded sample such as `docs/handoffs/example.md`, not for this continuity record.

Do not copy live API keys, marker HMAC values, or ProgramData secrets into receipts, commits, or further handoffs. Re-read `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml` after every service restart.

Do not overclaim. If you did not measure it, say you did not measure it. If a wrapper line is unverified, label it unverified. Operator Payton named overclaiming as lying and said trust is broken because of it.

## 1. Who you are

You are the implementer finishing QuadBrain server-side execution of every catalog `mcp_*` tool (FR-MCP-QBEXEC-001/002, TR-MCP-QBEXEC-001/002, TEST-MCP-QBEXEC-001/002).

- Code: Grok `grok-4.6` xhigh, identity `GrokCode`, plugin `mcpserver-grok-plugin`.
- Grok TUI session: `C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServerManager\01a08c4b-9d37-7633-a249-c5f2366b8061`
- MCP session (Manager workspace): `GrokCode-20260910T215253Z-plugin-session`
- Earlier QuadBrain/QBAgent MCP session: `GrokCode-20260910T185134Z-plugin-session`
- Open TODO: `PLAN-LLMSTRATEGY-001` stays open. Do not mark it done.
- Operator: Payton.

Related continuity file you should not reopen as your primary job: `docs/handoffs/handback-overlay-from-quadbrain-qbagent-20260910.md` (overlay/hygiene; do not ingest that either).

## 2. Operator stance that is in force

These are operator instructions, not optional tone notes.

1. Overclaiming unverified facts as measured knowledge is lying. Repeating a continuation wrapper line ("ran out of context") as if you counted tokens is that failure.
2. Compaction before submission is theft. A submitted turn must run in the full context it was submitted in. Auto-compact that discards turns and then answers against a summary is not a 500k working window.
3. Official Grok 4.6 context window is 500,000 tokens (`https://docs.x.ai/developers/models/grok-4.6`). Grok Build default auto-compact is 85% of the window it thinks you have. Compaction discards old conversation turns (user-guide `13-memory.md`).
4. Operator reported the previous turn meter at 81% when compact fired. That was not independently re-measured by this agent. Documented default trip is 85%. `features.two_pass_compaction` default true is labeled "prefire two-pass compaction". Status line ambers at 80% when the agent reports no threshold (`25-status-line.md`).
5. When the operator is on this subject, do not run away into executor edits. They said STOP when the previous agent did that.
6. No em-dashes. MCP store is the only TODO/session/requirements interface. Never edit `todo.yaml` or session-log files on disk. Tests must not depend on the live Windows service. Byrd: tests first, mocks, then implementation. All tests green in the executed scope before claiming a gate.

## 3. Local Grok config already written

File: `C:\Users\kingd\.grok\config.toml`

Added (parsed with Python `tomllib` after the write; values below are from that parse):

```toml
[features]
two_pass_compaction = false

[session]
auto_compact_threshold_percent = 100
```

`models.default` remained `grok-4.6`. Plugin enabled count remained 8.

What that write does not do:

- It does not restore discarded turns in this TUI session.
- Reload of `config.toml` in an already-running Grok process was not verified.
- `100` is not documented as "never compact". It is "compact when usage reaches this percent".
- `two_pass_compaction = false` is not documented as "never compact before submit". It is the prefire flag. Call order was not traced.

A new Grok session is the honest test of those knobs. Do not tell the operator the product now uses the full 500k window unless you have `/session-info` or `/context` evidence.

## 4. What you are authorized to do

- Finish server-side `mcp_*` execution in `F:\GitHub\McpServer` via in-process CQRS services, not HTTP.
- Write mocked unit tests first (no Windows service). Exhaustive catalog theory over `QuadBrainMcpToolCatalog.All`.
- Keep internals off `RemainingToolCalls`. All-internal success is `finish_reason=stop` with service-result content.
- Compile and run the Support.Mcp unit tests locally.
- Do not mark FR/TEST ACs satisfied until tests prove catalog completeness.
- Do not store-close `PLAN-LLMSTRATEGY-001`.
- Do not run `Update-McpService` unless the operator asks. SQL `PAYTON-DESKTOP` has been flaky; live service pid/key rotate on restart. Re-read the marker.

You are not authorized to:

- Ingest this handoff as a TODO.
- Claim compact-before-submit is fixed.
- Claim the executor is done, green, or compiled unless you have command output.
- Forward unhandled `mcp_*` internals to QBAgent as OpenAI `tool_calls`.

## 5. Requirements already in the MCP store (McpServer workspace)

Live store updates were made in an earlier turn (not re-fetched in the turn that wrote this file). Treat the store as source of truth; re-query before marking ACs.

Named IDs:

- FR-MCP-QBEXEC-001, FR-MCP-QBEXEC-002
- TR-MCP-QBEXEC-001, TR-MCP-QBEXEC-002
- TEST-MCP-QBEXEC-001, TEST-MCP-QBEXEC-002

Completeness ACs (all catalog `mcp_*` names, CQRS not HTTP, stop plus result content, tests not Windows-service) were added and left unsatisfied. Do not flip them from memory.

## 6. Where the code actually is (file facts)

No `dotnet` compile was run in the turn that wrote this file. Claims below are from reading and grepping the tree.

### Catalog

`src/McpServer.Support.Mcp/Services/QuadBrainMcpToolCatalog.cs` exists. `All` lists session, todo (query/get/update/create/delete/plan/status/implementation), repo (read/list/write/edit), desktop, PowerShell session create/command/close, requirements list/get/create/update for FR/TR/TEST, client invoke, GraphRAG CRUD, and `mcp_git`.

### Executor

`src/McpServer.Support.Mcp/Services/QuadBrainInternalToolExecutor.cs`

- Constructor takes 10 dependencies: `ITransactionGatedTodoMutationService`, `ITodoService`, `ITodoPromptService`, `IRepoFileService`, `IRequirementsDocumentService`, `ISessionLogService`, `IGraphRagService`, `IDesktopLaunchService`, `IProcessRunner`, `WorkspaceContext`.
- `TryExecuteAsync` switch includes every name in `QuadBrainMcpToolCatalog.All` (by inspection of the switch arms). Unknown names return `Unhandled`.
- List/get requirements methods exist (`ListFrAsync`, `GetFrAsync`, and siblings) and are routed from the switch.
- Stale comment at about lines 606-609 still says list/get requirements are NOT handled and fall through to Unhandled. That comment contradicts the switch. Fix the comment; do not revert the routes.
- `GetString` and `GetBool` exist at the bottom of the file (file ends ~line 708).
- `OkJson`, `CollectLinesAsync`, and `GetInt` are called and were not found as method definitions in that file. Expect compile errors until those helpers exist.
- `mcp_client_invoke` only dispatches a small todo/repo subset; other client names fail with an instruction to use a named `mcp_*` tool. Confirm that against FR-MCP-QBEXEC ACs before calling it complete.
- `mcp_git` runs `IProcessRunner` with workspace cwd. `push` is special-cased to `git push origin`.

### Desktop DI

`IDesktopLaunchService` exists. `DesktopLaunchService` implements it. `Program.cs` and `McpStdioHost` register `IDesktopLaunchService` to `DesktopLaunchService`.

### Interceptor / OpenAI surface

`QuadBrainToolInterception.cs`: unhandled internals are Failed notes, not `RemainingToolCalls`. Interceptor tests assert `mcp_unknown_tool` is Failed and `do_local_thing` remains for the agent.

`QuadBrainOpenAiChatService.cs` has `BuildInternalSuccessContent` (prior work). Re-read before changing the stop-plus-content contract.

## 7. Tests that will not compile or will assert the wrong contract

`tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs`

- `CreateSut()` is still `new(_todo, _todoQueries, _repo, _requirements)` (4 args). Production constructor has 10 args.
- `Execute_McpRequirementsListFr_ReturnsUnhandled` still expects Unhandled. Production switch handles `mcp_requirements_list_fr`. That test is the old contract. Replace it. Do not "fix" production to match Unhandled.
- There is no `[Theory]` / `MemberData` over `QuadBrainMcpToolCatalog.All`. That theory is required by TEST-MCP-QBEXEC completeness ACs: every catalog name Handled via mocks; unknown names Unhandled.

Interceptor tests do not yet walk the full catalog. OpenAI chat service tests were partially updated in prior turns; re-read them. Do not assume they cover every tool.

Byrd order for the next slice:

1. Rewrite `CreateSut` to the 10-arg constructor with NSubstitute mocks (including `ITodoPromptService`, `ISessionLogService`, `IGraphRagService`, `IDesktopLaunchService`, `IProcessRunner`, `WorkspaceContext`).
2. Add catalog theory: each `QuadBrainMcpToolCatalog.All` name returns `Handled` with mocked success. Unknown `mcp_*` remains Unhandled.
3. Invert or delete `Execute_McpRequirementsListFr_ReturnsUnhandled`.
4. Add helpers `OkJson`, `CollectLinesAsync`, `GetInt` so the executor compiles.
5. Fix DTO/property mismatches if the compiler names them (session query fields, GraphRAG request types, `ProcessRunRequest`).
6. Run `McpServer.Support.Mcp.Tests` only. No live Windows service.
7. Only then consider interceptor/OpenAI surface tests for catalog names never becoming `RemainingToolCalls`.

## 8. Resume here

1. Re-read `F:\GitHub\McpServer\AGENTS-README-FIRST.yaml`, health nonce, then MCP session/TODO via plugin tools. Use the McpServer workspace key for McpServer requirements. Manager key 401s on McpServer store rows.
2. Query FR/TR/TEST-MCP-QBEXEC-* from the McpServer store. Do not edit requirements markdown by hand.
3. Start with the failing tests in Section 7. Do not add more production routes until `CreateSut` and the catalog theory exist.
4. Implement missing helpers and any compile breaks the compiler prints. Do not invent "fixed" from this handoff.
5. Keep `PLAN-LLMSTRATEGY-001` open.
6. If the operator challenges a claim, stay on that challenge. Do not pivot to the executor.

## 9. qbagent note (orthogonal)

qbagent 1.3.2 local commands (`new session`, `list open todo`, progress timestamps) already shipped in prior turns. That is UX. It is not a substitute for server-side `mcp_*` via CQRS.

## 10. What was not verified

- No `dotnet build` / `dotnet test` in the turn that wrote this file.
- No live QBAgent turn after the catalog expansion.
- No `/session-info` token counts for the 81% compact event.
- No proof that the new `config.toml` knobs changed Grok Build call order.
- MCP QBEXEC AC satisfaction in the live store was not re-queried in this turn.

If you did not produce a new receipt, do not say those items are done.
