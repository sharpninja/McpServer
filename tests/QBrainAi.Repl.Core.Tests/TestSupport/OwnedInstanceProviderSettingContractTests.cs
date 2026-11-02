using QBrainAi.TestSupport.TemporaryInstance;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4: fixed owned-setting maps distinguish database engines from TODO and other providers.
/// The same rows and atomic replacement assertions bind scripted and real adapters; no ambient state or IO is used.
/// </summary>
public sealed class OwnedInstanceProviderSettingContractTests
{
    private const string Root = "/scratch/owned";

    /// <summary>A literal setting and its independently specified acceptance outcome.</summary>
    /// <param name="Key">Explicit configuration or environment setting name.</param>
    /// <param name="Value">Explicit setting value.</param>
    /// <param name="Accepted">Whether the owned-instance boundary accepts this map.</param>
    internal sealed record ProviderCase(string Key, string Value, bool Accepted);

    private static readonly ProviderCase[] Cases =
    [
        new("Mcp__TodoStorage__Provider", "database", true),
        new("QBrainAi__TodoStorage__Provider", "database", true),
        new("Mcp__Embedding__Provider", "onnx", true),
        new("Mcp__Database__Provider", "sqlite", true),
        new("Mcp__Database__Provider", "sqlserver", false),
        new("QBrainAi__Database__Provider", "sqlite", true),
        new("QBrainAi__Database__Provider", "postgresql", false),
        new("Mcp__DatabaseProvider", "sqlite", true),
        new("Mcp__DatabaseProvider", "sqlserver", false),
        new("QBrainAi__DatabaseProvider", "sqlite", true),
        new("QBrainAi__DatabaseProvider", "postgresql", false),
        new("MCP_DATABASE_PROVIDER", "sqlite", true),
        new("MCP_DATABASE_PROVIDER", "postgresql", false),
        new("QBRAINAI_DATABASE_PROVIDER", "sqlite", true),
        new("QBRAINAI_DATABASE_PROVIDER", "sqlserver", false),
        new("DOTNET_Mcp__Database__Provider", "postgresql", false),
        new("ASPNETCORE_QBrainAi__Database__Provider", "sqlite", true),
        new("Mcp__Instances__owned__Database__Provider", "sqlite", true),
        new("QBrainAi__Instances__owned__DatabaseProvider", "sqlserver", false),
        new("mCp__dAtAbAsE__pRoViDeR", "SQLITE", true),
        new("Mcp__Database__Provider", "Microsoft.EntityFrameworkCore.Sqlite", true),
        new("Mcp__Database__Provider", "not-a-provider", false),
    ];

    /// <summary>The independently scripted provider replies satisfy every fixed case before implementation.</summary>
    [Fact]
    public void ScriptedProviderCheck_SatisfiesEveryRow() => AssertCheck(ReferenceReply);

    /// <summary>Every individual acceptance/refusal assertion rejects its own deliberately wrong reply.</summary>
    [Fact]
    public void WrongProviderChecks_AreRejectedForEveryRow()
    {
        foreach (var target in Cases)
            Assert.ThrowsAny<XunitException>(() => AssertCheck(row => ReferenceEquals(row, target)
                ? target.Accepted ? "wrong refusal" : null : ReferenceReply(row)));
    }

    /// <summary>Scripted map replacement preserves unrelated values and refuses before mutation for every rejected row.</summary>
    [Fact]
    public void ScriptedProcessAdapter_SatisfiesEveryRowAndAtomicity() => AssertAdapter(ReferenceApply);

    /// <summary>Clearing before validation, ignoring a refusal and dropping the inherited PATH all fail actual assertions.</summary>
    [Fact]
    public void WrongProcessAdapters_AreRejected()
    {
        Action<ProviderCase, IDictionary<string, string?>>[] wrong =
        [
            (row, target) => { target.Clear(); ReferenceApply(row, target); },
            ApplyLiteralSettings,
            (row, target) => { ReferenceApply(row, target); target.Remove("PATH"); },
        ];
        foreach (var apply in wrong)
            Assert.ThrowsAny<XunitException>(() => AssertAdapter(apply));
    }

    /// <summary>The production setting check binds every unchanged mock-gated provider row (mock gate ug-r9-mockgate-r-1).</summary>
    [Fact]
    public void RealProviderCheck_SatisfiesMockGatedRows() => AssertCheck(row =>
    {
        var settings = Settings(row);
        var child = settings.ToDictionary(pair => pair.Key, pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
        return TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(child, settings, Root);
    });

    /// <summary>The actual process adapter binds the same acceptance, refusal and atomic replacement assertions.</summary>
    [Fact]
    public void RealProcessAdapter_SatisfiesMockGatedRowsAndAtomicity() =>
        AssertAdapter((row, target) => OwnedInstanceProcess.ReplaceEnvironment(target, Settings(row), Root));

    private static void AssertCheck(Func<ProviderCase, string?> check)
    {
        foreach (var row in Cases)
            Assert.True(row.Accepted == (check(row) is null), row.Key + "=" + row.Value);
    }

    private static void AssertAdapter(Action<ProviderCase, IDictionary<string, string?>> apply)
    {
        foreach (var row in Cases)
        {
            var target = new Dictionary<string, string?> { ["MCP_INSTANCE"] = "foreign", ["PATH"] = "/tools" };
            if (!row.Accepted)
            {
                Assert.Throws<InvalidOperationException>(() => apply(row, target));
                Assert.Equal(2, target.Count);
                Assert.True(target.TryGetValue("MCP_INSTANCE", out var original));
                Assert.Equal("foreign", original);
            }
            else
            {
                apply(row, target);
                var settings = Settings(row);
                Assert.Equal(settings.Count + 1, target.Count);
                foreach (var pair in settings)
                {
                    Assert.True(target.TryGetValue(pair.Key, out var actual));
                    Assert.Equal(pair.Value, actual);
                }
                Assert.False(target.ContainsKey("MCP_INSTANCE"));
            }
            Assert.True(target.TryGetValue("PATH", out var path));
            Assert.Equal("/tools", path);
        }
    }

    private static Dictionary<string, string> Settings(ProviderCase row)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mcp__Database__Provider"] = "sqlite",
            ["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=/scratch/owned/data/main.db",
        };
        settings[row.Key] = row.Value;
        return settings;
    }

    private static string? ReferenceReply(ProviderCase row) => row.Key switch
    {
        "Mcp__TodoStorage__Provider" or "QBrainAi__TodoStorage__Provider" => null,
        "Mcp__Embedding__Provider" => null,
        _ => row.Value is "sqlite" or "SQLITE" or "Microsoft.EntityFrameworkCore.Sqlite" ? null : "non-SQLite engine",
    };

    private static void ReferenceApply(ProviderCase row, IDictionary<string, string?> target)
    {
        if (ReferenceReply(row) is not null) throw new InvalidOperationException("non-SQLite engine");
        ApplyLiteralSettings(row, target);
    }

    private static void ApplyLiteralSettings(ProviderCase row, IDictionary<string, string?> target)
    {
        target.TryGetValue("PATH", out var path);
        target.Clear();
        target["PATH"] = path;
        foreach (var pair in Settings(row)) target[pair.Key] = pair.Value;
    }
}
