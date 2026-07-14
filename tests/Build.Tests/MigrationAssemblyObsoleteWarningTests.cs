using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-WARNREMEDIATION-001: Verifies provider migration assemblies compile and
/// prevents generated obsolete warning suppressions from returning.
/// </summary>
public sealed partial class MigrationAssemblyObsoleteWarningTests
{
    private const string PragmaToken = "#pragma";

    private static readonly string GeneratedObsoleteDisablePragma = PragmaToken + " warning disable " + "612, 618";

    private static readonly string GeneratedObsoleteRestorePragma = PragmaToken + " warning restore " + "612, 618";

    private static readonly MigrationProject[] MigrationProjects =
    [
        new(
            "SQLite",
            Path.Combine("src", "McpServer.Storage.SqliteMigrations", "McpServer.Storage.SqliteMigrations.csproj"),
            Path.Combine("src", "McpServer.Storage.SqliteMigrations"),
            []),
        new(
            "SQL Server",
            Path.Combine("src", "McpServer.Storage.SqlServerMigrations", "McpServer.Storage.SqlServerMigrations.csproj"),
            Path.Combine("src", "McpServer.Storage.SqlServerMigrations"),
            [
                "SqlServerModelBuilderExtensions.UseIdentityColumns",
                "SqlServerPropertyBuilderExtensions.UseIdentityColumn",
            ]),
        new(
            "PostgreSQL",
            Path.Combine("src", "McpServer.Storage.PostgreSqlMigrations", "McpServer.Storage.PostgreSqlMigrations.csproj"),
            Path.Combine("src", "McpServer.Storage.PostgreSqlMigrations"),
            [
                "NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns",
                "NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn",
            ]),
    ];

    /// <summary>
    /// W16/W17 service coverage: every provider migration assembly must compile with
    /// the generated obsolete-warning pragmas removed.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetMigrationProjects))]
    public async Task MigrationAssemblies_CompileSuccessfully(MigrationProject project)
    {
        var result = await RunDotnetBuildAsync(project.ProjectPath).ConfigureAwait(true);

        Assert.True(result.ExitCode == 0, $"{project.ProviderName} migrations failed to build.\nSTDOUT:\n{result.Stdout}\nSTDERR:\n{result.Stderr}");
        Assert.DoesNotContain(": warning ", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(": warning ", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// W17 regression coverage: generated EF migration files must not suppress CS0612
    /// or CS0618 now that the current provider APIs compile without those warnings.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetMigrationProjects))]
    public void GeneratedMigrationFiles_DoNotContainObsoleteWarningPragmas(MigrationProject project)
    {
        var occurrences = EnumerateGeneratedMigrationFiles(project)
            .SelectMany(FindObsoletePragmaOccurrences)
            .ToArray();

        Assert.True(
            occurrences.Length == 0,
            $"{project.ProviderName} generated migration files still contain obsolete warning pragmas: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// W16/W17 inventory coverage: records the provider identity extension symbols that
    /// remain in generated code and are proven compiler-clean by the provider builds.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetMigrationProjects))]
    public void GeneratedMigrationFiles_RecordProviderIdentitySymbols(MigrationProject project)
    {
        var symbols = EnumerateGeneratedMigrationFiles(project)
            .SelectMany(file => ExtensionMethodPattern().Matches(File.ReadAllText(file)).Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(project.ExpectedProviderIdentitySymbols, symbols);
    }

    /// <summary>
    /// Supplies provider migration projects to the theory tests.
    /// </summary>
    public static TheoryData<MigrationProject> GetMigrationProjects()
    {
        var data = new TheoryData<MigrationProject>();
        foreach (var project in MigrationProjects)
        {
            data.Add(project);
        }

        return data;
    }

    private static string[] EnumerateGeneratedMigrationFiles(MigrationProject project)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), project.SourceRoot);
        return Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(IsGeneratedFile)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsGeneratedFile(string file)
    {
        return File.ReadLines(file).FirstOrDefault()?.StartsWith("// <auto-generated", StringComparison.Ordinal) == true;
    }

    private static IEnumerable<string> FindObsoletePragmaOccurrences(string file)
    {
        var relativePath = Path.GetRelativePath(FindRepositoryRoot(), file).Replace(Path.DirectorySeparatorChar, '/');
        var lineNumber = 0;
        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            if (line.Contains(GeneratedObsoleteDisablePragma, StringComparison.Ordinal)
                || line.Contains(GeneratedObsoleteRestorePragma, StringComparison.Ordinal))
            {
                yield return $"{relativePath}:{lineNumber}";
            }
        }
    }

    private static async Task<ProcessResult> RunDotnetBuildAsync(string relativeProjectPath)
    {
        var root = FindRepositoryRoot();
        var processStartInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        processStartInfo.ArgumentList.Add("build");
        processStartInfo.ArgumentList.Add(Path.Combine(root, relativeProjectPath));
        processStartInfo.ArgumentList.Add("-c");
        processStartInfo.ArgumentList.Add("Debug");
        processStartInfo.ArgumentList.Add("--no-restore");
        processStartInfo.ArgumentList.Add("-v");
        processStartInfo.ArgumentList.Add("minimal");

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        return new ProcessResult(process.ExitCode, await stdoutTask.ConfigureAwait(true), await stderrTask.ConfigureAwait(true));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.sln.");
    }

    [GeneratedRegex(@"\b[A-Za-z0-9_]+Extensions\.[A-Za-z0-9_]+", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionMethodPattern();

    /// <summary>
    /// Describes one provider migration assembly and its generated provider identity symbol inventory.
    /// </summary>
    public sealed record MigrationProject(
        string ProviderName,
        string ProjectPath,
        string SourceRoot,
        string[] ExpectedProviderIdentitySymbols)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return ProviderName;
        }
    }

    private sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
}

