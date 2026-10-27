# Existing-ID Use-Case Component Update API Plan

Status: Approved bug-fix execution
Authorized by: operator directive "Fix it" on 2026-10-09
Requirements: FR-MCP-USECASE-018, TR-MCP-USECASE-020, TEST-MCP-USECASE-021

## Problem

The supported use-case surfaces can create and read actors, flows, and steps, but they cannot edit existing component rows. UC-125 therefore cannot be corrected without direct storage mutation or replacement rows, both of which violate the workspace contract and risk changing durable IDs.

## Required result

Add supported authenticated operations that update an actor already associated with a use case, a flow already owned by that use case, and a step already owned by that flow. Successful updates preserve ActorId, FlowId, StepId, UseCaseId, and parent relationships. The operations must not change approval status, version, requirement links, or unrelated aggregate members.

## API contract

- REST:
  - PUT /qbrainai/usecases/{useCaseId}/actors/{actorId}
  - PUT /qbrainai/usecases/{useCaseId}/flows/{flowId}
  - PUT /qbrainai/usecases/{useCaseId}/flows/{flowId}/steps/{stepId}
- Typed client:
  - UpdateActorAsync
  - UpdateFlowAsync
  - UpdateStepAsync
- MCP tools:
  - usecase_update_actor
  - usecase_update_flow
  - usecase_update_step
- CQRS:
  - UpdateUseCaseActorCommand
  - UpdateUseCaseFlowCommand
  - UpdateUseCaseStepCommand

Actor fields are Name, Description, Type, and IsPrimary. Actor identity is workspace-shared, while IsPrimary is association-specific. The endpoint must require the actor to be associated with the named use case before changing either record.

Flow fields are FlowType, Name, and SequenceNumber. Step fields are StepNumber, ActorId, Action, SystemResponse, and DataEntities. The full replacement requests carry every editable field so callers can intentionally clear nullable values.

## Validation and invariants

- Actor name is required and at most 100 characters.
- Actor type is Primary, Secondary, System, or External.
- Flow type is Basic, Alternative, or Exception.
- Flow name is null or at most 100 characters.
- Flow sequence and step number are positive.
- Step action is required.
- A supplied step ActorId must be associated with the same use case.
- Every lookup is scoped by workspace and complete parent identity.
- Missing, soft-deleted, cross-workspace, and parent-mismatched records return a classified not-found result.
- Successful writes change tracked rows in place, advance the parent use case UpdatedAtUtc, emit the established mutation audit event, and create no duplicate rows.

## BDPv4 test sequence

1. Add test-local request/result contract records, an independent scripted fake editor, and shared assertions derived from the acceptance criteria.
2. Run the mock contract suite. Correct fake responses must pass. Deliberately wrong IDs, parent IDs, fields, mutation counts, and exception paths must be rejected inside the passing harness.
3. Record the green mock gate before any production source edit.
4. Add production request models, CQRS commands and handlers, REST routes, typed-client methods, JSON metadata, and MCP tools.
5. Bind the same contract cases and assertions to real handlers using an in-memory database fixture.
6. Add focused controller, typed-client, MCP-tool, persistence, validation, and cross-workspace tests.
7. Run focused tests with zero failures and zero skips.
8. Run the complete applicable unit suite, Compile, and ValidateTraceability with zero failures and zero skips.
9. Commit the validated change, deploy only through Nuke UpdateService, and prove health/version.
10. Use the new supported API to edit existing UC-125 component IDs, then perform authenticated readback proving IDs and Draft/version state are preserved.

## Blast radius

Expected source changes are confined to use-case request models, commands, controller, typed client, JSON source generation, MCP tool surface, tests, and requirement documentation. No schema migration is required because every field already exists. Existing create, link, diagram, approval, and product operations remain unchanged.

## Rollback

Before deployment, rollback is the branch commit reversal. After deployment, Nuke UpdateService remains the only binary deployment path. UC-125 content changes are independently reversible through the same supported update operations because IDs remain stable.

## Completion evidence

Completion requires chronological receipts for the mock gate, production-focused gate, full suite, compile, traceability, signed commit, Nuke deployment, service health/version, and authenticated UC-125 before/after readback. Hostile validation remains required at the next applicable acceptance gate; no goal or TODO is marked done before an AGREE verdict with both scores at least 98 percent.
