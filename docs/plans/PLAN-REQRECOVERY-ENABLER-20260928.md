# PLAN-REQRECOVERY-ENABLER-20260928

## Slice 1 — Dense session-log graph load

`SessionLogService.FindExistingSessionAsync` loaded the session with several sibling `Include`s and no `AsSplitQuery()`. That single query cartesian-joins actions, tags, context, dialog, commits, and string lists. Dense beginTurn, completeTurn, and Submit spent the extra time in that join. On this branch, Submit `SaveChanges` also used the shared 5 second `StorageCommandBudget.Default`, so a dense submit that got past the join could still return HTTP 503.

Slice 1 changes:

- `FindExistingSessionAsync` calls `AsSplitQuery()` on the multi-include chain.
- That materialization runs inside `StorageCommandBudget.ExecuteAsync` using `Mcp:SessionLog:SubmitCommandBudgetSeconds` (default 30, valid 1 through 300).
- SQL deadlock 1205 throws `StorageGraphMaterializationException`. Budget expiry throws `StorageCommandBudgetExceededException`. Both classify as retryable `backend_unavailable`. Neither returns a null session, and the mutation is not persisted.
- Submit `SaveChanges` uses the same 30 second budget. Triage intake and replace/section `SaveChanges` stay on the 5 second default.

Proof is `SessionLogDenseGraphTests` (split SQL, budget expiry, deadlock 1205) and `SessionLogSubmitBudgetTests`.

## Slice 2 — Atomic requirements recovery

Out of scope for the Slice 1 change. This slice does not add a requirements recovery API.
