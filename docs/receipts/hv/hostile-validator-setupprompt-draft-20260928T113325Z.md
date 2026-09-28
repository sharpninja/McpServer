# Hostile validation: frontier-agent setup prompt draft

TimestampUtc: 2026-09-28T11:48:43.5558593Z
ValidatorIdentity: Codex
Work class: Project documentation draft review. No TODO completion claim.

OverallVerdict: **DISAGREE**
Checks: **8 PASS / 7 FAIL / 1 UNKNOWN** (16 total). Nine distinct defects.
Accuracy: **70/100**. Completeness: **60/100**. Both fail the required 98 threshold.

Accuracy rationale: Intake, DRAFT status, provider existence, secret discipline and deferred BDPv4 policy are accurate. Wrong package identity, incompatible service examples, storage-key mismatch and shell guidance prevent reliable execution.
Completeness rationale: All major headings exist but several agent branches, provider wiring, typed artifact fields and live comparison procedures are unresolved. Inventory and two intake-variant dry-runs remain open. Those open tasks are not falsely scored as completed implementation.
Score basis: Reviewer estimates of claimed copy-paste suitability, not empirical percentages or confidence that a DRAFT file exists.

## Identity, first action and scope

add-profile executed first; all **19** non-skill global profile Markdown files read in full. Skill: C:\Users\kingd\.codex\skills\add-profile\SKILL.md
Live model **gpt-6-astra**, effort **xhigh**. Proof: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T06-34-56-01a0e7cb-ee33-7d23-989f-682b67a09c52.jsonl:8
Thread: 01a0e7cb-ee33-7d23-989f-682b67a09c52. Turn: 01a0e7cb-ef85-76d2-859c-ee7356e67610.
The 2026-09-28T11:35:02.104Z turn_context records model/effort and matching collaboration settings; the exact opening public message binds that rollout to this review. This proves recorded runtime configuration, not a separate backend attestation.

Candidate: F:\GitHub\McpServer\docs\setup\2026-09-28-frontier-agent-setup-prompt.DRAFT.md
SHA-256: 9AE78BEA67D2A86A1E996522E9C50651140EDD4F00061A95FD461D096C1CE8A2
Git HEAD: 6a565d8762072ed040469047791a57c80dbf1e88. Draft hash stable and tracked git diff empty before receipt writes.
No product/TODO/requirements changes, installation, build, tests or deployment. Only HV receipts authored. Runtime-managed cache/output files may be created by tools.

Session-log scope exception: Latest user instruction explicitly limits writes to HV receipts. This overrides standing skill/profile instructions to create a reviewer MCP turn. No sessionId/turn persistence is claimed; live Codex thread/turn IDs above are not MCP log IDs.

## Full FAIL list

### F01 [high] Wrong NuGet package in both obtain examples

Affected checks: A1, A3, A5, A7
Evidence: draft:345; draft:489; src/McpServer.Repl.Host/McpServer.Repl.Host.csproj:12-13; README.md:79-84
Observation: Both examples install -g mcpserver-repl. The repo defines ToolCommandName=mcpserver-repl and PackageId=SharpNinja.McpServer.Repl. The executable name is not this product package ID.
Impact: A fresh agent cannot obtain the intended REPL from the provided install command.
Required correction: Use the repository-supported InstallReplTool target or the verified SharpNinja.McpServer.Repl package with a compatible .NET/version/source policy.

### F02 [high] Windows service recipe bypasses the approved deployment path

Affected checks: A1, A3, A5, A7, A8
Evidence: draft:195-198; draft:239-248; draft:513-530; draft:536; build/Build.UpdateService.cs:11-24,35-40,158-178; src/McpServer.Support.Mcp/Program.cs:97,1122-1130; src/McpServer.Support.Mcp/Services/WindowsServiceDeploymentGuard.cs:18-44; README.md:170
Observation: The concrete example creates/starts a service using publish/McpServer.exe. The actual binary is McpServer.Support.Mcp.exe. Windows service startup enforces appsettings.yaml plus an approved deployment manifest and executable hashes. sc.exe alone does not produce those. Nuke is only preferred in the draft although repository policy requires it. Install examples also start before Phase 3 storage configuration.
Impact: Even correcting the executable name leaves a service rejected by the deployment guard; configuration/order and rollback are unresolved.
Required correction: Inventory and document the supported elevated ./build.ps1 UpdateService path, real parameters and generated manifest, approved configuration staging before startup, and rollback. Do not offer raw sc.exe as the normal alternative.

### F03 [high] Linux systemd unit is incompatible with the inspected server

Affected checks: A1, A3, A5, A7, A8
Evidence: draft:360-361; draft:367-398; src/McpServer.Support.Mcp/McpServer.Support.Mcp.csproj:1-29; src/McpServer.Support.Mcp/Program.cs:96-112; rg -n Systemd|sd_notify|NOTIFY_SOCKET src --glob *.cs --glob *.csproj: no matches, exit 1
Observation: The unit uses Type=notify without repository notification wiring, ExecStart targets publish/McpServer.dll instead of McpServer.Support.Mcp.dll, and User/Group=mcpserver are used without an account-creation step. There is no exact publish/config-copy recipe. Illustrative/adjust comments do not make this actionable.
Impact: A fresh installation can fail on missing user/binary; even after those edits, notify readiness is unsubstantiated and likely times out. This is a static finding, not an executed Linux failure.
Required correction: Provide a tested repository-specific publish/service recipe with the actual assembly, service account/permissions/config layout, and a service type supported by the application; otherwise explicitly block that branch pending validation.

### F04 [high] Storage playbooks omit actual provider configuration and mislead on SQL Server key

Affected checks: A1, A3, A5, A7, A8
Evidence: draft:174-189; draft:245-247; draft:423-424; draft:554; src/McpServer.Support.Mcp/appsettings.yaml:46-75; src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs:77-87,96-168
Observation: No playbook maps the chosen provider to Mcp:Database:Provider and its provider-specific connection settings. The generic Windows reference env:ConnectionStrings__Mcp is a PostgreSQL fallback; SQL Server resolves ConnectionStrings:McpSqlServer or its explicit SqlServer keys. A connection secret alone does not select the provider, and defaults select SQLite.
Impact: An agent can leave the default provider active or use the wrong connection key while read-only smoke calls succeed.
Required correction: Supply non-secret per-provider mappings, supported configuration precedence, secret binding under the actual service identity, startup migration behavior, and a live provider/database identity check.

### F05 [medium] Several selected agent families have no executable playbook

Affected checks: A1, A3
Evidence: draft:41-42; draft:126-150; AGENTS-README-FIRST.yaml:78-190,203-204,245-260
Observation: Claude Code/Cowork/Cline/Copilot/OpenCode are collapsed into clone the matching repo if present. Cursor/other are intake options without a matching playbook or explicit unsupported disposition. The prompt does not supply exact acquisition sources, activation/hook checks or per-host invoke procedures for those choices, and accepts preferred identities without pinning truthful host identity.
Impact: A fresh frontier agent must invent host integration decisions and cannot consistently verify all selected agents.
Required correction: Inventory supported hosts; document verified acquisition, canonical identity, activation and smoke steps per family, with an explicit stop/redirect for unsupported families.

### F06 [medium] Manifest and plan cannot unambiguously round-trip multi-agent and host choices

Affected checks: A1, A3, A4
Evidence: draft:41; draft:63-70; draft:74-116; draft:122; draft:146-150; draft:448; draft:578
Observation: Intake is multi-select but playbooks.agent is one scalar and instructions require exactly one playbook per axis; agent-multi does not identify each selected family playbook. Host OS is inferred from service kind, which is ambiguous for other, and has no defined typed field. Examples reference a port from the manifest with no port field. Storage/service details are untyped empty objects and the plan has only a prose outline, no worked populated branch/evidence example.
Impact: Later steps cannot reliably load a complete and consistent choice-to-command mapping without adding unstated conventions.
Required correction: Define per-agent playbook IDs plus combination guidance, explicit host/port/version and conditional storage/service fields, and a populated plan showing deterministic statuses, evidence, verification and remediation.

### F07 [high] Prompt violates the requested PowerShell-only operating rule

Affected checks: B1, A1, A5
Evidence: draft:20; draft:232; draft:328-354; draft:392-448; AGENTS.md:21; AGENTS-README-FIRST.yaml:252-266; .github/copilot-instructions.md:15
Observation: PowerShell is merely preferred; bash is explicitly allowed for ordinary apt/systemd/journalctl operations and used throughout Linux examples. PowerShell.MCP routing is absent. The live TODO wording allows shell equivalents, but this review brief explicitly requires PowerShell-only guidance and current workspace rules require it.
Impact: An agent following the prompt violates the active shell/tool contract.
Required correction: Use PowerShell 7 and PowerShell.MCP for normal Windows and Linux automation, including invoking native Linux utilities; retain only the explicitly allowed pre-PowerShell installation exception.

### F08 [high] Verification does not specify how to prove the selected deployment

Affected checks: A1, A7
Evidence: draft:251-275; draft:442-449; draft:571-585; src/McpServer.Repl.Core/SessionLogWorkflow.cs:74-86; src/McpServer.Support.Mcp/Services/StorageConnectivityHealthCheck.cs:9-14
Observation: The prompt says assert workspace/identity/storage/service match, but gives no concrete evidence source/comparison for those assertions, selected build or every agent. Read-only todo/memory success plus Running does not prove the desired DB/provider or a newly installed target; bootstrap is a verified no-op. Listener examples inspect the fixed default port and Linux masks listener failure with || true. Trust verification and actionable rollback/retry detail are missing.
Impact: A wrong provider, stale workspace/endpoint or only one working agent can produce the stated smoke receipts. Completion depends on invented checks.
Required correction: Specify trusted marker/nonce verification and per-selected-agent checks against the exact endpoint, resolved provider/database/workspace and service executable/config; make expected evidence and fail-closed comparisons explicit, with bounded remediation.

### F09 [high] Linux example changes ownership outside the chosen installation

Affected checks: A1, A5
Evidence: draft:339-342
Observation: sudo chown "$USER:$USER" /opt changes ownership of the shared /opt directory, not just the selected McpServer/plugin directories.
Impact: A setup run unnecessarily grants the operator ownership of a global installation directory and affects unrelated software.
Required correction: Create and grant ownership only on the approved dedicated installation directories; do not chown /opt itself.

Score gate failures: accuracy 70 < 98; completeness 60 < 98. Neither can authorize acceptance.

## UNKNOWN list

U01 / A6: SQLITE_MULTI_AGENT is unsubstantiated as a categorical unsupported combination. No multi-agent SQLite runtime stress test was performed. Ordinary multiple agents using one server is not by itself proof of multiple independent database writers. Other explicit OS/provider/secret redirects exist.

## Per-claim results

- **P1 PASS** [Prerequisites] Run add-profile before validation and fully read all non-skill profile Markdown. First tool attempted the mandated skill read; sandbox os error 206 prevented launch. PowerShell MCP then read the skill and all 19 discovered files in full. Truncated reads were repeated in bounded ranges before claim checks.
- **P2 PASS** [Prerequisites] Live turn uses requested model and effort. Own rollout session_meta and matching public messages bind thread 01a0e7cb-ee33-7d23-989f-682b67a09c52 to this review. turn_context at 2026-09-28T11:35:02.104Z records model gpt-6-astra, effort xhigh and matching collaboration settings.
- **P3 PASS** [Prerequisites] Read-only review, receipts only, no product or TODO mutation. Only review receipts authored; MCP calls limited to plugin trust/status and read-only TODO/requirements queries. No install, deployment, build, tests, product edits, TODO state change, or session-log write. Tool-managed caches/output files may be created by the runtime.
- **A1 FAIL** [A] Draft exists and is suitable as copy-paste frontier-agent setup prompt. File exists and is explicitly DRAFT; suitability fails on reproducible source mismatches and gaps.
- **A2 PASS** [A] Mandatory pre-obtain/install intake covers agents, process, storage, service. draft:15-19,35-72,226. All four axes and pre-action ordering are explicit.
- **A3 FAIL** [A] Concrete scenario handling for every intake axis. Some guidance exists, but several supported choices need undisclosed host/configuration/command decisions.
- **A4 FAIL** [A] Session-log exclusion plus usable post-intake artifact schemas. Exclusion, filenames, paths, write-before-install and load rules PASS (draft:5,15-33,74-116). Bootstrap is a current no-op (SessionLogWorkflow.cs:74-86), not evidence of a log write. Combined claim FAILs on ambiguous artifact shapes/multi-agent mapping.
- **A5 FAIL** [A] Windows and Linux examples have comparable actionable depth. Both have obtain/configure/service/verify sections, so structural parity exists. Neither supplies a correct complete fresh-host installation recipe.
- **A6 UNKNOWN** [A] Accurate refuse/redirect guidance for conflicts and unsupported combinations. draft:213-222 provides concrete OS/plugin/provider/secret redirects. No source evidence found for categorical SQLITE_MULTI_AGENT rejection. Multiple agents talking to one server is not itself proof of multiple independent SQLite writers; intended topology/load limits require clarification or evidence. No SQLite stress test executed.
- **A7 FAIL** [A] Clear honest verify/stop criteria and repository-matching commands. Stop headings and criteria exist, but wrong commands and unspecified live comparisons defeat copy-paste verification.
- **A8 FAIL** [A] No invented unsupported storage/service paths or plugin-hardcoded BDPv4. All three DB providers exist (McpDatabaseProviderFactory.cs:11-20). BDPv4 is explicitly recorded/deferred and must not be hard-coded (draft:154-168), which PASSes. Service/config recipes do not match inspected implementation.
- **B1 FAIL** [B] Prompt follows PowerShell-only/no-Python workspace guidance. No-Python text PASSes, but PowerShell-only guidance fails. Review execution used pwsh.exe and no Python.
- **B2 PASS** [B] No secrets in manifest/plan and no private profile publication. draft:23,54-62,92-94,222,267,296,554 prohibits secrets/private profiles; inspected template contains placeholders, not credentials. Receipt stream redacts private profile material and marker credentials.
- **B3 PASS** [B] Host-matched plugin invoke rather than raw REST by default. draft:21,128-144,132,273,427-440,556-568 prefers host plugin; any REST fallback requires explicit operator approval. Do not interpret that as pre-authorizing a bypass of workspace rules.
- **C1 PASS** [C] Discover FR/TR and assess draft versus production-ready requirement status. Live TODO has empty FR/TR links. Effective layer-1 query returned 341 FR, 465 TR, 495 TEST; full FR/TR queries returned 341/465, zero setup-specific matches. Export searches likewise found none. Draft labeling is acceptable; no production-ready or completed implementation acceptance is granted.
- **D1 PASS** [D] Holistic TODO remains open until inventory, refinements and two dry-runs. workflow.todo.get req-20260928T114019Z-d717: done=false; inventory and Legion two-variant dry-run tasks false; remaining explicitly requires concrete install commands. Eight other task checkboxes are draft progress, not independent proof of validated quality. draft:312 requires revision after dry-runs. No TODO update made.

## Requirements and holistic plan evidence

Host-matched Codex wrapper: C:\Users\kingd\.codex\plugins\cache\mcpserver-codex-plugin\mcpserver\1.107.0\Invoke-CodexMcpPlugin.ps1
Plugin marker signature and live health nonce check: True. Status: available, with 14 pre-existing queued failsafe items; no replay/repair attempted.
TODO query: req-20260928T114019Z-d717; done=false; FR links=0; TR links=0.
Open task: Inventory current install surfaces: McpServer repo, NuGet/tooling, UpdateService, plugin repos (grok/codex), local skill mirrors under ~/.grok/skills
Open task: Dry-run on Legion with at least two intake variants; confirm manifest/plan artifacts and verify; revise gaps
Remaining: Draft prompt written at docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md. Next: refine playbooks from inventory, add concrete install commands per storage/service, Legion dry-runs with two intake variants.
Effective requirements: layer-1; 341 FR, 465 TR, 495 TEST. Request req-20260928T114036Z-60e0.
All FR: 341/341, request req-20260928T114127Z-b0fc. All TR: 465/465, request req-20260928T114132Z-60b7.
Search SETUPPROMPT|frontier.agent.setup|setup.choices.manifest|setup.implementation.plan found zero records; local exported-document search also found none. No linked/query-discoverable specific FR/TR found; differently named requirements are not categorically ruled out.
Missing setup-specific requirements do not fail the existence of a labeled DRAFT. They prevent treating it as production-ready acceptance without further requirements/AC/traceability work. Eight checked drafting tasks do not establish validated prompt quality.

## Important negative findings

Session-log exclusion is explicit. The Phase 4 bootstrap call is a no-op in current SessionLogWorkflow.cs:74-86; it is not falsely reported as a session-log mutation.
SQLite, PostgreSQL and SQL Server are genuine implemented providers. BDPv4 is explicitly deferred to policy/binding and not to be hard-coded into plugin code. These facts do not repair the broken service/config examples.

## Validation boundary and receipts

Source inspection and read-only live MCP queries were performed. No fresh installation or Windows/Linux runtime dry-run was executed. Unit/integration/build gates are unrun and are not claimed green.
Initial sandbox os error 206, truncated output reads, a shared-rollout read lock and a transient console-start failure were recovered; they are not product findings.
Request JSONL: F:\GitHub\McpServer\docs\receipts\hv\20260928T113325Z-setupprompt-draft.request.jsonl
Public response JSONL: F:\GitHub\McpServer\docs\receipts\hv\20260928T113325Z-setupprompt-draft.response.jsonl
Structured verdict: F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-draft-20260928T113325Z.json
Public JSONL contains public commentary/tool events with private profile/memory and marker batches redacted, plus the complete verdict and this receipt. Hidden reasoning is excluded. It is a sanitized stream through a recorded cutoff, not a byte-identical private runtime transcript.

Decision: Retain DRAFT and MCP-SETUPPROMPT-001 open. Correct the listed defects, finish inventory and dry-runs, establish requirement/acceptance mappings before any production-ready claim, and obtain a new independent review.

Receipt: F:\GitHub\McpServer\docs\receipts\hv\hostile-validator-setupprompt-draft-20260928T113325Z.md
OverallVerdict: DISAGREE