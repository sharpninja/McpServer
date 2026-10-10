# Hostile validation: setup-prompt draft, strict ImagePath remediation

TimestampUtc: 2026-09-28T13:40:43.3856082Z
ValidatorIdentity: Codex
Work class: Project documentation draft review. MCP-SETUPPROMPT-001 completion is neither claimed nor authorized.

OverallVerdict: **DISAGREE**
Checks: **12 PASS / 4 FAIL / 0 UNKNOWN** (16 claim groups).
Distinct findings: one blocking document defect and one reviewer routing deviation.
Accuracy: **96/100**. Completeness: **96/100**. Both fail the required 98 threshold.

Accuracy rationale: The revised regex enforces quotes and the isolated predicate produces all 19 expected outcomes. Phase 4 agrees with the canonical deployer. However, the literal verification code is invalid PowerShell, so the advertised copy-paste behavior is false.
Completeness rationale: Prior intake, provider, plugin, blocked Linux, and fail-closed requirements remain. The required literal ACCEPT/REJECT behavioral run cannot execute because the block cannot compile. Passing extracted fragments cannot close that gap. These are reviewer assessments of the draft, not measured percentages or a production readiness claim.

## Full FAIL list

### W05 [high] Strict ImagePath remediation introduces a parse error

Affected checks: A1, A5, A7.
Evidence: docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md:643.

The exact Windows verify fence at lines 630-662 was extracted without edits. PowerShell 7.6.6 Parser.ParseInput reports one UnexpectedToken at block line 14, column 85 (draft line 643): Unexpected token 'exe followed by backtick and double quote' in expression or statement. ScriptBlock.Create also throws before any command can execute. The exact parser message, source block, and per-case records are in the JSON twin and response JSONL.

The error message combines backtick and backslash quote escaping. PowerShell uses the backtick, not C-style backslash escaping; the resulting string terminates incorrectly. This also contradicts AGENTS-README-FIRST.yaml:262.

Required ACCEPT case:
```text
"C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"
```
Result from the literal block: **cannot execute, parse error**. This is not an accepted case. All 19 full-block case attempts stop at compilation; zero behavioral cases execute. Malformed inputs blocked by a parse error are not counted as successful rejection tests.

Required remediation: fix only the throw-message quoting while preserving the strict regex, then rerun the entire unmodified draft fence against canonical, malformed, and prior regression cases. Do not report the extracted-regex results as proof that the full fence runs.

### W04 [medium] Reviewer initially selected the wrong shell route

Affected check: B1. This is my deviation, not a document defect.

The first tool orchestration discovered PowerShell.MCP but also attempted tools.exec_command with shell pwsh.exe for the add-profile skill read. The sandbox helper failed before the shell command executed (os error 206). I then used PowerShell.MCP for every successful shell command. No escalated retry or successful unmanaged shell was used.

The attempted route did not follow the requested first-command preference and the workspace mandatory routing rule at AGENTS-README-FIRST.yaml:252-261. It is scored conservatively as FAIL rather than claiming flawless routing. No Python was used.

Score-gate failures: accuracy 96 < 98 and completeness 96 < 98. Either independently blocks AGREE; these are not additional claim rows.

## Literal versus isolated evidence

- Literal full fence: 1 parser error; 19 compilation-blocked cases; 0 behavioral cases executed.
- Extracted regex plus exact executable/URL comparisons only: **19 PASS / 0 FAIL**.
- Unchanged service/status/files/listener subfragment with in-memory mocks: **10 PASS / 0 FAIL**.
- No draft patch or hypothetical repaired full block was executed.
- No Windows service, file ACL, installation, database, Linux lifecycle, or agent UI activation was changed.

The isolated predicate ACCEPTS the fully quoted canonical command and its correctly quoted Program Files equivalent. It REJECTS all four prior unbalanced opening/closing quote cases, the unquoted Program Files case, quoted and unquoted .exe.old, port 71470, exe-as-later-arg, duplicate --urls, a trailing argument, unquoted exe only, unquoted URL only, both tokens unquoted, wrong executable, wrong URL host, and an embedded quote.

The regex at line 642 requires both quotes around both captured tokens. Phase 4 at line 415 states the same rule. build/WindowsServiceHelper.cs:485-488 generates the quoted executable and quoted URL. W01's regex defect is corrected in isolation, but W05 prevents accepting the remediation as runnable documentation.

The service subfragment accepts LocalSystem and NT AUTHORITY\SYSTEM and rejects wrong account, stopped state, mismatched listener PID, zero service PID, missing executable/manifest/YAML, and no listener. These results preserve prior LocalSystem/PID protections in isolation, not full-block success.

## A-D claim results

- P1 PASS: add-profile was the first attempted task; all 19 dynamically discovered non-skill profile Markdown files were read in full before claim checks. Truncated reads were completed. Routing is separately scored B1.
- P2 PASS: live model/effort verified as gpt-6-astra / xhigh and bound to the exact request.
- P3 PASS: authored writes limited to this review's receipts; no MCP session-log/TODO/requirements mutation, deployment, commit, or done-state claim.
- A1 FAIL: copy-paste verification suitability is disproved by W05.
- A2 PASS: pre-action intake covers agents, process, storage, service, host/workspace/plugin roots and endpoint (draft:15-19,35-76).
- A3 PASS for draft scope: per-family playbooks and explicit unsupported/blocked branches remain (draft:179-354). No unsupported host activation is claimed verified.
- A4 PASS: typed manifest/plan, multi-agent array, write/load order, and no-session-log dependency remain (draft:5,78-175). SessionLogWorkflow.cs:74-86 confirms bootstrap is an idempotent no-op.
- A5 FAIL: Linux is honestly blocked; Windows literal verify cannot compile (W05).
- A6 PASS: refuse/caution/redirect and bounded stop guidance remains; SQLite multi-agent is a topology-dependent caution (draft:356-373).
- A7 FAIL: DB identity and service criteria are explicit, but the verification example cannot execute (W05).
- A8 PASS: provider branches, actual package/command names, and approved deployment paths remain source-backed.
- B1 FAIL: draft retains mandatory PowerShell.MCP/no Python; reviewer initially attempted the wrong runner (W04).
- B2 PASS: API credential redacted from marker output by exact-value replacement; no credentials or private profile bodies in authored receipts. Manifest secrets remain references/placeholders.
- B3 PASS: host-matched plugin guidance remains. Read-only MCP queries followed a successful Codex plugin trust bootstrap; no raw API storage access.
- C1 PASS for draft scope: live effective layer-1 has 341 FR, 465 TR, 495 TEST and 341 mappings. No SETUPPROMPT/frontier-agent-setup/setup-prompt matches; TODO has no linked FR/TR. This is not acceptance of implementation traceability.
- D1 PASS for draft scope: live MCP-SETUPPROMPT-001 is Done=false. Inventory and at least two Legion intake-variant dry-runs remain open. Checked drafting subtasks do not satisfy those exit criteria.

Surface totals: P 3 PASS; A 5 PASS / 3 FAIL; B 2 PASS / 1 FAIL; C 1 PASS; D 1 PASS. UNKNOWN 0 within this draft-review scope. Unexecuted deployment, actual resolved DB identity, Linux lifecycle, UI activation, and product suites are not claimed passing. Byrd phase order was not inferred from file timestamps.

## Prior remediation audit

- F01 held: correct SharpNinja.McpServer.Repl package versus mcpserver-repl command; draft:384-387 and Repl.Host.csproj:12-13.
- F02 held as deployment guidance: approved UpdateService, real binary, preserved configuration before startup, rollback; draft:322-336,393-404. Build.UpdateService.cs:156-174 restores config, registers, writes manifest, then starts. WindowsServiceDeploymentGuard.cs:18-88 requires YAML, approved generator and executable hashes.
- F03 held: systemd and linux-publish-swap explicitly blocked (draft:64-65,338-348,493-515). Source/build search for sd_notify/UseSystemd/AddSystemd/Type=notify has no matches, exit 1.
- F04 held: provider-specific keys and SQL Server aliases match resolver source, including empty SQL Server primary-key behavior (resolver:148-168).
- F05 held for draft scope: GET search versus POST bucket install is correct (ToolRegistryController.cs:34,189). Codex Status is not claimed to expose trust fields (plugin mcp-status.ps1:84-101). Cline has npm ci/build, absolute dist/index.js, UI evidence or stop (draft:231-233; package/server manifests inspected).
- F06 held: typed host/port/version, per-agent playbooks and populated plan are retained (draft:78-175).
- F07 held in draft: mandatory PowerShell.MCP and no Python (draft:20,520); reviewer deviation separately reported.
- F08 remains blocked by W05 for executable verification; its prior prose remediations are retained. Resolved provider/database identity and active-override provenance are mandatory for every provider, including SQLite, or VERIFY_EVIDENCE_UNAVAILABLE (draft:420-424,657). Resolver:96-114,192-203 and Program.cs:100-104 confirm override risk; StorageConnectivityHealthCheck.cs:45-54 proves connectivity only.
- F09 held: no chown of shared /opt; dedicated-directory guidance remains (draft:508).
- Fresh-workspace guidance remains: RepoRoot and primary/enabled Workspaces entry before startup; plugin environment alone does not register a server workspace (draft:164,402,425; WorkspaceService.cs:519-546).
- LocalSystem, service Running, required files and PID/listener join remain and pass the isolated fragment checks above.

## Model, scope and artifact proof

- Live runtime: gpt-6-astra / xhigh. Both payload.model/effort and collaboration_mode.settings.model/reasoning_effort agree.
- Source: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T08-26-42-01a0e832-41b0-7bd0-900f-371622d503fd.jsonl:8.
- turn_context timestamp: 2026-09-28T13:26:45.437Z.
- Thread: 01a0e832-41b0-7bd0-900f-371622d503fd. Turn: 01a0e832-42a8-7780-8ea5-dc6cfc7673d0. CLI: 0.155.0-alpha.16.4.
- Runtime user message at line 9 matches the supplied .prompt.txt exactly after trimming. This proves recorded live configuration, not independent backend attestation.
- Candidate SHA-256: **759C8DF47850A503D4727864742C9832A4767748D91BA8054745FD00D9C79D30**; unchanged throughout checks.
- Git HEAD: 6a565d8762072ed040469047791a57c80dbf1e88. Tracked diff empty; pre-existing untracked work untouched.
- PowerShell.MCP 1.14.0 / pwsh 7.6.6. Codex plugin 1.107.0 Invoke-FullBootstrap returned true at 2026-09-28T13:32:18.1162368Z.
- User's explicit no-session-log instruction overrides skill/profile logging requirements. No sessionId or MCP turn persistence is claimed.
- Skill applied: C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md; add-profile: C:\Users\kingd\.codex\skills\add-profile\SKILL.md.
- No product fix was authored. Fixing the draft belongs to the implementing agent.

## Durable response boundary

Request: docs/receipts/hv/20260928T132642Z-setupprompt-draft.request.jsonl, with matching .prompt.txt.
Response: docs/receipts/hv/20260928T132642Z-setupprompt-draft.response.jsonl.
Markdown: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T132642Z.md.
JSON twin: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T132642Z.json.

The response contains a sanitized public runtime projection through its recorded cutoff, exact verification evidence, and the full verdict. Private reasoning, system/developer instructions, profile/memory/skill bodies, and credential values are excluded or explicitly withheld. It is not represented as a byte-identical raw CLI stream. Future completion events cannot be included at the time of writing and are not fabricated. Runtime-managed transcripts/output caches are not authored repository changes.

OverallVerdict: **DISAGREE**.
