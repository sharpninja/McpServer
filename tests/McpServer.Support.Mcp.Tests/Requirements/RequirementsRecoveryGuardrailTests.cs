using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001 guardrails. These tests call the real recovery service through the controller.
/// Placeholder text, acceptance-criteria removal, an empty body, a malformed id, and a type-prefix mismatch
/// return HTTP 400 validation_error and do not write.
/// </summary>
public sealed class RequirementsRecoveryGuardrailTests
{
    private const string WorkspacePath = "/tmp/mcp-req-recovery-guardrail";
    private const string OriginalBody = "Raster timing.\n\n## Acceptance Criteria\n- PAL has 312 lines.";

    /// <summary>An empty body is rejected and writes nothing.</summary>
    [Fact]
    public async Task RejectsEmptyBody()
    {
        await AssertRejectedAsync(
            "guard-empty",
            Item("fr", "FR-GUARD-EMPTY-001", "Guard empty", string.Empty)).ConfigureAwait(true);
    }

    /// <summary>A placeholder backfill sentence is rejected and writes nothing.</summary>
    [Fact]
    public async Task RejectsPlaceholderBody()
    {
        await AssertRejectedAsync(
            "guard-placeholder",
            Item("fr", "FR-GUARD-PLACEHOLDER-001", "Guard placeholder", "Placeholder requirement backfilled for TODO link.")).ConfigureAwait(true);
    }

    /// <summary>An id outside the kind pattern is rejected and writes nothing.</summary>
    [Fact]
    public async Task RejectsMalformedId()
    {
        await AssertRejectedAsync(
            "guard-malformed",
            Item("fr", "NOT-AN-ID", "Guard malformed", "A nonempty body that is not a requirement.")).ConfigureAwait(true);
    }

    /// <summary>A TEST id submitted as a TR is rejected and writes nothing.</summary>
    [Fact]
    public async Task RejectsTypePrefixMismatch()
    {
        await AssertRejectedAsync(
            "guard-type",
            Item("tr", "TEST-GUARD-TYPE-001", "Guard type", "A nonempty body with the wrong type prefix.")).ConfigureAwait(true);
    }

    /// <summary>Removing an acceptance-criteria heading from an existing body is rejected and the stored body stays.</summary>
    [Fact]
    public async Task RejectsAcceptanceCriteriaRemoval()
    {
        await using var connection = OpenConnection();
        var (service, db) = Build(connection);
        using (db)
        {
            db.Database.EnsureCreated();
            await SeedAsync(db, "FR-VIC-001", "Raster Engine", OriginalBody).ConfigureAwait(true);
            var controller = new RequirementsRecoveryController(service);
            var action = await controller.PostAsync(
                new RequirementsRecoveryRequest
                {
                    Mode = "apply",
                    IdempotencyKey = "guard-ac-removal",
                    Items =
                    [
                        Item("fr", "FR-VIC-001", "Raster Engine", "Raster timing."),
                    ],
                },
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            var (status, payload) = Read(action);
            var stored = await db.Requirements.SingleAsync(
                row => row.Id == "FR-VIC-001",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(status == 400, $"expected 400 nonretryable, got {status}. payload={payload}. storedBody={stored.Body}");
            Assert.Contains("\"retryable\":false", payload, StringComparison.Ordinal);
            Assert.Contains("validation_error", payload, StringComparison.Ordinal);
            Assert.Equal(OriginalBody, stored.Body);
        }
    }

    private static async Task AssertRejectedAsync(string key, RequirementsRecoveryItemRequest item)
    {
        await using var connection = OpenConnection();
        var (service, db) = Build(connection);
        using (db)
        {
            db.Database.EnsureCreated();
            var controller = new RequirementsRecoveryController(service);
            var action = await controller.PostAsync(
                new RequirementsRecoveryRequest
                {
                    Mode = "apply",
                    IdempotencyKey = key,
                    Items = [item],
                },
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            var (status, payload) = Read(action);
            var count = await db.Requirements.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(status == 400, $"expected 400 nonretryable, got {status}. payload={payload}. rowCount={count}");
            Assert.Contains("\"retryable\":false", payload, StringComparison.Ordinal);
            Assert.Contains("validation_error", payload, StringComparison.Ordinal);
            Assert.Equal(0, count);
        }
    }

    private static (int Status, string Payload) Read(ActionResult<RequirementsRecoveryResult> action)
    {
        var result = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        var payload = JsonSerializer.Serialize(result.Value);
        return (result.StatusCode ?? 0, payload);
    }

    private static RequirementsRecoveryItemRequest Item(string kind, string id, string title, string body)
        => new()
        {
            Kind = kind,
            Id = id,
            Title = title,
            Body = body,
            Priority = "low",
            Status = "pending",
        };

    private static async Task SeedAsync(McpDbContext db, string id, string title, string body)
    {
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = WorkspacePath,
            Kind = "fr",
            Id = id,
            Title = title,
            Body = body,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = "2026-09-29T00:00:00Z",
            UpdatedAtUtc = "2026-09-29T00:00:00Z",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static (RequirementsRecoveryService Sut, McpDbContext Db) Build(SqliteConnection connection)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection);
        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(builder.Options, workspace);
        db.OverrideWorkspaceId(WorkspacePath);
        return (new RequirementsRecoveryService(db, workspace), db);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }
}
