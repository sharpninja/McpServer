using System.Text.Json;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Ingestion;

/// <summary>
/// TR-PLANNED-CORE-013: the per-test state and helpers that both halves of SessionLogIngestorImportTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-PLANNED-CORE-013; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogIngestorImportTests (_db, _service, _tempDir, .ctor, Dispose, CreateIngestor, WriteSessionFile and WriteMdSessionFile) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class SessionLogIngestorImportTestsBase : IDisposable
{
    internal readonly McpDbContext _db;
    internal readonly SessionLogService _service;
    internal readonly string _tempDir;

    protected SessionLogIngestorImportTestsBase()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"IngestorImportTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SessionLogService(_db, NullLogger<SessionLogService>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"fwh-ingestor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs", "sessions"));
        _db.OverrideWorkspaceId(_tempDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best-effort cleanup */ }
    }

    internal SessionLogIngestor CreateIngestor()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempDir,
            SessionsPath = "docs/sessions"
        });
        return new SessionLogIngestor(new Chunker(), opts, new WorkspaceContext(), _service, NullLogger<SessionLogIngestor>.Instance);
    }

    internal void WriteSessionFile(string filename, UnifiedSessionLogDto dto)
    {
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", filename), json);
    }

    internal void WriteMdSessionFile(string filename, string content) =>
        File.WriteAllText(Path.Combine(_tempDir, "docs", "sessions", filename), content);
}
