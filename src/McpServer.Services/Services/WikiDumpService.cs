using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-WIKIEXPORT-003 through 005: Dump export, hydration, and todo.yaml deprecation.</summary>
public sealed class WikiDumpService : IWikiDumpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly McpDbContext _db;
    private readonly TimeProvider _time;
    private readonly ISessionLogSanitizer _sanitizer;

    /// <summary>TR-MCP-WIKIEXPORT-003: Constructor.</summary>
    /// <param name="db">Workspace database.</param>
    /// <param name="time">Optional clock.</param>
    /// <param name="sanitizer">Outbound sanitizer used for dump text. Defaults to session-log rules.</param>
    public WikiDumpService(McpDbContext db, TimeProvider? time = null, ISessionLogSanitizer? sanitizer = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? TimeProvider.System;
        _sanitizer = sanitizer ?? new SessionLogSanitizer(Microsoft.Extensions.Options.Options.Create(new SessionLogSanitizationOptions()));
    }

    /// <inheritdoc />
    public async Task<WikiDumpExportResult> ExportAsync(WikiDumpExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.OutputRoot))
            return new WikiDumpExportResult { Success = false, Error = "OutputRoot is required." };
        if (IsUnsafePath(request.OutputRoot))
            return new WikiDumpExportResult { Success = false, Error = "unsafe_path" };

        Directory.CreateDirectory(request.OutputRoot);
        if (!request.IncludeDump)
            return new WikiDumpExportResult { Success = true };

        if (!string.IsNullOrWhiteSpace(request.WorkspacePath))
            _db.OverrideWorkspaceId(request.WorkspacePath);

        var workspace = string.IsNullOrWhiteSpace(request.WorkspacePath) ? _db.CurrentWorkspaceId ?? string.Empty : request.WorkspacePath;
        var todos = await _db.TodoItems.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var links = await _db.TodoRequirementLinks.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var dump = new WikiDumpDocument
        {
            SchemaVersion = WikiDumpDefaults.SchemaVersion,
            ExportedAtUtc = _time.GetUtcNow(),
            SourceWorkspaceKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(workspace))).ToLowerInvariant()[..16],
            SourceWorkspacePath = workspace,
            Tables = WikiDumpTablePolicyRegistry.GetDbSetNames()
                .Select(name => new WikiDumpTable { Name = name, Policy = WikiDumpTablePolicyRegistry.PolicyFor(name) })
                .ToList(),
            Todos = todos.Select(item => new WikiDumpTodo
            {
                Id = item.Id,
                Title = SanitizeField(item.Title),
                Section = SanitizeField(item.Section),
                Priority = SanitizeField(item.Priority),
                Done = item.Done,
            }).ToList(),
            TodoRequirementLinks = links.Select(item => new WikiDumpTodoRequirementLink
            {
                TodoId = item.TodoId,
                RequirementKind = SanitizeField(item.RequirementKind),
                RequirementId = item.RequirementId,
            }).ToList(),
            Diagnostics = [],
        };
        dump.Diagnostics = dump.Diagnostics.Select(SanitizeField).ToList();
        dump.Sha256 = ComputeSha256(dump);
        var path = Path.Combine(request.OutputRoot, WikiDumpDefaults.FileName);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(dump, JsonOptions), cancellationToken).ConfigureAwait(false);
        return new WikiDumpExportResult { Success = true, DumpFilePath = path, Dump = dump };
    }

    /// <inheritdoc />
    public async Task<WikiDumpImportResult> ImportAsync(WikiDumpImportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (IsUnsafePath(request.DumpPath) || IsUnsafePath(request.DestinationWorkspacePath))
            return Fail("unsafe_path", "Dump or destination path is unsafe.");

        var dumpFile = ResolveDumpFile(request.DumpPath);
        if (dumpFile is null)
            return Fail("malformed_dump", "Dump path does not contain mcp-wiki-dump.json.");

        string json;
        try
        {
            json = await File.ReadAllTextAsync(dumpFile, cancellationToken).ConfigureAwait(false);
            JsonSerializer.Deserialize<WikiDumpDocument>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return Fail("malformed_dump", "Dump JSON is malformed.");
        }

        var dump = JsonSerializer.Deserialize<WikiDumpDocument>(json, JsonOptions);
        if (dump is null)
            return Fail("malformed_dump", "Dump JSON is malformed.");
        if (!string.Equals(dump.SchemaVersion, WikiDumpDefaults.SchemaVersion, StringComparison.Ordinal))
            return Fail("version_mismatch", "Dump schemaVersion is not mcp-wiki-dump/v1.");
        if (dump.Tables.Count == 0)
            return Fail("missing_tables", "Dump tables[] is missing.");

        var expected = ComputeSha256(dump);
        if (!string.IsNullOrWhiteSpace(dump.Sha256) && !string.Equals(dump.Sha256, expected, StringComparison.OrdinalIgnoreCase))
            return Fail("hash_mismatch", "Dump sha256 does not match canonical bytes.");

        var destination = Path.GetFullPath(request.DestinationWorkspacePath);
        var diagnostics = new List<string>();
        var created = new List<string>();
        foreach (var todo in dump.Todos)
        {
            var exists = await _db.TodoItems.AnyAsync(item => item.Id == todo.Id, cancellationToken).ConfigureAwait(false);
            if (exists)
                continue;
            _db.TodoItems.Add(new TodoItemEntity
            {
                Id = todo.Id,
                Title = todo.Title,
                Section = todo.Section,
                Priority = todo.Priority,
                Done = todo.Done,
                WorkspaceId = destination,
            });
            created.Add(todo.Id);
        }

        foreach (var link in dump.TodoRequirementLinks)
        {
            var exists = await _db.TodoRequirementLinks.AnyAsync(
                item => item.TodoId == link.TodoId && item.RequirementId == link.RequirementId,
                cancellationToken).ConfigureAwait(false);
            if (exists)
                continue;
            _db.TodoRequirementLinks.Add(new TodoRequirementLinkEntity
            {
                WorkspaceId = destination,
                TodoId = link.TodoId,
                RequirementKind = link.RequirementKind,
                RequirementId = link.RequirementId,
                CreatedAtUtc = _time.GetUtcNow(),
            });
        }

        if (created.Count > 0 || dump.TodoRequirementLinks.Count > 0)
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string? archive = null;
        var todoYaml = request.TodoYamlPath;
        if (string.IsNullOrWhiteSpace(todoYaml))
        {
            var sibling = Path.Combine(Path.GetDirectoryName(dumpFile) ?? destination, "todo.yaml");
            if (File.Exists(sibling))
                todoYaml = sibling;
            var destYaml = Path.Combine(destination, "docs", "todo.yaml");
            if (File.Exists(destYaml))
                todoYaml = destYaml;
        }

        if (!string.IsNullOrWhiteSpace(todoYaml) && File.Exists(todoYaml))
        {
            diagnostics.Add("dump_wins_over_todo_yaml: " + dumpFile + " | " + todoYaml);
            var archiveDir = Path.Combine(destination, ".mcpServer", "archive", "todo-yaml");
            Directory.CreateDirectory(archiveDir);
            archive = Path.Combine(archiveDir, "todo.yaml");
            File.Copy(todoYaml, archive, overwrite: true);
        }

        return new WikiDumpImportResult
        {
            Success = true,
            CreatedTodoIds = created,
            Diagnostics = diagnostics,
            TodoYamlArchivePath = archive,
        };
    }

    private static WikiDumpImportResult Fail(string code, string error)
        => new() { Success = false, ErrorCode = code, Error = error };

    private static string? ResolveDumpFile(string dumpPath)
    {
        var full = Path.GetFullPath(dumpPath);
        if (File.Exists(full))
            return full;
        if (Directory.Exists(full))
        {
            var nested = Path.Combine(full, WikiDumpDefaults.FileName);
            return File.Exists(nested) ? nested : null;
        }

        return null;
    }

    private static bool IsUnsafePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;
        if (path.Contains("..", StringComparison.Ordinal))
            return true;
        try
        {
            var full = Path.GetFullPath(path);
            return !Path.IsPathRooted(full);
        }
        catch (Exception)
        {
            return true;
        }
    }

    private string SanitizeField(string? value)
    {
        var sanitized = _sanitizer.SanitizeString(value) ?? string.Empty;
        if (TrySanitizeJson(value, out var jsonSanitized))
            sanitized = jsonSanitized;
        if (!string.IsNullOrWhiteSpace(value) && TryDecodeUtf8Base64(value, out var decoded))
        {
            var nested = SanitizeField(decoded);
            if (!string.Equals(decoded, nested, StringComparison.Ordinal))
                return nested;
        }

        return sanitized;
    }

    private bool TrySanitizeJson(string? value, out string sanitized)
    {
        sanitized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var trimmed = value.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] is not ('{' or '['))
            return false;
        try
        {
            using var document = JsonDocument.Parse(value);
            sanitized = JsonSerializer.Serialize(SanitizeJsonElement(document.RootElement));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private object? SanitizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => IsSecretJsonProperty(property.Name)
                    ? "[REDACTED:json-secret]"
                    : SanitizeJsonElement(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeJsonElement).ToList(),
            JsonValueKind.String => _sanitizer.SanitizeString(element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var number) ? number : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static bool IsSecretJsonProperty(string name)
        => name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("apikey", StringComparison.OrdinalIgnoreCase)
            || name.Contains("api_key", StringComparison.OrdinalIgnoreCase)
            || name.Contains("api-key", StringComparison.OrdinalIgnoreCase);

    private static bool TryDecodeUtf8Base64(string value, out string decoded)
    {
        decoded = string.Empty;
        if (value.Length < 16 || value.Length % 4 != 0)
            return false;
        Span<byte> buffer = stackalloc byte[value.Length];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written == 0)
            return false;
        decoded = Encoding.UTF8.GetString(buffer[..written]);
        return decoded.All(ch => !char.IsControl(ch) || ch is '\r' or '\n' or '\t');
    }

    private static string ComputeSha256(WikiDumpDocument dump)
    {
        var copy = JsonSerializer.Deserialize<WikiDumpDocument>(JsonSerializer.Serialize(dump, JsonOptions), JsonOptions)!;
        copy.Sha256 = string.Empty;
        var canonical = JsonSerializer.Serialize(copy, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
