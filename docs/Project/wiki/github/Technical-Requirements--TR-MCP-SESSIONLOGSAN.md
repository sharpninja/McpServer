# TR-MCP-SESSIONLOGSAN

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-SESSIONLOGSAN-001

**Bounded outbound session-log sanitizer** — A single sanitizer decorator must clone session-log DTO graphs, apply validated default and configured regex rules with deterministic replacement tokens and finite timeouts, and wrap the final local or federated ISessionLogService read result.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] ISessionLogSanitizer exposes DTO and string sanitization without mutating caller-owned objects.
- [ ] SessionLogSanitizationOptions limits rule count, pattern length, and per-match timeout; invalid or duplicate rule IDs fail options validation with the offending ID.
- [ ] Default rules cover bearer tokens, JWTs, private keys, common API-key and secret assignments, connection-string passwords, and known provider token prefixes.
- [ ] Replacement is deterministic as [REDACTED:<rule-id>] and does not expose captured secret fragments.
- [ ] SessionLogSanitizingService is the outermost ISessionLogService read decorator for HTTP/federation and is also registered in stdio.
- [ ] Regex timeout during a read fails closed for the affected value, emits rule diagnostics without the input value, and never returns the unsanitized value.

## TR-MCP-SESSIONLOGSAN-002

**Deterministic sanitizer timeout redaction and continuation** — The session-log sanitizer per-rule regex evaluation SHALL be exposed through an injectable seam (RegexReplaceInvoker, defaulting to Regex.Replace in production) so affected-field timeout redaction and continuation of other fields can be verified deterministically - forcing exactly one field/rule to raise RegexMatchTimeoutException - independent of catastrophic-backtracking wall-clock jitter. Acceptance Criteria: (AC1) with an injected invoker that raises RegexMatchTimeoutException for the pathological field, SanitizeString returns [REDACTED:{ruleId}:timeout] for that field while other fields continue to be sanitized by their rules, across repeated isolated runs (no flake); (AC2) production behavior is unchanged (default invoker is Regex.Replace). Origin: BUG-TRIAGE-081 (SessionLogSanitizerTimeoutTests flaked under scheduler jitter; the deployed 1ms->100ms budget was a mitigation, not deterministic). Validated by TEST-MCP-SESSIONLOGSAN-002.
**Covered by:** FR: FR-MCP-SESSIONLOGSAN-001; TEST: TEST-MCP-SESSIONLOGSAN-001, TEST-MCP-SESSIONLOGSAN-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] With an injected RegexReplaceInvoker raising RegexMatchTimeoutException for the pathological field, SanitizeString returns [REDACTED:{ruleId}:timeout] for that field while other fields continue to be sanitized; repeated isolated runs do not depend on wall-clock jitter.
- [ ] The production RegexReplaceInvoker defaults to Regex.Replace and preserves production sanitization behavior.

