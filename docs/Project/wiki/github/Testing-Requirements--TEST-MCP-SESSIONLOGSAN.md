# TEST-MCP-SESSIONLOGSAN

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-SESSIONLOGSAN

### TEST-MCP-SESSIONLOGSAN-001

Tests must prove default and configured redaction across the complete session-log DTO graph and all supported read transports without changing persisted raw data or query semantics.

**Acceptance Criteria:**
- [ ] Unit tests cover every default detector, overlapping rules, deterministic replacements, recursive object payloads, invalid patterns, duplicate IDs, timeouts, and non-mutation.
- [ ] Service tests cover QueryAsync and GetAsync for every DTO field and preserve total count, ordering, offset, and limit.
- [ ] HTTP, stdio, and federated integration tests return redacted payloads while direct database verification retains raw values.
- [ ] Executed current-plus-prior test scope reports zero failures and zero skips.

### TEST-MCP-SESSIONLOGSAN-002

Verify TR-MCP-SESSIONLOGSAN-002 using the injected RegexReplaceInvoker to raise RegexMatchTimeoutException deterministically for the pathological large field, without wall-clock timing. The single-string case must return the rule timeout token without logging input; the whole-session case must continue sanitizing other fields. Repeated isolated runs must be stable, and production must retain the default Regex.Replace invoker.

**Acceptance Criteria:**
- [ ] The single-string and whole-session tests inject RegexMatchTimeoutException for the pathological field, assert the timeout redaction token without logging the input, and prove other fields continue to be sanitized.
- [ ] Repeated isolated runs are stable without wall-clock timing, and the production default remains Regex.Replace.
