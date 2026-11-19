using System.Security.Cryptography;
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One immutable original migration source pin; raw host line endings are not a cross-host identity.</summary>
/// <param name="Path">Exact repository-relative source path.</param>
/// <param name="GitBlobId">Stored baseline Git object identity.</param>
/// <param name="LfSha256">SHA256 of UTF8 text with CRLF normalized to LF only, preserving the BOM if any.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0MigrationSourcePin(string Path, string GitBlobId, string LfSha256);

/// <summary>Complete provider migration source inventory and ordered declared/compiled migration identities.</summary>
/// <param name="Provider">sqlite, postgresql or sqlserver.</param>
/// <param name="Assembly">Existing provider migration assembly.</param>
/// <param name="Files">All source and project files in that migration project, excluding generated bin/obj.</param>
/// <param name="MigrationIds">Ordered EF migration identities, not inferred execution success.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0OriginalMigrationChain(string Provider, string Assembly,
    IReadOnlyList<Phase0MigrationSourcePin> Files, IReadOnlyList<string> MigrationIds);

/// <summary>Reads real source inventory plus compiled EF migration discovery in the later integration adapter.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0OriginalMigrationProbe
{
    /// <summary>Reads the named provider, including every actual source and EF-discovered migration.</summary>
    /// <param name="provider">Original provider key.</param>
    /// <returns>Observed source/compiled inventory, never a passed flag.</returns>
    Phase0OriginalMigrationChain? Read(string provider);
}

/// <summary>Reusable source-integrity assertions separate from the required real original-provider upgrade fixtures.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// Source equality does not prove successful upgrades. Both-host final MigrationIntegrationTests must still run
/// the existing real SQLite/PostgreSQL/SQL Server migration/backfill cases with zero failures or skips.
/// </remarks>
internal static class Phase0OriginalMigrationContract
{
    /// <summary>Freezes all expected chains before any adapter call and checks complete identity/content inventories.</summary>
    /// <param name="probe">Independent script or later source/compiled-metadata integration adapter.</param>
    /// <param name="expected">Pinned baseline from before Dolt implementation.</param>
    internal static void AssertIntegrity(IPhase0OriginalMigrationProbe probe, IReadOnlyList<Phase0OriginalMigrationChain> expected)
    {
        var frozen = expected.Select(chain => chain with { Files = chain.Files.ToArray(), MigrationIds = chain.MigrationIds.ToArray() }).ToArray();
        Assert.Equal(new[] { "postgresql", "sqlite", "sqlserver" }, frozen.Select(chain => chain.Provider).Order(StringComparer.Ordinal));
        foreach (var baseline in frozen)
        {
            Assert.NotEmpty(baseline.Files);
            Assert.NotEmpty(baseline.MigrationIds);
            var actual = probe.Read(baseline.Provider);
            Assert.NotNull(actual);
            Assert.Equal(baseline.Provider, actual.Provider);
            Assert.Equal(baseline.Assembly, actual.Assembly);
            Assert.NotNull(actual.Files);
            Assert.Equal(baseline.Files.Count, actual.Files.Count);
            Assert.All(actual.Files, Assert.NotNull);
            Assert.Equal(baseline.Files.Select(file => file.Path).Order(StringComparer.Ordinal), actual.Files.Select(file => file.Path).Order(StringComparer.Ordinal));
            foreach (var file in baseline.Files)
            {
                var observed = Assert.Single(actual.Files, item => item.Path == file.Path);
                Assert.Equal(file.GitBlobId, observed.GitBlobId, ignoreCase: true);
                Assert.Equal(file.LfSha256, observed.LfSha256, ignoreCase: true);
            }
            Assert.NotNull(actual.MigrationIds);
            Assert.Equal(baseline.MigrationIds, actual.MigrationIds);
        }
    }
}

/// <summary>Loads immutable source-derived fixture data from the assembly without an environment dependency.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal static class Phase0OriginalMigrationFixture
{
    /// <summary>Verifies independently captured fixture bytes with CRLF-to-LF normalization only, then deserializes complete chains.</summary>
    /// <returns>Fresh chain objects so test mutations cannot contaminate another case.</returns>
    internal static Phase0OriginalMigrationChain[] Load()
    {
        using var source = typeof(Phase0OriginalMigrationFixture).Assembly.GetManifestResourceStream("Federation.Phase0.original-provider-migration-baseline.json");
        Assert.NotNull(source);
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var bytes = buffer.ToArray();
        var normalized = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal("7272ED9541B60DF2A493BCC688E9F44D07BE1DC2BB963298E4C48B13F2AE7891", Convert.ToHexString(SHA256.HashData(normalized)));
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("chains").Deserialize<Phase0OriginalMigrationChain[]>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
