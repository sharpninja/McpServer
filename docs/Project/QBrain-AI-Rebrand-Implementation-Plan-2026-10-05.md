# QBrain.AI Rebrand Implementation Plan

- Date: 2026-10-05
- Author context: cloud plan for operator Payton
- Status: Draft pending approval
- Process: this file is the Byrd Development Process v4 Planning artifact (`docs/Development-Process-draft-v4.md`). It is not an implementation slice. Phase 1 is not approved.
- Requirements in this draft: branding and product identity are functional requirements `FR-MCP-QBRAIN-001` through `FR-MCP-QBRAIN-005`. Namespace, repository, package, path, and string renames, and the migration mechanics, are technical requirements `TR-MCP-QBRAIN-001` through `TR-MCP-QBRAIN-008`. These IDs are assigned here and are not yet copied into `docs/Project/Functional-Requirements.md`, `docs/Project/Technical-Requirements.md`, the traceability matrix, or the requirements store.
- Source checkout for the name map: `sharpninja/McpServer` at the `main` commit this plan was written from. Sibling repositories were not cloned in this run.
- This pull request: revise this plan only. It does not rename namespaces, packages, assemblies, routes, or GitHub repositories, and it does not approve or start Phase 1.

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
- Treating this draft as store registration. Copying these FR and TR records into the requirements documents is a later step that waits for Payton's approval. This revision does not approve Phase 1.
- Assigning TEST IDs in this draft. BDPv4 still requires testing requirements before implementation code. After approval, each slice registers its TEST records, then writes failing tests. No `TEST-MCP-QBRAIN-*` id is reserved here.

## 2. BDPv4 planning frame

BDPv4 Planning produces functional requirements, technical requirements, testing requirements, and iterative phases before implementation code (`docs/Development-Process-draft-v4.md`, Planning). This draft completes the FR set, the TR set, and the phase sequence. It does not complete testing requirements and it does not enter Implementation.

Viable and valuable: one product identity, QBrain.AI, is valuable to operators and agents who currently meet several spellings of the same product. The change is viable only if the Model Context Protocol, QuadBrain, QBAgent, historical receipts, and existing requirement IDs stay distinguishable, and if 1.x callers keep working. If either viability or value fails, the scope is wrong and Phase 1 does not start.

Requirement split, which the name map must not collapse:

- Functional requirements say what a person or agent must be able to recognize: the product brand, what is not the product, and what history and compatibility must still show.
- Technical requirements say how a rename or migration is performed: namespaces, repositories, packages, paths, strings, aliases, type-forwards, inventory rules, and the BDPv4 execution gate.
- When an FR and a TR disagree, the FR wins for what must be observable. The TR is corrected. The locked name map changes only if that correction cannot be expressed with the current tokens.

Implementation, after a future approval and not before, follows the BDPv4 cycle inside each phase: write a failing test for the next acceptance criterion, validate it against mocks where the production type is not the subject, make it pass, then refactor. Leaving a slice requires the unit suite for that slice and prior slices to finish with zero failures and zero skips. Deferred work stays in TODO or requirement state. Skipped tests are not a planning device. SessionLife work is not added to make a gate look finished.

Validation is the hostile or regression gate in Phase 5, plus the per-slice unit gate. Deployment in BDPv4 is Development, then Staging, then Production. Cloud phases in this plan stop at reviewable pull requests and package-feed edits. They are not a Staging or Production deploy onto PAYTON-LEGION2 or LAB-OMARCHY. That cutover is Phase 4-L and starts only when Payton says so.

These records are Draft pending approval. Acceptance checkboxes are open. No evidence path is cited as proof of implementation, because nothing has been renamed.

### Functional requirements (branding and product identity)

#### FR-MCP-QBRAIN-001 Product display brand is QBrain.AI

Where this product is named for a person, the display brand is QBrain.AI. That includes the README title, live documentation titles, the Windows service DisplayName, and other human-readable product titles. It does not by itself rename a namespace, a package id, a path, or a repository.

**Status:** Draft pending approval

**Acceptance criteria:**

- [ ] A reader of the live README title sees QBrain.AI.
- [ ] The Windows service DisplayName is QBrain.AI where the service is installed under the new name.
- [ ] Live product titles no longer use "MCP Server" for this product.
- [ ] Historical receipts are not rewritten to satisfy this requirement.

**Satisfied by:** TR-MCP-QBRAIN-001, TR-MCP-QBRAIN-006, TR-MCP-QBRAIN-007

#### FR-MCP-QBRAIN-002 Product identity stays distinct from the Model Context Protocol

The open protocol remains the Model Context Protocol. Protocol wording and the protocol endpoint stay recognizable as MCP. The product rename must not make agents treat `/mcp-transport` or MCP JSON-RPC as a retired product name.

**Status:** Draft pending approval

**Acceptance criteria:**

- [ ] Live docs that describe the wire protocol still say Model Context Protocol where they mean the protocol.
- [ ] `/mcp-transport` remains the MCP endpoint and is not redirected onto a QBrain.AI path.
- [ ] Bare "MCP" in protocol sentences is classified and left in place. It is not bulk-replaced.

**Satisfied by:** TR-MCP-QBRAIN-005, TR-MCP-QBRAIN-007

#### FR-MCP-QBRAIN-003 Product identity stays distinct from QuadBrain and QBAgent

QuadBrain remains the four-role feature. QBAgent remains the agent tool. The command `qbagent` remains `qbagent`. The product brand QBrain.AI must not absorb those names, and those names must not be rewritten into QBrain.AI.

**Status:** Draft pending approval

**Acceptance criteria:**

- [ ] Live docs still describe QuadBrain as the feature and QBrain.AI as the product.
- [ ] The tool command `qbagent` still exists.
- [ ] The namespace segment `QBAgent` remains `QBAgent` after the product root changes.

**Satisfied by:** TR-MCP-QBRAIN-001, TR-MCP-QBRAIN-002

#### FR-MCP-QBRAIN-004 1.x callers keep a working product identity

Through the 1.x line, a caller that still uses the old product route, the old config section, the old product environment variables, or the old NuGet package ids can still operate. At 2.0 those aliases end. This is the continuity requirement. How the aliases are built is a technical requirement.

**Status:** Draft pending approval

**Acceptance criteria:**

- [ ] A 1.x request to the old product HTTP prefix still succeeds for the representative route covered by tests.
- [ ] A 1.x configuration that contains only the old config section still boots.
- [ ] A 1.x package reference to each published `SharpNinja.McpServer.*` id still compiles against the moved public types via the type-forward package.
- [ ] 2.0 is the version where those aliases are allowed to stop. They are not removed inside 1.x.

**Satisfied by:** TR-MCP-QBRAIN-003, TR-MCP-QBRAIN-005

#### FR-MCP-QBRAIN-005 Historical identity and host names stay

People can still read historical receipts, existing requirement IDs, and the host names PAYTON-LEGION2 and LAB-OMARCHY as they were written. The rebrand does not rewrite that history and does not rename those machines or that lab.

**Status:** Draft pending approval

**Acceptance criteria:**

- [ ] Existing files under `docs/receipts/**` are not rewritten. New inventory and hostile-validation receipts may be added. They do not replace historical receipt bodies.
- [ ] Existing `FR-MCP-*`, `TR-MCP-*`, and `TEST-MCP-*` IDs are not renamed. The new IDs in this plan are additions.
- [ ] The hostname PAYTON-LEGION2 and the lab name LAB-OMARCHY still appear where they identify that host and that lab.

**Satisfied by:** TR-MCP-QBRAIN-007, TR-MCP-QBRAIN-004, TR-MCP-QBRAIN-006

### Technical requirements (rename and migration)

#### TR-MCP-QBRAIN-001 Locked identifier tokens

Use one token per surface class. Display text uses `QBrain.AI`. PascalCase identifiers use `QBrainAi`. The lowercase single token that replaces `mcpserver` is `qbrainai`. Already-hyphenated slugs use `qbrain-ai`. NuGet ids use `SharpNinja.QBrainAi.<Component>`. Do not mix these forms. Do not replace the `QBAgent` segment or the `qbagent` command.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-003

**Acceptance criteria:**

- [ ] A review of the rename diff finds no `QBrainAI`, no `QBrain.AI` inside a C# namespace, and no `QBrain` product root that dropped `Ai`.
- [ ] Plugin repository names and the container image use `qbrain-ai`.
- [ ] The HTTP product prefix, systemd unit, Linux account, and `/opt` path use `qbrainai` when those surfaces are renamed.
- [ ] `QBAgent` and `qbagent` are unchanged apart from a leading `McpServer` root becoming `QBrainAi`.

#### TR-MCP-QBRAIN-002 Namespace, assembly, project, and solution rename

Replace the root token `McpServer` with `QBrainAi` in namespaces, assembly names, project folders, `InternalsVisibleTo`, and `McpServer.sln`. Keep every segment after the root, including `.Mcp`, `.McpAgent`, and `.QBAgent`. Preserve RootNamespace exceptions: Storage, Services, and GraphRag stay under `QBrainAi.Support.Mcp`; migration projects keep their provider suffix; `tests/Build.Tests` stays `NukeBuild.Tests`; `_build` stays `_build`.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-003

**Acceptance criteria:**

- [ ] The solution file and project paths use `QBrainAi` as the root token.
- [ ] No public namespace still starts with `McpServer.`.
- [ ] `QBrainAi.QBAgent` exists and `QBrainAi.QBrainAi` does not.
- [ ] `NukeBuild.Tests` and `_build` are not renamed into `QBrainAi`.

#### TR-MCP-QBRAIN-003 Package and tool migration

Move published package ids from `SharpNinja.McpServer.<Component>` to `SharpNinja.QBrainAi.<Component>` for Client, Cqrs, Cqrs.Mvvm, McpAgent, Repl.Core, Repl, and QBAgent. Ship type-forward facades under the old ids through 1.x. Rename `ToolCommandName` `mcpserver-repl` to `qbrain-ai-repl` and keep the old command available from the deprecated package. Rename `@sharpninja/mcpserver-agent-core`, `@sharpninja/mcpserver-plugin-core`, and `@sharpninja/mcp-repl` to the `qbrain-ai` npm names, with a deprecated old name through 1.x. Do not push the new ids to nuget.org before Phase 4.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-004

**Acceptance criteria:**

- [ ] Each new package id packs.
- [ ] Each old package id packs as a type-forward facade that depends on the new package.
- [ ] `qbrain-ai-repl` is the new tool command and `mcpserver-repl` still installs from the deprecated package through 1.x.
- [ ] Phase 1 CI does not push `SharpNinja.QBrainAi.*` to nuget.org.

#### TR-MCP-QBRAIN-004 Repository rename sequence

Cloud pull requests land on the current GitHub names. Payton renames repositories only in Phase 3, in this order: `McpServerTools` to `QBrainAiTools`, `McpServerManager` to `QBrainAiManager`, each `mcpserver-*-plugin` to `qbrain-ai-*-plugin`, and `McpServer` to `QBrainAi` last. After each rename, update `RepositoryUrl` and live clone URLs. Tool-bucket rows accept `McpServerTools` and `QBrainAiTools` through 1.x. The data migration that writes `QBrainAiTools` runs after that repository exists and is reversible until 2.0. Do not rewrite historical receipt URLs.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-005

**Acceptance criteria:**

- [ ] No cloud phase renames a GitHub repository.
- [ ] Phase 2 pull requests merge under the old repository names.
- [ ] After Phase 3, the old repository URL redirects and the new URL returns the same file.
- [ ] A database row `McpServerTools` still resolves until the migration runs.

#### TR-MCP-QBRAIN-005 Route, config, and environment migration

Add product HTTP prefix `/qbrainai/*` and keep `/mcpserver/*` through 1.x. Leave `/mcp-transport` unchanged. Configuration root becomes `QBrainAi:`. If only `Mcp:` is present, bind it. If both are present, `QBrainAi:` wins and startup logs one warning. Product `MCP_*` variables listed in the name map are read when the matching `QBRAINAI_*` variable is unset; when both are set, `QBRAINAI_*` wins. Do not rename the sentinel `MCP_UNTRUSTED` through 1.x.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-004

**Acceptance criteria:**

- [ ] One representative product route answers on both `/qbrainai/...` and `/mcpserver/...`.
- [ ] `/mcp-transport` is still mapped and is not an alias of the product prefix.
- [ ] Config and environment alias precedence matches the two rules above.
- [ ] `MCP_UNTRUSTED` remains the literal sentinel.

#### TR-MCP-QBRAIN-006 Install path, service, container, and feed migration

Docker image, container, network, and volume use `qbrain-ai`. Windows service name becomes `QBrainAi`, DisplayName becomes QBrain.AI, and the default install path becomes `C:\ProgramData\QBrainAi`, with an override that can still target the old service and path. MSIX identity becomes `QBrainAi.Support.Mcp`. Octopus project string becomes `QBrainAi` only after that project exists. The pipeline must not call `octopus release deploy` during cloud phases. Linux paths, the `qbrainai` account, and `qbrainai.service` are Phase 4-L text until Payton starts that step. Do not rename database `McpServer_Omarchy` or login `mcpserver_omarchy` in Phases 0 through 4. Do not rename host PAYTON-LEGION2.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-005

**Acceptance criteria:**

- [ ] Compose and Dockerfile names in git match the `qbrain-ai` forms when Phase 1 edits them.
- [ ] Service script defaults in git match `QBrainAi` and `C:\ProgramData\QBrainAi`, and the old names remain available as parameters.
- [ ] No cloud-phase log shows an Octopus deploy to PAYTON-LEGION2.
- [ ] `McpServer_Omarchy` and `PAYTON-LEGION2` remain in the files that identify that database and that host.

#### TR-MCP-QBRAIN-007 Inventory before edit, and persisted state that stays

Before any replace, write a classification TSV for the patterns in section 5. Allowed classes and actions are those listed there. Do not rewrite existing files under `docs/receipts/**`. New files are limited to the inventory TSV and to hostile-validation receipts under `docs/receipts/hv/`. Do not hand-edit `docs/Project/wiki/**`. Do not rename `mcp.db`, `.mcpServer`, the class `McpDbContext`, or EF table names. Do not emit a migration whose only effect is a context or table rename.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-005

**Acceptance criteria:**

- [ ] The TSV exists before the first rename commit of an approved slice, and every hit has a class.
- [ ] No `product-brand` row is still open at the end of the slice that claimed it.
- [ ] `mcp.db`, `.mcpServer`, and `McpDbContext` remain the persisted-state names.
- [ ] The EF model diff for the slice contains no table rename.

#### TR-MCP-QBRAIN-008 BDPv4 slice gate

An implementation slice starts only after this plan is approved and only for the phase Payton has opened. The first commit of that slice registers the FR and TR ids it cites into the requirements workflow if they are not already stored. The next commit adds failing tests for that slice's acceptance criteria. Production edits come after those tests fail for the intended reason. The slice ends only when its unit scope and prior unit scope report zero failures and zero skips, and only after the hostile-validation checkpoint for that phase returns OverallVerdict AGREE from `gpt-6-astra` at reasoning effort `xhigh`. Operator shorthand `astra-6-xhigh` means that model and that effort. No other model satisfies the checkpoint. SessionLife tests are out of scope. This draft does not open that gate.

**Status:** Draft pending approval

**Covers:** FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-003, FR-MCP-QBRAIN-004, FR-MCP-QBRAIN-005

**Acceptance criteria:**

- [ ] No rename commit exists on this branch before approval.
- [ ] After approval, a slice cites stored FR and TR ids before it changes product code.
- [ ] The slice receipt shows a red test run before the green run.
- [ ] The exit log shows zero failures and zero skips for the executed unit scope.
- [ ] The phase receipt under `docs/receipts/hv/` records OverallVerdict AGREE from `gpt-6-astra` at effort `xhigh` before anyone marks the phase done.

### Trace map

- FR-MCP-QBRAIN-001 is implemented by TR-MCP-QBRAIN-001, TR-MCP-QBRAIN-006, and TR-MCP-QBRAIN-007.
- FR-MCP-QBRAIN-002 is implemented by TR-MCP-QBRAIN-005 and TR-MCP-QBRAIN-007.
- FR-MCP-QBRAIN-003 is implemented by TR-MCP-QBRAIN-001 and TR-MCP-QBRAIN-002.
- FR-MCP-QBRAIN-004 is implemented by TR-MCP-QBRAIN-003 and TR-MCP-QBRAIN-005.
- FR-MCP-QBRAIN-005 is implemented by TR-MCP-QBRAIN-004, TR-MCP-QBRAIN-006, and TR-MCP-QBRAIN-007.
- TR-MCP-QBRAIN-008 is the execution gate for every FR above. It does not add behavior of its own.

## 3. Locked naming convention

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

## 4. Canonical name map

Old values were read from this checkout. "New" is the target a later approval would use. Alias behavior is FR-MCP-QBRAIN-004, implemented in section 7. Do not apply the new column in this PR.

The map is a lookup, not a second requirements list. Display and title rows are FR-MCP-QBRAIN-001. Rows that stay because they are the protocol are FR-MCP-QBRAIN-002. Rows that stay because they are QuadBrain or QBAgent are FR-MCP-QBRAIN-003. Rows that stay because they are history or host names are FR-MCP-QBRAIN-005. Every old-to-new namespace, repository, package, path, and product string is a technical rename under TR-MCP-QBRAIN-001 through TR-MCP-QBRAIN-007.

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

## 5. Inventory method

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
- `ambiguous`: do not edit until the disposition in section 5.1 is applied.

Exclude from automated replace, even when a pattern matches:

- `docs/receipts/**`
- `docs/Project/wiki/**` until an explicit regenerate step
- vendored `lib/NSubstitute/**`
- historical probe projects under `docs/receipts/**` such as `FailClosedProbe.csproj` and the copied `_build.csproj` under `docs/receipts/sessionlife-completion/**`

The classifier is a dry run. Its TSV is the Phase 1 review artifact. A second person or a hostile pass can reject a row before edits. String replacement is not allowed to be a blind `McpServer` → `QBrainAi` substitute.

### 5.1 Ambiguous cases

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

## 6. Phased execution

Phases are the BDPv4 iterative breakdown of TR-MCP-QBRAIN-008. They are sequential. A phase does not start until the previous phase's acceptance criteria pass and Payton has approved that gate. This draft opens Phase 0 only. Phase 1 is written here so the approval can see it, and Phase 1 is not approved. Execution is cloud-only through Phase 4. No SSH, WinRM, Octopus deploy, systemd, or filesystem change on PAYTON-LEGION2 or LAB-OMARCHY.

After approval, work follows TR-MCP-QBRAIN-008: acceptance tests for the slice go red first, then implementation, then refactor, and the unit suite for the current and previous iterations finishes with zero failures and zero skips. Deferred behavior is a requirement or TODO, not a skipped test. Do not add SessionLife tests to these slices.

Hostile validation is a done-gate on every phase, including Phase 0 and Phase 4-L. Passing the phase acceptance list does not mark the phase done. Done requires a receipt from Codex running `gpt-6-astra` at reasoning effort `xhigh` (`astra-6-xhigh`). Grok, a Cursor cloud agent, or any other model is not that checkpoint. A missing receipt, a NOT RUN receipt, or OverallVerdict DISAGREE leaves the phase not done. The receipt path is `docs/receipts/hv/<yyyyMMddTHHmmssZ>-qbrain-ai-rebrand-phase-<phase>-hv.md` plus the request and response jsonl from that Codex run. The receipt records OverallVerdict, Accuracy, and Completeness. Phase 5 product regression checks stay. They do not replace this model-locked checkpoint.

### Phase 0 — this plan and the approval gate

Requirements: this phase records FR-MCP-QBRAIN-001 through FR-MCP-QBRAIN-005 and TR-MCP-QBRAIN-001 through TR-MCP-QBRAIN-008. It does not implement them.

Work: this markdown file and a draft PR on `sharpninja/McpServer`. No other product edits. `docs/Project` has no index that lists plans. `docs/Project/wiki/*/Documentation-Index.md` is generated wiki output and is not hand-edited here. The canonical requirement documents are not updated in this phase.

Gate: Payton approves or rejects the FR set, the TR set, the locked convention, the alias duration (through 1.x, removed at 2.0), the repo rename list, and the rule that Legion and LAB-OMARCHY deploys wait for Phase 4-L. Approval of this gate is not approval of Phase 1.

Rollback: close the draft PR. No runtime state changed.

Acceptance:

- The product diff is this plan plus new files under `docs/receipts/hv/` for the plan review. No namespace, package, or repository rename is in the diff.
- The file contains the five FR records and eight TR records in section 2, and the trace map matches those ids.
- The name map matches the old identifiers cited from this repo, or the PR discussion corrects a cited identifier before approval.
- Status remains Draft pending approval until Payton's approval comment. That comment is still Phase 0. It does not authorize namespace, package, or repository renames.
- Phases 0, 1, 2, 3, 4, 4-L, and 5 each contain a hostile-validation checkpoint that names `gpt-6-astra` and effort `xhigh`.

#### Hostile validation checkpoint (required before Phase 0 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Subject: this plan file, not a product rename.
- Claims the reviewer scores: status is still Draft pending approval; Phase 1 is not approved; FR-MCP-QBRAIN-001 through 005 are branding and identity; TR-MCP-QBRAIN-001 through 008 are rename and migration; the name map in section 4 matches section 3; every phase below has its own checkpoint with the same model lock; this cloud agent's receipt is not itself an AGREE.
- Done rule: Phase 0 is not done until that run's receipt says OverallVerdict AGREE. Payton's approval comment does not replace the receipt.

### Phase 1 — McpServer code, tests, and live docs (cloud PR series on the current repo)

Not approved. Do this only after a later Phase 0 approval that explicitly opens Phase 1. The requirements are TR-MCP-QBRAIN-002, TR-MCP-QBRAIN-003 (facades built, not published), TR-MCP-QBRAIN-005, TR-MCP-QBRAIN-007, and the live-doc half of FR-MCP-QBRAIN-001. Land as sequential PRs on the existing `McpServer` repository, not on a renamed remote. TR-MCP-QBRAIN-008 requires the cited ids to be stored before product code changes.

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
- `./build.ps1 ValidateConfig` and `./build.ps1 ValidateTraceability` pass. New public APIs have XML docs. Before product renames, the slice registers FR-MCP-QBRAIN-001 through FR-MCP-QBRAIN-005 and TR-MCP-QBRAIN-001 through TR-MCP-QBRAIN-008 through the requirements workflow if approval has stored them. This draft does not register them and does not start that slice.
- `/mcp-transport` still maps, and a test covers that the path string did not change.
- A test covers `GET` or the existing health/todo route on both `/qbrainai/...` and `/mcpserver/...` for one representative controller, plus the config binder accepting `Mcp:` when `QBrainAi:` is absent and preferring `QBrainAi:` when both are present.
- `mcp.db`, `.mcpServer`, and `McpDbContext` still appear as the persisted-state names. A test or snapshot diff shows no EF table rename migration from this phase.
- `rg` over the inventory TSV shows zero `product-brand` rows still marked open.
- `git diff` against Phase 0 does not modify `docs/receipts/` except the new inventory TSV.
- `PAYTON-LEGION2` and `McpServer_Omarchy` still occur where they are host and database identities.
- NuGet push log for this phase, if CI runs, shows the push step skipped or `--skip-duplicate` on unchanged old IDs only. No `SharpNinja.QBrainAi.*` package is on nuget.org yet.
- New receipts under `docs/receipts/hv/` for this phase do not rewrite older receipt bodies.

#### Hostile validation checkpoint (required before Phase 1 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Claims the reviewer scores against the merged Phase 1 tree: root namespaces are `QBrainAi` with `.Mcp`, `.McpAgent`, and `.QBAgent` segments kept; `/mcp-transport` is unchanged; `/mcpserver/*` still answers; `QBrainAi:` wins when both config sections exist; `mcp.db`, `.mcpServer`, and `McpDbContext` are unchanged; unit scope is zero failures and zero skips; nuget.org has no `SharpNinja.QBrainAi.*` package from this phase; `PAYTON-LEGION2` was not deployed.
- Done rule: Phase 1 is not done until that receipt says OverallVerdict AGREE. This draft does not start Phase 1.

### Phase 2 — one cloud PR per sibling repo

Not approved. Requirements: TR-MCP-QBRAIN-004 (pull requests still use the old repository names), TR-MCP-QBRAIN-007 in each sibling tree, and FR-MCP-QBRAIN-001 for display strings. Start only after Phase 1 acceptance. Each PR merges to that repo's default branch under the **old** GitHub name. Remote rename is Phase 3.

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

#### Hostile validation checkpoint (required before Phase 2 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Claims the reviewer scores: one merged PR per in-scope sibling repo that exists; pull requests used the old GitHub names; `McpServerTools` still contains the old `mcpserver-*-plugin` manifest names; QuadBrain and `qbagent` were not renamed; a missing `mcpserver-grok-bot-plugin` remote is recorded rather than invented.
- Done rule: Phase 2 is not done until that receipt says OverallVerdict AGREE. A missing repo stops only that slice.

### Phase 3 — GitHub repository renames and remote URL updates

Not approved. This phase implements TR-MCP-QBRAIN-004. It is operator-started. Cloud agents do not rename GitHub repositories. Payton renames, in order:

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

#### Hostile validation checkpoint (required before Phase 3 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Claims the reviewer scores: rename order was Tools, Manager, plugins, then `McpServer` last; the operator performed the GitHub renames; old repo URLs redirect; live clone URLs use the new names; tool-bucket rows still accept `McpServerTools` until the reversible migration runs; historical receipt URL text was not rewritten; the hostname `PAYTON-LEGION2` is unchanged.
- Done rule: Phase 3 is not done until that receipt says OverallVerdict AGREE. A cloud agent renaming a GitHub repository fails this checkpoint.

### Phase 4 — package feeds, Octopus project string, deploy scripts (cloud edits; no host deploy)

Not approved. This phase implements the publish half of TR-MCP-QBRAIN-003 and the in-git half of TR-MCP-QBRAIN-006. Cloud PRs may edit feed and script text in git. They do not install services, create Octopus releases, or push to a machine.

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

#### Hostile validation checkpoint (required before Phase 4 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Claims the reviewer scores: nuget.org has `SharpNinja.QBrainAi.*` and type-forward builds of the old ids; `qbrain-ai-repl` and `mcpserver-repl` both install; script defaults in git use the new service name and path and still accept the old parameters; no Phase 4 command deployed to PAYTON-LEGION2 or LAB-OMARCHY; `McpServer_Omarchy` is unchanged.
- Done rule: Phase 4 is not done until that receipt says OverallVerdict AGREE. Package publish without this receipt is not phase completion.

### Phase 4-L — later operator step (not in the first waves)

Not approved. This phase is the host half of TR-MCP-QBRAIN-006. Payton starts it explicitly. It is not scheduled by merging Phase 4.

- Windows: stop service `McpServer`, install service `QBrainAi` at `C:\ProgramData\QBrainAi`, copy preserved state (`mcp.db`, keys, `appsettings` secrets) from `C:\ProgramData\McpServer`. Keep the old directory until the new service passes `/health`.
- Octopus: rename or recreate project `QBrainAi`, then allow the pipeline step to create a release. Target names that are the machine `PAYTON-LEGION2` stay.
- LAB-OMARCHY, only under a separate approval: unit `qbrainai.service`, paths `/opt/qbrainai`, `/var/lib/qbrainai`, `/etc/qbrainai`, account `qbrainai`. Database `McpServer_Omarchy` and login `mcpserver_omarchy` stay unless Payton approves a database rename in writing. A database rename is a backup, detach/attach or copy, and connection-string cutover, not a find-and-replace.
- Operator profile copies of add-profile and any machine-local plugin checkouts are updated by Payton. They are not pushed from the cloud agent.

Acceptance:

- `/health` on the new Windows service returns success and the nonce echo still works.
- The previous install directory still exists until Payton deletes it.
- `PAYTON-LEGION2` as a hostname still resolves to the same machine.
- LAB-OMARCHY is unchanged unless this phase's separate approval includes it.

#### Hostile validation checkpoint (required before Phase 4-L is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- Claims the reviewer scores: the new Windows service answers `/health`; `C:\ProgramData\McpServer` still exists until Payton deletes it; the hostname is still `PAYTON-LEGION2`; LAB-OMARCHY changed only if a separate written approval names that host; database `McpServer_Omarchy` was not renamed unless that same approval says so.
- Done rule: Phase 4-L is not done until that receipt says OverallVerdict AGREE. Merging Phase 4 does not open this checkpoint.

### Phase 5 — verification and hostile gate

Not approved. This phase is the BDPv4 validation gate for FR-MCP-QBRAIN-001 through FR-MCP-QBRAIN-005. Run after Phase 4's cloud edits merge. Re-run the parts that touch a host only after Phase 4-L if that step happened.

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

#### Hostile validation checkpoint (required before Phase 5 is done)

- Model lock: `gpt-6-astra`, reasoning effort `xhigh` (`astra-6-xhigh`).
- This checkpoint reviews the Phase 5 command receipt. It is not a substitute for those commands, and those commands are not a substitute for this checkpoint.
- Claims the reviewer scores: every Phase 5 check has a command and an output path; failed and skipped counts are zero; `/mcp-transport` was not retargeted; old and new product prefixes both answered; QuadBrain, `qbagent`, existing `FR-MCP-` ids, and `PAYTON-LEGION2` remain; historical receipt bodies match the pinned pre-rebrand SHA except added inventory and HV files.
- Done rule: Phase 5 is not done, and the rebrand is not done, until that receipt says OverallVerdict AGREE.

## 7. Compatibility policy

This section is the migration mechanic for FR-MCP-QBRAIN-004. TR-MCP-QBRAIN-003 and TR-MCP-QBRAIN-005 own the package, route, config, and environment aliases. TR-MCP-QBRAIN-007 owns the persisted names that are not aliased because they do not change.

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

## 8. Risks

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

## 9. Out of scope

- Historical receipts under `docs/receipts/`, including SessionLife receipts and hostile-validator transcripts.
- Chat logs and stored session-log bodies.
- Unfinished SessionLife work. Do not add SessionLife tasks, tests, or phases to this rebrand.
- Renaming the machine `PAYTON-LEGION2`.
- Renaming the lab `LAB-OMARCHY` or the host label `PAYTON-OMARCHY`.
- Deploying or restarting services on Legion or LAB-OMARCHY during Phases 0 through 4.
- Renaming QuadBrain, QBAgent, or the `qbagent` command.
- Renaming the Model Context Protocol or `/mcp-transport`.
- Renaming existing `FR-MCP-*`, `TR-MCP-*`, or `TEST-MCP-*` ids. The QBRAIN ids in section 2 are new draft records, not renames of those ids.
- Renaming `mcp.db`, `.mcpServer`, `McpDbContext`, or EF tables in the 1.x rebrand.
- This run: any Phase 1 code rename, any GitHub repository rename, any merge.

## 10. Path and approval

File path: `docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md`.

Dated documents already in `docs/Project` use a Title Case name and a `YYYY-MM-DD` suffix (`Documentation-Audit-2026-05-27.md`, `Dependency-Vulnerability-Audit-2026-04-25.md`). This file follows that pattern. A `YYYY-MM-DD-` prefix is used under `docs/setup/`, not for these project documents. There is no `docs/Project` index of plans to update. The generated wiki documentation index is not a plan catalog and is not edited in the Phase 0 PR.

Approval means a comment from Payton that accepts section 2 (the FR and TR records), section 3 (the locked tokens), section 7 (the alias duration), and the phase order, or a short list of edits. This revision is not that approval. Status stays Draft pending approval. After that comment, a follow-up commit changes Status to Approved and records the comment date. That commit still does not start Phase 1 unless the comment opens Phase 1. Implementation PRs cite this path and the FR and TR ids. They do not start from an unapproved draft.

## 11. Evidence used for the old column

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
