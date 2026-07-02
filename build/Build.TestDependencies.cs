using System.Diagnostics;
using System.Net.Http;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable CA1416 // Platform compatibility - integration-test tooling targets Windows dev machines

partial class Build
{
    const string SqlLocalDbMsiUrl =
        "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi";

    const string PostgresBinariesZipUrl =
        "https://get.enterprisedb.com/postgresql/postgresql-17.5-1-windows-x64-binaries.zip";

    static AbsolutePath TestToolsDirectory =>
        (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "McpServer" / "test-tools";

    /// <summary>
    /// Idempotent installer for the provider integration-test dependencies:
    /// SQL Server LocalDB (elevated MSI install when missing) and PostgreSQL server binaries
    /// (existing installation preferred; otherwise the portable EDB binaries zip is downloaded
    /// to a user-scoped tools directory, no elevation required). Re-running when everything is
    /// already present is a no-op.
    /// </summary>
    public Target InstallTestDependencies => _ => _
        .Description("Ensure SQL Server LocalDB and PostgreSQL binaries exist for provider integration tests (idempotent)")
        .Executes(() =>
        {
            EnsureSqlLocalDb();
            var pgBin = EnsurePostgresBinaries();
            Log.Information("Test dependencies ready. LocalDB: OK; PostgreSQL binaries: {PgBin}", pgBin);
        });

    /// <summary>
    /// Runs the provider migration integration tests (SQLite in-memory, PostgreSQL, SQL Server
    /// LocalDB). The PostgreSQL tests boot their own ephemeral cluster with generated credentials
    /// on a free port via their xUnit class fixture (or honor <c>MCP_TEST_POSTGRES_CONNECTION</c>);
    /// SQL Server tests use an ad-hoc LocalDB database (auto-started). This target only guarantees
    /// the dependencies exist and runs the gate.
    /// </summary>
    public Target MigrationIntegrationTests => _ => _
        .Description("Run provider migration integration tests against ephemeral PostgreSQL + LocalDB instances")
        .DependsOn(InstallTestDependencies)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(TestsDirectory / "McpServer.Support.Mcp.Tests" / "McpServer.Support.Mcp.Tests.csproj")
                .SetFilter(
                    "FullyQualifiedName~Decompose4nfBackfillMigrationTests" +
                    "|FullyQualifiedName~PostgresDecompose4nfBackfillMigrationTests" +
                    "|FullyQualifiedName~SqlServerDecompose4nfBackfillMigrationTests")
                .SetVerbosity(DotNetVerbosity.minimal));
        });

    static void EnsureSqlLocalDb()
    {
        var existing = FindSqlLocalDbExe();
        if (existing is not null)
        {
            Log.Information("SQL Server LocalDB already installed: {Path}", existing);
            return;
        }

        Log.Information("SQL Server LocalDB missing; downloading installer ...");
        WindowsServiceHelper.AssertElevated(nameof(InstallTestDependencies));
        var msiPath = Path.Combine(Path.GetTempPath(), "SqlLocalDB.msi");
        DownloadFile(SqlLocalDbMsiUrl, msiPath);

        Log.Information("Installing SQL Server LocalDB (silent) ...");
        var install = Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /qn IACCEPTSQLLOCALDBLICENSETERMS=YES",
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Failed to start msiexec for SqlLocalDB.msi.");
        install.WaitForExit();
        if (install.ExitCode != 0)
            throw new InvalidOperationException($"SqlLocalDB.msi install failed with exit code {install.ExitCode}.");

        _ = FindSqlLocalDbExe()
            ?? throw new InvalidOperationException("SqlLocalDB.exe not found after installation.");
        Log.Information("SQL Server LocalDB installed.");
    }

    static string? FindSqlLocalDbExe()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SQL Server");
        if (!Directory.Exists(root))
            return null;
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
        };
        return Directory
            .EnumerateFiles(root, "SqlLocalDB.exe", enumeration)
            .OrderByDescending(p => p)
            .FirstOrDefault();
    }

    static string EnsurePostgresBinaries()
    {
        // Prefer an existing full installation (any version), newest first.
        var installedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
        if (Directory.Exists(installedRoot))
        {
            var installedBin = Directory
                .EnumerateDirectories(installedRoot)
                .OrderByDescending(d => d)
                .Select(d => Path.Combine(d, "bin"))
                .FirstOrDefault(bin => File.Exists(Path.Combine(bin, "initdb.exe")));
            if (installedBin is not null)
            {
                Log.Information("Using installed PostgreSQL binaries: {Bin}", installedBin);
                return installedBin;
            }
        }

        // Portable fallback: EDB binaries zip extracted to a user-scoped tools directory.
        var toolsBin = TestToolsDirectory / "pgsql" / "bin";
        if (File.Exists(toolsBin / "initdb.exe"))
        {
            Log.Information("Using portable PostgreSQL binaries: {Bin}", toolsBin);
            return toolsBin;
        }

        Log.Information("PostgreSQL binaries missing; downloading portable EDB zip ...");
        var zipPath = Path.Combine(Path.GetTempPath(), "postgresql-binaries.zip");
        DownloadFile(PostgresBinariesZipUrl, zipPath);
        TestToolsDirectory.CreateDirectory();
        // The zip contains a single top-level "pgsql" folder.
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, TestToolsDirectory, overwriteFiles: true);
        if (!File.Exists(toolsBin / "initdb.exe"))
            throw new InvalidOperationException($"initdb.exe not found under {toolsBin} after extracting PostgreSQL binaries.");
        Log.Information("Portable PostgreSQL binaries ready: {Bin}", toolsBin);
        return toolsBin;
    }

    static void DownloadFile(string url, string destinationPath)
    {
        using var http = new HttpClient();
        using var response = http.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var target = File.Create(destinationPath);
        response.Content.CopyToAsync(target).GetAwaiter().GetResult();
        Log.Information("Downloaded {Url} -> {Path} ({Bytes:N0} bytes)", url, destinationPath, target.Length);
    }

}
