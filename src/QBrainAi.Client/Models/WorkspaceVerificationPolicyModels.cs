// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Client.Models;

/// <summary>TR-QB-VERIFY-001: administrator-configured verification obligations for one workspace.</summary>
public sealed record WorkspaceVerificationPolicy
{
    /// <summary>Supported policy schema; version one is currently required.</summary>
    public int SchemaVersion { get; init; }
    /// <summary>Maximum consecutive unchanged completion attempts, from one through three.</summary>
    public int ContinuationLimit { get; init; }
    /// <summary>Commands selected from trusted source ownership and mutation scope.</summary>
    public VerificationPolicyCheck[] Checks { get; init; } = [];
    /// <summary>TODO mutations that require a phase-closing review.</summary>
    public VerificationPhaseGate[] PhaseGates { get; init; } = [];
}

/// <summary>TR-QB-VERIFY-001: one verification command, without shell interpolation.</summary>
public sealed record VerificationPolicyCheck
{
    /// <summary>Stable check identifier, unique within the policy.</summary>
    public string Id { get; init; } = "";
    /// <summary>Executable and exact arguments; argument order is significant.</summary>
    public string[] Argv { get; init; } = [];
    /// <summary>Workspace-relative working directory; dot means the workspace root.</summary>
    public string Cwd { get; init; } = "";
    /// <summary>Mutation kinds: code and/or doc.</summary>
    public string[] Triggers { get; init; } = [];
    /// <summary>Explicitly configured mandatory/optional status.</summary>
    public bool? Required { get; init; }
    /// <summary>Suite classification: build, typecheck, unit, integration, e2e, packaging or other.</summary>
    public string SuiteKind { get; init; } = "";
    /// <summary>Completion scope: turn or phaseGate.</summary>
    public string Scope { get; init; } = "";
    /// <summary>Optional workspace-relative source ownership globs; empty means all paths.</summary>
    public string[] PathGlobs { get; init; } = [];
}

/// <summary>TR-QB-VERIFY-001: review selectors for one governed TODO.</summary>
public sealed record VerificationPhaseGate
{
    /// <summary>Exact TODO identity.</summary>
    public string TodoId { get; init; } = "";
    /// <summary>Task-name prefixes whose completion requires review.</summary>
    public string[] TaskPrefixes { get; init; } = [];
    /// <summary>Whether setting the TODO's done flag requires review.</summary>
    public bool OnDone { get; init; }
}

/// <summary>TR-QB-VERIFY-001: signed payload data, scoped to an absolute workspace.</summary>
public sealed record WorkspaceVerificationPolicyDocument
{
    /// <summary>Canonical workspace path authenticated by the signature.</summary>
    public string WorkspacePath { get; init; } = "";
    /// <summary>The administrator's validated policy.</summary>
    public WorkspaceVerificationPolicy Policy { get; init; } = new();
}

/// <summary>TR-QB-VERIFY-001: a challenge-bound attestation; contains no signing secret.</summary>
public sealed record WorkspaceVerificationPolicyEnvelope
{
    /// <summary>Wire/signature format identifier.</summary>
    public string Format { get; init; } = "";
    /// <summary>Base64-encoded UTF-8 JSON of the complete signed document.</summary>
    public string PayloadBase64 { get; init; } = "";
    /// <summary>SHA256 digest of the decoded payload bytes.</summary>
    public string PolicyDigest { get; init; } = "";
    /// <summary>Fresh 32-byte hexadecimal request challenge.</summary>
    public string Challenge { get; init; } = "";
    /// <summary>HMAC-SHA256 over format, challenge and digest, separated and terminated by LF.</summary>
    public string Signature { get; init; } = "";
}

/// <summary>TR-QB-VERIFY-001: verified typed policy with its retained attestation.</summary>
public sealed record WorkspaceVerificationPolicySnapshot
{
    /// <summary>Authenticated policy document; never reconstructed from model arguments.</summary>
    public WorkspaceVerificationPolicyDocument Document { get; init; } = new();
    /// <summary>Digest used to bind verification evidence and host identity.</summary>
    public string PolicyDigest { get; init; } = "";
    /// <summary>Original attestation for downstream governed-host verification.</summary>
    public WorkspaceVerificationPolicyEnvelope Attestation { get; init; } = new();
}
