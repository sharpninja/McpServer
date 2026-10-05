# QBrain.AI Rebrand Implementation Plan

- Date: 2026-10-05
- Author context: cloud plan for operator Payton
- Status: Draft pending approval
- Process: planning artifact for a Byrd Development Process v4 requirement-change approval (`docs/Development-Process-draft-v4.md`). This file is not an implementation slice.
- Source checkout for the name map: `sharpninja/McpServer` at the `main` commit this plan was written from. Sibling repositories were not cloned in this run.
- This pull request: add this plan only. It does not rename namespaces, packages, assemblies, routes, or GitHub repositories, and it does not start Phase 1.

## 1. Goal and non-goals

### Goal

Replace the product brand **MCP Server** with **QBrain.AI** across the product repositories listed below, while leaving the open **Model Context Protocol** (the MCP wire protocol) named as it is.

In-scope repositories for the overall effort:

- `sharpninja/McpServer` (primary; this plan lives here)
- `sharpninja/McpServerManager`
- `sharpninja/McpServerTools`
- `sharpninja/mcpserver-grok-plugin`
- `sharpninja/mcpserver-grok-bot-plugin`
- `sharpninja/mcpserver-claude-code-plugin`
- `sharpninja/mcpserver-claude-cowork-plugin`
- `sharpninja/mcpserver-cline-plugin`
- `sharpninja/mcpserver-cline-v2-plugin`
- `sharpninja/mcpserver-codex-plugin`
- `sharpninja/mcpserver-copilot-plugin`
- `sharpninja/mcpserver-opencode-plugin`

`mcpserver-grok-bot-plugin` has no string hits in this McpServer checkout. Phase 2 still includes it. The executor confirms the remote exists before opening that PR and stops that slice if it does not.

### Non-goals

- Renaming the Model Context Protocol, the `/mcp-transport` endpoint, or MCP JSON-RPC wording.
- Renaming the QuadBrain feature or the QBAgent tool (`qbagent`, `McpServer.QBAgent` segment, `SharpNinja.McpServer.QBAgent` feature token `QBAgent`).
- Renaming requirement IDs (`FR-MCP-*`, `TR-MCP-*`, `TEST-MCP-*`).
- Rewriting historical receipts, chat logs, or session-log archives.
- Renaming the machine `PAYTON-LEGION2` or the lab name `LAB-OMARCHY`.
- Deploying to Legion or LAB-OMARCHY in Phases 0 through 4. Those hosts are a later operator step, written below as Phase 4-L, and they do not run until Payton explicitly starts that step.
- Opening SessionLife / P3 work, or any unfinished SessionLife gate, as part of this rebrand.
- Minting FR/TR/TEST rows in this plan PR. Register those through the requirements workflow after this plan is approved.

## 2. Locked naming convention

Verified pattern in this repo: the product root is the PascalCase token `McpServer` (namespaces, project folders, solution, Windows service name, `C:\ProgramData\McpServer`). The same word in lowercase concatenation is `mcpserver` (HTTP prefix, systemd unit, Linux account, `/opt/mcpserver`). Docker already uses a hyphen (`mcp-server`). Plugin repositories already use `{lowercase-token}-{role}` (`mcpserver-grok-plugin`). NuGet IDs keep the vendor prefix `SharpNinja.` plus the product root (`SharpNinja.McpServer.Client`).

`QBrainAi` is the C# / assembly / NuGet token. It matches `McpServer` better than `QBrainAI` (this repo writes `Mcp` and `QBAgent`, not `MCP` or `QBAGENT`) and better than `QBrain.AI` (a dot is not a namespace segment). `QBrain` alone collides with the existing QuadBrain feature and the `QBAgent` / `qbagent` tool. Those feature names stay.

Use the forms below everywhere in later phases. Do not mix them.

- Display, doc titles, Windows service DisplayName: `QBrain.AI`
- PascalCase identifier (namespaces, assemblies, project folders, solution, product GitHub repos, Windows service name, `C:\ProgramData` folder): `QBrainAi`
- Lowercase single token where the current string is `mcpserver` with no hyphen (HTTP product prefix, systemd unit, Linux account, `/opt`, `/var/lib`, `/etc`, `/var/log`): `qbrainai`
- Kebab slug where the current string is already hyphenated (`mcp-server` image, plugin repos, dotnet tool command, npm package names): `qbrain-ai`
- NuGet: `SharpNinja.QBrainAi.<Component>`
- Vendor and publisher strings `SharpNinja` and MSIX publisher `CN=FunWasHad` stay

Filesystem checkout folders follow the GitHub repo name (`F:\GitHub\McpServer` becomes `F:\GitHub\QBrainAi`; `F:\github\mcpserver-grok-plugin` becomes `F:\github\qbrain-ai-grok-plugin`). Updating local clones is an operator step after Phase 3.

## 3. Canonical name map

Old values were read from this checkout. "New" is the approved target. Alias behavior is in section 7. Do not apply the new column in this PR.

### Brand and identifiers

| Surface | Old | New |
| --- | --- | --- |
| Display / brand | MCP Server | QBrain.AI |
| C# root namespace | `McpServer` | `QBrainAi` |
| Solution | `McpServer.sln` | `QBrainAi.sln` |
| Assembly / project folder | `McpServer.<Rest>` | `QBrainAi.<Rest>` |
| NuGet ID | `SharpNinja.McpServer.<Component>` | `SharpNinja.QBrainAi.<Component>` |
| Product GitHub repo | `sharpninja/McpServer` | `sharpninja/QBrainAi` |
| Manager repo | `sharpninja/McpServerManager` | `sharpninja/QBrainAiManager` |
| Tools bucket repo | `sharpninja/McpServerTools` | `sharpninja/QBrainAiTools` |
| Plugin repo | `sharpninja/mcpserver-<role>-plugin` | `sharpninja/qbrain-ai-<role>-plugin` |
| Docker image and container | `mcp-server` (`mcp-server:latest`) | `qbrain-ai` (`qbrain-ai:latest`) |
| Docker network | `mcp-network` | `qbrain-ai-network` |
| Docker volume | `mcp-data` | `qbrain-ai-data` |
| Compose service key | `mcp-server` in `docker-compose.mcp.yml` | `qbrain-ai` |
| Config section | `Mcp:` | `QBrainAi:` |
| ASP.NET env binding | `Mcp__*` | `QBrainAi__*` |
| Product env vars | `MCP_WORKSPACE`, `MCP_API_KEY`, `MCP_INSTANCE`, `MCP_PLUGIN_ROOT`, and the other `MCP_*` keys listed below | `QBRAINAI_*` with the same suffix |
| Octopus project | `McpServer` | `QBrainAi` |
| Octopus server URL | `http://PAYTON-LEGION2:8066` | unchanged hostname |
| Windows service name | `McpServer` | `QBrainAi` |
| Windows service DisplayName | `MCP Server` | `QBrain.AI` |
| Windows install path | `C:\ProgramData\McpServer` | `C:\ProgramData\QBrainAi` |
| Windows executable | `McpServer.Support.Mcp.exe` | `QBrainAi.Support.Mcp.exe` |
| Launcher executable | `McpServer.Launcher.exe` | `QBrainAi.Launcher.exe` |
| Linux install path | `/opt/mcpserver` (`/opt/mcpserver/app`) | `/opt/qbrainai` (`/opt/qbrainai/app`) |
| Linux state path | `/var/lib/mcpserver` | `/var/lib/qbrainai` |
| Linux env file | `/etc/mcpserver/mcpserver.env` | `/etc/qbrainai/qbrainai.env` |
| Linux log path | `/var/log/mcpserver` | `/var/log/qbrainai` |
| systemd unit | `mcpserver.service` | `qbrainai.service` |
| Linux account | `mcpserver` | `qbrainai` |
| Docs titles | "MCP Server" when it means this product | "QBrain.AI" |
| README H1 | `# MCP Server` | `# QBrain.AI` |
| dotnet tool command | `mcpserver-repl` | `qbrain-ai-repl` |
| npm | `@sharpninja/mcpserver-agent-core`, `@sharpninja/mcpserver-plugin-core` | `@sharpninja/qbrain-ai-agent-core`, `@sharpninja/qbrain-ai-plugin-core` |
| npm REPL package | `@sharpninja/mcp-repl` | `@sharpninja/qbrain-ai-repl` |
| MSIX identity | `McpServer.Support.Mcp` | `QBrainAi.Support.Mcp` |
| MSIX application id | `McpServer` | `QBrainAi` |
| Keycloak API client id | `mcp-server-api` | `qbrain-ai-api` |
| User-profile backup dir | `%USERPROFILE%\McpServer-Backups` | `%USERPROFILE%\QBrainAi-Backups` |

`RepositoryUrl` in packed projects currently uses `https://github.com/SharpNinja/McpServer`. GitHub owner casing is not significant. Phase 3 updates these URLs to `https://github.com/SharpNinja/QBrainAi` after the rename. Until then they keep the live repo name so package metadata does not point at a repository that does not exist yet.

### Plugin repo map

| Old repo | New repo |
| --- | --- |
| `mcpserver-grok-plugin` | `qbrain-ai-grok-plugin` |
| `mcpserver-grok-bot-plugin` | `qbrain-ai-grok-bot-plugin` |
| `mcpserver-claude-code-plugin` | `qbrain-ai-claude-code-plugin` |
| `mcpserver-claude-cowork-plugin` | `qbrain-ai-claude-cowork-plugin` |
| `mcpserver-cline-plugin` | `qbrain-ai-cline-plugin` |
| `mcpserver-cline-v2-plugin` | `qbrain-ai-cline-v2-plugin` |
| `mcpserver-codex-plugin` | `qbrain-ai-codex-plugin` |
| `mcpserver-copilot-plugin` | `qbrain-ai-copilot-plugin` |
| `mcpserver-opencode-plugin` | `qbrain-ai-opencode-plugin` |

Tool-registry manifest file names inside `McpServerTools` follow the same old-to-new repo names. This checkout references `mcpserver-grok-plugin.json` on `sharpninja/McpServerTools`.

### Namespace and project rule

Replace only the root token `McpServer` with `QBrainAi`. Keep every segment after it, including `.Mcp`, `.McpAgent`, and `.QBAgent`.

Examples:

- `McpServer.Client` becomes `QBrainAi.Client`
- `McpServer.Support.Mcp` becomes `QBrainAi.Support.Mcp`
- `McpServer.McpAgent` becomes `QBrainAi.McpAgent`
- `McpServer.QBAgent` becomes `QBrainAi.QBAgent`
- `SharpNinja.McpServer.QBAgent` becomes `SharpNinja.QBrainAi.QBAgent`
- Tool command `qbagent` stays `qbagent`

Projects in `McpServer.sln` that follow that rule include `McpServer.Common.AgentCli`, `McpServer.ServiceDefaults`, `McpServer.Support.Mcp`, `McpServer.Client`, `McpServer.Cqrs`, `McpServer.Cqrs.Mvvm`, `McpServer.Launcher`, `McpServer.Storage`, `McpServer.Services`, `McpServer.GraphRag`, `McpServer.McpAgent`, `McpServer.McpAgent.SampleHost`, `McpServer.Storage.SqliteMigrations`, `McpServer.Storage.PostgreSqlMigrations`, `McpServer.Storage.SqlServerMigrations`, `McpServer.Repl.Core`, `McpServer.Repl.Host`, `McpServer.TransactionSecurity`, `McpServer.KeyServer`, `McpServer.Subscriber`, `McpServer.QBAgent`, `McpServer.QBAgent.Tools`, `McpServer.QBAgent.Skills`, `McpServer.SessionLog.Transcripts`, and the matching test and validation projects (`McpServer.Support.Mcp.Tests`, `McpServer.Support.Mcp.IntegrationTests`, `McpServer.Client.Tests`, `McpServer.Cqrs.Tests`, `McpServer.Launcher.Tests`, `McpServer.Context.Validation`, `McpServer.GitHub.Validation`, `McpServer.Repo.Validation`, `McpServer.SessionLog.Validation`, `McpServer.Todo.Validation`, `McpServer.ToolRegistry.Validation`, `McpServer.Workspace.Validation`, `McpServer.McpAgent.Tests`, `McpServer.Repl.Core.Tests`, `McpServer.Repl.IntegrationTests`, `McpServer.PlanReview.Tests`, `McpServer.TransactionSecurity.IntegrationTests`, `McpServer.Acid.IntegrationTests`, `McpServer.QBAgent.Tests`, `McpServer.Review.Tests`, `McpServer.PluginIntegration.Tests`, `McpServer.ProcessTree.TestHelper`).

RootNamespace does not always equal the folder name. Phase 1 replaces `McpServer` inside the existing `RootNamespace` and does not "fix" these exceptions:

- `src/McpServer.Storage`, `src/McpServer.Services`, and `src/McpServer.GraphRag` set `RootNamespace` to `McpServer.Support.Mcp`. New value: `QBrainAi.Support.Mcp`.
- Migration projects use `McpServer.Support.Mcp.Storage.{Sqlite|SqlServer|PostgreSql}Migrations`. New value: `QBrainAi.Support.Mcp.Storage.{Provider}Migrations`.
- `tests/Build.Tests` sets `RootNamespace` to `NukeBuild.Tests`. Leave that namespace. Rename the project only if a product string inside it is a brand hit, not because of the namespace.
- `_build` / `build/_build.csproj` stays `_build`.

`InternalsVisibleTo` arguments repeat assembly names (`McpServer.Services`, `McpServer.Support.Mcp`, test assemblies). They move with the assembly rename in the same commit as the project rename.

`azure-pipelines.yml` path filters still list `src/McpServer.Director/**`, `src/McpServer.UI.Core/**`, and `src/McpServer.Web/**`. Those directories are not in the solution. Phase 1 updates filters for real renamed folders and does not recreate the missing projects.

### Published package IDs found in csproj files

| Project | Current PackageId | New PackageId |
| --- | --- | --- |
| `McpServer.Client` | `SharpNinja.McpServer.Client` | `SharpNinja.QBrainAi.Client` |
| `McpServer.Cqrs` | `SharpNinja.McpServer.Cqrs` | `SharpNinja.QBrainAi.Cqrs` |
| `McpServer.Cqrs.Mvvm` | `SharpNinja.McpServer.Cqrs.Mvvm` | `SharpNinja.QBrainAi.Cqrs.Mvvm` |
| `McpServer.McpAgent` | `SharpNinja.McpServer.McpAgent` | `SharpNinja.QBrainAi.McpAgent` |
| `McpServer.Repl.Core` | `SharpNinja.McpServer.Repl.Core` | `SharpNinja.QBrainAi.Repl.Core` |
| `McpServer.Repl.Host` | `SharpNinja.McpServer.Repl` | `SharpNinja.QBrainAi.Repl` |
| `McpServer.QBAgent` | `SharpNinja.McpServer.QBAgent` | `SharpNinja.QBrainAi.QBAgent` |

`McpServer.Repl.Host` also sets `ToolCommandName` to `mcpserver-repl`. `McpServer.QBAgent` sets `ToolCommandName` to `qbagent` (unchanged).

### Config keys and env vars

The product configuration root in `src/McpServer.Support.Mcp/appsettings.yaml`, `appsettings.yaml`, and `Dockerfile` is the YAML section `Mcp:`. Nested keys (`Mcp:Database:Provider`, `Mcp:Port`, `Mcp:DataSource`, `Mcp:RepoRoot`, `Mcp:TodoFilePath`, `Mcp:SessionsPath`, workspace entries, and the rest of that section) keep their names. Only the root key changes from `Mcp` to `QBrainAi`. Dockerfile and compose currently pass `Mcp__Port`, `Mcp__DataSource`, `Mcp__DataDirectory`, `Mcp__RepoRoot`, `Mcp__TodoFilePath`, `Mcp__SessionsPath`. Those become `QBrainAi__*` under the alias policy.

Product environment variables seen in scripts and plugin tests, with the same suffix after the rename:

- `MCP_WORKSPACE` → `QBRAINAI_WORKSPACE`
- `MCP_API_KEY` → `QBRAINAI_API_KEY`
- `MCP_BEARER_TOKEN` → `QBRAINAI_BEARER_TOKEN`
- `MCP_INSTANCE` → `QBRAINAI_INSTANCE`
- `MCP_PLUGIN_ROOT` → `QBRAINAI_PLUGIN_ROOT`
- `MCP_WORKSPACE_PATH` → `QBRAINAI_WORKSPACE_PATH`
- `MCP_AGENT_NAME` → `QBRAINAI_AGENT_NAME`
- `MCP_CACHE_DIR_OVERRIDE` → `QBRAINAI_CACHE_DIR_OVERRIDE`
- `MCP_PLUGIN_PERSIST_LOG` → `QBRAINAI_PLUGIN_PERSIST_LOG`
- `MCP_PLUGIN_REPL_LOG` → `QBRAINAI_PLUGIN_REPL_LOG`
- `MCP_PLUGIN_REPL_RESPONSE` → `QBRAINAI_PLUGIN_REPL_RESPONSE`
- `MCP_PLUGIN_HOST` → `QBRAINAI_PLUGIN_HOST`
- `MCP_BRAIN_AOT_ENDPOINT`, `MCP_BRAIN_CLAUDE_CODE_ENDPOINT`, `MCP_BRAIN_CODEX_ENDPOINT` → `QBRAINAI_BRAIN_*`
- `MCP_BRAIN_AOT_API_KEY`, `MCP_BRAIN_CLAUDE_CODE_API_KEY`, `MCP_BRAIN_CODEX_API_KEY` → `QBRAINAI_BRAIN_*`

Phase 2 re-runs the inventory in each plugin repo and adds any `MCP_*` keys this list missed. It does not rename keys that the inventory classifies as protocol or sentinel.

### Strings that keep their current bytes through 1.x

| Surface | Current value | Decision |
| --- | --- | --- |
| SQLite file | `mcp.db` | Keep the filename. A new default would open an empty database beside the existing file. |
| Workspace state directory | `.mcpServer` | Keep the directory name. It is per-workspace state, not a title. |
| EF context type | `McpDbContext` | Keep the class name. Change only its namespace root. Renaming the class rewrites migration snapshots without a schema change. |
| EF tables | `TodoItems`, `SessionLogs`, `ToolBuckets`, and the other domain table names | No product prefix. Do not emit a rename migration. |
| SQL database on the Omarchy installer | `McpServer_Omarchy` | Do not rename in Phases 0–4. See Phase 4-L. |
| SQL login on that installer | `mcpserver_omarchy` | Do not rename in Phases 0–4. |
| Tool bucket row | `Repo: McpServerTools` | Accept both repo names until Phase 3, then migrate rows. |
| Requirement IDs | `FR-MCP-*`, `TR-MCP-*`, `TEST-MCP-*` | Never rename. |
| Trust sentinel | `MCP_UNTRUSTED` | Keep the literal through 1.x. Agents and docs match this exact string. |
| Protocol endpoint | `/mcp-transport` | Unchanged. |
| QuadBrain / QBAgent | feature names and `qbagent` | Unchanged, except the product root in front of `QBAgent`. |

## 4. Inventory method

Phase 1 starts with a classification pass and a committed receipt of dispositions. No replacement runs before that receipt exists. The receipt is a new file under `docs/receipts/` created by that later phase. This plan does not add it. Existing files under `docs/receipts/` are not edited.

Search the worktree with ripgrep, one pattern at a time, and write every hit to a TSV with columns: path, line, pattern, class, action.

Patterns:

- `McpServer`
- `MCP Server`
- `mcpserver`
- `MCP_SERVER` (record hits; none were required to justify the name map)
- `SharpNinja.McpServer` and `SharpNinja/McpServer`
- `mcp-server`
- `@sharpninja/mcpserver` and `@sharpninja/mcp-repl`
- `MCP_` as an environment-variable prefix, classified hit by hit

Classes and the only allowed action for each:

- `product-brand`: eligible for the name map in the phase that owns that file.
- `protocol`: leave. Examples: "Model Context Protocol", MCP JSON-RPC, `/mcp-transport`, package tags that only say `model-context-protocol`.
- `requirement-id`: leave `FR-MCP-*`, `TR-MCP-*`, `TEST-MCP-*`.
- `historical-receipt`: leave every path under `docs/receipts/`.
- `generated-wiki`: do not hand-edit `docs/Project/wiki/`. Regenerate only if a later docs slice already has a wiki export step, and only from the requirements store. This rebrand does not rewrite historical wiki snapshots by hand.
- `feature-quadbrain`: leave `QuadBrain`, `QBAgent`, and `qbagent` except where `McpServer` is the product root in front of them.
- `host-or-lab`: leave `PAYTON-LEGION2`, `LAB-OMARCHY`, and `PAYTON-OMARCHY`. List `McpServer_Omarchy`, `mcpserver_omarchy`, `/opt/mcpserver`, and `mcpserver.service` in the receipt and mark them Phase 4-L, not Phase 1.
- `persisted-state`: leave `.mcpServer`, `mcp.db`, and `McpDbContext` as decided above.
- `ambiguous`: do not edit until the disposition in section 4.1 is applied.

Exclude from automated replace, even when a pattern matches:

- `docs/receipts/**`
- `docs/Project/wiki/**` until an explicit regenerate step
- vendored `lib/NSubstitute/**`
- historical probe projects under `docs/receipts/**` such as `FailClosedProbe.csproj` and the copied `_build.csproj` under `docs/receipts/sessionlife-completion/**`

The classifier is a dry run. Its TSV is the Phase 1 review artifact. A second person or a hostile pass can reject a row before edits. String replacement is not allowed to be a blind `McpServer` → `QBrainAi` substitute.

### 4.1 Ambiguous cases

- **MCP Server versus Model Context Protocol.** "MCP Server" as a product title becomes "QBrain.AI". "Model Context Protocol" stays. A bare "MCP" in prose is ambiguous: if the sentence is about JSON-RPC, tools/list, or `/mcp-transport`, it stays; if it is this product, it becomes QBrain.AI. The inventory TSV decides each bare "MCP" hit. There is no bulk replace of `\bMCP\b`.
- **`/mcpserver/*` versus `/mcp-transport`.** Controllers use routes such as `mcpserver/todo`, `mcpserver/sessionlog`, and `mcpserver/requirements`. That prefix is the product API. It gains a `/qbrainai/*` canonical prefix and keeps `/mcpserver/*` as an alias through 1.x. `/mcp-transport` (`app.MapMcp("/mcp-transport")`) is the protocol endpoint and is not aliased or renamed.
- **`Mcp:` config versus MCP protocol.** `Mcp:` is the product settings root. It is not the protocol name. Alias policy applies. Nested key names stay.
- **`MCP_*` environment variables.** The keys listed above are product process settings. `MCP_UNTRUSTED` is a sentinel string, not an environment variable to rename.
- **`McpServer.Support.Mcp` and `McpServer.McpAgent`.** The trailing `.Mcp` and `.McpAgent` segments stay. Only the root token changes. Treating `.Mcp` as protocol text would produce inconsistent project names.
- **`@sharpninja/mcp-repl`.** This is the product REPL package in `tools/typescript/mcp-repl-ts/package.json`, not the protocol specification. It follows the tool-command rename to `@sharpninja/qbrain-ai-repl`.
- **`mcp-server-api` Keycloak client.** This is an identity-provider client id in `scripts/Setup-McpKeycloak.ps1`. Creating a new client is an operator IdP change. Phase 1 does not switch the server to the new audience unless the old client still validates. Default: leave the Keycloak client id until Phase 4, and do not apply it on a live realm in the cloud waves.
- **`McpServer_Omarchy` / `mcpserver.service` / `/opt/mcpserver`.** These are LAB-OMARCHY install identities in `docs/setup/install-local-service.ps1`. Editing the script so a later run creates different database and unit names is Phase 4-L, not a cloud rename of the lab.
- **Octopus step label "Octopus LEGION2 release".** The project string inside that step is `McpServer`. The URL host is `PAYTON-LEGION2`. Rename the project string only in the operator Octopus phase. Do not rename the host. Do not let rebrand CI call `octopus release deploy`.
- **`docs/Development-Process-draft-v4.md` product sentences.** The process doc links the product as "MCP Server" and to `https://github.com/sharpninja/mcpserver` and `mcpservermanager`. Those product references are live docs and are in Phase 1. The process name Byrd Development Process v4 stays.
- **Public blob URLs that embed `McpServer`.** After Phase 3, GitHub usually redirects old repo URLs. Historical receipts keep the old URL text. Live docs update to the new repo after the rename is confirmed. Phase 5 records whether a sample pre-rename SHA URL still resolves.

## 5. Phased execution

Phases are sequential. A phase does not start until the previous phase's acceptance criteria pass and, where noted, Payton has approved the gate. Execution is cloud-only through Phase 4. No SSH, WinRM, Octopus deploy, systemd, or filesystem change on PAYTON-LEGION2 or LAB-OMARCHY.

Work follows BDPv4 once implementation starts: acceptance tests for the slice go red first, then implementation, then refactor, and the unit suite for the current and previous iterations finishes with zero failures and zero skips. Deferred behavior is a requirement or TODO, not a skipped test. Do not add SessionLife tests to these slices.

### Phase 0 — this plan and the approval gate

Work: this markdown file and a draft PR on `sharpninja/McpServer`. No other product edits. `docs/Project` has no index that lists plans. `docs/Project/wiki/*/Documentation-Index.md` is generated wiki output and is not hand-edited here.

Gate: Payton approves or rejects the locked convention, the alias duration (through 1.x, removed at 2.0), the repo rename list, and the rule that Legion and LAB-OMARCHY deploys wait for Phase 4-L.

Rollback: close the draft PR. No runtime state changed.

Acceptance:

- The only product diff is this file.
- The name map matches the old identifiers cited from this repo, or the PR discussion corrects a cited identifier before approval.
- Status remains Draft until Payton's approval comment. Approval flips status in a follow-up commit on this file. That commit is still Phase 0.

### Phase 1 — McpServer code, tests, and live docs (cloud PR series on the current repo)

Do this only after Phase 0 approval. Land as sequential PRs on the existing `McpServer` repository, not on a renamed remote.

Order inside the phase:

1. Inventory TSV committed, with every hit classed. No source renames in that commit.
2. Project folders, `McpServer.sln`, `RootNamespace`, `AssemblyName`, `InternalsVisibleTo`, and namespace declarations. `Build.Tests` keeps `NukeBuild.Tests`.
3. Product strings: display name, route alias, config alias, env-var alias, Docker compose names, NuGet ids, tool command, npm package names in this repo, MSIX script defaults, Windows service script defaults.
4. Tests updated to the new names and to the aliases they must still accept.
5. Live docs only: `README.md`, `AGENTS.md`, `CLAUDE.md`, `docs/*.md` operator guides, `templates/prompt-templates.yaml`, `skills/**` that ship in this repo. Not `docs/receipts/**`. Not hand edits under `docs/Project/wiki/**`.
6. Type-forward facade projects for the seven published package IDs, still built, not pushed to nuget.org in this phase.

`docs/**` and `src/**` are path filters on `.github/workflows/build.yml`. A push to `main` runs pack and, on `main`, `dotnet nuget push` with `--skip-duplicate`. Phase 1 PRs therefore target a branch policy that does not push new package IDs: either merge only after Phase 4's publish gate exists, or disable the push step until Phase 4. Packing on a pull request is allowed. Pushing renamed IDs to nuget.org is not a Phase 1 act.

`azure-pipelines.yml` on `main` and `develop` can create an Octopus release and deploy to Development on `PAYTON-LEGION2` when `OCTOPUS_API_KEY` is set. Phase 1 changes that touch this file must leave that deploy step unrun (definition-level skip) until Phase 4-L. Do not "fix" the skip by deploying.

Rollback: revert the Phase 1 PR series. Because aliases ship in the same release as the new names, a reverted build still speaks the old names. Do not uninstall the Windows service in this phase; Phase 1 does not deploy it.

Acceptance:

- Solution builds with `./build.ps1 Compile`.
- `./build.ps1 Test` reports zero failures and zero skips for the unit scope that target already runs.
- `./build.ps1 ValidateConfig` and `./build.ps1 ValidateTraceability` pass. New public APIs have XML docs. Requirement IDs cited by new code already exist or were registered through the requirements workflow in this phase; this plan does not pre-assign an ID.
- `/mcp-transport` still maps, and a test covers that the path string did not change.
- A test covers `GET` or the existing health/todo route on both `/qbrainai/...` and `/mcpserver/...` for one representative controller, plus the config binder accepting `Mcp:` when `QBrainAi:` is absent and preferring `QBrainAi:` when both are present.
- `mcp.db`, `.mcpServer`, and `McpDbContext` still appear as the persisted-state names. A test or snapshot diff shows no EF table rename migration from this phase.
- `rg` over the inventory TSV shows zero `product-brand` rows still marked open.
- `git diff` against Phase 0 does not modify `docs/receipts/` except the new inventory TSV.
- `PAYTON-LEGION2` and `McpServer_Omarchy` still occur where they are host and database identities.
- NuGet push log for this phase, if CI runs, shows the push step skipped or `--skip-duplicate` on unchanged old IDs only. No `SharpNinja.QBrainAi.*` package is on nuget.org yet.

### Phase 2 — one cloud PR per sibling repo

Start only after Phase 1 acceptance. Each PR merges to that repo's default branch under the **old** GitHub name. Remote rename is Phase 3.

Order:

1. `McpServerTools` — add manifest names for `qbrain-ai-*-plugin` while the old `mcpserver-*-plugin` manifest names still resolve. Do not delete old manifest files in this phase.
2. `McpServerManager` — UI strings, paths, and links to the product. QuadBrain and QBAgent feature names stay.
3. Plugins, one PR each, in this order: grok, grok-bot, claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode.

Each plugin PR updates display strings, default repo URLs (still the old GitHub name until Phase 3, unless the link is built to tolerate both), tool command `mcpserver-repl` callers so they accept `qbrain-ai-repl` and still launch `mcpserver-repl` when that is the installed command, and skill text that says "MCP Server" as the product. Protocol instructions stay. Private add-profile content is not copied into the public repo. If add-profile lives only on an operator profile outside these repos, the PR notes that Payton updates that copy outside the PR.

If `mcpserver-grok-bot-plugin` is absent, record the missing remote in the Phase 2 receipt and continue with the next plugin. Do not create the repository.

Rollback: revert that repo's PR. Other repos stay on their last green phase.

Acceptance, per repo:

- That repo's existing test command finishes with zero failures and zero skips.
- A diff stat shows one repo only.
- Old plugin manifest names still exist in `McpServerTools` at the end of item 1.
- No PR in this phase changes GitHub repository settings or the `PAYTON-LEGION2` hostname.

### Phase 3 — GitHub repository renames and remote URL updates

This phase is operator-started. Cloud agents do not rename GitHub repositories. Payton renames, in order:

1. `McpServerTools` → `QBrainAiTools`
2. `McpServerManager` → `QBrainAiManager`
3. Each plugin repo to the `qbrain-ai-*-plugin` name
4. `McpServer` → `QBrainAi` last, so in-flight PR links to the primary repo stay valid until the consumers point at the new tool and plugin names

After each rename, a cloud PR on the new name updates `RepositoryUrl`, README clone URLs, tool-bucket seed and matcher so `QBrainAiTools` is canonical, and a data migration that rewrites stored `ToolBuckets.Repo` from `McpServerTools` to `QBrainAiTools` while still matching the old value if a row was not migrated. New databases seed `QBrainAiTools` only after this migration ships.

GitHub redirects old repo URLs. Phase 3 does not rewrite `docs/receipts/` to chase those redirects.

Rollback: rename the repository back. GitHub redirects follow the current name. Revert the URL-update PR. The tool-bucket migration must be reversible (store the previous repo string, or accept both names until the rollback window closes at 2.0).

Acceptance:

- `gh repo view` (or the GitHub UI) shows the new name and the old name as a redirect.
- A raw URL under the old repo name for a known file returns a redirect, and the same file at the new name returns 200.
- A fresh database seed writes `QBrainAiTools`. An existing database row `McpServerTools` still resolves until the migration runs, and resolves as `QBrainAiTools` after it runs.
- Clone instructions in live README files use the new repo name.
- `PAYTON-LEGION2` is unchanged.

### Phase 4 — package feeds, Octopus project string, deploy scripts (cloud edits; no host deploy)

Cloud PRs may edit feed and script **text** in git. They do not install services, create Octopus releases, or push to a machine.

Work:

- Publish `SharpNinja.QBrainAi.*` and the deprecated `SharpNinja.McpServer.*` type-forward packages to nuget.org from the intended `main` version. Push is the first time the Phase 1 publish gate is opened.
- Publish the renamed npm packages. Keep the old npm names as deprecated wrappers that depend on the new packages through 1.x, if the package layout can express that. If a wrapper is not practical, publish a deprecated metadata release on the old name whose readme points at the new name, and record that limitation in the Phase 4 receipt.
- Update `azure-pipelines.yml` project string from `McpServer` to `QBrainAi` only after the Octopus project exists under the new name. Until that operator action, the pipeline must not call `octopus release create`.
- Update default parameters in `scripts/Manage-McpService.ps1`, `scripts/Update-McpService.ps1`, and `scripts/Package-McpServerMsix.ps1` to the new service name, display name, install path, and MSIX identity. Leave a documented parameter override that can still target `McpServer` and `C:\ProgramData\McpServer` for rollback.
- Do not execute those scripts against Legion or LAB-OMARCHY in this phase.
- Do not change `McpServer_Omarchy`, `mcpserver_omarchy`, or the systemd unit that the Omarchy installer writes.

Rollback: stop publishing new versions; leave already pushed NuGet and npm versions in place (`--skip-duplicate` and NuGet's immutability mean a pushed version is not deleted as the rollback). Revert script-default PRs. Octopus project rename rollback is an operator rename back to `McpServer`.

Acceptance:

- nuget.org shows the new package IDs at the Phase 4 version and the old IDs at a type-forward build whose nuspec dependencies point at the new IDs.
- `dotnet tool install -g SharpNinja.QBrainAi.Repl` yields command `qbrain-ai-repl`. The deprecated tool package still yields `mcpserver-repl`.
- The Octopus deploy step's log for the merge that updates scripts shows it did not run, or Payton has already completed the project rename and the log shows project `QBrainAi` and host `PAYTON-LEGION2` unchanged.
- No Phase 4 PR diff changes the hostname `PAYTON-LEGION2`.

### Phase 4-L — later operator step (not in the first waves)

Payton starts this explicitly. It is not scheduled by merging Phase 4.

- Windows: stop service `McpServer`, install service `QBrainAi` at `C:\ProgramData\QBrainAi`, copy preserved state (`mcp.db`, keys, `appsettings` secrets) from `C:\ProgramData\McpServer`. Keep the old directory until the new service passes `/health`.
- Octopus: rename or recreate project `QBrainAi`, then allow the pipeline step to create a release. Target names that are the machine `PAYTON-LEGION2` stay.
- LAB-OMARCHY, only under a separate approval: unit `qbrainai.service`, paths `/opt/qbrainai`, `/var/lib/qbrainai`, `/etc/qbrainai`, account `qbrainai`. Database `McpServer_Omarchy` and login `mcpserver_omarchy` stay unless Payton approves a database rename in writing. A database rename is a backup, detach/attach or copy, and connection-string cutover, not a find-and-replace.
- Operator profile copies of add-profile and any machine-local plugin checkouts are updated by Payton. They are not pushed from the cloud agent.

Acceptance:

- `/health` on the new Windows service returns success and the nonce echo still works.
- The previous install directory still exists until Payton deletes it.
- `PAYTON-LEGION2` as a hostname still resolves to the same machine.
- LAB-OMARCHY is unchanged unless this phase's separate approval includes it.

### Phase 5 — verification and hostile gate

Run after Phase 4's cloud edits merge. Re-run the parts that touch a host only after Phase 4-L if that step happened.

Checks:

- `./build.ps1 Compile`, `./build.ps1 Test` (zero failures, zero skips in the unit scope), `./build.ps1 ValidateConfig`, `./build.ps1 ValidateTraceability`.
- Hostile or regression cases, each with a named test or a recorded command:
  - Old `/mcpserver/todo` (or the representative route from Phase 1) still responds.
  - New `/qbrainai/...` responds with the same status for the same request.
  - `POST /mcp-transport` is still the MCP endpoint and is not redirected to a QBrain path.
  - Config file that contains only `Mcp:` still boots.
  - `MCP_API_KEY` still authenticates when `QBRAINAI_API_KEY` is unset. When both are set, `QBRAINAI_API_KEY` wins.
  - A project that references `SharpNinja.McpServer.Client` at the type-forward version compiles against a public type that now lives in `SharpNinja.QBrainAi.Client`.
  - `docs/receipts/` at a pinned pre-rebrand SHA matches the current tree for those files (no historical receipt edits).
  - `QuadBrain` and `qbagent` still exist as feature names.
  - `FR-MCP-` IDs in `docs/Project/Functional-Requirements.md` are unchanged.
  - `PAYTON-LEGION2` still appears as the Octopus host default.
- Inventory TSV from Phase 1 has no unclassified rows.
- Sample public SHA URL recorded in the Phase 5 receipt: old repo URL result (redirect or failure) and new repo URL result.

Rollback of a failed gate: do not start Phase 4-L. Revert the failing phase's PR. Aliases keep old clients working while the fix is prepared.

Acceptance: the receipt for Phase 5 lists each check above as pass or fail with the command output path. The gate passes only when every check passes. A skip is a fail.

## 6. Compatibility policy

Aliases are required. They last through the 1.x line and are removed at 2.0. `README.md` currently states GitVersion's next version is 1.4.39, so this rebrand stays inside 1.x. Removal means the 2.0 packages, routes, and config binder stop accepting the old names. Already published 1.x packages stay on the feed.

Required aliases:

- HTTP: `/mcpserver/*` and `/qbrainai/*` both bound. New docs and new clients use `/qbrainai/*`.
- Config: `Mcp:` accepted when `QBrainAi:` is absent. If both are present, `QBrainAi:` wins and startup logs one warning.
- Environment: each renamed `MCP_*` product variable is read when the `QBRAINAI_*` variable is unset. If both are set, `QBRAINAI_*` wins.
- NuGet: old package IDs ship as type-forward facades that depend on the new packages. Public types move once. Facade assemblies use `TypeForwardedTo`.
- dotnet tool: old command `mcpserver-repl` remains installable from the deprecated package ID; new command is `qbrain-ai-repl`.
- Tool bucket repo string: readers accept `McpServerTools` and `QBrainAiTools` through 1.x.
- Plugin manifest file names: old and new names both resolve through 1.x.

Not aliased, because they are persisted state and stay put through 1.x: `mcp.db`, `.mcpServer`, `McpDbContext`, EF table names, `MCP_UNTRUSTED`, requirement IDs, `/mcp-transport`.

Not aliased in cloud phases, because changing them is a host migration: Windows service name, `C:\ProgramData` path, systemd unit, Linux account, Omarchy SQL database and login, Keycloak client id. Phase 4-L is the cutover, with the old path kept on disk until health passes.

## 7. Risks

- **Plugins break** if Phase 2 lands before Phase 1 aliases, or if a plugin is renamed in GitHub before its manifests exist in `McpServerTools`. The phase order is the mitigation.
- **NuGet consumers** fail to compile if public types move without type-forward packages, or if Phase 1 merges to `main` and the workflow pushes new IDs early. The publish gate is the mitigation.
- **dotnet tool** users have `mcpserver-repl` on PATH. Removing `ToolCommandName` without the deprecated package breaks agent stdio wrappers.
- **SSH and deploy configs** on machines still point at `C:\ProgramData\McpServer`, `/opt/mcpserver`, and service `McpServer`. Cloud phases must not assume those paths moved. Phase 4-L copies state; it does not delete the old path first.
- **Octopus** project `McpServer` and a pipeline that deploys to Development on `PAYTON-LEGION2` can fire on `main`/`develop` when `OCTOPUS_API_KEY` is set. A rebrand merge that leaves the step active will deploy mid-rename. Skip the step until Phase 4-L.
- **add-profile and operator docs** outside the repo (user-profile skill copies, lab checkouts under `F:\GitHub\...`) will still say MCP Server after the git repos change. Phase 2 and Phase 4-L call that out; cloud PRs cannot see those copies.
- **Public SHA URLs** in issues and receipts will 404 if GitHub redirects are turned off or if the path after the repo name changed because a file moved. Phase 5 samples one URL. Receipts are not mass-rewritten.
- **Database and schema.** Domain tables are not product-named. The Omarchy database name `McpServer_Omarchy` is. Renaming it without a backup breaks the connection string in `/etc/mcpserver/mcpserver.env`. It is out of the first waves.
- **Empty data on upgrade** if code starts looking for `qbrainai.db` or `.qbrainai` instead of `mcp.db` and `.mcpServer`. Those names stay.
- **EF snapshot noise** if `McpDbContext` is renamed. The class name stays.
- **QuadBrain confusion.** Docs that say QBrain.AI and QuadBrain in the same paragraph must keep both terms. Replacing `QuadBrain` with `QBrain.AI` would rename a feature.
- **Requirement traceability** breaks if someone rewrites `FR-MCP-*` ids inside comments while renaming `McpServer`. The inventory class `requirement-id` is leave-as-is.
- **Wiki drift.** Hand-editing `docs/Project/wiki` would diverge from the generator. Live docs change in Phase 1; generated wiki changes only through the existing export path.
- **Stale pipeline paths** (`McpServer.Director`, `McpServer.UI.Core`, `McpServer.Web`) can hide a trigger mistake if they are rewritten as if those projects exist.

## 8. Out of scope

- Historical receipts under `docs/receipts/`, including SessionLife receipts and hostile-validator transcripts.
- Chat logs and stored session-log bodies.
- Unfinished SessionLife work. Do not add SessionLife tasks, tests, or phases to this rebrand.
- Renaming the machine `PAYTON-LEGION2`.
- Renaming the lab `LAB-OMARCHY` or the host label `PAYTON-OMARCHY`.
- Deploying or restarting services on Legion or LAB-OMARCHY during Phases 0 through 4.
- Renaming QuadBrain, QBAgent, or the `qbagent` command.
- Renaming the Model Context Protocol or `/mcp-transport`.
- Renaming `FR-MCP-*`, `TR-MCP-*`, or `TEST-MCP-*`.
- Renaming `mcp.db`, `.mcpServer`, `McpDbContext`, or EF tables in the 1.x rebrand.
- This run: any Phase 1 code rename, any GitHub repository rename, any merge.

## 9. Path and approval

File path: `docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md`.

Dated documents already in `docs/Project` use a Title Case name and a `YYYY-MM-DD` suffix (`Documentation-Audit-2026-05-27.md`, `Dependency-Vulnerability-Audit-2026-04-25.md`). This file follows that pattern. A `YYYY-MM-DD-` prefix is used under `docs/setup/`, not for these project documents. There is no `docs/Project` index of plans to update. The generated wiki documentation index is not a plan catalog and is not edited in the Phase 0 PR.

Approval means a comment from Payton that accepts sections 2, 6, and the phase order, or a short list of edits. After that comment, a follow-up commit changes Status from "Draft pending approval" to "Approved" and records the comment date. Implementation PRs cite this path. They do not start from an unapproved draft.

## 10. Evidence used for the old column

These paths were read or searched while writing the old column. They are not a complete hit list; Phase 1's TSV is that list.

- `McpServer.sln` project names
- `Directory.Build.props` (no product root override; names come from each csproj)
- `src/McpServer.Client/McpServer.Client.csproj`, `src/McpServer.Cqrs/McpServer.Cqrs.csproj`, `src/McpServer.Cqrs.Mvvm/McpServer.Cqrs.Mvvm.csproj`, `src/McpServer.McpAgent/McpServer.McpAgent.csproj`, `src/McpServer.Repl.Core/McpServer.Repl.Core.csproj`, `src/McpServer.Repl.Host/McpServer.Repl.Host.csproj`, `src/McpServer.QBAgent/McpServer.QBAgent.csproj`
- `RootNamespace` exceptions in `src/McpServer.Storage`, `src/McpServer.Services`, `src/McpServer.GraphRag`, and the three `McpServer.Storage.*Migrations` projects
- `Dockerfile`, `docker-compose.mcp.yml` (`image: mcp-server:latest`, container `mcp-server`, network `mcp-network`, volume `mcp-data`)
- `src/McpServer.Support.Mcp/appsettings.yaml` section `Mcp:` and `Repo: McpServerTools`
- `src/McpServer.Support.Mcp/Program.cs` maps `/mcp-transport`
- Controller routes under `src/McpServer.Support.Mcp/Controllers/` use the `mcpserver/...` prefix
- `scripts/Manage-McpService.ps1` and `scripts/Update-McpService.ps1`: service `McpServer`, display name `MCP Server`, install path `C:\ProgramData\McpServer`
- `scripts/Package-McpServerMsix.ps1`: package name `McpServer.Support.Mcp`
- `azure-pipelines.yml`: Octopus project `McpServer`, default URL `http://PAYTON-LEGION2:8066`, deploy to Development
- `docs/setup/install-local-service.ps1`: `/opt/mcpserver`, `/var/lib/mcpserver`, `/etc/mcpserver/mcpserver.env`, user `mcpserver`, unit `mcpserver.service`, database `McpServer_Omarchy`, login `mcpserver_omarchy`
- `packages/mcpserver-agent-core/package.json`, `plugins/core/lib-node/package.json`, `tools/typescript/mcp-repl-ts/package.json`
- `README.md` title, QuadBrain feature paragraph, package `SharpNinja.McpServer.Client`, command `mcpserver-repl`
- `src/McpServer.Storage/McpDbContext.cs` and migration snapshots (`ToTable` names are domain names)
- Tool bucket seed in `src/McpServer.Storage/Migrations/20260309100500_SeedCanonicalToolBucket.cs`
