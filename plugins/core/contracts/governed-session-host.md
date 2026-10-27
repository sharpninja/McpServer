<!-- SPDX-License-Identifier: Apache-2.0 -->
# Canonical A8 host session composition

FR-QB-002/003/005, TR-QB-TRUST-001/AUDIT-001/RUNTIME-001,
AC/TEST-QB-005/007/008/009/012/014/069.

The host session boundary composes Invoke-McpGovernedTurnStore,
Invoke-McpGovernedAuditAliasConsumer, Invoke-McpGovernedAuditOutbox and the existing
typed SessionLog Begin/Complete/read and audit submit/read methods. It owns no
second turn map, event journal, active-session singleton or transport. Bootstrap
supplies private store/outbox roots, validated workspace/agent context, captured
native host key/source, original MCP session, ingress nonce/time/full text and
actual operation identity. Tool arguments cannot replace those roots or native
fields. Missing actual ingress/nonce fails; no environment or latest-cache guess.

mintTurn registers ingress and reserves its full normalized metadata event under
pending-<nonce>. The existing A8 alias transaction reserves the minted request ID
once before server submission; restart reuses it. Reconcile the committed alias
through the existing consumer before replay or acknowledgment. Registration and
minting do not mark delivery or cancel another turn. beginTurn additionally marks
actual host delivery. Legacy Begin delegates to this same boundary. Ordinary
writes resolve the captured host and optional explicit request through A8, require
delivery, and use the resolved original MCP session. Two delivered open requests
are ambiguous without an explicit target. A different host cannot target them.

Action/dialog writes preserve their distinct kinds and full payloads. An explicit
decision uses one decision event; C7 projects the same event identity/conclusion
into the designDecisions/decision dialog/design_decision action triple. A trusted
operation identity supplies the capture key, so retry retains one UUID and order.
An explicit no-decision attestation is identified in its own decision payload;
ordinary tool activity never invents a decision or attestation.

recordReview captures the complete raw review JSON through the lossless normalizer,
retaining lexical accuracy/completeness tokens and all findings. An AGREE review
requires unsigned non-exponent numeric scores at least98 without rounding. Other
requires otherNotes. Review/classification trust and product completion obligations
come from validated host injection, not model-selected roots, phase or reviewer
identity. A recorded review is evidence, never implicit phase acceptance.

Complete first invokes the host's stateless obligation gate. A refusal retains an
open turn and returns its continue/blocked diagnostics without inventing a final
result or automatically retrying. Once allowed, capture the complete prepared
response, flush every expected event and apply CheckCapturedCompletion. Invoke
the existing typed Complete only after matching event readback. Independently
read the same owning server turn and compare full normalized ingress/final text,
roots and terminal status before A8 Terminal. A timeout after commit requires
fresh read-before-retry; matching final state may reconcile the same attempt,
never another turn. Cancellation captures its actual reason, checks all actual
captured evidence without inventing a result, and preserves separate failure
status. Persisted-before-delivery is never claimed as operator delivery.

The same interaction runner first uses an independent test-only model with
scripted storage/transport/policy responses and rejecting controls. Production
substitutes this canonical composition, then binds actual typed-client/SQLite
and native entrypoint tests under a fresh serialized heavy lease. Legacy cache
migration, actual restart/process-crash integration and complete affected and
cumulative suites remain required; component tests do not close this scope.

The existing appendActions/appendDialog method boundary accepts every nonempty
array item in input order. Each item follows this same canonical host capture,
replay and receipt path, and a refusal stops later items. Retry retains prior
item UUIDs. A distinct actual operation remains distinct even with equal content.
The trusted adapter supplies a zero-based internal batchIndex only for multi-item
calls. Batch capture keys use a disjoint kind namespace and a structured original
operation-ID/index pair. Existing single-item capture keys and original host
operation identity remain unchanged; model parameters cannot supply batchIndex.

