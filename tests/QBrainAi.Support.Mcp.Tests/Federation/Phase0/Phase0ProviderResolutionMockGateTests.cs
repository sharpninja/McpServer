namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Discriminating mock proof for runtime/design-time four-provider resolution.</summary>
/// <remarks>
/// 2026-11-17: FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// Scripted results are independent fixtures. Passing proves assertions only; no EF provider,
/// database, environment, real factory or implementation acceptance is involved.
/// </remarks>
public sealed class Phase0ProviderResolutionMockGateTests
{
    /// <summary>Each approved provider passes the same assertions for both factory paths.</summary>
    /// <param name="providerKey">Approved canonical provider from the plan.</param>
    [Theory]
    [InlineData("sqlite")]
    [InlineData("postgresql")]
    [InlineData("sqlserver")]
    [InlineData("dolt")]
    public void CorrectSelectionsPass(string providerKey)
        => Phase0ProviderResolutionContract.AssertProvider(providerKey, new ScriptedProbe());

    /// <summary>Unknown keys produce the exact stable refusal on both paths.</summary>
    /// <param name="unknownKey">Unsupported key independent of the four approved keys.</param>
    [Theory]
    [InlineData("unknown")]
    [InlineData("dolt2")]
    public void CorrectUnknownRefusalsPass(string unknownKey)
        => Phase0ProviderResolutionContract.AssertUnknown(unknownKey, new ScriptedProbe());

    /// <summary>All four providers have separate strategies and migration histories.</summary>
    [Fact]
    public void CorrectSelectionsAreDistinct()
        => Phase0ProviderResolutionContract.AssertDistinctSelections(new ScriptedProbe());

    /// <summary>Gets every supported-key fault independently for each provider and path.</summary>
    public static IEnumerable<object[]> SupportedFaultCases
    {
        get
        {
            foreach (var provider in new[] { "sqlite", "postgresql", "sqlserver", "dolt" })
            foreach (var path in Enum.GetValues<Phase0FactoryPath>())
            foreach (var fault in new[] { Fault.WrongCanonical, Fault.WrongStrategy, Fault.WrongHistory, Fault.UnexpectedError, Fault.MissingResult })
                yield return [provider, path, fault];
        }
    }

    /// <summary>Gets every unknown-key fault independently for both paths and both keys.</summary>
    public static IEnumerable<object[]> UnknownFaultCases
    {
        get
        {
            foreach (var key in new[] { "unknown", "dolt2" })
            foreach (var path in Enum.GetValues<Phase0FactoryPath>())
            foreach (var fault in new[] { Fault.AcceptedUnknown, Fault.WrongErrorCode, Fault.UnknownHasCanonical, Fault.UnknownHasStrategy, Fault.UnknownHasHistory, Fault.MissingResult })
                yield return [key, path, fault];
        }
    }

    /// <summary>Every deliberately wrong supported selection is rejected by the real assertions.</summary>
    /// <param name="providerKey">Provider whose scripted result is corrupted.</param>
    /// <param name="path">Only this factory path is corrupted, proving both are inspected.</param>
    /// <param name="fault">Independent wrong result injected through the seam.</param>
    [Theory]
    [MemberData(nameof(SupportedFaultCases))]
    public void WrongSupportedSelectionIsRejected(string providerKey, Phase0FactoryPath path, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0ProviderResolutionContract.AssertProvider(providerKey, new ScriptedProbe(providerKey, path, fault)));

    /// <summary>Every deliberately wrong unknown-key outcome is rejected by the real assertions.</summary>
    /// <param name="unknownKey">Unsupported key whose result is corrupted.</param>
    /// <param name="path">Only this factory path is corrupted.</param>
    /// <param name="fault">Independent wrong result injected through the seam.</param>
    [Theory]
    [MemberData(nameof(UnknownFaultCases))]
    public void WrongUnknownOutcomeIsRejected(string unknownKey, Phase0FactoryPath path, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0ProviderResolutionContract.AssertUnknown(unknownKey, new ScriptedProbe(unknownKey, path, fault)));

    /// <summary>Both distinctness assertions reject duplicates on either factory path.</summary>
    /// <param name="path">Only this factory path contains a duplicate.</param>
    /// <param name="fault">Duplicate strategy or migration history.</param>
    [Theory]
    [InlineData(Phase0FactoryPath.Runtime, Fault.DuplicateStrategy)]
    [InlineData(Phase0FactoryPath.Runtime, Fault.DuplicateHistory)]
    [InlineData(Phase0FactoryPath.DesignTime, Fault.DuplicateStrategy)]
    [InlineData(Phase0FactoryPath.DesignTime, Fault.DuplicateHistory)]
    public void DuplicateSelectionsAreRejected(Phase0FactoryPath path, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0ProviderResolutionContract.AssertDistinctSelections(new ScriptedProbe("dolt", path, fault)));

    /// <summary>Wrong scripted responses used as expected negative controls.</summary>
    public enum Fault
    {
        /// <summary>No mutation.</summary>
        None,
        /// <summary>Wrong canonical provider.</summary>
        WrongCanonical,
        /// <summary>Another provider's strategy.</summary>
        WrongStrategy,
        /// <summary>Another provider's migration history.</summary>
        WrongHistory,
        /// <summary>Refusal of a supported provider.</summary>
        UnexpectedError,
        /// <summary>No result returned.</summary>
        MissingResult,
        /// <summary>Unknown provider accepted.</summary>
        AcceptedUnknown,
        /// <summary>Wrong stable refusal code.</summary>
        WrongErrorCode,
        /// <summary>Refusal incorrectly retains a canonical selection.</summary>
        UnknownHasCanonical,
        /// <summary>A second provider's strategy is reused.</summary>
        DuplicateStrategy,
        /// <summary>A second provider's migration history is reused.</summary>
        DuplicateHistory,
        /// <summary>Refusal incorrectly retains a strategy.</summary>
        UnknownHasStrategy,
        /// <summary>Refusal incorrectly retains migration history.</summary>
        UnknownHasHistory,
    }

    /// <summary>Independent fixture responses, corrupted only for one requested key/path.</summary>
    private sealed class ScriptedProbe(
        string? faultyKey = null,
        Phase0FactoryPath faultyPath = Phase0FactoryPath.Runtime,
        Fault fault = Fault.None) : IPhase0ProviderResolutionProbe
    {
        /// <inheritdoc />
        public Phase0ProviderResolution? Resolve(string providerKey, Phase0FactoryPath path)
        {
            var response = providerKey switch
            {
                "sqlite" => new Phase0ProviderResolution("sqlite", "SqliteMcpDatabaseProviderStrategy", "QBrainAi.Storage.SqliteMigrations", null),
                "postgresql" => new Phase0ProviderResolution("postgresql", "PostgreSqlMcpDatabaseProviderStrategy", "QBrainAi.Storage.PostgreSqlMigrations", null),
                "sqlserver" => new Phase0ProviderResolution("sqlserver", "SqlServerMcpDatabaseProviderStrategy", "QBrainAi.Storage.SqlServerMigrations", null),
                "dolt" => new Phase0ProviderResolution("dolt", "DoltMcpDatabaseProviderStrategy", "QBrainAi.Storage.DoltMigrations", null),
                _ => new Phase0ProviderResolution(null, null, null, "unsupported_provider"),
            };
            if (providerKey != faultyKey || path != faultyPath)
                return response;

            return fault switch
            {
                Fault.None => response,
                Fault.WrongCanonical => response with { CanonicalName = "foreign-provider" },
                Fault.WrongStrategy => response with { StrategyType = "ForeignMcpDatabaseProviderStrategy" },
                Fault.WrongHistory => response with { MigrationAssembly = "QBrainAi.Storage.ForeignMigrations" },
                Fault.UnexpectedError => response with { ErrorCode = "unsupported_provider" },
                Fault.MissingResult => null,
                Fault.AcceptedUnknown => new("dolt", "DoltMcpDatabaseProviderStrategy", "QBrainAi.Storage.DoltMigrations", null),
                Fault.WrongErrorCode => response with { ErrorCode = "invalid_input" },
                Fault.UnknownHasCanonical => response with { CanonicalName = "dolt" },
                Fault.DuplicateStrategy => response with { StrategyType = "SqliteMcpDatabaseProviderStrategy" },
                Fault.DuplicateHistory => response with { MigrationAssembly = "QBrainAi.Storage.SqliteMigrations" },
                Fault.UnknownHasStrategy => response with { StrategyType = "DoltMcpDatabaseProviderStrategy" },
                Fault.UnknownHasHistory => response with { MigrationAssembly = "QBrainAi.Storage.DoltMigrations" },
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            };
        }
    }
}
