using McpServer.SessionLog.Transcripts;
using Microsoft.Data.Sqlite;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>OpenCode SQLite snapshot contract tests for transcript ingestion.</summary>
public sealed class OpenCodeSqliteTranscriptTests
{
    /// <summary>Verifies OpenCode SQLite snapshots are detected as independent transcript bundles.</summary>
    [Fact]
    public async Task Detector_DiscoversOpenCodeSqliteSnapshot()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-opencode-sqlite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var databasePath = Path.Combine(tempDirectory, "opencode.sqlite");
            await CreateOpenCodeSqliteFixtureAsync(databasePath).ConfigureAwait(true);
            ITranscriptBundleDetector detector = new TranscriptBundleDetector();

            var bundles = await detector.DetectAsync(databasePath, recursive: false, TestContext.Current.CancellationToken).ConfigureAwait(true);

            var bundle = Assert.Single(bundles);
            Assert.Equal(TranscriptSourceKind.OpenCode, bundle.SourceKind);
            Assert.Equal(databasePath, Assert.Single(bundle.Files));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>Verifies OpenCode SQLite snapshots normalize without writing to the source database file.</summary>
    [Fact]
    public async Task IngestionService_NormalizesOpenCodeSqliteSnapshotWithoutWritingSource()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-opencode-sqlite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var databasePath = Path.Combine(tempDirectory, "opencode.db");
            await CreateOpenCodeSqliteFixtureAsync(databasePath).ConfigureAwait(true);
            var sourceInfo = new FileInfo(databasePath);
            var originalLastWrite = sourceInfo.LastWriteTimeUtc;
            var service = TranscriptIngestionService.CreateDefault();

            var result = await service.IngestPathAsync(new TranscriptIngestionRequest(databasePath)
            {
                SourceKind = TranscriptSourceKind.OpenCode,
                Persist = false,
                CompatibilityProfile = TranscriptCompatibilityProfile.None
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);

            sourceInfo.Refresh();
            var session = Assert.Single(result.Sessions);
            Assert.Equal("ses_sqlite_fixture", session.SessionId);
            Assert.Equal("ses_sqlite_fixture", session.NativeSessionId);
            Assert.Equal("opencode/gpt-test", session.Model);
            Assert.Equal("F:/GitHub/SampleWorkspace", session.WorkspacePath);
            Assert.Contains(session.Events, item => item.Role.Equals("user", StringComparison.Ordinal) && JoinText(item.Content).Contains("hello from sqlite", StringComparison.Ordinal));
            Assert.Contains(session.Events, item => item.Role.Equals("assistant", StringComparison.Ordinal) && JoinText(item.Content).Contains("reply from sqlite", StringComparison.Ordinal));
            Assert.Contains(session.Events, item => item.NativeType.Equals("tool_event", StringComparison.Ordinal) && JoinText(item.Content).Contains("sqlite tool result", StringComparison.Ordinal));
            Assert.Contains("sourceType: OpenCode", session.CanonicalYaml, StringComparison.Ordinal);
            Assert.Contains("sessionId: ses_sqlite_fixture", session.CanonicalYaml, StringComparison.Ordinal);
            Assert.Equal(originalLastWrite, sourceInfo.LastWriteTimeUtc);
            Assert.False(File.Exists(databasePath + "-wal"), "Read-only ingestion must not create or mutate a source WAL file.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static async Task CreateOpenCodeSqliteFixtureAsync(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "CREATE TABLE session (id TEXT PRIMARY KEY, title TEXT, version TEXT, time_created INTEGER, time_updated INTEGER, workspace_path TEXT);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "CREATE TABLE message (id TEXT PRIMARY KEY, session_id TEXT NOT NULL, role TEXT NOT NULL, model_id TEXT, provider_id TEXT, time_created INTEGER, time_completed INTEGER);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "CREATE TABLE part (id TEXT PRIMARY KEY, message_id TEXT NOT NULL, session_id TEXT NOT NULL, type TEXT NOT NULL, json TEXT NOT NULL);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "CREATE TABLE tool_event (id TEXT PRIMARY KEY, session_id TEXT NOT NULL, message_id TEXT, tool_name TEXT, status TEXT, payload_json TEXT);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO session (id, title, version, time_created, time_updated, workspace_path) VALUES ('ses_sqlite_fixture', 'SQLite Fixture', '1.0', 1735689600000, 1735689602000, 'F:/GitHub/SampleWorkspace');").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO message (id, session_id, role, model_id, provider_id, time_created, time_completed) VALUES ('msg-user', 'ses_sqlite_fixture', 'user', NULL, 'opencode', 1735689600000, 1735689600000);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO message (id, session_id, role, model_id, provider_id, time_created, time_completed) VALUES ('msg-assistant', 'ses_sqlite_fixture', 'assistant', 'opencode/gpt-test', 'opencode', 1735689601000, 1735689602000);").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO part (id, message_id, session_id, type, json) VALUES ('part-user', 'msg-user', 'ses_sqlite_fixture', 'text', '{\"text\":\"hello from sqlite\"}');").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO part (id, message_id, session_id, type, json) VALUES ('part-assistant', 'msg-assistant', 'ses_sqlite_fixture', 'text', '{\"text\":\"reply from sqlite\"}');").ConfigureAwait(true);
        await ExecuteNonQueryAsync(connection, "INSERT INTO tool_event (id, session_id, message_id, tool_name, status, payload_json) VALUES ('tool-sqlite', 'ses_sqlite_fixture', 'msg-assistant', 'shell', 'completed', '{\"content\":\"sqlite tool result\"}');").ConfigureAwait(true);
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static string JoinText(IEnumerable<TranscriptContentBlock> blocks)
        => string.Join("\n", blocks.Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));
}
