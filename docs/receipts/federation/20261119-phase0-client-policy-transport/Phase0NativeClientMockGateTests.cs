using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Immutable nonsecret launch inputs configured before observing the native client.</summary>
/// <param name="Executable">Inventory-verified absolute PowerShell executable.</param>
/// <param name="ScriptPath">Absolute helper path inside the disposable run root.</param>
/// <param name="Origin">Configured canonical HTTPS gateway origin.</param><param name="Pipe">Nonsecret broker reference.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Corrects example-path coupling so real adapters assert actual configured run-specific inputs.</remarks>
internal sealed record Phase0NativeLaunchSettings(string Executable, string ScriptPath, string Origin, string Pipe);

/// <summary>Actual helper launch and safe public outcomes, excluding its private credential response.</summary>
/// <param name="Executable">Observed attempted executable.</param><param name="Arguments">Actual ordered argument vector.</param>
/// <param name="Shell">Whether launch passed through a shell.</param><param name="Input">Observed origin-only stdin.</param>
/// <param name="Environment">Observed child environment, privately scanned before retention.</param>
/// <param name="Attempts">Launch attempts.</param><param name="Starts">Successfully created helper processes.</param>
/// <param name="ExitCode">Client exit code.</param><param name="Code">Safe local outcome.</param>
/// <param name="Credential">Privately byte-compared credential classification.</param><param name="Header">Credential channel.</param>
/// <param name="BearerSends">Actual downstream bearer requests.</param><param name="Captures">Complete safe diagnostic captures.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233;
/// FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials; TEST-MCP-232. Observation labels are not production DTOs.</remarks>
internal sealed record Phase0HelperRun(string Executable, IReadOnlyList<string> Arguments, bool Shell, string Input,
    IReadOnlyDictionary<string, string> Environment, int Attempts, int Starts, int ExitCode, string Code,
    string Credential, string Header, int BearerSends, IReadOnlyDictionary<string, string?> Captures);

/// <summary>Observed native trust policy and chronological TLS/bearer boundaries.</summary>
/// <param name="Channel">Actual transport channel.</param><param name="Ca">Privately compared CA fixture identity.</param>
/// <param name="ServerName">Actual verified hostname.</param><param name="Pins">Installed allowed-pin fixture identities.</param>
/// <param name="SkipVerification">Whether ordinary TLS verification was disabled.</param>
/// <param name="Events">Actual ordered policy, handshake and send events.</param><param name="Code">Safe local outcome.</param>
/// <param name="BearerSends">Observed bearer send count.</param>
/// <remarks>2026-11-19: FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// CA/pin labels require in-memory comparison with real fixture bytes, never configuration echo.</remarks>
internal sealed record Phase0NativeTlsRun(string Channel, string Ca, string ServerName, IReadOnlyList<string> Pins,
    bool SkipVerification, IReadOnlyList<string> Events, string Code, int BearerSends);

/// <summary>Test-owned native-client boundary, bound to real child processes and TLS only after group readiness.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal interface IPhase0NativeClientProbe
{
    /// <summary>Attempts the selected host/helper fixture with an invocation-private canary.</summary>
    /// <param name="host">Windows or Linux fixture.</param><param name="scenario">Explicit protocol or policy fault.</param>
    /// <param name="canary">In-memory canary, never a test argument or retained credential value.</param>
    /// <param name="settings">Immutable nonsecret launch inputs supplied before execution.</param>
    /// <returns>Actual launch facts and safe public captures.</returns>
    Phase0HelperRun? RunHelper(string host, string scenario, string canary, Phase0NativeLaunchSettings settings);
    /// <summary>Attempts one channel with the specified valid or defective peer fixture.</summary>
    /// <param name="channel">Unary gRPC, stream gRPC or object HTTP.</param><param name="peer">Named peer defect or valid.</param>
    /// <returns>Actual installed trust policy and ordered transmission observations.</returns>
    Phase0NativeTlsRun? RunTls(string channel, string peer);
}

/// <summary>Independent assertions for the selected narrow-client launch, diagnostic and TLS design.</summary>
/// <remarks>2026-11-19: FR-MCP-185 ac-UnknownCertificatePinRejected and ac-RawRemoteCannotBypassPublicationPolicy;
/// FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials; TR-MCP-FEDERATION-012/011; TEST-MCP-233/232.
/// No process, network or product code runs in this mock gate.</remarks>
internal static class Phase0NativeClientContract
{
    /// <summary>Checks exact direct argv/stdin, safe failures and no credential transmission after helper rejection.</summary>
    /// <param name="probe">Scripted or later real native observer.</param><param name="host">Fixture host.</param>
    /// <param name="scenario">Explicit independent input case.</param><param name="canary">Private canary.</param>
    /// <param name="settings">Configured run-specific inputs, or deterministic unit-fixture defaults.</param>
    internal static void AssertHelper(IPhase0NativeClientProbe probe, string host, string scenario, string canary,
        Phase0NativeLaunchSettings? settings = null)
    {
        settings ??= new(host == "Windows" ? "C:/Program Files/PowerShell/7/pwsh.exe" : "/usr/bin/pwsh",
            host == "Windows" ? "C:/Phase0 Run/helper & literal.ps1" : "/tmp/Phase0 Run/helper & literal.ps1",
            "https://gateway.lab:7443", "fixture-pipe");
        var run = probe.RunHelper(host, scenario, canary, settings);
        Assert.NotNull(run);
        var policy = scenario.StartsWith("Policy:", StringComparison.Ordinal);
        var success = scenario is "Success" or "SuccessNoExpiry";
        var code = policy ? "federation_transport_policy_invalid" : scenario switch
        {
            "Success" or "SuccessNoExpiry" => "",
            "StartFailure" or "ExitFailure" or "BrokerUnavailable" => "credential_helper_failed",
            _ => "credential_helper_response_invalid"
        };
        // Scan before any diagnostic can render an attacker-controlled observation.
        Assert.False(run.Executable.Contains(canary, StringComparison.Ordinal), "Credential in executable.");
        foreach (var arg in run.Arguments)
            Assert.False(arg.Contains(canary, StringComparison.Ordinal), "Credential in argv.");
        Assert.False(run.Input.Contains(canary, StringComparison.Ordinal), "Credential in helper request.");
        foreach (var entry in run.Environment)
        {
            Assert.False(entry.Key.Contains(canary, StringComparison.Ordinal), "Credential in environment key.");
            Assert.False(entry.Value.Contains(canary, StringComparison.Ordinal), "Credential in environment value.");
        }
        foreach (var capture in run.Captures)
        {
            Assert.False(capture.Key.Contains(canary, StringComparison.Ordinal), "Credential in capture key.");
            Assert.NotNull(capture.Value);
            Assert.False(capture.Value.Contains(canary, StringComparison.Ordinal), "Credential in diagnostic capture.");
        }
        Assert.False(run.Shell);
        Assert.Equal(policy ? 0 : 1, run.Attempts);
        Assert.Equal(policy || scenario == "StartFailure" ? 0 : 1, run.Starts);
        Assert.Equal(success ? 0 : 1, run.ExitCode);
        Assert.True(run.Code == code, "Wrong safe helper outcome.");
        Assert.True(run.Credential == (success ? "Reference" : "None"), "Wrong private credential classification.");
        Assert.True(run.Header == (success ? "Proxy-Authorization" : ""), "Wrong credential channel.");
        Assert.Equal(success ? 1 : 0, run.BearerSends);
        if (policy)
        {
            Assert.Empty(run.Executable); Assert.Empty(run.Arguments); Assert.Empty(run.Input); Assert.Empty(run.Environment);
        }
        else
        {
            Assert.Equal(settings.Executable, run.Executable);
            Assert.Equal(new[] { "-NoProfile", "-NonInteractive", "-File", settings.ScriptPath, "get" }, run.Arguments);
            using var input = JsonDocument.Parse(run.Input);
            Assert.Equal(JsonValueKind.Object, input.RootElement.ValueKind);
            var fields = input.RootElement.EnumerateObject().ToArray();
            Assert.Single(fields); Assert.Equal("uri", fields[0].Name);
            Assert.Equal(settings.Origin, fields[0].Value.GetString());
            Assert.True(run.Environment.TryGetValue("QBRAIN_PHASE0_TOKEN_PIPE", out var pipe) && pipe == settings.Pipe && !string.IsNullOrWhiteSpace(pipe),
                "Nonsecret broker reference is missing.");
        }
        foreach (var name in new[] { "client_stdout", "client_stderr", "exception_text", "helper_diagnostics" })
            Assert.True(run.Captures.ContainsKey(name), "Required native diagnostic capture is missing.");
        Assert.True(run.Captures["client_stdout"] == "", "Unexpected public helper stdout.");
        Assert.True(run.Captures["client_stderr"] == (success ? "" : code + "\n"), "Unexpected public helper stderr.");
        Assert.True(run.Captures["exception_text"] == "", "Raw helper exception was retained.");
        Assert.True(run.Captures["helper_diagnostics"] == "", "Raw helper diagnostic was retained.");
    }

    /// <summary>Requires the same complete policy and handshake-before-bearer order on every native channel.</summary>
    /// <param name="probe">Independent native observation boundary.</param><param name="channel">Actual channel fixture.</param>
    /// <param name="peer">Valid, CA, SAN, expiration or pin fixture.</param>
    internal static void AssertTls(IPhase0NativeClientProbe probe, string channel, string peer)
    {
        var run = probe.RunTls(channel, peer); Assert.NotNull(run);
        Assert.Equal(channel, run.Channel); Assert.Equal("fixture-ca-A", run.Ca);
        Assert.Equal("gateway.lab", run.ServerName); Assert.False(run.SkipVerification);
        Assert.Equal(new[] { "fixture-pin-A", "fixture-pin-B" }, run.Pins.Order(StringComparer.Ordinal));
        var valid = peer == "Valid";
        Assert.Equal(valid ? new[] { "policy_loaded", "tls_verified", "bearer_sent" }
            : new[] { "policy_loaded", "tls_rejected" }, run.Events);
        Assert.Equal(valid ? "" : "tls_peer_untrusted", run.Code);
        Assert.Equal(valid ? 1 : 0, run.BearerSends);
    }
}

/// <summary>Discriminating scripts validate the native-client assertions without implementing the client.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233;
/// supporting FR-MCP-184/TEST-MCP-232. Fixture inputs and expected outcomes are literal independent transcripts.</remarks>
public sealed class Phase0NativeClientMockGateTests
{
    /// <summary>All helper protocol and policy cases on both fixture hosts.</summary>
    public static IEnumerable<object[]> Helpers => new[] { "Windows", "Linux" }
        .SelectMany(host => HelperFixtures.Keys.Select(scenario => new object[] { host, scenario }));
    /// <summary>Individually wrong helper observations, each required to reach its intended injection.</summary>
    public static IEnumerable<object[]> HelperFaults => Helpers.SelectMany(row => HelperFields.Select(faultName =>
        new object[] { row[0], row[1], faultName }));
    /// <summary>All three native channels against valid and four defective peer fixtures.</summary>
    public static IEnumerable<object[]> TlsCases => new[] { "grpc-unary", "grpc-stream", "object-http" }
        .SelectMany(channel => new[] { "Valid", "UntrustedCa", "WrongSan", "Expired", "UnapprovedPin" }
        .Select(peer => new object[] { channel, peer }));
    /// <summary>Each TLS observation field is separately corrupted.</summary>
    public static IEnumerable<object[]> TlsFaults => TlsCases.SelectMany(row => TlsFields.Select(faultName =>
        new object[] { row[0], row[1], faultName }));

    /// <summary>Correct helper transcripts, including literal metacharacters, pass every assertion.</summary>
    /// <param name="host">Fixture host.</param><param name="scenario">Independent input case.</param>
    [Theory, MemberData(nameof(Helpers))]
    public void CorrectHelperPasses(string host, string scenario)
    {
        var probe = new ScriptedProbe();
        Phase0NativeClientContract.AssertHelper(probe, host, scenario, Guid.NewGuid().ToString("N"));
        Assert.Equal(new[] { $"helper:{host}:{scenario}" }, probe.Calls);
    }
    /// <summary>Actual run paths, gateway origin and broker reference may vary while exact launch checks remain.</summary>
    /// <param name="host">Fixture host.</param><param name="scenario">Independent input case.</param>
    [Theory, MemberData(nameof(Helpers))]
    public void ValidRunSpecificSettingsPass(string host, string scenario)
    {
        var settings = new Phase0NativeLaunchSettings(
            host == "Windows" ? "C:/Program Files/PowerShell/7/pwsh.exe" : "/usr/bin/pwsh",
            host == "Windows" ? "F:/GitHub/McpServer/.mcpServer/tmp/run-42/helper & literal.ps1"
                : "/home/sharpninja/github/McpServer/.mcpServer/tmp/run-42/helper & literal.ps1",
            "https://another-gateway.lab:7843", "fixture-pipe-run-42");
        Phase0NativeClientContract.AssertHelper(new ScriptedProbe(), host, scenario, Guid.NewGuid().ToString("N"), settings);
    }

    /// <summary>Wrong launch, protocol outcome, credential or diagnostic facts are rejected safely.</summary>
    /// <param name="host">Fixture host.</param><param name="scenario">Independent case.</param><param name="fault">Single mutation.</param>
    [Theory, MemberData(nameof(HelperFaults))]
    public void WrongHelperIsRejected(string host, string scenario, string fault)
    {
        var probe = new ScriptedProbe(fault);
        var error = Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0NativeClientContract.AssertHelper(probe, host, scenario, probe.Canary));
        Assert.True(probe.Injected, "Helper fault was never exercised.");
        Assert.False(error.ToString().Contains(probe.Canary, StringComparison.Ordinal), "Negative-control diagnostic leaked its canary.");
    }
    /// <summary>Correct shared-policy and TLS ordering observations pass without a network.</summary>
    /// <param name="channel">Fixture channel.</param><param name="peer">Fixture peer.</param>
    [Theory, MemberData(nameof(TlsCases))]
    public void CorrectTlsPasses(string channel, string peer)
    {
        var probe = new ScriptedProbe(); Phase0NativeClientContract.AssertTls(probe, channel, peer);
        Assert.Equal(new[] { $"tls:{channel}:{peer}" }, probe.Calls);
    }
    /// <summary>Policy drift, skipped verification, early/missing bearer sends and wrong outcomes fail.</summary>
    /// <param name="channel">Fixture channel.</param><param name="peer">Fixture peer.</param><param name="fault">Single mutation.</param>
    [Theory, MemberData(nameof(TlsFaults))]
    public void WrongTlsIsRejected(string channel, string peer, string fault)
    {
        var probe = new ScriptedProbe(fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0NativeClientContract.AssertTls(probe, channel, peer));
        Assert.True(probe.Injected, "TLS fault was never exercised.");
    }

    /// <summary>Literal expected launch/process/output transcripts; independent of the assertion implementation.</summary>
    private static readonly IReadOnlyDictionary<string, (int Attempts, int Starts, string Code, bool Success)> HelperFixtures =
        new Dictionary<string, (int, int, string, bool)>(StringComparer.Ordinal)
        {
            ["Success"] = (1, 1, "", true), ["SuccessNoExpiry"] = (1, 1, "", true),
            ["StartFailure"] = (1, 0, "credential_helper_failed", false),
            ["ExitFailure"] = (1, 1, "credential_helper_failed", false),
            ["BrokerUnavailable"] = (1, 1, "credential_helper_failed", false),
            ["MalformedJson"] = (1, 1, "credential_helper_response_invalid", false),
            ["TrailingJson"] = (1, 1, "credential_helper_response_invalid", false),
            ["Expired"] = (1, 1, "credential_helper_response_invalid", false),
            ["ReservedHeader"] = (1, 1, "credential_helper_response_invalid", false),
            ["ConflictingHeader"] = (1, 1, "credential_helper_response_invalid", false),
            ["MissingCredential"] = (1, 1, "credential_helper_response_invalid", false),
            ["MultipleCredentials"] = (1, 1, "credential_helper_response_invalid", false),
            ["Policy:Empty"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:Missing"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:Malformed"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:DuplicateKey"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:UnknownKey"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:WrongVersion"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:InsecureOverride"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:PasswordOverride"] = (0, 0, "federation_transport_policy_invalid", false),
            ["Policy:HelperOverride"] = (0, 0, "federation_transport_policy_invalid", false)
        };
    /// <summary>Independent mutation inventory, including credential exposure on every newly observed surface.</summary>
    private static readonly string[] HelperFields = ["Missing", "Executable", "Arguments", "Shell", "Input", "Environment",
        "Attempts", "Starts", "Exit", "Code", "Credential", "Header", "Sends", "MissingCapture", "NullCapture",
        "Stdout", "Stderr", "Exception", "Diagnostics", "CanaryExecutable", "CanaryArg", "CanaryInput", "CanaryEnvKey",
        "CanaryEnvValue", "CanaryCaptureKey", "CanaryCaptureValue"];
    /// <summary>Independent trust and chronology mutation inventory.</summary>
    private static readonly string[] TlsFields = ["Missing", "Channel", "Ca", "ServerName", "PinsMissing", "PinsExtra",
        "PinsDuplicate", "SkipVerification", "EventMissing", "EventExtra", "EventOrder", "Code", "Sends"];

    /// <summary>Pure scripted observation source, with no parser, TLS, process or transport implementation.</summary>
    /// <param name="fault">Optional observation corruption.</param>
    private sealed class ScriptedProbe(string? fault = null) : IPhase0NativeClientProbe
    {
        /// <summary>Invocation-private canary, generated without entering test discovery arguments.</summary>
        internal string Canary { get; } = Guid.NewGuid().ToString("N");
        /// <summary>Observed assertion calls.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Whether the selected observation was actually corrupted.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc/>
        public Phase0HelperRun? RunHelper(string host, string scenario, string canary, Phase0NativeLaunchSettings settings)
        {
            Calls.Add($"helper:{host}:{scenario}");
            var row = HelperFixtures[scenario]; var launched = row.Attempts == 1;
            var captures = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["client_stdout"] = "", ["client_stderr"] = row.Code.Length == 0 ? "" : row.Code + "\n",
                ["exception_text"] = "", ["helper_diagnostics"] = ""
            };
            var environment = new Dictionary<string, string>(StringComparer.Ordinal);
            if (launched) environment.Add("QBRAIN_PHASE0_TOKEN_PIPE", settings.Pipe);
            Phase0HelperRun run = new(launched ? settings.Executable : "",
                launched ? ["-NoProfile", "-NonInteractive", "-File", settings.ScriptPath, "get"] : [], false,
                launched ? JsonSerializer.Serialize(new Dictionary<string, string> { ["uri"] = settings.Origin }) : "",
                environment, row.Attempts, row.Starts,
                row.Success ? 0 : 1, row.Code, row.Success ? "Reference" : "None", row.Success ? "Proxy-Authorization" : "",
                row.Success ? 1 : 0, captures);
            if (fault is null) return run;
            Injected = true;
            switch (fault)
            {
                case "Missing": return null;
                case "Executable": return run with { Executable = run.Executable + "wrong" };
                case "Arguments": return run with { Arguments = run.Arguments.Append("-Command").ToArray() };
                case "Shell": return run with { Shell = true };
                case "Input": return run with { Input = "{\"uri\":\"https://foreign.lab\"}" };
                case "Environment": environment.Clear(); environment["UNEXPECTED"] = "value"; break;
                case "Attempts": return run with { Attempts = run.Attempts + 1 };
                case "Starts": return run with { Starts = run.Starts + 1 };
                case "Exit": return run with { ExitCode = run.ExitCode + 1 };
                case "Code": return run with { Code = "wrong" };
                case "Credential": return run with { Credential = "Basic" };
                case "Header": return run with { Header = "Authorization" };
                case "Sends": return run with { BearerSends = run.BearerSends + 1 };
                case "MissingCapture": captures.Remove("client_stderr"); break;
                case "NullCapture": captures["client_stderr"] = null; break;
                case "Stdout": captures["client_stdout"] = "unexpected"; break;
                case "Stderr": captures["client_stderr"] += "raw failure"; break;
                case "Exception": captures["exception_text"] = "raw failure"; break;
                case "Diagnostics": captures["helper_diagnostics"] = "raw failure"; break;
                case "CanaryExecutable": return run with { Executable = canary };
                case "CanaryArg": return run with { Arguments = [canary] };
                case "CanaryInput": return run with { Input = canary };
                case "CanaryEnvKey": environment[canary] = "value"; break;
                case "CanaryEnvValue": environment["extra"] = canary; break;
                case "CanaryCaptureKey": captures[canary] = ""; break;
                case "CanaryCaptureValue": captures["extra"] = canary; break;
                default: Injected = false; throw new InvalidOperationException("Unknown helper fault.");
            }
            return run;
        }
        /// <inheritdoc/>
        public Phase0NativeTlsRun? RunTls(string channel, string peer)
        {
            Calls.Add($"tls:{channel}:{peer}");
            var valid = peer == "Valid";
            Phase0NativeTlsRun run = new(channel, "fixture-ca-A", "gateway.lab", ["fixture-pin-B", "fixture-pin-A"], false,
                valid ? ["policy_loaded", "tls_verified", "bearer_sent"] : ["policy_loaded", "tls_rejected"],
                valid ? "" : "tls_peer_untrusted", valid ? 1 : 0);
            if (fault is null) return run;
            Injected = true;
            return fault switch
            {
                "Missing" => null, "Channel" => run with { Channel = "wrong" }, "Ca" => run with { Ca = "wrong" },
                "ServerName" => run with { ServerName = "foreign.lab" }, "PinsMissing" => run with { Pins = [] },
                "PinsExtra" => run with { Pins = ["fixture-pin-A", "fixture-pin-B", "foreign-pin"] },
                "PinsDuplicate" => run with { Pins = ["fixture-pin-A", "fixture-pin-A"] },
                "SkipVerification" => run with { SkipVerification = true },
                "EventMissing" => run with { Events = run.Events.Skip(1).ToArray() },
                "EventExtra" => run with { Events = run.Events.Append("bearer_sent").ToArray() },
                "EventOrder" => run with { Events = run.Events.Reverse().ToArray() },
                "Code" => run with { Code = "wrong" }, "Sends" => run with { BearerSends = run.BearerSends + 1 },
                _ => throw new InvalidOperationException("Unknown TLS fault.")
            };
        }
    }
}
