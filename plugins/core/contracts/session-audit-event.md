<!-- SPDX-License-Identifier: Apache-2.0 -->
# Session audit event and decision assertions

FR-QB-003/005; TR-QB-AUDIT-001; AC/TEST-QB-008/009/069.
The canonical plan's dependency 11 and audit classification contract govern this
increment. Existing SessionLogService, typed SessionLogClient, MCP tools and REPL
remain the production boundaries. This contract grants no turn, creates no
parallel lifecycle, and does not replace the existing outbox or A8 store.

The captured event has workspacePath, agentIdentity, mcpSessionId, requestId,
eventId, order, kind, payloadJson and payloadHash, matching the governed outbox.
Its UUID, positive safe integer order and exact UTF-8 payload bytes are immutable.
The SHA-256 is lowercase. A receipt echoes all seven outbox identity/order/hash
fields and returns code plus positive committed revision for persisted/unchanged.
An unchanged reply must independently read as persisted at that same revision.
Turn outcomes created/merged/skipped_terminal are separate from event receipts.
Identical turn resubmission remains merged with reason no_change, never
skipped_duplicate. A successful turn write is not proof of an event receipt.

One event replay applies once. Equal content with distinct event identities
remains distinct. Changing the owning request, payload, kind or captured order
under an existing event identity is refused without replacing original evidence.
Captured order governs projections even when commits/acknowledgments arrive
out of order. Two distinct events cannot claim one captured position in a turn.
Invalid event identity, order, kind, JSON or hash is rejected before mutation.
Unknown requests never create another turn.

An explicit decision payload contains its nonblank conclusion and original
timestamp. One committed event produces the same conclusion in designDecisions,
a category=decision processingDialog item and a type=design_decision action.
The event identity links the representations; replay adds none of them again.
Distinct equal decisions remain distinct. A tool action or tool dialog does not
invent a decision. Rejection or failed transaction cannot leak part of a triple.
Legacy write/replace/delete semantics remain subject to their existing contracts;
the event ledger must retain immutable event evidence independently of later
explicit section maintenance. The full regression suite must verify coexistence.

The first common assertion runner is ServerAuditEvent.Contract.ps1.
Its independent model executes state transitions, hashes payloads, stores events
and rejection evidence, commits revisions, and projects action/decision/dialog
state. It has no case-name outcome table. Deliberate faults alter actual state
transitions, effects or dependency replies and must fail the scenario assertions.
Both modes run identical scenario inputs and assertions. Production requires an
explicit real service adapter; missing adapters fail before the run.

The adapter operations Seed, Submit, Read, Project, Snapshot and Complete are
test-only fixture seams. Seed uses an existing session/turn fixture. Submit/Read
bind to the actual session-log event boundary, Project independently reads the
real stored turn, and Snapshot captures real storage/transport effects. Complete
uses the existing completion assertion. Configure is only dependency-failure
injection before/after the actual commit; it is never an event field, public
model tool, setting or environment switch. No injected success replaces a real
database read or commit.

The original sixteen scenarios and eighteen controls are retained. Three decision-stage rollback scenarios, six serialized-reopen/two-context scenarios and the captured safe-order boundary extend the same runner to twenty-six cases. All original twenty-seven controls remain, with forty-five typed-revision controls at submit, independent-read and replay boundaries, one captured-order control, and ten valid-positive revision probes. Committed revisions must be numeric, finite, integral and positive; booleans, fractional/non-finite numbers, missing or malformed values cannot acknowledge persistence. No new revision upper bound is inferred. The independent model stages and commits actual row state; no case-name outcome summaries are returned.

ConcurrentSubmit and Reopen are additional test-only adapter operations. Mock mode serializes submissions against independently identified contexts and serializes/restores store state. That is deterministic assertion evidence, not actual concurrent SQLite execution, context disposal, process restart, filesystem durability or power-loss proof. Production must use genuinely overlapping independent SQLite contexts and reopen the real stored graph while retaining these exact scenario inputs and assertions. All affected transports/hosts and cumulative gates remain required; historical C6 success is a prerequisite, not C7 acceptance.

Terminal rejection retains the event and matching receipt/reason/storedStatus,
does not mutate the terminal turn, is never retried or quarantined, and blocks
successful completion even after pending counters reach zero. Before/after-commit
timeouts preserve attribution and permit safe same-event reconciliation through
the supported boundary. No queued, synthetic, zero-revision or mismatched reply
satisfies persistence. All chronological failures and prior fixture versions
remain in the S receipts. ProductComplete remains false.
The original twenty-six scenarios and seventy-three controls remain intact. One exact captured-envelope scenario adds backslash path spelling distinct from canonical tenancy, JSON formatting/CRLF and non-ASCII text retained through commit, independent reopen/read and replay. Two independently corrupting storage controls normalize the captured path or reformat captured JSON and must reject through those same assertions. Totals are twenty-seven scenarios and seventy-five rejecting controls; production substitutes the actual provider only. New captured evidence fields must avoid the inherited canonical workspace-key conversion without changing that upstream policy.

The original twenty-seven scenarios and seventy-five controls remain intact. A second valid workspace uses the same agent/session/request and event UUID. Its fresh read sees no foreign receipt; submission returns a root-matching payload_conflict rejection with no foreign revision, payload or stored status, and changes none of either workspace's original projections or event evidence. Reopen and retry preserve these outcomes. Six independent interaction/state faults prove rejection of UUID rebind, foreign receipt disclosure, original erasure, foreign projection, foreign revision and an unclassified storage exception. The SAME runner now contains twenty-eight scenarios and eighty-one rejecting controls; real storage must bind each operation to its captured workspace and preserve the global UUID unique constraint and tenant filters.
