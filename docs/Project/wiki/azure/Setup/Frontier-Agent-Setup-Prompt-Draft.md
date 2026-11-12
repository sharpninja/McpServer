# DRAFT: Frontier-agent QBrain.AI setup prompt

Status: draft for MCP-SETUPPROMPT-001  
Audience: a frontier coding agent acting for an operator  
Session log: not available during setup. Persist progress only via the choices manifest and implementation plan files below. Do not claim that `workflow.sessionlog.bootstrap` writes a session log -- current `SessionLogWorkflow.BootstrapAsync` is an idempotent no-op.

---

## Copy-paste agent prompt

You are setting up QBrain.AI for an operator. Your job is to obtain, install, configure, and verify a working host without inventing tribal knowledge.

### Hard rules

1. Ask the interactive intake questions first. Do not obtain, install, or configure until intake is complete.
2. Session log is unavailable. After intake, write two files before any install work:
   - `setup-choices.manifest` (YAML or JSON)
   - `setup-implementation.plan` (Markdown or YAML)
3. Every later step loads those files and follows them. Do not re-ask answered questions unless the operator changes a choice.
4. **PowerShell-only + mandatory PowerShell.MCP.** Use PowerShell 7 (`pwsh`) for all Windows and Linux automation after PowerShell exists. For every ordinary `pwsh` invocation, route through **PowerShell.MCP >= 1.14.0** (`execute_command` / host-qualified name such as `mcp__powershell__execute_command` or `mcp__pwsh__execute_command`). **Obtain substep (Windows):** assert `Get-Command pwsh`; then `Get-InstalledPSResource PowerShell.MCP` (or `Get-Module -ListAvailable PowerShell.MCP`) and require version **>= 1.14.0**; if missing/below, `Install-PSResource PowerShell.MCP -Version 1.14.0 -Scope CurrentUser` (or newer) / environment-sanctioned bootstrap; re-assert version before any other setup step. If it cannot be installed or the host tool is unavailable, stop with `POWERSHELL_MCP_UNAVAILABLE` rather than spawning ad-hoc unmanaged shells. Invoke native OS utilities only *through* that PowerShell.MCP/`pwsh` path (e.g. `systemctl`, `apt-get`, `journalctl`, `ss`). Do not author bash/sh/cmd playbooks for ordinary setup steps. Do not use Python. **Only exception:** installing PowerShell itself on a host that lacks `pwsh` (documented one-time bootstrap below).
5. After plugins exist, call MCP only through the host-matched plugin (`lib/repl-invoke.ps1` or `skills/*/scripts/invoke.ps1`). Do not default to raw `:7147` REST or a broken box connector.
6. If an intake combination is unsupported or blocked pending validation, stop, explain the conflict, and propose remediation. Do not improvise a third path.
7. Never publish or paste private operator profiles / add-profile skills into public repos.
8. Report receipts: paths written, commands run (no secrets), verify outcomes. Verify is fail-closed: any failed comparison stops success claims.

### Artifact paths

Ask where to write artifacts (default under the chosen workspace):

- `<workspace>/docs/setup/setup-choices.manifest`
- `<workspace>/docs/setup/setup-implementation.plan`

If the workspace does not exist yet, write under an operator-chosen staging directory and move them into the workspace once it exists.

### Phase 0: Interactive intake

Ask these questions one group at a time. Collect follow-ups for each answer.

#### A. Agents used

- Which agents will talk to this QBrain.AI? (multi-select: Grok Bot / GrokCode, Codex, Claude Code, Claude Cowork, Cline, Copilot, OpenCode, Cursor other, Other)
- For each selected agent: preferred plugin identity name (must match host truth -- see playbooks; do not invent identities), plugin root path if known, and whether a local skill mirror exists (path if known).
- Record **one agent playbook ID per selected family** in `playbooks.agents[]` (array). Also set `playbooks.agentCombo` to `agent-single` or `agent-multi`.

#### B. Development process

- Use BDPv4 as the active process, or a custom/user-owned process?
- If BDPv4: confirm policy source will be MCP memory / process binding after server is up (do not invent gates during setup).
- If custom: name, version, and where the process definition lives (path or URL). If unknown, record `process: deferred` and continue setup without inventing process enforcement.

#### C. Storage backend

- Choose one: `MSSQL` | `PostgreSQL` | `SQLite` (maps to provider values `sqlserver` | `postgresql` | `sqlite`)
- Follow-ups:
  - MSSQL / sqlserver: server host, instance/port, database name, auth mode (Integrated / SQL auth). Do not echo passwords into the manifest; store a secret reference or placeholder only.
  - PostgreSQL: host, port, database, user; secret by reference only.
  - SQLite: file path; confirm single-writer expectations. If multiple agents share one server, record caution `SQLITE_MULTI_AGENT` (see conflict table) -- prefer MSSQL/PostgreSQL when concurrent writers are expected; do not treat multi-agent alone as automatic hard refuse without topology clarification.

#### D. Service installation target

- Choose one: `WindowsService` | `systemd` | `linux-publish-swap` | `other`
- Follow-ups:
  - WindowsService: service name (default `QBrainAi`), install path (default `C:\ProgramData\QBrainAi`), port (default `7147`), **runAs** (supported by `UpdateService` today: `LocalSystem` only). If operator wants NetworkService/custom: stop with `WINDOWS_RUNAS_UNSUPPORTED` -- UpdateService/EnsureServiceRegistration sets ImagePath/start mode only and does not provision alternate accounts/credentials.
  - systemd: **blocked pending validation** (no `sd_notify` / `Type=notify`). Stop with `SYSTEMD_PENDING_VALIDATION` or redirect to `other` (`service-other`) / stop-only. Do **not** redirect to `linux-publish-swap` (also blocked).
  - linux-publish-swap: **blocked pending inventory/validation** (`LINUX_PUBLISH_SWAP_PENDING_VALIDATION`). Prior draft recipes were not executable end-to-end; do not select this as the primary service playbook until a validated lifecycle lands. Redirect to `other` only with operator-owned, tested commands, or stop.
  - other: describe exactly (IIS, Docker, manual console, Scheduled Task, etc.). On Linux, this is the only non-blocked service kind until publish-swap is validated.

#### E. Host, workspace, and plugin roots

- Host OS: `windows` | `linux` (typed field; do not infer solely from service kind)
- MCP workspace path (example on lab: `F:\github\QBrainAi`)
- Plugin root(s) for selected agents (example: `F:\github\mcpserver-grok-plugin`)
- Endpoint: scheme/host/port (default port `7147` unless intake overrides); record as `endpoint.port` / `endpoint.baseUrl`
- Product/version pin if known (GitVersion / deployment version string)

Record answers, then write the manifest and plan. Show both to the operator and wait for acknowledgment before Phase 1 unless they said to proceed without review.

### Manifest schema (`setup-choices.manifest`)

```yaml
schemaVersion: 1
createdAt: <ISO-8601>
host:
  os: windows|linux
  notes: <free text>
  hostname: <string or null>
endpoint:
  scheme: http
  host: localhost   # or operator host
  port: 7147
  baseUrl: http://localhost:7147
product:
  version: <string or null>   # deployment / GitVersion when known
agents:
  - family: grok|codex|claude-code|claude-cowork|cline|copilot|opencode|cursor|other
    identity: <canonical identity string>   # must match playbook
    pluginRoot: <path or null>
    skillMirror: <path or null>
    playbookId: <agent-*>
    status: pending|obtained|configured|verified|blocked
process:
  kind: bdpv4|custom|deferred
  name: <string or null>
  version: <string or null>
  source: <path-or-url or null>
storage:
  kind: mssql|postgresql|sqlite
  provider: sqlserver|postgresql|sqlite   # Mcp:Database:Provider value
  connectionRef: <name of secret store entry or env var, never raw password>
  details:
    # sqlite:
    #   dataSource: <path>          # -> Mcp:Database:Sqlite:DataSource
    # postgresql:
    #   host: <h>
    #   port: 5432
    #   database: <db>
    #   username: <user>
    # sqlserver / mssql:
    #   host: <h>
    #   instanceOrPort: <instance|port>
    #   database: <db>
    #   authMode: integrated|sql
workspacePath: <path>
service:
  kind: windowsService|systemd|linux-publish-swap|other
  name: <string>                 # Windows service / unit name
  installPath: <path>            # e.g. C:\ProgramData\QBrainAi or /opt/mcpserver
  port: 7147
  runAs: LocalSystem|<linux-user>|unsupported
  executable: <string or null>   # Windows: QBrainAi.Support.Mcp.exe
  runtimeDir: <path or null>     # Linux publish-swap: directory containing DLL + appsettings.yaml (often <installPath>/current)
  details: {}
playbooks:
  agents: [<playbook-id>, ...]   # one per selected family
  agentCombo: agent-single|agent-multi
  process: <playbook-id>
  storage: <playbook-id>
  service: <playbook-id>
unsupportedFlags: []  # conflict / caution codes accepted with operator note
evidence: []          # append verify receipts: {check, expected, actual, pass}
```

### Plan schema (`setup-implementation.plan`)

Must include:

1. Summary of choices (human readable), including host OS, endpoint port/baseUrl, every selected agent identity + playbookId
2. Ordered steps: Obtain -> Install -> Configure -> Verify
3. For each step: commands branched by selected playbooks, expected evidence, status (`pending|done|blocked`), stop condition
4. Verify criteria that reference typed manifest fields (endpoint, provider, workspace, service exe/config, per-agent smoke)
5. Rollback / remediation notes for failed smoke (bounded retries; Windows: `Update-McpService.ps1 -Restore` when applicable)

#### Populated plan sketch (example -- Legion Grok + WindowsService + sqlserver)

```markdown
# setup-implementation.plan (example)
## Summary
- host.os: windows; endpoint.port: 7147; workspace: F:\github\QBrainAi
- agents: [agent-grok / GrokCode]; storage: storage-mssql (provider sqlserver); service: service-windowsService
## Obtain
- [ ] Clone/pull workspace + mcpserver-grok-plugin -> evidence: git rev-parse HEAD
- [ ] `./build.ps1 InstallReplTool` -> evidence: `qbrain-ai-repl --version`
## Configure-before-start (required before UpdateService)
- [ ] Write installed/staged `C:\ProgramData\QBrainAi\appsettings.yaml`: Provider=sqlserver; Omit empty SqlServer.ConnectionString; set `Mcp:RepoRoot` + primary `Workspaces[]` entry to workspacePath (replace lab-specific defaults)
- [ ] Bind secret into **service** identity env as `Mcp__Database__SqlServer__ConnectionString` (machine/service env), validate name present without printing value
- [ ] Plugin env prepared: MCP_PLUGIN_ROOT / MCP_AGENT_NAME=GrokCode / MCP_WORKSPACE_PATH
## Install (starts service)
- [ ] Elevated: `./build.ps1 UpdateService --service-name QBrainAi --install-path C:\ProgramData\QBrainAi --port 7147`
- [ ] Evidence: `.mcpservice-deployment.json`; exe; StartName LocalSystem; ImagePath exact exe+urls
## Verify (fail-closed)
- [ ] GET /health with nonce -> 200 + byte-for-byte nonce; storage field reachable
- [ ] Service Running; installPath contains approved manifest generatedBy build/Build.UpdateService.cs or scripts\Update-McpService.ps1
- [ ] Plugin todo.query + memory.list/get type=result
- [ ] Compare live provider/workspace/port/identity to manifest; mismatch -> STOP
```

---

## Scenario playbooks

Select playbook IDs from intake. For agents, select **one ID per selected family** into `playbooks.agents[]`. For process/storage/service, select exactly one ID each.

### Agent playbooks

Canonical families from `AGENTS-README-FIRST.yaml` / lab plugin repos. Prefer tool-registry acquisition (`official` bucket) then root hints.

#### `agent-grok`

- Plugin: `mcpserver-grok-plugin` (lab: `F:\github\mcpserver-grok-plugin`; skills may live under `%USERPROFILE%\.grok\skills`)
- Canonical identity: `GrokCode` (`PLUGIN_AGENT_NAME` / `MCP_AGENT_NAME`) -- do not accept a preferred alias that disagrees with host truth
- Env: `MCP_PLUGIN_ROOT`, `GROK_PLUGIN_ROOT`; `MCP_WORKSPACE_PATH`
- PATH: prepend `$HOME/.dotnet/tools` or `%USERPROFILE%\.dotnet\tools` so `qbrain-ai-repl` resolves (REPL itself comes from Phase 1 `./build.ps1 InstallReplTool` only -- do not also run plugin `ensure-repl` in the ordinary dry-run)
- **Acquire (preferred):** after server can serve the tool registry, `GET /qbrainai/tools/search?keyword=mcpserver-grok-plugin`, select the result whose `name` exactly equals `mcpserver-grok-plugin`; if missing, **POST** `/qbrainai/tools/buckets/official/install?toolName=mcpserver-grok-plugin` (authenticated; search is GET, install is POST), search again, then execute the returned `commandTemplate` with `targetParent` = plugin parent directory. Root hints are fallback verification only.
- **Skills mirror (Obtain/Configure evidence when intake `skillMirror` is set):** copy or symlink MCP skills from `pluginRoot\skills\` into the operator `skillMirror` path (lab: `%USERPROFILE%\.grok\skills`). Record mirrored skill names as evidence. Do **not** publish private add-profile / operator-only profiles into public repos.
- **Activate:** set env; after server up, run the Grok plugin trust/marker bootstrap that performs signature verification + `/health` nonce echo against `endpoint.baseUrl` (not a vague "root exists" check). Record nonce/signature pass in evidence.
- **Smoke:** `. "$env:MCP_PLUGIN_ROOT\lib\repl-invoke.ps1"`; `Invoke-ReplMethod -Method 'workflow.todo.query' -ParamsYaml 'done: false'` must return `type: result`
- Unavailable: `MCP_PLUGIN_UNAVAILABLE:GrokCode` -- stop MCP usage

#### `agent-codex`

- Plugin: `mcpserver-codex-plugin`
- Canonical identity: `Codex`
- Env: `CODEX_PLUGIN_ROOT`, `PLUGIN_AGENT_NAME=Codex`, `MCP_WORKSPACE_PATH`
- **Acquire:** GET search exact name `mcpserver-codex-plugin`; if missing **POST** `/qbrainai/tools/buckets/official/install?toolName=mcpserver-codex-plugin`; search again; run returned `commandTemplate` with `targetParent`
- **Trust evidence (do not invent Status fields):** use the plugin marker/trust bootstrap helper that actually returns signature+nonce success (e.g. `Invoke-FullBootstrap` / marker-resolver path used by the Codex plugin). `Invoke-CodexMcpPlugin.ps1 -Command Status` reports session/queue metadata and is **not** by itself proof of workspacePath, marker signature, nonce, or health version -- do not claim those fields from Status.
- **Smoke:** after trust bootstrap succeeds against `endpoint.baseUrl`, `Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method workflow.todo.query -Params "done: false"` expecting `type: result`. Record the endpoint used.
- Remember Codex native hooks may be unreliable; prefer explicit plugin invokes for mutations
- Unavailable: `MCP_PLUGIN_UNAVAILABLE:Codex`

#### `agent-claude-code`

- Plugin: `mcpserver-claude-code-plugin`
- Canonical identity: `Claude`
- Env: `CLAUDE_PLUGIN_ROOT`, `PLUGIN_AGENT_NAME=Claude`, `MCP_WORKSPACE_PATH`
- **Acquire:** GET search exact name `mcpserver-claude-code-plugin`; if missing **POST** install `toolName=mcpserver-claude-code-plugin`; then `commandTemplate` with `targetParent`
- **Activate:** Claude Code hooks / `.mcp.json` mcpserver entry; run `claude-hook-validation` skill (may call `install-claude-mcp-hooks.ps1`) so UserPromptSubmit/Stop/PostToolUse MCP hooks exist; restart Claude Code after install
- **Smoke:** host-matched Claude plugin PowerShell invoke helpers (not Grok `repl-invoke`) -> read-only todo query `type: result`. Record script/helper name used. Hook evidence: `install-claude-mcp-hooks.ps1 -VerifyOnly` (or equivalent) showing UserPromptSubmit/Stop/PostToolUse wired; Claude Code restarted after install.
- Unavailable: `MCP_PLUGIN_UNAVAILABLE:Claude`

#### `agent-claude-cowork`

- Plugin: `mcpserver-claude-cowork-plugin` (lab repo may exist even when not listed in the primary AGENTS contract table)
- Canonical identity: use plugin contract / operator-confirmed host identity (do not silently reuse `Claude` unless the host truly is Claude Code)
- If plugin cannot be acquired or activation is unclear: stop with `AGENT_PLAYBOOK_UNVERIFIED:claude-cowork` and redirect to `agent-claude-code` or remove from selection

#### `agent-cline`

- Plugin: `mcpserver-cline-plugin` (lab may also have `mcpserver-cline-v2-plugin` -- pin exact name in manifest `details`; default `mcpserver-cline-plugin`)
- Canonical identity: `Cline`
- Env: `CLINE_PLUGIN_ROOT`, `PLUGIN_AGENT_NAME=Cline`, `MCP_WORKSPACE_PATH`
- **Acquire:** GET search exact name `mcpserver-cline-plugin`; if missing **POST** install; then `commandTemplate` into `targetParent`
- **Build (fresh clone):** from plugin root in pwsh: `npm ci`; `npm run build` (produces `dist/index.js` per package.json). Record absolute path to `dist/index.js`.
- **Activate:** point Cline MCP config at `command: node` with `args: [<absolute>/dist/index.js]` and env `MCP_WORKSPACE_PATH=<workspace>` (see plugin `server.json`). Restart/reload Cline. Confirm the Cline MCP panel lists this server connected (host UI evidence required -- wrapper-only smoke is insufficient).
- **Smoke:** from Cline host, run a read-only MCP tool that maps to todo query / list; must succeed. If Cline host activation cannot be proved, stop with `AGENT_PLAYBOOK_UNVERIFIED:cline`.
- Unavailable: `MCP_PLUGIN_UNAVAILABLE:Cline`

#### `agent-copilot`

- Plugin: `mcpserver-copilot-plugin`
- Canonical identity: `Copilot`
- Env: `COPILOT_PLUGIN_ROOT`, `PLUGIN_AGENT_NAME=Copilot`, `MCP_WORKSPACE_PATH`
- **Acquire:** GET search exact name `mcpserver-copilot-plugin`; if missing **POST** install; then `commandTemplate` with `targetParent`
- **Activate:** Copilot plugin hooks + `.mcp.json` mcpserver entry; run plugin `hooks/session-start` equivalent for the workspace when the contract provides `startup_command` (see AGENTS-README-FIRST). Confirm hooks wired before claiming configured.
- **Smoke:** host-matched plugin invoke for read-only todo query (`type: result`); do not substitute another agent's plugin
- Unavailable: `MCP_PLUGIN_UNAVAILABLE:Copilot`

#### `agent-opencode`

- Plugin: `mcpserver-opencode-plugin` (lab repo may exist; not always in primary AGENTS contract table)
- Pin identity from plugin contract; verify acquisition + one smoke invoke before claiming configured
- If acquisition/activation fails: `AGENT_PLAYBOOK_UNVERIFIED:opencode` -- stop or remove from selection

#### `agent-cursor` / `agent-other`

- **No verified first-party playbook in AGENTS-README-FIRST.** Do not invent install/activate steps.
- Action: stop with `AGENT_FAMILY_UNSUPPORTED:<family>` unless operator supplies a concrete, documented plugin path and invoke procedure recorded in manifest `details` and acknowledged as operator-owned (`unsupportedFlags` entry). Without that, remove the family from selection.

#### `agent-multi` (combo flag, not a substitute for per-family IDs)

- When `playbooks.agents` length > 1: set `playbooks.agentCombo: agent-multi`
- Prefer `postgresql` or `sqlserver` when concurrent writers are expected; for SQLite see caution `SQLITE_MULTI_AGENT`
- Each agent keeps its own plugin root, identity, and playbookId; one workspace unless operator explicitly splits
- Verify must smoke **every** selected agent, not only the first

### Process playbooks

#### `process-bdpv4`

- After server smoke succeeds, load BDPv4 policy from MCP (example global memory id `MEMORY-DEVPROCESS-001` when present) or the active process binding API when available
- Do not hard-code BDPv4 phase names into plugin code during setup
- Setup itself is not full BDPv4 enforcement; record `process.kind: bdpv4` for later binding

#### `process-custom`

- Record name/version/source in manifest
- Do not invent gates; point configure notes at where the custom process will be registered later

#### `process-deferred`

- Complete obtain/install/configure/verify without process binding
- Plan must include a follow-up step: bind process after first successful smoke

### Storage playbooks

Provider configuration is resolved by `McpDatabaseConfigurationResolver` from `Mcp:Database:*` (and legacy aliases). Setting a connection string alone does **not** select the provider; defaults remain SQLite if `Provider` is unset.

Common keys (non-secret shapes only in manifest):

| Choice | `Mcp:Database:Provider` | Primary settings | Env / legacy aliases (secret refs OK) |
|--------|-------------------------|------------------|----------------------------------------|
| SQLite | `sqlite` | `Mcp:Database:Sqlite:DataSource` or `Sqlite:ConnectionString` | `MCP_SQLITE_DATA_SOURCE`; also `Mcp:DataSource` / TodoStorage paths may apply |
| PostgreSQL | `postgresql` | `Mcp:Database:PostgreSql:ConnectionString` | `Mcp:PostgresConnectionString`, `ConnectionStrings:Mcp`, `POSTGRES_CONNECTION_STRING`, `MCP_POSTGRES_CONNECTION_STRING` |
| MSSQL | `sqlserver` | `Mcp:Database:SqlServer:ConnectionString` | `Mcp:SqlServerConnectionString`, `ConnectionStrings:McpSqlServer`, `MCP_SQLSERVER_CONNECTION_STRING` |

**Do not** point SQL Server at `ConnectionStrings:Mcp` / `ConnectionStrings__Mcp` -- that alias is the PostgreSQL fallback path.

#### `storage-mssql`

- Prerequisites: reachable SQL Server; database created or creatable; firewall/auth worked out
- Set `Mcp:Database:Provider: sqlserver`.
- **Secret binding (precedence):** `McpDatabaseConfigurationResolver` uses null-coalescing on configuration values. An **empty string** primary key wins over env aliases and then throws. Therefore:
  - Prefer binding the **primary** key via environment overlay: `Mcp__Database__SqlServer__ConnectionString=<secret>` (service account / process env), **or**
  - Put the real secret only in `Mcp:Database:SqlServer:ConnectionString` through a secret store that materializes a non-empty value, **or**
  - If using fallbacks (`Mcp:SqlServerConnectionString`, `ConnectionStrings:McpSqlServer`, `MCP_SQLSERVER_CONNECTION_STRING`), **omit** the primary `Mcp:Database:SqlServer:ConnectionString` key entirely from yaml (do not leave it as `""`).
- Never use `ConnectionStrings:Mcp` / `ConnectionStrings__Mcp` for SQL Server (PostgreSQL fallback path).
- Startup: product applies EF migrations for the selected provider as implemented; do not invent a separate migrate CLI unless product docs for this build provide one
- Live check: `/ready` or health storage reachable; read non-secret `Provider` from the **effective** installed `appsettings.yaml`; confirm database name from manifest `details.database` against a non-secret probe the operator approves (or mark `VERIFY_EVIDENCE_UNAVAILABLE` if no safe probe exists)

#### `storage-postgresql`

- Prerequisites: server reachable; database and role exist
- Set `Provider: postgresql` and bind `PostgreSql:ConnectionString` (or aliases). Same secret discipline
- Confirm provider is supported by the installed build (all three providers exist in current factory); if a future build drops one, stop with `STORAGE_PROVIDER_UNSUPPORTED`

#### `storage-sqlite`

- Local file via `Sqlite:DataSource` (example Windows: `C:\ProgramData\QBrainAi\mcp.db`; Linux: `/var/lib/mcpserver/mcp.db`)
- Single-writer expectations: prefer dedicated data directory writable by the service account
- Multi-agent: caution `SQLITE_MULTI_AGENT` -- redirect toward MSSQL/PostgreSQL when concurrent independent writers are intended; allow continue only with operator-acknowledged risk note in `unsupportedFlags` (not a categorical hard refuse without topology evidence)

### Service playbooks

#### `service-windowsService`

- **Normal path (required):** elevated Nuke/script deployer that writes `.mcpservice-deployment.json` with `generatedBy` of `build/Build.UpdateService.cs` or `scripts\Update-McpService.ps1`, plus executable hashes. Windows service startup enforces `appsettings.yaml` + that manifest (`WindowsServiceDeploymentGuard`).
- **runAs:** `LocalSystem` only for this playbook. Record `service.runAs: LocalSystem`. NetworkService/custom --  `WINDOWS_RUNAS_UNSUPPORTED`.
- Commands (from repo root, Administrator / gsudo):
  - `pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService --service-name <name> --install-path <path> --port <port>`
  - Equivalent: `gsudo .\scripts\Update-McpService.ps1 -ServiceName <name> -InstallPath <path> -Port <port>`
- Defaults: service name `QBrainAi`, install path `C:\ProgramData\QBrainAi`, port `7147`, main exe **`QBrainAi.Support.Mcp.exe`** (also launcher `QBrainAi.Launcher.exe`)
- **Config authority / order (critical):**
  1. **Fresh install** (no `installPath\appsettings.yaml` yet): write the desired `appsettings.yaml` (Provider + non-secret settings; **omit** empty connection-string keys) into the repo staging copy that UpdateService will publish, **and** place the same file at `installPath\appsettings.yaml` before/with first deploy so restore preserves the intended config. Bind secrets via environment variables that overlay the **same** primary keys (see storage playbooks) under the service account **before** first start expectations.
  2. **Upgrade** (existing install): UpdateService **backs up and restores** `installPath\appsettings.yaml`. Edit the **installed** `installPath\appsettings.yaml` (or restore target) for provider changes -- do not assume a repo-only edit survives. Secrets remain out of band on the same primary env keys.
  3. UpdateService stops -> publish -> restore preserved yaml -> register ImagePath -> **starts immediately**. Therefore provider + secret env must be correct **before** that start, not in a later "Configure after Install" improvisation.
  4. **Config-before-start is non-negotiable for dry-runs:** stage Provider, omit-empty connection-string keys, service-identity secret env binding, and rewrite `Mcp:RepoRoot` + primary `Mcp:Workspaces` for the fresh host (remove/disable foreign drive-letter lab defaults) **before** elevating UpdateService. Do not assume lab yaml Workspaces are valid on a new host.
- **Do not** use raw `sc.exe` / `New-Service` pointing at `publish\QBrainAi.exe` as the normal path -- that binary name is wrong and skips the deployment guard
- Rollback: `gsudo .\scripts\Update-McpService.ps1 -Restore` (optional `-BackupArchive`); backups under `%USERPROFILE%\QBrainAi-Backups`
- Health: service Running + `/health` nonce + storage reachable + MCP smoke

#### `service-systemd`

- **Blocked pending validation** (`SYSTEMD_PENDING_VALIDATION`). Product source has no `sd_notify` / systemd integration; do not ship `Type=notify` units.
- Action: refuse systemd as the primary service playbook until a tested unit exists. Redirect to `service-other` with operator-owned commands, or stop. Do **not** redirect to `service-linux-publish-swap` (also blocked pending validation).

#### `service-linux-publish-swap`

- **Blocked pending inventory/validation** (`LINUX_PUBLISH_SWAP_PENDING_VALIDATION`).
- Reason: product has a lab note (publish/swap to `/opt/mcpserver`) but this draft does not yet ship a Parser-valid, permission-complete, repeated-upgrade-safe PowerShell lifecycle with PID/log binding. Do not invent one during setup.
- Action: refuse this playbook; choose `service-other` with operator-supplied tested commands, or stop setup on Linux until inventory closes.
- systemd remains separately blocked (`SYSTEMD_PENDING_VALIDATION`).
#### `service-other`

- Operator description drives the plan (Docker, console, Task Scheduler, etc.)
- Plan must spell exact start/stop/log/health commands in PowerShell
- If description is too vague, ask one clarifying round before writing the plan
- Scheduled Task / Docker examples must target `QBrainAi.Support.Mcp` (not `QBrainAi.exe`) and still require verify gates

### Conflict table (refuse, caution, or redirect)

| Code | Condition | Action |
|------|-----------|--------|
| `SQLITE_MULTI_AGENT` | SQLite + more than one agent | Caution / redirect to MSSQL or PostgreSQL when concurrent writers expected; continue only with informed risk note in `unsupportedFlags` (pending stronger topology evidence) |
| `SYSTEMD_ON_WINDOWS` | systemd on Windows host | Refuse; choose WindowsService or other |
| `WINDOWS_SERVICE_ON_LINUX` | WindowsService on Linux | Refuse; choose `service-other` (operator-owned) or stop -- not linux-publish-swap (blocked) |
| `SYSTEMD_PENDING_VALIDATION` | systemd selected as primary | Refuse/redirect until a tested unit exists; use `service-other` or stop -- not linux-publish-swap (blocked) |
| `MISSING_PLUGIN_ROOT` | Agent selected, no plugin path and clone denied | Stop configure until plugin available |
| `AGENT_FAMILY_UNSUPPORTED` | Cursor/other without operator-owned procedure | Stop or remove family |
| `AGENT_PLAYBOOK_UNVERIFIED` | Cowork/OpenCode activation unclear | Stop, redirect, or remove |
| `STORAGE_PROVIDER_UNSUPPORTED` | Chosen storage not in this build | Stop; switch storage or build |
| `SECRET_IN_MANIFEST` | Password written into manifest | Redact, rewrite manifest, rotate if exposed |
| `VERIFY_MISMATCH` | Live endpoint/provider/workspace/exe != manifest | Fail-closed stop; bounded remediation then re-verify |
| `VERIFY_EVIDENCE_UNAVAILABLE` | Cannot obtain required proof (ImagePath, Provider, health version, listener PID bind) | Block success; treat as failed verify |
| `WINDOWS_RUNAS_UNSUPPORTED` | WindowsService runAs not LocalSystem | Refuse; UpdateService does not set alternate accounts |
| `POWERSHELL_MCP_UNAVAILABLE` | PowerShell.MCP cannot be installed/used | Stop ordinary automation until resolved |
| `LINUX_PUBLISH_SWAP_PENDING_VALIDATION` | linux-publish-swap selected as primary | Refuse; use `service-other` with operator-owned tested commands, or stop until validated lifecycle exists |

---

## Phase 1: Obtain (after manifest + plan exist)

Typical steps (branch in plan); all via `pwsh` unless installing `pwsh` itself:

1. Confirm git remotes / clone `QBrainAi` to `workspacePath` if missing
2. Clone/pull selected agent plugin repos into `pluginRoot` paths (prefer tool-registry official bucket per AGENTS contract)
3. Ensure PowerShell 7 (`pwsh`). **Pre-PowerShell exception (Linux example, one-time):** install PowerShell using the vendor script, then switch entirely to `pwsh`. On Windows, install/repair PowerShell 7 via documented Microsoft channels if missing.
4. Ensure `dotnet` SDK/runtime as required. **REPL acquisition (pin ONE primary path for the dry-run; do not thrash versions):**
   - **Primary (required default):** from workspace root run `./build.ps1 InstallReplTool` (Nuke target DependsOn PackReplTool). PackageId is **`QBrainAI.Repl`**; ToolCommandName / executable is **`qbrain-ai-repl`**. Verify: `qbrain-ai-repl --version`. Do **not** `dotnet tool install -g qbrain-ai-repl` (command name is not the PackageId). Do **not** use plugin `ensure-repl` / GitHub-release / Codex `setup.sh` as the ordinary dry-run path (those dual paths thrash versions and Codex bash conflicts with PowerShell-only).
   - **Secondary / non-default only** (local-feed policy understood, and recorded as non-default in the plan): run Pack first (`./build.ps1 PackReplTool` or `scripts\Pack-ReplTool.ps1`) so `local-packages` has the nupkg, **then** either `scripts\Install-ReplTool.ps1` or `dotnet tool install -g QBrainAI.Repl` (with version/source). Plain `Install-ReplTool.ps1` without Pack fails on an empty feed. Record which path was used in obtain evidence.
5. Storage prerequisites per storage playbook
6. Record obtain evidence in the plan checklist

## Phase 2: Install

**Execution order (WindowsService):** complete Phase 3 configure-before-start items (Provider, omit-empty connection keys, service secret env, RepoRoot/Workspaces rewrite) **before** any UpdateService elevation. Phase numbers are section labels; on Windows the configure-before-start work precedes Install because UpdateService starts the service at the end of deploy.

1. Windows: elevated `./build.ps1 UpdateService` (or `scripts\Update-McpService.ps1`) with real `--service-name` / `--install-path` / `--port`; expect `QBrainAi.Support.Mcp.exe` + `.mcpservice-deployment.json`. **Only after** Phase 3 config/secrets/workspace yaml+env are staged for that installPath.
2. Linux: do not run blocked `linux-publish-swap` / `systemd` playbooks. `service-other` only with operator-owned commands already recorded in the plan.
3. Do not treat install as complete until binaries and (Windows) deployment manifest are in place
4. Storage/workspace configuration must already be applied for the start that Install triggers

## Phase 3: Configure (mostly before Install start on Windows)

1. Set env vars from manifest (plugin roots, identity, workspace) for operator/plugin sessions
2. Wire storage **before UpdateService starts**: set `Mcp:Database:Provider`; bind secrets on primary env keys under the **service** account; **omit** empty YAML connection-string keys
3. **Workspace registry (fresh host):** in the same installed/staged `appsettings.yaml`, set `Mcp:RepoRoot` to the absolute `workspacePath`, and ensure `Mcp:Workspaces` contains that path with `IsPrimary: true`, `IsEnabled: true` (remove or disable unrelated lab defaults such as foreign drive letters). Plugin `MCP_WORKSPACE_PATH` does **not** register the server workspace. Do not hand-author trust markers; after service up, run the plugin marker/trust bootstrap so the selected workspace can satisfy marker checks. If registration/marker evidence cannot be obtained -> `VERIFY_EVIDENCE_UNAVAILABLE` / stop.
4. Apply process note (bind now only if API available; else leave deferred follow-up)
5. Windows: UpdateService starts at end of Install -- steps 2-3 must already be on disk/env. Linux primary service playbooks are blocked; `service-other` only with operator-owned start after the same config rules.
6. Update plan statuses

## Phase 4: Verify (fail-closed)

Run in order; stop on first hard failure. Session-log bootstrap must **not** be treated as proof of a log write (current bootstrap is an idempotent no-op). Use built-in `throw` / exit codes - do not depend on Pester `Should` unless Pester is an explicit prerequisite.

1. **Trusted marker / nonce:** plugin trust/bootstrap against `endpoint.baseUrl` `/health`; require HTTP 200 and **byte-for-byte nonce echo**. Signature failure -> `MCP_UNTRUSTED`. Record health JSON `version` vs `product.version` when pinned; if unreadable -> `VERIFY_EVIDENCE_UNAVAILABLE`.
2. **Endpoint:** live port/baseUrl must equal manifest. Listener checks use manifest port, count matches, and (Windows) join OwningProcess to service PID (step 3). Never mask failures.
3. **Service identity (Windows) - exact ImagePath + StartName + PID join:**
   - `Get-Service` Status must equal `Running` (capture object; do not use empty-pipeline `$?`).
   - Win32_Service PathName must match the **strict canonical UpdateService form** with **balanced quotes on both tokens** and **no trailing/duplicate args**: `"<installPath>\QBrainAi.Support.Mcp.exe" --urls "http://+:<port>"`. Quoting is atomic (both exe and URL fully quoted -- UpdateService always quotes). Reject optional/unbalanced quotes, unquoted paths (especially with spaces), `.exe.old`, prefix ports (`71470`), exe-as-later-arg, and duplicate `--urls`. Compare captured exe via GetFullPath equality and URL string equality.
   - StartName must be LocalSystem / NT AUTHORITY\SYSTEM. Existing alternate accounts are not fixed by UpdateService -> fail.
   - ProcessId > 0; listeners on manifest port must have OwningProcess equal to that PID.
   - Files: exe, `.mcpservice-deployment.json` with approved `generatedBy`, `appsettings.yaml`, no legacy `appsettings.json`.
4. **Service identity (Linux):** `systemd` and `linux-publish-swap` are blocked. For `service-other`, still require operator PID<->listener join on manifest port via `ss` through pwsh.
5. **Provider/DB (effective config):**
   - Installed yaml `Mcp:Database:Provider` must equal `storage.provider`, but yaml alone is never sufficient effective identity (Sqlite ConnectionString can override DataSource; env/instance/CLI overlays apply).
   - Require a **resolved** provider + non-secret database identity with provenance covering active overrides. Prefer product diagnostics/redacted connection summary if available.
   - `/ready` storage reachable is connectivity only and does not prove identity.
   - For **every** provider including SQLite: if resolved non-secret identity cannot be proven -> `VERIFY_EVIDENCE_UNAVAILABLE` (do not pass on raw `Sqlite:DataSource` yaml alone). Never print secrets.
6. **Workspace / identity:** installed yaml must list `workspacePath` under `Mcp:RepoRoot` / primary `Workspaces` (Phase 3). Plugin env alone is insufficient. Trust bootstrap + per-agent identity must match manifest; do not claim Codex `Status` provides workspacePath/nonce fields it does not.
7. **Per-selected-agent smoke:** host-matched plugin invoke for each `playbooks.agents` entry: sessionlog bootstrap (may no-op), `workflow.todo.query` -> `type: result`, memory list/get when applicable. Host UI/connection proof required for Cline/Copilot-class activations.
8. Append `evidence[]` expected/actual/pass. Any failure or unavailable required evidence -> stop; one bounded remediation then re-verify once.
9. Mark verify complete only when all asserts pass.

### Success criteria

- Manifest and plan files exist and match what was executed (including per-agent IDs, host OS, port/version)
- Service healthy with approved deployment artifacts where applicable
- Trust/nonce check passed
- Provider/DB/workspace/endpoint/identity comparisons passed
- Every selected agent plugin smoke returned `type: result` for todo query and memory list/get
- No secrets in manifest/plan
- Operator receives a short receipt: paths, playbook IDs, evidence summary

### Stop conditions

- Unsupported / blocked combination without override
- Plugin invoke unavailable and REST fallback not approved
- Storage unreachable or provider mismatch
- Service fails to start twice with the same error
- Verify mismatch after one bounded remediation cycle
- Operator withdraws consent

---

## Operator-facing checklist (short)

- [ ] Intake complete (agents, process, storage, service, host, workspace/plugins, endpoint)
- [ ] `setup-choices.manifest` written (typed multi-agent + host/port fields)
- [ ] `setup-implementation.plan` written and acknowledged
- [ ] Obtain complete (`./build.ps1 InstallReplTool` primary; PackageId QBrainAI.Repl; command qbrain-ai-repl)
- [ ] Configure-before-start complete (Provider + omit-empty connection keys + secret env binding + RepoRoot/Workspaces rewrite for foreign Workspaces) BEFORE service start
- [ ] Install complete (Windows: UpdateService; or `service-other` with operator-owned commands only -- NOT linux-publish-swap / systemd)
- [ ] Verify smoke green (nonce, endpoint, provider, workspace, per-agent)
- [ ] Receipt delivered

---

## Non-goals (v1)

- Production multi-tenant deploy hardening
- Publishing add-profile / private operator profiles
- Full process-drift enforcement (see MCP-PROCESS-001); setup only records process choice
- Claiming a validated product systemd unit (blocked pending validation)
- Claiming a validated linux-publish-swap lifecycle (blocked pending inventory)
- MSIX packaging (./build.ps1 PackageMsix / Package-QBrainAiMsix.ps1) as a setup path
- Keycloak / IdP helper scripts as part of ordinary setup
- Maintainer SyncAgentPlugins / plugin-core sync during operator first-install

## Reference lab pattern (optional)

When the operator's answers match the PAYTON-LEGION2 lab:

- Host OS: windows
- Workspace: `F:\github\QBrainAi`
- Grok plugin: `F:\github\mcpserver-grok-plugin`
- Identity: `GrokCode`
- Storage: often sqlserver to a Desktop SQL host (`Provider: sqlserver`, not PG-shaped keys)
- Service: Windows Service via `./build.ps1 UpdateService` -> `C:\ProgramData\QBrainAi\QBrainAi.Support.Mcp.exe`
- Endpoint port: 7147
- Still run full intake; do not skip questions because this pattern is familiar

---

End of DRAFT core. OS-specific command appendices follow. Revise after Legion dry-runs with at least two intake variants.

---

## Linux-specific examples

**Status: service playbooks `systemd` and `linux-publish-swap` are blocked pending validation/inventory.**

Use this section only for:
- Installing PowerShell 7 (pre-pwsh exception) and PowerShell.MCP
- Obtaining/cloning workspace + plugins via `pwsh`
- `service-other` when the operator supplies tested start/stop/log/health commands
- Storage client prerequisites invoked through pwsh

Do **not** treat any prior publish-swap/unit snippets as an approved product path. If intake selected those service kinds, stop with `SYSTEMD_PENDING_VALIDATION` or `LINUX_PUBLISH_SWAP_PENDING_VALIDATION`.

### Obtain notes (Linux, pwsh)

```powershell
# After pwsh + PowerShell.MCP exist:
$env:PATH = "$HOME/.dotnet/tools:$env:PATH"
# Clone into operator-chosen paths (dedicated dirs only; never chown /opt itself)
# PRIMARY: From workspace: ./build.ps1 InstallReplTool ; qbrain-ai-repl --version
# PackageId QBrainAI.Repl -- never dotnet tool install -g qbrain-ai-repl
# Secondary only after PackReplTool: scripts\Install-ReplTool.ps1 (non-default; empty feed fails)
```

### Verify notes (Linux `service-other` only)

Operator-owned process must still satisfy Phase 4: trust/nonce against manifest endpoint, Provider effective-config rules, PID-listener join on manifest port via `ss` through pwsh (not `Get-NetTCPConnection`), per-agent smokes.

---
## Windows-specific examples

Use when `host.os: windows`. Use **PowerShell 7** (`pwsh`) through **mandatory PowerShell.MCP >= 1.14.0** (`execute_command`).

### Reference Windows pattern (example / lab)

- OS: Windows 11 / Windows Server with required .NET runtime
- Workspace: `F:\github\QBrainAi`
- Grok plugin: `F:\github\mcpserver-grok-plugin`
- Identity: `GrokCode`
- Storage: often `sqlserver`; SQLite/PostgreSQL valid when chosen
- Service: `./build.ps1 UpdateService` -> `C:\ProgramData\QBrainAi`
- Port: 7147
- Shell: `pwsh`; elevate only for UpdateService

### Obtain (Windows)

```powershell
Get-Command pwsh -ErrorAction Stop
# PowerShell.MCP >= 1.14.0 (mandatory bridge for ordinary automation)
$psMcp = Get-InstalledPSResource PowerShell.MCP -ErrorAction SilentlyContinue | Sort-Object Version -Descending | Select-Object -First 1
if (-not $psMcp -or [version]$psMcp.Version -lt [version]'1.14.0') {
  Install-PSResource PowerShell.MCP -Version 1.14.0 -Scope CurrentUser -TrustRepository -ErrorAction Stop
  $psMcp = Get-InstalledPSResource PowerShell.MCP | Sort-Object Version -Descending | Select-Object -First 1
}
if (-not $psMcp -or [version]$psMcp.Version -lt [version]'1.14.0') { throw 'POWERSHELL_MCP_UNAVAILABLE' }
# Evidence: PowerShell.MCP version string (no secrets)
$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"

New-Item -ItemType Directory -Force -Path F:\github | Out-Null
Set-Location F:\github
if (-not (Test-Path .\QBrainAi\.git)) { git clone <QBrainAi-remote> QBrainAi }
if (-not (Test-Path .\mcpserver-grok-plugin\.git)) { git clone <plugin-remote> mcpserver-grok-plugin }

Set-Location F:\github\QBrainAi
# PRIMARY REPL path (pin this; do not also run plugin ensure-repl):
./build.ps1 InstallReplTool
qbrain-ai-repl --version
# PackageId QBrainAI.Repl -- NOT: dotnet tool install -g qbrain-ai-repl
# Secondary only: PackReplTool / Pack-ReplTool.ps1 FIRST, then Install-ReplTool.ps1 (non-default)
```

MSSQL prerequisites:

```powershell
# Test-NetConnection <sql-host> -Port 1433
# Create database/login out of band; secret by reference
```

SQLite:

```powershell
$dataDir = 'C:\ProgramData\QBrainAi'
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
# details.dataSource example: C:\ProgramData\QBrainAi\mcp.db
# ACL for service account
```

### Install + Windows Service (approved path only)

**Only after** Configure-before-start: stage Provider + omit-empty connection keys + RepoRoot/Workspaces rewrite into the repo/service `appsettings.yaml`, bind service-identity secrets, then:

```powershell
Set-Location F:\github\QBrainAi
# Elevated:
# sudo --chdir . pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService --service-name QBrainAi --install-path C:\ProgramData\QBrainAi --port 7147
# or:
# gsudo .\scripts\Update-McpService.ps1 -ServiceName QBrainAi -InstallPath C:\ProgramData\QBrainAi -Port 7147

Get-Service -Name QBrainAi | Format-List Status, StartType, Name
Test-Path 'C:\ProgramData\QBrainAi\QBrainAi.Support.Mcp.exe'
Test-Path 'C:\ProgramData\QBrainAi\.mcpservice-deployment.json'
Test-Path 'C:\ProgramData\QBrainAi\appsettings.yaml'
# Rollback if needed:
# gsudo .\scripts\Update-McpService.ps1 -Restore
```

Do **not** document raw `sc.exe create` against `publish\QBrainAi.exe` as a normal alternative.

### Configure-before-start (Windows) -- required BEFORE UpdateService

UpdateService **starts the service** at the end of Install. Complete the block below (and secret binding) on staged/installed yaml **before** elevation.

```powershell
$env:MCP_PLUGIN_ROOT = 'F:\github\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = $env:MCP_PLUGIN_ROOT
$env:MCP_AGENT_NAME = 'GrokCode'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = 'F:\github\QBrainAi'
$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
# Optional Grok skills mirror (when intake skillMirror set):
# Copy-Item / New-Item -ItemType SymbolicLink from $env:MCP_PLUGIN_ROOT\skills\* -> $skillMirror
# Evidence: Get-ChildItem $skillMirror | Select-Object Name  (do not publish private add-profile)
```

Example non-secret `appsettings.yaml` database + workspace block for SQL Server (**omit** empty ConnectionString so aliases/env can apply; **rewrite** foreign Workspaces):

```yaml
Mcp:
  RepoRoot: F:\github\QBrainAi
  Workspaces:
    - Path: F:\github\QBrainAi
      IsPrimary: true
      IsEnabled: true
      # Remove or disable unrelated lab defaults (foreign drive letters) on fresh hosts.
  Database:
    Provider: sqlserver
    # OMIT SqlServer.ConnectionString unless a secret store materializes a non-empty value.
    # Empty "" shadows fallbacks and fails resolver coalescing.
```

Preferred secret binding for the service process: `Mcp__Database__SqlServer__ConnectionString` (primary key overlay).  
Fallbacks only if primary key is absent: `Mcp:SqlServerConnectionString`, `ConnectionStrings:McpSqlServer`, `MCP_SQLSERVER_CONNECTION_STRING`.  
Manifest `connectionRef` example: `env:Mcp__Database__SqlServer__ConnectionString` -- **not** `ConnectionStrings__Mcp`.

Plugin invoke:

```powershell
pwsh -NoProfile -Command {
  $env:MCP_PLUGIN_ROOT = 'F:\github\mcpserver-grok-plugin'
  $env:GROK_PLUGIN_ROOT = $env:MCP_PLUGIN_ROOT
  $env:MCP_AGENT_NAME = 'GrokCode'
  $env:PLUGIN_AGENT_NAME = 'GrokCode'
  $env:MCP_WORKSPACE_PATH = 'F:\github\QBrainAi'
  $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
  . "$env:MCP_PLUGIN_ROOT\lib\repl-invoke.ps1"
  Invoke-ReplMethod -Method 'workflow.todo.query' -ParamsYaml 'done: false'
}
```

### Verify (Windows)

```powershell
$name = 'QBrainAi'    # manifest.service.name
$port = 7147           # manifest.endpoint.port
$install = 'C:\ProgramData\QBrainAi'

$svc = Get-Service -Name $name -ErrorAction Stop
if ($svc.Status -ne 'Running') { throw "Service status=$($svc.Status)" }
$wmi = Get-CimInstance Win32_Service -Filter "Name='$name'"
$expectedExe = [IO.Path]::GetFullPath((Join-Path $install 'QBrainAi.Support.Mcp.exe'))
$expectedUrls = "http://+:$port"
# Strict canonical UpdateService ImagePath: BOTH exe and --urls value fully quoted, balanced, no trailing junk.
# Rejects unbalanced quotes, unquoted paths with spaces, .exe.old, 71470, exe-as-later-arg, duplicate --urls.
$actual = $wmi.PathName.Trim()
$m = [regex]::Match($actual, '^"(?<exe>[^"]+\.exe)"\s+--urls\s+"(?<urls>[^"]+)"\s*$', 'IgnoreCase')
if (-not $m.Success) { $form = '"{0}" --urls "{1}"' -f 'exe','http://+:port'; throw "ImagePath must be fully quoted UpdateService form: $form; got: $actual" }
$actualExe = [IO.Path]::GetFullPath($m.Groups['exe'].Value)
$actualUrls = $m.Groups['urls'].Value
if (-not $actualExe.Equals($expectedExe, [StringComparison]::OrdinalIgnoreCase)) { throw "ImagePath exe mismatch: $actualExe" }
if (-not $actualUrls.Equals($expectedUrls, [StringComparison]::OrdinalIgnoreCase)) { throw "ImagePath --urls mismatch: $actualUrls" }
$canonical = '"{0}" --urls "{1}"' -f $expectedExe, $expectedUrls
$normalized = '"{0}" --urls "{1}"' -f $actualExe, $actualUrls
if (-not $normalized.Equals($canonical, [StringComparison]::OrdinalIgnoreCase)) { throw "ImagePath canonical mismatch" }
$startOk = @('LocalSystem','NT AUTHORITY\SYSTEM','.\LocalSystem') -contains $wmi.StartName
if (-not $startOk) { throw "StartName=$($wmi.StartName) not LocalSystem" }
if ($wmi.ProcessId -le 0) { throw 'Service ProcessId missing' }
if (-not (Test-Path $expectedExe)) { throw 'exe missing' }
if (-not (Test-Path "$install\.mcpservice-deployment.json")) { throw 'deployment manifest missing' }
if (-not (Test-Path "$install\appsettings.yaml")) { throw 'appsettings.yaml missing' }
# Provider/DB: require resolved identity with provenance; yaml alone insufficient (incl. SQLite ConnectionString vs DataSource)
$listeners = @(Get-NetTCPConnection -LocalPort $port -State Listen -EA SilentlyContinue | Where-Object OwningProcess -eq $wmi.ProcessId)
if ($listeners.Count -lt 1) { throw "No listener on $port owned by PID $($wmi.ProcessId)" }
# Evidence: assert reject for unbalanced quotes, unquoted Program Files path, .exe.old, http://+:71470, exe-as-later-arg.
# Accept only fully quoted canonical form matching UpdateService GetServiceImagePath.
# Trust/nonce against endpoint.baseUrl; per-agent smokes (Phase 4)
```

### Windows conflict reminders

- `SYSTEMD_ON_WINDOWS`: refuse systemd
- Elevate only for UpdateService / Restore; plugin smokes run as operator identity
- Prefer MSSQL remote/local as chosen; use sqlserver keys, not PostgreSQL aliases
