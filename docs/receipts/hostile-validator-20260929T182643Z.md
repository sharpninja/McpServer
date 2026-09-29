# Hostile validator — PR67 Codex P2 classified recovery dispatcher errors

TimestampUtc: 2026-09-29T18:26:43Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer\.worktrees\pr67-reqrecovery
Branch: local/pr67-reqrecovery-slice2 (tracks origin/cursor/dense-sessionlog-split-budget-df51)
BaseTipBeforeCommit: e3e9456b4e439bde8ec22e4b7a93df9d6349ac12
add-profile: executed yes; profile file count read = 20
WorkClass: 1 (project implementation — FR-MCP-REQRECOVERY-001 / Codex P2 on PR #67)
Gate: pr67-codex-p2-preserve-classified-recovery-errors
OverallVerdict: AGREE
Accuracy: 99
Completeness: 99

Accuracy justification: Fresh independent re-read of ReplCommandDispatcher catch + BuildClassifiedError, ReplMcpErrorClassifier typed routes, and three fresh dotnet test filters all Failed=0 Skipped=0. Working tree matches the claimed fix (classified call present; method_invocation_error absent from getRecovery catch slice).
Completeness justification: Surfaces A–D evaluated. A covered Codex P2 contract (409/503/404 classified codes + tests). B checked PowerShell/no-Python and honesty of evidence. C confirmed FR-MCP-REQRECOVERY-001 citations and existing TEST-MCP-REQRECOVERY coverage plus new dispatcher tests. D treated as remediation of an open Codex review thread on PR #67 (not a full plan DoD claim); no plan-step [x] asserted.

## FAIL list
- None.

## UNKNOWN list
- None.

## Claims

### A. Requested validation
- A1 PASS. Working tree on e3e9456b with 1 modified + 1 untracked file for this remediation (ReplCommandDispatcher.cs, RequirementsRecoveryDispatcherTests.cs). Porcelain count 2.
- A2 PASS. DispatchRequirementsRequestAsync catch at ~2005 calls BuildClassifiedError; helper uses ReplMcpErrorClassifier.FromException; BuildError accepts retryable; getRecovery catch slice no longer hardcodes method_invocation_error.
- A3 PASS. ReplMcpErrorClassifier maps McpConflictException→conflict, KeyNotFoundException→not_found, McpClientException StatusCode 503→backend_unavailable (retryable when envelope says so).
- A4 PASS. Fresh tests: RequirementsRecoveryDispatcherTests Failed=0 Passed=4 Skipped=0 Total=4 EXIT=0; RequirementsRecoveryWorkflowTests Failed=0 Passed=3 Skipped=0 Total=3 EXIT=0; ReplMcpErrorClassifierTests Failed=0 Passed=15 Skipped=0 Total=15 EXIT=0.
- A5 PASS. New dispatcher tests assert conflict, backend_unavailable (+Retryable true), and not_found — not method_invocation_error.

### B. Workspace rules
- B1 PASS. Lab PowerShell only for operator scripts; no Python in this change set.
- B2 PASS. Evidence gathered by independent re-run of file reads and dotnet test; not trusted from chat narrative alone.
- B3 PASS. Receipt written under docs/receipts with json twin; also mirrored under ~/.grok/hv-receipts/McpServer.

### C. Requirements
- C1 PASS. Change cites FR-MCP-REQRECOVERY-001 / FR-MCP-TRIAGEERR-001 in dispatcher helper and TEST-MCP-REQRECOVERY-002 in new tests. Existing workflow tests already prove typed 404/409/503 exceptions from RequirementsWorkflow.
- C2 PASS. AC for this Codex P2: 409/503/404 surface classified codes with retryable where applicable — covered by A4/A5.

### D. Plan holistically
- D1 PASS. Scope is a single open Codex P2 on PR #67 discussion_r4136324844 (preserve classified recovery errors). No full plan DoD or merge claim. PR remains unmerged per operator constraint.

## Decision
AGREE. Codex P2 is remediated in the working tree: requirements dispatcher classifies typed recovery failures; tests prove conflict / backend_unavailable / not_found; Accuracy 99 Completeness 99.
