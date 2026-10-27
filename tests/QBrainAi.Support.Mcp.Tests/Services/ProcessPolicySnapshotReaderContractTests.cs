using System.Security.Cryptography;
using System.Text;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

#if PROCESS_MOCK
using TargetReader = QBrainAi.Support.Mcp.Tests.Services.MockProcessPolicySnapshotReader;
#else
using TargetReader = QBrainAi.Support.Mcp.Services.ProcessPolicySnapshotReader;
#endif

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-PROCESS-002 increment A contract: identical cases use a behavior-providing
/// reader double first, then the real EF-backed exact snapshot reader.
/// </summary>
public sealed class ProcessPolicySnapshotReaderContractTests
{
    private const string Id = "MEMORY-DEVPROCESS-001";
    private const string TextV1 = "Process policy version one.";
    private const string TextV2 = "Process policy version two, exact bytes.";

    [Fact]
    public async Task ExactIdVersionScopeAndDigestReturnsPinnedBytes()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync(Id, 2, "Global", Digest(TextV2));
        Assert.Equal("ok", actual.Status);
        Assert.Equal(Id, actual.Id);
        Assert.Equal(2, actual.Version);
        Assert.Equal("Global", actual.Scope);
        Assert.Equal(TextV2, actual.Text);
        Assert.Equal(Digest(TextV2), actual.Sha256);
    }

    [Fact]
    public async Task PreviousImmutableVersionRemainsAvailableAfterCurrentChanges()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync(Id, 1, "Global", Digest(TextV1));
        Assert.Equal("ok", actual.Status);
        Assert.Equal(TextV1, actual.Text);
        Assert.Equal(1, actual.Version);
    }

    [Fact]
    public async Task NearMatchIdCannotSubstituteForPinnedId()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync("MEMORY-DEVPROCESS-01", 2, "Global", Digest(TextV2));
        Assert.Equal("confirmed_not_found", actual.Status);
        Assert.Null(actual.Text);
    }

    [Fact]
    public async Task MissingPinnedVersionIsStale()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync(Id, 3, "Global", Digest(TextV2));
        Assert.Equal("PROCESS_POLICY_STALE", actual.Status);
        Assert.Null(actual.Text);
    }

    [Fact]
    public async Task WrongScopeIsStale()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync(Id, 2, "Workspace", Digest(TextV2));
        Assert.Equal("PROCESS_POLICY_STALE", actual.Status);
        Assert.Null(actual.Text);
    }

    [Fact]
    public async Task ChangedByteFailsPinnedDigest()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync(Id, 2, "Global", Digest("Process policy version two, exact byte."));
        Assert.Equal("PROCESS_POLICY_STALE", actual.Status);
        Assert.Null(actual.Text);
    }

    [Fact]
    public async Task CaseChangedIdIsNotAnExactMatch()
    {
        using var fixture = new PolicyFixture();
        var actual = await new TargetReader(fixture.Db)
            .GetExactAsync("memory-devprocess-001", 2, "Global", Digest(TextV2));
        Assert.Equal("confirmed_not_found", actual.Status);
        Assert.Null(actual.Text);
    }

    private static string Digest(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private sealed class PolicyFixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public McpDbContext Db { get; }

        public PolicyFixture()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(_connection).Options;
            Db = new McpDbContext(options);
            Db.Database.EnsureCreated();
            Db.Memories.Add(new MemoryEntity
            {
                Id = Id,
                Category = "DEVPROCESS",
                Scope = MemoryEntity.GlobalScope,
                Text = TextV2,
                Version = 2,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            Db.SaveChanges();
            Db.MemoryVersions.AddRange(
                new MemoryVersionEntity
                {
                    MemoryId = Id,
                    VersionNumber = 1,
                    Content = TextV1,
                    CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                },
                new MemoryVersionEntity
                {
                    MemoryId = Id,
                    VersionNumber = 2,
                    Content = TextV2,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            Db.SaveChanges();
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }
}

#if PROCESS_MOCK
/// <summary>Test-double result with the intended exact policy reader contract.</summary>
public sealed record MockProcessPolicySnapshotResult(
    string Status, string? Id = null, int Version = 0, string? Scope = null,
    string? Text = null, string? Sha256 = null);

/// <summary>Behavior-providing policy double used only in PROCESS_MOCK mode.</summary>
public sealed class MockProcessPolicySnapshotReader
{
    private readonly McpDbContext _db;
    public MockProcessPolicySnapshotReader(McpDbContext db) => _db = db;

    public Task<MockProcessPolicySnapshotResult> GetExactAsync(
        string id, int version, string scope, string expectedSha256)
    {
        var parent = _db.Memories.Local.FirstOrDefault(memory =>
            string.Equals(memory.Id, id, StringComparison.Ordinal));
        if (parent is null)
            return Task.FromResult(new MockProcessPolicySnapshotResult("confirmed_not_found"));
        if (!string.Equals(parent.Scope, scope, StringComparison.Ordinal))
            return Task.FromResult(new MockProcessPolicySnapshotResult("PROCESS_POLICY_STALE"));
        var rows = _db.MemoryVersions.Local.Where(snapshot =>
            string.Equals(snapshot.MemoryId, id, StringComparison.Ordinal) &&
            snapshot.VersionNumber == version).ToArray();
        if (rows.Length != 1)
            return Task.FromResult(new MockProcessPolicySnapshotResult("PROCESS_POLICY_STALE"));
        var text = rows[0].Content;
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        if (!string.Equals(sha, expectedSha256, StringComparison.Ordinal))
            return Task.FromResult(new MockProcessPolicySnapshotResult("PROCESS_POLICY_STALE"));
        return Task.FromResult(new MockProcessPolicySnapshotResult(
            "ok", id, version, scope, text, sha));
    }
}
#endif
