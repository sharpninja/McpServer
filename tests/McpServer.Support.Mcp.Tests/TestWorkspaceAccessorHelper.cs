using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests;

/// <summary>
/// Test helper for creating <see cref="WorkspaceServiceAccessor"/> instances in unit tests.
/// Provides a real accessor that delegates to a mock <see cref="ITodoService"/>.
/// </summary>
internal static class TestWorkspaceAccessorHelper
{
    /// <summary>Creates a <see cref="WorkspaceServiceAccessor"/> that resolves to the given mock <see cref="ITodoService"/>.</summary>
    public static WorkspaceServiceAccessor Create(ITodoService todoService, string? repoRoot = null)
    {
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = repoRoot ?? "." });
        var storageOptions = Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions { Provider = "yaml" });
        var auditLog = Substitute.For<IWriteAuditLog>();
        var loggerFactory = NullLoggerFactory.Instance;

        var resolver = new TodoServiceResolver(todoService, ingestionOptions, storageOptions, auditLog, loggerFactory);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        return new WorkspaceServiceAccessor(resolver, httpContextAccessor, ingestionOptions);
    }
}
