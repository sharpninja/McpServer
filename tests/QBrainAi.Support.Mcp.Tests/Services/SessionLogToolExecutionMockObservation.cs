// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-AUDITACTION-001: one actual independent model execution.</summary>
/// <param name="Name">Scenario or injected fault identifier.</param>
/// <param name="ExpectedRejection">Whether the same scenario must reject the dependency.</param>
/// <param name="ExceptionType">Observed scenario exception type.</param>
/// <param name="Message">Observed scenario exception message.</param>
/// <param name="Effects">Recorded storage and validation interactions in occurrence order.</param>
public sealed record SessionLogToolExecutionMockObservation(string Name, bool ExpectedRejection, string? ExceptionType, string? Message, IReadOnlyList<string> Effects);
