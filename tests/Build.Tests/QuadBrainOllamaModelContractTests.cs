using Xunit.Sdk;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-016 AC-001 (BDPv4 mock gate): the required QuadBrain Ollama model is QBRAINAI_QUADBRAIN_OLLAMA_MODEL when
/// it holds a non-blank value, else MCP_QUADBRAIN_OLLAMA_MODEL when it does, else gemma4:e4b, trimmed. Fixed rows over
/// explicit inputs and an independent scripted reference; wrong selections (legacy first, the earlier provisioning rule
/// that only skips an unset canonical value, legacy only, no trim) are rejected by the same assertions inside this passing
/// harness. No process environment, checkout or network is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001 (appended: the required model is chosen by one rule that InstallOllama and QuadBrainOllamaEndpointIntegrationTests both use): added the selection contract, because hostile validation finding R9-F5 showed provisioning preferred the canonical variable while the cases read only the legacy one (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// </list>
/// </remarks>
public sealed class QuadBrainOllamaModelContractTests
{
    /// <summary>The default model the AC names.</summary>
    internal const string DefaultModel = "gemma4:e4b";

    /// <summary>Explicit canonical and legacy inputs and the model the rule must select.</summary>
    internal static readonly (string Name, string? Canonical, string? Legacy, string Expected)[] Rows =
    [
        ("canonical wins over legacy", "model-a", "model-b", "model-a"),
        ("empty canonical uses legacy", "", "model-b", "model-b"),
        ("whitespace canonical uses legacy", "   ", "model-b", "model-b"),
        ("unset canonical uses legacy", null, "model-b", "model-b"),
        ("no override uses the default", null, null, DefaultModel),
        ("blank values use the default", " ", "", DefaultModel),
        ("a value is trimmed", "  model-a  ", null, "model-a"),
    ];

    /// <summary>The scripted reference satisfies every row.</summary>
    [Fact]
    public void ScriptedSelection_SatisfiesEveryRow() => AssertSelection(ScriptedSelect);

    /// <summary>Each wrong selection fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("legacy first")]
    [InlineData("skips only an unset canonical value")]
    [InlineData("legacy only")]
    [InlineData("no trim")]
    public void WrongSelections_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertSelection((canonical, legacy) => fault switch
        {
            "legacy first" => ScriptedSelect(legacy, canonical),
            // The earlier provisioning rule: an empty canonical value hides the legacy one, and whitespace becomes an empty model.
            "skips only an unset canonical value" => (canonical ?? legacy) is { Length: > 0 } configured ? configured.Trim() : DefaultModel,
            "legacy only" => string.IsNullOrWhiteSpace(legacy) ? DefaultModel : legacy.Trim(),
            "no trim" => !string.IsNullOrWhiteSpace(canonical) ? canonical : !string.IsNullOrWhiteSpace(legacy) ? legacy : DefaultModel,
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }));

    /// <summary>Asserts a selection against every row.</summary>
    /// <param name="select">The selection under test: the canonical and legacy values in, the model out.</param>
    internal static void AssertSelection(Func<string?, string?, string> select)
    {
        ArgumentNullException.ThrowIfNull(select);
        foreach (var row in Rows)
            Assert.True(string.Equals(row.Expected, select(row.Canonical, row.Legacy), StringComparison.Ordinal), $"{row.Name}: selected '{select(row.Canonical, row.Legacy)}'");
    }

    /// <summary>TR-MCP-NUKE-016 AC-001 scripted reference: the rule as the AC states it.</summary>
    /// <param name="canonical">The QBRAINAI_QUADBRAIN_OLLAMA_MODEL value, or null.</param>
    /// <param name="legacy">The MCP_QUADBRAIN_OLLAMA_MODEL value, or null.</param>
    /// <returns>The selected model.</returns>
    private static string ScriptedSelect(string? canonical, string? legacy) =>
        !string.IsNullOrWhiteSpace(canonical) ? canonical.Trim()
        : !string.IsNullOrWhiteSpace(legacy) ? legacy.Trim()
        : DefaultModel;
}
