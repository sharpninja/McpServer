using Nuke.Common;
using Serilog;

partial class Build
{
    [Parameter("Fail on missing TR/TEST coverage (default false)")]
    readonly bool StrictTrAndTestCoverage;

    /// <summary>Validate requirements traceability across FR/TR/TEST documents.</summary>
    public Target ValidateTraceability => _ => _
        .Executes(() =>
        {
            var docsPath = RootDirectory / "docs" / "Project";
            var functionalLines = File.ReadAllLines(docsPath / "Functional-Requirements.md");
            var technicalLines = File.ReadAllLines(docsPath / "Technical-Requirements.md");
            var testingLines = File.ReadAllLines(docsPath / "Testing-Requirements.md");
            var mappingLines = File.ReadAllLines(docsPath / "TR-per-FR-Mapping.md");
            var matrixLines = File.ReadAllLines(docsPath / "Requirements-Matrix.md");

            var result = TraceabilityValidator.Validate(
                functionalLines, technicalLines, testingLines, mappingLines, matrixLines);

            if (result.MissingFrInMapping.Count > 0)
            {
                Log.Warning("Missing FR in TR-per-FR-Mapping:");
                result.MissingFrInMapping.ForEach(id => Log.Warning("  - {Id}", id));
            }

            if (result.MissingFrInMatrix.Count > 0)
            {
                Log.Warning("Missing FR in Requirements-Matrix:");
                result.MissingFrInMatrix.ForEach(id => Log.Warning("  - {Id}", id));
            }

            if (result.MissingTrInMatrix.Count > 0)
            {
                Log.Warning("Missing TR in Requirements-Matrix:");
                result.MissingTrInMatrix.ForEach(id => Log.Warning("  - {Id}", id));
            }

            if (result.MissingTestInMatrix.Count > 0)
            {
                Log.Warning("Missing TEST in Requirements-Matrix:");
                result.MissingTestInMatrix.ForEach(id => Log.Warning("  - {Id}", id));
            }

            var fail = result.HasFrErrors ||
                       (StrictTrAndTestCoverage && (result.HasTrErrors || result.HasTestErrors));

            if (fail)
                throw new InvalidOperationException("Traceability validation failed.");

            if (result.HasTrErrors || result.HasTestErrors)
                Log.Information("Traceability validation passed with TR/TEST coverage warnings.");
            else
                Log.Information("Traceability validation passed.");
        });
}
