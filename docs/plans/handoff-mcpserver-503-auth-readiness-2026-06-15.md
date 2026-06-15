# Handoff: Fix blanket HTTP 503 on `/mcpserver/*` (401 for bad creds + readiness health check)

- **Date:** 2026-06-15
- **From:** ClaudeCode (session `ClaudeCode-20260615T172241Z-diag-mcpserver-503`)
- **To:** Codex (`mcpserver-codex-plugin`)
- **MCP TODO:** `PLAN-MCPSERVER503-001`
- **Requirements:** FR-MCP-132, FR-MCP-133, TR-MCP-AUTH-010, TR-MCP-AUTH-011, TR-MCP-HEALTH-002, TEST-MCP-AUTH-010/011/012, TEST-MCP-HEALTH-002/003 (already appended to `docs/Project/`).
- **Status:** Diagnosis complete and reproduced. Requirements + RED regression tests written. Production fix **NOT YET APPLIED** (paused to avoid clobbering your concurrent edits). This document is decision-complete: implement exactly as written.

> Coordination note: this worktree was being edited concurrently by you (Codex) when this was written: `Program.cs`, `McpDbContext.cs`, and `WorkspaceTokenService.cs` were dirty (a "BrainSlot" feature). You already added `WorkspaceTokenService.IsInitialized` (~18:01Z). That property is exactly what this fix consumes; do not remove it. The fix below touches `WorkspaceAuthMiddleware.cs` (clean), `Program.cs` (one line in the health-checks chain), and adds new files. Re-check `git status` before editing shared files.

---

## 1. Root cause (confirmed by live reproduction, no restart)

Every `/mcpserver/*` data route is in `WorkspaceResolutionMiddleware.WorkspaceIndependentPrefixes` (todo, tools, requirements, repo, federation, sessionlog, context, gh, ...). For those routes:

1. `WorkspaceResolutionMiddleware` (`src/McpServer.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs`) does an API-key reverse lookup `tokenService.ResolveWorkspaceByToken(apiKey)`. An **unknown / stale / missing** key returns `null`, so the workspace is **not resolved**; because the route is workspace-independent it proceeds with `WorkspaceContext.WorkspacePath = null` (lines 119, 149-155).
2. `WorkspaceAuthMiddleware` (`src/McpServer.Support.Mcp/Middleware/WorkspaceAuthMiddleware.cs`, lines 140-175) then sets `workspacePath = WorkspacePath ?? configuration["Mcp:RepoRoot"]`. `Mcp:RepoRoot` is `"."`, normalized against the server **working directory**, which is **not** the registered primary workspace path. So `GetToken(thatPath)` is `null` and it returns **503** "workspace API token has not been initialized. Retry after startup completes." (lines 161-175).

Because the in-memory tokens **rotate on every restart** (`WorkspaceTokenService`, in-memory only), a client holding a cached key after a restart hits this on **every** `/mcpserver/*` call. An authentication failure is being reported as a startup/readiness condition.

**Live proof (PID 47520, build 2fd9eff, same process the reporter hit):** `GOOD key -> 200`; `BAD key / NO key -> 503` on `/mcpserver/todo`, `/mcpserver/tools`, `/mcpserver/federation/status`. The 503 body is present (152 bytes, `application/json`) but has **no `Retry-After`** header. The reporter's "empty body" was a client artifact; "recovered without restart" = a later test used the current marker key.

### Health gap
- `/health` and `/alive` only run `live`-tagged checks: `self` (always Healthy) + `upstream` (federation; "Federation disabled." -> Healthy).
- `/ready` runs **all** checks, but **no** workspace/token/DB readiness check is registered (the "DB check" comment in `Extensions.cs` is Aspire-Npgsql-only; this app is SQLite). So `/ready` is also falsely Healthy while `/mcpserver/*` is 503.

---

## 2. Fix part A - `WorkspaceAuthMiddleware` 503/401 semantics (FR-MCP-132 / TR-MCP-AUTH-010)

File: `src/McpServer.Support.Mcp/Middleware/WorkspaceAuthMiddleware.cs`.

Replace the API-key section (current lines ~140-218, from the comment `// ── API key path (agents only) ──` through the final 401 write) with the control flow below. **Keep** the existing JWT, hub-token, OIDC, and "already authenticated" branches above it unchanged. Reuse the existing 401 and 403 JSON bodies verbatim.

```csharp
        // ── API key path (agents only) ────────────────────────────────────────
        var workspacePath = workspaceContext.WorkspacePath ?? configuration["Mcp:RepoRoot"] ?? string.Empty;
        var expected = string.IsNullOrWhiteSpace(workspacePath) ? null : tokenService.GetToken(workspacePath);

        if (expected is not null)
        {
            // Full-access token — unrestricted.
            if (tokenService.ValidateToken(workspacePath, provided))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Default (anonymous) token — read-only only.
            if (tokenService.ValidateDefaultToken(workspacePath, provided))
            {
                context.Items[IsDefaultKeyItem] = true;
                if (s_readOnlyMethods.Contains(context.Request.Method))
                {
                    await _next(context).ConfigureAwait(false);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var forbiddenBody = new
                {
                    error = "Default API key grants read-only access only. " +
                            "Use the full workspace API key from the AGENTS-README-FIRST.yaml marker file or a valid JWT Bearer token for write operations."
                };
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(forbiddenBody, s_json),
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            // Known workspace, wrong key → 401.
            await WriteUnauthorizedAsync(context).ConfigureAwait(false);
            return;
        }

        // `expected` is null: no full token for the effective workspace path.
        // TR-MCP-AUTH-010: 503 is reserved STRICTLY for genuine startup readiness — no full token has
        // been seeded for ANY workspace yet, so we cannot authenticate anyone. Always send Retry-After.
        if (!tokenService.IsInitialized)
        {
            _logger.LogWarning("[WS-Auth] {Method} {Path} | Auth-token subsystem not initialized → 503 (startup)",
                method, path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "5";
            context.Response.ContentType = "application/json";
            var startupBody = new
            {
                error = "Workspace authentication is starting up: the per-workspace token subsystem has not been initialized yet. Retry shortly."
            };
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(startupBody, s_json),
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        // Subsystem is initialized: a valid full/default token would have reverse-resolved a workspace
        // (WorkspaceResolutionMiddleware) and produced a non-null `expected`. Reaching here means the
        // credential is unknown / stale / missing → 401, NOT a blanket 503.
        _logger.LogWarning("[WS-Auth] {Method} {Path} | Unresolved workspace / unknown API key (initialized) → 401",
            method, path);
        await WriteUnauthorizedAsync(context).ConfigureAwait(false);
```

Add this private helper (factor the existing final-401 body into it; the body text is unchanged from the current code):

```csharp
    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var body = new
        {
            error = "Invalid or missing API key. Re-read the AGENTS-README-FIRST.yaml marker file in the workspace root to get the current auth token and include it as the X-Api-Key header."
        };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, s_json),
            context.RequestAborted).ConfigureAwait(false);
    }
```

**Behavioral delta (only these change):**
- Unresolved workspace + `Mcp:RepoRoot` fallback path has no token + subsystem initialized: `503 -> 401`.
- Empty `Mcp:RepoRoot` + subsystem initialized: `503 -> 401`.
- Genuine startup (`!IsInitialized`): still `503`, now **with `Retry-After`**.
- Valid full token, wrong key, default-token read/write (403): unchanged.

The two existing tests `MissingWorkspaceToken_Returns503` and `EmptyWorkspaceContext_Returns503` use an **uninitialized** token service, so they still expect `503` and remain green.

---

## 3. Fix part B - `WorkspaceReadinessHealthCheck` (FR-MCP-133 / TR-MCP-HEALTH-002)

Create `src/McpServer.Services/Services/WorkspaceReadinessHealthCheck.cs`. **Use the same namespace and `using` directives as the existing `FederationUpstreamHealthCheck.cs` in that folder** so the existing `using` in `Program.cs` (which already references `FederationUpstreamHealthCheck`) resolves this type too. `IWorkspaceService` is **scoped**, so resolve it through `IServiceScopeFactory` (do NOT inject it directly into a health check). `WorkspaceTokenService` is a singleton and is injected directly.

```csharp
/// <summary>
/// FR-MCP-133 / TR-MCP-HEALTH-002: Readiness check for the subsystem that gates <c>/mcpserver/*</c>.
/// Reports Unhealthy when the auth-token subsystem is uninitialized, when no enabled workspace is
/// registered, or when the primary workspace has no seeded full-access token. Surfaced on <c>/ready</c>.
/// </summary>
public sealed class WorkspaceReadinessHealthCheck : IHealthCheck
{
    private readonly WorkspaceTokenService _tokenService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkspaceReadinessHealthCheck> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadinessHealthCheck"/> class.</summary>
    public WorkspaceReadinessHealthCheck(
        WorkspaceTokenService tokenService,
        IServiceScopeFactory scopeFactory,
        ILogger<WorkspaceReadinessHealthCheck> logger)
    {
        _tokenService = tokenService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_tokenService.IsInitialized)
            return HealthCheckResult.Unhealthy("Workspace auth-token subsystem has not been initialized.");

        WorkspaceListResult list;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
            list = await workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workspace readiness check could not enumerate workspaces.");
            return HealthCheckResult.Unhealthy("Workspace registry is unavailable.", ex);
        }

        var primary = list.Items.FirstOrDefault(w => w.IsPrimary && w.IsEnabled)
                   ?? list.Items.FirstOrDefault(w => w.IsEnabled);
        if (primary is null || string.IsNullOrWhiteSpace(primary.WorkspacePath))
            return HealthCheckResult.Unhealthy("No enabled workspace is registered.");

        if (_tokenService.GetToken(primary.WorkspacePath) is null)
            return HealthCheckResult.Unhealthy($"Primary workspace '{primary.Name}' has no seeded auth token.");

        return HealthCheckResult.Healthy("Workspace registry and auth tokens are ready.");
    }
}
```

Register it in `src/McpServer.Support.Mcp/Program.cs` at the existing health-checks chain (currently lines 529-530). Add **one line** (do not rewrite the block):

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<FederationUpstreamHealthCheck>("upstream", tags: ["live"])
    .AddCheck<WorkspaceReadinessHealthCheck>("workspace-ready", tags: ["ready"]);
```

The `ready` tag keeps it off `/health` and `/alive` (which filter to `live`) and includes it in `/ready` (no predicate -> runs all checks). See `src/McpServer.ServiceDefaults/Extensions.cs:170-195`.

---

## 4. Tests

### Already written (RED until part A lands)
- `tests/McpServer.Support.Mcp.Tests/Middleware/WorkspaceAuthMiddlewareTests.cs`
  - `UnknownApiKey_Unresolved_Initialized_Returns401`
  - `NoApiKey_Unresolved_Initialized_Returns401`
  - `SubsystemNotInitialized_Returns503WithRetryAfter`
- `tests/McpServer.Support.Mcp.Tests/Services/WorkspaceTokenServiceTests.cs` - `IsInitialized_*` (already GREEN; the property exists).

### To add (part B + integration)
Create `tests/McpServer.Support.Mcp.Tests/Services/WorkspaceReadinessHealthCheckTests.cs` (xUnit + NSubstitute, both available). Build the scope factory from a real `ServiceCollection` with a substituted `IWorkspaceService`:

```csharp
using McpServer.Support.Mcp.Services;        // WorkspaceTokenService (+ match the health check's namespace)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-HEALTH-002: Unit tests for WorkspaceReadinessHealthCheck.</summary>
public sealed class WorkspaceReadinessHealthCheckTests
{
    private const string Primary = @"C:\real\workspace";

    private static WorkspaceDto Ws(string path, bool isPrimary = true, bool isEnabled = true) => new()
    {
        WorkspacePath = path, Name = "ws", TodoPath = "docs/todo.yaml",
        StatusPrompt = "s", ImplementPrompt = "i", PlanPrompt = "p",
        IsPrimary = isPrimary, IsEnabled = isEnabled,
    };

    private static (WorkspaceReadinessHealthCheck Check, WorkspaceTokenService Tokens) Build(
        WorkspaceTokenService tokens, params WorkspaceDto[] items)
    {
        var svc = Substitute.For<IWorkspaceService>();
        svc.ListAsync(Arg.Any<CancellationToken>())
           .Returns(new WorkspaceListResult(items, items.Length));
        var sp = new ServiceCollection().AddScoped(_ => svc).BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return (new WorkspaceReadinessHealthCheck(tokens, scopeFactory,
            NullLogger<WorkspaceReadinessHealthCheck>.Instance), tokens);
    }

    /// <summary>Healthy when an enabled primary workspace is registered and has a seeded token.</summary>
    [Fact]
    public async Task Healthy_WhenPrimaryRegisteredAndTokenSeeded()
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(Primary);
        var (check, _) = Build(tokens, Ws(Primary));
        var r = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, r.Status);
    }

    /// <summary>Unhealthy when the token subsystem has not been initialized.</summary>
    [Fact]
    public async Task Unhealthy_WhenSubsystemNotInitialized()
    {
        var (check, _) = Build(new WorkspaceTokenService(), Ws(Primary));
        var r = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, r.Status);
    }

    /// <summary>Unhealthy when no enabled workspace is registered (token exists for some path).</summary>
    [Fact]
    public async Task Unhealthy_WhenNoEnabledWorkspace()
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(@"C:\some\other");
        var (check, _) = Build(tokens /* no workspaces */);
        var r = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, r.Status);
    }
}
```

Create `tests/McpServer.Support.Mcp.IntegrationTests/ReadinessAndAuthIntegrationTests.cs` (mirror `HealthEndpointTests` / `MultiTenantIntegrationTests` conventions; `IClassFixture<CustomWebApplicationFactory>`):

```csharp
using System.Net;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TEST-MCP-HEALTH-003: agent-flow auth semantics + /ready readiness coverage.</summary>
public sealed class ReadinessAndAuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public ReadinessAndAuthIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    /// <summary>Pure X-Api-Key agent flow (no X-Workspace-Path) with a valid token returns 200.</summary>
    [Fact]
    public async Task Todo_ValidToken_NoWorkspaceHeader_Returns200()
    {
        using var scope = _factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var token = tokens.GetToken(_factory.WorkspacePath)!;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", token);
        var resp = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    /// <summary>Unknown key on the agent flow returns 401, not a blanket 503.</summary>
    [Fact]
    public async Task Todo_UnknownKey_NoWorkspaceHeader_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "stale-or-wrong-key");
        var resp = await client.GetAsync(new Uri("/mcpserver/todo", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    /// <summary>/ready is Healthy when the data layer is up and lists the workspace-ready check.</summary>
    [Fact]
    public async Task Ready_WhenUp_Healthy_IncludesWorkspaceReadinessCheck()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/ready", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("workspace-ready", json, StringComparison.OrdinalIgnoreCase);
    }
}
```

> Note: in the integration factory `Mcp:RepoRoot == _workspacePath == the seeded primary path`, so `Todo_UnknownKey_NoWorkspaceHeader_Returns401` already returns 401 there (it is a regression lock for the agent flow, not the production-only 503 repro). The production 503->401 repro is covered by the `WorkspaceAuthMiddlewareTests` component tests, where `Mcp:RepoRoot` deliberately differs from the seeded path.

---

## 5. Validation (Byrd gate: zero failed, zero skipped before exit)

```powershell
# RED before part A, GREEN after:
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~WorkspaceAuthMiddlewareTests"
# Part B unit + token signal:
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~WorkspaceReadinessHealthCheckTests"
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~WorkspaceTokenServiceTests"
# Integration:
dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter "FullyQualifiedName~ReadinessAndAuthIntegrationTests"
# Full gates:
./build.ps1 Test
./build.ps1 ValidateTraceability
```

Manual smoke against a running server (replace key from the marker):
```powershell
# expect 200, 401, 401, 200
irm  http://localhost:7147/mcpserver/todo -Headers @{ 'X-Api-Key' = '<current-marker-key>' }
iwr  http://localhost:7147/mcpserver/todo -Headers @{ 'X-Api-Key' = 'bad' } -SkipHttpErrorCheck | % StatusCode  # 401
iwr  http://localhost:7147/mcpserver/todo -SkipHttpErrorCheck | % StatusCode                                    # 401
iwr  http://localhost:7147/ready -SkipHttpErrorCheck | % StatusCode                                             # 200, body lists workspace-ready
```

## 6. Acceptance criteria
- [ ] Unknown / stale / missing `X-Api-Key` on a workspace-independent `/mcpserver/*` route returns **401** once the token subsystem is initialized (was 503).
- [ ] A genuine startup-not-ready state (`!WorkspaceTokenService.IsInitialized`) returns **503 with a `Retry-After` header** and a JSON body.
- [ ] Valid full token (200), wrong key (401), default-token read (pass) / write (403) behavior is unchanged.
- [ ] `/ready` returns **Unhealthy** when the token subsystem is uninitialized or no enabled workspace is registered; **Healthy** otherwise, and its body lists `workspace-ready`.
- [ ] `/health` and `/alive` remain liveness-only (unchanged).
- [ ] All new + existing tests pass; `./build.ps1 Test` and `./build.ps1 ValidateTraceability` are green; zero skipped tests.
- [ ] Update MCP TODO `PLAN-MCPSERVER503-001` to done with a doneSummary citing the requirement IDs.

## 7. Out of scope / notes
- Do not widen timeouts or add blanket retries; the 503 was a mis-categorized auth failure, not a slow subsystem.
- A separate minor startup window exists: the marker file is (re)written by `WorkspaceProcessManager.StartAsync` (an `IHostedService` that runs after Kestrel is live), while tokens are seeded synchronously pre-Kestrel (`Program.cs:756-776`) and reused. The readiness check + 401 semantics make this benign; a follow-up could move the marker write earlier, but it is not required for this fix.
