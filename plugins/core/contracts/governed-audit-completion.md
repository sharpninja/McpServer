<!-- SPDX-License-Identifier: Apache-2.0 -->
# Complete expected audit evidence

FR-QB-003/005, TR-QB-AUDIT-001, AC/TEST-QB-007/008/009/069 require
completion to prove the actual expected request, event and final-result set.
This extends the existing canonical outbox boundary. It introduces no other
lifecycle, transport, reviewer authority or product acceptance.

The trusted canonical host supplies its durably captured expected event
manifest, including the ingress event identity and, for successful completion,
the final prepared-result identity. The manifest contains complete immutable
event envelopes, not counts or summaries. Model tool arguments never supply,
replace or shorten this manifest. Missing expected ingress or final-result
identity is a final completion refusal even when no files are pending.

The existing outbox must compare the exact expected UUID set with retained
evidence for that logical owning turn. Roots, kind, positive order, full
payloadJson, payloadHash and positive committed revision must match. Validate
the payload hash from actual UTF-8 bytes. Duplicate or reordered expected
orders, duplicate UUIDs, missing files, same-count substituted evidence,
unexpected records and changed full text fail. Pending, quarantined, rejected
or lost mandatory evidence fails. A retained rejection cannot be erased by
empty pending counters. Another turn's records cannot substitute for this turn.

Every archived expected event requires independent readback through the SAME
supported transport.read boundary. A reply must be persisted and match every
root, UUID, order, hash and the retained positive committed revision. Missing,
wrong, queued, rejected or unchanged-only replies cannot grant completion.
Readiness does not itself complete or mutate a turn. Only the existing
canonical lifecycle performs its selected Terminal operation after this gate.

Pending aliases are first reconciled through the existing durable store and
outbox consumer. UUID/order/payload/hash remain immutable; only the explicit
pending-to-minted request exception applies. Neither old cache identity nor a
latest unrelated session can replace captured ownership. Legacy evidence and
unsupported/missing manifest fields stay retained for supported recovery.

An independent input-only model first executes storage enumeration, byte/hash
inspection and scripted server reads through the common assertion runner.
Negative dependency responses/effects must be rejected by those SAME scenario
assertions before production is written. Production then substitutes actual
retained outbox files and the existing typed-client read transport. The host
must separately prove durable manifest capture, exact full ingress/result
binding, restart/alias/migration and completion ordering through its existing
lifecycle integration tests; this component alone does not establish them.
