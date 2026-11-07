namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006: the two reference times of the age-boundary contract, defined once in a base class that AgeBoundaryContractTests derives from, so the unit classes that name AgeBoundaryContractTests.References keep doing so, and compiled into both QBrainAi.Support.Mcp.Tests and QBrainAi.Support.Mcp.EnvironmentIntegrationTests, where the linked WorkspaceHygieneG5OverlayTestsBase seeds its clock from them.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from AgeBoundaryContractTests (References, with its documentation) when WorkspaceHygieneG5OverlayTests split into its unit half and its integration half (slice S8, hostile validation finding R14-F4): the clock both halves share is seeded from References, and the integration project cannot link a unit test class, so the data moved into this base class, which both projects compile (DRY); its accessibility stays internal.</remarks>
public abstract class AgeBoundaryContractTestsBase
{
    /// <summary>The reference times every decision is asserted at: one far before and one far after the real clock.</summary>
    internal static readonly DateTimeOffset[] References =
    [
        new(2001, 2, 3, 4, 5, 6, TimeSpan.Zero),
        new(2041, 2, 3, 4, 5, 6, TimeSpan.Zero),
    ];
}
