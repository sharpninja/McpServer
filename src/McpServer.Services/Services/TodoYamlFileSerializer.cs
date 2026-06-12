using System.Text;
using McpServer.Support.Mcp.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Shared YAML serializer helpers for TODO document import and deterministic projection.
/// </summary>
internal static class TodoYamlFileSerializer
{
    private static readonly UTF8Encoding s_utf8NoBom = new(false);
    private static readonly TimeSpan[] s_atomicWriteRetryDelays =
    [
        TimeSpan.FromMilliseconds(20),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100)
    ];
    private static readonly IDeserializer s_deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeConverter(new TodoStringListYamlConverter())
        .WithTypeConverter(new TodoFileYamlConverter())
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer s_serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeConverter(new TodoStringListYamlConverter())
        .WithTypeConverter(new TodoFileYamlConverter())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    /// <summary>Reads a TODO YAML file if it exists.</summary>
    internal static async Task<TodoFile?> ReadIfExistsAsync(string todoFilePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(todoFilePath);

        if (!File.Exists(todoFilePath))
            return null;

        var yaml = await File.ReadAllTextAsync(todoFilePath, cancellationToken).ConfigureAwait(false);
        return s_deserializer.Deserialize<TodoFile>(yaml);
    }

    /// <summary>Serializes a TODO document to YAML text.</summary>
    internal static string Serialize(TodoFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return s_serializer.Serialize(file);
    }

    /// <summary>Writes a TODO YAML document atomically.</summary>
    internal static async Task WriteAtomicallyAsync(string todoFilePath, TodoFile file, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(todoFilePath);
        ArgumentNullException.ThrowIfNull(file);

        var yaml = Serialize(file);
        var fullPath = Path.GetFullPath(todoFilePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
        await File.WriteAllTextAsync(tempPath, yaml, s_utf8NoBom, cancellationToken).ConfigureAwait(false);

        try
        {
            await ReplaceOrMoveWithRetryAsync(tempPath, fullPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static async Task ReplaceOrMoveWithRetryAsync(string tempPath, string fullPath, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                ReplaceOrMove(tempPath, fullPath);
                return;
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or UnauthorizedAccessException)
            {
                File.Move(tempPath, fullPath, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < s_atomicWriteRetryDelays.Length)
            {
                await Task.Delay(s_atomicWriteRetryDelays[attempt], cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                File.Move(tempPath, fullPath, overwrite: true);
                return;
            }
        }
    }

    private static void ReplaceOrMove(string tempPath, string fullPath)
    {
        if (File.Exists(fullPath))
        {
            File.Replace(tempPath, fullPath, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, fullPath);
        }
    }
}
