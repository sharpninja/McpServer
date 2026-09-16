using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>
    /// TR-MCP-QUALITY-001 ac-23 / W18: opt-in mechanical replacement of exact
    /// generated-migration CS0612/CS0618 pragma pairs. Compile, Test, and
    /// ValidateWarningSuppressions never invoke this target.
    /// </summary>
    public Target NormalizeGeneratedMigrationObsoletePragmas => _ => _
        .Executes(() =>
        {
            var catalog = new List<string>();
            string[] roots =
            [
                "src/McpServer.Storage/Migrations",
                "src/McpServer.Storage.SqliteMigrations",
                "src/McpServer.Storage.SqlServerMigrations",
                "src/McpServer.Storage.PostgreSqlMigrations",
            ];

            foreach (var relativeRoot in roots)
            {
                var absoluteRoot = RootDirectory / relativeRoot;
                if (!Directory.Exists(absoluteRoot))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories))
                {
                    catalog.Add(Path.GetRelativePath(RootDirectory, file));
                }
            }

            var written = 0;
            GeneratedMigrationObsoletePragmaNormalizer.Normalize(
                catalog,
                relativePath => File.ReadAllText(RootDirectory / relativePath),
                (relativePath, contents) =>
                {
                    WriteAtomically(RootDirectory / relativePath, contents);
                    written++;
                    Log.Information("Normalized generated migration pragmas in {Path}", relativePath.Replace('\\', '/'));
                });

            Log.Information("NormalizeGeneratedMigrationObsoletePragmas wrote {Count} file(s).", written);
        });

    private static void WriteAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException($"Unable to resolve directory for {path}.");
        var tempPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempPath, contents);
        File.Move(tempPath, path, overwrite: true);
    }
}
