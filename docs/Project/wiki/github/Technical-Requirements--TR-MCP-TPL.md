# TR-MCP-TPL

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-TPL-001

**Prompt Template YAML Storage** — `PromptTemplateService` persists templates in a single YAML file (default `templates/prompt-templates.yaml`) using YamlDotNet with `HyphenatedNamingConvention`. Root structure: `templates:` → map of template-id → entry object (title, category, tags, description, engine, variables, content). Read/write serialization uses `SemaphoreSlim(1,1)` for write safety. Templates are loaded on-demand and not cached (file is source of truth).
**Covered by:** `PromptTemplateService`, `TemplateStorageOptions`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-002

**Prompt Template Rendering** — `PromptTemplateRenderer` compiles Handlebars templates via `HandlebarsDotNet` with content-hash-based caching in a `ConcurrentDictionary`. Variable validation checks required variables against supplied data and reports missing values. `RenderAsync` returns `PromptTemplateTestResult` with `RenderedContent` on success or `MissingVariables`/`Error` on failure. Thread-safe for concurrent rendering.
**Covered by:** `PromptTemplateRenderer`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-003

**Prompt Template REST + MCP Endpoints** — `PromptTemplateController` exposes 7 REST endpoints at `/mcpserver/templates` (list/filter with query params, CRUD by ID, test stored template, test inline template). `FwhMcpTools` exposes 6 MCP tools (`prompt_template_list`, `prompt_template_get`, `prompt_template_create`, `prompt_template_update`, `prompt_template_delete`, `prompt_template_test`). Both delegate to `IPromptTemplateService`.
**Covered by:** `PromptTemplateController`, `FwhMcpTools`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-004

**Prompt Template CQRS + Director UI** — Full 4-layer CQRS stack: `TemplateMessages.cs` defines queries/commands/results, 6 handlers (`ListTemplatesQueryHandler`, `GetTemplateQueryHandler`, `TestTemplateQueryHandler`, `CreateTemplateCommandHandler`, `UpdateTemplateCommandHandler`, `DeleteTemplateCommandHandler`) delegate to `ITemplateApiClient`. `TemplateApiClientAdapter` bridges to `McpServerClient.Template`. `TemplateListViewModel` and `TemplateDetailViewModel` drive `TemplatesScreen` in Director TUI. Authorization: `McpArea.Templates` with Viewer (read) and Admin (write) roles.
**Covered by:** `TemplateMessages`, `\*TemplateQueryHandler`, `\*TemplateCommandHandler`, `ITemplateApiClient`, `TemplateApiClientAdapter`, `TemplateListViewModel`, `TemplateDetailViewModel`, `TemplatesScreen`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-005

**System Template Externalization** — Three provider interfaces decouple system prompt templates from inline C# constants: (1) `IMarkerPromptProvider` / `FileMarkerPromptProvider` reads `templates/prompt-templates.yaml` via `IPromptTemplateService` (id: `default-marker-prompt`), throwing a critical exception on file-missing. Fallback to `MarkerFileService.DefaultPromptTemplate` is REMOVED. Injected into `WorkspaceProcessManager` with precedence: config override (`Mcp:MarkerPromptTemplate`) > file template. (2) `ITodoPromptProvider` / `TodoPromptProvider` looks up templates from `IPromptTemplateService` by well-known IDs (`todo-status-prompt`, `todo-implement-prompt`, `todo-plan-prompt`), falling back to `TodoPromptDefaults` constants. Injected into `TodoPromptService` with precedence: `IOptionsMonitor<TodoPromptOptions>` > file template > built-in default. (3) `PairingHtmlRenderer` replaces static `PairingHtml` calls with DI-injected instance class, loading templates from `IPromptTemplateService` by well-known IDs (`pairing-login-page`, `pairing-key-page`, `pairing-not-configured-page`) using `string.Replace` token substitution (`{errorBanner}`, `{apiKey}`, `{serverUrl}`), falling back to `PairingHtml` static methods. Template YAML files ship via `.csproj` Content items and are preserved across deployments.
**Covered by:** `IMarkerPromptProvider`, `FileMarkerPromptProvider`, `ITodoPromptProvider`, `TodoPromptProvider`, `PairingHtmlRenderer`, `templates/prompt-templates.yaml`
**Status:** pending
Scope: layer-1+

## TR-MCP-TPL-006

**Template Resolution for One-Shot Requests** — Template rendering SHALL support:
- Explicit template mode: `promptTemplateId` + optional values dictionary.
- Context resolution mode: context-based template selection when template ID is omitted.
- Value precedence: caller-provided values override workspace-context-derived values on key collision.
- Placeholder binding: request `id` injected into render variables for `{id}` substitution.

For `AdHoc` context with no template ID, explicit ad-hoc prompt text is required.

The server SHALL provide a prompt resolution endpoint returning the populated prompt for a given template ID and values dictionary.

**Status:** 🔴 Planned

**Covered by:** `PromptTemplateController` *(planned extension)*, `PromptTemplateRenderer`, `AgentPoolController` *(planned)*
Scope: layer-1+

## TR-MCP-TPL-007

**Marker template requires actionable requirements-backed plans** — The default marker prompt must instruct every agent in every workspace to make plans decision-complete, capture FR/TR/TEST requirements, include explicit TDD unit-test expectations, and preserve Byrd gates so implementation agents can execute the plan directly.
**Covered by:** FR: FR-MCP-104; TEST: TEST-MCP-137, TEST-MCP-141
**Status:** pending
Scope: layer-1+

