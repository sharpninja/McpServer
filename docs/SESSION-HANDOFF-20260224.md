# Session Handoff — 2026-02-24

**Session Time:** 2026-02-24 09:58 CST → 15:46 CST (~6 hours)
**Agent:** Cline CLI
**Commit Base:** `26703a3ad400a09dbc354b509a3a3f04f1450744`

---

## Summary

This session focused on Keycloak OIDC authentication integration for the Director CLI/TUI, user management scripting, and TUI usability improvements (text copy support). Several items were completed successfully, but **critical CQRS handler implementations are missing** in `McpServer.UI.Core`, causing the Workspaces tab to crash at runtime.

---

## ✅ Completed Work

### 1. Keycloak Auth Config Auto-Discovery (LoginDialog)

**Files modified:**

- `src/McpServer.Director/Screens/LoginDialog.cs`

**What:** The LoginDialog now calls `GET /auth/config` from the MCP server on open, auto-populating the Authority URL, Client ID, and OIDC endpoints. Previously these had to be entered manually or set via environment variables.

### 2. OidcAuthOptions Registration in Workspace Apps

**Files modified:**

- `src/McpServer.Support.Mcp/Services/WorkspaceAppFactory.cs`

**What:** Workspace sub-applications now load their own `appsettings.json` and register `OidcAuthOptions` so the `/auth/config` endpoint works on workspace ports (e.g., `:7147`), not just the primary server (`:7147`).

### 3. User Management Script

**Files created:**

- `scripts/New-McpUser.ps1`

**What:** PowerShell script to create users in the `mcpserver` Keycloak realm via the Admin REST API. Accepts `-Username` and `-Password` as required parameters, plus optional `-Role` (admin/agent-manager/viewer), `-Email`, `-FirstName`, `-LastName`, `-Temporary`, `-KeycloakUrl`, `-AdminUser`, `-AdminPassword`, `-RealmName`. Idempotent — updates password and role if user already exists.

**Tested:** Created users `testuser` (viewer→admin) and `plbyrd` (viewer→admin) successfully.

### 4. TUI Text Copy Support

**Files modified:**

- `src/McpServer.Director/Screens/HealthScreen.cs` — `_detailLabel` (Label) → `_detailView` (TextView, ReadOnly, WordWrap) for selectable/copyable health JSON
- `src/McpServer.Director/Screens/LoginDialog.cs` — `_codeLabel`/`_uriLabel` (Label) → `_codeField`/`_uriField` (TextField, ReadOnly) for selectable user code and verification URL
- `src/McpServer.Director/Screens/WorkspaceListScreen.cs` — Error label → `errorField` (TextField, ReadOnly); also fixed `Colors.ColorSchemes["Error"]` → `TryGetValue` to prevent crash if scheme not registered
- `src/McpServer.Director/Screens/MainScreen.cs` — Added global **Ctrl+C** handler (`CopyFocusedText()`) that copies from focused TextView, TextField, Label, or TableView row to system clipboard. Added Ctrl+C to status bar hints.

### 5. Token Auto-Refresh in McpHttpClient

**Files modified:**

- `src/McpServer.Director/McpHttpClient.cs`

**What:** `TrySetCachedBearerToken()` now attempts to refresh an expired token using the refresh token before returning false. Previously it just gave up on expired tokens, requiring manual re-login. The refresh calls the Keycloak token endpoint directly using the cached authority and refresh token.

### 6. Director Tool Rebuilt & Reinstalled

Version `0.1.0-alpha.18` packed and installed as global dotnet tool `SharpNinja.McpServer.Director`.

---

## ❌ Known Bugs / Incomplete Work

### CRITICAL: Missing CQRS Handlers in McpServer.UI.Core

**Error:** `No service for type 'McpServer.Cqrs.IQueryHandler<ListWorkspacesQuery, ListWorkspacesResult>' has been registered.`

**Root Cause:** The `WorkspaceListViewModel` dispatches `ListWorkspacesQuery` through the CQRS `Dispatcher`, but **no handler class implementing `IQueryHandler<ListWorkspacesQuery, ListWorkspacesResult>` exists anywhere in the codebase**. The `AddCqrsHandlers()` assembly scan finds nothing.

**Architecture Agreement:** All CQRS handlers and MVVM code belong in `McpServer.UI.Core`, not in `McpServer.Director`.

**Missing Handlers (at minimum):**

| Query/Command | Handler Needed | API Endpoint |
|---|---|---|
| `ListWorkspacesQuery` → `ListWorkspacesResult` | `ListWorkspacesQueryHandler` | `GET /mcpserver/workspace` |
| `GetWorkspaceQuery` → `WorkspaceDetail?` | `GetWorkspaceQueryHandler` | `GET /mcpserver/workspace/{path}` |
| `UpdateWorkspacePolicyCommand` → `bool` | `UpdateWorkspacePolicyCommandHandler` | `POST /mcpserver/workspace/policy` |

**Where to create them:** `src/McpServer.UI.Core/Handlers/`

**Pattern to follow:**

```csharp
// src/McpServer.UI.Core/Handlers/ListWorkspacesQueryHandler.cs
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Handlers;

public sealed class ListWorkspacesQueryHandler : IQueryHandler<ListWorkspacesQuery, ListWorkspacesResult>
{
    private readonly IWorkspaceApiClient _client; // needs an abstraction

    public ListWorkspacesQueryHandler(IWorkspaceApiClient client)
    {
        _client = client;
    }

    public async Task<Result<ListWorkspacesResult>> HandleAsync(
        ListWorkspacesQuery query, CallContext context)
    {
        try
        {
            var result = await _client.ListWorkspacesAsync(context.CancellationToken);
            var items = result.Items.Select(w => new WorkspaceSummary(
                w.WorkspacePath, w.Name, w.WorkspacePort, w.IsPrimary, w.IsEnabled
            )).ToList();
            return Result<ListWorkspacesResult>.Success(
                new ListWorkspacesResult(items, items.Count));
        }
        catch (Exception ex)
        {
            return Result<ListWorkspacesResult>.Failure(ex);
        }
    }
}
```

**Key Decision Needed:** The handlers in `McpServer.UI.Core` need an HTTP client abstraction (e.g., `IWorkspaceApiClient`) to call the MCP server REST API. Options:

1. Add `McpServer.Client` as a dependency of `McpServer.UI.Core` and use `WorkspaceClient` directly
1. Define an `IWorkspaceApiClient` interface in `McpServer.UI.Core` and implement it in `McpServer.Director` (injected at DI registration time)
1. Have `McpServer.UI.Core` reference `McpServer.Client` which already has `WorkspaceClient.ListAsync()`

**Note:** `McpServer.Client` already has `WorkspaceClient` with `ListAsync()` returning `WorkspaceListResult`. The `McpServer.Client.Models.WorkspaceListResult` is a different type from `McpServer.Support.Mcp.Services.WorkspaceListResult` — mapping will be needed.

### Token Expiration Still Fast

The Keycloak access token expires quickly (default 5 minutes for Keycloak). The auto-refresh in `McpHttpClient.TrySetCachedBearerToken()` should help, but:

- It's only called at startup and after login — not before each API call
- Consider adding a `DelegatingHandler` that auto-refreshes before every HTTP request
- Or increase the Keycloak token lifespan in realm settings

### SyncScreen Already Uses TextView

`SyncScreen.cs` already had `TextView { ReadOnly = true }` — no changes needed there. Confirmed working.

---

## Files Modified This Session

| File | Change |
|---|---|
| `src/McpServer.Director/Screens/LoginDialog.cs` | Auth auto-discovery, TextField for code/URI, Ctrl+Y/U hotkeys |
| `src/McpServer.Director/Screens/HealthScreen.cs` | Label → TextView for detail view |
| `src/McpServer.Director/Screens/MainScreen.cs` | Global Ctrl+C copy handler, status bar hint |
| `src/McpServer.Director/Screens/WorkspaceListScreen.cs` | Error Label → TextField, TryGetValue for color scheme |
| `src/McpServer.Director/McpHttpClient.cs` | Token auto-refresh in TrySetCachedBearerToken |
| `src/McpServer.Support.Mcp/Services/WorkspaceAppFactory.cs` | Load workspace appsettings.json, register OidcAuthOptions |
| `scripts/New-McpUser.ps1` | **NEW** — Keycloak user creation script |

---

## Keycloak Users Created

| Username | Role | Realm |
|---|---|---|
| `testuser` | admin | mcpserver |
| `plbyrd` | admin | mcpserver |
| `mcpadmin` | admin | mcpserver (created by Setup-McpKeycloak.ps1) |

---

## Next Session Priority

1. **Create CQRS handlers in `src/McpServer.UI.Core/Handlers/`** — This is the blocking issue. At minimum: `ListWorkspacesQueryHandler`, `GetWorkspaceQueryHandler`, `UpdateWorkspacePolicyCommandHandler`.
2. **Decide on HTTP client abstraction** — Interface in UI.Core vs. direct McpServer.Client dependency.
3. **Register handlers** — `ServiceCollectionExtensions.AddUiCore()` already calls `AddCqrsHandlers(thisAssembly)` which scans the UI.Core assembly, so handlers placed there will be auto-discovered.
4. **Test the full Workspaces tab flow** end-to-end.
5. **Consider per-request token refresh** — Add a `DelegatingHandler` to `McpHttpClient` that checks token expiry before each call.
