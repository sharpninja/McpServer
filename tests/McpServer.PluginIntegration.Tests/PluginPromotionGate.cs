using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P20: fail-closed plugin promotion gate loader.
/// Staging and Production require an operator approval artifact. Development is the P20 harness environment.
/// </summary>
public sealed class PluginPromotionGate
{
    /// <summary>True when Staging requires an operator approval artifact.</summary>
    public required bool StagingRequiresApproval { get; init; }

    /// <summary>True when Production requires an operator approval artifact.</summary>
    public required bool ProductionRequiresApproval { get; init; }

    /// <summary>
    /// Loads the product promotion gate from build/Build.PluginPromotion.cs and build/plugin-promotion-policy.json.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    /// <returns>Loaded gate.</returns>
    public static PluginPromotionGate Load(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var sourcePath = Path.Combine(repositoryRoot, "build", "Build.PluginPromotion.cs");
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Build.PluginPromotion.cs is missing.", sourcePath);
        }

        var source = File.ReadAllText(sourcePath);
        if (!source.Contains("RequiresOperatorApproval", StringComparison.Ordinal)
            || !source.Contains("AllowPluginPromotion", StringComparison.Ordinal)
            || !source.Contains("Staging", StringComparison.Ordinal)
            || !source.Contains("Production", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Build.PluginPromotion.cs does not define the operator-approval fail-closed gate.");
        }

        var policyPath = Path.Combine(repositoryRoot, "build", "plugin-promotion-policy.json");
        if (!File.Exists(policyPath))
        {
            throw new FileNotFoundException("plugin-promotion-policy.json is missing.", policyPath);
        }

        var policy = JsonSerializer.Deserialize<PolicyFile>(File.ReadAllText(policyPath), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("plugin-promotion-policy.json deserialized to null.");

        if (!policy.Staging.RequireOperatorApproval || !policy.Production.RequireOperatorApproval)
        {
            throw new InvalidOperationException("Staging and Production must require operator approval.");
        }

        return new PluginPromotionGate
        {
            StagingRequiresApproval = policy.Staging.RequireOperatorApproval,
            ProductionRequiresApproval = policy.Production.RequireOperatorApproval,
        };
    }

    /// <summary>
    /// True when the named environment requires an operator approval artifact.
    /// </summary>
    /// <param name="environment">Development, Staging, or Production.</param>
    /// <returns>True for Staging and Production.</returns>
    public bool RequiresOperatorApproval(string environment)
    {
        if (string.Equals(environment, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            return StagingRequiresApproval;
        }

        if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return ProductionRequiresApproval;
        }

        return false;
    }

    /// <summary>
    /// Fail-closed allow check. Missing or nonexistent approval artifacts block Staging and Production.
    /// </summary>
    /// <param name="environment">Development, Staging, or Production.</param>
    /// <param name="operatorApprovalArtifactPath">Optional approval artifact path.</param>
    /// <returns>False when approval is required and the artifact is missing.</returns>
    public bool Allow(string environment, string? operatorApprovalArtifactPath)
    {
        if (!RequiresOperatorApproval(environment))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(operatorApprovalArtifactPath)
            && File.Exists(operatorApprovalArtifactPath);
    }

    private sealed class PolicyFile
    {
        public PolicyEnvironment Staging { get; set; } = new();

        public PolicyEnvironment Production { get; set; } = new();
    }

    private sealed class PolicyEnvironment
    {
        [JsonPropertyName("requireOperatorApproval")]
        public bool RequireOperatorApproval { get; set; }
    }
}
