# Hostile validator: setup prompt post-dry-run review

TimestampUtc: 2026-09-28T16:24:35.7684267Z
Validator: Codex; model gpt-6-astra; effort xhigh. Runner: GrokCode on PAYTON-LEGION2.
Work class: project documentation draft review. MCP-SETUPPROMPT-001 remains open.

OverallVerdict: **AGREE**
DraftScopeVerdict: **AGREE**
Accuracy: **99%**. All scoped draft claims are supported by current source, literal fence execution, and current TODO/requirements evidence. This is confidence in draft accuracy, not a claim of a successful live installation.
Completeness: **99%**. All requested A-D surfaces, F01-F09, G1-G4, D1-D3, parser, encoding, ImagePath, provider provenance, and host-specific evidence gates are covered. Blocked and operator-owned paths remain explicit, and inventory/live dry-runs remain open.

- Draft: PASS 26; FAIL 0; UNKNOWN 0.
- ReviewerOnly: PASS 7; FAIL 0; UNKNOWN 0.
- Combined: PASS 33; FAIL 0; UNKNOWN 0.
- Full FAIL list: none (Scope=Draft: none; Scope=ReviewerOnly: none).
- UNKNOWN list: none in the scoped review.

Candidate SHA-256 before and after: `9933BAB3B92198E90BA23588CE9F459E191684D0BAB3140F1C0A8C10448910C0`. No draft edits.
Literal Verify fence: Parser.ParseInput 0 errors; ScriptBlock.Create succeeded; behavioral cases 28/28 PASS, 0 failed, 0 skipped. The entire fence is unchanged, including strict regex and repaired throw.
Encoding: 50,655 bytes / characters; 0 non-ASCII bytes or codepoints; no UTF-8 BOM.
Credential hygiene: PASS. No live marker credential echoed in this run; query credential candidates are redacted placeholders. Final artifact scan is appended below.

## Scope and evidence boundaries

- No product build or full unit suite was run for this documentation review; no implementation phase exit is claimed.
- Known lab unquoted ImagePath drift is explicitly out of DraftScope and was left unchanged.
- No live install, UpdateService, service restart, hook install, database mutation or end-to-end agent smoke was performed.
- No dedicated setup-prompt FR/TR/TEST linkage was found. Formal completion and the two live intake variants remain unclaimed.
- Profile: add-profile executed first; all 19 non-skill profile markdown files read in full.
- Model proof: C:\Users\kingd\.codex\sessions\2026\09\28\rollout-2026-09-28T11-12-09-01a0e8c9-ba8a-71f1-bed5-2eb9ffbd3def.jsonl; turn_context 2026-09-28T16:12:12.894Z reports gpt-6-astra / xhigh in both direct and collaboration fields. Runner command independently agrees.
- Read-only MCP calls: todo_get and requirements_effective. Session log writes: 0. TODO writes: 0. Commits/pushes: 0.
- Effective Layer 1 query: 341 FR, 465 TR, 495 TEST, 341 mappings; no setup-prompt-specific match. This draft review does not authorize a later implementation exit without the applicable requirements and validation gates.
- Response JSONL boundary: Reviewer writes a redacted public assistant/tool event snapshot and full verdict before returning. Runner lines 62-70 are configured to replace response JSONL with full redacted Codex --json stdout after exit. Future runner replacement is not yet observed; no hidden reasoning is included in the reviewer snapshot.

## Per-claim findings: surfaces A-D

- **A01 PASS** (Scope=Draft; Surface=A): Candidate identity is current and unchanged. Get-FileHash SHA256 equals 9933BAB3B92198E90BA23588CE9F459E191684D0BAB3140F1C0A8C10448910C0 before review and pre-receipt. No draft edits.
- **A02 PASS** (Scope=Draft; Surface=A): Literal Windows Verify fence compiles. Extracted live draft lines 658-690; Parser.ParseInput returned 0 errors; ScriptBlock.Create succeeded.
- **A03 PASS** (Scope=Draft; Surface=A): All 28 original behavioral cases pass on the literal fence. 28/28 passed with scoped mocks for four OS commands; canonical and SYSTEM alias accepted; 26 negative cases rejected with expected failure messages. Detailed fixtures and traces in JSON twin.
- **A04 PASS** (Scope=Draft; Surface=A): ASCII-only encoding. Live draft 50655 characters; 0 codepoints above U+007F. Byte/BOM check in EncodingEvidence.
- **A05 PASS** (Scope=Draft; Surface=A): Strict regex and repaired throw preserved. Draft lines 669-678; entire extracted Verify fence equals prior literal code, independently parsed and rerun. Regex requires balanced quoted executable and URL; throw uses formatted form string.
- **F01 PASS** (Scope=Draft; Surface=A): REPL PackageId and command are distinct and correct. Draft 386-388,559-564; src/McpServer.Repl.Host/McpServer.Repl.Host.csproj:12-13; build/Build.InstallReplTool.cs:16-20,78-80; Build.PackReplTool.cs:8-23.
- **F02 PASS** (Scope=Draft; Surface=A): Approved Windows deployer, executable, guard and config restore order. Draft 323-338,583-606; Build.UpdateService.cs:156-174 restores then registers then writes manifest then starts; WindowsServiceHelper.cs:106-134,485-488; WindowsServiceDeploymentGuard.cs:18-88; Update-McpService.ps1 Restore/BackupArchive parameters and approved generator.
- **F03 PASS** (Scope=Draft; Surface=A): Linux service paths remain blocked. Draft 64-66,340-350,364-375,397,499-522. Source search in src/build for sd_notify, UseSystemd, AddSystemd, Type=notify returned 0 matches. Only operator-owned tested service-other is allowed.
- **F04 PASS** (Scope=Draft; Surface=A): Storage provider keys and precedence are correct. Draft 285-319; McpDatabaseConfigurationResolver.cs:96-168,192-203 confirms SQLite default, SQLite ConnectionString override, SQL Server alias chain and empty-primary failure; Program.cs:100-104 reapplies environment and CLI overrides.
- **F05 PASS** (Scope=Draft; Surface=A): Plugin acquisition verbs, host identity and smoke are supported. Draft 187-256,414-430; ToolRegistryController.cs:34 GET search and :189 POST install; Codex mcp-status.ps1:84-101 exposes session/queue fields, not trust; Cline package.json:6-15 and server.json:9-18 support node/dist activation with host proof.
- **F06 PASS** (Scope=Draft; Surface=A): Typed manifest and plan drive multi-agent verification. Draft 78-175,258-263,410-441; host/endpoint/product fields, one ID per selected agent, plan branches, stop conditions and expected/actual/pass evidence are explicit.
- **F07 PASS** (Scope=Draft; Surface=A): PowerShell-only and mandatory PowerShell.MCP are retained. Draft 20,381-388,527,540-565. Every ordinary command requires PowerShell.MCP >= 1.14.0; only pre-pwsh bootstrap excepted; Python prohibited.
- **F08 PASS** (Scope=Draft; Surface=A): Resolved DB identity is mandatory for every provider. Draft 423-427,685; provenance must cover overrides; SQLite yaml alone and ready connectivity cannot pass; unavailable proof blocks with VERIFY_EVIDENCE_UNAVAILABLE. Resolver and StorageConnectivityHealthCheck.cs:45-54 support distinction.
- **F09 PASS** (Scope=Draft; Surface=A): Linux acquisition avoids broad directory ownership changes. Draft 499-522 requires dedicated clone directories and never chown /opt itself; no executable publish-swap lifecycle remains in draft.
- **G1 PASS** (Scope=Draft; Surface=A): Checklist install-complete permits only UpdateService or service-other. Draft 459-462 explicitly places Configure-before-start before Install; linux-publish-swap and systemd excluded.
- **G2 PASS** (Scope=Draft; Surface=A): Blocked Linux conflict paths redirect only to service-other or stop. Draft 64-66,340-350,364-375,397,507; no blocked lifecycle offered as a working redirect.
- **G3 PASS** (Scope=Draft; Surface=A): REPL primary path is pinned to InstallReplTool. Draft 192,387-388,459,515-517,559-564; source target DependsOn PackReplTool, uses SharpNinja.McpServer.Repl and verifies mcpserver-repl.
- **G4 PASS** (Scope=Draft; Surface=A): PowerShell.MCP prerequisite, config-before-start and Grok mirror are actionable. Draft 20,194,331-335,404-407,545-550,585,604-638. Version assert/install/reassert and fail-closed code; service secrets/workspace configuration precede deploy; skillMirror triggers copy/symlink and evidence.
- **D1 PASS** (Scope=Draft; Surface=A): Phase numbering cannot reverse Windows execution order. Draft 394-399 explicitly says Phase 3 configure-before-start precedes UpdateService elevation despite section labels; plan sketch 163-174 has the same order.
- **D2 PASS** (Scope=Draft; Surface=A): MSIX, Keycloak and SyncAgentPlugins are explicit non-goals. Draft 474-476; no ordinary setup branch depends on these maintainer/optional surfaces.
- **D3 PASS** (Scope=Draft; Surface=A): Claude dual-agent smoke requires host-matched helpers, hook evidence and restart. Draft 210-218,258-263,429; current Claude lib/repl-invoke.ps1 supports Method/ParamsYaml; hook installer has VerifyOnly at line 7 and checks UserPromptSubmit/Stop/PostToolUse at 85-87,313; restart required in draft.
- **A06 PASS** (Scope=Draft; Surface=A): LocalSystem, service state and PID/listener binding are fail-closed. Draft 416-421,662-687; literal cases reject NetworkService, stopped service, zero PID, wrong listener PID, no listener and missing artifacts. Source registration does not provision alternate accounts.
- **B01 PASS** (Scope=Draft; Surface=B): Draft makes no implementation or Byrd exit claim. Draft header remains DRAFT; no product source changed or project completion asserted. Applicable validation is fence compilation and behaviors plus source review; full build/test gate is not claimed or waived for a future implementation exit.
- **B02 PASS** (Scope=Draft; Surface=B): Setup progress and secrets have bounded storage rules. Draft 5,15-24,55-57,76-175,412; manifest/plan replace unavailable session logs during setup, references replace secrets, bootstrap is not a write. SessionLogWorkflow.cs:74-86 confirms no-op bootstrap.
- **C01 PASS** (Scope=Draft; Surface=C): Requirements boundary evaluated without inventing draft completion. Live todo_get MCP-SETUPPROMPT-001: Done=false; FunctionalRequirements and TechnicalRequirements null. requirements_effective local Layer 1 returned 341 FR, 465 TR, 495 TEST, 341 mappings, no setup-prompt matches. FR/TR/AC mapping and live dry-runs remain future completion-gate concerns, not a claim in this draft review.
- **D01 PASS** (Scope=Draft; Surface=D): Plan remains open and paper evidence is not live install proof. Live TODO inventory and two-variant dry-run tasks remain false. Paper receipt explicitly disclaims install, trust/smokes and service repair. No overall TODO/plan completion claimed; pending inventory/dry-runs remain visible.
- **R01 PASS** (Scope=ReviewerOnly; Surface=B): add-profile executed first and read in full. First shell command read add-profile SKILL.md; all 19 dynamically enumerated non-skill profile markdown files fully consumed before validation; truncated batches reread in smaller chunks.
- **R02 PASS** (Scope=ReviewerOnly; Surface=B): Reviewer model and identity are live verified. Codex session 01a0e8c9-ba8a-71f1-bed5-2eb9ffbd3def turn_context 2026-09-28T16:12:12.894Z reports model gpt-6-astra and effort xhigh in both direct and collaboration fields. Runner is GrokCode; reviewer remains Codex.
- **R03 PASS** (Scope=ReviewerOnly; Surface=B): All shell work routed through PowerShell.MCP. mcp__powershell__execute_command from first command; pwsh.exe 7.6.6 on PAYTON-LEGION2; PowerShell.MCP 1.14.0 present. No unmanaged shell, Python or native shell fallback executed.
- **R04 PASS** (Scope=ReviewerOnly; Surface=B): Review writes confined to authorized receipts. Only request/response JSONL and Markdown/JSON twins under docs/receipts/hv authored. No draft fix necessary. Initial unrelated dirty files preserved; no service/config mutations.
- **R05 PASS** (Scope=ReviewerOnly; Surface=B): Explicit no-log/no-done/no-commit instructions followed. Only read-only todo_get and requirements_effective MCP calls. No MCP session-log writes, TODO changes, commits, pushes, deployment or hook installation.
- **R06 PASS** (Scope=ReviewerOnly; Surface=B): Durable request and response with receipt twins. Request sidecar supplemented with exact prompt, launch command, identity, model proof and evidence paths. Response snapshot contains public assistant/tool records plus complete verdict. Outer runner is configured to replace snapshot with full redacted codex --json stdout after exit; future replacement is not claimed verified.
- **R07 PASS** (Scope=ReviewerOnly; Surface=B): This run has no live credential echoes. Known live marker credential occurrence count in raw current-session transcript is 0. Query candidates seen are REDACTED placeholders. Pre-persistence sanitizer and final receipt/JSONL scans enforce the same gate.

## Literal behavioral cases

- canonical: PASS; expected ACCEPT; actual ACCEPT.
- exe-open-only: PASS; expected REJECT; actual REJECT.
- exe-close-only: PASS; expected REJECT; actual REJECT.
- url-open-only: PASS; expected REJECT; actual REJECT.
- url-close-only: PASS; expected REJECT; actual REJECT.
- program-files-unquoted: PASS; expected REJECT; actual REJECT.
- exe-old-quoted: PASS; expected REJECT; actual REJECT.
- exe-old-unquoted: PASS; expected REJECT; actual REJECT.
- prefix-port-71470: PASS; expected REJECT; actual REJECT.
- exe-later-arg: PASS; expected REJECT; actual REJECT.
- duplicate-urls: PASS; expected REJECT; actual REJECT.
- trailing-argument: PASS; expected REJECT; actual REJECT.
- exe-unquoted: PASS; expected REJECT; actual REJECT.
- url-unquoted: PASS; expected REJECT; actual REJECT.
- both-unquoted: PASS; expected REJECT; actual REJECT.
- wrong-executable: PASS; expected REJECT; actual REJECT.
- wrong-host: PASS; expected REJECT; actual REJECT.
- embedded-quote: PASS; expected REJECT; actual REJECT.
- program-files-quoted-other-install: PASS; expected REJECT; actual REJECT.
- system-alias: PASS; expected ACCEPT; actual ACCEPT.
- wrong-account: PASS; expected REJECT; actual REJECT.
- stopped: PASS; expected REJECT; actual REJECT.
- zero-service-pid: PASS; expected REJECT; actual REJECT.
- wrong-listener-pid: PASS; expected REJECT; actual REJECT.
- no-listener: PASS; expected REJECT; actual REJECT.
- missing-exe: PASS; expected REJECT; actual REJECT.
- missing-manifest: PASS; expected REJECT; actual REJECT.
- missing-yaml: PASS; expected REJECT; actual REJECT.

Controlled fixtures stub only Get-Service, Get-CimInstance, Test-Path and Get-NetTCPConnection; the draft code was executed literally without rewriting its logic. JSON twin contains exact code, fixtures, errors and call traces.

## Known live observation

The literal fence rejects the current lab ImagePath because its executable and URL tokens are unquoted. This matches the user-disclosed drift and is explicitly excluded from DraftScope. No elevated UpdateService or repair was attempted.

## Reviewer diagnostics and corrections

- Two progress timestamps were inaccurate and were explicitly corrected; authoritative receipt timestamps come from live UTC clock.
- Initial large read outputs were truncated; all required profile and primary draft content was reread in bounded chunks.
- A locked session metadata read was retried with FileShare.ReadWrite; final live metadata verified.
- One search referenced a nonexistent Build.ReplTool.cs path; corrected to Build.InstallReplTool.cs and Build.PackReplTool.cs.
- A local helper-scope error and one tool-orchestration syntax error were corrected before results were used.

## Artifacts

- RequestJsonl: `F:\github\McpServer\docs\receipts\hv\20260928T161208Z-setupprompt-postdryrun.request.jsonl`
- ResponseJsonl: `F:\github\McpServer\docs\receipts\hv\20260928T161208Z-setupprompt-postdryrun.response.jsonl`
- Markdown: `F:\github\McpServer\docs\receipts\hv\hostile-validator-setupprompt-postdryrun-20260928T161208Z.md`
- Json: `F:\github\McpServer\docs\receipts\hv\hostile-validator-setupprompt-postdryrun-20260928T161208Z.json`

## Final artifact verification

At 2026-09-28T16:28:37.7201348Z, all four artifacts existed. JSON twin parsed; request JSONL had 2 valid records and response snapshot had 103 valid records, with zero parse errors. The snapshot is followed by a verification record. Known live credential hits were zero in all artifacts and the current review transcript. Unredacted query-credential candidates were zero. All 17 inspected source hashes remained unchanged. Candidate SHA-256 still matched 9933BAB3B92198E90BA23588CE9F459E191684D0BAB3140F1C0A8C10448910C0. OverallVerdict AGREE; DraftScopeVerdict AGREE.
