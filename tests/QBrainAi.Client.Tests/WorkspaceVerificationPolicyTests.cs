// SPDX-License-Identifier: Apache-2.0
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QBrainAi.Client.Models;

namespace QBrainAi.Client.Tests;

/// <summary>TEST-QB-011/012: policy authentication, exact command preservation and fail-closed schema handling.</summary>
public sealed class WorkspaceVerificationPolicyTests
{
    private const string Key = "fixture-only-policy-key";
    private static readonly string Challenge = new('A', 64);

    private static WorkspaceVerificationPolicyDocument Document() => new()
    {
        WorkspacePath = "/test/policy",
        Policy = new()
        {
            SchemaVersion = 1,
            ContinuationLimit = 3,
            Checks = [new() { Id = "sdk-build", Argv = ["bun", "run", "build:sdk", ""], Cwd = ".", Triggers = ["code"], Required = true, SuiteKind = "build", Scope = "turn", PathGlobs = ["sdk/**"] }],
            PhaseGates = [new() { TodoId = "QB-AGENT-001", TaskPrefixes = ["P1:", "Final:"], OnDone = true }],
        },
    };

    /// <summary>Valid policy preserves literal arguments, selectors and a verifiable signed digest.</summary>
    [Fact]
    public void SignedPolicy_RoundTripsExactCommandsAndSelectors()
    {
        var document = Document();
        var envelope = WorkspaceVerificationPolicyCodec.Sign(document, Challenge, Key);
        var snapshot = WorkspaceVerificationPolicyCodec.Verify(envelope, document.WorkspacePath, Challenge, Key);
        Assert.Equal(document.Policy.Checks[0].Argv, snapshot.Document.Policy.Checks[0].Argv);
        Assert.True(snapshot.Document.Policy.PhaseGates[0].OnDone);
        Assert.Equal(["Final:", "P1:"], snapshot.Document.Policy.PhaseGates[0].TaskPrefixes);
        Assert.Equal(64, snapshot.PolicyDigest.Length);
        Assert.DoesNotContain(Key, JsonSerializer.Serialize(snapshot));
    }

    /// <summary>Changing context, challenge, signature or payload cannot authenticate.</summary>
    [Theory]
    [InlineData("workspace")]
    [InlineData("challenge")]
    [InlineData("key")]
    [InlineData("payload")]
    [InlineData("digest")]
    [InlineData("signature")]
    [InlineData("format")]
    public void TamperingOrReplay_IsRejected(string field)
    {
        var envelope = WorkspaceVerificationPolicyCodec.Sign(Document(), Challenge, Key);
        envelope = field switch
        {
            "payload" => envelope with { PayloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")) },
            "digest" => envelope with { PolicyDigest = new string('0', 64) },
            "signature" => envelope with { Signature = new string('0', 64) },
            "format" => envelope with { Format = "future-format" },
            _ => envelope,
        };
        Assert.ThrowsAny<ArgumentException>(() => WorkspaceVerificationPolicyCodec.Verify(envelope,
            field == "workspace" ? "/test/other" : "/test/policy",
            field == "challenge" ? new string('B', 64) : Challenge,
            field == "key" ? "other-fixture-key" : Key));
    }

    /// <summary>Equivalent policy ordering has one digest while argv ordering remains significant.</summary>
    [Fact]
    public void Digest_IsDeterministicAndChallengeIndependent()
    {
        var a = Document();
        var check = a.Policy.Checks[0];
        var b = a with { Policy = a.Policy with { PhaseGates = [a.Policy.PhaseGates[0] with { TaskPrefixes = ["Final:", "P1:"] }] } };
        var first = WorkspaceVerificationPolicyCodec.Sign(a, Challenge, Key);
        var second = WorkspaceVerificationPolicyCodec.Sign(b, new string('B', 64), Key);
        Assert.Equal(first.PolicyDigest, second.PolicyDigest);
        Assert.NotEqual(first.Signature, second.Signature);
        var reordered = a with { Policy = a.Policy with { Checks = [check with { Argv = ["bun", "build:sdk", "run", ""] }] } };
        Assert.NotEqual(first.PolicyDigest, WorkspaceVerificationPolicyCodec.Sign(reordered, Challenge, Key).PolicyDigest);
    }

    /// <summary>Malformed schema, ambiguous identities and escaping paths cannot be signed as trusted configuration.</summary>
    [Theory]
    [InlineData("version")]
    [InlineData("limit")]
    [InlineData("duplicate")]
    [InlineData("argv")]
    [InlineData("cwd")]
    [InlineData("windows-cwd")]
    [InlineData("trigger")]
    [InlineData("scope")]
    [InlineData("suite")]
    [InlineData("required")]
    [InlineData("glob")]
    [InlineData("selector")]
    [InlineData("null-checks")]
    public void InvalidPolicy_IsRejectedBeforeSigning(string invalid)
    {
        var d = Document();
        var p = d.Policy;
        var c = p.Checks[0];
        p = invalid switch
        {
            "version" => p with { SchemaVersion = 2 },
            "limit" => p with { ContinuationLimit = 4 },
            "duplicate" => p with { Checks = [c, c] },
            "argv" => p with { Checks = [c with { Argv = [] }] },
            "cwd" => p with { Checks = [c with { Cwd = "../outside" }] },
            "windows-cwd" => p with { Checks = [c with { Cwd = "C:\\outside" }] },
            "trigger" => p with { Checks = [c with { Triggers = ["arbitrary"] }] },
            "scope" => p with { Checks = [c with { Scope = "anything" }] },
            "suite" => p with { Checks = [c with { SuiteKind = "anything" }] },
            "required" => p with { Checks = [c with { Required = null }] },
            "glob" => p with { Checks = [c with { PathGlobs = ["../private/**"] }] },
            "selector" => p with { PhaseGates = [new() { TodoId = "QB-AGENT-001" }] },
            "null-checks" => p with { Checks = null! },
            _ => p,
        };
        Assert.ThrowsAny<ArgumentException>(() => WorkspaceVerificationPolicyCodec.Sign(d with { Policy = p }, Challenge, Key));
    }

    /// <summary>Rotation changes authentication without changing the configuration digest.</summary>
    [Fact]
    public void KeyRotation_RequiresCurrentKey()
    {
        var old = WorkspaceVerificationPolicyCodec.Sign(Document(), Challenge, Key);
        var current = WorkspaceVerificationPolicyCodec.Sign(Document(), Challenge, "rotated-fixture-key");
        Assert.Equal(old.PolicyDigest, current.PolicyDigest);
        Assert.ThrowsAny<ArgumentException>(() => WorkspaceVerificationPolicyCodec.Verify(old, "/test/policy", Challenge, "rotated-fixture-key"));
        Assert.NotNull(WorkspaceVerificationPolicyCodec.Verify(current, "/test/policy", Challenge, "rotated-fixture-key"));
    }
}
