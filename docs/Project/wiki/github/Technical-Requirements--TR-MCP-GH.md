# TR-MCP-GH

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-GH-001

**GitHub OAuth Bootstrap Configuration Contract** — The server SHALL bind GitHub integration settings from `Mcp:GitHub`, including OAuth client metadata (`ClientId`, `RedirectUri`, `AuthorizeEndpoint`, `Scopes`) and token store path/fallback policy flags. REST endpoints under `/mcpserver/gh/oauth/*` SHALL expose the effective bootstrap configuration and authorize URL composition.
**Status:** ✅ Complete

**Covered by:** `GitHubIntegrationOptions`, `Program.cs` options binding/post-configure, `McpStdioHost` options binding/post-configure, `GitHubController` (`/oauth/config`, `/oauth/authorize-url`)
Scope: layer-1+

## TR-MCP-GH-002

**Encrypted Workspace GitHub Token Persistence** — Workspace GitHub tokens SHALL be stored encrypted-at-rest using ASP.NET Core Data Protection with atomic file writes and normalized workspace-path keys. The server SHALL expose `/mcpserver/gh/auth/status`, `/mcpserver/gh/auth/token` (PUT), and `/mcpserver/gh/auth/token` (DELETE) for token lifecycle management.
**Status:** ✅ Complete

**Covered by:** `IGitHubWorkspaceTokenStore`, `FileGitHubWorkspaceTokenStore`, `GitHubController` auth endpoints, `Program.cs` DI registration
Scope: layer-1+

## TR-MCP-GH-003

**Authenticated GitHub CLI Execution Path with Policy-Governed Fallback** — GitHub CLI execution SHALL support per-call token overrides so workspace-stored tokens can be applied as `GH_TOKEN` when present. The execution path SHALL prefer stored tokens when configured, emit telemetry indicating selected auth mode, and reject/allow fallback based on `AllowCliFallback`. When a workspace path is known, gh commands SHALL execute with that workspace root as the working directory.
**Status:** ✅ Complete

**Covered by:** `IProcessRunner` (`ProcessRunRequest` overload), `ProcessRunner`, `GitHubCliService` token resolution + auth-mode selection logs, `GitHubIntegrationOptions`
Scope: layer-1+

## TR-MCP-GH-004

**GitHub Actions Workflow Run API Surface** — The server SHALL support workflow run list/detail/rerun/cancel operations via gh CLI and expose them at `/mcpserver/gh/actions/runs*` with typed model contracts and client parity.
**Status:** ✅ Complete

**Covered by:** `IGitHubCliService`, `GitHubCliService`, `GitHubController` actions endpoints, `McpServer.Client` (`GitHubClient`, `Models/GitHubModels.cs`)
Scope: layer-1+

## TR-MCP-GH-005

**Workspace-Scoped gh Repository Execution** — GitHub issue and sync operations that rely on the local gh CLI SHALL execute inside the resolved workspace root so repository-scoped gh commands run against the correct checkout. This SHALL apply to both stored-token and fallback-auth execution modes.
**Status:** ✅ Complete

**Covered by:** `WorkspaceServiceAccessor`, `GitHubCliService`, `ProcessRunRequest`
Scope: layer-1+

## TR-MCP-GH-006

**Canonical GitHub Priority Labels, MCP-Authoritative Priority Sync, and ISSUE Change Comments** — TODO-to-GitHub issue sync SHALL canonicalize priority labels to `priority: HIGH|MEDIUM|LOW`, SHALL remove stale or non-canonical priority labels, and SHALL treat the MCP TODO priority as authoritative even if GitHub labels drift. GitHub-to-TODO refresh for existing `ISSUE-*` items SHALL preserve the current local priority and description, and endpoint-triggered ISSUE updates SHALL add a GitHub issue comment that summarizes the applied local change set after sync completes.
**Status:** ✅ Complete

**Covered by:** `IssueTodoSyncService`, `GitHubCliService`
Scope: layer-1+

## TR-MCP-GH-007

**Generated GitHub Comment Note Sections and TODO Comment Round-Trip** — GitHub-to-TODO sync for existing `ISSUE-*` items SHALL rebuild a generated note section that contains GitHub issue comments inside explicit begin/end markers, SHALL preserve user-authored TODO note text outside that generated section, and SHALL continue to avoid mutating the established TODO description. TODO-to-GitHub comment export SHALL detect newly appended user-authored note text and publish that text as a GitHub issue comment rather than collapsing the change to a generic note-update summary. When GitHub marks the issue closed, the next GitHub-to-TODO sync SHALL reconcile the TODO as done.
**Status:** ✅ Complete

**Covered by:** `IssueTodoSyncService`
Scope: layer-1+

## TR-MCP-GH-008

**Ownership-safe GitHub CLI repository selection** — GitHub CLI invocations SHALL either use an explicit configured or inferred repository selector through gh --repo without local repository discovery, or pass a command-scoped safe.directory Git configuration for the active workspace when a workspace working directory is required.
**Covered by:** FR: FR-MCP-116; TEST: TEST-MCP-156
**Status:** pending
Scope: layer-1+

