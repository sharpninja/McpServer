using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using QBrainAi.Support.Mcp.DatabaseMaintenance;

namespace QBrainAi.Support.Mcp.Tests.DatabaseMaintenance;

/// <summary>TEST-MCP-PATHNORM-001: deterministic raw SQLite fixtures preserve legacy spellings and exercise the rekey contract without a live store.</summary>
public sealed class WorkspacePathRekeyTests
{
    private static Type Subject => typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType("QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekey", throwOnError: true)!;

    /// <summary>Policy A literals cover drive, device, UNC, dot segments, case and unaffected non-Windows ids.</summary>
    [Theory]
    [InlineData(@"C:\Users\Mixed", "C:/Users/Mixed")]
    [InlineData(@"c:\MiXeD\a\..\b", "c:/MiXeD/b")]
    [InlineData(@"\\?\C:\x", "C:/x")]
    [InlineData(@"\\?\UNC\srv\share\x", "//srv/share/x")]
    [InlineData(@"\\srv\share\x", "//srv/share/x")]
    [InlineData("C:/Mixed/x", "C:/Mixed/x")]
    [InlineData(@"/home/a\b", @"/home/a\b")]
    [InlineData(@"opaque\id", @"opaque\id")]
    [InlineData("", "")]
    public void Normalize_LiteralContract(string input, string expected)
        => Assert.Equal(expected, Subject.GetMethod("Normalize")!.Invoke(null, [input]));

    /// <summary>Independent good/bad fixture values prove the literal assertion rejects case loss, prefix retention and slash loss.</summary>
    [Theory]
    [InlineData("C:/Mixed", "c:/mixed")]
    [InlineData("C:/x", "//?/C:/x")]
    [InlineData("//srv/share", "/srv/share")]
    public void MockData_NegativeControls(string expected, string wrong)
    {
        Assert.Equal(expected, expected);
        Assert.ThrowsAny<Exception>(() => Assert.Equal(expected, wrong));
    }

    /// <summary>A seeded copy reports exact key mapping/counts, retains manual keys, and does not change any rows.</summary>
    [Fact]
    public async Task DryRun_ReportsWithoutMutation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.RowsAsync();
        var report = await RunAsync(fixture.Connection, true, null);
        Assert.Contains("C:/Mixed", report);
        Assert.Contains("Manual", report, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Workspaces", report);
        Assert.DoesNotContain("never-print-this-secret", report);
        Assert.Equal(before, await fixture.RowsAsync());
    }

    /// <summary>Missing backup prevents DML, and a failed backup target leaves the legacy graph intact.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Apply_RequiresSuccessfulBackup(bool existingTarget)
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.RowsAsync();
        var target = existingTarget ? fixture.Path : null;
        await Assert.ThrowsAnyAsync<Exception>(() => RunAsync(fixture.Connection, false, target));
        Assert.Equal(before, await fixture.RowsAsync());
    }

    /// <summary>Canonical and device spellings converging with an old key abort before any row update.</summary>
    [Theory]
    [InlineData("C:/Mixed")]
    [InlineData(@"\\?\C:\Mixed")]
    public async Task Collision_AbortsBeforeMutation(string second)
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.ExecuteAsync("INSERT INTO Workspaces VALUES (@p, @p, 'private')", second);
        await fixture.ExecuteAsync("CREATE TRIGGER reject_mutation BEFORE UPDATE ON Workspaces BEGIN SELECT RAISE(ABORT, 'mutation attempted'); END;");
        var before = await fixture.RowsAsync();
        var error = await Assert.ThrowsAnyAsync<Exception>(() => RunAsync(fixture.Connection, false, fixture.Backup));
        Assert.Contains("converg", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mutation attempted", error.ToString());
        Assert.Equal(before, await fixture.RowsAsync());
    }

    /// <summary>Composite parents/children and manual triage keys survive apply; backup contains old rows; replay is a no-op.</summary>
    [Fact]
    public async Task Apply_AtomicGraph_Backup_AndIdempotent()
    {
        await using var fixture = await Fixture.CreateAsync();
        await RunAsync(fixture.Connection, false, fixture.Backup);
        Assert.Equal("C:/Mixed", await fixture.ScalarAsync("SELECT WorkspaceId FROM Workspaces"));
        Assert.Equal("C:/Mixed", await fixture.ScalarAsync("SELECT WorkspaceId FROM Child"));
        Assert.Equal("manual-key", await fixture.ScalarAsync("SELECT GroupKey FROM TriageGroups"));
        Assert.Equal("manual-group", await fixture.ScalarAsync("SELECT GroupId FROM TriageReports"));
        Assert.Null(await fixture.ScalarAsync("PRAGMA foreign_key_check"));
        await using var backup = new SqliteConnection($"Data Source={fixture.Backup};Mode=ReadOnly;Pooling=False");
        await backup.OpenAsync(TestContext.Current.CancellationToken);
        await using var query = backup.CreateCommand();
        query.CommandText = "SELECT WorkspaceId FROM Workspaces";
        Assert.Equal(@"C:\Mixed", await query.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        var after = await fixture.RowsAsync();
        var second = await RunAsync(fixture.Connection, false, fixture.Backup + ".second");
        Assert.Equal(after, await fixture.RowsAsync());
        Assert.Contains("\"ChangedRows\":0", second);
    }

    /// <summary>Injected child update failure rolls back all preceding updates and restores FK enforcement.</summary>
    [Fact]
    public async Task Failure_RollsBackAllTables()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.ExecuteAsync("CREATE TRIGGER reject_child BEFORE UPDATE ON Child BEGIN SELECT RAISE(ABORT, 'fixture failure'); END;");
        var before = await fixture.RowsAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => RunAsync(fixture.Connection, false, fixture.Backup));
        Assert.Equal(before, await fixture.RowsAsync());
        Assert.Null(await fixture.ScalarAsync("PRAGMA foreign_key_check"));
        Assert.Equal(1L, await fixture.ScalarAsync("PRAGMA foreign_keys"));
    }

    /// <summary>Raw startup probe rejects legacy keys with remediation text and accepts the same rekeyed fixture.</summary>
    [Fact]
    public async Task StartupGuard_LegacyRefuses_CanonicalAccepts()
    {
        await using var fixture = await Fixture.CreateAsync();
        var method = Subject.GetMethod("ValidateStartupAsync")!;
        var error = await Assert.ThrowsAnyAsync<Exception>(async () => await (Task)method.Invoke(null, [fixture.Connection, CancellationToken.None])!);
        Assert.Contains("--workspace-path-rekey", error.ToString());
        await RunAsync(fixture.Connection, false, fixture.Backup);
        await (Task)method.Invoke(null, [fixture.Connection, CancellationToken.None])!;
    }

    /// <summary>TEST-MCP-PATHNORM-001: PR 81 regression uses raw legacy rows to show canonical EF predicates miss them; startup must reject before migration.</summary>
    [Fact]
    public async Task Startup_RejectsBeforeCanonicalQueriesAndMigrations()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var db = new QBrainAi.Support.Mcp.Storage.McpDbContext(
            Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions.UseSqlite(
                new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QBrainAi.Support.Mcp.Storage.McpDbContext>(), fixture.Connection).Options);
        Assert.False(System.Linq.Queryable.Any(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.Workspaces), w => w.WorkspaceId == @"C:\Mixed"));
        var provider = new QBrainAi.Support.Mcp.Storage.Database.McpDatabaseProviderOptions(
            QBrainAi.Support.Mcp.Storage.Database.McpDatabaseProviderKind.Sqlite, "sqlite",
            fixture.Connection.ConnectionString, "QBrainAi.Storage.SqliteMigrations");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            QBrainAi.Support.Mcp.Storage.Database.McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(
                db, provider, TestContext.Current.CancellationToken));
        Assert.Contains("--workspace-path-rekey", error.Message);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT count(*) FROM sqlite_master WHERE name='__EFMigrationsHistory'"));
        await RunAsync(fixture.Connection, false, fixture.Backup);
        Assert.True(System.Linq.Queryable.Any(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.Workspaces), w => w.WorkspaceId == @"C:\Mixed"));
    }

    /// <summary>TEST-MCP-PATHNORM-001: report has exact old/new keys, counts and retained manual count, without changing file bytes.</summary>
    [Fact]
    public async Task DryRun_ExactReportAndBytes()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Connection.CloseAsync();
        var before = await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken);
        using var report = JsonDocument.Parse(await RunAsync(fixture.Connection, true, null));
        Assert.Equal(1, report.RootElement.GetProperty("Mappings").GetArrayLength());
        var mapping = report.RootElement.GetProperty("Mappings")[0];
        Assert.Equal(@"C:\Mixed", mapping.GetProperty("OldKey").GetString());
        Assert.Equal("C:/Mixed", mapping.GetProperty("NewKey").GetString());
        Assert.Equal(1, report.RootElement.GetProperty("ManualTriageGroupsRetained").GetInt32());
        Assert.All(report.RootElement.GetProperty("Tables").EnumerateArray(), table =>
        {
            Assert.Equal(1, table.GetProperty("Before").GetInt64());
            Assert.Equal(1, table.GetProperty("After").GetInt64());
        });
        await fixture.Connection.CloseAsync();
        Assert.Equal(before, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
    }

    /// <summary>TEST-MCP-PATHNORM-001: independent SHA payloads prove automatic triage, references, forced handoff and hub ids migrate together.</summary>
    [Fact]
    public async Task DerivedKeys_RecomputeAndPropagate()
    {
        await using var fixture = await Fixture.CreateAsync();
        var oldGroup = Digest(@"C:\Mixed|fingerprint").ToLowerInvariant();
        var newGroup = Digest("C:/Mixed|fingerprint").ToLowerInvariant();
        await fixture.ExecuteAsync("UPDATE TriageGroups SET GroupKey=@p", oldGroup);
        await fixture.ExecuteAsync("PRAGMA foreign_keys=OFF");
        await fixture.ExecuteAsync("UPDATE TriageGroups SET GroupId=@p", "triage-group-" + oldGroup[..16]);
        await fixture.ExecuteAsync("UPDATE TriageReports SET GroupId=@p", "triage-group-" + oldGroup[..16]);
        await fixture.ExecuteAsync("PRAGMA foreign_keys=ON");
        await fixture.ExecuteAsync("""
            CREATE TABLE TriageResearchRuns (GroupId TEXT REFERENCES TriageGroups(GroupId));
            INSERT INTO TriageResearchRuns SELECT GroupId FROM TriageGroups;
            CREATE TABLE HandoffIngestionRuns (RunId TEXT PRIMARY KEY, WorkspaceId TEXT, ContentSha256 TEXT, PromptVersion TEXT, Force INTEGER, ReplayIdentity TEXT UNIQUE);
            INSERT INTO HandoffIngestionRuns VALUES ('run1', 'C:\Mixed', 'abc', 'v1', 1, 'old-replay');
            CREATE TABLE FederationWorkspaces (ProxyId TEXT, WorkspacePath TEXT, GlobalWorkspaceId TEXT, CanonicalWorkspaceId TEXT);
            """);
        var oldHub = Digest(@"proxy:windows:c:\mixed");
        var newHub = Digest("proxy:windows:c:/mixed");
        await fixture.ExecuteAsync(@"INSERT INTO FederationWorkspaces VALUES ('proxy','C:\Mixed',@p,@p)", oldHub);
        await fixture.ExecuteAsync("INSERT INTO Workspaces VALUES (@p,@p,'private')", oldHub);
        await fixture.ExecuteAsync("INSERT INTO Parent VALUES (@p,'hub-parent')", oldHub);
        await RunAsync(fixture.Connection, false, fixture.Backup);
        Assert.Equal(newGroup, await fixture.ScalarAsync("SELECT GroupKey FROM TriageGroups"));
        Assert.Equal("triage-group-" + newGroup[..16], await fixture.ScalarAsync("SELECT GroupId FROM TriageResearchRuns"));
        Assert.Equal(Digest("v1\n8:C:/Mixed\n3:abc\n2:v1\n4:run1"), await fixture.ScalarAsync("SELECT ReplayIdentity FROM HandoffIngestionRuns"));
        Assert.Equal(newHub, await fixture.ScalarAsync("SELECT GlobalWorkspaceId FROM FederationWorkspaces"));
        Assert.Equal(newHub, await fixture.ScalarAsync("SELECT WorkspaceId FROM Parent WHERE Id='hub-parent'"));
        Assert.Null(await fixture.ScalarAsync("PRAGMA foreign_key_check"));
    }

    /// <summary>TEST-MCP-PATHNORM-001: cancellation is observed before any changes or successful report.</summary>
    [Fact]
    public async Task Cancelled_LeavesFixtureIntact()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.RowsAsync();
        _ = Subject;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var task = (Task)Subject.GetMethod("RunAsync")!.Invoke(null,
            [fixture.Connection, false, fixture.Backup, cancellation.Token])!;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(before, await fixture.RowsAsync());
    }

    /// <summary>TEST-MCP-PATHNORM-001: fail closed for dry-run without a copy, apply without backup, and unknown arguments.</summary>
    [Theory]
    [InlineData("--dry-run")]
    [InlineData("--apply")]
    [InlineData("--unknown")]
    public void Command_RejectsUnsafeArguments(string option)
    {
        var type = typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType(
            "QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekeyCommand", true)!;
        object?[] args = [new[] { "--workspace-path-rekey", option }, null, null];
        Assert.True((bool)type.GetMethod("TryParse")!.Invoke(null, args)!);
        Assert.Null(args[1]);
        Assert.False(string.IsNullOrWhiteSpace((string?)args[2]));
    }

    /// <summary>TEST-MCP-PATHNORM-001: command selects only an explicit distinct read-only SQLite copy without opening the configured database.</summary>
    [Fact]
    public async Task Command_CopySelectionIsReadOnly()
    {
        await using var fixture = await Fixture.CreateAsync();
        var type = typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType(
            "QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekeyCommand", true)!;
        var method = type.GetMethod("SelectCopyConnectionString")!;
        var configured = new SqliteConnectionStringBuilder { DataSource = fixture.Path + ".live", Password = "fake-secret" }.ToString();
        var result = (string)method.Invoke(null, ["sqlite", configured, fixture.Path])!;
        var selected = new SqliteConnectionStringBuilder(result);
        Assert.Equal(fixture.Path, selected.DataSource);
        Assert.Equal(SqliteOpenMode.ReadOnly, selected.Mode);
        Assert.False(File.Exists(fixture.Path + ".live"));
        var error = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, ["sqlite", configured, fixture.Path + ".live"]));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    /// <summary>TEST-MCP-PATHNORM-001: token-copy rekey preserves encrypted payload and backup, rejects convergence, and replays without another backup.</summary>
    [Fact]
    public async Task TokenCopy_BackupCollisionAndReplay()
    {
        var type = typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType(
            "QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekeyTokenStore", true)!;
        var method = type.GetMethod("RunAsync")!;
        var path = Path.Combine(Path.GetTempPath(), $"p15-token-{Guid.NewGuid():N}.json");
        var original = JsonSerializer.Serialize(new { Entries = new[] { new { WorkspacePath = @"C:\Mixed", AccessTokenProtected = "fixture-ciphertext" } } });
        try
        {
            await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
            await (Task)method.Invoke(null, [path, false, TestContext.Current.CancellationToken])!;
            var canonical = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            Assert.Contains("C:/Mixed", canonical);
            Assert.Contains("fixture-ciphertext", canonical);
            Assert.Equal(original, await File.ReadAllTextAsync(path + ".pre-rekey", TestContext.Current.CancellationToken));
            await (Task)method.Invoke(null, [path, false, TestContext.Current.CancellationToken])!;
            Assert.Equal(canonical, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            var collision = JsonSerializer.Serialize(new { Entries = new[] {
                new { WorkspacePath = @"C:\Mixed" }, new { WorkspacePath = "C:/Mixed" } } });
            await File.WriteAllTextAsync(path, collision, TestContext.Current.CancellationToken);
            var task = (Task)method.Invoke(null, [path, true, TestContext.Current.CancellationToken])!;
            await Assert.ThrowsAsync<InvalidOperationException>(() => task);
            Assert.Equal(collision, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            foreach (var file in new[] { path, path + ".pre-rekey", path + ".lock" })
                if (File.Exists(file)) File.Delete(file);
        }
    }

    /// <summary>TEST-MCP-PATHNORM-001: ordinal mapping preserves distinct case spellings even on a case-insensitive column.</summary>
    [Fact]
    public async Task Apply_PreservesCaseUnderNoCaseCollation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.ExecuteAsync("""
            CREATE TABLE CaseRows (Id INTEGER PRIMARY KEY, WorkspaceId TEXT COLLATE NOCASE);
            INSERT INTO CaseRows VALUES (1,'D:\Mixed'), (2,'d:\mixed');
            """);
        await RunAsync(fixture.Connection, false, fixture.Backup);
        Assert.Equal("D:/Mixed", await fixture.ScalarAsync("SELECT WorkspaceId FROM CaseRows WHERE Id=1"));
        Assert.Equal("d:/mixed", await fixture.ScalarAsync("SELECT WorkspaceId FROM CaseRows WHERE Id=2"));
    }

    /// <summary>TEST-MCP-PATHNORM-001: provider copy selection retains auth/options and applies read-only only to dry-run.</summary>
    [Fact]
    public void Command_PostgresApplyCopy()
    {
        var source = "Host=localhost;Database=fixture-source;Username=fixture;Password=not-a-secret;Options=-c statement_timeout=1000";
        var type = typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType(
            "QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekeyCommand", true)!;
        var dry = new Npgsql.NpgsqlConnectionStringBuilder((string)type.GetMethod("SelectCopyConnectionString")!
            .Invoke(null, ["postgresql", source, "fixture-copy"])!);
        Assert.Equal("fixture-copy", dry.Database);
        Assert.Contains("default_transaction_read_only=on", dry.Options);
        var method = type.GetMethod("SelectApplyCopyConnectionString");
        Assert.NotNull(method);
        var apply = new Npgsql.NpgsqlConnectionStringBuilder((string)method.Invoke(null, ["postgresql", source, "fixture-copy"])!);
        Assert.Equal("fixture-copy", apply.Database);
        Assert.Equal("-c statement_timeout=1000", apply.Options);
        Assert.Equal("not-a-secret", apply.Password);
    }

    /// <summary>TEST-MCP-PATHNORM-001: SQL Server copy catalog differs before a connection is opened.</summary>
    [Fact]
    public void Command_SqlServerCopyCatalog()
    {
        var source = "Server=localhost;Initial Catalog=fixture-source;Integrated Security=True";
        var type = typeof(McpDatabaseEncryptionTransitionCommand).Assembly.GetType(
            "QBrainAi.Support.Mcp.DatabaseMaintenance.WorkspacePathRekeyCommand", true)!;
        var method = type.GetMethod("SelectCopyConnectionString")!;
        var copy = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder((string)method.Invoke(null,
            ["sqlserver", source, "fixture-copy"])!);
        Assert.Equal("fixture-copy", copy.InitialCatalog);
        Assert.True(copy.IntegratedSecurity);
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null,
            ["sqlserver", source, "FIXTURE-SOURCE"]));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    /// <summary>TEST-MCP-PATHNORM-001: the read-only guard does not create an absent database before normal first-start migrations.</summary>
    [Fact]
    public async Task Guard_FreshDatabaseDoesNotCreateFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"p15-fresh-{Guid.NewGuid():N}.db");
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            var method = Subject.GetMethod("ValidateStartupAsync")!;
            await (Task)method.Invoke(null, [connection, TestContext.Current.CancellationToken])!;
            Assert.False(File.Exists(path));
            Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>TEST-MCP-PATHNORM-001: execute the real CLI against a synthetic copy and verify both files remain byte-identical and no secret is reported.</summary>
    [Fact]
    public async Task Cli_DryRun_UsesOnlySyntheticCopy()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Connection.CloseAsync();
        var directory = Path.Combine(Path.GetTempPath(), $"p15-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var configured = Path.Combine(directory, "configured.db");
        File.Copy(fixture.Path, configured);
        var before = await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken);
        var config = new { Mcp = new { Database = new { Provider = "sqlite", Sqlite = new { DataSource = configured } } } };
        await File.WriteAllTextAsync(Path.Combine(directory, "appsettings.json"), JsonSerializer.Serialize(config), TestContext.Current.CancellationToken);
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var key in start.Environment.Keys.Where(k =>
                k.StartsWith("MCP", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith("QBRAIN", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith("QBAI", StringComparison.OrdinalIgnoreCase)
                || k.StartsWith("ConnectionStrings__", StringComparison.OrdinalIgnoreCase)
                || k.EndsWith("CONTENTROOT", StringComparison.OrdinalIgnoreCase)).ToArray())
                start.Environment.Remove(key);
            start.ArgumentList.Add(typeof(McpDatabaseEncryptionTransitionCommand).Assembly.Location);
            start.ArgumentList.Add("--workspace-path-rekey");
            start.ArgumentList.Add("--dry-run");
            start.ArgumentList.Add("--copy-database");
            start.ArgumentList.Add(fixture.Path);
            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.True(process.ExitCode == 0, await error);
            using var report = JsonDocument.Parse(await output);
            Assert.True(report.RootElement.GetProperty("DryRun").GetBoolean());
            Assert.Contains("C:/Mixed", await output);
            Assert.DoesNotContain("never-print-this-secret", await output);
            Assert.Equal(before, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
            Assert.Equal(before, await File.ReadAllBytesAsync(configured, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Independent digest of a contract literal, without calling production identity code.</summary>
    private static string Digest(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    private static async Task<string> RunAsync(DbConnection connection, bool dryRun, string? backup)
    {
        try
        {
            var task = (Task)Subject.GetMethod("RunAsync")!.Invoke(null, [connection, dryRun, backup, CancellationToken.None])!;
            await task;
            return JsonSerializer.Serialize(task.GetType().GetProperty("Result")!.GetValue(task));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"p15-{Guid.NewGuid():N}.db");
        internal string Backup => Path + ".bak";
        internal SqliteConnection Connection { get; private set; } = null!;

        internal static async Task<Fixture> CreateAsync()
        {
            var value = new Fixture();
            value.Connection = new SqliteConnection($"Data Source={value.Path};Foreign Keys=True;Pooling=False");
            await value.Connection.OpenAsync();
            await value.ExecuteAsync("""
                CREATE TABLE Workspaces (WorkspaceId TEXT PRIMARY KEY, WorkspacePath TEXT UNIQUE, Secret TEXT);
                CREATE TABLE Parent (WorkspaceId TEXT, Id TEXT, PRIMARY KEY (WorkspaceId, Id), FOREIGN KEY(WorkspaceId) REFERENCES Workspaces(WorkspaceId));
                CREATE TABLE Child (WorkspaceId TEXT, ParentId TEXT, FOREIGN KEY(WorkspaceId, ParentId) REFERENCES Parent(WorkspaceId, Id));
                CREATE TABLE TriageGroups (WorkspaceId TEXT, GroupId TEXT PRIMARY KEY, GroupKey TEXT UNIQUE, EffectiveWorkspacePath TEXT, FOREIGN KEY(WorkspaceId) REFERENCES Workspaces(WorkspaceId));
                CREATE TABLE TriageReports (WorkspaceId TEXT, GroupId TEXT, Fingerprint TEXT, OriginalWorkspacePath TEXT, EffectiveWorkspacePath TEXT, FOREIGN KEY(GroupId) REFERENCES TriageGroups(GroupId));
                INSERT INTO Workspaces VALUES ('C:\Mixed', 'C:\Mixed', 'never-print-this-secret');
                INSERT INTO Parent VALUES ('C:\Mixed', 'p1');
                INSERT INTO Child VALUES ('C:\Mixed', 'p1');
                INSERT INTO TriageGroups VALUES ('C:\Mixed', 'manual-group', 'manual-key', 'C:\Mixed');
                INSERT INTO TriageReports VALUES ('C:\Mixed', 'manual-group', 'fingerprint', 'C:\Mixed', 'C:\Mixed');
                """);
            return value;
        }

        internal async Task ExecuteAsync(string sql, string? parameter = null)
        {
            await using var command = Connection.CreateCommand();
            command.CommandText = sql;
            if (parameter is not null) command.Parameters.AddWithValue("@p", parameter);
            await command.ExecuteNonQueryAsync();
        }

        internal async Task<object?> ScalarAsync(string sql)
        {
            await using var command = Connection.CreateCommand();
            command.CommandText = sql;
            return await command.ExecuteScalarAsync();
        }

        internal async Task<string> RowsAsync()
        {
            var rows = new List<string>();
            foreach (var table in new[] { "Workspaces", "Parent", "Child", "TriageGroups", "TriageReports" })
            {
                await using var command = Connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY 1, 2";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    rows.Add(table + ":" + string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)));
            }
            return string.Join("\n", rows);
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            foreach (var file in new[] { Path, Backup, Backup + ".second" })
                if (File.Exists(file)) File.Delete(file);
        }
    }
}
