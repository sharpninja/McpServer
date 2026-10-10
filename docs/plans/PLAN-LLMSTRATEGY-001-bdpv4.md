# BDPv4 plan: PLAN-LLMSTRATEGY-001 per-role LLM completion strategy

Workspace: `F:\GitHub\McpServer`
Requirements: FR-MCP-LLMSTRATEGY-001, TR-MCP-LLMSTRATEGY-001, TEST-MCP-LLMSTRATEGY-001
TODO: PLAN-LLMSTRATEGY-001 (leave `Done: false` until Codex READY, tests Failed 0/Skipped 0, and hostile AGREE)

## Reconciliation (Codex round 1)

Applied all path_to_90 items from 64 NOT-READY: BG-01 role prompt separate from context; BG-02 prepared-payload transport cores; BG-03 private AoT core + stage-boundary evidence; BG-04 real harness tests not a runner; BG-05 migrate four test factories + transaction substitute; BG-06 full Test + Build.Tests + integration compile; D-01 csproj snapshot compare.

## Outcome

Each QuadBrain role completes through a per-slot provider strategy. Orchestration owns one `BrainSlotTurnContext` class instance per full turn. Strategies receive `(slot, rolePrompt, context, temperature)`. HTTP flatten includes role prompt, original input, identifiers, and evidence key/value pairs. No new third-party agent SDK.

## Current seam (verified)

- `IBrainSlotChatClient.CompleteAsync(slot, string input, double? temperature, ct)`
- Factory `Create(slot, credential)` returns OpenAI / OpenAICompatible / Cli clients that each build their own payload from the string input
- Invocation calls `Create` then string `CompleteAsync` with `request.Input` (the role prompt, not only the user original)
- Full orchestration calls public `ExecuteAotReconciliationAsync` (would allocate a second context if that method always `new`s one)
- Concrete test factories implementing `IBrainSlotChatClientFactory`: `QuadBrainLiveOrchestrationTests.RecordingChatClientFactory`; `QuadBrainOllamaEndpointIntegrationTests.RecordingBrainSlotChatClientFactory` and `FieldVariantBrainSlotChatClientFactory`; `QuadBrainLiveEndpointIntegrationTests.RecordingChatClientFactory`. Transaction tests substitute `Create` only.

## Target seam

### BrainSlotTurnContext (class)

File: `src/McpServer.Support.Mcp/Services/BrainSlotTurnContext.cs`

Init properties: `OriginalInput` (required string), `SessionId`, `TurnId`, `TransactionId` (nullable strings; this `TransactionId` is the **upstream turn** id, not per-invocation `brain-slot-{guid}`).

Evidence: private `OrderedDictionary` or `SortedDictionary<string,string>` owned by orchestration. Expose `IReadOnlyDictionary<string,string> CommittedRoleEvidence`. Mutate only between awaited stages after commit checks. Canonical keys: `BrainSlotRoles.*`. Latest committed output wins. Retain committed AoT rejection text for voting. Concurrent Creativity/Logic must not mutate evidence until both awaits complete.

### IBrainSlotCompletionStrategy

```
Task<string> CompleteAsync(
    BrainSlotDefinitionEntity slot,
    string input,
    BrainSlotTurnContext context,
    double? temperature,
    CancellationToken cancellationToken = default);
```

`input` is `request.Input` unchanged (role prompt). Context is shared. Do not put the role prompt on the context object.

### Factory

`IBrainSlotChatClientFactory.Create` unchanged.

Add `IBrainSlotCompletionStrategy CreateStrategy(BrainSlotDefinitionEntity slot, string credential)`.

Implement `CreateStrategy` on production `BrainSlotChatClientFactory` and on all four concrete test factories listed above. NSubstitute setups in `BrainSlotInvocationTransactionTests` must stub `CreateStrategy` and assert it (not only `Create`) for no-provider-call rejection paths.

### Transport cores (BG-02)

Extract from existing clients without changing legacy string payloads when context is unused:

1. **OpenAI-compatible HTTP core:** `SendOpenAiCompatibleAsync(slot, credential, requestJson, ct)` used by legacy `CompleteAsync(slot, input, temp)` (JSON from `BuildOpenAiCompatibleRequestJson(slot, input, temp)`) and by `OpenAiCompatibleCompletionStrategy` (JSON from context-aware builder).
2. **OpenAI adapter core:** `SendOpenAiAsync(slot, IReadOnlyList<ChatMessage> messages, ChatOptions options, ct)` used by legacy message build and by `OpenAiCompletionStrategy` prepared messages.
3. **Cli:** strategy formats `input` plus context identifiers/evidence, then calls existing `CliBrainSlotChatClient.CompleteAsync(slot, formattedInput, temperature, ct)` so system prompt is added once. Preserve CLI session reuse.

Preserve credentials, endpoint resolution, temperature, max tokens, timeout, cancellation, response extraction.

### HTTP flatten contract

`BuildOpenAiCompatibleRequestJson(slot, input, context, temperature)` messages MUST contain:

1. optional slot system prompt
2. user content equal to the **role prompt** `input`
3. user content including `context.OriginalInput`
4. `sessionId=`, `turnId=`, `transactionId=` prefixes when those properties are non-null
5. every evidence pair as `evidence.{Role}={value}` in canonical role order (Creativity, Logic, CuriosityEngine, ArbiterOfTruth), omitting empty values

Legacy `BuildOpenAiCompatibleRequestJson(slot, input, temperature)` stays byte-compatible for no-context calls.

### Invocation

`BrainSlotInvokeRequest.TurnContext` is `[JsonIgnore]` in-process only.

When `TurnContext` is null, construct a fallback context: `OriginalInput = request.Input`, `TurnId = request.TurnId`, session/transaction from metadata keys `sessionId`/`transactionId`, empty evidence.

Then: `var strategy = _chatClientFactory.CreateStrategy(slot, credential); output = await strategy.CompleteAsync(slot, request.Input, turnContext, request.Temperature, token);`

Do not call string `IBrainSlotChatClient.CompleteAsync` from invocation after this change.

Pass `TurnContext` through both scoped (`IServiceScopeFactory`) and unscoped invocation paths in orchestration.

### Orchestration (BG-03)

`ExecuteFullOrchestrationAsync` allocates **one** context at start (`OriginalInput = request.Input`, `TurnId = request.TurnId`, session/transaction from metadata). Never allocate another for AoT during a full turn.

Split AoT: private `ExecuteAotReconciliationCoreAsync(AotReconciliationRequest, BrainSlotTurnContext, ct)`. Public `ExecuteAotReconciliationAsync` allocates and seeds evidence from supplied Creativity/Logic (and any other supplied role outputs), then calls the core. Full orchestration calls the core with the existing instance.

Evidence updates only after awaited stage commit: after Creativity+Logic both return; after Curiosity; after each AoT; after voting replacements. Canonical role keys. Latest committed output. Keep AoT rejection evidence for the voting pass.

## Tests (BG-04, BG-05, D-01)

Delete any `BrainSlotTurnRunner`.

New/extended tests in `BrainSlotLlmStrategyTests.cs` plus extensions to `QuadBrainLiveOrchestrationTests` / `BrainSlotInvocationTransactionTests`:

1. Real factory `CreateStrategy` for Creativity/OpenAICompatible vs Logic/OpenAI returns two different types.
2. Live orchestration harness with recording `CreateStrategy` implementations: same `BrainSlotTurnContext` instance through Creativity, Logic, and AoT (including scoped parallel Creativity/Logic).
3. Evidence snapshots after commit stages; voting replacement; Curiosity escalation; standalone AoT seeds evidence from supplied outputs.
4. Direct invocation null-context fallback builds a context from Input/TurnId/metadata.
5. Flatten JSON includes role prompt, original input, identifiers, and evidence values.
6. Transaction rejection paths assert `CreateStrategy` not called (migrate from `Create`).
7. Existing commit-failure behaviors still pass through the new seam.
8. New strategy-backed invocation tests: provider timeout (`OperationCanceledException` from the strategy while the caller token is not canceled) returns `ProviderFailed` and does not admit/commit GraphRAG; caller cancellation (`canceled` token) propagates `OperationCanceledException` and does not reach transaction commit or admission.

Implement `CreateStrategy` on the four concrete test factories; compile `McpServer.Support.Mcp.Tests` and `McpServer.Support.Mcp.IntegrationTests`.

Do not mock `BrainSlotChatClientFactory` or `BuildOpenAiCompatibleRequestJson` for flatten tests. Fake only provider I/O or recording strategy boundary.

D-01: tests compare `McpServer.Support.Mcp.csproj` **two separate lists** (not a mixed bag):

PackageReference.Include (current snapshot):
Microsoft.EntityFrameworkCore; Microsoft.AspNetCore.Authentication.JwtBearer; Microsoft.Extensions.AI.OpenAI; OpenAI; Microsoft.AspNetCore.Identity.EntityFrameworkCore; Duende.IdentityServer; Duende.IdentityServer.AspNetIdentity; Duende.IdentityServer.EntityFramework; Microsoft.Extensions.Hosting.WindowsServices; Microsoft.EntityFrameworkCore.Sqlite; Microsoft.EntityFrameworkCore.SqlServer; Npgsql.EntityFrameworkCore.PostgreSQL; Microsoft.EntityFrameworkCore.InMemory; Microsoft.EntityFrameworkCore.Design; Microsoft.EntityFrameworkCore.Analyzers; Microsoft.CodeAnalysis.Common; Microsoft.CodeAnalysis.CSharp; Microsoft.CodeAnalysis.CSharp.Workspaces; Microsoft.CodeAnalysis.Workspaces.Common; Microsoft.CodeAnalysis.Workspaces.MSBuild; Microsoft.Build.Framework; ModelContextProtocol; ModelContextProtocol.AspNetCore; Serilog.AspNetCore; Serilog.Sinks.Console; Serilog.Sinks.File; Serilog.Sinks.Http; Swashbuckle.AspNetCore; YamlDotNet; NetEscapades.Configuration.Yaml; Microsoft.ML.OnnxRuntime; HNSWIndex; Handlebars.Net; QRCoder

ProjectReference.Include (current snapshot):
..\McpServer.ServiceDefaults\McpServer.ServiceDefaults.csproj; ..\McpServer.TransactionSecurity\McpServer.TransactionSecurity.csproj; ..\McpServer.Common.AgentCli\McpServer.Common.AgentCli.csproj; ..\McpServer.Storage\McpServer.Storage.csproj; ..\McpServer.Storage.SqliteMigrations\McpServer.Storage.SqliteMigrations.csproj; ..\McpServer.Storage.PostgreSqlMigrations\McpServer.Storage.PostgreSqlMigrations.csproj; ..\McpServer.Storage.SqlServerMigrations\McpServer.Storage.SqlServerMigrations.csproj; ..\McpServer.Services\McpServer.Services.csproj; ..\McpServer.SessionLog.Transcripts\McpServer.SessionLog.Transcripts.csproj; ..\McpServer.GraphRag\McpServer.GraphRag.csproj

No new PackageReference names and no new ProjectReference names vs that snapshot. Reflection `CreateStrategy` is only an API-existence check.

Filter for intermediate gate: `FullyQualifiedName~BrainSlot`

## Gates

Intermediate: focused BrainSlot tests Failed 0 Skipped 0.

Before hostile + TODO done (BG-06):

- Focused filter Failed 0 Skipped 0: `FullyQualifiedName~BrainSlotLlmStrategyTests|FullyQualifiedName~BrainSlotInvocationTransactionTests|FullyQualifiedName~BrainSlotChatClientFactoryTests|FullyQualifiedName~QuadBrainLiveOrchestrationTests`
- `dotnet build tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug`

Do not require `./build.ps1 Test` or `FullyQualifiedName~BrainSlot` for this TODO. That substring includes `QuadBrainSlotConfigurationTests`, which currently fails Expected OpenAICompatible Actual Cli on `config/brain-slots/quad-brain-slot-assignments.yaml`. That yaml/runtimeCompatibility check is outside FR/TR/TEST-MCP-LLMSTRATEGY-001.

Any failure or skip in the focused filter blocks completion. Save TRX/console under scratch.

Then: Codex READY >= 98 on this plan (operator: 90 is not correctness); hostile AGREE FAIL 0; then `todo_update` done true with receipts in `doneSummary`.

## Non-goals

PLAN-SHARPMIND-001, compact-before-submit, Update-McpService, live QBAgent, new in-process engine, QBEXEC catalog.

## Byrd order

Requirements captured. This plan + Codex READY. Red tests. Implement. Green focused then full Test gates. Hostile AGREE. TODO done.
