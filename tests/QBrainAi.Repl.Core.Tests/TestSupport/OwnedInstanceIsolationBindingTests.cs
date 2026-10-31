using QBrainAi.Repl.Host;
using QBrainAi.TestSupport.TemporaryInstance;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4/ac-12 and FR-MCP-REPL-008 ac-4: binds the unchanged slice I mock-gated
/// cases and assertions to the real shared checks, consumer, environment adapter and cache selector.
/// Inputs remain explicit maps and strings; no process, environment, file system or network is used.
/// </summary>
public sealed class OwnedInstanceIsolationBindingTests
{
    /// <summary>The real functions satisfy every exact case that passed the mock gate.</summary>
    [Fact]
    public void SharedIsolation_SatisfiesMockGatedContracts()
    {
        OwnedInstanceIsolationContractTests.AssertEnvironment(TemporaryInstanceChecks.BuildOwnedInstanceEnvironment);
        OwnedInstanceIsolationContractTests.AssertDatabase(TemporaryInstanceChecks.CheckDatabaseSelection);
        OwnedInstanceIsolationContractTests.AssertStart(environment => TemporaryInstanceChecks.CheckReplStartContext(
            ["host.dll", "--workspace-path", "/scratch/owned/workspace", "--marker-file", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"],
            environment, OwnedInstanceIsolationContractTests.Root, OwnedInstanceIsolationContractTests.Address));
        OwnedInstanceIsolationContractTests.AssertCacheDirectory(MarkerFileClientOptionsResolver.ResolveVerifiedCacheDirectory);
        OwnedInstanceIsolationContractTests.AssertHealth(TemporaryInstanceChecks.CheckHealthAnswer);
    }

    /// <summary>The actual shared REPL consumer calls its guard and propagates its refusal before returning a start.</summary>
    [Fact]
    public void RealReplConsumer_EnforcesMockGatedGuardContract() =>
        OwnedInstanceIsolationContractTests.AssertConsumer(guard =>
        {
            var plan = OwnedReplStart.CreatePlan("host.dll", "/scratch/owned/workspace",
                "/scratch/owned/workspace/AGENTS-README-FIRST.yaml", OwnedInstanceIsolationContractTests.Root,
                OwnedInstanceIsolationContractTests.Address, "test-agent", guard);
            return (plan.Arguments, plan.Settings.ToDictionary(pair => pair.Key, pair => (string?)pair.Value));
        });

    /// <summary>The same adapter used by ProcessStartInfo refuses atomically and replaces an accepted explicit dictionary.</summary>
    [Fact]
    public void RealEnvironmentAdapter_SatisfiesMockGatedAtomicityContract() =>
        OwnedInstanceIsolationContractTests.AssertAdapter((target, settings, accepted) =>
        {
            var own = new Dictionary<string, string>(settings);
            if (!accepted) own["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=/scratch/other/data/main.db";
            OwnedInstanceProcess.ReplaceEnvironment(target, own, OwnedInstanceIsolationContractTests.Root);
        });

    /// <summary>Proves every original workspace/marker row on the actual consumer, rather than repeating the pure check.</summary>
    internal static void AssertOriginalConsumerRows()
    {
        foreach (var row in TemporaryInstanceChecksTests.StartRows)
        {
            var error = Record.Exception(() => OwnedReplStart.CreatePlan("host.dll",
                ValueAfter(row.Arguments, "--workspace-path"), ValueAfter(row.Arguments, "--marker-file"), row.Root,
                OwnedInstanceIsolationContractTests.Address));
            Assert.True(row.Accepted == (error is null), row.Name);
            if (error is not null) Assert.IsType<InvalidOperationException>(error);
        }
    }

    private static string ValueAfter(string[] arguments, string name)
    {
        var index = Array.IndexOf(arguments, name);
        return index >= 0 && index < arguments.Length - 1 ? arguments[index + 1] : string.Empty;
    }
}
