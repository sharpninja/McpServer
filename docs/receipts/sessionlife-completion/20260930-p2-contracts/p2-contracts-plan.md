# P2 Cache, Identity, Metadata, and Outcome Contracts

## Status and authority

- Phase: P2 items 1-5 from `docs/plans/sessionlife-batch-completion-20260927.md`, plus the ordered clarification on metadata precedence, immutable session/request binding, degraded preservation, omission, and primary/queued/lost outcomes.
- Base: `cursor/sessionlife-p1-validator-behavior-2908` at `f80e9af6f57029af57b6a3db388002cbdafedb4e`.
- Branch: `cursor/sessionlife-p2-contracts-5cb2`.
- Pull request target: `develop`. The branch carries the unmerged P1 stack.
- Predecessor: P1 validator-behavior AGREE 99/99 on `363855ec`, receipts at `f80e9af6`. This slice does not claim P2 HV AGREE. The parent launches independent Grok HV.
- All 35 batch TODOs stay open. No acceptance-criterion closure. `history-r1` is not rewritten. P3 through P7 are not started.
- `AGENTS-README-FIRST.yaml` is absent in this checkout. This checkpoint records `MCP_UNTRUSTED` and does not call the MCP session log.

## What this slice proves

Proofs go through the real `repl-invoke.ps1` builder and shim, and through child `pwsh -File` for process exit. Receipts are the cache file `session-verb-outcome.yaml` plus `$script:LastReplPersistenceDetails`. Session verbs stay silent on stdout.

- Absent cache, degraded begin, same-request degraded retry, durable reopen, explicit metadata, `canceled`/`cancelled` supersede, and empty query.
- Wrong-workspace marker, same-path marker drift, inherited Grok variables on a Codex host, session A / request R preservation, request mismatch, and native-to-plugin append.
- Session verbs `beginTurn`, `updateTurn`, `appendActions`, `appendDialog`, `completeTurn`, `failTurn`, `setTurnTitle`, and `setSessionTitle` across primary success, confirmed queued success, and neither-destination failure. Assertions cover process exit, receipt fields (`code`, `retryable`, `persisted`, `degraded`, `queued`, `method`, `requestId`, `failsafePath`, `message`, `childStderr`), retained files, and an empty server-state file when the write is lost.
- Duplicate queued updates reuse one failsafe. Additive `appendActions` and `appendDialog` stay distinct. A later drain removes them once. Durable server query is not claimed.
- BUG-TRIAGE-246 proof for `docs/context/module-bootstrap.md`, `docs/REPL-USER-GUIDE.md`, and `docs/context/repl-yaml-message.schema.json`. Contradictions were corrected in those three documents.

## Shared-helper decisions

- `Complete-ReplBeginTurnAfterPersist` and `Get-ReplCompleteTurnPersistSessionId` are unchanged. Degraded and durable reopen keep the existing turn object so that resolver still only appends `degraded` on the already-written file.
- Verb identity is `$script:ReplPersistVerbMethod`, set immediately before `Invoke-ReplPersistTurn`. The persist function does not grow a `VerbMethod` parameter, so existing stubs keep working.
- `unchanged` is exit 0 with `persisted=false` when the logical fingerprint matches `lastPersistFingerprint`. It is not a primary claim.
- `queued` is only a confirmed retryable outage after a failsafe file exists. A non-retryable server rejection is `rejected` and keeps the failsafe.
- `lost` is exit nonzero when neither the server nor a failsafe file received the write.
- A markerless `MCP_CACHE_DIR_OVERRIDE` gets a `failsafe-pending` directory beside that cache when the V4 workspace root cannot be resolved. `MCPSERVER_FAILSAFE_DIR` and `MCP_FAILSAFE_DIR` still win, including a non-directory used to prove a lost write.
- Codex host plus an inherited Grok `PLUGIN_AGENT_NAME` or `MCP_AGENT_NAME` pins the cache key to `codex` and `$script:AgentName` to `Codex`.

## Out of scope

- P3 stop enforcement and bounded child execution.
- P4 replay security and atomicity.
- Closing any of the 35 batch TODOs.
- Rewriting `history-r1`.
- Claiming P2 HV AGREE.
- Treating the focused Pester run as the cumulative unit gate. The follow-up on this branch added the producers and ran the gate. That run is red. P2 exit and HV AGREE stay unclaimed.

## Result

Focused evidence for items 1-5 is in `p2-contracts-focused-run.md`. Those items stay green on the SessionLog suites.

Item 7 was executed as run `p2-unit-20260930T010500Z`. The validator rejected it: `reason=incomplete-trx-outcome` on `McpServer.Support.Mcp.Tests`. Zero skips. The exact failures are in `p2-unit-gate-run.md` and `p2-unit-gate-failures.json`. HV AGREE is not claimed.
