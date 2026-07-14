# aiUnit Review: warning-suppression

- Run-log: `from attribute execution`

## Prompt

```text
Review warning suppression governance for PLAN-WARNREMEDIATION-001.

Scope
- docs/Project requirement exports and live MCP requirements for FR-MCP-139, TR-MCP-QUALITY-001, TEST-MCP-AIUNIT-002
- PLAN-WARNREMEDIATION-001 TODO current decisions and implementation task state
- Directory.Build.props, project NoWarn entries, pragma warning directives, SuppressMessage attributes, editorconfig analyzer severity, and any broad warning bypasses
- tests/McpServer.Review.Tests/AiReviewTests.cs and build/Build.AiWarningSuppressionReview.cs

Approved decisions
- CA1416 may remain suppressed only for Windows only code paths with justification and review condition
- CA1819 may remain suppressed where array returning API is intentional and justified
- Current CA2227 suppressions may remain only for non observable JSON or YAML or options binding DTOs and EF navigation collections
- Observable collections must be repopulated in place rather than suppressed
- CA1308 is not approved and code must use invariant case insensitive comparison or explicit mapping rather than lowercase normalization
- CS8632 is not approved and every project must enable nullable annotations and remove CS8632 NoWarn entries
- TreatWarningsAsErrors false and stale ASP0019 suppressions are not approved and must remain removed

Completed remediation decisions to audit
- xUnit1051 is not approved and test projects must pass TestContext cancellation tokens to cancellable async APIs instead of suppressing the analyzer
- xUnit1041 is not approved and xUnit v3 tests must use supported fixture or ITestOutputHelper patterns instead of suppressing constructor injection diagnostics
- CA1812 is not approved and middleware or DI-only types must be made visible to analyzers through real construction or removed
- CA1848 is not approved and no editorconfig or project-level disable may remain for LoggerMessage guidance
- CA2000 is not approved and disposal warnings must be fixed or proven stale by removing the pragma and building clean
- CA1861 is not approved and constant array arguments must be hoisted rather than suppressed
- CA1062 is not approved and public migration methods must validate arguments rather than suppressing the rule
- CS0436 is not approved and stale type-conflict NoWarn entries must be removed
- CS0618 is not approved and obsolete APIs must be replaced with current APIs plus focused regression tests
- CA1055 is not approved and string return APIs must not advertise URI semantics
- NU5104 is not approved and stable packages must not depend on prerelease packages
- NU1901 and NU1903 are not approved and vulnerable package advisories must be resolved by package updates and a clean vulnerability scan

Acceptance criteria to audit
- Every suppression decision above is captured in TR-MCP-QUALITY-001 structured acceptance criteria
- TEST-MCP-AIUNIT-002 maps to TR-MCP-QUALITY-001 and has aiUnit prompt coverage
- PLAN-WARNREMEDIATION-001 lists approved suppressions separately from required code fixes and marks only validated work done
- No unapproved warning suppression or broad warning bypass is introduced or marked complete without build or test evidence
- Generated requirements documents and traceability mappings include the FR, TR, and TEST records

Return structured findings in the aiUnit review format with severity, title, detail, recommendation, filePath, and line.
Report no findings only if every item above is satisfied by durable artifacts.
```

## Response

```json
{"schemaVersion":"aiunit.review.findings.v1","reviewType":"project","status":"error","summary":"Review response failed aiUnit findings schema validation: Response is not valid JSON: \u0027A\u0027 is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 0.","agent":{"name":"claude","provider":"claude:aiunit-claude-print.cmd","model":"claude-sonnet-4-6"},"findings":[{"severity":"high","category":"review-execution","title":"Review did not produce schema-valid findings JSON","detail":"Review response failed aiUnit findings schema validation: Response is not valid JSON: \u0027A\u0027 is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 0.\n\nRaw response:\nAll access gated - Bash, pwsh, Grep, Glob outside launch dir all need approval. Cannot inspect repo yet.\r\n\r\nAlso: \u0060McpServer.Review.Tests\u0060 source not on disk (glob empty), only the \u0060bin/Debug/net10.0\u0060 launch dir exists. And it\u0027s \u0060net10.0\u0060, but CLAUDE.md says .NET 9 - mismatch.\r\n\r\nBlocked. Need one of:\r\n\r\n- **Approve tool access** (Grep/Glob on \u0060F:\\GitHub\\McpServer\u0060, or pwsh/git), or\r\n- **Tell me scope** of \u0022aiUnit project review\u0022:\r\n  - whole McpServer project?\r\n  - current branch diff only?\r\n  - the \u0060McpServer.Review.Tests\u0060 project specifically (source missing - regenerate?)?\r\n\r\nWhat you want reviewed?","recommendation":"Check the configured aiUnit review agent and retry the review.","agent":"claude"}],"runLog":{"path":"aiunit-results\\aiunit-review-project-20260713T115919.067Z.json","markdownPath":"aiunit-results\\aiunit-review-project-20260713T115919.067Z.md","startedUtc":"2026-07-13T11:59:19.0674389\u002B00:00"}}
```
