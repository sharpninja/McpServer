# Director Requirements

This document tracks functional and technical requirements for the `McpServer.Director` CLI and TUI application.

## Functional Requirements

### FR-MCP-030 Director CLI

A console application (`McpServer.Director`) shall provide agent orchestration commands (init, add, launch, ban, unban, delete, merge, login, list, agents, validate, interactive) dispatched through the CQRS framework. Authentication uses OIDC Device Authorization Flow with the configured provider. Interactive mode uses Terminal.Gui v2 with ViewModel-bound screens.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project — 15 source files: `Program.cs`, `McpHttpClient.cs`, `Auth/DirectorAuthOptions.cs`, `Auth/OidcAuthService.cs`, `Auth/TokenCache.cs`, `Commands/AuthCommands.cs`, `Commands/CommandHelpers.cs`, `Commands/DirectorCommands.cs`, `Commands/InteractiveCommand.cs`, `Screens/MainScreen.cs`, `Screens/HealthScreen.cs`, `Screens/AgentScreen.cs`, `Screens/TodoScreen.cs`, `Screens/SessionLogScreen.cs`, `Screens/WorkspaceListScreen.cs`, `Screens/WorkspacePolicyScreen.cs`, `Screens/LoginDialog.cs`, `Screens/ViewModelBinder.cs`

**Implementation:** 17 CLI commands registered via System.CommandLine. All commands communicate with the MCP server via `McpHttpClient` (reads connection details from `AGENTS-README-FIRST.yaml`). Auth uses OIDC Device Authorization Flow with token caching to `~/.mcpserver/tokens.json`. Interactive mode (`director interactive|tui|ui`) launches Terminal.Gui v2 with 6 tabs (Health, Workspaces, Agents, TODO, Sessions, Policy) plus a Login dialog, menu bar, auth status indicator, and keyboard shortcuts (F2 Login, F5 Refresh, Ctrl+Q Quit). ViewModels from `McpServer.UI.Core` are bound to Terminal.Gui controls via `ViewModelBinder` (INotifyPropertyChanged → Application.Invoke).

### FR-MCP-037 Director CLI Exec Command

The Director CLI shall support a `director exec <ViewModelName>` command that instantiates the named ViewModel from the registry, populates properties from JSON input (stdin or `--input` flag), executes the primary `IRelayCommand`, and returns the result as JSON to stdout. Exit code 0 = success, 1 = failure.

**Covered by:** `McpServer.Director` project, `IViewModelRegistry`

### FR-MCP-057 Director Agent Pool Management UI

Director shall provide an Agent Pool tab to monitor pooled agents and one-shot queue state, connect to an agent, recycle an agent immediately, stop/start an agent, cancel/remove/reorder queued requests, and enqueue free-form one-shot requests.

**Covered by:** `AgentPoolScreen` *(planned)*, `AgentPoolViewModel` *(planned)*

### FR-MCP-060 Director MVVM/CQRS Full Endpoint Coverage

Director SHALL expose complete administrative endpoint coverage through the shared `McpServer.UI.Core` MVVM/CQRS layer so interactive tabs and `director exec` operations use the same command/query contracts, handlers, and authorization rules.

Each covered administration area SHALL provide ViewModel-first orchestration (list/detail or operation-focused ViewModel patterns), and Director screens SHALL remain presentation-only shells that delegate state and workflows to ViewModels and CQRS dispatch.

Tab composition SHALL be role-aware and declarative, with registration metadata separated from shell rendering logic and enforced via shared authorization policy checks.

**Technical Implementation:** [TR-MCP-DIR-005](./Requirements-Director.md#tr-mcp-dir-005) | [TR-MCP-DIR-006](./Requirements-Director.md#tr-mcp-dir-006) | [TR-MCP-DIR-007](./Requirements-Director.md#tr-mcp-dir-007) | [TR-MCP-DIR-008](./Requirements-Director.md#tr-mcp-dir-008)

## Technical Requirements

### TR-MCP-DIR-001

**Director Console App with CQRS** — `McpServer.Director` console application using `System.CommandLine` for CLI parsing and `McpServer.Cqrs` for all action dispatch. CLI commands: `health`, `list`, `agents` (defs/ws/events), `add`, `ban`, `unban`, `delete`, `validate`, `init`, `sync` (status/run), `todo`, `session-log`, `login`, `logout`, `whoami`, `interactive` (aliases: `tui`, `ui`), `exec`, `list-viewmodels`. Interactive mode uses Terminal.Gui v2 with 7 tabbed screens (Health, Workspaces, Agents, TODO, Sessions, Sync, Policy) plus LoginDialog, menu bar, auth status indicator, and keyboard shortcuts (F2 Login, F5 Refresh, Ctrl+Q Quit).

**Status:** ✅ Complete — 18 CLI commands, 9 Terminal.Gui screens, solution builds with 0 warnings

**Covered by:** `McpServer.Director` project (`Program.cs`, `DirectorCommands.cs`, `AuthCommands.cs`, `InteractiveCommand.cs`, `McpHttpClient.cs`, `MainScreen.cs`, `HealthScreen.cs`, `AgentScreen.cs`, `TodoScreen.cs`, `SessionLogScreen.cs`, `SyncScreen.cs`, `WorkspaceListScreen.cs`, `WorkspacePolicyScreen.cs`, `LoginDialog.cs`, `ViewModelBinder.cs`)

### TR-MCP-DIR-002

**Director OIDC Authentication** — `OidcAuthService` implements OIDC Device Authorization Flow against the configured provider. Initiates device flow, displays user code and verification URI, polls for token. Tokens cached to `~/.mcpserver/tokens.json` via `TokenCache`. `McpHttpClient.TrySetCachedBearerToken()` loads cached tokens on startup. CLI commands: `login`, `logout`, `whoami`. TUI: `LoginDialog` with Device Flow UI, authority/client-id fields, user code display, polling status, and whoami frame. Token includes `sub`, `preferred_username`, `email`, `realm_roles` claims.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project (`Auth/OidcAuthService.cs`, `Auth/TokenCache.cs`, `Auth/DirectorAuthOptions.cs`, `Commands/AuthCommands.cs`, `Screens/LoginDialog.cs`)

### TR-MCP-DIR-003

**Director Exec Command** — `director exec <ViewModelName>` CLI command. `IViewModelRegistry` maps ViewModel names/aliases to types. `ExecCliCommand` resolves ViewModel from DI, deserializes JSON input to properties via `System.Text.Json`, executes primary `IRelayCommand`, serializes `Result<T>` to JSON stdout. `[ViewModelCommand("alias")]` attribute for CLI aliases. Exit code 0/1 maps to Result success/failure.

**Status:** ✅ Complete

**Covered by:** `McpServer.Director` project (`Program.cs` exec/list-viewmodels commands), `McpServer.UI.Core` (`IViewModelRegistry`)

### TR-MCP-DIR-004

**Director Agent Pool Tab and Queue Controls** — Director interactive UI SHALL include an Agent Pool tab that renders pooled agent status, default-intent assignments, active work metadata, queue state, and notification events.

Tab actions SHALL include connect, immediate recycle, stop/start, queued-item move up/down (queued items only), cancel/remove, and free-form one-shot enqueue.

**Status:** 🔴 Planned

**Covered by:** `AgentPoolScreen` *(planned)*, `AgentPoolViewModel` *(planned)*, `McpHttpClient` *(planned extension)*

### TR-MCP-DIR-005

**Director Endpoint-to-Handler Coverage Contract** — Every Director-administered MCP endpoint in covered areas SHALL be represented by a UI.Core command/query message and a corresponding CQRS handler that delegates to a UI.Core API-client abstraction (`I*ApiClient`) rather than direct screen-level HTTP calls.

Director non-interactive command paths (`director` CLI commands and `director exec`) SHALL dispatch through the same CQRS handler layer used by interactive tabs to prevent duplicate business logic.

**Status:** ✅ Complete

**Covered by:** `McpServer.UI.Core/Messages/*Messages.cs`, `McpServer.UI.Core/Handlers/*Handlers.cs`, `McpServer.Director/*ApiClientAdapter.cs`, `McpServer.Director/Commands/DirectorCommands.cs`, `McpServer.Director/Commands/AuthCommands.cs`

### TR-MCP-DIR-006

**Director ViewModel Conventions for Area Workflows** — Covered administration areas SHALL expose ViewModel orchestration that owns UI-facing state (`Items`, `Detail`, `IsLoading/IsBusy`, `StatusMessage`, `ErrorMessage`) and uses `Dispatcher` for command/query execution.

List/detail areas SHALL follow `AreaListViewModelBase<T>` / `AreaDetailViewModelBase<TDetail>` conventions where applicable; operation-centric areas may use focused `ObservableObject` ViewModels with explicit async workflow methods.

**Status:** ✅ Complete

**Covered by:** `McpServer.UI.Core/ViewModels/*ViewModel.cs`, `McpServer.UI.Core/ViewModels/Base/AreaListViewModelBase.cs`, `McpServer.UI.Core/ViewModels/Base/AreaDetailViewModelBase.cs`, `McpServer.Director/Screens/*Screen.cs`

### TR-MCP-DIR-007

**Director RBAC Visibility and Action Gating** — Tab visibility and action execution SHALL be enforced through `IAuthorizationPolicyService` using normalized `McpArea` and `McpActionKeys` contracts with role tiers (`viewer`, `agent-manager`, `admin`).

Viewer-level users SHALL retain read access surfaces while admin-only surfaces (for example workspaces/policy mutation) remain hidden or blocked unless role requirements are satisfied.

**Status:** ✅ Complete

**Covered by:** `McpServer.UI.Core/Authorization/McpArea.cs`, `McpServer.UI.Core/Authorization/McpActionKeys.cs`, `McpServer.Director/Auth/DirectorAuthorizationPolicyService.cs`, `McpServer.Director/Screens/MainScreen.cs`, `McpServer.UI.Core/Handlers/*Handlers.cs`

### TR-MCP-DIR-008

**Declarative Director Tab Registry** — Director tab metadata SHALL be registered through a dedicated registry contract that captures area, caption, required role metadata, screen factory, and optional availability predicate.

Main shell rendering SHALL iterate registrations dynamically and avoid hardcoded per-tab branching in the tab rebuild path.

**Status:** ✅ Complete

**Covered by:** `McpServer.UI.Core/Navigation/ITabRegistry.cs`, `McpServer.Director/DirectorTabRegistry.cs`, `McpServer.Director/Screens/MainScreen.cs`, `McpServer.Director/DirectorServiceRegistration.cs`
