# PR87 Round 15 prelaunch holistic audit

Measured local audit time: 2026-11-09T14:02:06-06:00. Auditor: Codex, gpt-6.1-sol high.

## Scope and authoritative state

- Objective: publish PR87 for full Claude Code Opus 5.5 high hostile validation, then continue until overall and applicable per-task AGREE scores are at least 98/98. Merge remains a separate operator decision.
- Operator publication scope: Linux tests only. Historical Windows evidence remains available, but no new Windows test is a publication gate.
- Final code/test head: pushed `a1e19d7331a2e9804c66ef96a27ee50b2365510c` on `origin/fix/unit-suite-green-20261019`.
- Code delta from `03eea747` to `a1e19d73`: six test or test-project paths and records only, with zero product paths. Receipt: `ug-r15p3-code-delta-03eea747-to-a1e19d73.txt`, SHA-256 `2BB0A536525200D93333213473C93430DC4FBB8D5E97B057573D145A8A3B71D5`.
- Current Linux evidence manifest: `docs/receipts/hv/unitgreen-r15-trx-a1e19d73/manifest.json`, SHA-256 `99A3DE760BF29C31BBDE0B98C3E31855CB92CB95FC202FCC79A96912AAC6321E`.

## Requirement and mapping audit

- FR-MCP-004 criterion `embedding-vector-contract` remains unsatisfied before HV. Its four outcomes are source-pinned: nonzero output for `hello world`, equal outputs for identical input, distinct outputs for the two existing different inputs, and 384 nonzero values for the existing 500-word input.
- TEST-MCP-EMBEDDING-001 remains unsatisfied before HV. Its twelve supplied-response rows cover four correct responses, four independently wrong responses, and four unavailable responses. The pure contract uses no disk, environment, network, native inference or clock.
- TR-MCP-EMBEDDING-001 remains unsatisfied before HV. Its criteria cover all four real bindings, unique test-owned artifacts, missing or corrupt artifacts as failures, and no user-cache mutation.
- The supported plugin readback shows FR-MCP-004 mapped to TR-MCP-EMBEDDING-001 and TEST-MCP-EMBEDDING-001. Receipt SHA-256: `5F447CE2B7FD0177056EF33CB59C0562CC9323F46CC8B103643744F552381CCA`.
- ValidateTraceability passed at the final head. Its output had no live SQLite mapping source, so that exit alone is not used as mapping proof; the plugin readback above is the supplemental evidence.
- No criterion or task is marked accepted by this audit. Acceptance remains the Claude HV result and the existing per-task process.

## Structured what-if pass

- Crash or interrupted process: the fixture uses a unique temporary root. A process kill can leave that test-owned directory behind, but cannot alter the user model cache or product state. The requirement does not claim process-kill cleanup.
- Restart or replay: every fixture instance has a unique root and downloads independent artifacts. Repeated runs do not reuse or trust a prior fixture directory.
- Ordering: the assertion contract mock gate ran before the real fixture and binding changes. The service is disposed before root deletion. Native module resolution is test-assembly scoped so xUnit ordering cannot decide whether the packaged ONNX runtime loads.
- Concurrency: the real model is owned once by the asynchronous class fixture, and the fixture root is not shared with user or product state. The cleanup retry responds only to `IOException`; other exceptions are not swallowed.
- Boundary and malformed input: the contract rejects all-zero output, unstable identical-input output, equal different-input output, wrong-sized long-input output, correctly sized all-zero long-input output and unavailable model state. The fixture controls reject missing and corrupt artifacts.
- Unsupported hosts: no unit case is platform conditional. The only measured Linux and Windows count difference is in the integration lane. The operator explicitly selected Linux-only publication validation after the Windows cleanup diagnostic.
- Lifecycle: cleanup retry is bounded to 20 attempts with 100 ms intervals. A final `IOException` remains a failing cleanup, so a lock cannot be converted into a pass.
- Isolation: artifact paths are explicit, implicit product auto-download is disabled, and only the unique test-owned root is deleted. Product downloader behavior and the user cache are unchanged.
- Upgrade and rollback: the change is test-only, adds no public API, package, schema, migration, service configuration or deployment state. Rollback is ordinary commit reversion and does not require data recovery.
- Operator-visible outcome: current-head receipts report Test 4,847/0/0, MigrationIntegrationTests 746/0/0, Build.Tests 530/0/0 and ValidateTraceability passing. All selected cases have zero failed and zero skipped or not executed.

## Propagation and claim audit

- The current as-built plan has sections 27.46 through 27.48 for the R14-F4 reconstruction, Codex takeover, embedding slice and Linux-only publication scope.
- Both handoffs point to the latest Linux-only state and no longer direct the next agent to run Windows gates.
- `.github/copilot-instructions.md` and `CLAUDE.md` use the existing `EfTodoServiceTests.QueryAsync_FiltersByPriorityAndKeyword` method and Nuke TestGroups for the class example. The stale `TodoServiceTests.QueryAsync_NoFilters_ReturnsAllItems` example is absent from both current files.
- The current task snapshot contains 75 fixed-position tasks, 45 accepted and 30 open. Tasks 73 to 75 cover quarantine evidence, embedding verification and record/HV recovery. The snapshot remains evidence, not an acceptance mutation.
- The Linux case-identity receipt passes with 6,108 baseline executions, 6,123 current executions, zero gone identities, zero non-passed identities and exactly 15 intended additions. The four original real embedding test identities remain one-for-one. Receipt SHA-256: `0D1C4099E8C6097C0A958DA005303A41F15884A4DD9364530FA8CB510DB0252D`.
- The manifest recheck verifies all 14 TRX hashes, byte counts, passing counters and six source pins. No old-head TRX is used as current-head proof.

## Scope decisions retained

- The 68 classification-only tests and 1,915 uncited methods remain outside PR87 under PLAN-TESTCITATIONS-001.
- GitHub review comments are superseded by the operator-selected full Claude HV and are not an active findings queue.
- GitHub Actions, Azure, hostile-environment runs, held-out integration projects and scanner work under the existing override remain outside this publication gate.
- The first current-head Build.Tests attempt failed because its launcher selected a disallowed ambient path under `.mcpServer`; it is retained as an infrastructure-launch failure. The corrected same-head run used an allowed absolute path under TestResults and passed 530/0/0. No code change was made between those attempts.
- The historical Windows lane at `455f3cbd` is not claimed green: its TRX had 722 passing cases, but xUnit cleanup failed and the target exited nonzero. The test-only correction was proven by a red compile gate and targeted Linux green gate; the operator ended further Windows publication work.

## Prelaunch findings

- No unresolved product or test-code defect was found in the current approved scope.
- The five Round 15 records contained launch placeholders when this audit began. They were subsequently replaced with exact current paths, model, effort, head, scope and evidence; the prelaunch evidence audit reports zero unresolved launch-placeholder lines.
- The exact staged records path set received a 53-file, zero-hit secret-shaped text scan after record replacement. A final rescan follows the last record-variable correction and is recorded in the active Codex session turn immediately before commit.
- PR87 remains draft until the complete records commit is pushed and the ready transition is read back.
- Full Claude HV has not started. No AGREE or 98/98 score is claimed.

Audit assessment after record replacement and first staged scan: accuracy 98%, completeness 98%. Records commit and push, PR ready readback and full Claude HV remain execution steps, not evidence already claimed by this audit.
