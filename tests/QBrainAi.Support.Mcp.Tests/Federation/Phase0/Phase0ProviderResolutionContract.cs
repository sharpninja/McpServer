namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Factory paths required by FR-MCP-186 ac-FourProvidersResolve.</summary>
/// <remarks>2026-11-17: TR-MCP-FEDERATION-013; TEST-MCP-234. Test seam only, before real binding.</remarks>
public enum Phase0FactoryPath
{
    /// <summary>The application's runtime provider factory.</summary>
    Runtime,
    /// <summary>The EF tooling design-time context factory.</summary>
    DesignTime,
}

/// <summary>Observable factory selection, independent of an EF adapter.</summary>
/// <param name="CanonicalName">Selected canonical provider, or null on refusal.</param>
/// <param name="StrategyType">Selected strategy type, or null on refusal.</param>
/// <param name="MigrationAssembly">Selected history assembly, or null on refusal.</param>
/// <param name="ErrorCode">Stable refusal code, or null on success.</param>
/// <remarks>2026-11-17: FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0ProviderResolution(
    string? CanonicalName, string? StrategyType, string? MigrationAssembly, string? ErrorCode);

/// <summary>Test-only boundary for unchanged mock and later real-factory assertions.</summary>
/// <remarks>2026-11-17: FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0ProviderResolutionProbe
{
    /// <summary>Observes one factory path for the supplied key.</summary>
    /// <param name="providerKey">Canonical or unsupported fixture key.</param>
    /// <param name="path">Runtime or design-time factory.</param>
    /// <returns>Selection, refusal, or a missing-result negative control.</returns>
    Phase0ProviderResolution? Resolve(string providerKey, Phase0FactoryPath path);
}

/// <summary>Requirement-derived assertions for mock proof and later real binding.</summary>
/// <remarks>
/// 2026-11-17: FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// Original assembly names are pinned from existing strategy declarations. Dolt names come
/// from plan section 6. These assertions invoke no production factory themselves.
/// </remarks>
internal static class Phase0ProviderResolutionContract
{
    /// <summary>Gets the complete approved canonical set.</summary>
    internal static IReadOnlyList<string> ProviderKeys => ["sqlite", "postgresql", "sqlserver", "dolt"];

    /// <summary>Checks canonical identity, own strategy and own history on both paths.</summary>
    /// <param name="providerKey">Approved canonical key.</param>
    /// <param name="probe">Scripted or real factory boundary.</param>
    internal static void AssertProvider(string providerKey, IPhase0ProviderResolutionProbe probe)
    {
        var (strategy, history) = providerKey switch
        {
            "sqlite" => ("SqliteMcpDatabaseProviderStrategy", "QBrainAi.Storage.SqliteMigrations"),
            "postgresql" => ("PostgreSqlMcpDatabaseProviderStrategy", "QBrainAi.Storage.PostgreSqlMigrations"),
            "sqlserver" => ("SqlServerMcpDatabaseProviderStrategy", "QBrainAi.Storage.SqlServerMigrations"),
            "dolt" => ("DoltMcpDatabaseProviderStrategy", "QBrainAi.Storage.DoltMigrations"),
            _ => throw new ArgumentOutOfRangeException(nameof(providerKey)),
        };
        foreach (var path in Enum.GetValues<Phase0FactoryPath>())
        {
            var actual = probe.Resolve(providerKey, path);
            Assert.NotNull(actual);
            Assert.Null(actual.ErrorCode);
            Assert.Equal(providerKey, actual.CanonicalName);
            Assert.Equal(strategy, actual.StrategyType);
            Assert.Equal(history, actual.MigrationAssembly);
        }
    }

    /// <summary>Checks stable unsupported_provider refusal without a selected provider.</summary>
    /// <param name="unknownKey">Key outside the approved set.</param>
    /// <param name="probe">Scripted or real factory boundary.</param>
    internal static void AssertUnknown(string unknownKey, IPhase0ProviderResolutionProbe probe)
    {
        Assert.DoesNotContain(unknownKey, ProviderKeys);
        foreach (var path in Enum.GetValues<Phase0FactoryPath>())
        {
            var actual = probe.Resolve(unknownKey, path);
            Assert.NotNull(actual);
            Assert.Equal("unsupported_provider", actual.ErrorCode);
            Assert.Null(actual.CanonicalName);
            Assert.Null(actual.StrategyType);
            Assert.Null(actual.MigrationAssembly);
        }
    }

    /// <summary>Checks that the four strategies and history assemblies remain distinct.</summary>
    /// <param name="probe">Scripted or real factory boundary.</param>
    internal static void AssertDistinctSelections(IPhase0ProviderResolutionProbe probe)
    {
        foreach (var path in Enum.GetValues<Phase0FactoryPath>())
        {
            var selections = ProviderKeys.Select(key => probe.Resolve(key, path)).ToArray();
            Assert.All(selections, selection => Assert.NotNull(selection));
            Assert.Equal(4, selections.Select(selection => selection!.StrategyType).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(4, selections.Select(selection => selection!.MigrationAssembly).Distinct(StringComparer.Ordinal).Count());
        }
    }
}
