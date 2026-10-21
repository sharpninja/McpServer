# PowerShell governed audit outbox

FR-QB-003/005; TR-QB-AUDIT-001; AC-QB-008/009/069; TEST-QB-008/009/069.

`Invoke-McpGovernedAuditOutbox` receives a trusted absolute `RootDirectory`, a
captured `Context` containing workspacePath, agentIdentity, mcpSessionId and
hostSessionKey, an `Operation`, and `InputObject`. It has no environment lookup,
active-turn singleton, HTTP client or authority to complete a turn. The caller
supplies the supported plugin/REPL transport's submit/read scriptblocks for Flush.
Enqueue accepts requestId, kind and an already captured payloadJson object string.
It validates but never reserializes that string. Its UTF-8 SHA256 is lowercase.

Operations: Enqueue returns queued with its durable event, or lost; ApplyAlias
returns applied/unchanged or lost; Resolve returns resolved with requestId or lost;
Flush returns persisted/pending/quarantined/rejected counts and ordered receipts.
Any operation-level storage or validation failure returns lost and blocks execution.
These local results never grant phase or turn completion.

The context key, YAML sequence, event and applied-alias formats retain the existing
lib-node audit-outbox contract. Host-session identity participates in the directory
key. Every new event has an independent UUID and a durably reserved positive safe
integer order. All retained folders participate in sequence rollback checks.
Existing event payloads, ids, order and filenames survive alias application; only
the owning requestId changes, after the alias journal has been flushed and published.
Contradictory aliases, independent canonical sequences and already attempted pending
events cannot be rebound. Acknowledgment belongs to A8 after durable application.

PowerShell writers use an OS-held exclusive `.lock` handle. A nonempty legacy lock
is refused rather than guessed stale. The supported migration must stop legacy
writers before opening the same storage; mixed language writers are not supported.
Paths, including dangling symlinks, are rejected before traversal or transport.
Atomic same-directory writes flush the file before publication; abandoned temporary
files remain visible recovery evidence and are quarantined before replay. The
contract tests prove process termination/restart, not machine power-loss durability.

Before each submit, persist its attempt and retry deadline. Only structured known
transient transport errors, or explicitly retryable degraded persistence errors,
retry. Delays are 1,2,4,8,16,30 seconds, capped at 10 attempts; unknown/final errors
remain final even if the peer reports retryable=true. An unchanged acknowledgment
requires an independent matching persisted readback at the same positive revision.
Every receipt must match workspace, agent, session, request, event, order and hash.
Terminal skipped_terminal receipts are durably retained as rejected, not quarantined,
and never resubmitted. Final failures and exhausted retries quarantine evidence and
stop further replay. An empty pending directory cannot hide retained blockers.

Clock, sleep and publication-observer scriptblocks are trusted test-only injection
seams, not settings or environment flags. Fixtures first validate every outcome and
interaction assertion with independent correct and deliberately incorrect scripted
responses, then run those same scenarios against production. Linux is the current
execution target; Windows and full P1 acceptance remain pending.
