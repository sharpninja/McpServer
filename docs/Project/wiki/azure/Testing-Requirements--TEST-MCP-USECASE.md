# TEST-MCP-USECASE

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-USECASE

### TEST-MCP-USECASE-001

Unit tests cover schema creation, workspace isolation, soft-delete hide, FR string FK link uniqueness, and handler CRUD/link behaviors with zero skips in the executed gate scope.


### TEST-MCP-USECASE-002

Controller unit tests for UseCasesController. Acceptance: Controller tests green 0 skip.


### TEST-MCP-USECASE-003

Diagram golden tests mermaid and plantuml. Acceptance: Mermaid sequenceDiagram; plantuml @startuml.


### TEST-MCP-USECASE-004

Client unit tests with live JSON shapes. Acceptance: Client UseCase filter green including coverage DTO.


### TEST-MCP-USECASE-005

Coverage query tests. Acceptance: Coverage gap tests green.


### TEST-MCP-USECASE-006

Migration apply empty and production-shaped. Acceptance: UseCaseMigrationApplyTests green 0 skip.


### TEST-MCP-USECASE-007

Audit emission on UC mutations. Acceptance: Audit tests green for create/update/delete/link.


### TEST-MCP-USECASE-008

UI asset REST-only tests. Acceptance: UseCaseUiAssetTests green.


### TEST-MCP-USECASE-009

Approval and product hooks tests. Acceptance: Expanded scope approval/product tests green.


### TEST-MCP-USECASE-010

Plugin-core usecase jest tests. Acceptance: usecase.test.ts green.


### TEST-MCP-USECASE-011

Deploy smoke after UpdateService. Acceptance: health, /usecases/, create/link/diagram/coverage smoke log green.


### TEST-MCP-USECASE-012

UML graph serialization goldens Mermaid schema v1 + PlantUML. Covers AC-013-*, AC-014-*, AC-T14-*.


### TEST-MCP-USECASE-013

Diagram graph CQRS/storage get put isolation soft-delete invalid audit. Covers AC-012-*, AC-T11-3, AC-T12-1, AC-T16-1.


### TEST-MCP-USECASE-014

Controller/client diagram-graph and kind=usecase export. Covers AC-T13-*, HTTP AC-012-5.


### TEST-MCP-USECASE-015

Canvas UI asset/contract tests palette canvas drag hooks REST-only. Covers AC-011-*, AC-T15-*.


### TEST-MCP-USECASE-016

Migration apply for graph storage. Covers AC-T11-1, AC-T11-2.


### TEST-MCP-USECASE-017

Adversarial Grok hostile validator + live canvas smoke claim pack.


### TEST-MCP-USECASE-018

For each fourteenth-review behavior, a consumer test and a real-service, native-provider, or adversarial test fail before production changes and pass afterward. SQLite, SQL Server LocalDB, healthy external PostgreSQL, requirements/wiki, repository root/process lifetime, marker/client/REST-MCP/workspace preflight, release build, build.ps1 Compile/Test/ValidateTraceability, trailer, migration, and evidence gates execute with zero failures and zero skips; aborted or timed-out hosts remain inconclusive failures.

**Acceptance Criteria:**
- [ ] Three provider tests prove Unicode dash values persist and replay as canonical ASCII identities and hashes.
- [ ] Real rollback-deletion and directory-creation junction swap tests prove external sentinels are never changed.
- [ ] Native physical-resolution tests prove finite cancellation and repeated-call worker stability, and SaveChangesAsync plus federation upsert exercise that native path.
- [ ] A Linux FIFO with no writer completes promptly with a classified non-regular-file rejection.
- [ ] SQLite probes intentionally diverge managed and native timeouts and handlers, cover pooling and non-pooling plus success/failure/cancellation/disposal/repeat paths, and prove exact restoration.
- [ ] SQLite pre-handle open cancellation, close/dispose races, repeated opens, and interrupt lifetime tests prove bounded completion with no leaked workers or handles.
- [ ] SQLite, SQL Server, and PostgreSQL classification tests inject real provider failures concurrent with cancellation and assert the original exception survives.
- [ ] A real no-environment external-artifact run passes all twenty inherited tests and a real descendant-held-pipe process is tree-killed within the drain bound.
- [ ] Focused RED artifacts precede production edits; every GREEN gate records exact passed, failed, skipped, outcome, and abort status.
- [ ] The final fourteenth evidence hash inventory has no missing, extra, duplicate, stale, or mismatched entry when independently verified.

### TEST-MCP-USECASE-019

For every fifteenth-review behavior, preserve a failing consumer test and failing real-service, kernel, provider, or adversarial test before production changes, then prove focused and cumulative GREEN with exact zero-failure, zero-skip, zero-abort, and zero-inconclusive accounting from C:-physical artifacts.

**Acceptance Criteria:**
- [ ] A real Linux rollback test recreates nested directories and files, validates contents and mode, and proves containment without catch-all exception swallowing.
- [ ] Native platform tests reproduce physical-identity cancellation timing gaps and blocking resolution, assert finite completion and stable worker or resource counts, and contain no silent returns.
- [ ] Linux-native SQLite blocking-open coverage runs where supported and every other platform runs an explicit applicable bounded-open contract with no silent return.
- [ ] A real kernel-wait coincidence test issues cancellation and then throws a distinct IOException or ObjectDisposedException; the exact original exception and message survive close and dispose races.
- [ ] With ContinuousIntegrationBuild=true and no MCP_REPOSITORY_ROOT, the true external seven-class suite passes exactly 20 of 20 and Agent Help passes exactly 18 of 18.
- [ ] All focused, affected-provider, SQLite, LocalDB, PostgreSQL, BDPv4 unit, build.ps1, 12-project Release, repository, harness, and EOL gates report exact parsed zero-failure and zero-skip outcomes.
- [ ] The final fifteenth receipt maps all seven current and ten prior findings to fixes, tests, and results; records prior deterministic 13/20 and Agent Help 8/18 failures; and passes independent inventory and command-output verification.

### TEST-MCP-USECASE-020

For each sixteenth-review finding, preserve a failing consumer-facing test and a failing real operating-system, mount, provider, process, or evidence-boundary test before production changes, then prove focused and cumulative GREEN from C:-physical artifacts with exact zero-failure, zero-skip, zero-abort, zero-inconclusive, and expected-minimum accounting.

**Acceptance Criteria:**
- [ ] Windows and native Linux tests reproduce identity cancellation timing, alias-to-unsafe-mount and replacement races, failed cancellation cleanup, and leaked-worker accounting; public completion is bounded and workers remain observable until actual exit.
- [ ] Native Linux/FUSE tests route a local safe-path symlink or equivalent physical alias into a delayed mount and exercise replacement timing for SQLite and identity resolution, proving finite public completion plus exact worker and handle lifecycle.
- [ ] Mocked PostgreSQL contract tests and a fresh PostgreSQL statement-timeout race distinguish independent server 57014 from a genuinely caller-triggered cancel while a caller token is concurrently cancelled.
- [ ] Platform test discovery and inventory tests prove the exact applicable Windows and Linux/FUSE/non-root cases executed; source-inspection fallbacks, silent returns, skips, aborts, inconclusive results, and below-minimum totals fail the gate.
- [ ] Containment alias-swap read and write loops each prove at least one successful contained operation plus no escape or corruption, and deterministic all-rejected doubles fail the success requirement.
- [ ] A fast native or managed Windows helper spawns a descendant before ordinary post-start assignment could run; timeout and cancellation evidence proves the immediate descendant is captured and terminated, with finite non-Windows process-group regression coverage.
- [ ] Final evidence supersedes the rejected review, supplies exact invocations or a complete command ledger for every mapped cell, retests the immutable final candidate after its last content/evidence commit, and independently verifies reviewed SHA, hashes, mappings, classifications, and TRX totals.
- [ ] Focused RED and GREEN, external deterministic 20/20, Agent Help 18/18, SQLite, LocalDB, fresh PostgreSQL, full BDPv4, build.ps1 Compile/Test/ValidateTraceability, exact-final-HEAD 12-project Release, repository/history/harness/EOL, cleanup, and all seven-current plus ten-prior maps pass exactly as required.
- [ ] A generated-migration inventory proves the exact 23 historical SQL Server OPENJSON backfills remain byte-immutable and the test-only command interceptor scopes bounded plans to that inventory; a real LocalDB provider run completes within the finite 300-second allowance, preserves values, and leaves no database remnant.
- [ ] Consumer and real Windows boundary tests persist and read acceptance criteria with opaque ws:test-style workspace identifiers without native path errors, while existing rooted-path cancellation, alias, and mount tests continue to require physical resolution.

### TEST-MCP-USECASE-021

Prove identity-preserving actor, flow, and step editing through discriminating independent fakes before production, then bind the same assertions to CQRS, REST, client, MCP, persistence, and authenticated readback.

**Acceptance Criteria:**
- [ ] Before production edits, contract tests pass against correct independent fake editor responses and prove inside the passing harness that wrong IDs, parent IDs, fields, and mutation counts are rejected.
- [ ] The same contract cases and assertions pass against the real CQRS handlers for actor, flow, and step updates, including validation, missing, cross-workspace, and parent-mismatch cases.
- [ ] Controller tests prove the three update routes and classified 400/404 responses; typed-client tests prove HTTP methods, paths, payloads, cancellation, and response deserialization; MCP-tool tests prove dispatcher inputs and serialized results.
- [ ] A persistence test proves IDs and row counts are unchanged, aggregate approval/version and unrelated data are unchanged, UpdatedAtUtc advances, and authenticated readback returns the edited values.
- [ ] Focused tests, the full applicable unit suite, Compile, and ValidateTraceability complete with zero failures and zero skips before deployment.

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
