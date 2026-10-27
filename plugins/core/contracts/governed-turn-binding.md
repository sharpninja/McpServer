<!-- SPDX-License-Identifier: Apache-2.0 -->
# Governed turn binding: A8 request selection

This is the first implementation increment of the transferred A8 PowerShell
lifecycle. It implements selection under QB-AGENT-001's captured-binding
contract, FR-QB-003 / TR-QB-AUDIT-001 / AC-QB-008 and AC-QB-069, and
FR-QB-005 / TR-QB-RUNTIME-001 / AC-QB-014. It does not claim those criteria
complete. It introduces no transport, storage, application language change,
or fallback to the retired Node lifecycle.

`Resolve-McpGovernedTurnBinding` receives a trusted, consistent snapshot from
the host's lifecycle store, `Context`, a captured `HostSessionKey`, and an
optional `RequestId`. Model arguments never supply the context or host key.
The caller obtains the snapshot and commits any write under the same lifecycle
transaction; this pure selector is not a lock or a persistence acknowledgment.

The snapshot contains `workspacePath`, `agentIdentity`, and `hosts`. Each host
has `hostSessionKey` and `turns`. Each turn has the captured binding fields
`workspacePath`, `agentIdentity`, `mcpSessionId`, `hostSessionKey`, `keySource`,
`ingressNonce`, `requestId`, `boundAtUtc`, plus `delivered` (Boolean) and
`status` (`open` or `terminal`). Workspace and agent identities are already
validated and canonicalized by bootstrap. Selection compares identity strings
ordinally without trimming or case folding. A restored snapshot preserves the
original binding, never a new active session substituted by the selector.

One matching host is required. A missing, blank, non-string, or unknown host
key returns `host_session_unknown`; duplicate host keys return
`host_session_ambiguous`. Within that host, an omitted request ID selects only
when exactly one delivered, open turn exists. Zero gives `request_unknown`;
multiple gives `request_ambiguous`. Mint-only turns never change the implicit
target. An explicit, nonempty string request ID selects its exact owning
record, including a minted record; readiness still must be established by the
ingress/persistence gate before ordinary work. A missing or malformed explicit
ID gives `request_unknown`, never an implicit fallback. Duplicate matching
records give `request_ambiguous`; a terminal selection gives `turn_immutable`.

No match crosses the host, workspace, or agent boundary. A malformed or
cross-boundary record in the selected host fails closed as `request_unknown`;
it is not silently dropped to manufacture an unambiguous set. Trusted recovery
using an earlier `(mcpSessionId, requestId)` pair is a separate lifecycle
operation and cannot be requested by this ordinary-write selector.

The result has exactly `ok`, `code`, `retryable`, and `binding`. Success uses
`ok: true`, `code: resolved`, `retryable: false`, and an independent read-only
dictionary containing all eight binding fields. Failure uses `ok: false`, the
code above, `retryable: false`, and a null binding. Input and captured binding
are never mutated; there is no filesystem, environment, clock, network, or
session-cache access. These error codes reuse the approved final-error list.

The fixture `GovernedTurnBinding.Contract.ps1` runs the same case inputs and
assertions with a scripted, independently specified response provider first,
and the production function second. Negative controls corrupt each root/binding
field, error/success classification, retryability, shape, input preservation,
and read-only behavior. Correct mock results and rejected negative controls
must all pass before production code is added. Results identify this limited
selection increment; durable minting, multi-open storage, alias replay,
transport, decisions, S6 review, and product startup remain separate required
A8/P1 work. No AC or phase is closed by this increment.
