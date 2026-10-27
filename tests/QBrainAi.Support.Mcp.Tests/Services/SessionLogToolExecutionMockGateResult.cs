// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-AUDITACTION-001: complete mock case/control denominator and interaction evidence.</summary>
/// <param name="Cases">Original checkpoint scenario invocations.</param>
/// <param name="NegativeControls">Scripted wrong dependency behavior rejected through those scenarios.</param>
/// <param name="Observations">Actual effects and any expected rejection.</param>
public sealed record SessionLogToolExecutionMockGateResult(int Cases, int NegativeControls, IReadOnlyList<SessionLogToolExecutionMockObservation> Observations);

