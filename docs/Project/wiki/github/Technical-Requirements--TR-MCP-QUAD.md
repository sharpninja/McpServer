# TR-MCP-QUAD

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-QUAD-001

**Brain-slot storage, DTOs, CRUD, and validation** — Persist BrainSlotDefinition and BrainSlotInvocation rows per workspace; expose client DTOs, REST endpoints, and STDIO/MCP parity; validate known roles, credential-reference-only secrets, one enabled slot per workspace and role, replaceExisting replacement audit, soft delete, and readiness status.
**Covered by:** FR: FR-MCP-123, FR-MCP-129; TEST: TEST-MCP-163, TEST-MCP-170, TEST-MCP-171, TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Brain-slot definitions and invocations persist per workspace with role validation, one-enabled-slot enforcement, soft delete, credentialReference-only storage, and readiness projection. (evidence: BrainSlotRegistryServiceTests; BrainSlotDefinitionEntity; BrainSlotInvocationEntity)
- [x] REST, client, STDIO, and plugin DTOs round-trip slot CRUD without returning raw credential material. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)

## TR-MCP-QUAD-002

**External model provider adapter, credentials, endpoint allowlist, timeout, and redaction** — Resolve credentials from env:, config:, or file: references without persisting raw secrets; create OpenAI/OpenAI-compatible chat clients; enforce custom endpoint host allowlists, explicit loopback allowance, per-slot timeout and cancellation, and redacted audit/log output.
**Covered by:** FR: FR-MCP-129; TEST: TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Credential references resolve from env:, config:, and file: sources without persisting or logging raw secrets. (evidence: BrainSlotCredentialResolverTests)
- [x] OpenAI-compatible endpoints enforce host allowlists, explicit loopback allowance, timeout, cancellation, and redaction gates. (evidence: BrainSlotProviderTests and invocation tests)

## TR-MCP-QUAD-003

**Keyserver party mapping and transaction diffgram admission** — Require enabled trusted party/key mapping before invocation; invoke external models only when brain-slot execution and required turn transactions are enabled; commit brain-slot.invoke diffgrams before returning output.
**Covered by:** FR: FR-MCP-129, FR-MCP-130, FR-MCP-134; TEST: TEST-MCP-174, TEST-MCP-175, TEST-MCP-176, TEST-MCP-177, TEST-MCP-178, TEST-MCP-179, TEST-MCP-181, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Invocation rejects until execution, slot, endpoint, credential, party/key, and required transaction gates pass. (evidence: BrainSlotInvocationTransactionTests)
- [x] brain-slot.invoke diffgrams include slot, role, provider, model, prompt hash, output hash, admission target, and timestamps before output is returned. (evidence: BrainSlotInvocationTransactionTests)

## TR-MCP-QUAD-004

**Quad branch containment and authorization** — Provide explicit runtime gates proving AoT reconciliation execution, weight update execution, and full automatic quad orchestration execute only through FR-MCP-134/FR-MCP-135 paths, while non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed.
**Covered by:** FR: FR-MCP-131; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Authorized AoT reconciliation, full orchestration, and weight updates route through FR-MCP-134/135 services only. (evidence: QuadBrainOrchestrationServiceTests)
- [x] Non-Curiosity GraphRAG mutation and implicit fallback model behavior remain fail-closed. (evidence: BrainSlotContainmentTests)

## TR-MCP-QUAD-005

**Quad orchestration service and contracts** — Add service, DTO, REST, client, STDIO, and plugin contracts for full Quad-Brain orchestration and AoT reconciliation while reusing the existing transaction-gated brain-slot invocation path.
**Covered by:** FR: FR-MCP-131, FR-MCP-134, FR-MCP-135; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Quad orchestration DTOs, services, REST endpoints, typed client methods, STDIO tools, and Node plugin tools are present for orchestrate, AoT reconcile, and weight update operations. (evidence: BrainSlotContracts; BrainSlotsController; BrainSlotClient; FwhMcpTools; brain-slots.ts)
- [x] Public contract tests prove route/tool parity and mutation failsafe classification. (evidence: BrainSlotsControllerTests; BrainSlotClientTests; BrainSlotContractArtifactTests; brain-slots.test.ts)

## TR-MCP-QUAD-006

**AoT reconciliation decision loop** — Implement deterministic orchestration prompts, role-output aggregation, ArbiterOfTruth reconciliation execution, and final decision response shaping with transaction IDs and diffgram IDs preserved for every role.
**Covered by:** FR: FR-MCP-134; TEST: TEST-MCP-181, TEST-MCP-182, TEST-MCP-184, TEST-MCP-185
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Full orchestration invokes LeftHemisphere, RightHemisphere, CuriosityEngine, and ArbiterOfTruth through transaction-gated slots and returns final committed Arbiter output. (evidence: QuadBrainOrchestrationServiceTests.ExecuteFullOrchestrationAsync_WhenQuadReady_ReturnsCommittedAotDecision)
- [x] Orchestration rejects non-ready workspaces before any role invocation. (evidence: QuadBrainOrchestrationServiceTests.ExecuteFullOrchestrationAsync_WhenNotQuadReady_DoesNotInvokeAnySlot)

## TR-MCP-QUAD-007

**Durable weight versioning and safety gates** — Persist role weights and versions on brain-slot definitions, enforce dual-control and safety-gate validation, audit before/after snapshots, and expose explicit weight update APIs.
**Covered by:** FR: FR-MCP-131, FR-MCP-135; TEST: TEST-MCP-174, TEST-MCP-180, TEST-MCP-181, TEST-MCP-183, TEST-MCP-184, TEST-MCP-185
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] Weight updates require AoT approval, admin approval, safety gates, reason text, valid enabled roles, valid weights, and expected versions before mutation. (evidence: QuadBrainOrchestrationServiceTests)
- [x] Approved updates persist weight/version/timestamp changes, audit before/after snapshots, and provide rollback metadata through the transaction coordinator. (evidence: QuadBrainOrchestrationService; AddBrainSlotWeights migrations; QuadBrainOrchestrationServiceTests)

