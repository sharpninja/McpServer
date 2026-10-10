# Hostile validation: setup-prompt draft, literal Windows fence remediation

TimestampUtc: 2026-09-28T13:56:02.2928833Z
ValidatorIdentity: Codex
Work class: Project documentation draft review. MCP-SETUPPROMPT-001 completion is not claimed or authorized.

OverallVerdict: **DISAGREE**
Claim groups: **15 PASS / 1 FAIL / 0 UNKNOWN** (16 total).
Accuracy: **97/100**. Completeness: **99/100**.
Reviewer-only unresolved findings: **0 FAIL / 0 UNKNOWN**.

Accuracy rationale: The intended W05 fix is correct. The exact full Windows fence compiles and passes every required behavioral case. However, the assertion that only the throw-message line changed is false: 35 other lines were re-encoded, worsening existing mojibake, including version and workflow punctuation. This prevents accepting the candidate as the claimed narrowly scoped documentation remediation.
Completeness rationale: All requested cases, prior safeguards, source contracts, live requirement scope, and open plan state were checked. The receipt includes the exact fence, executable harness, per-case traces, source comparison, model proof, and full findings. This is a draft review; live deployment, actual database identity, agent UI activation, and the two intake dry-runs are not claimed complete. Scores are reviewer assessments, not test-pass percentages.

## Full FAIL list

### W06 [medium] The remediation also re-encodes 35 other documentation lines

Affected claim: A1. This is a document/scope defect, not a reviewer-routing issue.

The candidate has SHA-256:
A24439617408225E0FAE6D26541745C0E908F5E26788F467923FC414865EA230

The prior W05 candidate has recorded SHA-256:
759C8DF47850A503D4727864742C9832A4767748D91BA8054745FD00D9C79D30

Replacing only the new throw line with the old line, in memory, produces:
A3C01E367D0BC91ED9015630A78D163D20DEE09F9E1069774C56D9CFCDC9418F
That is not the prior file.

Taking that in-memory text through Windows-1252 encoding followed by UTF-8 decoding reproduces the prior SHA-256 exactly:
759C8DF47850A503D4727864742C9832A4767748D91BA8054745FD00D9C79D30

This establishes a whole-document encoding conversion in addition to the intended line 643 edit. It is not merely terminal rendering: the reconstructed bytes match the prior recorded file hash. Independently, 666 source lines recovered from the prior response receipt show the same differences; the three uncaptured lines are blank or ASCII and do not differ in the hash-verified reconstruction.

Changed lines:
5, 20, 42, 57, 63, 148, 153, 161, 162, 171, 174, 190, 196, 204, 216, 227, 232, 250, 294, 318, 325, 332, 333, 334, 369, 402, 404, 415, 474, 481, 510, 515, 520, 529, 610, 643.

Only line 643 is the intended throw edit. The other 35 are encoding changes. The prior candidate already contained mojibake; this remediation adds another layer. Examples include the PowerShell.MCP version comparator at lines 20 and 520, phase arrows at line 148, the step range at line 404, and punctuation in Phase 4 at line 415. The JSON twin preserves the complete before/after line comparison.

Required document-only remediation: apply the valid throw change without re-encoding unrelated text, and clean the existing damaged punctuation using explicit ASCII or verified UTF-8 text. Preserve the strict regex, re-pin the candidate hash, and rerun the literal fence plus the scope/encoding comparison. No product feature change is requested.

Score-gate failure: accuracy 97 < 98 independently prevents AGREE. It is not counted as a second claim row.
UNKNOWN list: none within the stated draft-review scope.

## W05 and literal verification

W05 is resolved. Draft lines 630-662 were extracted directly from the Verify (Windows) fence and passed unchanged to both:
- System.Management.Automation.Language.Parser.ParseInput: **0 parse errors**.
- System.Management.Automation.ScriptBlock.Create: **success**.

The resulting full script block was executed against in-memory Get-Service, Get-CimInstance, Test-Path, and Get-NetTCPConnection mocks. Join-Path, GetFullPath, regex matching, comparisons, throws, filtering, and the complete control flow were real PowerShell execution. No isolated-regex result is credited as full-fence proof.

Exact extracted fence SHA-256 (UTF-8, preserving its extracted final newline):
E97D35C7C33B8F6FB4263E1EB8C7D36DBA858F37B29A287C3A64E6B25CB74D6C

**28 PASS / 0 FAIL**, comprising 19 ImagePath cases and nine additional service/file/listener cases.

Required ACCEPT:
```text
"C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"
```
Accepted. Its trace reaches Get-Service, Win32_Service, all three file checks, and Get-NetTCPConnection on port 7147 with the matching service PID.

Required REJECT cases all throw for the intended reason:
- Prior malformed-exe-open: executable opening quote without closing quote.
- Prior malformed-exe-close: executable closing quote without opening quote.
- Prior malformed-url-open: URL opening quote without closing quote.
- Prior malformed-url-close: URL closing quote without opening quote.
- Unquoted Program Files executable path with spaces.
- .exe.old (both quoted and unquoted variants).
- URL port 71470.
- Executable passed as a later argument.

Additional rejection coverage: duplicate --urls, trailing argument, only exe unquoted, only URL unquoted, both unquoted, wrong executable, wrong host, and embedded quote. A fully quoted Program Files path is rejected by this literal example because its unchanged install variable is C:\ProgramData\McpServer; this is an expected path mismatch, not a claim that quoted Program Files installations are unsupported. The four original malformed strings and every exception are preserved verbatim in the JSON twin.

Service coverage: LocalSystem baseline and NT AUTHORITY\SYSTEM accepted; wrong account, stopped service, zero service PID, wrong listener PID, missing exe/manifest/YAML, and no listener rejected.

My first harness used a map named state that collided case-insensitively with its mock's State parameter. That harness falsely rejected the two expected positive cases. I corrected only the harness variable, reran all 28 cases, and retained the first run separately for audit. The final harness contains no such collision. This was a corrected reviewer test-fixture issue, not a draft defect. The mock functions were local; normal command resolution was verified afterward.

Phase 4 line 415 retains the same strict balanced-quotes/exact-exe/exact-URL rule as lines 639-650 and build/WindowsServiceHelper.cs:485-488. Its encoding defect is W06; its ImagePath semantics match.

## A-D claim results

- P1 PASS: add-profile was the first shell task. All 19 dynamically enumerated non-skill Markdown profile files were read in full before validation. Truncated reads were completed in bounded reads.
- P2 PASS: live runtime gpt-6-astra / xhigh is verified and bound to the exact supplied prompt.
- P3 PASS: authored writes are confined to this review's receipts under docs/receipts/hv/. No MCP session-log, TODO, requirement, deployment, commit, or done-state mutation.
- A1 FAIL: the assertion that only the throw line changed is disproved by W06; documentation encoding regressed.
- A2 PASS: intake remains ahead of action and covers agents, process, storage, service, host/workspace/plugin roots, and endpoint (draft:15-19,35-76).
- A3 PASS for draft scope: per-family playbooks, blocked/unsupported branches, and host activation evidence requirements remain (draft:179-354).
- A4 PASS: typed manifest, per-agent array, populated plan, write/load order, and no-session-log setup dependency remain (draft:5,78-175). SessionLogWorkflow.cs:74-86 confirms bootstrap is a no-op.
- A5 PASS: Linux remains explicitly blocked, and the exact Windows Verify fence now compiles and executes the required cases.
- A6 PASS: conflict, refusal, caution, redirect, and bounded-stop guidance remain; SQLite multi-agent is topology-dependent caution (draft:356-373).
- A7 PASS: strict executable/URL matching, service identity, LocalSystem and PID/listener join, resolved DB identity, and fail-closed unavailable-evidence criteria remain (draft:407-428,630-662).
- A8 PASS: package/command distinction, provider aliases, approved deployer, and Configure-before-start remain source-backed.
- B1 PASS: PowerShell.MCP was used from the first shell command through every later shell action. pwsh 7.6.6, PowerShell.MCP 1.14.0. No tools.exec_command, unmanaged shell, or Python was invoked.
- B2 PASS: credentials were redacted before marker output and receipt serialization; no private profile bodies or live API-key values are included in authored receipts. No secret-store changes.
- B3 PASS: Codex plugin 1.107.0 Invoke-FullBootstrap returned true before read-only MCP requirement/TODO queries. No raw MCP storage access.
- C1 PASS for draft scope: current effective layer-1 contains 341 FR, 465 TR, 495 TEST, and 341 mappings. No SETUPPROMPT/frontier-agent-setup/setup-prompt matches; the TODO has no FR/TR links. This does not accept implementation traceability or an implementation exit.
- D1 PASS for draft scope: live MCP-SETUPPROMPT-001 remains Done=false. Inventory and at least two Legion intake-variant dry-runs remain open. Checked drafting tasks do not satisfy those exit criteria.

Surface totals: P 3 PASS; A 7 PASS / 1 FAIL; B 3 PASS; C 1 PASS; D 1 PASS. No Byrd phase order was inferred from file timestamps. Full product builds/tests and deployment were not run for this bounded documentation review.

## Prior remediation audit

- F01 held: SharpNinja.McpServer.Repl is the package; mcpserver-repl is the executable. Draft:384-387; src/McpServer.Repl.Host/McpServer.Repl.Host.csproj:12-13.
- F02 held: approved UpdateService, real binary, preserved config before start, deployment guard, and rollback. Draft:322-336,393-404; build/Build.UpdateService.cs:156-174; WindowsServiceDeploymentGuard.cs:18-88.
- F03 held: systemd and linux-publish-swap remain blocked. Draft:64-65,338-348,493-515. Fresh source/build search for sd_notify, UseSystemd, AddSystemd, Type=notify: no matches, rg exit 1.
- F04 held: correct provider-specific keys and SQL Server null-coalescing/empty-primary warning. McpDatabaseConfigurationResolver.cs:96-168.
- F05 held: GET search versus POST bucket install (ToolRegistryController.cs:34,189), explicit trust bootstrap rather than invented Codex Status fields (mcp-status.ps1:84-101), and Cline npm ci/build plus absolute dist/index.js and host UI evidence or stop (draft:231-233; current Cline package.json/server.json inspected).
- F06 held: typed host/endpoint/version fields, per-family playbooks, and populated plan remain.
- F07 held: PowerShell.MCP is mandatory and Python forbidden. The version comparator's encoding damage is separately reported as W06.
- F08 held for draft scope: require resolved provider/database identity and active-override provenance for every provider, including SQLite, or VERIFY_EVIDENCE_UNAVAILABLE (draft:420-424,657). Resolver:96-114,192-203 and Program.cs:100-104 confirm override risk; StorageConnectivityHealthCheck.cs:45-54 proves connectivity only.
- F09 held: no chown of shared /opt; dedicated directory guidance remains (draft:508).
- Fresh workspace registration remains mandatory before startup (draft:164,402,425; WorkspaceService.cs:519-546).
- LocalSystem and PID/listener protections were rerun in the complete literal fence, not merely a fragment.
- Prior W04 routing deviation was not repeated. No new product change is proposed for reviewer-only issues.

## Runtime and scope proof

- Live model: gpt-6-astra. Live effort: xhigh.
- Both payload.model/effort and collaboration_mode.settings.model/reasoning_effort agree.
- Source: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T08-42-03-01a0e840-4df0-7b63-8fd9-97c5eaef4162.jsonl:8.
- turn_context timestamp: 2026-09-28T13:42:06.372Z.
- Thread: 01a0e840-4df0-7b63-8fd9-97c5eaef4162. Turn: 01a0e840-4f74-7080-8125-c881853cbd35.
- CLI: 0.155.0-alpha.16.4. The runtime user prompt matches the supplied .prompt.txt exactly after trimming.
- This proves recorded live configuration, not independent backend attestation.
- Git HEAD: 6a565d8762072ed040469047791a57c80dbf1e88.
- Candidate hash remained A24439617408225E0FAE6D26541745C0E908F5E26788F467923FC414865EA230 throughout review.
- Codex plugin trust bootstrap succeeded at 2026-09-28T13:48:11.9258865Z.
- Existing unrelated untracked work was present at entry. Four tracked docs/benchmarks/results files changed concurrently during review; I issued no writes to them and did not attribute them to this review.
- The explicit no-session-log instruction overrides the skill/profile logging requirement. No sessionId, MCP turn, or persistence claim is made.
- Skills read: C:\Users\kingd\.codex\skills\add-profile\SKILL.md and C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md. The generic per-turn enforcement skill was also read; its session-log mutations were not used because of the explicit override.
- No product or draft fix was authored.

## Durable artifacts and response boundary

- Request: docs/receipts/hv/20260928T134202Z-setupprompt-draft.request.jsonl; supplied prompt: matching .prompt.txt.
- Response: docs/receipts/hv/20260928T134202Z-setupprompt-draft.response.jsonl.
- Markdown: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T134202Z.md.
- JSON twin: docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T134202Z.json.

The response JSONL contains a sanitized public runtime projection through its recorded cutoff, exact verification evidence, and this full verdict. It excludes private reasoning, system/developer instructions, private profile/memory/skill bodies, and credentials. It is not represented as a byte-identical raw CLI stream; future completion events cannot be included before they occur. Runtime-managed transcripts/output caches are not authored repository changes.

OverallVerdict: **DISAGREE**.
