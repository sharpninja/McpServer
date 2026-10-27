<!-- SPDX-License-Identifier: Apache-2.0 -->
# Authoritative expected audit manifest

FR-QB-003/005, TR-QB-AUDIT-001, AC/TEST-QB-007/008/009/069.
This is an extension of the canonical outbox sequence journal and its existing
lock, alias and transport boundaries, not a second lifecycle or transport.

Before an event file is published, the same atomic sequence publication reserves
its UUID, positive order and complete immutable envelope in an `evidence` entry
for the stable logical request key. The entry contains expectedEvents,
ingressEventId, resultEventId, captureKeys and legacyIncomplete. A crash after
that commit and before event publication retains the expected missing UUID;
empty pending files can never erase that obligation. The original order counter
must agree with the complete manifest length. Legacy reserved orders without a
manifest remain incomplete and require supported recovery, not inferred text.

Trusted host capture supplies optional evidenceRole ingress/result and a stable
captureKey from the actual ingress/tool/result identity. Neither is taken from
model-authored routing arguments. A repeated captureKey with the identical kind,
full payload and role returns the same event without incrementing order or
publishing another file. Conflicting reuse or a second distinct ingress/result
is refused without overwriting original evidence. Unlabelled events remain
expected but cannot invent ingress or final-result evidence. Equal payloads with
distinct capture identities remain distinct events.

ReadManifest returns an independent durable snapshot after alias reconciliation.
CheckCapturedCompletion derives the expected set and ingress/result identities
from that journal and the selected original owning request. Caller-supplied
expectedEvents or role identities cannot substitute, truncate or rewrite it.
Successful completion requires a captured final result; cancellation requires
actual ingress and all actual captured events but does not invent a result.
The existing CheckCompletion field-by-field/storage/readback assertions apply.

Alias application journals the binding first, then reconciles manifest and event
request IDs idempotently on restart. UUID/order/kind/payload/hash/capture keys
remain unchanged. Old and minted request IDs resolve to the same logical key.
Only the existing A8 store/alias consumer and canonical host lifecycle may use
these internal operations. Host integration must prove full ingress/result
capture, multi-open ownership, explicit targeting and Terminal ordering; these
manifest component tests alone do not establish host or P1 acceptance.

A retry after interrupted event publication recreates a missing event file from
its already committed capture identity and full envelope, retaining the original
UUID/order/hash. It never increments the sequence or invents a second capture.

