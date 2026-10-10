# Hostile validation: setup-prompt draft, V01-V07 remediation

TimestampUtc: 2026-09-28T13:10:50Z  
ValidatorIdentity: Codex  
Work class: Project documentation draft review. No claim that MCP-SETUPPROMPT-001 is done.

OverallVerdict: **DISAGREE**  
Checks: **12 PASS / 4 FAIL / 0 UNKNOWN** (16 checks).  
Distinct findings: two document defects and one reviewer failure.  
Accuracy: **94/100**. Completeness: **92/100**. Both fail the required 98 threshold.

Accuracy rationale: The Linux blocks, registry verbs, Cline build/activation instructions, Codex Status correction, pre-start configuration and fresh-workspace YAML mapping now match the requested remediation. Two consequential verification claims still contradict executable checks or resolver precedence. Completeness rationale: the scenarios and schemas are substantially covered, but exact service identity and resolved database identity cannot reliably be established from the supplied verification procedure. Scores assess this draft's claimed usability, not completion of deferred inventory or deployment dry-runs. They are reviewer assessments, not measured test percentages. The reviewer credential exposure is a separate execution failure.

## First action, live model and scope

- add-profile was the first attempted action. The initial pwsh launch failed before execution with sandbox-helper os error 206. PowerShell.MCP then read the Codex add-profile skill and all **19** dynamically enumerated non-skill profile Markdown files in full. Truncated outputs were reread before claim checking. Profile load completed at 2026-09-28T12:58:43Z. No private profile bodies are reproduced here.
- **Live runtime: gpt-6-astra / xhigh.** Source: `C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T07-56-54-01a0e816-f897-7a91-803f-c3102d8adce2.jsonl:8`. The turn_context at `2026-09-28T12:56:57.431Z` records both payload.model/effort and matching collaboration settings. Line 9 matches the launch prompt exactly after trimming.
- Thread: `01a0e816-f897-7a91-803f-c3102d8adce2`; turn: `01a0e816-f9a3-7c83-8741-0ec08f674d26`; CLI: `0.155.0-alpha.16.4`. This proves recorded runtime configuration, not independent backend attestation.
- Candidate: `docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md`, SHA-256 `12669C1558CBE80530A4B13DFEB285114C610DE9614D2D6E566AB3AF9494F225`. Hash remained stable during review. Git HEAD: `6a565d8762072ed040469047791a57c80dbf1e88`; tracked diff empty.
- Executed shell work used **PowerShell.MCP 1.14.0 / pwsh 7.6.6**. No Python, product edits, service changes, installs, deployment or product test suite. Synthetic checks ran in memory only.
- No MCP session logs were opened, read or written. No TODO/requirement mutations. The explicit user prohibition overrides the older logging skill/profile instructions. Codex runtime metadata is not an MCP session log.
- The read-only Codex `lib/marker-resolver.ps1` helper `Invoke-FullBootstrap` returned true. Read-only requirements/TODO discovery used the installed MCP plugin tools. The standalone invoke wrapper was not run: inspection showed cache writes and possible queued session-log replay. Tool-managed runtime/output files are distinct from authored receipts.

## Full FAIL list

### W01 [high] Exact ImagePath verification still accepts incorrect commands

Affected checks: A1, A5, A7. Prior V04 remains partial.

Evidence: draft:639-643; `build/WindowsServiceHelper.cs:485-488`. The executable regex only finds the expected quoted path anywhere in PathName. The unquoted URL alternative has no trailing token boundary. Neither performs the token equality promised at draft:415.

In-memory reproduction executed the literal draft verification block (lines 630-654) with synthetic service/listener/filesystem responses. The block parsed with **0 errors**. All three cases passed:

- Correct executable with `--urls "http://+:7147"`: PASS, expected.
- Correct executable with `--urls http://+:71470`: PASS, incorrect URL token accepted.
- First executable `C:\Other\wrong.exe`, with `--target "C:\ProgramData\McpServer\McpServer.Support.Mcp.exe" --urls "http://+:7147"`: PASS, wrong executable accepted.

Mock services were Running as LocalSystem with PID 12345; the mocked selected-port listener belonged to PID 12345. This is a controlled counterexample to the comparison logic, not a claim that these deployments exist. StartName and PID ownership additions are credited; they do not repair PathName parsing.

Correction: compare the first executable token and complete URL argument, or the entire approved canonical command. Reject executable-in-argument, suffix, prefix-port and duplicate/overriding argument cases. Retain StartName and listener PID checks.

### W02 [high] SQLite identity still comes from unproven YAML values

Affected checks: A1, A7. Prior V05 remains partial.

Evidence: draft:306,420-424,651; `src/McpServer.Support.Mcp/Program.cs:100-104`; `Options/McpDatabaseConfigurationResolver.cs:96-114,192-203` under the same project.

The draft correctly says YAML is necessary but insufficient and blocks unavailable network database identity. However, line 424 still prescribes SQLite identity as the DataSource path from YAML. A nonblank `Sqlite:ConnectionString` wins over `Sqlite:DataSource` even within the same file. Environment-specific YAML, environment variables, command-line arguments and instance settings can also change the effective provider or path. Merely checking provider environment overrides cannot establish all of these facts.

Source-derived counterexample: base YAML contains DataSource `C:\expected.db` and ConnectionString `Data Source=C:\actual.db`. Reading DataSource can match the manifest while the resolver selects the other database. No database was opened for this review. Storage health only calls CanConnect (`Services/StorageConnectivityHealthCheck.cs:45-54`); it cannot repair identity proof.

Correction: require resolved provider and non-secret database identity, with provenance for all active overrides, for every provider including SQLite. If that proof is unavailable, emit `VERIFY_EVIDENCE_UNAVAILABLE`. Remove the remaining YAML-only identity shortcut.

### W03 [high] Reviewer exposed the marker credential in tool output

Affected check: B2. This is my execution failure, not a draft defect.

My marker-read redaction used an `api.key` pattern that missed camel-case `apiKey`, exposing that field in tool output. I disclosed the mistake and switched to exact-value redaction. The value is excluded from the Markdown, structured verdict and public response JSONL. Private profile/memory outputs are also redacted from that public stream.

The exposed credential should be rotated through an authorized service operation. I did not rotate it, restart the service or edit the marker because this run permits receipt-only writes. No claim is made that redacting the receipt removes the earlier exposure.

Score-gate failures: accuracy **94 < 98**; completeness **92 < 98**. Either independently prevents AGREE.

## V01-V07 disposition

- V01 RESOLVED for DRAFT scope: both Linux lifecycle playbooks explicitly blocked; appendix provides obtain/stop/redirect guidance (draft:64-65,338-348,491-515). No runnable Linux lifecycle is claimed or tested.
- V02 RESOLVED: GET search and POST install now distinguished (draft:193,203,214,230,241; `ToolRegistryController.cs:34,189-206`). No registry mutation performed.
- V03 RESOLVED for DRAFT scope: real trust helper instead of invented Status fields; Cline npm ci/build, absolute dist/index.js, host connection/tool proof or stop (draft:204-205,231-233,425-426). Cline package.json build/main, lockfile, bundled dependency and server.json confirmed. Host activation itself was not performed.
- V04 PARTIAL: StartName and PID ownership repaired; exact PathName comparison still fails W01.
- V05 PARTIAL: base-YAML caveat and network identity stop repaired; SQLite/configuration provenance still fails W02.
- V06 RESOLVED: populated sketch stages config/secrets before UpdateService (draft:163-169,330-333,393-404; `Build.UpdateService.cs:156-174`).
- V07 RESOLVED for requested fresh bootstrap mapping: RepoRoot plus primary enabled Workspaces in server YAML, plugin env explicitly insufficient, marker-evidence stop (draft:164,402,425; `WorkspaceService.cs:519-546`). This is documentation review, not proof of a registered live deployment. Existing database registrations take precedence in source and remain an inventory/dry-run consideration.

## A-D claim results

- P1 PASS: add-profile first; 19 full profile reads.
- P2 PASS: live model/effort and exact request binding.
- P3 PASS: intentional writes restricted to receipts; no session-log or TODO mutation.
- A1 FAIL: copy-paste verification suitability disproved by W01/W02.
- A2 PASS: mandatory intake covers all axes (draft:15-19,35-76).
- A3 PASS: supported scenario handling and explicit blocked paths (draft:179-354).
- A4 PASS: manifest/plan schemas and session-log exclusion (draft:5,78-175). Bootstrap no-op verified at `SessionLogWorkflow.cs:74-86`.
- A5 FAIL: Linux depth honestly restricted; Windows verifier remains incorrect (W01).
- A6 PASS: conflicts, refusals, cautions and redirects (draft:356-373).
- A7 FAIL: exact service and effective database identity remain unreliable (W01/W02).
- A8 PASS: package/executable/provider names and approved deployment paths match source; no invented Linux lifecycle or BDP plugin implementation.
- B1 PASS: mandatory PowerShell.MCP/no-Python guidance and executed shell route.
- B2 FAIL: draft secret guidance passes; reviewer credential exposure fails W03.
- B3 PASS: host-matched plugin guidance; review used plugin trust and read-only MCP tools.
- C1 PASS: current effective layer `layer-1` returned **341 FR, 465 TR, 495 TEST, 341 mappings**. No matches for SETUPPROMPT/frontier-agent-setup/setup-prompt variants; TODO has no FR/TR links. Exported docs search also had no matches. Differently worded requirements are not categorically excluded. DRAFT is allowed; production/completion acceptance would be premature.
- D1 PASS: live `MCP-SETUPPROMPT-001` has `Done=false`. Inventory and two Legion intake-variant dry-runs remain open. Checked drafting subtasks do not complete the TODO.

UNKNOWN list: none within the reviewed documentation scope. Deployments, actual DB identity, host UI activation and product suites were not executed and are not represented as passing.

## Durable response

Request: `docs/receipts/hv/20260928T125653Z-setupprompt-draft.request.jsonl`.

Response: `docs/receipts/hv/20260928T125653Z-setupprompt-draft.response.jsonl`. Public runtime projection includes reviewer messages/tool events through the recorded cutoff, explicit privacy redactions, structured evidence and the entire verdict. Private reasoning, system/developer instructions and operator profile bodies are excluded. The receipt cannot contain its own future tool completion or platform turn-completed event; no such event is fabricated.

Markdown receipt: `docs/receipts/hv/hostile-validator-setupprompt-draft-20260928T125653Z.md`.

OverallVerdict: **DISAGREE**.
