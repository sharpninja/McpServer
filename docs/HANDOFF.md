# Handoff: Remove Workspaces from appsettings - Make DB Sole Source of Truth

## What Was Done

Removed `Mcp:Workspaces` from both `appsettings.yaml` files (source + deployed) and audited all code reading workspaces from `IConfiguration`. Fixed 5 of 8 call sites.

### Files Changed This Session

- `src/McpServer.Support.Mcp/appsettings.yaml` - removed entire `Workspaces:` block (379 lines removed); DB is sole source
- `src/McpServer.Services/Services/IWorkspaceService.cs` - added `AgentPath` property to `WorkspaceDto`
- `src/McpServer.Services/Services/WorkspaceService.cs` - mapped `AgentPath` in `ToDtoAsync`
- `src/McpServer.Services/Services/MarkerDiagnosticsEndpointHelper.cs` - `GetMarkerFileTimestampResult` now accepts `IEnumerable<string>? workspacePaths` instead of reading from config
- `src/McpServer.Support.Mcp/Program.cs` - removed pre-DI `primaryWorkspaceEntry` config block; replaced `Configure<TodoPromptOptions>` with `AddOptions<TodoPromptOptions>().Configure<IWorkspaceService>(...)` lazy pattern; updated `ResolvePrimaryApiKeyWorkspacePath` to accept optional `IServiceProvider?`; updated `/marker-file-timestamp` endpoint to inject `IWorkspaceService`

### Also changed (deployed config, not in git)

- `C:\ProgramData\McpServer\appsettings.yaml` - same `Workspaces:` block removed via gsudo

## Remaining Work - 3 Call Sites Still Read from IConfiguration

### 1. `src/McpServer.Services/Services/FederationLocalProxyEnrollmentService.cs`

Line 181: `BuildWorkspaceInventory()` reads `_configuration.GetSection("Mcp:Workspaces")`.

**Fix:** Replace `IConfiguration _configuration` with `IServiceScopeFactory _scopeFactory`. Make `BuildWorkspaceInventory()` async. Call `workspaceSvc.ListAsync()` and map `WorkspaceDto` to `FederationWorkspaceRegistrationRequest`. Update `EnrollAsync` and `HeartbeatAsync` to `await BuildWorkspaceInventoryAsync()`.

### 2. `src/McpServer.Services/Services/TodoBootstrapImporter.cs`

Line 80 in `RunAsync()`: `_configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? []`

**Fix:** `_scopeFactory` already injected. Use it to get `IWorkspaceService`, call `ListAsync()`, map `WorkspaceDto` to `WorkspaceConfigEntry` (fields needed: `WorkspacePath`, `Name`, `TodoPath`, `DataDirectory`). Keep `_configuration` - still needed for legacy `BuildLegacySingleWorkspace()` which reads `Mcp:RepoRoot` and `Mcp:TodoFilePath`.

### 3. `src/McpServer.Services/Services/VoiceConversationService.cs`

Line ~1244 in `ResolveAgentPathAsync()`: reads `_configuration.GetSection("Mcp:Workspaces")` to look up `AgentPath` by workspace path.

**Fix:** Constructor already takes `IServiceProvider serviceProvider`. Store `serviceProvider.GetRequiredService<IServiceScopeFactory>()` as a `_scopeFactory` field. In `ResolveAgentPathAsync`, create a scope, get `IWorkspaceService`, call `GetAsync(workspacePath, cancellationToken)`, check `dto?.AgentPath`. `AgentPath` is now on `WorkspaceDto` (added this session).

## Notes

- `GitHubToken` was on `WorkspaceConfigEntry` but is NOT in the DB schema - effectively null after this change. No code reads it at runtime in a user-visible way.
- `AgentPath` IS in DB schema (`WorkspaceEntity.AgentPath`) - was missing from `WorkspaceDto` before this session; now added.
- `RiskyStars` workspace missing marker: verify it is in DB as enabled, then restart server - `WorkspaceProcessManager.StartAsync()` calls `MarkerFileService.WriteMarkerAsync()` for each registered, enabled workspace.
