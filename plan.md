# Implementation Plan - MVP-APP-007

## Goal
Add `GitRemoteUrl` property to `WorkspaceDto` to enable GitHub repository linking in the UI. This property will be populated by running `git config --get remote.origin.url` in the workspace root.

## Proposed Changes

### 1. Update Data Models
- **Server-side**: Add `GitRemoteUrl` property to `WorkspaceDto` record in `src/McpServer.Services/Services/IWorkspaceService.cs`.
- **Client-side**: Add `GitRemoteUrl` property to `WorkspaceDto` class in `src/McpServer.Client/Models/WorkspaceModels.cs`.

### 2. Update WorkspaceService
- **Dependency Injection**: Inject `IProcessRunner` into `WorkspaceService` constructor.
- **Helper Method**: Implement `private async Task<string?> GetGitRemoteUrlAsync(string workspacePath)`:
  - Run `git config --get remote.origin.url` using `IProcessRunner`.
  - Handle errors (e.g., not a git repo) gracefully by returning null.
- **DTO Mapping**:
  - Convert `ToDto` from synchronous to `private async Task<WorkspaceDto> ToDtoAsync(WorkspaceConfigEntry e)`.
  - Await `GetGitRemoteUrlAsync` during DTO construction.
- **Async Propagation**:
  - Update `ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, and `DeleteAsync` to use `await ToDtoAsync(...)`.

### 3. Verification
- **Build**: Ensure `McpServer.Services` and `McpServer.Client` compile.
- **Tests**: Update any impacted tests (mock `IProcessRunner`).
- **Runtime**: Verify the property appears in the `/mcpserver/workspace` API response.

## Considerations
- **Performance**: Running `git` for every workspace in `ListAsync` adds overhead.
  - *Mitigation*: The command is fast (<50ms). We will execute it sequentially for simplicity first. If performance degrades, we can parallelize `ToDtoAsync` calls in `ListAsync` using `Task.WhenAll`.
- **Error Handling**: Missing git or non-git directories should not crash the endpoint; `GitRemoteUrl` will simply be null.

## Files to Modify
1. `src/McpServer.Services/Services/IWorkspaceService.cs`
2. `src/McpServer.Client/Models/WorkspaceModels.cs`
3. `src/McpServer.Services/Services/WorkspaceService.cs`
