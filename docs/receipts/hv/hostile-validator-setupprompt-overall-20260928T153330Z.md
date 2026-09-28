# Hostile validation: setup prompt overall rerun

TimestampUtc: 2026-09-28T15:47:10.2934462Z
ValidatorIdentity: Codex. Runner: GrokCode (operator supplied). Host: PAYTON-LEGION2.
Work class: Project documentation draft review. This does not accept MCP-SETUPPROMPT-001 as done.

OverallVerdict: **AGREE**. DraftScopeVerdict: **AGREE**.
Draft: **10 PASS / 0 FAIL / 0 UNKNOWN**. Reviewer-only: **6 PASS / 0 FAIL / 0 UNKNOWN**. Total: **16 PASS / 0 FAIL / 0 UNKNOWN**.
Accuracy: **99/100**. Candidate and literal fence unchanged; all 28 behavioral cases pass; source checks confirm the held fixes. Scores assess draft correctness and are not claims of a live installation or backend model attestation.
Completeness: **99/100**. All requested checks and applicable A-D surfaces evaluated, with live requirements/TODO reads and separated reviewer hygiene. Inventory, live installation, agent UI activation and two Legion dry-runs remain explicitly outside this draft acceptance and open for the parent task.

## Full FAIL and UNKNOWN lists

- Scope=Draft: no FAILs; no UNKNOWNs within this draft review.
- Scope=ReviewerOnly: no FAILs; no UNKNOWNs in this run.
- Prior R01 was a reviewer-only credential leak in a different run. It is not a draft defect and does not force DISAGREE on this clean rerun.

## Candidate, encoding, and literal fence

Candidate: docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md. No edits.
Before and after SHA-256: `5B518FF2CF4AF0D4F74399F2B186486FEEA430801FB181D8EB92FE812718D766`. Exact supplied hash match.
Encoding: 45,855 bytes and codepoints; **0 non-ASCII**; strict UTF-8 decode succeeds; 669 CRLF sequences. ASCII throw fix at line 643 preserved; strict regex at line 642 unchanged.
Literal Verify (Windows) lines 630-662: **Parser.ParseInput 0 errors; ScriptBlock.Create succeeds; 28/28 PASS; 0 FAIL; 0 skipped**.
Fence SHA-256: `E97D35C7C33B8F6FB4263E1EB8C7D36DBA858F37B29A287C3A64E6B25CB74D6C`. Exact prior accepted fence equality: true.
The full extracted script block ran without changes. Mocks were limited to Get-Service, Get-CimInstance, Test-Path, and Get-NetTCPConnection in local scopes; post-test real command resolution was checked. Negative cases had to throw the intended error. A standalone regex result was not substituted for executing the fence.

- canonical: PASS; ACCEPT
- exe-open-only: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls "http://+:7147"
- exe-close-only: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"
- url-open-only: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147
- url-close-only: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls http://+:7147"
- program-files-unquoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: C:\Program Files\McpServer\McpServer.Support.Mcp.exe --urls "http://+:7147"
- exe-old-quoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe.old" --urls "http://+:7147"
- exe-old-unquoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: C:\ProgramData\McpServer\McpServer.Support.Mcp.exe.old --urls "http://+:7147"
- prefix-port-71470: PASS; REJECT; ImagePath --urls mismatch: http://+:71470
- exe-later-arg: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\wrapper.exe" "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"
- duplicate-urls: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147" --urls "http://+:7147"
- trailing-argument: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147" --other
- exe-unquoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls "http://+:7147"
- url-unquoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls http://+:7147
- both-unquoted: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147
- wrong-executable: PASS; REJECT; ImagePath exe mismatch: C:\Other\McpServer.Support.Mcp.exe
- wrong-host: PASS; REJECT; ImagePath --urls mismatch: http://localhost:7147
- embedded-quote: PASS; REJECT; ImagePath must be fully quoted UpdateService form: "exe" --urls "http://+:port"; got: "C:\Program"Data\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"
- program-files-quoted-other-install: PASS; REJECT; ImagePath exe mismatch: C:\Program Files\McpServer\McpServer.Support.Mcp.exe
- system-alias: PASS; ACCEPT
- wrong-account: PASS; REJECT; StartName=NT AUTHORITY\NetworkService not LocalSystem
- stopped: PASS; REJECT; Service status=Stopped
- zero-service-pid: PASS; REJECT; Service ProcessId missing
- wrong-listener-pid: PASS; REJECT; No listener on 7147 owned by PID 24680
- no-listener: PASS; REJECT; No listener on 7147 owned by PID 24680
- missing-exe: PASS; REJECT; exe missing
- missing-manifest: PASS; REJECT; deployment manifest missing
- missing-yaml: PASS; REJECT; appsettings.yaml missing

The quoted Program Files case is rejected because the unchanged example fixes install to ProgramData. This does not claim that quoted Program Files installs are unsupported. The fence is illustrative: Phase 4 additionally requires deployment-manifest contents, legacy JSON absence, resolved DB identity, nonce/trust and per-agent smoke. Those live-installation checks were not claimed as executed here.

## Claims and surfaces A-D

- P1 / P / Scope=ReviewerOnly / PASS: add-profile was the first filesystem/shell action after tool discovery. All 19 dynamically enumerated non-skill profile Markdown files were read in full before validation. Truncated output was re-read in bounded batches. Requested hostile-validator surfaces A-D applied; explicit Codex identity and no-session-log instructions supersede generic skill defaults.
- P2 / P / Scope=ReviewerOnly / PASS: Current runtime line 8 proves gpt-6-astra / xhigh in both turn_context and collaboration settings. The current user prompt exactly matches the supplied prompt file after trimming. Runner host is PAYTON-LEGION2; validator is Codex.
- P3 / P / Scope=ReviewerOnly / PASS: Authored writes are confined to the four named receipt files under docs/receipts/hv. Candidate unchanged. No session-log, TODO, requirements, deployment, commit, push, or done-state writes.
- A1 / A / Scope=Draft / PASS: Candidate SHA-256 equals supplied 5B518FF2CF4AF0D4F74399F2B186486FEEA430801FB181D8EB92FE812718D766 before and after review. Strict UTF-8 succeeds; 45855 bytes/codepoints; zero non-ASCII; 669 CRLFs. Throw line 643 and regex line 642 are unchanged.
- A2 / A / Scope=Draft / PASS: Full draft read. Intake precedes action and covers agents, process, storage, service, host/workspace/plugin roots, endpoint and required follow-ups. Evidence: draft lines 15-19 and 35-76.
- A3 / A / Scope=Draft / PASS: Per-family playbooks and multi-agent composition include canonical identities, host-matched plugin acquisition/activation/smoke, and refusal for unverified branches. Cline runtime/build inspected; host activation proof is still required. Evidence: draft 179-262; Cline package.json/server.json.
- A4 / A / Scope=Draft / PASS: Manifest/plan schemas, paths, post-intake write/load discipline, populated example, and no-session-log setup dependency remain. Bootstrap is correctly described as no-op. Evidence: draft 5,78-175; SessionLogWorkflow.cs:74-86.
- A5 / A / Scope=Draft / PASS: Literal unchanged Windows Verify fence extracted at lines 630-662: Parser.ParseInput 0 errors; ScriptBlock.Create succeeds; 28/28 cases pass, 0 fail, 0 skipped. Linux systemd and publish-swap explicitly remain blocked. Source search for integration markers returned 0 matches, rg exit 1.
- A6 / A / Scope=Draft / PASS: Refuse/caution/redirect codes and bounded stop/remediation remain. SQLite multi-agent guidance distinguishes concurrent-writer topology from an unconditional refusal. Evidence: draft 356-373,427,440-447.
- A7 / A / Scope=Draft / PASS: Strict balanced quoting, exact executable/URL comparisons, LocalSystem account, positive service PID and matching listener PID are retained and behavior-tested. Resolved provider/database identity with override provenance is mandatory for every provider; unavailable evidence blocks success. Evidence: draft 407-428,630-662; resolver 96-114,192-203; Program.cs:100-104.
- A8 / A / Scope=Draft / PASS: F01-F09 held remediations remain source-backed: PackageId vs command name, approved UpdateService/Restore, Configure-before-start, provider aliases, fresh workspace registration, POST install, mandatory PowerShell.MCP, safe dedicated Linux paths. Ten prior source hashes rechecked and unchanged.
- B1 / B / Scope=ReviewerOnly / PASS: Every shell action used PowerShell.MCP execute_command from the first command; PowerShell 7.6.6 and PowerShell.MCP 1.14.0 verified. No tools.exec_command, unmanaged pwsh, Python, or YAML edits.
- B2 / B / Scope=ReviewerOnly / PASS: This run has zero known live credential echoes in runtime public messages/tool outputs. Query-string credentials are sanitized before receipt serialization. All four final files are checked for exact live marker credential and non-placeholder credential query forms. Prior R01 is reviewer-only historical evidence and is not carried forward as a draft failure.
- B3 / B / Scope=ReviewerOnly / PASS: Codex plugin 1.107.0 Invoke-FullBootstrap returned true for signature and nonce before native read-only requirements_effective/todo_get calls. No session lifecycle or storage mutations. Small tool read/parameter/scope errors were corrected; no required check remains unrun.
- C1 / C / Scope=Draft / PASS: Project documentation DRAFT review, not implementation exit. Live effective layer-1: 341 functional, 465 technical, 495 testing, 341 mappings; no SETUPPROMPT/frontier-agent-setup/setup-prompt matches; TODO has no FR/TR links. This receipt does not certify implementation traceability, phase order, or full unit-suite acceptance.
- D1 / D / Scope=Draft / PASS: Live MCP-SETUPPROMPT-001 remains Done=false. Inventory and two Legion intake-variant dry-runs remain open, consistent with draft scope. Checked writing subtasks do not establish whole-plan completion; no TODO or plan completion is claimed.

## F01-F09 held remediations

- F01 PASS: Draft 384-387; McpServer.Repl.Host.csproj:12-13: SharpNinja.McpServer.Repl package and mcpserver-repl command.
- F02 PASS: Draft 322-336,393-404; Build.UpdateService.cs:156-174 restores before register/start; WindowsServiceDeploymentGuard.cs:18-88 enforces yaml, generator and hashes; Update-McpService.ps1 Restore/BackupArchive.
- F03 PASS: Draft 338-348,493-515 blocks systemd and publish-swap; rg sd_notify|UseSystemd|AddSystemd|Type=notify in src/build: zero matches, exit 1.
- F04 PASS: McpDatabaseConfigurationResolver.cs:96-168 confirms provider selection, PostgreSQL/SQL Server aliases, empty-primary SQL Server failure and SQLite default.
- F05 PASS: ToolRegistryController.cs:34/189 confirms GET search and POST install. Codex mcp-status exposes session/queue data. Cline package.json/server.json support dist/index.js activation; unproved host activation stops.
- F06 PASS: Draft manifest: typed host/endpoint/product version, per-agent array and selected playbooks; populated plan; manifest-driven Verify.
- F07 PASS: Draft 20 and 520: ASCII >= 1.14.0 mandatory PowerShell.MCP; no Python; only pre-PowerShell bootstrap exception.
- F08 PASS: Draft 420-424,657: all providers require resolved non-secret DB identity with override provenance or VERIFY_EVIDENCE_UNAVAILABLE. Resolver 96-114/192-203, Program 100-104, StorageConnectivityHealthCheck 45-54 corroborate.
- F09 PASS: Draft 508: dedicated clone directories only; never chown shared /opt; Linux lifecycle remains blocked.

All ten prior source-file SHA-256 values match the current files. Source checks were rerun; the JSON twin stores the hashes.

## Model, reviewer process, and credential hygiene

- add-profile executed first: all 19 non-skill global profile files read in full. Private bodies omitted from public receipts.
- Runtime proof: `C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T10-33-31-01a0e8a6-5bed-7fc0-bf74-5f61d388de0e.jsonl:8`, turn_context UTC `09/28/2026 15:33:35`.
- payload.model = gpt-6-astra; payload.effort = xhigh. collaboration_mode.settings confirms model and reasoning_effort. Prompt-file match = true.
- Thread: 01a0e8a6-5bed-7fc0-bf74-5f61d388de0e. Turn: 01a0e8a6-5d66-7732-aeea-314ecc2c1615. This proves runtime configuration, not independent backend attestation.
- PowerShell 7.6.6; PowerShell.MCP 1.14.0. All shell work used execute_command from the first command; no Python or unmanaged shell.
- Codex plugin 1.107.0 trust bootstrap returned true before read-only effective-requirements and TODO queries. No MCP session-log writes, TODO changes, requirement changes, deployment, commit or push.
- This-run credential hygiene: **PASS**. Exact live marker credential echoes = **0** across 48 scanned tool-output records plus public messages at cutoff 09/28/2026 15:47:09.
- All four persisted files are checked for the live marker value and credential query forms (apiKey, api_key, token, key, password, secret). Live matches = **0**; non-placeholder credential query forms = **0**. Sanitization occurs before persistence; no reliance on runner post-redaction.
- Corrected reviewer tooling issues: output truncation required bounded re-reads; unsupported Show-TextFiles range parameters were replaced with documented Skip/First; transient transcript file sharing used a shared read; local helper scope was reloaded; rune count was corrected to a scalar. These did not alter the draft or leave a required check incomplete.

## Scope and receipt stream

Live TODO Done=false; inventory and two Legion intake-variant dry-runs remain open. No implementation exit, full product suite, actual database identity or agent UI activation is certified. Effective requirements contain no setup-prompt-specific matches and the TODO has no FR/TR links; this review does not supply missing implementation traceability.
Git branch: develop; HEAD: 6a565d8762072ed040469047791a57c80dbf1e88. Existing dirty state preserved. Authored file writes are limited to the four paths below.

- `F:\GitHub\McpServer\docs\receipts\hv\20260928T153330Z-setupprompt-overall.request.jsonl`
- `F:\GitHub\McpServer\docs\receipts\hv\20260928T153330Z-setupprompt-overall.response.jsonl`
- `F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-overall-20260928T153330Z.md`
- `F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-overall-20260928T153330Z.json`

Sanitized public runtime projection through recorded cutoff plus exact evidence and complete verdict. Private reasoning, system/developer instructions, profile/skill/memory bodies and raw marker-output bodies are omitted with explicit records. Not byte-identical raw CLI output; no invented future turn-completion event.

OverallVerdict: **AGREE**. DraftScopeVerdict: **AGREE**. This is draft acceptance only.

Post-write verification: all four artifacts checked; JSON and both JSONL files parse; Markdown and embedded JSON receipt match after line-ending normalization. Zero live credential matches and zero unsafe query values. Final runtime scan: 51 tool-output records, zero live credential echoes. Candidate hash unchanged. Verified at 2026-09-28T15:49:15.7904861Z.