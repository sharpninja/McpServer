# MCP-SETUPPROMPT-001 — Install-surface inventory

**Status:** inventory receipt (feeds setup prompt draft; does **not** mark TODO done)  
**Author:** GrokCode on PAYTON-LEGION2  
**When:** 2026-09-28 (America/Chicago)  
**Draft cross-check:** docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md  
**Draft SHA256:** 5B518FF2CF4AF0D4F74399F2B186486FEEA430801FB181D8EB92FE812718D766 (verified)  
**HV overall AGREE stamp:** 20260928T153330Z  
**Workspace:** F:\github\McpServer  
**Method:** read-only inspection of draft + scripts/Nuke targets + plugin/skill trees. **Did not** run UpdateService, mutate the live service, dump secrets, commit, or push.

---

## Scope note

This catalogs **install/update surfaces** an agent or human might touch during frontier-agent MCP Server setup. Each row: path/command, who runs it, elevated?, idempotent?, what it installs/updates, how verify works, risks. Draft claims are called out where they diverge from repo truth.

Live lab (read-only observation): C:\ProgramData\McpServer\McpServer.Support.Mcp.exe, .mcpservice-deployment.json (generatedBy: build/Build.UpdateService.cs), and ppsettings.yaml are present. No live mutation performed.

---

## Table of surfaces

| # | Surface | Path / command | Who | Elevated? | Idempotent? | Installs / updates | Verify | Risks / notes |
|---|---------|----------------|-----|-----------|-------------|--------------------|--------|---------------|
| 1 | Repo clone / workspace | git clone / pull into workspacePath (lab: F:\github\McpServer) | Agent or human | No | Yes (pull) | Source tree, build scripts, ppsettings.yaml templates | git rev-parse HEAD; Test-Path solution | Foreign drive letters in repo ppsettings.yaml Workspaces (e.g. E:\…) must be rewritten for fresh hosts before service start |
| 2 | Nuke / build bootstrap | ./build.ps1 <Target> → dotnet run --project build/_build.csproj | Agent or human | Only for elevating targets | Target-dependent | Compiles Nuke host; routes all Nuke targets | Target exit 0 | Wrapper re-invokes self NoProfile once; needs SDK |
| 3 | Compile / publish (dev) | Nuke Compile / Publish; scripts\Start-McpServer.ps1 (console); optional -Docker | Agent or human | No | Mostly | Dev binaries under src/**/bin; console run of McpServer.Support.Mcp | /health; script exit | **Not** the Windows Service normal path; Run-McpServer.ps1 hardcodes stale E:\github\McpServer |
| 4 | REPL pack (local feed) | Nuke PackReplTool **or** scripts\Pack-ReplTool.ps1 | Agent or human | No | Yes (overwrite nupkg) | Packs McpServer.Repl.Host → local-packages/ as SharpNinja.McpServer.Repl | nupkg present under local-packages | Must precede local-feed install; plain Install-ReplTool.ps1 does **not** pack |
| 5 | REPL global tool (preferred) | ./build.ps1 InstallReplTool (uild/Build.InstallReplTool.cs) | Agent or human | No | Yes (skip if same version; uninstall+reinstall on mismatch / --update-tool) | Global tool **PackageId** SharpNinja.McpServer.Repl, **command** mcpserver-repl from local NuGet.config pointing at local-packages | mcpserver-repl --version (asserted in target) | DependsOn PackReplTool. Never dotnet tool install -g mcpserver-repl (command≠PackageId). Needs ~/.dotnet/tools on PATH |
| 6 | REPL global tool (script alt) | scripts\Install-ReplTool.ps1 [-Update\|-Uninstall] | Agent or human | No | Partial (-Update / reinstall) | Same PackageId via --add-source local-packages | mcpserver-repl --version | **Does not** pack first — fails if feed empty. Draft correctly prefers Nuke target |
| 7 | REPL via plugin ensure | mcpserver-grok-plugin\lib\ensure-repl.ps1; Codex setup.sh → lib/ensure-repl.sh | Agent/plugin hooks | No | Intended yes | May install mcpserver-repl (Grok README: GitHub releases; Codex bash setup) | Get-Command mcpserver-repl | Dual acquisition path vs local Pack+InstallReplTool. Codex setup.sh is **bash** — conflicts with draft PowerShell-only ordinary-setup rule |
| 8 | Windows Service deploy (approved) | Elevated ./build.ps1 UpdateService --service-name … --install-path … --port … (uild/Build.UpdateService.cs + WindowsServiceHelper) | Human elevates; agent may request | **Yes** (AssertElevated) | Upgrade-safe (backup → publish → restore yaml/data → register → start) | Publishes McpServer.Support.Mcp.exe + McpServer.Launcher.exe to install path (default C:\ProgramData\McpServer); registers service; writes .mcpservice-deployment.json (generatedBy: build/Build.UpdateService.cs); **starts service** | Built-in /health + workspace health; then Phase 4 ImagePath/PID checks | **Starts immediately** after register — config/secrets must be staged first. Internally uses sc.exe (create/config) — OK inside helper; **not** operator-authored sc.exe against publish\McpServer.exe. runAs LocalSystem only |
| 9 | Windows Service deploy (script twin) | gsudo .\scripts\Update-McpService.ps1 -ServiceName … -InstallPath … -Port … ; -Restore [-BackupArchive] | Human elevates | **Yes** | Same lifecycle; restore from %USERPROFILE%\McpServer-Backups | Same exe + generatedBy: scripts\Update-McpService.ps1 | Same + Restore evidence | Draft-approved equivalent. Do not confuse with Manage-McpService |
| 10 | Windows Service manage (ops only) | scripts\Manage-McpService.ps1 -Action Start\|Stop\|Restart\|Status\|Uninstall | Human/agent | Start/Stop/Uninstall need elev (gsudo; winget tip for gsudo) | Status yes | Lifecycle of **already deployed** service | Status / Get-Service | **Install** and **Publish** actions are **intentionally blocked** → redirect to Update-McpService. Agents must not treat this as install path |
| 11 | Config / connection strings / provider keys | Installed installPath\appsettings.yaml + service/process env overlays (e.g. Mcp__Database__SqlServer__ConnectionString); repo staging copy for first publish | Agent prepares; human may set machine/service env | Env for service account may need elev | Edits are overwrite; UpdateService **restores** installed yaml on upgrade | Sets Mcp:Database:Provider (sqlite\|postgresql\|sqlserver), non-secret details, Mcp:RepoRoot + primary Workspaces[]; secrets **out of band** | Fail-closed: resolved provider + non-secret identity provenance; /ready connectivity alone insufficient; never print secrets | Empty " primary ConnectionString **shadows** env aliases (resolver coalescing). Omit empty keys. Do not use ConnectionStrings:Mcp for SQL Server (PG path). Plugin MCP_WORKSPACE_PATH does **not** register server workspace |
| 12 | Validate config (helper) | scripts\Validate-McpConfig.ps1 / Nuke ValidateConfig | Agent or human | No | Yes (read) | None (validation) | Script/target exit | Useful preflight; not a substitute for live Phase 4 |
| 13 | Plugin acquire (registry preferred) | After server up: GET /mcpserver/tools/search?keyword=… then POST …/buckets/official/install?toolName=…; run returned commandTemplate with argetParent | Agent (authenticated) | No | Install may be re-runnable | Clones/materializes mcpserver-*-plugin under chosen parent | Search hit exact name; plugin root exists; smoke invoke | Needs live server + auth for POST. Root hints are fallback only |
| 14 | Plugin clone (lab / fallback) | git clone sibling repos under F:\github\ (known: grok, codex, claude-code, claude-cowork, cline, cline-v2, copilot, opencode) | Agent or human | No | Pull yes | Plugin trees + skills + hooks | Test-Path root; host-specific smoke | Cowork/OpenCode playbooks marked unverified until activation proved |
| 15 | Plugin core sync (maintainer) | Nuke SyncAgentPlugins → plugins/core/sync/sync-plugin-core.ps1 (+ wrapper generator) | Maintainer / agent in product repo | No | Intended | Syncs canonical core into sibling mcpserver-*-plugin repos; version bumps | Target logs; ValidatePluginPowerShellOnly | **Not** operator first-install; mutates plugin repos. Known plugin list matches lab siblings |
| 16 | Grok skills mirror | Copy/symlink mcpserver-grok-plugin\skills\* → %USERPROFILE%\.grok\skills (also marketplace plugin load) | Agent or human | No | Overwrite/symlink | Grok-native SKILL.md set (todo, session, requirements, …) | Skill discovery on next Grok start; smokes via lib\repl-invoke.ps1 | Draft mentions path but under-specifies copy/symlink as an install step; GROK-USAGE.md has the steps. Lab already has rich ~\.grok\skills (incl. mcpserver-* + hostile-validator) |
| 17 | Claude hooks install | mcpserver-claude-code-plugin\skills\claude-hook-wiring\scripts\install-claude-mcp-hooks.ps1 (−VerifyOnly) | Agent or human | No | Merges settings (backup unless -NoBackup) | Wires UserPromptSubmit/Stop/PostToolUse into ~/.claude/settings.json | -VerifyOnly; restart Claude Code; hook-validation skill | Host restart required; draft playbook covers this |
| 18 | Codex plugin setup | mcpserver-codex-plugin\setup.sh + Invoke-CodexMcpPlugin.ps1 | Agent or human | No | Partial | Ensures REPL; activation via codex --plugin . | Trust bootstrap (not Status alone) + Invoke todo.query | Bash setup vs PowerShell-only rule; Status ≠ trust evidence (draft correct) |
| 19 | Cline plugin build | From plugin root: 
pm ci; 
pm run build → dist/index.js; point Cline MCP at 
ode + abs path | Agent or human | No | 
pm ci fresh | Node MCP server binary | Host UI shows connected; host smoke | Host UI evidence required; draft correct |
| 20 | Skill mirrors (Codex / Claude / agents) | ~\.codex\skills, ~\.claude\skills, ~\.agents\skills (lab present); plugin skills/ are source of truth for MCP skills | Human/agent copy as needed | No | Overwrite | Host-local skill mirrors (add-profile, wrap-up, etc.) | Host skill list | Draft intake asks skillMirror path but no canonical sync recipe for Codex/Claude MCP skill parity; do not publish private add-profile into public repos |
| 21 | PowerShell 7 + PowerShell.MCP | Install/repair pwsh; Install-PSResource PowerShell.MCP (≥1.14.0) | Human/agent | May need elev for system install | Package managers usually yes | Shell host + MCP bridge for all ordinary automation | Get-Command pwsh; PowerShell.MCP tool available | Draft hard rule; missing PS.MCP → POWERSHELL_MCP_UNAVAILABLE. Concrete Windows bootstrap commands thin in draft appendices |
| 22 | Docker (service-other) | Dockerfile + docker-compose.mcp.yml; Start-McpServer.ps1 -Docker | Agent or human | Docker group / elev varies | Compose up idempotent-ish | Container image running McpServer.Support.Mcp.dll on 7147; volumes /data, /workspace | compose healthcheck curl /health | Valid only under service-other with operator-owned plan. SDK image pin 9.0 may drift from repo 
et10 — validate before claiming. Uses .env (see .env.example; no secrets in receipt) |
| 23 | MSIX package | ./build.ps1 PackageMsix / scripts\Package-McpServerMsix.ps1 | Maintainer / human | Sign may need cert | Rebuild | McpServer.Support.Mcp-*.msix via makeappx | File exists; optional signtool | **Not** in draft v1 playbooks; not Windows Service path; needs Windows SDK |
| 24 | Keycloak helper | scripts\Setup-McpKeycloak.ps1 (+ .sh) | Human / advanced | Depends | Partial | IdP client/roles for JWT flows | Manual IdP checks | Optional auth surface; **out of draft v1** non-goals-adjacent; do not invent during setup |
| 25 | Linux primary service | systemd unit / linux-publish-swap lifecycle | — | — | — | **Blocked** | — | Draft: SYSTEMD_PENDING_VALIDATION, LINUX_PUBLISH_SWAP_PENDING_VALIDATION. Only service-other with operator-owned pwsh commands until inventory closes |
| 26 | winget / chocolatey product | *(none for McpServer)* | — | — | — | — | — | No product winget/choco package found. Manage-McpService mentions winget install gerardog.gsudo only as elev helper. Chocolatey absent on lab host |

---

## Draft claims vs repo truth (cross-check)

| Claim in draft | Repo / lab truth | Verdict |
|----------------|------------------|---------|
| Prefer ./build.ps1 InstallReplTool; PackageId SharpNinja.McpServer.Repl; command mcpserver-repl | Build.InstallReplTool.cs + McpServer.Repl.Host.csproj (PackAsTool, ToolCommandName) match | **Aligned** |
| Never dotnet tool install -g mcpserver-repl | Correct — PackageId ≠ command name | **Aligned** |
| Elevated UpdateService → McpServer.Support.Mcp.exe under ProgramData; ImagePath …\McpServer.Support.Mcp.exe --urls http://+:port | MainExeName, GetServiceImagePath match exactly; live install present | **Aligned** |
| Do not use raw operator sc.exe / New-Service as normal path | Manage-McpService blocks Install/Publish; helper still uses sc.exe internally | **Aligned** (clarify internal vs operator) |
| Config before UpdateService start; yaml restore on upgrade | UpdateService backup/restore + start-at-end match | **Aligned** |
| systemd + linux-publish-swap blocked | No sd_notify / no validated swap lifecycle in product | **Aligned** (see gap on checklist/conflict redirects) |
| Plugin registry acquire then smoke | Playbooks + SyncAgentPlugins known list match lab repos | **Aligned** for acquire model |
| Grok skills under ~\.grok\skills | Lab populated; plugin README/GROK-USAGE prescribe copy/symlink | **Partially under-specified** in draft steps |
| Docker / Scheduled Task only as service-other targeting Support.Mcp | Dockerfile ENTRYPOINT uses Support.Mcp.dll; compose exists | **Aligned** as other; version pin risk |
| Operator checklist: Install complete (UpdateService **or linux-publish-swap**) | linux-publish-swap blocked in same draft | **Contradiction** |
| Conflict: systemd → redirect to linux-publish-swap | That target also blocked | **Contradiction** |

---

## Gaps vs draft (must-fix before dry-runs)

### Critical (block clean dry-run interpretation)

1. **Checklist contradiction:** Operator checklist still allows linux-publish-swap as install-complete; body blocks it. Fix checklist to Windows UpdateService **or** service-other (operator-owned) only.
2. **Conflict-table dead redirect:** SYSTEMD_PENDING_VALIDATION / WINDOWS_SERVICE_ON_LINUX redirect to linux-publish-swap which is also refused. Redirect only to service-other (or stop) until swap is validated.
3. **Config-before-start must be non-negotiable in dry-run plans:** UpdateService **starts the service**. Dry-run plans must stage Provider + omit-empty connection keys + service env secret binding + RepoRoot/Workspaces **before** elevation. Repo ppsettings.yaml still carries foreign workspace entries — plans must rewrite installed/staged yaml, not assume lab defaults.
4. **REPL dual acquisition:** Draft prefers local Pack+InstallReplTool; plugins also ensure-repl / GitHub releases / Codex setup.sh. Dry-run must pin **one** REPL acquisition path and record evidence so versions do not thrash.

### High (should fix or explicitly accept in dry-run notes)

5. **Install-ReplTool.ps1 without pack:** If agents use the script instead of Nuke, install fails on empty local-packages. Draft should say Nuke-only or require Pack first.
6. **PowerShell.MCP bootstrap surface:** Mandated but thin concrete Windows steps (version assert, Install-PSResource, failure code). Add a short Obtain substep.
7. **Grok skills mirror step:** Intake asks skillMirror; playbook should list copy/symlink from plugin skills/ → ~\.grok\skills as Obtain/Configure evidence (without publishing private add-profile).
8. **Codex bash setup.sh:** Conflicts with PowerShell-only hard rule for ordinary setup — document exception or provide pwsh equivalent for dry-runs.
9. **MSIX / Keycloak / SyncAgentPlugins:** Exist but are out of v1 setup prompt — explicitly list as non-goals so agents do not improvise them mid-setup.
10. **Docker SDK 9.0 vs current TFM:** If dry-run B uses Docker service-other, validate image build against current global.json/TFM first or refuse.

### Medium (documentation clarity)

11. Stale scripts\Run-McpServer.ps1 path (E:\github\McpServer) — not setup path but agent trap.
12. Manage-McpService -Action Install looks install-shaped but errors — one-line draft reminder already partial; keep in conflict reminders.
13. No winget/choco package — draft silent is OK; inventory records absence.

---

## Recommended Legion intake dry-run variants

### Dry-run A — Legion reference (single-agent Windows Service + SQL Server)

Concrete choice set:

- host.os: windows (PAYTON-LEGION2)
- gents: [{ family: grok, identity: GrokCode, pluginRoot: F:\github\mcpserver-grok-plugin, skillMirror: C:\Users\kingd\.grok\skills, playbookId: agent-grok }]
- playbooks.agentCombo: agent-single
- process.kind: bdpv4 (bind after smoke) **or** deferred if policy API not yet available
- storage: mssql / provider: sqlserver (Desktop SQL host; connectionRef: env:Mcp__Database__SqlServer__ConnectionString; **omit** empty yaml ConnectionString)
- service: windowsService — name McpServer, installPath C:\ProgramData\McpServer, port 7147, runAs LocalSystem, executable McpServer.Support.Mcp.exe
- workspacePath: F:\github\McpServer; endpoint http://localhost:7147
- Obtain: pull workspace + plugin; ./build.ps1 InstallReplTool only (no plugin ensure-repl); PATH ~\.dotnet\tools
- Configure-before-start: rewrite staged/installed yaml Provider+RepoRoot+primary Workspace; bind secret env under service identity
- Install: elevated ./build.ps1 UpdateService … (**do not** run during inventory; dry-run later)
- Verify: full Phase 4 ImagePath regex + nonce + provider provenance + Grok Invoke-ReplMethod todo.query

**One-liner:** Lab-faithful GrokCode + WindowsService + sqlserver dry-run that exercises config-before-start and canonical UpdateService verify.

### Dry-run B — Multi-agent + SQLite caution (still Windows Service)

Concrete choice set:

- Same host/workspace/endpoint/service as A (windowsService / ProgramData / 7147 / LocalSystem)
- gents: GrokCode **and** Claude (pluginRoot: F:\github\mcpserver-claude-code-plugin, identity Claude, playbookId gent-claude-code) — gentCombo: agent-multi
- storage: sqlite with details.dataSource: C:\ProgramData\McpServer\mcp.db; record unsupportedFlags: [SQLITE_MULTI_AGENT] with operator risk note (or redirect to sqlserver if operator refuses caution)
- Obtain: both plugin roots; Claude hook install script + restart note; Grok skills mirror evidence; single REPL path via InstallReplTool
- Verify: **both** agent smokes (Grok repl-invoke + Claude plugin invoke); SQLite resolved-identity provenance (not yaml DataSource alone); conflict-table handling documented in plan

**One-liner:** Multi-agent (Grok+Claude) WindowsService dry-run that forces SQLITE_MULTI_AGENT handling, dual smokes, and Claude hook activation evidence.

*(Alternative B if SQL Server preferred for multi-writer: keep sqlserver and drop SQLITE flag — still valuable for dual-agent verify.)*

---

## Surfaces count

- **Documented in table:** 26 surfaces (including explicit absences: Linux primary blocked, no winget/choco product)
- **Critical gaps:** 4
- **High gaps:** 6
- **Medium gaps:** 3

---

## Draft edits before dry-runs?

**Yes** — at least critical items 1–4 (checklist, conflict redirects, emphasize config-before-start + yaml rewrite, pin REPL acquisition). Prefer also high items 5–8 before claiming dry-run AGREE on the prompt text itself.

---

## Non-actions (this receipt)

- Did **not** mark MCP-SETUPPROMPT-001 TODO done
- Did **not** commit or push
- Did **not** run UpdateService / Restore / Manage Install
- Did **not** print connection strings or other secrets
