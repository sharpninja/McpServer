using Nuke.Common;
using Serilog;

partial class Build
{
    [Parameter("Fail on missing TR/TEST coverage (default false)")]
    readonly bool StrictTrAndTestCoverage = false;

    [Parameter("Optional path to workspace SQLite DB for UseCaseFrLinks Realizes coverage (FR-MCP-USECASE-010)")]
    readonly string UseCaseSqlitePath = string.Empty;

    [Parameter("Fail when UseCaseFrLinks Realizes coverage findings are non-empty (default false)")]
    readonly bool StrictUseCaseFrCoverage = false;

    /// <summary>Validate requirements traceability across FR/TR/TEST documents and UseCaseFrLinks.</summary>
    public Target ValidateTraceability => _ => _
        .Executes(() =>
        {
            var docsPath = RootDirectory / "docs" / "Project";
            var functionalLines = File.ReadAllLines(docsPath / "Functional-Requirements.md");
            var technicalLines = File.ReadAllLines(docsPath / "Technical-Requirements.md");
            var testingLines = File.ReadAllLines(docsPath / "Testing-Requirements.md");
            var mappingLines = File.ReadAllLines(docsPath / "TR-per-FR-Mapping.md");
            var matrixLines = File.ReadAllLines(docsPath / "Requirements-Matrix.md");

            // FR-MCP-USECASE-010: load UseCaseFrLinks via shared Realizes algorithm (same wording as UseCaseTraceabilityGate).
            var sqlitePath = string.IsNullOrWhiteSpace(UseCaseSqlitePath)
                ? UseCaseFrTraceabilityLoader.ResolveDefaultSqlitePath(RootDirectory)
                : UseCaseSqlitePath;
            var useCaseFindings = UseCaseFrTraceabilityLoader.LoadFindingsFromSqlite(sqlitePath);
            if (!string.IsNullOrWhiteSpace(sqlitePath))
                Log.Information("UseCaseFrLinks coverage source: {Path} (findings={Count})", sqlitePath, useCaseFindings.Count);
            else
                Log.Information("UseCaseFrLinks coverage source: none (no SQLite path resolved)");

            var result = TraceabilityValidator.Validate(
                functionalLines, technicalLines, testingLines, mappingLines, matrixLines, useCaseFindings);

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

            if (result.UseCaseFrFindings.Count > 0)
            {
                Log.Warning("UseCaseFrLinks Realizes coverage findings:");
                result.UseCaseFrFindings.ForEach(f => Log.Warning("  - {Finding}", f));
            }

            var fail = result.HasFrErrors ||
                       (StrictTrAndTestCoverage && (result.HasTrErrors || result.HasTestErrors)) ||
                       (StrictUseCaseFrCoverage && result.HasUseCaseFrErrors);

            if (fail)
                throw new InvalidOperationException("Traceability validation failed.");

            if (result.HasTrErrors || result.HasTestErrors || result.HasUseCaseFrErrors)
                Log.Information("Traceability validation passed with TR/TEST/UseCaseFr coverage warnings.");
            else
                Log.Information("Traceability validation passed.");
        });
}
