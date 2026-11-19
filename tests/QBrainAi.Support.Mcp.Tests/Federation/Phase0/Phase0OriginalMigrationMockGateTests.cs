using System.Globalization;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Discriminates source/compiled migration inventory drift without accessing a repository or database.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-OriginalProviderMigrationsUnchanged; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// All expected values originate in the hash-pinned source baseline; observations are independent detached scripts.
/// Real original-provider migration/backfill fixtures and final both-host gates remain mandatory.
/// </remarks>
public sealed class Phase0OriginalMigrationMockGateTests
{
    /// <summary>Accepts correct complete chains, unordered source inventories and equivalent digest casing.</summary>
    /// <param name="variation">Valid presentation difference only.</param>
    [Theory]
    [InlineData("exact")]
    [InlineData("reverse-files")]
    [InlineData("digest-case")]
    public void CorrectOriginalChainsPass(string variation)
    {
        var expected = Phase0OriginalMigrationFixture.Load();
        var probe = new ScriptedProbe(Phase0OriginalMigrationFixture.Load(), variation: variation);
        Phase0OriginalMigrationContract.AssertIntegrity(probe, expected);
        Assert.Equal(expected.Select(chain => chain.Provider), probe.Calls);
    }

    /// <summary>Gets omissions, duplicates and every source/hash/migration identity defect independently.</summary>
    public static IEnumerable<object[]> WrongCases
    {
        get
        {
            foreach (var chain in Phase0OriginalMigrationFixture.Load())
            {
                foreach (var fault in new[] { "Missing", "Provider", "Assembly", "FilesNull", "FileMissing", "FileExtra", "FileDuplicate",
                    "IdsNull", "IdMissing", "IdExtra", "IdDuplicate", "IdsReverse" })
                    yield return [chain.Provider, fault];
                for (var file = 0; file < chain.Files.Count; file++)
                foreach (var corruptField in new[] { "Null", "Path", "Blob", "Sha" })
                    yield return [chain.Provider, $"File:{file}:{corruptField}"];
                for (var id = 0; id < chain.MigrationIds.Count; id++)
                    yield return [chain.Provider, $"Id:{id}"];
            }
        }
    }

    /// <summary>Every wrong inventory must trigger an assertion after its intended fault is injected.</summary>
    /// <param name="provider">Only this provider chain is changed.</param>
    /// <param name="fault">One independent source or migration identity defect.</param>
    [Theory]
    [MemberData(nameof(WrongCases))]
    public void WrongOriginalChainIsRejected(string provider, string fault)
    {
        var probe = new ScriptedProbe(Phase0OriginalMigrationFixture.Load(), provider, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OriginalMigrationContract.AssertIntegrity(probe, Phase0OriginalMigrationFixture.Load()));
        Assert.True(probe.CorruptionApplied);
    }

    /// <summary>Freezing only the current provider is insufficient: an earlier call cannot rewrite a later expectation.</summary>
    [Fact]
    public void CrossProviderExpectationRewriteIsRejected()
    {
        var expected = Phase0OriginalMigrationFixture.Load();
        var probe = new ScriptedProbe(expected, fault: "RewriteNextExpected");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OriginalMigrationContract.AssertIntegrity(probe, expected));
        Assert.True(probe.CorruptionApplied);
        Assert.Equal(2, probe.Calls.Count);
    }

    /// <summary>Changes one hexadecimal digest character while preserving a plausible digest shape.</summary>
    /// <param name="value">Correct independent digest.</param>
    private static string Flip(string value) => (value[0] == '0' ? "1" : "0") + value[1..];

    /// <summary>Returns detached source-derived observations, with an independent wrong result when requested.</summary>
    /// <param name="chains">Independent pinned fixture load.</param>
    /// <param name="faultProvider">Only this provider is corrupted.</param>
    /// <param name="fault">The single negative control.</param>
    /// <param name="variation">Valid source-order or digest-case difference.</param>
    private sealed class ScriptedProbe(Phase0OriginalMigrationChain[] chains, string faultProvider = "", string fault = "", string variation = "exact") : IPhase0OriginalMigrationProbe
    {
        /// <summary>Records requested provider order.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Proves the intended negative control was reached.</summary>
        internal bool CorruptionApplied { get; private set; }

        /// <inheritdoc />
        public Phase0OriginalMigrationChain? Read(string provider)
        {
            if (fault == "RewriteNextExpected" && Calls.Count == 0)
            {
                var shared = Assert.IsAssignableFrom<IList<Phase0MigrationSourcePin>>(chains[1].Files);
                shared[0] = shared[0] with { LfSha256 = Flip(shared[0].LfSha256) };
                CorruptionApplied = true;
            }
            Calls.Add(provider);
            var source = Assert.Single(chains, chain => chain.Provider == provider);
            var result = source with { Files = source.Files.Select(file => file with { }).ToArray(), MigrationIds = source.MigrationIds.ToArray() };
            if (variation == "reverse-files") result = result with { Files = result.Files.Reverse().ToArray() };
            if (variation == "digest-case") result = result with { Files = result.Files.Select(file => file with { GitBlobId = file.GitBlobId.ToUpperInvariant(), LfSha256 = file.LfSha256.ToLowerInvariant() }).ToArray() };
            if (provider != faultProvider) return result;
            CorruptionApplied = true;
            if (fault.StartsWith("File:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var files = result.Files.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var file = files[index];
                files[index] = parts[2] switch
                {
                    "Null" => null!, "Path" => file with { Path = file.Path + "/wrong" },
                    "Blob" => file with { GitBlobId = Flip(file.GitBlobId) }, "Sha" => file with { LfSha256 = Flip(file.LfSha256) },
                    _ => throw new ArgumentOutOfRangeException(nameof(fault))
                };
                return result with { Files = files };
            }
            if (fault.StartsWith("Id:", StringComparison.Ordinal))
            {
                var ids = result.MigrationIds.ToArray();
                ids[int.Parse(fault.Split(':')[1], CultureInfo.InvariantCulture)] = "unexpected-migration";
                return result with { MigrationIds = ids };
            }
            return fault switch
            {
                "Missing" => null, "Provider" => result with { Provider = "wrong" }, "Assembly" => result with { Assembly = "wrong" },
                "FilesNull" => result with { Files = null! }, "FileMissing" => result with { Files = result.Files.Skip(1).ToArray() },
                "FileExtra" => result with { Files = [.. result.Files, result.Files[0]] },
                "FileDuplicate" => result with { Files = [result.Files[0], result.Files[0], .. result.Files.Skip(2)] },
                "IdsNull" => result with { MigrationIds = null! }, "IdMissing" => result with { MigrationIds = result.MigrationIds.Skip(1).ToArray() },
                "IdExtra" => result with { MigrationIds = [.. result.MigrationIds, "unexpected-migration"] },
                "IdDuplicate" => result with { MigrationIds = [result.MigrationIds[0], result.MigrationIds[0], .. result.MigrationIds.Skip(2)] },
                "IdsReverse" => result with { MigrationIds = result.MigrationIds.Reverse().ToArray() },
                _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown migration inventory fault.")
            };
        }
    }
}
