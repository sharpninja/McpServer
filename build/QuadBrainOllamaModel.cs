namespace QBrainAi.Build.Shared;

/// <summary>
/// TR-MCP-NUKE-016 AC-001: the one rule that chooses the QuadBrain Ollama model. The required model is
/// <see cref="CanonicalVariable"/> when it holds a non-blank value, else <see cref="LegacyVariable"/> when it does, else
/// <see cref="DefaultModel"/>, trimmed. InstallOllama provisions it and QuadBrainOllamaEndpointIntegrationTests (which links
/// this file) requests it, so both use the same model. A pure function over explicit inputs; callers read the variables.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001 (appended: the required model is chosen by one rule that InstallOllama and QuadBrainOllamaEndpointIntegrationTests both use): added, because hostile validation finding R9-F5 showed provisioning preferred the canonical variable, let an empty canonical value hide the legacy one and turned whitespace into an empty model, while the cases read only the legacy variable (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)"; contract QuadBrainOllamaModelContractTests, mock gate ug-r10-mockgate-t-1).</description></item>
/// </list>
/// </remarks>
internal static class QuadBrainOllamaModel
{
    /// <summary>The canonical variable that names the model.</summary>
    internal const string CanonicalVariable = "QBRAINAI_QUADBRAIN_OLLAMA_MODEL";

    /// <summary>The legacy variable that names the model when the canonical one is blank.</summary>
    internal const string LegacyVariable = "MCP_QUADBRAIN_OLLAMA_MODEL";

    /// <summary>
    /// The model used when neither variable holds a value: strong enough that the ArbiterOfTruth slot reliably accepts
    /// simple prompts (1b-class models reject them as ambiguous and fail the content assertions).
    /// </summary>
    internal const string DefaultModel = "gemma4:e4b";

    /// <summary>Selects the required model from the two variables' values.</summary>
    /// <param name="canonical">The <see cref="CanonicalVariable"/> value, or null when unset.</param>
    /// <param name="legacy">The <see cref="LegacyVariable"/> value, or null when unset.</param>
    /// <returns>The trimmed canonical value when it is not blank, else the trimmed legacy value when it is not blank, else <see cref="DefaultModel"/>.</returns>
    internal static string Select(string? canonical, string? legacy) =>
        !string.IsNullOrWhiteSpace(canonical) ? canonical.Trim()
        : !string.IsNullOrWhiteSpace(legacy) ? legacy.Trim()
        : DefaultModel;
}
