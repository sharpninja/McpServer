using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G7 wiki dump Amendment D: named tests against shipped
/// <see cref="WikiDumpTablePolicyRegistry"/> and <see cref="WikiDumpService"/>.
/// TEST-MCP-WIKIEXPORT-003.
/// </summary>
public sealed class WikiDumpG7OverlayTests : IDisposable
{
    private const string SecretSentinel = "supersecretvalue123456";
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace.</summary>
    public WikiDumpG7OverlayTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "wiki-g7", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        try
        {
            if (Directory.Exists(_workspace))
                Directory.Delete(_workspace, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: dump policy registry covers every current DbSet exactly once.</summary>
    [Fact]
    public void DumpPolicyRegistry_CoversEveryCurrentDbSetExactlyOnce()
    {
        var expected = typeof(McpDbContext).GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var actual = WikiDumpTablePolicyRegistry.GetDbSetNames();
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Length, actual.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in actual)
            Assert.False(string.IsNullOrWhiteSpace(WikiDumpTablePolicyRegistry.PolicyFor(name)));
        Assert.Throws<ArgumentException>(() => WikiDumpTablePolicyRegistry.PolicyFor("NotADbSet"));
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: secret sentinels are redacted across scalar, nested, and encoded payloads.</summary>
    [Fact]
    public async Task Dump_SecretSentinels_RedactedAcrossScalarNestedAndEncodedPayloads()
    {
        SeedTodo("TODO-SECRET-SCALAR", $"api_key={SecretSentinel}");
        SeedTodo("TODO-SECRET-NESTED", "{\"api_key\":\"" + SecretSentinel + "\"}");
        SeedTodo("TODO-SECRET-ENCODED", Convert.ToBase64String(Encoding.UTF8.GetBytes("api_key=" + SecretSentinel)));
        var exported = await ExportAsync();
        var json = JsonSerializer.Serialize(exported.Dump, Camel());
        Assert.DoesNotContain(SecretSentinel, json, StringComparison.Ordinal);
        Assert.Contains(exported.Dump!.Todos, item => item.Id == "TODO-SECRET-SCALAR");
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: diagnostics never echo the secret value.</summary>
    [Fact]
    public async Task Dump_Diagnostics_NeverEchoSecretValue()
    {
        SeedTodo("TODO-DIAG-SECRET", $"password={SecretSentinel}");
        var exported = await ExportAsync();
        Assert.DoesNotContain(exported.Dump!.Diagnostics, item => item.Contains(SecretSentinel, StringComparison.Ordinal));
        var json = JsonSerializer.Serialize(exported.Dump, Camel());
        Assert.DoesNotContain(SecretSentinel, json, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: dump excludes unrelated workspace rows and global secrets.</summary>
    [Fact]
    public async Task Dump_ExcludesUnrelatedWorkspaceAndGlobalSecrets()
    {
        SeedTodo("TODO-KEEP", "keep");
        using (var db = CreateDb())
        {
            db.TodoItems.Add(new TodoItemEntity
            {
                Id = "TODO-OTHER-WS",
                Title = $"api_key={SecretSentinel}",
                Section = "overlay",
                Priority = "high",
                WorkspaceId = _workspace + "-other",
            });
            db.SaveChanges();
        }

        var exported = await ExportAsync();
        Assert.Contains(exported.Dump!.Todos, item => item.Id == "TODO-KEEP");
        Assert.DoesNotContain(exported.Dump.Todos, item => item.Id == "TODO-OTHER-WS");
        var json = JsonSerializer.Serialize(exported.Dump, Camel());
        Assert.DoesNotContain(SecretSentinel, json, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: sha256 is computed after redaction on canonical UTF-8 JSON.</summary>
    [Fact]
    public async Task Dump_FinalByteHash_VerifiesAfterRedaction()
    {
        SeedTodo("TODO-HASH-SECRET", $"secret={SecretSentinel}");
        var exported = await ExportAsync();
        var dump = exported.Dump!;
        Assert.DoesNotContain(SecretSentinel, dump.Todos.Select(item => item.Title));
        var copy = JsonSerializer.Deserialize<WikiDumpDocument>(JsonSerializer.Serialize(dump, Camel()), Camel())!;
        copy.Sha256 = string.Empty;
        var canonical = JsonSerializer.Serialize(copy, Camel());
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        Assert.Equal(expected, dump.Sha256);
        Assert.DoesNotContain(SecretSentinel, canonical, StringComparison.Ordinal);
    }

    private async Task<WikiDumpExportResult> ExportAsync()
    {
        var output = Path.Combine(_workspace, "dump-" + Guid.NewGuid().ToString("N"));
        var exported = await new WikiDumpService(CreateDb()).ExportAsync(new WikiDumpExportRequest
        {
            OutputRoot = output,
            IncludeDump = true,
            WorkspacePath = _workspace,
        }, TestContext.Current.CancellationToken);
        Assert.True(exported.Success, exported.Error);
        Assert.NotNull(exported.Dump);
        return exported;
    }

    private McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

    private void SeedTodo(string id, string title)
    {
        using var db = CreateDb();
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = id,
            Title = title,
            Section = "overlay",
            Priority = "high",
            WorkspaceId = _workspace,
        });
        db.SaveChanges();
    }

    private static JsonSerializerOptions Camel()
        => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
