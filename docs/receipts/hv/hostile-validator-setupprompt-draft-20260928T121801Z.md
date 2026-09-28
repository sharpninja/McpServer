# Hostile validation: remediated frontier-agent setup prompt draft

TimestampUtc: 2026-09-28T12:30:29.3767768Z
ValidatorIdentity: Codex
Work class: Project documentation draft review; not ops-only; not MCP-SETUPPROMPT-001 completion

OverallVerdict: **DISAGREE**
Checks: **10 PASS / 6 FAIL / 0 UNKNOWN** (16 checks; eight distinct defects).
Accuracy: **84/100**. Completeness: **76/100**. Each fails the required 98 threshold.

Accuracy rationale: Core product names, storage key names, deployment guard, intake and truthful draft limits are substantially corrected. Invalid Linux verification, false-positive assertions and the broken SQL alias example still contradict source or execution semantics.

Completeness rationale: The manifest and headings cover the requested axes, but executable lifecycle, account/configuration sequencing, supported-host activation and selected-deployment proof remain insufficient for a fresh frontier agent. Scores assess draft copy-paste suitability, not whether deferred inventory/dry-runs are complete.

Reviewer estimates, not empirical percentages or model confidence. Both scores below 98 independently fail the gate.

## Identity, first action and scope

add-profile executed first; all **19** non-skill global profile Markdown files read in full. The first pwsh launch failed before executing due to sandbox helper os error 206. PowerShell.MCP recovered the read. Truncated previews were reread in bounded batches.

Live model **gpt-6-astra**, effort **xhigh**. Proof: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T07-18-03-01a0e7f3-64bb-78a3-b50e-9e6affb8caf3.jsonl:8
Thread: 01a0e7f3-64bb-78a3-b50e-9e6affb8caf3. Turn: 01a0e7f3-65ff-7fe0-8e14-5a953e32168d.
turn_context timestamp: 09/28/2026 12:18:05. Model/effort and collaboration settings agree. Exact user prompt matches the launch prompt and public opening message binds this rollout to the review. This proves recorded runtime configuration, not separate backend attestation.

Candidate: F:\GitHub\McpServer\docs\setup\2026-09-28-frontier-agent-setup-prompt.DRAFT.md
SHA-256: 265C6A0D884689C65720750B3F457F533F2F2899265018913653CCFCB459B757
Git HEAD: 6a565d8762072ed040469047791a57c80dbf1e88. Candidate hash stable; tracked git diff empty before receipt creation. Existing untracked work preserved.

Session-log scope exception: User explicitly limits writes to HV receipts. No reviewer MCP session-log turn was created or persisted. This overrides standing skill/profile session-log writes for this run; Codex runtime IDs are not MCP session IDs.

## Full FAIL list

### R01 [high] Linux verification uses a Windows networking cmdlet

Affected checks: A1, A5, A7
Evidence: draft:570-580; NetTCPIP module: C:\WINDOWS\system32\WindowsPowerShell\v1.0\Modules\NetTCPIP\NetTCPIP.psd1:6-23

Observation: The Linux block calls Get-NetTCPConnection, a Windows NetTCPIP/CIM cmdlet unavailable on a normal Ubuntu PowerShell installation. Both OS examples additionally use the Pester Should assertion without declaring Pester as a prerequisite.

Impact: The advertised Linux verify block is not runnable on the stated fresh Linux host. The examples also assume an undeclared assertion module.

Required correction: Supply a Linux-native listener check invoked through PowerShell, test exact output and exit code, and use built-in throwing assertions or explicitly provision the assertion dependency. No Linux execution was performed here.

### R02 [high] Service and fallback listener checks accept no-match results

Affected checks: A1, A5, A7
Evidence: draft:704-705; draft:577; PowerShell 7.6.6 read-only reproductions: StoppedServicePipelineSuccess=true; NoMatchingListenerPipelineSuccess=true

Observation: Where-Object Status -eq Running followed by Out-Null can return no objects with $? still true. Select-String with no matching listener likewise succeeds as a pipeline. The subsequent if (-not $?) therefore does not prove Running or a matching listener.

Impact: A stopped service or missing selected-port listener can pass these asserted checks. This directly contradicts fail-closed verification.

Required correction: Capture and assert the selected service state and the count/identity of exact listener matches, separately checking native command exit status. Include negative cases.

### R03 [high] SQL Server sample shadows the advertised secret aliases

Affected checks: A1, A5, A8
Evidence: draft:670-680; draft:276-283; src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs:148-161,192-204; src/McpServer.Support.Mcp/Program.cs:100-104; src/McpServer.Support.Mcp/appsettings.yaml:54-69

Observation: The YAML example sets Mcp:Database:SqlServer:ConnectionString to an empty string while proposing env:MCP_SQLSERVER_CONNECTION_STRING or env:ConnectionStrings__McpSqlServer. The resolver uses null coalescing, not nonblank fallback, and returns the primary empty value before those aliases, then throws. GetEffectiveDatabaseValue returns the root value unchanged. Environment overlay only replaces the same configuration key.

Impact: Following this explicit sample plus either proposed alias can fail startup despite supplying a valid SQL Server secret. Correct alias names alone did not repair source-aligned storage wiring.

Required correction: Bind the selected primary key through Mcp__Database__SqlServer__ConnectionString, or document removing all higher-precedence empty keys before using a supported fallback. Verify precedence without exposing the secret. Source proof plus an in-memory empty-string coalescing reproduction were used; no database was connected.

### R04 [high] Linux publish-swap branch lacks an executable host lifecycle

Affected checks: A1, A3, A5
Evidence: draft:318-326; draft:482-568; draft:520-539; src/McpServer.Support.Mcp/Program.cs:86-104; README.md:170-171

Observation: The appendix publishes directly into a publish directory but supplies no concrete swap/backup/rollback procedure, process start/stop/log commands, service-user creation, data ACL application, or manifest endpoint binding. The core only says dotnet <installPath>/McpServer.Support.Mcp.dll under a created account, while the appendix puts the DLL and YAML in <installPath>/publish and leaves cwd at <installPath>. Linux does not receive the Windows-service cwd correction in Program.cs; config loading is relative to the content root. The mentioned /etc/mcpserver/mcpserver.env is not loaded by the documented console path.

Impact: A fresh frontier agent must invent the actual runnable deployment and secret/config environment. Config can be missed when launching the appendix DLL from its retained cwd. Merely blocking unvalidated systemd does not make the replacement branch actionable.

Required correction: Provide a consistent source/staging/runtime layout and concrete PowerShell commands for account creation, permissions, config/environment binding, selected-port startup, stop/logs, backup/swap and rollback, or explicitly block this branch pending that inventory and validation.

### R05 [high] Windows run-as and configuration order remain unresolved

Affected checks: A1, A3, A5
Evidence: draft:63; draft:124-130; draft:162-167; draft:303-310; draft:366-378; draft:640-680; build/Build.UpdateService.cs:156-174; build/WindowsServiceHelper.cs:108-134; scripts/Update-McpService.ps1:43-58

Observation: Intake offers LocalSystem, network service and custom run-as choices, but the manifest has no defined run-as field and the installation commands have no credential/account selection. EnsureServiceRegistration creates or updates ImagePath/start mode only. UpdateService restores existing appsettings.yaml and starts immediately, while the plan sketch binds secrets in the later Configure step and says stage into repo/service without choosing the authoritative file for fresh versus existing installs.

Impact: Non-default run-as selections cannot be executed or verified from the prompt. Integrated SQL permissions or secret environment can be wrong at initial startup, and preserved installed config can overwrite a staged repo change.

Required correction: Define the run-as fields and a verified account/secret provisioning sequence before UpdateService starts, or refuse unsupported account choices. Distinguish fresh install from upgrade and specify the exact config location preserved by deployment.

### R06 [medium] Supported agent branches still omit acquisition and activation procedures

Affected checks: A1, A3
Evidence: draft:183-239; draft:356-362; draft:493-495; draft:611-614; AGENTS-README-FIRST.yaml:78-190,194-204

Observation: Copilot has only plugin name, identity and an unavailable code, with no activation or invoke procedure. Cline names server.json but omits its build/launch and smoke invocation. Grok activation checks merely root existence and trust availability. Tool registry acquisition is preferred rather than specified with exact selection/install/commandTemplate handling, and both clone examples retain unspecified remote placeholders. Explicit stops for Cursor/Other and unverified Cowork/OpenCode are improvements but do not finish the supported families.

Impact: A fresh agent still needs undisclosed integration decisions for advertised supported choices; directory existence does not establish host activation.

Required correction: Inventory and provide host-specific acquisition, required environment, activation/hooks and read-only smoke commands for each supported family, or explicitly stop those branches until their procedure is verified.

### R07 [high] Verification still lacks binding to the selected live deployment

Affected checks: A1, A7
Evidence: draft:385-404; draft:570-580; draft:697-710; src/McpServer.ServiceDefaults/Extensions.cs:180-235; src/McpServer.Support.Mcp/Services/StorageConnectivityHealthCheck.cs:45-54; src/McpServer.Support.Mcp/Program.cs:100-104

Observation: Provider verification remains read from live config or health/details as available, with no defined resolved configuration or database-identity evidence source. The health payload reports status/version/checks/nonce/storage; storage is a CanConnect result, not provider/database identity. Workspace/agent truth is inferred from environment plus successful calls. No exact check binds listener PID to service ImagePath, config, selected baseUrl, database name, or pinned product.version. File existence plus a reachable stale endpoint can satisfy the concrete examples.

Impact: The prompt does not show how to prove the selected DB, workspace, service executable/config and version, or fail when that evidence is unavailable. Stale or wrong deployments remain indistinguishable.

Required correction: Specify concrete evidence and comparisons for process/service identity, endpoint/workspace routing, resolved provider plus non-secret database identity, effective config provenance, product version and each host integration. Treat unavailable proof as a blocked verification, not merely a mismatch check.

### R08 [medium] PowerShell.MCP is optional despite the workspace routing requirement

Affected checks: A1, B1
Evidence: draft:20; draft:592; AGENTS-README-FIRST.yaml:252-261

Observation: The draft requires PowerShell but says prefer PowerShell.MCP when available. The rendered workspace contract requires PowerShell.MCP 1.14.0 or newer for every ordinary pwsh invocation, including installing/updating it when absent and avoiding ad hoc processes.

Impact: An agent can follow the prompt literally while violating the mandatory command runner contract. The prior bash guidance is repaired; runner compliance remains incomplete.

Required correction: State the mandatory PowerShell.MCP route and its bootstrap/block behavior, preserving only the explicitly allowed pre-PowerShell installation exception.

Score gate failures: **accuracy 84 < 98; completeness 76 < 98**. Neither score authorizes acceptance.

## Per-claim results

- **P1 PASS** [Prerequisites] Execute add-profile first and read every non-skill profile Markdown in full. First action attempted the mandated skill read; shell helper os error 206 blocked it. PowerShell.MCP then read the skill and all 19 enumerated files in full. Oversized previews were repeated in bounded batches before validation.

- **P2 PASS** [Prerequisites] Verify live model and effort. Own rollout line 8 records gpt-6-astra/xhigh and matching collaboration settings. Exact user prompt matches the launch prompt; initial public commentary matches this review. Runtime configuration proof, not separate backend attestation.

- **P3 PASS** [Prerequisites] Read-only product review; write HV receipts only; no TODO mutation. Product candidate hash unchanged. Tracked git diff empty. Commands inspected source and queried MCP through the Codex wrapper; no product edits, installs, service changes, builds or tests. Tool-managed output/cache files may be created. No session-log or TODO write.

- **A1 FAIL** [A] Draft exists and is suitable as the copy-paste setup prompt. DRAFT exists, but R01-R08 defeat claimed copy-paste suitability.

- **A2 PASS** [A] Mandatory intake precedes obtain/install and covers all requested axes. draft:15-19,35-76: agents, process, provider, service, explicit host OS, workspace, endpoint and version intake precede artifacts and actions.

- **A3 FAIL** [A] Concrete scenario handling for every supported intake axis. R04-R06: incomplete Linux lifecycle, Windows run-as/configuration sequence, and supported-agent integration procedures. Explicit unsupported-family stops are credited.

- **A4 PASS** [A] No setup session log; artifacts round-trip multi-agent and typed host selections. draft:5,15-33,78-173,179,241-246: per-agent entries and playbook IDs, combo flag, host/endpoint/version fields, status/evidence arrays, plan statuses and filenames are explicit. Current SessionLogWorkflow.cs:74-86 is a no-op. Conditional run-as procedure shortcomings are scored under A3/R05 rather than denying the corrected multi-agent schema.

- **A5 FAIL** [A] Windows and Linux examples are comparably actionable and source-aligned. Both have parallel headings, but R01-R05 show invalid verification, broken SQL alias recipe and incomplete deployment/configuration paths.

- **A6 PASS** [A] Unsupported combinations have honest refuse/redirect/caution guidance. draft:57,64,211-239,293-348: SQLite multi-agent is now a caution; systemd is unvalidated and blocked as primary; OS/plugin/provider/secret conflicts are explicit. No SQLite stress conclusion is inferred.

- **A7 FAIL** [A] Verification is fail-closed and proves the selected endpoint/provider/database/workspace/service. Trust/nonce and stop rules are explicit, but R01-R02 and R07 show broken negative checks and unspecified selected-deployment evidence.

- **A8 FAIL** [A] Storage/service recipes match product, correct package, no unsupported notify or hard-coded BDPv4. Package, executable, deployment guard, provider names/alias names, dedicated chown and BDPv4 deferral are corrected; no sd_notify source matches. R03 still makes the concrete SQL Server configuration/alias recipe incompatible with resolver precedence.

- **B1 FAIL** [B] Prompt satisfies PowerShell-only and mandatory PowerShell.MCP guidance. PowerShell-only/no-Python guidance now passes. R08 remains: required runner is only preferred. Reviewer used pwsh.exe 7.6.6 after the first failed shell-launch attempt; no Python.

- **B2 PASS** [B] Manifest/plan secret discipline and no private profile publication. draft:23,55-56,109,347,405,543,677-680: secret references/placeholders only. Public response redacts private profile/memory/tool batches and marker credentials; no private profiles are copied into receipts.

- **B3 PASS** [B] Host-matched plugin invocation rather than raw REST by default. draft:21,185-239,391-394,557-568,682-695. Explicit operator approval for a possible REST fallback is not treated as current authorization. Review uses the Codex plugin wrapper and its marker trust helper.

- **C1 PASS** [C] Discover applicable FR/TR and distinguish draft from production-ready acceptance. Live TODO has zero FR/TR links. Effective layer-1 returns 341 FR, 465 TR, 495 TEST with zero setup-specific matches; full FR 341/341 and TR 465/465 searches also zero. Export search zero. No linked/query-discoverable setup requirements found; differently worded records are not categorically excluded. Missing requirements do not invalidate DRAFT existence but production-ready acceptance would be premature.

- **D1 PASS** [D] Holistic TODO remains open; inventory and two intake-variant dry-runs remain outstanding. workflow.todo.get req-20260928T122255Z-be34: done=false; inventory and Legion two-variant dry-run false. Remaining text calls for refinement and concrete install commands. Draft:454 explicitly requires dry-run revision. Checked drafting tasks establish progress, not validated setup or overall completion.

UNKNOWN list: none. All mandatory surfaces were evaluated within the read-only documentation-review boundary. Unrun deployment scenarios are explicitly outside the executed validation, not claimed PASS.

## Prior remediation re-validation

- Prior F01 resolved: correct SharpNinja.McpServer.Repl package and InstallReplTool.
- Prior F02 partially resolved: correct executable/approved deployer/guard; Windows account/config sequencing remains R05.
- Prior F03 partially resolved: Type=notify removed and primary systemd branch blocked; replacement Linux lifecycle and verification remain R01/R04.
- Prior F04 partially resolved: provider keys and SQL Server alias names corrected; empty primary value shadows suggested aliases, R03.
- Prior F05 partially resolved: per-family IDs and explicit unsupported stops added; supported-family procedures remain incomplete, R06.
- Prior F06 substantially resolved for the stated multi-agent/typed-host schema; conditional service-account handling remains R05.
- Prior F07 partially resolved: ordinary bash removed and pwsh required; mandatory PowerShell.MCP route remains optional, R08.
- Prior F08 partially resolved: trust/nonce and comparisons are named; executable negative checks and live identity proofs remain R02/R07.
- Prior F09 resolved: chown is limited to dedicated directories.
- Prior U01 resolved: SQLITE_MULTI_AGENT is caution with topology clarification, not categorical unsupported status.

## Requirements and plan receipts

Host-matched wrapper: C:\Users\kingd\.codex\plugins\cache\mcpserver-codex-plugin\mcpserver\1.107.0\Invoke-CodexMcpPlugin.ps1
Marker signature and live health nonce verified True by the plugin marker-resolver Invoke-FullBootstrap helper. Status available; 14 pre-existing failsafe items were observed and not drained or repaired. Status alone is cache inspection, not independent nonce proof.

TODO query: req-20260928T122255Z-be34; done=false; linked FR=0, TR=0. Inventory and Legion two-variant dry-run remain false. Eight checked drafting subtasks are progress only. No TODO mutation was made.

Effective query req-20260928T122306Z-9fce: layer-1, 341 FR / 465 TR / 495 TEST. All-FR req-20260928T122638Z-f0f8: 341/341. All-TR req-20260928T122641Z-5fc7: 465/465. Searches of full records and exported requirement files for SETUPPROMPT, frontier-agent setup and both artifact names returned zero matching setup-specific records. Differently worded records are not categorically ruled out.

A labeled DRAFT is acceptable without claiming completed requirements. Production-ready acceptance would be premature without requirements/AC/traceability and the remaining validation. This review does not fail draft existence merely because those tasks remain open.

## Executed negative cases and validation boundary

Read-only PowerShell 7.6.6 reproductions, with synthetic values and no service mutations:

```powershell
[pscustomobject]@{Status='Stopped'} | Where-Object Status -eq 'Running' | Out-Null
$?  # True: no Running object, yet the draft check accepts it
'LISTEN 0 128 127.0.0.1:9999' | Select-String ':7147' | Out-Null
$?  # True: no matching listener, yet the draft fallback accepts it
('' ?? 'valid-external-secret-binding').Length  # 0: empty primary prevents fallback
```

Observed: StoppedServicePipelineSuccess=true; NoMatchingListenerPipelineSuccess=true; EmptyPrimarySqlValueFallsBack=false; ResolvedSqlLength=0. `Should` happens to exist in this reviewer console, which does not establish availability on a fresh setup host. `Get-NetTCPConnection` resolves to the local Windows NetTCPIP module. No Linux runtime check was run.

Source search `rg -n 'sd_notify|UseSystemd|AddSystemd|NOTIFY_SOCKET|Type=notify' src --glob '*.cs' --glob '*.csproj'` returned no matches, exit 1. Source checks confirm package identity, approved service deployers, guard hash checks, resolver precedence, health payload shape and current bootstrap no-op.

Source inspection, in-memory PowerShell negative cases and read-only trusted Codex-plugin queries. No install, service change, product edit, TODO/requirements mutation, full build/test suite or Windows/Linux setup dry-run. Tool-managed caches/output may exist. No Python.

Recovered review-tool errors (oversized reads, invalid read parameter, several absent search paths) are not product findings. No success is inferred from an attempted command.

## Durable artifacts

- Request JSONL: F:\GitHub\McpServer\docs\receipts\hv\20260928T121801Z-setupprompt-draft.request.jsonl
- Public response JSONL: F:\GitHub\McpServer\docs\receipts\hv\20260928T121801Z-setupprompt-draft.response.jsonl
- Structured verdict: F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-draft-20260928T121801Z.json

Sanitized public commentary and tool events through recorded cutoff plus full structured verdict and receipt; private profile/memory and marker batches redacted, hidden reasoning excluded. Not a byte-identical private runtime transcript.

Decision: Retain DRAFT and open TODO. Remediate R01-R08, finish inventory and two variants, establish requirements/AC traceability before production-ready acceptance, and rerun independent review.

Receipt: F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-draft-20260928T121801Z.md
OverallVerdict: DISAGREE