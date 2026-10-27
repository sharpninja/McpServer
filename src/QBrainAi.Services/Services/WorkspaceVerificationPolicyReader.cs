// SPDX-License-Identifier: Apache-2.0
using Microsoft.Extensions.Configuration;
using QBrainAi.Client;
using QBrainAi.Client.Models;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>TR-QB-VERIFY-001: reads administrator-owned configuration for the authenticated workspace.</summary>
public static class WorkspaceVerificationPolicyReader
{
    /// <summary>
    /// Reads QBrainAi:VerificationPolicies (legacy Mcp fallback), validates an unambiguous
    /// workspace match, and signs it with the current marker token. Never executes commands.
    /// </summary>
    public static WorkspaceVerificationPolicyEnvelope Read(
        IConfiguration configuration, WorkspaceContext workspace, WorkspaceTokenService tokens, string challenge)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(tokens);
        WorkspaceVerificationPolicyCodec.ValidateChallenge(challenge);
        if (!workspace.IsResolved || workspace.IsDefaultKey)
            throw new UnauthorizedAccessException("A full authenticated workspace context is required.");
        try
        {
            var section = configuration.GetSection("QBrainAi:VerificationPolicies");
            if (!section.Exists()) section = configuration.GetSection("Mcp:VerificationPolicies");
            var registrations = section.Get<WorkspaceVerificationPolicyDocument[]>(
                options => options.ErrorOnUnknownConfiguration = true) ?? [];
            var matches = registrations.Where(document => document is not null
                && !string.IsNullOrWhiteSpace(document.WorkspacePath)
                && WorkspaceIdentityPath.Comparer.Equals(document.WorkspacePath, workspace.WorkspacePath)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Workspace verification policy is absent or ambiguous.");
            var token = tokens.GetToken(workspace.WorkspacePath!);
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Workspace signing token is unavailable.");
            return WorkspaceVerificationPolicyCodec.Sign(matches[0] with { WorkspacePath = workspace.WorkspacePath! }, challenge, token);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            // Configuration values may include sensitive arguments. Do not return binder details.
            throw new InvalidOperationException("Workspace verification policy is unavailable or invalid.");
        }
    }
}
