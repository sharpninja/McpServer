// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.McpAgent.PowerShellSessions;

/// <summary>
/// TR-QB-RUNTIME-001/TEST-QB-069: Optional named-object execution capability of
/// the existing persistent hosted PowerShell session manager. Existing string
/// and interactive callers keep the base interface and lifecycle unchanged.
/// </summary>
public interface IHostedPowerShellObjectSessionManager : IHostedPowerShellSessionManager
{
    /// <summary>
    /// Executes trusted script text with named native objects, preserving object
    /// references and payload text without serializing or interpolating arguments.
    /// </summary>
    /// <param name="sessionId">An existing hosted PowerShell session identifier.</param>
    /// <param name="command">Trusted script text whose parameter block receives the values.</param>
    /// <param name="parameters">Named values captured shallowly before waiting for the session gate.</param>
    /// <param name="cancellationToken">Cancellation propagated through the existing execution gate.</param>
    /// <returns>The existing formatted command result.</returns>
    Task<PowerShellSessionCommandResult> ExecuteCommandAsync(
        string sessionId,
        string command,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}
