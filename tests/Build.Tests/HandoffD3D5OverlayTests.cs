using System.Reflection;
using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// Overlay G3 D3: C3 ValidateTraceability against live <c>docs/Project</c> via the shipped
/// <see cref="TraceabilityValidator"/> and the Nuke <c>ValidateTraceability</c> target.
/// TEST-HANDOFF-007 / FR-HANDOFF-001 through FR-HANDOFF-007. Skipped tests are failures.
/// </summary>
public sealed class HandoffD3D5OverlayTests
{
    private static readonly Regex FrHeadingPattern = new(
        @"^##\s+(FR-[A-Z0-9]+(?:[-.–]+[A-Z0-9]+)*)\b",
        RegexOptions.Compiled);

    /// <summary>
    /// D3: the shipped Nuke target exists and invokes TraceabilityValidator.Validate on docs/Project.
    /// </summary>
    [Fact]
    public void D3_NukeValidateTraceabilityTarget_InvokesShippedValidatorOnDocsProject()
    {
        var target = typeof(Build).GetProperty("ValidateTraceability", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(target);

        var sourcePath = Path.Combine(FindRepositoryRoot(), "build", "Build.ValidateTraceability.cs");
        Assert.True(File.Exists(sourcePath), sourcePath);
        var source = File.ReadAllText(sourcePath);
        Assert.Contains("TraceabilityValidator.Validate", source, StringComparison.Ordinal);
        Assert.Contains("docs", source, StringComparison.Ordinal);
        Assert.Contains("Project", source, StringComparison.Ordinal);
        Assert.Contains("Functional-Requirements.md", source, StringComparison.Ordinal);
        Assert.Contains("Technical-Requirements.md", source, StringComparison.Ordinal);
        Assert.Contains("Testing-Requirements.md", source, StringComparison.Ordinal);
        Assert.Contains("TR-per-FR-Mapping.md", source, StringComparison.Ordinal);
        Assert.Contains("Requirements-Matrix.md", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// D3 / C3: TraceabilityValidator.Validate on live docs/Project reports no FR mapping/matrix gaps,
    /// and every live FR-HANDOFF heading is covered.
    /// </summary>
    [Fact]
    public void D3_ValidateTraceability_ShippedValidator_PassesOnLiveDocsProject()
    {
        var docsPath = Path.Combine(FindRepositoryRoot(), "docs", "Project");
        var functionalLines = File.ReadAllLines(Path.Combine(docsPath, "Functional-Requirements.md"));
        var technicalLines = File.ReadAllLines(Path.Combine(docsPath, "Technical-Requirements.md"));
        var testingLines = File.ReadAllLines(Path.Combine(docsPath, "Testing-Requirements.md"));
        var mappingLines = File.ReadAllLines(Path.Combine(docsPath, "TR-per-FR-Mapping.md"));
        var matrixLines = File.ReadAllLines(Path.Combine(docsPath, "Requirements-Matrix.md"));

        var result = TraceabilityValidator.Validate(
            functionalLines,
            technicalLines,
            testingLines,
            mappingLines,
            matrixLines);

        Assert.False(
            result.HasFrErrors,
            "C3 FR gaps: mapping=[" + string.Join(",", result.MissingFrInMapping)
            + "] matrix=[" + string.Join(",", result.MissingFrInMatrix) + "]");

        var liveHandoffFr = TraceabilityValidator.GetIdsFromHeadings(functionalLines, FrHeadingPattern)
            .Where(id => id.StartsWith("FR-HANDOFF-", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(liveHandoffFr);

        foreach (var id in liveHandoffFr)
        {
            Assert.DoesNotContain(id, result.MissingFrInMapping);
            Assert.DoesNotContain(id, result.MissingFrInMatrix);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
