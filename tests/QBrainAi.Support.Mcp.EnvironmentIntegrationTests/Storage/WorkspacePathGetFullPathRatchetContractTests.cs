using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>BDP v4 step 3: the ratchet over the repository <c>src/</c> tree.</summary>
/// <remarks>
/// <para>Integration: this provider enumerates and reads current checkout source; the shared assertions and scripted provider remain unit tests (TR-MCP-USECASE-018 AC-006).</para>
/// <para>2026-11-07, TEST-MCP-FED-PATH-001; TR-MCP-FED-PATH-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its inherited Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/WorkspacePathGetFullPathRatchetContractTests.cs), whose scripted fixture stays there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the contract it derives from is Storage/WorkspacePathGetFullPathRatchetContract.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); it still scans the repository src/ tree; only the namespace changed.</para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class WorkspacePathGetFullPathRatchetRealTests : WorkspacePathGetFullPathRatchetContractTests
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, int> Counts()
    {
        var root = QBrainAi.Support.Mcp.Tests.Infrastructure.RepositoryEvidenceTestSupport.ResolveRepositoryRoot();
        var excluded = new Regex(@"[\\/](obj|bin|Migrations)[\\/]", RegexOptions.CultureInvariant);
        return Count(Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !excluded.IsMatch(file))
            .Select(file => KeyValuePair.Create(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file))));
    }
}
