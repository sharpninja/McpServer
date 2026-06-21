# Phase F: Live QuadBrain Loop (Byrd Development Process)

## Context

Phases A-E delivered the QBAgent tool/skill surface and wired it into the OpenAI-compatible
QuadBrain endpoint. The live 4-brain pipeline is fully implemented and unit-tested:

- `QuadBrainOrchestrationService` (real four-role decision loop: LeftHemisphere, RightHemisphere,
  CuriosityEngine, ArbiterOfTruth).
- `BrainSlotInvocationService` (gated, transaction-wrapped, credential-resolved per-brain invocation).
- `BrainSlotRegistryService` (durable slot registry; auto-registers active party signing keys on enable).
- `BrainSlotChatClientFactory` / `BrainSlotChatClient` (real OpenAI / OpenAI-compatible LLM calls).
- `BrainSlotCredentialResolver` (env:/config:/file: secret references).

Two real gaps remain for a genuinely live loop:

1. No startup provisioning. `appsettings.yaml` has no `Mcp:BrainSlots` section and no seeder reads
   the `config/brain-slots/*.yaml` artifact. A fresh server cannot run the quad without manual
   HTTP upserts.
2. No test exercises the real `QuadBrainOrchestrationService`. Every existing test substitutes
   `IQuadBrainOrchestrationService` wholesale, so the four-role loop is never under test end to end.

## Scope (confirmed)

- **F1**: Config-driven brain-slot startup seeder (production capability).
- **F2**: Real-loop proof at the service layer (F2a) and through the OpenAI-compatible endpoint (F2b).

## Seam strategy

Drop the test double one level below the orchestration: keep the real
`QuadBrainOrchestrationService` + `BrainSlotInvocationService` + `BrainSlotRegistryService` +
`InMemoryKeyServerService`; substitute only:

- `IBrainSlotChatClientFactory` (the per-brain LLM calls), and
- the turn-transaction coordinator with a committing, non-degraded `FakeTurnTransactionCoordinator`.

The transaction-commit machinery is independently covered by the ACID integration suite, so faking
the coordinator here keeps the test deterministic while still proving the real four-role loop runs.

## Requirements

- `FR-MCP-QBSEED-001`: The server provisions the Quad-Brain (all four roles) from configuration at
  startup when brain-slot execution is enabled, without manual API calls.
- `TR-MCP-QBSEED-002`: Startup provisioning is idempotent, runs through `IBrainSlotRegistryService`,
  is gated on `Mcp:BrainSlots:ExecutionEnabled`, references credentials only by safe reference
  (env:/config:/file:), and never aborts host startup on a single invalid slot definition.
- `TEST-MCP-QBSEED-001`: Unit tests for `BrainSlotStartupSeeder`.
- `TEST-MCP-QBLIVE-001`: Service-composition test of the real four-role orchestration loop.
- `TEST-MCP-QBLIVEINT-001`: HTTP integration test driving the real orchestration through
  `POST /v1/chat/completions`.

## TDD test plan (written first, must be red before implementation)

### F1 - `BrainSlotStartupSeederTests` (TEST-MCP-QBSEED-001)

1. `StartAsync_WhenExecutionEnabledAndFourSlotsConfigured_SeedsQuadReady`: after `StartAsync`,
   `GetStatusAsync().QuadReady` is true and all four roles are enabled.
2. `StartAsync_RunTwice_IsIdempotent`: two runs leave exactly four enabled slots, no exception.
3. `StartAsync_WhenExecutionDisabled_SeedsNothing`.
4. `StartAsync_WhenNoSlotsConfigured_SeedsNothing`.
5. `StartAsync_WhenOneSlotInvalid_SeedsRemainingAndDoesNotThrow`.

### F2a - `QuadBrainLiveOrchestrationTests` (TEST-MCP-QBLIVE-001)

1. `ExecuteFullOrchestrationAsync_WithRealServicesAndFakeBrains_CommitsArbiterDecision`: four real
   invocations occur in order Left -> Right -> Curiosity -> Arbiter; the fake chat client is called
   four times; the response is committed and `Output` equals the Arbiter output.
2. `ExecuteFullOrchestrationAsync_WhenArbiterEmitsToolCalls_ReturnsToolCallJsonAsOutput`.
3. `ExecuteFullOrchestrationAsync_WhenOnlyThreeRolesSeeded_RejectsQuadNotReady`.
4. `ExecuteFullOrchestrationAsync_WhenExecutionDisabled_RejectsWithoutCallingBrains`.

### F2b - `QuadBrainLiveEndpointIntegrationTests` (TEST-MCP-QBLIVEINT-001)

1. `ChatCompletions_RealOrchestration_ReturnsArbiterContent`: with four seeded slots, execution
   enabled, fake chat factory + committing coordinator, `POST /v1/chat/completions` returns the
   Arbiter decision as the assistant message (finish_reason `stop`).
2. `ChatCompletions_RealOrchestration_ArbiterToolCall_ReturnedAsToolCall`: Arbiter emits a
   `tool_calls` payload that surfaces as an OpenAI tool call (finish_reason `tool_calls`).
3. `ChatCompletions_RealOrchestration_QuadNotReady_ReturnsServerErrorOrEmpty`: no slots seeded ->
   the endpoint reports the rejection rather than a committed decision.

## Implementation (only after tests are red)

- `BrainSlotOptions`: add `SeedOnStartup` flag and `Slots` (`List<BrainSlotSeedDefinition>`),
  where each definition carries a `SlotId` plus the `UpsertBrainSlotRequest` fields.
- `BrainSlotStartupSeeder : IHostedService`: gated, idempotent, per-slot try/catch, summary logging.
- `Program.cs`: `AddHostedService<BrainSlotStartupSeeder>()` (every environment; internally gated).
- `appsettings.yaml`: add a disabled-by-default `Mcp:BrainSlots` section with an empty `Slots`
  list and a comment pointing at `config/brain-slots/quad-brain-slot-assignments.yaml`.

## Exit criteria (Byrd gate)

- All new tests green, plus the entire existing unit + integration suite green.
- `./build.ps1 ValidateTraceability` passes (new FR/TR/TEST IDs mapped and referenced).
- No guarded-file mutation during integration runs.
