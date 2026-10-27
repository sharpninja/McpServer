// SPDX-License-Identifier: Apache-2.0
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>TR-QB-VERIFY-001: validates and authenticates workspace verification policy snapshots.</summary>
public static class WorkspaceVerificationPolicyCodec
{
    /// <summary>Exact policy-envelope and signature format.</summary>
    public const string Format = "workspace-verification-policy-v1";
    private static readonly JsonSerializerOptions s_json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = McpClientJsonContext.Default,
    };

    private static JsonTypeInfo<WorkspaceVerificationPolicyDocument> DocumentTypeInfo => (JsonTypeInfo<WorkspaceVerificationPolicyDocument>)s_json.GetTypeInfo(typeof(WorkspaceVerificationPolicyDocument));
    /// <summary>Validates and returns a detached, canonical policy document.</summary>
    public static WorkspaceVerificationPolicyDocument Normalize(WorkspaceVerificationPolicyDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Text(document.WorkspacePath);
        if (!Path.IsPathFullyQualified(document.WorkspacePath)
            && !Regex.IsMatch(document.WorkspacePath, @"^[A-Za-z]:[\\/]")
            && !document.WorkspacePath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("Verification policy requires an absolute workspace path.");
        var policy = document.Policy ?? throw new ArgumentException("Verification policy is missing.");
        if (policy.SchemaVersion != 1 || policy.ContinuationLimit is < 1 or > 3
            || policy.Checks is null || policy.PhaseGates is null
            || policy.Checks.Length > 2048 || policy.PhaseGates.Length > 2048)
            throw new ArgumentException("Verification policy version, bounds or collections are invalid.");
        var checks = policy.Checks.Select(check =>
        {
            if (check is null) throw new ArgumentException("Verification check is missing.");
            Text(check.Id);
            if (check.Argv is null || check.Argv.Length is < 1 or > 256
                || string.IsNullOrWhiteSpace(check.Argv[0]) || check.Argv.Any(arg => arg is null || arg.Contains('\0')))
                throw new ArgumentException("Verification argv is invalid.");
            RelativePath(check.Cwd);
            var triggers = Set(check.Triggers, false);
            if (triggers.Any(trigger => trigger is not ("code" or "doc"))
                || check.Required is null || check.Scope is not ("turn" or "phaseGate")
                || check.SuiteKind is not ("build" or "typecheck" or "unit" or "integration" or "e2e" or "packaging" or "other"))
                throw new ArgumentException("Verification check classification is invalid.");
            var globs = Set(check.PathGlobs, true);
            foreach (var glob in globs) RelativePath(glob);
            return check with { Argv = [.. check.Argv], Triggers = triggers, PathGlobs = globs };
        }).OrderBy(check => check.Id, StringComparer.Ordinal).ToArray();
        if (checks.Select(check => check.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != checks.Length)
            throw new ArgumentException("Verification check identifiers must be unique.");
        var gates = policy.PhaseGates.Select(gate =>
        {
            if (gate is null) throw new ArgumentException("Phase selector is missing.");
            Text(gate.TodoId);
            var prefixes = Set(gate.TaskPrefixes, true);
            if (!gate.OnDone && prefixes.Length == 0)
                throw new ArgumentException("Phase selector has no governed mutation.");
            return gate with { TaskPrefixes = prefixes };
        }).OrderBy(gate => gate.TodoId, StringComparer.Ordinal).ToArray();
        if (gates.Select(gate => gate.TodoId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != gates.Length)
            throw new ArgumentException("Phase selector TODO identifiers must be unique.");
        return document with
        {
            WorkspacePath = WorkspaceIdentityPath.NormalizePath(document.WorkspacePath),
            Policy = policy with { Checks = checks, PhaseGates = gates },
        };
    }

    /// <summary>Creates a challenge-bound attestation using the current workspace marker key.</summary>
    public static WorkspaceVerificationPolicyEnvelope Sign(WorkspaceVerificationPolicyDocument document, string challenge, string key)
    {
        ValidateChallenge(challenge);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var payload = JsonSerializer.SerializeToUtf8Bytes(Normalize(document), DocumentTypeInfo);
        var digest = Convert.ToHexString(SHA256.HashData(payload));
        return new()
        {
            Format = Format,
            PayloadBase64 = Convert.ToBase64String(payload),
            PolicyDigest = digest,
            Challenge = challenge,
            Signature = Convert.ToHexString(Signature(challenge, digest, key)),
        };
    }

    /// <summary>Authenticates the payload, challenge and workspace before returning any policy data.</summary>
    public static WorkspaceVerificationPolicySnapshot Verify(WorkspaceVerificationPolicyEnvelope envelope, string workspacePath, string challenge, string key)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ValidateChallenge(challenge);
        try
        {
            if (envelope.Format != Format || envelope.Challenge != challenge
                || envelope.PayloadBase64 is null || envelope.PayloadBase64.Length > 8 * 1024 * 1024
                || envelope.PolicyDigest is null || envelope.PolicyDigest.Length != 64
                || envelope.Signature is null || envelope.Signature.Length != 64)
                throw new ArgumentException("Verification policy attestation is invalid.");
            var payload = Convert.FromBase64String(envelope.PayloadBase64);
            var digest = SHA256.HashData(payload);
            if (!CryptographicOperations.FixedTimeEquals(digest, Convert.FromHexString(envelope.PolicyDigest))
                || !CryptographicOperations.FixedTimeEquals(Signature(challenge, envelope.PolicyDigest, key), Convert.FromHexString(envelope.Signature)))
                throw new ArgumentException("Verification policy authentication failed.");
            var document = Normalize(JsonSerializer.Deserialize(payload, DocumentTypeInfo)
                ?? throw new ArgumentException("Verification policy document is missing."));
            if (!WorkspaceIdentityPath.Comparer.Equals(workspacePath, document.WorkspacePath)
                || !payload.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(document, DocumentTypeInfo)))
                throw new ArgumentException("Verification policy workspace or canonical payload does not match.");
            return new() { Document = document, PolicyDigest = envelope.PolicyDigest, Attestation = envelope };
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new ArgumentException("Verification policy attestation is malformed.");
        }
    }

    private static byte[] Signature(string challenge, string digest, string key)
        => HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(Format + "\n" + challenge + "\n" + digest + "\n"));

    /// <summary>Rejects malformed or missing request challenges before configuration is read.</summary>
    public static void ValidateChallenge(string challenge)
    {
        if (challenge is null || !Regex.IsMatch(challenge, "^[A-Fa-f0-9]{64}$"))
            throw new ArgumentException("Verification policy challenge must be 32 hexadecimal bytes.");
    }

    private static void Text(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Any(char.IsControl))
            throw new ArgumentException("Verification policy text is invalid.");
    }

    private static string[] Set(string[] values, bool allowEmpty)
    {
        if (values is null || (!allowEmpty && values.Length == 0) || values.Length > 2048)
            throw new ArgumentException("Verification policy list is invalid.");
        foreach (var value in values) Text(value);
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("Verification policy list has duplicates.");
        return values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static void RelativePath(string value)
    {
        Text(value);
        var portable = value.Replace('\\', '/');
        if (portable.StartsWith('/') || portable.Contains(':') || portable.Split('/').Any(part => part == ".."))
            throw new ArgumentException("Verification paths must remain workspace-relative.");
    }
}
