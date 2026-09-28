# Hostile validation: setup-prompt draft ASCII and throw remediation

TimestampUtc: 2026-09-28T14:13:56.7362791Z
ValidatorIdentity: Codex
Work class: Project documentation draft review. This does not accept MCP-SETUPPROMPT-001 as done.

OverallVerdict: **DISAGREE**
All claim groups: **15 PASS / 1 FAIL / 0 UNKNOWN** (16 total).
DraftScopeVerdict: **AGREE**, **10 PASS / 0 FAIL / 0 UNKNOWN**.
ReviewerOnly: **5 PASS / 1 FAIL / 0 UNKNOWN**.
Draft accuracy: **99/100**. Draft completeness: **99/100**.
Reviewer process compliance: **90/100**, because the reviewer exposed a live marker credential in one tool output. This score is separate from draft quality; it is not a proposed product requirement.

Accuracy rationale: The candidate matches the supplied SHA-256. Replacing exactly the 35 ASCII punctuation-cleanup lines and the throw line with their recorded prior values reconstructs the complete prior file SHA-256. The literal, unchanged Verify (Windows) fence compiles and passes all 28 fresh behavioral cases, including every required reject. Source checks support the held remediations. The 99 scores are bounded reviewer assessments, not test-pass percentages or proof of a live installation.

Completeness rationale: Every requested remediation and surfaces A-D were evaluated. Current effective requirements and the open TODO were queried read-only after plugin trust bootstrap. The full fence, harness, cases, traces, source hashes, encoding comparison, model proof, and full verdict are in the JSON twin. Live installation, actual database identity, agent UI activation, inventory completion, and two Legion intake dry-runs are outside this draft acceptance and remain unclaimed.

## Full FAIL list

### R01 [high, reviewer-only, B2] Credential redaction missed a query-string value

At 2026-09-28T14:04:38Z, reviewer tool call `call_jHkYXTOvWMWTv672hy7FenUL` printed the marker's authentication section. The initial filter redacted the top-level key and header form but missed the query-string form, exposing the live credential in tool output. The user explicitly required all live marker credentials to be redacted. This is a reviewer violation, not a defect in the draft, and is the sole FAIL.

I acknowledged the error during the review. I changed the in-memory sanitizer to replace the exact live value before any generic pattern processing and to remove the marker signature value from authored artifacts. The response projection omits raw marker/profile output and all three result files are scanned for the known credential before delivery. Sanitizing later files does not erase the earlier exposure. I did not rotate credentials, restart services, delete runtime logs, change the draft, or invent a product change to repair this reviewer issue. Credential containment or rotation remains outside this receipt-only authorization.

R01 prevents an unqualified OverallVerdict=AGREE even though the draft accuracy and completeness both exceed 98. No TODO/requirement/plan done-state change is authorized.

Draft FAIL list: none. UNKNOWN list: none within the stated draft-review scope.

## Encoding and scope evidence: W06 resolved

Candidate: `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md`
Candidate SHA-256, before and after validation:
`5B518FF2CF4AF0D4F74399F2B186486FEEA430801FB181D8EB92FE812718D766`

The full candidate is 45,855 bytes, 669 CRLF line endings, and zero non-ASCII codepoints. Strict UTF-8 decoding succeeds. No new mojibake layer remains.

The previous receipt's 36 Before line values were substituted into the current candidate in memory, preserving all other bytes and line endings. The result hashes to:
`759C8DF47850A503D4727864742C9832A4767748D91BA8054745FD00D9C79D30`
This exactly matches the recorded pre-W05 candidate. This is whole-file evidence that the current differences are limited to the 35 punctuation cleanups plus line 643; it does not merely infer encoding from terminal rendering or claim independent knowledge of the author's chronological commands.

Changed lines relative to that hash-verified baseline:
5, 20, 42, 57, 63, 148, 153, 161, 162, 171, 174, 190, 196, 204, 216, 227, 232, 250, 294, 318, 325, 332, 333, 334, 369, 402, 404, 415, 474, 481, 510, 515, 520, 529, 610, 643.

All 35 punctuation substitutions were read in context. The explicit ASCII forms preserve intent: `>=` for the PowerShell.MCP minimum version (20,520), `->` for step/evidence transitions, `!=` for mismatch (369), `--` for separators, `2-3` for the step range (404), ASCII apostrophe in the operator reference (474), and `PID-listener` (515). Line 643 preserves the parse-safe formatted throw text. The strict regex on line 642 is unchanged. The JSON twin includes every before/after line.

## Literal Windows fence: W05 remains resolved

Draft lines 630-662 were extracted from the Verify (Windows) fenced block without rewriting any code.

- Parser.ParseInput: **0 errors**.
- ScriptBlock.Create: **success**.
- Exact fence SHA-256: `E97D35C7C33B8F6FB4263E1EB8C7D36DBA858F37B29A287C3A64E6B25CB74D6C`.
- Fence is byte-for-byte equal to the previously accepted W05 fence.
- Fresh full-fence behavioral execution: **28 PASS / 0 FAIL / 0 skipped**.

The harness mocks only Get-Service, Get-CimInstance, Test-Path, and Get-NetTCPConnection in a local scope. The actual complete script block executes its own comparisons, regex, path normalization, throws, file gating, and PID filtering. Negative cases must throw the expected diagnostic; unrelated fixture exceptions do not count as passes. Normal command resolution was checked after the mocks ended. No service was installed or changed.

Accepted: the quoted canonical `C:\ProgramData\McpServer\McpServer.Support.Mcp.exe` ImagePath with quoted `http://+:7147`, and the NT AUTHORITY\SYSTEM account alias.

Rejected for the intended reason: executable opening quote only, executable closing quote only, URL opening quote only, URL closing quote only, unquoted Program Files path, quoted and unquoted `.exe.old`, port 71470, executable as a later argument, duplicate --urls, trailing argument, unquoted exe, unquoted URL, both unquoted, wrong exe, wrong host, and embedded quote. A quoted Program Files path is correctly rejected by this unchanged example because its install variable is fixed to ProgramData, not because quoted Program Files installs are unsupported.

Additional rejected cases: NetworkService account, stopped service, zero service PID, wrong listener PID, no listener, missing exe, missing deployment manifest, and missing appsettings.yaml.

The fence is a partial illustrative verifier. Phase 4 still requires additional deployment-manifest content, legacy-config, resolved database identity, trust/nonce, and agent-smoke evidence. This review does not represent the illustrative fence as executing those commented/prose obligations.

## Per-claim results across A-D

- P1 PASS (reviewer): add-profile was the first filesystem/shell action, after tool discovery needed to find PowerShell.MCP. All 19 dynamically enumerated non-skill profile Markdown files were read fully before validation; truncated reads were completed in bounded batches. Skills read: the Codex add-profile and requested Grok hostile-validator SKILL.md. The generic enforcement skill was read; its logging mutations were suppressed by the user's explicit override.
- P2 PASS (reviewer): live model/effort is gpt-6-astra/xhigh, and the runtime prompt matches the supplied prompt file exactly after trimming.
- P3 PASS (reviewer): authored writes are confined to receipts under docs/receipts/hv/. No draft/product edits, MCP session-log writes, TODO writes, requirement writes, deployment, commit, or done-state mutation.
- A1 PASS (draft): candidate hash and encoding scope verified; W06 resolved; throw fix and strict regex preserved.
- A2 PASS (draft): intake precedes action and covers agents, process, storage, service, host/workspace/plugin roots, endpoint, and follow-ups (draft 15-19,35-76).
- A3 PASS (draft): per-family playbooks, blocked/unsupported branches, and host activation evidence remain (179-262). No claim of live host UI activation is made here.
- A4 PASS (draft): typed manifest, per-agent array, populated plan, write/load order, and no-session-log setup dependency remain (5,78-175). SessionLogWorkflow.cs:74-86 confirms bootstrap is a no-op.
- A5 PASS (draft): Linux systemd and publish-swap remain explicitly blocked; the literal Windows Verify fence compiles and passes all 28 cases (338-348,493-515,630-662).
- A6 PASS (draft): refusal/caution/redirect codes and bounded stop/remediation remain (356-373,427,440-447). SQLite multi-agent is a caution dependent on topology, not an unconditional hard refusal.
- A7 PASS (draft): strict ImagePath, LocalSystem, PID/listener join, resolved DB identity/provenance, and VERIFY_EVIDENCE_UNAVAILABLE remain (407-428,630-662).
- A8 PASS (draft): correct package/command distinction, approved deployer/rollback, Configure-before-start, provider aliases, fresh workspace registration, POST installation, and mandatory PowerShell.MCP remain source-backed.
- B1 PASS (reviewer): every shell action used PowerShell.MCP execute_command, including the first command. pwsh 7.6.6; PowerShell.MCP 1.14.0. No tools.exec_command, unmanaged shell, or Python.
- B2 FAIL (reviewer): R01 above. Corrected artifact redaction does not retroactively satisfy the tool-output hygiene rule.
- B3 PASS (reviewer): Codex plugin 1.107.0 Invoke-FullBootstrap returned true before native read-only MCP requirements_effective and todo_get. No raw MCP storage calls or session lifecycle calls. The generic REPL wrapper was inspected but not invoked because its cache/failsafe behavior was unnecessary for these native read-only queries.
- C1 PASS for draft scope: live effective layer-1 has 341 FR, 465 TR, 495 TEST, and 341 mappings. No SETUPPROMPT/frontier-agent-setup/setup-prompt matches; the TODO has no FR/TR links. This draft review does not certify implementation traceability or a Byrd implementation exit.
- D1 PASS for draft scope: live MCP-SETUPPROMPT-001 remains Done=false; inventory and two Legion intake-variant dry-runs remain open. The draft's checked writing subtasks do not prove those exit criteria. No phase order was inferred from file timestamps.

Surface totals: P 3 PASS; A 8 PASS; B 2 PASS / 1 FAIL; C 1 PASS; D 1 PASS.

## Held remediation source audit

- F01: draft 384-387; src/McpServer.Repl.Host/McpServer.Repl.Host.csproj:12-13 distinguishes SharpNinja.McpServer.Repl from mcpserver-repl.
- F02: draft 322-336,393-404; build/Build.UpdateService.cs:156-174 restores config before registration/start; WindowsServiceDeploymentGuard.cs:18-88 enforces YAML, approved generator, and executable hashes; Update-McpService.ps1 exposes Restore/BackupArchive.
- F03: blocked Linux remains; fresh `rg -n 'sd_notify|UseSystemd|AddSystemd|Type=notify' src build` returns no matches, exit 1.
- F04: McpDatabaseConfigurationResolver.cs:96-168 confirms provider keys, SQL Server fallback names, empty-primary failure, and SQLite default.
- F05: ToolRegistryController.cs:34,189 confirms GET search and POST bucket install. Codex mcp-status.ps1:84-101 exposes session/queue metadata, not invented trust fields. Cline package.json:6-15 and server.json:9-18 confirm build/runtime targets. Host activation proof or stop remains required.
- F06: typed host/endpoint/version, per-agent selection, and populated plan remain.
- F07: draft 20,520 now uses explicit ASCII >= and still mandates PowerShell.MCP; Python is prohibited.
- F08: draft 420-424,657 requires resolved provider/database identity and override provenance for every provider. Resolver:96-114,192-203 and Program.cs:100-104 support override risk. StorageConnectivityHealthCheck.cs:45-54 proves connectivity only.
- F09: draft 508 forbids chown of shared /opt and requires dedicated directories.
- WorkspaceService.cs:519-546 supports the fresh-host registration requirement at draft 164,402,425.
- WindowsServiceHelper.cs:485-488 emits exactly the quoted exe/URL form tested. EnsureServiceRegistration:124-130 sets binPath/start mode without provisioning alternate accounts.

## Model/effort and scope proof

- Runtime: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T09-01-21-01a0e851-f91f-73d2-9f9f-edcc288eb619.jsonl:8.
- turn_context UTC: 2026-09-28T14:01:25.155Z.
- payload.model=gpt-6-astra; payload.effort=xhigh.
- collaboration_mode.settings.model=gpt-6-astra; reasoning_effort=xhigh.
- Thread: 01a0e851-f91f-73d2-9f9f-edcc288eb619. Turn: 01a0e851-fab0-7a91-a8bd-b0cddf2d5437.
- Runtime user prompt equals 20260928T140120Z-setupprompt-draft.prompt.txt after trimming.
- This is live runtime configuration proof, not independent backend attestation.
- Git HEAD: 6a565d8762072ed040469047791a57c80dbf1e88. Candidate hash remained unchanged. Existing dirty state was preserved.
- Explicit no-session-log override honored. No MCP sessionId, turn persistence, or TODO completion is claimed.

## Receipt files and stream boundary

- Request: docs/receipts/hv/20260928T140120Z-setupprompt-draft.request.jsonl, enriched with the complete supplied prompt and runtime linkage.
- Response: docs/receipts/hv/20260928T140120Z-setupprompt-draft.response.jsonl.
- Markdown: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T140120Z.md.
- JSON twin: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T140120Z.json.

The response JSONL is a sanitized public runtime projection through its recorded cutoff plus exact verification evidence and the complete verdict. It omits private reasoning, system/developer instructions, private profile/memory/skill bodies, and raw marker outputs. It records those omissions explicitly. It is not a byte-identical raw CLI stream and does not fabricate future turn-completion events. Runtime-managed transcripts/output caches are not authored repository changes.

OverallVerdict: **DISAGREE**. DraftScopeVerdict: **AGREE**. Sole failure: reviewer-only R01.
