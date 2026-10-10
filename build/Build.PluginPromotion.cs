using System.Text.Json;
using System.Text.Json.Serialization;
using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>
    /// Plugin promotion environment. Development is the P20 harness default.
    /// Staging and Production require an operator approval artifact.
    /// </summary>
    [Parameter("Plugin promotion environment: Development, Staging, or Production")]
    readonly string PluginPromotionEnvironment = "Development";

    /// <summary>
    /// Path to an operator approval artifact required for Staging or Production plugin promotion.
    /// </summary>
    [Parameter("Path to the operator approval artifact for Staging/Production plugin promotion")]
    readonly string PluginPromotionApproval = "";

    /// <summary>
    /// PLAN-PLUGINHANDOFF-001 P20: true when Staging or Production plugin promotion requires operator approval.
    /// </summary>
    /// <param name="environment">Development, Staging, or Production.</param>
    /// <param name="policyJson">Serialized plugin-promotion-policy.json.</param>
    /// <returns>True for Staging and Production when the policy requires approval.</returns>
    internal static bool RequiresOperatorApproval(string environment, string policyJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyJson);
        var policy = JsonSerializer.Deserialize<PluginPromotionPolicyFile>(policyJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("plugin-promotion-policy.json deserialized to null.");

        if (string.Equals(environment, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            return policy.Staging.RequireOperatorApproval;
        }

        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return policy.Production.RequireOperatorApproval;
        }

        return false;
    }

    /// <summary>
    /// PLAN-PLUGINHANDOFF-001 P20: fail-closed allow check for plugin promotion.
    /// Missing or nonexistent approval artifacts block Staging and Production.
    /// </summary>
    /// <param name="environment">Development, Staging, or Production.</param>
    /// <param name="operatorApprovalArtifactPath">Optional operator approval artifact path.</param>
    /// <param name="policyJson">Serialized plugin-promotion-policy.json.</param>
    /// <returns>False when approval is required and the artifact is missing.</returns>
    internal static bool AllowPluginPromotion(string environment, string? operatorApprovalArtifactPath, string policyJson)
    {
        if (!RequiresOperatorApproval(environment, policyJson))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(operatorApprovalArtifactPath)
            && File.Exists(operatorApprovalArtifactPath);
    }

    /// <summary>
    /// Throws when Staging or Production plugin promotion is requested without an operator approval artifact.
    /// </summary>
    internal void AssertPluginPromotionAllowed()
    {
        var policyPath = RootDirectory / "build" / "plugin-promotion-policy.json";
        if (!File.Exists(policyPath))
        {
            throw new FileNotFoundException("plugin-promotion-policy.json is missing.", policyPath);
        }

        var policyJson = File.ReadAllText(policyPath);
        if (!AllowPluginPromotion(PluginPromotionEnvironment, PluginPromotionApproval, policyJson))
        {
            throw new InvalidOperationException(
                "Staging and Production plugin promotion is blocked without an operator approval artifact. Pass --plugin-promotion-approval <path>.");
        }

        Log.Information(
            "Plugin promotion allowed for environment {Environment} (approval required={Required}).",
            PluginPromotionEnvironment,
            RequiresOperatorApproval(PluginPromotionEnvironment, policyJson));
    }

    private sealed class PluginPromotionPolicyFile
    {
        public PluginPromotionPolicyEnvironment Staging { get; set; } = new();

        public PluginPromotionPolicyEnvironment Production { get; set; } = new();
    }

    private sealed class PluginPromotionPolicyEnvironment
    {
        [JsonPropertyName("requireOperatorApproval")]
        public bool RequireOperatorApproval { get; set; }
    }
}
