using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-108, TR-MCP-TODO-009, TEST-MCP-144 (ISS-TODO-001): Validates that a TODO item's
/// <c>description</c> is treated as formatted Markdown and that meaningful whitespace - blank
/// lines, indentation, code fences, list spacing, and trailing content - survives the
/// create/read, append-only audit, and YAML projection/import paths of the authoritative
/// SQLite-backed TODO store. Each test uses isolated temp DB and TODO.yaml fixtures so the
/// round-trip can be asserted without repository interference.
/// </summary>
public sealed class TodoMarkdownPreservationTests
{
    /// <summary>
    /// A Markdown description, represented as the canonical list-of-lines model, that exercises
    /// every preservation concern called out by ISS-TODO-001: a heading, blank separator lines,
    /// a trailing-whitespace line, nested list indentation, a fenced code block with indented
    /// content, and a final line with no trailing newline.
    /// </summary>
    private static readonly string[] MarkdownLines =
    [
        "# Heading",
        "",
        "Paragraph with **bold** and trailing spaces here:   ",
        "",
        "- list item 1",
        "  - nested indented item",
        "",
        "```csharp",
        "var x = 1;   // keep the indented line below intact",
        "    int y = 2;",
        "```",
        "",
        "Final line without trailing newline",
    ];

    /// <summary>
    /// FR-MCP-108, TEST-MCP-144: Verifies that creating a TODO and reading it back from the
    /// authoritative SQLite store returns the Markdown description line-for-line, including blank
    /// lines, indentation, and trailing whitespace. The fixture uses an isolated temp DB and YAML
    /// projection path so the create/read round-trip is asserted in isolation.
    /// </summary>
    [Fact]
    public async Task CreateThenGetById_PreservesMarkdownDescriptionExactly()
    {
        var fixture = CreateFixture();
        try
        {
            var create = await fixture.Store.CreateAsync(new TodoCreateRequest
            {
                Id = "MD-PRESERVE-001",
                Title = "Markdown create round-trip",
                Section = "Backlog",
                Priority = "low",
                Description = MarkdownLines,
            }).ConfigureAwait(true);
            Assert.True(create.Success);

            var item = await fixture.Store.GetByIdAsync("MD-PRESERVE-001").ConfigureAwait(true);
            Assert.NotNull(item);
            Assert.NotNull(item!.Description);
            Assert.Equal(MarkdownLines, item.Description!);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    /// <summary>
    /// FR-MCP-108, TEST-MCP-144: Verifies that the append-only audit snapshot captured on create
    /// preserves the Markdown description exactly, so audit history remains a faithful record of
    /// formatted content. The fixture reuses the isolated temp DB/YAML pair.
    /// </summary>
    [Fact]
    public async Task Audit_PreservesMarkdownDescriptionSnapshot()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Store.CreateAsync(new TodoCreateRequest
            {
                Id = "MD-PRESERVE-002",
                Title = "Markdown audit snapshot",
                Section = "Backlog",
                Priority = "low",
                Description = MarkdownLines,
            }).ConfigureAwait(true);

            var audit = await fixture.Store.GetAuditAsync("MD-PRESERVE-002").ConfigureAwait(true);
            Assert.Equal(1, audit.TotalCount);
            var snapshot = audit.Entries[0].Snapshot;
            Assert.NotNull(snapshot);
            Assert.NotNull(snapshot!.Description);
            Assert.Equal(MarkdownLines, snapshot.Description!);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    /// <summary>
    /// FR-MCP-108, TR-MCP-TODO-009, TEST-MCP-144: Verifies that the Markdown description survives a
    /// full YAML projection/import round-trip. The first store writes the deterministic TODO.yaml
    /// projection; a second store with an empty database bootstraps from that same YAML file. The
    /// imported description must equal the original line-for-line, proving the YAML converter does
    /// not strip blank lines, indentation, or trailing content during projection or import.
    /// </summary>
    [Fact]
    public async Task YamlProjectionRoundTrip_PreservesBlankLinesAndIndentation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"todo_md_{Guid.NewGuid():N}");
        var yamlPath = Path.Combine(root, "docs", "Project", "TODO.yaml");
        var dbPathA = Path.Combine(root, "a.db");
        var dbPathB = Path.Combine(root, "b.db");
        Directory.CreateDirectory(Path.GetDirectoryName(yamlPath)!);
        var auditLog = Substitute.For<IWriteAuditLog>();

        try
        {
            using (var writer = new SqliteTodoService(dbPathA, yamlPath, auditLog, NullLogger<SqliteTodoService>.Instance))
            {
                var create = await writer.CreateAsync(new TodoCreateRequest
                {
                    Id = "MD-PRESERVE-003",
                    Title = "Markdown projection round-trip",
                    Section = "Backlog",
                    Priority = "low",
                    Description = MarkdownLines,
                }).ConfigureAwait(true);
                Assert.True(create.Success);
            }

            // Fresh, empty database bootstraps from the projected YAML written above.
            using var importer = new SqliteTodoService(dbPathB, yamlPath, auditLog, NullLogger<SqliteTodoService>.Instance);
            var imported = await importer.GetByIdAsync("MD-PRESERVE-003").ConfigureAwait(true);
            Assert.NotNull(imported);
            Assert.NotNull(imported!.Description);
            Assert.Equal(MarkdownLines, imported.Description!);
        }
        finally
        {
            TryDelete(root);
        }
    }

    /// <summary>
    /// Creates an isolated SQLite-backed TODO store over a fresh temp directory containing the
    /// authoritative database and projected TODO.yaml so each test runs without shared state.
    /// </summary>
    private static MarkdownFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"todo_md_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "docs", "Project"));
        var dbPath = Path.Combine(root, "todo.db");
        var yamlPath = Path.Combine(root, "docs", "Project", "TODO.yaml");
        var store = new SqliteTodoService(dbPath, yamlPath, Substitute.For<IWriteAuditLog>(), NullLogger<SqliteTodoService>.Instance);
        return new MarkdownFixture(root, store);
    }

    /// <summary>Removes a temp directory tree, ignoring transient IO failures during cleanup.</summary>
    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Holds an isolated store plus its temp root so a test can dispose both deterministically.</summary>
    private sealed class MarkdownFixture(string root, SqliteTodoService store) : IDisposable
    {
        /// <summary>Gets the SQLite-backed TODO store under test.</summary>
        public SqliteTodoService Store { get; } = store;

        /// <summary>Disposes the store and deletes the temp root.</summary>
        public void Dispose()
        {
            Store.Dispose();
            TryDelete(root);
        }
    }
}
