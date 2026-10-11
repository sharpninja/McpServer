# BUG-TRIAGE-139 twelfth final validation evidence

## Superseded evidence claims

Candidate 4e0751530fdb6035a8195bfd411260260002ef48 was **NOT APPROVED** by the thirteenth independent review. This directory is retained as historical evidence, but it is not authoritative proof of remediation:

- The genuine external-artifact run without MCP_REPOSITORY_ROOT was 13 passed, 7 failed, and 0 skipped. The recorded 20/20 result depended on the root injection shown below.
- The final command-mapping claim was false for federation-affected-final-build-console.txt, marker-fixture-fix-build-console.txt, and root-fix-build-console.txt.
- All aborted testhost runs described below are inconclusive/failing, regardless of partial passing results.

See the fourteenth remediation receipt and evidence directory for corrected proof. No twelfth run output is deleted or relabeled.

Captured on 2026-09-03 from `codex/bug-triage-139-remediation`.

- Test-only RED commit: `16435544c538a331437ab103ec62070d43fb22a3`.
- Primary GREEN commit: `a02762da7e89c5cf13676bef8227b894b9ab14ad`.
- SQLite cancellation and hermetic-root follow-up GREEN commit: `f57de8dead919ad30239ba2cba0da3b37912eb00`.
- Authoritative twelfth review SHA-256: `8789F550072A2AE9470BAFBD7F86CC349DC2CA76D7B8A65202FBD816A2631251`.

All build and test outputs were routed outside the source tree:

- `UseArtifactsOutput=true`
- `ArtifactsPath=C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-twelfth-20260903T201419Z`
- `MCP_REPOSITORY_ROOT=F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-review`

## RED and focused GREEN

The committed RED run executed 20 focused tests: 19 failed, 1 passed, and 0 skipped. The final post-GREEN focused run executed the same 20-test scope: 20 passed, 0 failed, and 0 skipped.

## Complete current-plus-prior gates

- Complete authoritative UseCase class ledger: 252 passed, 0 failed, 0 skipped. The runner selected the same 39 exact classes as the historical 226-definition VSTest scope; xUnit v3 materialized 252 theory rows.
- Marker resolver, nonce trust, and marker trust: 24 passed, 0 failed, 0 skipped.
- Client UseCase: 30 passed, 0 failed, 0 skipped.
- REST/MCP UseCase: 17 passed, 0 failed, 0 skipped.
- Requirements/wiki/transaction/renderer/DocFx: 74 passed, 0 failed, 0 skipped.
- SQLite UseCase provider: 22 passed, 0 failed, 0 skipped.
- SQL Server LocalDB UseCase provider: 16 passed, 0 failed, 0 skipped.
- Healthy external PostgreSQL 17 UseCase provider: 17 passed, 0 failed, 0 skipped.
- Federation provider matrix: 5 passed, 0 failed, 0 skipped.
- Workspace identity/provider preflight matrix: 16 passed, 0 failed, 0 skipped.
- Final twelfth focused scope after the follow-up GREEN commit: 20 passed, 0 failed, 0 skipped.
- Repository audit after the corrected adjacent trailers: 10 passed, 0 failed, 0 skipped.
- Release builds: 12 projects succeeded with 0 warnings and 0 errors.
- `ValidateTraceability`: succeeded.
- `git diff --check`: captured before and after evidence staging in the remediation receipt.

## Provider environment evidence

Two monolithic VSTest authoritative UseCase attempts aborted because their child testhost crashed after 32 and 163 passing results, respectively; neither partial run recorded a failed or skipped test. The same exact 39-class ledger then passed 252/252 in the in-process xUnit v3 runner. Two narrower VSTest PostgreSQL fixture attempts similarly aborted after 7 and 11 passing results. The affected identity and seventh-provider subsets then passed 4/4 and 2/2. A first external-provider launch aborted before producing a test result. The corrected healthy external PostgreSQL 17 environment then executed the complete PostgreSQL UseCase matrix and passed 17/17 with zero failures and zero skips. All aborted and healthy artifacts are retained together.

## Harness evidence retained

- The client test executable first received the xUnit in-process option dialect, rejected those options, and then passed 30/30 with its Microsoft.Testing.Platform option dialect.
- The first traceability invocation stopped at NUKE's first-run telemetry prompt. Only that verified validator child process tree was terminated. The bounded retry used `NUKE_TELEMETRY_OPTOUT=1` and succeeded.
- The first post-GREEN trailer audit found a blank line between `AI-Signature` and `AI-Confidence`. The commit message alone was amended; the rerun passed 10/10. Both artifacts are retained.

Every command is preserved in a matching `*-command.txt` file. Console/stdout/stderr and TRX evidence use the same stem. `SHA256SUMS.txt` records SHA-256 provenance for every other file in this directory.
