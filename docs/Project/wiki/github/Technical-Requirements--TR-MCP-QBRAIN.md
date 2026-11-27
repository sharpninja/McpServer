# TR-MCP-QBRAIN

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-QBRAIN-001

**Locked identifier tokens** — Use one token per surface class. Display text uses `QBrain.AI`. C# namespaces and assemblies use `QBrainAi`. NuGet package ids use `QBrainAI.<Component>` with no `SharpNinja` prefix. The lowercase single token that replaces `mcpserver` is `qbrainai`. Already-hyphenated slugs use `qbrain-ai`. npm scopes use `@qbrainai` and drop `@sharpninja`. Do not mix these forms. Do not replace the `QBAgent` segment or the `qbagent` command.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-003; TEST: TEST-MCP-QBRAIN-001, TEST-MCP-QBRAIN-006, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008, TEST-MCP-QBRAIN-002
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] A review of the rename diff finds no `QBrainAI` and no `QBrain.AI` inside a C# namespace or assembly name, and no `QBrain` product root that dropped `Ai`. `QBrainAI` is allowed only as the NuGet package-id prefix.
- [ ] Plugin repository names and the container image use `qbrain-ai`.
- [ ] The HTTP product prefix, systemd unit, Linux account, and `/opt` path use `qbrainai` when those surfaces are renamed.
- [ ] `QBAgent` and `qbagent` are unchanged apart from a leading `McpServer` root becoming `QBrainAi`.

## TR-MCP-QBRAIN-002

**Namespace, assembly, project, and solution rename** — Replace the root token `McpServer` with `QBrainAi` in namespaces, assembly names, project folders, `InternalsVisibleTo`, and `McpServer.sln`. Keep every segment after the root, including `.Mcp`, `.McpAgent`, and `.QBAgent`. Preserve RootNamespace exceptions: Storage, Services, and GraphRag stay under `QBrainAi.Support.Mcp`; migration projects keep their provider suffix; `tests/Build.Tests` stays `NukeBuild.Tests`; `_build` stays `_build`.
**Covered by:** FR: FR-MCP-QBRAIN-003; TEST: TEST-MCP-QBRAIN-001, TEST-MCP-QBRAIN-002, TEST-MCP-QBRAIN-008
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The solution file and project paths use `QBrainAi` as the root token.
- [ ] No public namespace still starts with `McpServer.`.
- [ ] `QBrainAi.QBAgent` exists and `QBrainAi.QBrainAi` does not.
- [ ] `NukeBuild.Tests` and `_build` are not renamed into `QBrainAi`.

## TR-MCP-QBRAIN-003

**Package and tool migration** — Move published package ids from `SharpNinja.McpServer.<Component>` to `QBrainAI.<Component>` for Client, Cqrs, Cqrs.Mvvm, McpAgent, Repl.Core, Repl, and QBAgent. The new ids do not include a `SharpNinja` prefix. `QBrainAI.QBAgent` is the QBAgent package id. Ship type-forward facades under the old `SharpNinja.McpServer.*` ids through 1.x. Rename `ToolCommandName` `mcpserver-repl` to `qbrain-ai-repl` and keep the old command available from the deprecated package. Rename `@sharpninja/mcpserver-agent-core`, `@sharpninja/mcpserver-plugin-core`, and `@sharpninja/mcp-repl` to `@qbrainai/qbrain-ai-agent-core`, `@qbrainai/qbrain-ai-plugin-core`, and `@qbrainai/qbrain-ai-repl`, and keep a deprecated release on the old `@sharpninja` names through 1.x. Do not push the new ids to nuget.org before Phase 4.
**Covered by:** FR: FR-MCP-QBRAIN-004; TEST: TEST-MCP-QBRAIN-003, TEST-MCP-QBRAIN-005, TEST-MCP-QBRAIN-008
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each new package id packs.
- [ ] Each old package id packs as a type-forward facade that depends on the new package.
- [ ] `qbrain-ai-repl` is the new tool command and `mcpserver-repl` still installs from the deprecated package through 1.x.
- [ ] Phase 1 CI does not push `QBrainAI.*` to nuget.org. New package ids do not start with `SharpNinja`.

## TR-MCP-QBRAIN-004

**Repository rename sequence** — Cloud pull requests land on the current GitHub names. Payton renames repositories only in Phase 3, in this order: `McpServerTools` to `QBrainAiTools`, `McpServerManager` to `QBrainAiManager`, each `mcpserver-*-plugin` to `qbrain-ai-*-plugin`, and `McpServer` to `QBrainAi` last. After each rename, update `RepositoryUrl` and live clone URLs. Tool-bucket rows accept `McpServerTools` and `QBrainAiTools` through 1.x. The data migration that writes `QBrainAiTools` runs after that repository exists and is reversible until 2.0. Do not rewrite historical receipt URLs.
**Covered by:** FR: FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-004, TEST-MCP-QBRAIN-006, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] No cloud phase renames a GitHub repository.
- [ ] Phase 2 pull requests merge under the old repository names.
- [ ] After Phase 3, the old repository URL redirects and the new URL returns the same file.
- [ ] A database row `McpServerTools` still resolves until the migration runs.

## TR-MCP-QBRAIN-005

**Route, config, and environment migration** — Add product HTTP prefix `/qbrainai/*` and keep `/mcpserver/*` through 1.x. Leave `/mcp-transport` unchanged. Configuration root becomes `QBrainAi:`. If only `Mcp:` is present, bind it. If both are present, `QBrainAi:` wins and startup logs one warning. Product `MCP_*` variables listed in the name map are read when the matching `QBRAINAI_*` variable is unset; when both are set, `QBRAINAI_*` wins. Do not rename the sentinel `MCP_UNTRUSTED` through 1.x.
**Covered by:** FR: FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-004; TEST: TEST-MCP-QBRAIN-005, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008, TEST-MCP-QBRAIN-003
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] One representative product route answers on both `/qbrainai/...` and `/mcpserver/...`.
- [ ] `/mcp-transport` is still mapped and is not an alias of the product prefix.
- [ ] Config and environment alias precedence matches the two rules above.
- [ ] `MCP_UNTRUSTED` remains the literal sentinel.

## TR-MCP-QBRAIN-006

**Install path, service, container, and feed migration** — Docker image, container, network, and volume use `qbrain-ai`. Windows service name becomes `QBrainAi`, DisplayName becomes QBrain.AI, and the default install path becomes `C:\ProgramData\QBrainAi`, with an override that can still target the old service and path. MSIX identity becomes `QBrainAi.Support.Mcp`. Octopus project string becomes `QBrainAi` only after that project exists. The pipeline must not call `octopus release deploy` during cloud phases. Linux paths, the `qbrainai` account, and `qbrainai.service` are Phase 4-L text until Payton starts that step. Do not rename database `McpServer_Omarchy` or login `mcpserver_omarchy` in Phases 0 through 4. Do not rename host PAYTON-LEGION2.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-001, TEST-MCP-QBRAIN-006, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008, TEST-MCP-QBRAIN-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Compose and Dockerfile names in git match the `qbrain-ai` forms when Phase 1 edits them.
- [ ] Service script defaults in git match `QBrainAi` and `C:\ProgramData\QBrainAi`, and the old names remain available as parameters.
- [ ] No cloud-phase log shows an Octopus deploy to PAYTON-LEGION2.
- [ ] `McpServer_Omarchy` and `PAYTON-LEGION2` remain in the files that identify that database and that host.

## TR-MCP-QBRAIN-007

**Inventory before edit, and persisted state that stays** — Before any replace, write a classification TSV for the patterns in section 5. Allowed classes and actions are those listed there. Do not rewrite existing files under `docs/receipts/**`. New files are limited to the inventory TSV and to hostile-validation receipts under `docs/receipts/hv/`. Do not hand-edit `docs/Project/wiki/**`. Do not rename `mcp.db`, `.mcpServer`, the class `McpDbContext`, or EF table names. Do not emit a migration whose only effect is a context or table rename.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-001, TEST-MCP-QBRAIN-006, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008, TEST-MCP-QBRAIN-005, TEST-MCP-QBRAIN-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] The TSV exists before the first rename commit of an approved slice, and every hit has a class.
- [ ] No `product-brand` row is still open at the end of the slice that claimed it.
- [ ] `mcp.db`, `.mcpServer`, and `McpDbContext` remain the persisted-state names.
- [ ] The EF model diff for the slice contains no table rename.

## TR-MCP-QBRAIN-008

**BDPv4 slice gate** — An implementation slice starts only after this plan is approved and only for the phase Payton has opened. The first commit of that slice registers the FR and TR ids it cites into the requirements workflow if they are not already stored. The next commit adds failing tests for that slice's acceptance criteria. Production edits come after those tests fail for the intended reason. The slice ends only when its unit scope and prior unit scope report zero failures and zero skips, and only after the hostile-validation checkpoint for that phase returns OverallVerdict AGREE from `gpt-6-astra` at reasoning effort `xhigh`. Operator shorthand `astra-6-xhigh` means that model and that effort. No other model satisfies the checkpoint. SessionLife tests are out of scope. This draft does not open that gate.
**Covered by:** FR: FR-MCP-QBRAIN-001, FR-MCP-QBRAIN-002, FR-MCP-QBRAIN-003, FR-MCP-QBRAIN-004, FR-MCP-QBRAIN-005; TEST: TEST-MCP-QBRAIN-001, TEST-MCP-QBRAIN-006, TEST-MCP-QBRAIN-007, TEST-MCP-QBRAIN-008, TEST-MCP-QBRAIN-005, TEST-MCP-QBRAIN-002, TEST-MCP-QBRAIN-003, TEST-MCP-QBRAIN-004
**Status:** pending
Scope: layer-1+
**Acceptance Criteria:**
- [ ] No rename commit exists on this branch before approval.
- [ ] After approval, a slice cites stored FR and TR ids before it changes product code.
- [ ] The slice receipt shows a red test run before the green run.
- [ ] The exit log shows zero failures and zero skips for the executed unit scope.
- [ ] The phase receipt under `docs/receipts/hv/` records OverallVerdict AGREE from `gpt-6-astra` at effort `xhigh` before anyone marks the phase done.

