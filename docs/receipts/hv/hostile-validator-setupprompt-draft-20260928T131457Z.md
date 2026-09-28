# Hostile validation: setup-prompt draft, W01/W02 remediation

TimestampUtc: 2026-09-28T13:24:27Z  
ValidatorIdentity: Codex  
Work class: Project documentation draft review. MCP-SETUPPROMPT-001 completion is not claimed or authorized.

OverallVerdict: **DISAGREE**  
Checks: **11 PASS / 5 FAIL / 0 UNKNOWN** (16 checks, same claim groups as the prior review).  
Distinct findings: one remaining document defect and two reviewer execution failures.  
Accuracy: **96/100**. Completeness: **96/100**. Both fail the required 98 threshold.

Accuracy rationale: W02 and the named W01 counterexamples are corrected. However, the replacement regex still accepts ImagePath strings that are not the canonical UpdateService command and does not reliably identify a quoted executable token. Completeness rationale: intake, schemas, scenarios and prior remediation are retained, but the verifier omits quote pairing and safe handling of executable paths containing spaces. These scores assess the reviewed draft, not the completion of deferred inventory or deployment dry-runs. They are reviewer assessments, not measured percentages of passing test cases. Reviewer failures are listed separately below and independently prevent an all-surfaces AGREE.

## First action and live model proof

- add-profile was the first attempted action. The first exec launch failed before executing with sandbox-helper `os error 206`. The retry read `C:\Users\kingd\.codex\skills\add-profile\SKILL.md`; all **19** dynamically enumerated non-skill Markdown files under `C:\Users\kingd\.claude\profile` were read in full before validation. Truncated portions were reread. The load was confirmed at 2026-09-28T13:16:34Z. Private profile bodies are excluded from this receipt and the public response projection.
- **Recorded live model: gpt-6-astra. Recorded live effort: xhigh.** Source: `C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T08-14-58-01a0e827-8274-78e0-b48e-9ba1f6e49e70.jsonl:8`, turn_context timestamp `2026-09-28T13:15:01.31Z`. Both payload.model/effort and collaboration_mode.settings.model/reasoning_effort agree. This proves recorded runtime configuration, not independent backend attestation.
- Thread: `01a0e827-8274-78e0-b48e-9ba1f6e49e70`; turn: `01a0e827-838d-7171-ae94-ed284303f218`; CLI: `0.155.0-alpha.16.4`. The session's user request matches the supplied prompt file exactly after trimming (one match).
- Candidate: `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md`; SHA-256 **360168DA87E60863C443E646FEAD77E65425A2174F3BBE31245E888E32A76F82**. The hash stayed unchanged during the review. Git HEAD: `6a565d8762072ed040469047791a57c80dbf1e88`; tracked diff empty. Existing unrelated untracked files were left alone.
- Shell verification ultimately used PowerShell.MCP **1.14.0**, pwsh **7.6.6**. Initial direct exec calls violated the required routing; see W04. No Python was used.
- No MCP session-log APIs were invoked; no TODO/requirement mutation occurred. The user's explicit prohibition overrides the older skill/profile logging rules. Codex runtime metadata is not an MCP session log. Intentional authored writes are restricted to these receipts. Runtime-managed transcript/output files are not authored product changes.
- Read-only Codex plugin `lib/marker-resolver.ps1` / `Invoke-FullBootstrap -StartDir F:\github\McpServer` returned **true** at `2026-09-28T13:19:37.6693573Z`. Read-only TODO/effective-requirements queries then used installed MCP tools. No session-start wrapper, queued-log replay, install, service change, deployment, commit or product test suite was run.

## Full FAIL list

### W01 [high] Canonical ImagePath parser still accepts malformed or ambiguous quoting

Affected checks: A1, A5, A7. Prior V04 remains partial.

Evidence: draft:415,639-645; `build/WindowsServiceHelper.cs:485-488`. The deployer generates a quoted executable followed by `--urls` and a quoted URL. The verifier instead makes each opening and closing quote independently optional; its executable capture also accepts whitespace in the unquoted branch.

The literal draft Windows verification block (lines 630-655) parsed with **0 errors** and ran against in-memory mocks. Sixteen cases produced **11 expected outcomes / 5 unexpected acceptances**. No Windows service was created or changed. Mocks supplied Running, LocalSystem, PID 12345, existing required files, and a matching port listener unless a test deliberately changed one of those values.

The explicitly requested regression cases now pass:

- Correct quoted command: accepted.
- `.exe.old`, both quoted and unquoted: rejected.
- `--urls http://+:71470`: rejected by complete value comparison.
- Wrong first executable with the expected executable in a later `--target` argument: rejected.
- Duplicate `--urls`: rejected.
- Wrong account, stopped service, and wrong listener PID: rejected.
- Correct quoted executable under `C:\Program Files\McpServer`: accepted.

The following noncanonical strings all incorrectly passed the literal block:

```text
"C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147
C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls http://+:7147
"C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147
"C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls http://+:7147"
C:\Program Files\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147
```

The last case used the corresponding manifest install path `C:\Program Files\McpServer`. A service binary path containing spaces requires quotation for correct interpretation; accepting this unquoted string does not prove the executable identity. See Microsoft's [CreateServiceW lpBinaryPathName contract](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-createservicew). The quote-pair cases also contradict the draft's own canonical-command requirement. These are verifier counterexamples, not claims about the live installed service.

Required remediation: make quoting an atomic choice (fully quoted or a whitespace-free unquoted token), require balanced quotes, and reject trailing/duplicate arguments. Preserve exact executable and URL equality plus StartName/PID checks. Alternatively compare the complete approved canonical command using narrowly defined normalization that cannot remove required quoting. Add these cases to the verification evidence before the next review.

### W03 [high] Reviewer credential-redaction failure

Affected check: B2. This is my failure, not a document defect.

I masked the marker's camel-case `apiKey` field and query parameter, but missed an inline `X-Api-Key` header example containing the same live credential. That value appeared in tool output. I disclosed this and switched to exact-value redaction. The value is excluded from the Markdown receipt and public response JSONL. The public stream labels redactions; it does not claim the prior output exposure was undone.

The credential needs rotation through an authorized service operation. I did not rotate it, restart the service, alter the marker, or delete runtime-managed output because this review permits receipt-only writes.

### W04 [medium] Initial shell commands bypassed mandatory PowerShell.MCP routing

Affected check: B1. This is my failure, not a document defect.

`AGENTS-README-FIRST.yaml:252-261` requires PowerShell.MCP for every ordinary pwsh invocation. I initially used `functions.exec` / `tools.exec_command` with `shell: pwsh.exe`, including an escalated retry after the sandbox helper launch failure. The first successful launch also loaded the PowerShell profile, which printed startup-helper output. After discovering the PowerShell.MCP tool, I switched to it for the remaining shell work. Later compliance does not erase the earlier bypass. The sandbox failure did not justify skipping discovery of the available required tool.

Score-gate failures: **accuracy 96 < 98** and **completeness 96 < 98**. Either independently prevents AGREE. These are not additional rows in the 16-check count.

## W02 disposition and prior regression checks

- **W02 RESOLVED for DRAFT scope.** Draft:420-424 now requires resolved provider and non-secret database identity with active-override provenance for every provider, explicitly including SQLite. It forbids success based on raw `Sqlite:DataSource` YAML and mandates `VERIFY_EVIDENCE_UNAVAILABLE` if proof is absent. Draft:652 repeats the rule. Source confirms why: `src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs:96-114` prioritizes SQLite ConnectionString over DataSource; lines 192-203 apply instance overrides; `Program.cs:100-104` reapplies environment and CLI after YAML. `Services/StorageConnectivityHealthCheck.cs:45-54` proves connectivity only. No live database identity or probe availability is claimed by this review.
- **V01 held:** systemd and linux-publish-swap remain blocked at draft:64-65,338-348,493-515. `rg` over source/build found no sd_notify/UseSystemd/AddSystemd/Type=notify hits (exit 1). No Linux lifecycle is represented as validated.
- **V02 held:** GET search and POST bucket install remain distinct at draft:193,203,214,230,241. `ToolRegistryController.cs:34,189` confirms the verbs.
- **V03 held:** draft:204-205 does not invent trust fields from Codex Status. The actual status object in plugin `lib/mcp-status.ps1:84-101` carries session/queue metadata. Draft:231-233 retains Cline `npm ci`, `npm run build`, absolute `dist/index.js`, and host connection/tool evidence or stop. The Cline package, lockfile, bundled dependency and server.json were reread. No Cline host activation was performed.
- **V04 partial:** exact equality and the previously named counterexamples are repaired; canonical parsing remains defective under W01. StartName and listener ownership protections were preserved and tested.
- **V05 held by W02 remediation:** resolved identity and override provenance now apply uniformly, with fail-closed evidence-unavailable behavior.
- **V06 held:** draft:163-169,330-333,393-404 still places installed/staged config and service secrets before UpdateService starts. `build/Build.UpdateService.cs:156-174` restores config, registers, writes manifest, then starts and checks health.
- **V07 held for fresh bootstrap:** draft:164,402,425 retains RepoRoot plus enabled primary Workspaces mapping and explicitly says plugin env does not register a workspace. `src/McpServer.Services/Services/WorkspaceService.cs:519-546` reads these keys. This does not prove a live registered host or supersede existing database registrations.

## A-D check results

- P1 PASS: add-profile first, 19 full profile reads. Execution-route failure is separately scored B1.
- P2 PASS: requested live model/effort verified from current turn_context and bound to the request.
- P3 PASS: intentional authored changes limited to receipts; no MCP session-log/TODO/requirement mutation. No done-state claim.
- A1 FAIL: copy-paste verification suitability remains disproved by W01.
- A2 PASS: required intake covers agents, process, storage, service, host/workspace/plugin roots and endpoint (draft:15-19,35-76).
- A3 PASS: scenario branches and explicit blocked/unsupported paths remain (draft:179-354).
- A4 PASS: choices/plan schemas and no-session-log dependency remain (draft:5,78-175). Bootstrap's no-op is confirmed at `src/McpServer.Repl.Core/SessionLogWorkflow.cs:74-86`.
- A5 FAIL: Windows verifier still fails canonical parsing (W01); Linux is honestly blocked.
- A6 PASS: conflict/refusal/caution/redirect handling retained (draft:356-373).
- A7 FAIL: resolved database identity guidance now passes, but exact service-command verification still fails (W01).
- A8 PASS: provider branches, package/command names and approved deployment paths remain source-backed. `src/McpServer.Repl.Host/McpServer.Repl.Host.csproj:12-13` confirms package/command distinction. `WindowsServiceDeploymentGuard.cs:18-88` confirms YAML, approved generators and hashes.
- B1 FAIL: draft correctly mandates PowerShell.MCP/no Python; reviewer initially bypassed PowerShell.MCP (W04).
- B2 FAIL: draft secret guidance is retained; reviewer exposed a credential in tool output (W03).
- B3 PASS: host-matched plugin guidance remains, and read-only MCP queries followed successful plugin trust verification.
- C1 PASS for draft scope: effective layer `layer-1` returned **341 FR, 465 TR, 495 TEST, 341 mappings**. No SETUPPROMPT/frontier-agent-setup/setup-prompt matches in those objects or exported docs. The TODO has no linked FR/TR. Differently worded requirements are not categorically excluded. This is a draft review, not an implementation/requirements completion gate; production acceptance without traceability would be premature.
- D1 PASS for draft scope: live `MCP-SETUPPROMPT-001` has **Done=false**, with inventory and two Legion intake-variant dry-runs still open. Checked drafting subtasks do not complete the TODO. No unrelated plan completion was evaluated or asserted.

Surface totals: A **5 PASS / 3 FAIL**; B **1 PASS / 2 FAIL**; C **1 PASS / 0 FAIL**; D **1 PASS / 0 FAIL**; prerequisites P **3 PASS / 0 FAIL**. UNKNOWN: **0** within this documentation scope. Deployment, Linux lifecycle, actual DB identity, agent UI activation and product suites were not executed and are not claimed passing. Byrd phase-order was not inferred from file timestamps.

## Durable response and audit boundary

Request: `docs/receipts/hv/20260928T131457Z-setupprompt-draft.request.jsonl`; exact prompt: matching `.prompt.txt`.

Response: `docs/receipts/hv/20260928T131457Z-setupprompt-draft.response.jsonl`.

Markdown: `docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T131457Z.md`.

The response is a public runtime projection plus structured evidence and this entire verdict. It retains public reviewer messages/tool events through its recorded cutoff with explicit redactions. It excludes private reasoning, system/developer instructions, operator profile/memory bodies and credential values. It is not represented as a byte-identical CLI stream. It cannot contain its own future completion events; none are fabricated. No MCP session-log receipt exists because the user explicitly prohibited that write.

OverallVerdict: **DISAGREE**.
