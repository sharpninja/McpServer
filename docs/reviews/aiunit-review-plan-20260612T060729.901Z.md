# aiUnit Review: plan

- Run-log: `aiunit-review-plan-20260612T060729.901Z.json`
- Source: `F:\github\mcpserver\artifacts/aiunit-plan-review/aiunit-review-plan-20260612T060729.901Z.json`

## Prompt

```text
Review PLAN-TURNTRANSACTIONS-001 in F:\GitHub\McpServer. Scope: docs/Project/Quad-Model-Transactional-Diffgram-Plan.md, docs/Project/TurnTransactions-Architecture-Round1.md, docs/Project/TurnTransactions-Design-Round2.md, docs/Project/Functional-Requirements.md FR-MCP-118 through FR-MCP-128, docs/Project/Technical-Requirements.md TR-MCP-KEYSERVER-001 through TR-MCP-TXNDESIGN-001, docs/Project/Testing-Requirements.md TEST-MCP-158 through TEST-MCP-173, and the current transaction-security implementation/tests. Do not edit files. Treat explicitly documented deferred work as non-blocking for this review unless it hides an untracked safety, correctness, or validation gap. Flag critical/high only for issues that should block continuing the next PLAN-TURNTRANSACTIONS-001 slice.
```

## Response

```json
{
    "schemaVersion": "aiunit.review.findings.v1",
    "reviewType": "plan",
    "status": "pass",
    "summary": "No critical or high findings. All material gaps are explicitly documented as deferred in requirements, test records, and design docs. The first-slice in-process implementation is internally consistent with its stated scope. Seven medium and four low findings are present; none should block continuing the next PLAN-TURNTRANSACTIONS-001 slice.",
    "reviewedScope": "docs/Project/Quad-Model-Transactional-Diffgram-Plan.md, docs/Project/TurnTransactions-Architecture-Round1.md, docs/Project/TurnTransactions-Design-Round2.md, FR-MCP-118..128, TR-MCP-KEYSERVER-001..TR-MCP-TXNDESIGN-001, TEST-MCP-158..173, src/McpServer.Support.Mcp/Services/TransactionSecurityServices.cs, src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs, src/McpServer.Support.Mcp/Models/TransactionSecurityModels.cs, src/McpServer.Support.Mcp/Options/TransactionSecurityOptions.cs, src/McpServer.Client/McpServerClient.cs, tests/**/*TransactionSecurity*Tests.cs, tests/**/*TurnTransactionCoordinator*Tests.cs",
    "agent": {
      "name": "ClaudeCode",
      "provider": "Anthropic",
      "model": "claude-sonnet-4-6"
    },
    "findings": [
      {
        "severity": "medium",
        "category": "correctness",
        "title": "TransactionDegradedModePolicy includes TransactionsDisabled as a degraded trigger",
        "detail": "TransactionDegradedModePolicy.ShouldEnterDegradedMode at TurnTransactionCoordinator.cs:113-115 lists TransactionFailureReason.TransactionsDisabled as a degraded-mode trigger. This is semantically wrong: a disabled feature is not an unavailable dependency. Degraded mode implies the system is healthy but a dependency is down; TransactionsDisabled means the feature is off by config. The path is dead code in practice (the disabled case exits via bypass before any failure reason is produced) but the false equivalence will mislead future slices that branch on degraded state. Unknown is also listed; triggering degraded on every uncategorized failure masks bugs.",
        "recommendation": "Remove TransactionsDisabled (and consider removing Unknown) from the degraded-mode reason list in TransactionDegradedModePolicy. If Unknown must degrade, add a test that asserts it and a comment explaining why.",
        "filePath": "src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs",
        "line": 110,
        "ruleId": "TXNPLAN-DEGRADED-POLICY",
        "confidence": 0.95
      },
      {
        "severity": "medium",
        "category": "correctness",
        "title": "TurnTransactionCoordinator mutates the caller\u0027s TurnTransactionRequest object",
        "detail": "ExecuteAsync at lines 243-245 of TurnTransactionCoordinator.cs writes back to request.TransactionId, request.PublisherPartyId, and request.SubscriberPartyId. A caller that inspects those fields after ExecuteAsync returns will see coordinator-assigned values rather than the original inputs. No current test catches this. Callers that log or audit the original request before calling, then compare after, will observe unexpected mutations.",
        "recommendation": "Copy the normalized values to local variables and build the result from those instead of writing back to the caller\u0027s request. The interface contract should be non-mutating.",
        "filePath": "src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs",
        "line": 243,
        "ruleId": "TXNPLAN-REQUEST-MUTATION",
        "confidence": 0.92
      },
      {
        "severity": "medium",
        "category": "correctness",
        "title": "EnsureDefaultPartiesAsync hard-codes key ID convention from InMemoryKeyServerService",
        "detail": "TurnTransactionCoordinator.EnsureDefaultPartiesAsync at lines 472 and 479 probes for keys using string-interpolated IDs {partyId}:signing:1 and {partyId}:encryption:1. This mirrors the private NormalizeKeyId convention inside InMemoryKeyServerService but is not defined on any interface or configuration. When a real or external keyserver is introduced in a later slice, this probe will fail for any party registered with a non-default key ID, causing spurious auto-re-registration and potentially masking real missing-party errors.",
        "recommendation": "Define the default key ID naming convention as a constant or configuration property shared between InMemoryKeyServerService and TurnTransactionCoordinator, or add a HasPartyAsync method to IKeyServerPartyRegistry so the coordinator doesn\u0027t need to know key ID conventions.",
        "filePath": "src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs",
        "line": 472,
        "ruleId": "TXNPLAN-KEYID-CONVENTION",
        "confidence": 0.9
      },
      {
        "severity": "medium",
        "category": "documentation",
        "title": "ISubscriberCommitService XMLDoc cites wrong requirement IDs",
        "detail": "The XMLDoc summary for ISubscriberCommitService at TransactionSecurityServices.cs:68 reads \u0027FR-MCP-123 and FR-MCP-124\u0027 but FR-MCP-123 is Quad-model future scaffolding and FR-MCP-124 is aiUnit plan review. The subscriber commit service should reference FR-MCP-119 (Subscriber diffgram commit service). Similarly TransactionStatusResponse at Models.cs:345 cites FR-MCP-122 (Byrd v4 execution control) when it should reference FR-MCP-119. ValidateTraceability will not catch these because both cited IDs exist; the semantic traceability matrix is silently incorrect.",
        "recommendation": "Correct the XMLDoc references: ISubscriberCommitService -\u003E FR-MCP-119, TransactionStatusResponse -\u003E FR-MCP-119 AC-FR119-008.",
        "filePath": "src/McpServer.Support.Mcp/Services/TransactionSecurityServices.cs",
        "line": 68,
        "ruleId": "TXNPLAN-WRONG-FR-CITATION",
        "confidence": 0.98
      },
      {
        "severity": "medium",
        "category": "security",
        "title": "Nonce is deterministic {transactionId}:{sequence} rather than cryptographic random",
        "detail": "TurnTransactionCoordinator.SignManifestAsync at line 397 sets Nonce = \u0027{transactionId}:{sequence}\u0027. Since transactionId is UUID-based, replay protection holds for the first in-process slice. However, a deterministic nonce means that any future component that generates a manifest independently (rather than through the coordinator) can predict the nonce for a given transaction, weakening the replay guard. The architecture doc explicitly defers complete replay/sequence/expiry enforcement, so this is documented, but the gap must be tracked for the crypto extraction slice.",
        "recommendation": "Add a TODO requirement tracking that nonce must be replaced with cryptographic random (e.g., Guid.NewGuid().ToString(\u0027N\u0027)) before the crypto extraction slice. Do not change in this slice.",
        "filePath": "src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs",
        "line": 397,
        "ruleId": "TXNPLAN-DETERMINISTIC-NONCE",
        "confidence": 0.88
      },
      {
        "severity": "medium",
        "category": "correctness",
        "title": "InMemorySubscriberCommitService sequence enforcement has a TOCTOU gap under concurrency",
        "detail": "CommitDiffgramAsync checks manifest.Sequence \u003C= lastSequence at line 588 and updates lastSequence at line 608, but these are not atomic. Two concurrent commits for the same publisher-subscriber pair at the same sequence number can both pass the check (before either updates the cursor) and both succeed if they use distinct transaction IDs and nonces. The architecture requires monotonic sequences but the in-memory implementation cannot enforce uniqueness without a compare-and-swap on the sequence. The durable DB implementation (explicitly deferred) would enforce this with a unique constraint.",
        "recommendation": "Add a test documenting this known in-memory limitation with a comment referencing the deferred durable-storage AC (AC-FR119-004). No code change needed in this slice; track as a durable-storage acceptance criterion.",
        "filePath": "src/McpServer.Support.Mcp/Services/TransactionSecurityServices.cs",
        "line": 587,
        "ruleId": "TXNPLAN-SEQUENCE-TOCTOU",
        "confidence": 0.82
      },
      {
        "severity": "medium",
        "category": "correctness",
        "title": "Degraded-mode mutation gate is not enforced by the coordinator",
        "detail": "When a subscriber commit fails and degraded mode is entered, the coordinator sets _degraded = true via RecordStatus but subsequent calls to ExecuteAsync do not check _degraded before signing and mutating. FR-MCP-121 AC-FR121-003 and AC-FR121-004 require that degraded mode blocks all writes except health/status/audit. The coordinator only signals degraded state in GetStatus(); it never refuses work because of it. TEST-MCP-161 explicitly defers global mutation adapter coverage, so this is documented. However, the gap means future adapter slices could inadvertently rely on coordinator-level enforcement that does not exist.",
        "recommendation": "Add a comment in ExecuteAsync noting that degraded-mode write blocking is the caller\u0027s responsibility (via GetStatus().Degraded) until global mutation adapters are implemented per AC-FR121-003 and AC-FR121-004.",
        "filePath": "src/McpServer.Support.Mcp/Services/TurnTransactionCoordinator.cs",
        "line": 232,
        "ruleId": "TXNPLAN-DEGRADED-NOT-GATED",
        "confidence": 0.9
      },
      {
        "severity": "low",
        "category": "correctness",
        "title": "RegisterPartyAsync accepts any string as party role without validation",
        "detail": "InMemoryKeyServerService.RegisterPartyAsync at line 179 accepts any non-empty string for the role field. No validation against the known roles (publisher, subscriber, arbiter) is performed. Manifests signed for a party with a typo\u0027d role would pass all current checks. The architecture doc defines four roles but does not yet require enforcement.",
        "recommendation": "Either add a role allowlist check, or add a test asserting the current behavior and a comment explaining that role validation is deferred to the durable registry slice.",
        "filePath": "src/McpServer.Support.Mcp/Services/TransactionSecurityServices.cs",
        "line": 179,
        "ruleId": "TXNPLAN-ROLE-VALIDATION",
        "confidence": 0.78
      },
      {
        "severity": "low",
        "category": "correctness",
        "title": "DiffgramCommitResponse.Status and TurnTransactionResult.Status use stringly-typed values",
        "detail": "Commit response status values (\u0027committed\u0027, \u0027duplicate\u0027, \u0027rejected\u0027, \u0027aborted\u0027) and coordinator result status values (\u0027bypassed\u0027, \u0027committed\u0027, \u0027rejected\u0027, \u0027degraded\u0027, \u0027aborted\u0027) are all untyped strings. TransactionFailureReason is a proper enum but status is not. A typo in a future adapter or handler comparing these strings would compile silently. The test suite hardcodes expected string literals which partially mitigates this.",
        "recommendation": "Define a TransactionStatus enum or string constants class for the status values to enable compile-time checking in future adapter slices.",
        "filePath": "src/McpServer.Support.Mcp/Models/TransactionSecurityModels.cs",
        "line": 326,
        "ruleId": "TXNPLAN-STRINGLY-STATUS",
        "confidence": 0.75
      },
      {
        "severity": "low",
        "category": "correctness",
        "title": "CommitDiffgramAsync does not null-guard request.Manifest before field access",
        "detail": "InMemorySubscriberCommitService.CommitDiffgramAsync at line 558 assigns manifest = request.Manifest then immediately accesses manifest.TransactionId at line 559 without a null check. DiffgramCommitRequest.Manifest is initialized to new TransactionManifestDto() in the property initializer, preventing null in normal usage. However, a caller that explicitly sets Manifest = null would cause a NullReferenceException rather than a structured rejection. The controller layer should validate before the service is called.",
        "recommendation": "Add ArgumentNullException.ThrowIfNull(request.Manifest) immediately after the request null check at line 556, or use pattern matching to return a structured rejection when Manifest is null.",
        "filePath": "src/McpServer.Support.Mcp/Services/TransactionSecurityServices.cs",
        "line": 558,
        "ruleId": "TXNPLAN-MANIFEST-NULL-GUARD",
        "confidence": 0.8
      },
      {
        "severity": "low",
        "category": "documentation",
        "title": "AC-FR122-001 through AC-FR122-005 have no captured validation evidence for this slice",
        "detail": "FR-MCP-122 (Byrd v4 execution control) requires validation command output to be attached as evidence before marking each AC satisfied. All five ACs remain unchecked. TEST-MCP-163 explicitly notes broader evidence is required before later slice closeout. No evidence artifacts exist in the repository for the current slice\u0027s test runs.",
        "recommendation": "Before closing out the current slice, run dotnet test on the affected test projects and attach the output as a validation artifact. This is not a code defect but a process gate required by AC-FR122-005.",
        "ruleId": "TXNPLAN-BYRD-EVIDENCE",
        "confidence": 0.95
      }
    ]
  }
```
