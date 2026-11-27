# FR-MCP-SESSIONLIFE

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-SESSIONLIFE-001 Degraded begin keeps the full turn and does not advertise a stored open

A degraded beginTurn keeps queryText, queryTitle, planFile, todoId, turnRequestId, sessionId, openedAt, build state, and audit counters. Hook status is turn-opened-degraded until the store has the turn. Complete recovery order is cached queryText, then cached queryTitle, then Recovered session-log turn, without forcing queryTitle. Update and append omit an empty QueryText. Supersede persists canceled. appendDialog on a degraded missing turn resubmits once or queues a dialog failsafe. A genuine 404 that was never degraded does not.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A degraded begin retains every cached turn field, including prompt, context, metadata, identity, status, timestamps, title, and counters; it never replaces the turn with a reduced three-field object.
- [ ] Prompt-hook status reports durable opened only after confirmed primary persistence; otherwise it reports opened-degraded and identifies the retained recovery artifact without representing the turn as server-persisted.
- [ ] Complete recovers required query text from the cached query text, then a valid cached title, then the approved placeholder, without replacing a valid title; update omits empty query text so stored query text and title are preserved.
- [ ] A missing-turn appendDialog on an already degraded local turn performs at most one bounded recovery submit and then retains a session_dialog failsafe; an unrelated never-degraded not-found remains a classified nonretryable error.
- [ ] A same-request retry supplies preserved creation metadata, normalizes canceled and cancelled supersession consistently, and never silently converts locally active state into proof of server persistence.

## FR-MCP-SESSIONLIFE-002 Durability before verb mode for plan and todo metadata

Resolve durability first. A matching requestId is omit-unbound only when the turn is already durable. Otherwise beginTurn, update, append, and complete use explicit values, then cached values, then exact None. Ordinary raw first persist rejects an omitted pair. canceled and cancelled supersede first persists may omit either field and receive exact None. Durable reopen omits unbound fields. Assert-ReplCurrentTurnFresh does not rewrite a cached session id. Wrong-workspace marker paths and marker drift are rejected. Get-ReplCompleteTurnPersistSessionId stays unchanged.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] First persistence resolves planFile and todoId using explicit request values, then verified cache values, then the exact sentinel None, and stores a non-null pair.
- [ ] Ordinary raw first persistence rejects omitted, null, empty, or whitespace metadata; canceled or cancelled supersession may omit it and stores None, and a durable reopen preserves that omission contract.
- [ ] Explicit metadata updates both cache and outgoing persistence. Omission after binding is allowed only after durable identity is proven for the exact workspace, agent, session, and request.
- [ ] Freshness validation rejects workspace, agent, session, or request drift and never rewrites or rebinds an already cached session identifier from a marker or inherited process value.
- [ ] The three governing contract documents consistently describe explicit/cache/None precedence, ordinary first-persist rejection, canceled/cancelled supersession, and durable-reopen omission.

## FR-MCP-SESSIONLIFE-003 Three session-log persist outcomes

Primary success, or a confirmed retained failsafe as classified queued success that does not claim primary persistence, or neither destination which is a nonzero failure. appendActions refuses a request-id mismatch. A title that reached neither the server nor a failsafe exits nonzero. Primary success for response, interpretation, tags, and contextList requires a query. Repeated unchanged updateTurn does not write a second session_submit. The wrapper object carries code, retryable, persisted, degraded, queued, method, requestId, failsafePath, message, and child stderr.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Every session-log mutation returns exactly one truthful outcome: primary persisted, degraded queued with a confirmed retained failsafe, or failed/lost with a nonzero process result.
- [ ] The result includes code, retryable, persisted, degraded, queued, method, requestId, failsafePath, message, and diagnostic stderr fields appropriate to the observed outcome.
- [ ] Caller requestId is honored only when it matches the active turn; mismatches are rejected without mutating cache, server state, or recovery artifacts.
- [ ] A primary-success result is accepted only with exact server readback of the intended response, interpretation, tags, contextList, actions or dialog, and identity.
- [ ] Repeated unchanged updates and sequential append/complete calls are idempotent: no duplicate recovery work, child rows, or outcome ambiguity is introduced.
- [ ] Write-ahead recovery is retained before backend submission and deleted only after durable confirmation; a queued result is never represented as primary persistence or accepted TODO completion evidence.
- [ ] A dense existing session containing representative turns and every supported child collection accepts a session update inside the five-second storage command budget, or returns one classified retryable outcome inside that budget. A successful retry has exact durable readback and introduces no duplicate or partial child rows.

## FR-MCP-SESSIONLIFE-004 Failsafe replay, child deadline, and contention classification

One child deadline covers asynchronous stdin and stdout and process-tree cleanup. Hook default bound is 30 seconds. Codex failsafe identity stays Codex when PLUGIN_AGENT_NAME or MCP_AGENT_NAME is inherited as Grok. Quarantine files 3358, d5f9, and 9ad7 are repaired, replayed even when drainAttempts is exhausted, and deleted only after durable success. Import recovery scans pending importRecovery yaml, persists a mapped DTO, and deletes only that envelope after persisted true and degraded false. Busy, locked, budget expiry, and wrapped contention are retryable. Connection outage is backend_unavailable. Automatic and explicit drain do not latch completion on contention.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] One bounded child-process deadline, default 30 seconds for the hook operation, covers stdin writes, stdout and stderr reads, wait, cancellation, and process-tree cleanup without a stranded child or premature disposal race.
- [ ] Codex child execution preserves Codex agent identity and workspace-scoped cache ownership even when inherited environment variables identify Grok or another agent.
- [ ] Quarantine repair accepts historical supported shapes through object-first parsing, fills only missing values with None, preserves valid values, and never fabricates identity.
- [ ] Contention and service outage remain distinct classified outcomes; both drain entry points retain retryable work without consuming attempts or latching completion and later replay exactly once. When storage is actually unavailable, health.storage reports unreachable; contention is not mislabeled as storage-down.
- [ ] Import-recovery ingestion accepts a validated canonical session bundle or reconstructs the complete bundle from verified sources, never submits the importRecovery envelope as a session, and preserves all files on failure.
- [ ] Path containment, reparse-point containment, size and count bounds, source identity, and canonical schema are verified before any source or recovery artifact is read or persisted.
- [ ] Partial and canceled replay remains retryable and idempotent; concurrent replay cannot duplicate work; only the exact unchanged recovery artifact is deleted after verified durable persistence.

## FR-MCP-SESSIONLIFE-005 Stop hook stale pin and session-log uniqueness

Stop hook runs failed-build and incomplete-audit gates first. It allows a stale pin only when authoritative server completion shows the local request id is not the active turn. A genuinely active different request id is not cleared. Both timestamp and lastUpdated stale is the age case. A fresh lastUpdated alone must not pass a stale pin. BUG-TRIAGE-229 is predecessor-schema detection before save, classified provider text in details.inner, and one bounded uniqueness retry.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Failed-build and incomplete-audit enforcement executes before any stale-pin allow, timestamp refresh, auto-close, or recovery that could bypass those gates.
- [ ] A stale local in_progress turn is reconciled only by authoritative Completed proof for the exact workspace, agent, session, and request; active, missing, ambiguous, or unavailable proof fails closed and retains state.
- [ ] Old, fresh, missing, and invalid timestamp and lastUpdated combinations, completed or missing local files, and mismatched request IDs are covered; recency alone never proves completion.
- [ ] Schema-predecessor failures are detected before mutation and provider errors retain a classified details.inner diagnostic without leaking credentials.
- [ ] One bounded retry applies only to the known unique same-session/request race; the winner is reread and merged without duplicate children or loss of unrelated state, while unrelated failures and cancellation propagate.

