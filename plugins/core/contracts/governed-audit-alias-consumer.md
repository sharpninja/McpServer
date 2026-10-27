<!-- SPDX-License-Identifier: Apache-2.0 -->
# A8 audit alias consumer

FR-QB-003/005; TR-QB-AUDIT-001; AC/TEST-QB-008/009/069.
This adapter uses the existing durable turn store and audit outbox operations.
It creates no turn, session, journal, notification bus or transport.

Invoke-McpGovernedAuditAliasConsumer receives trusted StoreDirectory, OutboxRoot,
Context (workspacePath, agentIdentity, aliasConsumers) and captured HostSessionKey.
The consumer id is qb-audit-outbox-v1. It durably lists unacknowledged A8 aliases,
validates their captured workspace/agent/host/session attribution, applies each
through the outbox ApplyAlias boundary, then acknowledges the exact pending/minted
session pair through A8 Acknowledge. An apply failure prevents acknowledgement.
An acknowledgement failure retains discoverable alias evidence. Duplicate calls
and interruption after durable apply are harmless; no remint or event replacement.
The consumer returns ok/code/applied/acknowledged/detailCode; failure blocks work.

Dependencies and StepObserver are trusted internal test seams, never model tools,
settings or environment flags. Normal dependencies call only the canonical store
and outbox modules. The same scenario runner first uses independently authored
test-only store/outbox/consumer models, with real retained fixture state and
rejecting interaction controls, then binds the identical assertions to production.
Application startup/mint/drain wiring and legacy migration remain required.
