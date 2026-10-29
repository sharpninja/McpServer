using System.Text.Json;
using System.Text.Json.Nodes;

namespace QBrainAi.Support.Mcp.DatabaseMaintenance;

/// <summary>TR-MCP-PATHNORM-001: rekeys encrypted token entries without decrypting or reporting their contents.</summary>
internal static class WorkspacePathRekeyTokenStore
{
    /// <summary>Preflights or rewrites the offline token store, retaining the exact original as a verified backup.</summary>
    public static async Task RunAsync(string path, bool dryRun, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return;
        var original = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var document = JsonNode.Parse(original)?.AsObject()
            ?? throw new InvalidOperationException("Token store format is invalid.");
        var entriesKey = document.Select(p => p.Key).FirstOrDefault(k => k.Equals("Entries", StringComparison.OrdinalIgnoreCase));
        if (entriesKey is null || document[entriesKey] is not JsonArray entries)
            throw new InvalidOperationException("Token store entries are missing.");
        var mappings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry is not JsonObject value) throw new InvalidOperationException("Token store entry format is invalid.");
            var key = value.Select(p => p.Key).FirstOrDefault(k => k.Equals("WorkspacePath", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Token store workspace key is missing.");
            var old = value[key]?.GetValue<string>() ?? throw new InvalidOperationException("Token store workspace key is empty.");
            mappings[old] = WorkspacePathRekey.Normalize(old);
            value[key] = mappings[old];
        }
        if (mappings.GroupBy(m => m.Value, StringComparer.Ordinal).Any(g => g.Skip(1).Any()))
            throw new InvalidOperationException("Token store workspace keys converge; rekey aborted before mutation.");
        if (dryRun || mappings.All(m => m.Key == m.Value)) return;
        await using var storeLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!(await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false)).AsSpan().SequenceEqual(original))
            throw new InvalidOperationException("Token store changed during preflight; retry while offline.");
        var backupPath = path + ".pre-rekey";
        if (File.Exists(backupPath))
        {
            if (!(await File.ReadAllBytesAsync(backupPath, cancellationToken).ConfigureAwait(false)).AsSpan().SequenceEqual(original))
                throw new InvalidOperationException("Token backup already exists with different content; retain it and resolve before retry.");
        }
        else
        {
            await using var backup = new FileStream(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await backup.WriteAsync(original, cancellationToken).ConfigureAwait(false);
            backup.Flush(true);
        }
        if (!(await File.ReadAllBytesAsync(backupPath, cancellationToken).ConfigureAwait(false)).AsSpan().SequenceEqual(original))
            throw new InvalidOperationException("Token backup verification failed.");
        var temporary = path + ".rekey-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(output, document, cancellationToken: cancellationToken).ConfigureAwait(false);
                output.Flush(true);
            }
            File.Replace(temporary, path, null);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
