using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC-004 (PLAN-SESSIONTRIAGEFIX-001 E6, triage-report-3546932eb5e94bbc8602fcb586cf5d5b, BUG-BASELINE-017; operator
/// decision 2026-10-15 "Launch + evaluator schema"): the AI lane's aiUnit strategy names a command the running operating system can start
/// (Linux: an executable PowerShell script with a pwsh shebang; Windows: a .cmd launcher), and that command asks grok for the plugin
/// evaluator's own JSON (valid, missingFields and contradictions, nothing else) with aiUnit's prompt intact as one argument, returning grok's
/// structured output, so a receipt the model judges invalid fails the lane. Root cause measured 2026-10-15: the strategy ran
/// aiunit-grok-json.cmd, which Linux cannot start (aiUnit answered spawn_failed for every host), and that shared wrapper forced the
/// review-findings schema, whose answer the evaluator reads as valid. Mock gate: the startable, schema and grok-call assertions are proven
/// against pinned facts, pinned schemas and pinned argument lists. Bindings: the strategy and command in this test's output folder, and the
/// evaluator wrapper run through pwsh with a fake grok first on PATH. Fixture: a temporary folder holding the fake grok; no server, no model.
/// </summary>
[Trait("PluginInt", "Deterministic")]
[Trait("Category", "Integration")]
public sealed class AiEvaluatorStrategyTests
{
    private const string Prompt = "Return only JSON with keys valid (boolean), missingFields (string array), and contradictions (string array).\n\n{\"agent\":\"ClaudeCode\",\"status\":\"completed\",\"note\":\"a & b < c\"}";

    private const string Shebang = "#!/usr/bin/env -S pwsh -NoLogo -NoProfile -NonInteractive -File";

    /// <summary>What a strategy's command looks like on disk, as the startable assertion reads it.</summary>
    /// <param name="Command">The strategy's Command value.</param>
    /// <param name="Exists">Whether the command file is in the output folder.</param>
    /// <param name="Executable">Whether the file carries the owner's execute bit (Linux).</param>
    /// <param name="FirstLine">The file's first line.</param>
    /// <param name="IsWindows">Whether the lane runs on Windows.</param>
    private sealed record CommandFacts(string Command, bool Exists, bool Executable, string FirstLine, bool IsWindows);

    /// <summary>TEST-MCP-PLUGININT-001 AC-004: the running operating system can start the strategy's command.</summary>
    private static void AssertStartable(CommandFacts facts)
    {
        Assert.True(facts.Exists, facts.Command + " is not in the output folder");
        if (facts.IsWindows)
        {
            Assert.EndsWith(".cmd", facts.Command, StringComparison.OrdinalIgnoreCase);
            return;
        }

        Assert.False(facts.Command.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase), "Linux cannot start a Windows command script");
        Assert.True(facts.Executable, facts.Command + " has no execute bit");
        Assert.StartsWith("#!", facts.FirstLine, StringComparison.Ordinal);
        Assert.Contains("pwsh", facts.FirstLine, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-PLUGININT-001 AC-004: the schema requires valid (boolean), missingFields and contradictions (string arrays), nothing else.</summary>
    private static void AssertEvaluatorSchema(string schemaJson)
    {
        var schema = JsonNode.Parse(schemaJson)!.AsObject();
        Assert.Equal("object", (string?)schema["type"]);
        Assert.False((bool?)schema["additionalProperties"] ?? true, "the schema allows properties beyond the evaluator's three");
        string[] fields = ["contradictions", "missingFields", "valid"];
        Assert.Equal(fields, schema["required"]!.AsArray().Select(item => (string?)item).Order(StringComparer.Ordinal).ToArray());
        var properties = schema["properties"]!.AsObject();
        Assert.Equal(fields, properties.Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("boolean", (string?)properties["valid"]!["type"]);
        foreach (var arrayField in new[] { "missingFields", "contradictions" })
        {
            Assert.Equal("array", (string?)properties[arrayField]!["type"]);
            Assert.Equal("string", (string?)properties[arrayField]!["items"]!["type"]);
        }
    }

    /// <summary>TEST-MCP-PLUGININT-001 AC-004: grok was asked for the evaluator schema with the prompt intact as one argument.</summary>
    private static void AssertGrokCall(IReadOnlyList<string> arguments, string prompt)
    {
        Assert.Equal(prompt, arguments[ValueIndex(arguments, "--single")]);
        AssertEvaluatorSchema(arguments[ValueIndex(arguments, "--json-schema")]);
    }

    /// <summary>The index of the value that follows <paramref name="flag"/>; fails when the flag or its value is missing.</summary>
    private static int ValueIndex(IReadOnlyList<string> arguments, string flag)
    {
        var index = arguments.ToList().IndexOf(flag);
        Assert.True(index >= 0 && index + 1 < arguments.Count, flag + " and its value are missing");
        return index + 1;
    }

    /// <summary>A pinned evaluator schema; the switches produce the wrong variants the mock gate rejects.</summary>
    private static string EvaluatorSchema(bool additionalProperties = false, string validType = "boolean", bool includeValid = true)
    {
        var stringArray = new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string" } };
        var properties = new Dictionary<string, object>();
        if (includeValid)
        {
            properties["valid"] = new Dictionary<string, object> { ["type"] = validType };
        }

        properties["missingFields"] = stringArray;
        properties["contradictions"] = stringArray;
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = additionalProperties,
            ["required"] = includeValid ? new[] { "valid", "missingFields", "contradictions" } : new[] { "missingFields", "contradictions" },
            ["properties"] = properties,
        });
    }

    /// <summary>A pinned review-findings schema (aiunit.review.findings.v1), the one the shared Review.Tests wrapper forces.</summary>
    private static string ReviewFindingsSchema() => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new[] { "schemaVersion", "reviewType", "status", "summary", "findings" },
        ["properties"] = new Dictionary<string, object>
        {
            ["schemaVersion"] = new Dictionary<string, object> { ["const"] = "aiunit.review.findings.v1" },
            ["reviewType"] = new Dictionary<string, object> { ["const"] = "project" },
            ["status"] = new Dictionary<string, object> { ["enum"] = new[] { "pass", "fail", "error" } },
            ["summary"] = new Dictionary<string, object> { ["type"] = "string" },
            ["findings"] = new Dictionary<string, object> { ["type"] = "array" },
        },
    });

    /// <summary>
    /// Mock gate for TEST-MCP-PLUGININT-001 AC-004: the startable assertion accepts an executable shebang script on Linux and a .cmd on
    /// Windows, and rejects a .cmd on Linux, a missing file, a script without its execute bit, a script without a shebang and a .ps1 on
    /// Windows; the schema assertion accepts the evaluator schema and rejects the review-findings schema, one without valid, one allowing
    /// other properties and one typing valid as a string; the grok-call assertion accepts the evaluator call and rejects a call without a
    /// schema, a prompt cut at its first line and the review-findings schema. Data: pinned facts, schemas and argument lists only.
    /// </summary>
    [Fact]
    public void EvaluatorStrategyAssertion_AcceptsStartableEvaluatorAndRejectsCmdOnUnixMissingOrReviewSchema()
    {
        AssertStartable(new CommandFacts("aiunit-grok-evaluator.ps1", true, true, Shebang, IsWindows: false));
        AssertStartable(new CommandFacts("aiunit-grok-evaluator.cmd", true, false, "@echo off", IsWindows: true));
        Assert.ThrowsAny<Exception>(() => AssertStartable(new CommandFacts("aiunit-grok-json.cmd", true, false, "@echo off", IsWindows: false)));
        Assert.ThrowsAny<Exception>(() => AssertStartable(new CommandFacts("aiunit-grok-evaluator.ps1", false, true, Shebang, IsWindows: false)));
        Assert.ThrowsAny<Exception>(() => AssertStartable(new CommandFacts("aiunit-grok-evaluator.ps1", true, false, Shebang, IsWindows: false)));
        Assert.ThrowsAny<Exception>(() => AssertStartable(new CommandFacts("aiunit-grok-evaluator.ps1", true, true, "param(", IsWindows: false)));
        Assert.ThrowsAny<Exception>(() => AssertStartable(new CommandFacts("aiunit-grok-evaluator.ps1", true, true, Shebang, IsWindows: true)));

        AssertEvaluatorSchema(EvaluatorSchema());
        Assert.ThrowsAny<Exception>(() => AssertEvaluatorSchema(ReviewFindingsSchema()));
        Assert.ThrowsAny<Exception>(() => AssertEvaluatorSchema(EvaluatorSchema(includeValid: false)));
        Assert.ThrowsAny<Exception>(() => AssertEvaluatorSchema(EvaluatorSchema(additionalProperties: true)));
        Assert.ThrowsAny<Exception>(() => AssertEvaluatorSchema(EvaluatorSchema(validType: "string")));

        AssertGrokCall(["--single", Prompt, "--json-schema", EvaluatorSchema()], Prompt);
        Assert.ThrowsAny<Exception>(() => AssertGrokCall(["--single", Prompt], Prompt));
        Assert.ThrowsAny<Exception>(() => AssertGrokCall(["--single", Prompt.Split('\n')[0], "--json-schema", EvaluatorSchema()], Prompt));
        Assert.ThrowsAny<Exception>(() => AssertGrokCall(["--single", Prompt, "--json-schema", ReviewFindingsSchema()], Prompt));
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-004: the appsettings.aiunit.json in this test's output folder names, for its active strategy, a
    /// command that is in the output folder and that the running operating system can start. Data: the output folder as built.
    /// </summary>
    [Fact]
    public void EvaluatorStrategy_OutputFolder_NamesStartableCommand()
    {
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.aiunit.json")))!["AiUnit"]!;
        var active = (string?)settings["ActiveStrategy"];
        Assert.False(string.IsNullOrWhiteSpace(active));
        var command = (string?)settings["Strategies"]![active!]!["Command"];
        Assert.False(string.IsNullOrWhiteSpace(command));
        var path = Path.Combine(AppContext.BaseDirectory, command!);
        var exists = File.Exists(path);
        var executable = false;
        if (exists && !OperatingSystem.IsWindows())
        {
            executable = (File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != 0;
        }

        var firstLine = exists ? File.ReadLines(path).FirstOrDefault() ?? string.Empty : string.Empty;
        AssertStartable(new CommandFacts(command!, exists, executable, firstLine, OperatingSystem.IsWindows()));
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-004: aiunit-grok-evaluator.ps1 from this test's output folder, run through pwsh with a fake grok
    /// first on PATH, asks grok for the evaluator schema with the prompt intact and returns grok's structured output, which the evaluator
    /// reads as invalid when the model says so. The fake grok.ps1 records its arguments, answers like the Grok Build CLI and ends with exit 0,
    /// as the executable it stands in for sets its exit code. Data: the pinned multi-line prompt with quotes, an ampersand and a less-than.
    /// </summary>
    [Fact]
    public async Task EvaluatorWrapper_FakeGrok_SendsEvaluatorSchemaAndReturnsStructuredOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "aiunit-evaluator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var log = Path.Combine(root, "grok-args.json");
            File.WriteAllText(
                Path.Combine(root, "grok.ps1"),
                string.Join(
                    '\n',
                    "param([Parameter(ValueFromRemainingArguments = $true)][string[]]$GrokArgs)",
                    "ConvertTo-Json -InputObject @($GrokArgs) -Compress | Set-Content -LiteralPath $env:FAKE_GROK_LOG -Encoding utf8NoBOM",
                    "ConvertTo-Json -Compress -Depth 5 -InputObject ([ordered]@{ structuredOutput = [ordered]@{ valid = $false; missingFields = @('status'); contradictions = @() } })",
                    "exit 0",
                    string.Empty));
            var result = await new RealPluginProcessRunner().RunAsync(
                new PluginProcessLaunchRequest
                {
                    Executable = "pwsh",
                    Arguments = ["-NoProfile", "-NonInteractive", "-File", Path.Combine(AppContext.BaseDirectory, "aiunit-grok-evaluator.ps1"), Prompt],
                    StandardInput = string.Empty,
                    Environment = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["PATH"] = root + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
                        ["FAKE_GROK_LOG"] = log,
                    },
                    WorkingDirectory = root,
                    Timeout = TimeSpan.FromSeconds(60),
                },
                TestContext.Current.CancellationToken);

            Assert.True(result.ExitCode == 0, "the evaluator wrapper failed: " + result.StandardError);
            var evaluation = AiStrategyFixture.ParseAiUnitPayload(result.StandardOutput.Trim());
            Assert.False(evaluation.Valid);
            Assert.Equal(new[] { "status" }, evaluation.MissingFields);
            AssertGrokCall(JsonSerializer.Deserialize<string[]>(File.ReadAllText(log))!, Prompt);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
