# TR-MCP-LOG

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-LOG-001

**Exception Logging in Catch Blocks** *(DIRECTIVE)* - Every `catch` block that handles an exception must log the exception. Unexpected exceptions must use `LogError` with `ex.ToString()` as the message body. Expected/anticipated exceptions (e.g., `OperationCanceledException` on shutdown, `InvalidOperationException` for process-already-exited races, validation exceptions returned as HTTP 4xx) must use `LogWarning` with `ex.ToString()`. Catch blocks must not silently swallow exceptions with empty bodies or comments-only. The only permitted exception is re-throwing (`throw;`) without logging, where the exception will be logged by an outer handler.
**Status:** pending
Scope: layer-1+

## TR-MCP-LOG-002

**Identifier Naming Validation** — `TodoValidator` SHALL validate persisted TODO IDs against the canonical regex set `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` or `^ISSUE-\d+$` for create/update dependency paths across all configured TODO storage providers (`yaml` and `database` per TR-MCP-TODO-005). `ISSUE-NEW` SHALL remain a create-time alias handled before persistence, not a persisted TODO identifier. `SessionLogIdentifierValidator` SHALL validate session/request IDs using canonical timestamped patterns and enforce exact source-type prefix parity (`SessionId` starts with `{sourceType}-` or `{agent}-`). Invalid values return HTTP 400 at controller boundaries and `ArgumentException` for direct service invocation.
**Status:** ✅ Complete

**Covered by:** `TodoValidator`, `TodoService`, `EfTodoService`, `TodoCreationService`, `SessionLogIdentifierValidator`, `SessionLogController`, `SessionLogService`
Scope: layer-1+

## TR-MCP-LOG-003

**Parseable Event Field-Cap Enforcement** — `ParseableEventFormatter` SHALL emit no more than 250 top-level fields for any individual Parseable event payload. The formatter SHALL always preserve the canonical Parseable metadata keys (`timestamp`, `level`, `message`, and `exception` when present), SHALL prevent user-supplied structured properties from overwriting those reserved keys, and SHALL drop excess non-reserved properties once the remaining field budget is exhausted. Property selection for retained non-reserved fields SHALL be deterministic so tests and operational analysis can reason about which fields survive truncation.
**Status:** ✅ Complete

**Covered by:** `ParseableEventFormatter`, `ParseableBatchFormatter`
Scope: layer-1+

