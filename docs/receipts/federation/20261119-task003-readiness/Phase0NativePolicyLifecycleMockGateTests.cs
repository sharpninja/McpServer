using System.Text;
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Explicit nonsecret policy inputs supplied to the configuration boundary before any connection.</summary>
/// <param name="Host">Logical fixture host.</param><param name="Present">Environment-variable presence.</param>
/// <param name="Path">Environment value.</param><param name="FileState">Available, Missing or Unreadable.</param>
/// <param name="Json">Complete input bytes decoded as UTF8.</param><param name="CaState">Public PEM fixture availability.</param>
/// <param name="Overrides">Explicit incompatible CLI flags.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185 ac-UnknownCertificatePinRejected and
/// ac-RawRemoteCannotBypassPublicationPolicy; TR-MCP-FEDERATION-012; TEST-MCP-233. Test-owned inputs, not product DTOs.</remarks>
internal sealed record Phase0PolicyInput(string Host, bool Present, string Path, string FileState, string Json,
    string CaState, IReadOnlyList<string> Overrides);

/// <summary>Observed configuration facts before helper launch or socket creation.</summary>
/// <param name="Mode">Stock, Strict or Rejected.</param><param name="Exit">Local exit code.</param>
/// <param name="Code">Safe outcome.</param><param name="Origin">Installed canonical origin.</param>
/// <param name="Ca">Installed CA path.</param><param name="Pins">Installed SPKI digest bytes encoded as Base64.</param>
/// <param name="Executable">Selected direct executable.</param><param name="Arguments">Ordered configured helper arguments.</param>
/// <param name="Reads">Policy-file read attempts.</param><param name="ProxyDisabled">Environment proxy disabled.</param>
/// <param name="HelperStarts">Created helpers.</param><param name="Sockets">Created sockets.</param>
/// <param name="GlobalWrites">Global Dolt configuration writes.</param><param name="MachineWrites">Machine environment writes.</param>
/// <param name="Credentials">Emitted credentials.</param>
/// <remarks>2026-11-19: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. Real binding must observe installed
/// values, not echo the input. Strict CA state requires successful PEM loading; cryptographic handshakes are a separate seam.</remarks>
internal sealed record Phase0PolicyObservation(string Mode, int Exit, string Code, string Origin, string Ca,
    IReadOnlyList<string> Pins, string Executable, IReadOnlyList<string> Arguments, int Reads, bool ProxyDisabled,
    int HelperStarts = 0, int Sockets = 0, int GlobalWrites = 0, int MachineWrites = 0, int Credentials = 0);

/// <summary>Actual command-construction or cancellation facts, with no private helper payload.</summary>
/// <param name="Executable">Observed executable.</param><param name="Arguments">Actual ordered argv.</param>
/// <param name="Shell">Shell dispatch.</param><param name="Events">Ordered lifecycle facts.</param>
/// <param name="StartedPid">Observed live blocked-helper identity before cancellation.</param>
/// <param name="LiveHelpers">Retained helper count.</param><param name="LiveDescendants">Retained descendant count.</param>
/// <param name="CacheEntries">Retained credentials.</param><param name="Sends">Bearer sends.</param>
/// <param name="Exit">Exit code.</param><param name="Code">Safe local outcome.</param>
/// <param name="Stdout">Public stdout.</param><param name="Stderr">Public stderr.</param>
/// <param name="PolicyReads">Strict policy reads.</param><param name="GlobalWrites">Global setting writes.</param>
/// <param name="MachineWrites">Machine environment writes.</param>
/// <remarks>2026-11-19: FR-MCP-185; FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials;
/// TR-MCP-FEDERATION-012/011; TEST-MCP-233/232. A PID in a scripted unit fixture is not OS termination evidence.</remarks>
internal sealed record Phase0HelperLifecycle(string Executable, IReadOnlyList<string> Arguments, bool Shell,
    IReadOnlyList<string> Events, int StartedPid, int LiveHelpers, int LiveDescendants, int CacheEntries, int Sends,
    int Exit, string Code, string Stdout, string Stderr, int PolicyReads, int GlobalWrites, int MachineWrites);

/// <summary>Test-owned boundary later implemented by disposable native instrumentation.</summary>
/// <remarks>2026-11-19: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. No product implementation in this mock gate.</remarks>
internal interface IPhase0PolicyLifecycleProbe
{
    /// <summary>Configures from complete input, pausing before connection.</summary>
    /// <param name="input">Exact immutable policy input.</param><returns>Observed selected configuration.</returns>
    Phase0PolicyObservation? Configure(Phase0PolicyInput input);
    /// <summary>Observes command construction or a blocked helper cancelled through the existing context.</summary>
    /// <param name="settings">Immutable run-specific executable, script, origin and broker reference.</param>
    /// <param name="cancel">True for cancellation; false for executable-only stock construction.</param>
    /// <returns>Actual safe lifecycle observation.</returns>
    Phase0HelperLifecycle? Observe(Phase0NativeLaunchSettings settings, bool cancel);
}

/// <summary>Independent configuration and lifecycle assertions shared by scripted and later real adapters.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal static class Phase0PolicyLifecycleContract
{
    /// <summary>Requires exact configured values and no connection or global side effects.</summary>
    /// <param name="probe">Observation provider.</param><param name="input">Explicit actual input.</param>
    /// <param name="expected">Requirement-derived fixture outcome, not a production result.</param>
    internal static void AssertPolicy(IPhase0PolicyLifecycleProbe probe, Phase0PolicyInput input, Phase0PolicyObservation expected)
    {
        // Freeze collection expectations before invoking an untrusted observation provider.
        var expectedPins = expected.Pins.ToArray(); var expectedArguments = expected.Arguments.ToArray();
        var actual = probe.Configure(input); Assert.NotNull(actual);
        Assert.Equal(expected.Mode, actual.Mode); Assert.Equal(expected.Exit, actual.Exit); Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.Origin, actual.Origin); Assert.Equal(expected.Ca, actual.Ca);
        Assert.Equal(expectedPins.Order(StringComparer.Ordinal), actual.Pins.Order(StringComparer.Ordinal));
        Assert.Equal(expected.Executable, actual.Executable); Assert.Equal(expectedArguments, actual.Arguments);
        Assert.Equal(expected.Reads, actual.Reads); Assert.Equal(expected.ProxyDisabled, actual.ProxyDisabled);
        Assert.Equal(0, actual.HelperStarts); Assert.Equal(0, actual.Sockets); Assert.Equal(0, actual.GlobalWrites);
        Assert.Equal(0, actual.MachineWrites); Assert.Equal(0, actual.Credentials);
    }
    /// <summary>Preserves stock construction or proves ordered cancellation, cleanup and safe output.</summary>
    /// <param name="probe">Observation provider.</param><param name="settings">Run-specific immutable inputs.</param>
    /// <param name="cancel">Cancellation versus stock command-construction fixture.</param>
    internal static void AssertLifecycle(IPhase0PolicyLifecycleProbe probe, Phase0NativeLaunchSettings settings, bool cancel)
    {
        var actual = probe.Observe(settings, cancel); Assert.NotNull(actual);
        Assert.Equal(settings.Executable, actual.Executable); Assert.False(actual.Shell);
        Assert.Equal(cancel ? new[] { "-NoProfile", "-NonInteractive", "-File", settings.ScriptPath, "get" } : ["get"], actual.Arguments);
        Assert.Equal(cancel ? new[] { "helper_started", "cancellation_requested", "helper_exited", "cache_cleared" }
            : ["command_constructed"], actual.Events);
        if (cancel) Assert.True(actual.StartedPid > 0, "No live blocked helper was observed before cancellation.");
        else Assert.Equal(0, actual.StartedPid);
        Assert.Equal(0, actual.LiveHelpers); Assert.Equal(0, actual.LiveDescendants); Assert.Equal(0, actual.CacheEntries);
        Assert.Equal(0, actual.Sends); Assert.Equal(cancel ? 1 : 0, actual.Exit);
        Assert.Equal(cancel ? "credential_helper_cancelled" : "", actual.Code);
        Assert.Equal("", actual.Stdout); Assert.Equal(cancel ? "credential_helper_cancelled\n" : "", actual.Stderr);
        Assert.Equal(cancel ? 1 : 0, actual.PolicyReads); Assert.Equal(0, actual.GlobalWrites); Assert.Equal(0, actual.MachineWrites);
    }
}

/// <summary>Finite explicit-input policy, cancellation and compatibility fixtures with independent wrong observations.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-UnknownCertificatePinRejected; FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials;
/// TR-MCP-FEDERATION-012/011; TEST-MCP-233/232. No files, helpers, sockets, tokens or production parser are used.</remarks>
public sealed class Phase0NativePolicyLifecycleMockGateTests
{
    /// <summary>Both logical unit-fixture hosts across all finite policy inputs.</summary>
    public static IEnumerable<object[]> Policies => new[] { "Windows", "Linux" }
        .SelectMany(host => Cases.Select(name => new object[] { host, name }));
    /// <summary>Each observed policy field is independently corrupted for every input.</summary>
    public static IEnumerable<object[]> PolicyFaults => Policies.SelectMany(row => PolicyFields.Select(faultName =>
        new object[] { row[0], row[1], faultName }));
    /// <summary>Cancellation and stock command-construction on both fixture hosts.</summary>
    public static IEnumerable<object[]> Lifecycles => new[] { "Windows", "Linux" }
        .SelectMany(host => new[] { false, true }.Select(cancel => new object[] { host, cancel }));
    /// <summary>Every lifecycle fact is independently corrupted.</summary>
    public static IEnumerable<object[]> LifecycleFaults => Lifecycles.SelectMany(row => LifecycleFields.Select(faultName =>
        new object[] { row[0], row[1], faultName }));

    /// <summary>Correct scripted configuration observations accept the complete explicit inputs.</summary>
    /// <param name="host">Fixture host.</param><param name="name">Independent input case.</param>
    [Theory, MemberData(nameof(Policies))]
    public void CorrectPolicyPasses(string host, string name)
    {
        var fixture = PolicyFixture(host, name); var probe = new ScriptedProbe(fixture.Expected);
        Phase0PolicyLifecycleContract.AssertPolicy(probe, fixture.Input, fixture.Expected);
        Assert.Same(fixture.Input, probe.LastInput); Assert.Equal(1, probe.Calls);
    }
    /// <summary>Wrong observed behavior must be injected and rejected by the same assertions.</summary>
    /// <param name="host">Fixture host.</param><param name="name">Input case.</param><param name="fault">Single fault.</param>
    [Theory, MemberData(nameof(PolicyFaults))]
    public void WrongPolicyIsRejected(string host, string name, string fault)
    {
        var fixture = PolicyFixture(host, name); var probe = new ScriptedProbe(fixture.Expected, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0PolicyLifecycleContract.AssertPolicy(probe, fixture.Input, fixture.Expected));
        Assert.True(probe.Injected); Assert.Same(fixture.Input, probe.LastInput); Assert.Equal(1, probe.Calls);
    }
    /// <summary>A provider cannot rewrite shared fixture pins and thereby rewrite the expected baseline.</summary>
    /// <param name="host">Logical fixture host.</param>
    /// <remarks>2026-11-19: FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-012; TEST-MCP-233.
    /// Freezes expected collections before observation; this control would pass incorrectly without that freeze.</remarks>
    [Theory, InlineData("Windows"), InlineData("Linux")]
    public void SharedPolicyBaselineMutationIsRejected(string host)
    {
        var fixture = PolicyFixture(host, "Valid"); var probe = new ScriptedProbe(fixture.Expected, "SharedAlias");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0PolicyLifecycleContract.AssertPolicy(probe, fixture.Input, fixture.Expected));
        Assert.True(probe.Injected);
    }
    /// <summary>Correct lifecycle facts preserve stock argv or observe clean cancellation.</summary>
    /// <param name="host">Fixture host.</param><param name="cancel">Selected lifecycle.</param>
    [Theory, MemberData(nameof(Lifecycles))]
    public void CorrectLifecyclePasses(string host, bool cancel)
    {
        var settings = Settings(host); var probe = new ScriptedProbe(null);
        Phase0PolicyLifecycleContract.AssertLifecycle(probe, settings, cancel);
        Assert.Same(settings, probe.LastSettings); Assert.Equal(1, probe.Calls);
    }
    /// <summary>Wrong cancellation, cleanup, construction or output facts cannot pass.</summary>
    /// <param name="host">Fixture host.</param><param name="cancel">Selected lifecycle.</param><param name="fault">Single fault.</param>
    [Theory, MemberData(nameof(LifecycleFaults))]
    public void WrongLifecycleIsRejected(string host, bool cancel, string fault)
    {
        var settings = Settings(host); var probe = new ScriptedProbe(null, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0PolicyLifecycleContract.AssertLifecycle(probe, settings, cancel));
        Assert.True(probe.Injected); Assert.Same(settings, probe.LastSettings); Assert.Equal(1, probe.Calls);
    }

    /// <summary>Finite independent policy input inventory; none is a skipped future placeholder.</summary>
    private static readonly string[] Cases = ["Absent", "Valid", "CanonicalOrigin", "ReorderedPins", "EmptyPath", "RelativePath",
        "MissingFile", "UnreadableFile", "MalformedJson", "DuplicateKey", "UnknownKey", "WrongVersion", "StringVersion",
        "MissingVersion", "MissingCa", "RelativeCa", "MissingCaFile", "UnreadableCa", "MalformedCa", "HttpOrigin",
        "UserInfoOrigin", "PathOrigin", "QueryOrigin", "FragmentOrigin", "MissingPins", "EmptyPins", "DuplicatePins",
        "MalformedPin", "WrongWidthPin", "RelativeExecutable", "MissingArguments", "ExtraArguments", "ShellArguments",
        "RelativeScript", "InsecureOverride", "PasswordOverride", "HelperOverride"];
    /// <summary>All policy observation fields, including zero side-effect invariants.</summary>
    private static readonly string[] PolicyFields = ["Missing", "Mode", "Exit", "Code", "Origin", "Ca", "Pins", "DuplicatePin",
        "Executable", "Arguments", "Reads", "Proxy", "Helper", "Socket", "Global", "Machine", "Credential"];
    /// <summary>All lifecycle observation fields, including chronological order and descendant cleanup.</summary>
    private static readonly string[] LifecycleFields = ["Missing", "Executable", "Arguments", "Shell", "Events", "EventOrder",
        "Pid", "Helpers", "Descendants", "Cache", "Sends", "Exit", "Code", "Stdout", "Stderr", "Reads", "Global", "Machine"];

    /// <summary>Run-specific literal inputs used independently by fixtures and assertions.</summary>
    /// <param name="host">Logical host.</param><returns>Immutable nonsecret launch settings.</returns>
    private static Phase0NativeLaunchSettings Settings(string host) => new(host == "Windows"
        ? "C:/Program Files/PowerShell/7/pwsh.exe" : "/usr/bin/pwsh", host == "Windows"
        ? "F:/GitHub/McpServer/.mcpServer/tmp/run-policy/helper & literal.ps1"
        : "/home/sharpninja/github/McpServer/.mcpServer/tmp/run-policy/helper & literal.ps1", "https://gateway.lab:7443", "pipe-policy");

    /// <summary>Builds concrete JSON from native objects; intentional invalid JSON derives from serialized valid data.</summary>
    /// <param name="host">Logical host.</param><param name="name">Explicit fixture case.</param>
    /// <returns>Immutable input and separately specified expected configuration.</returns>
    private static (Phase0PolicyInput Input, Phase0PolicyObservation Expected) PolicyFixture(string host, string name)
    {
        var settings = Settings(host); var root = host == "Windows" ? "F:/Phase0 Run" : "/tmp/Phase0 Run";
        var pinA = Convert.ToBase64String(Enumerable.Repeat((byte)0x11, 32).ToArray());
        var pinB = Convert.ToBase64String(Enumerable.Repeat((byte)0x22, 32).ToArray());
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = 1, ["gatewayOrigin"] = "https://gateway.lab:7443", ["caBundlePath"] = root + "/ca.pem",
            ["spkiSha256Pins"] = new[] { pinA, pinB }, ["helperExecutable"] = settings.Executable,
            ["helperArguments"] = new[] { "-NoProfile", "-NonInteractive", "-File", settings.ScriptPath }
        };
        var path = root + "/policy.json"; var fileState = "Available"; var caState = "Valid"; string[] overrides = [];
        var reads = name is "Absent" or "EmptyPath" or "RelativePath" ? 0 : 1;
        switch (name)
        {
            case "EmptyPath": path = ""; break; case "RelativePath": path = "policy.json"; break;
            case "MissingFile": fileState = "Missing"; break; case "UnreadableFile": fileState = "Unreadable"; break;
            case "UnknownKey": values["unexpected"] = true; break;
            case "WrongVersion": values["schemaVersion"] = 2; break; case "StringVersion": values["schemaVersion"] = "1"; break;
            case "MissingVersion": values.Remove("schemaVersion"); break;
            case "MissingCa": values.Remove("caBundlePath"); break; case "RelativeCa": values["caBundlePath"] = "ca.pem"; break;
            case "MissingCaFile": caState = "Missing"; break; case "UnreadableCa": caState = "Unreadable"; break;
            case "MalformedCa": caState = "Malformed"; break;
            case "HttpOrigin": values["gatewayOrigin"] = "http://gateway.lab:7443"; break;
            case "UserInfoOrigin": values["gatewayOrigin"] = "https://user@gateway.lab:7443"; break;
            case "PathOrigin": values["gatewayOrigin"] = "https://gateway.lab:7443/path"; break;
            case "QueryOrigin": values["gatewayOrigin"] = "https://gateway.lab:7443/?q=1"; break;
            case "FragmentOrigin": values["gatewayOrigin"] = "https://gateway.lab:7443/#fragment"; break;
            case "CanonicalOrigin": values["gatewayOrigin"] = "HTTPS://GATEWAY.LAB:443/"; break;
            case "MissingPins": values.Remove("spkiSha256Pins"); break; case "EmptyPins": values["spkiSha256Pins"] = Array.Empty<string>(); break;
            case "DuplicatePins": values["spkiSha256Pins"] = new[] { pinA, pinA }; break;
            case "MalformedPin": values["spkiSha256Pins"] = new[] { "invalid-base64" }; break;
            case "WrongWidthPin": values["spkiSha256Pins"] = new[] { Convert.ToBase64String(new byte[31]) }; break;
            case "ReorderedPins": values["spkiSha256Pins"] = new[] { pinB, pinA }; break;
            case "RelativeExecutable": values["helperExecutable"] = "pwsh"; break;
            case "MissingArguments": values.Remove("helperArguments"); break;
            case "ExtraArguments": values["helperArguments"] = new[] { "-NoProfile", "-NonInteractive", "-File", settings.ScriptPath, "extra" }; break;
            case "ShellArguments": values["helperArguments"] = "-File helper.ps1"; break;
            case "RelativeScript": values["helperArguments"] = new[] { "-NoProfile", "-NonInteractive", "-File", "helper.ps1" }; break;
            case "InsecureOverride": overrides = ["insecure"]; break; case "PasswordOverride": overrides = ["password"]; break;
            case "HelperOverride": overrides = ["helper"]; break;
        }
        var json = JsonSerializer.Serialize(values);
        if (name == "MalformedJson") json = json[..^1];
        if (name == "DuplicateKey")
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var item in values) { writer.WritePropertyName(item.Key); JsonSerializer.Serialize(writer, item.Value); }
                writer.WriteNumber("schemaVersion", 1); writer.WriteEndObject();
            }
            json = Encoding.UTF8.GetString(stream.ToArray());
        }
        var valid = name is "Valid" or "CanonicalOrigin" or "ReorderedPins";
        var expected = valid
            ? new Phase0PolicyObservation("Strict", 0, "", name == "CanonicalOrigin" ? "https://gateway.lab:443" : "https://gateway.lab:7443",
                root + "/ca.pem", new[] { pinA, pinB }, settings.Executable, ["-NoProfile", "-NonInteractive", "-File", settings.ScriptPath], reads, true)
            : new Phase0PolicyObservation(name == "Absent" ? "Stock" : "Rejected", name == "Absent" ? 0 : 1,
                name == "Absent" ? "" : "federation_transport_policy_invalid", "", "", [], "", [], reads, false);
        return (new(host, name != "Absent", path, fileState, json, caState, overrides), expected);
    }

    /// <summary>Independent scripted outcomes; never parses policy or implements process/TLS behavior.</summary>
    /// <param name="configured">Literal fixture outcome.</param><param name="fault">Optional single observation corruption.</param>
    private sealed class ScriptedProbe(Phase0PolicyObservation? configured, string? fault = null) : IPhase0PolicyLifecycleProbe
    {
        /// <summary>Captured exact input instance.</summary>
        internal Phase0PolicyInput? LastInput { get; private set; }
        /// <summary>Captured exact launch settings instance.</summary>
        internal Phase0NativeLaunchSettings? LastSettings { get; private set; }
        /// <summary>Observed calls.</summary>
        internal int Calls { get; private set; }
        /// <summary>Negative-control reachability.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc/>
        public Phase0PolicyObservation? Configure(Phase0PolicyInput input)
        {
            Calls++; LastInput = input; Assert.NotNull(configured); var value = configured;
            if (fault is null) return value; Injected = true;
            if (fault == "SharedAlias")
            {
                var shared = Assert.IsType<string[]>(value.Pins); shared[0] = "wrong"; return value;
            }
            return fault switch
            {
                "Missing" => null, "Mode" => value with { Mode = "wrong" }, "Exit" => value with { Exit = value.Exit + 1 },
                "Code" => value with { Code = "wrong" }, "Origin" => value with { Origin = value.Origin + "wrong" },
                "Ca" => value with { Ca = value.Ca + "wrong" }, "Pins" => value with { Pins = value.Pins.Append("wrong").ToArray() },
                "DuplicatePin" => value with { Pins = value.Pins.Concat(value.Pins.Count == 0 ? ["extra"] : [value.Pins[0]]).ToArray() },
                "Executable" => value with { Executable = value.Executable + "wrong" },
                "Arguments" => value with { Arguments = value.Arguments.Append("wrong").ToArray() },
                "Reads" => value with { Reads = value.Reads + 1 }, "Proxy" => value with { ProxyDisabled = !value.ProxyDisabled },
                "Helper" => value with { HelperStarts = 1 }, "Socket" => value with { Sockets = 1 },
                "Global" => value with { GlobalWrites = 1 }, "Machine" => value with { MachineWrites = 1 },
                "Credential" => value with { Credentials = 1 }, _ => throw new InvalidOperationException("Unknown policy fault.")
            };
        }
        /// <inheritdoc/>
        public Phase0HelperLifecycle? Observe(Phase0NativeLaunchSettings settings, bool cancel)
        {
            Calls++; LastSettings = settings;
            Phase0HelperLifecycle value = new(settings.Executable,
                cancel ? ["-NoProfile", "-NonInteractive", "-File", settings.ScriptPath, "get"] : ["get"], false,
                cancel ? ["helper_started", "cancellation_requested", "helper_exited", "cache_cleared"] : ["command_constructed"],
                cancel ? 42 : 0, 0, 0, 0, 0, cancel ? 1 : 0, cancel ? "credential_helper_cancelled" : "", "",
                cancel ? "credential_helper_cancelled\n" : "", cancel ? 1 : 0, 0, 0);
            if (fault is null) return value; Injected = true;
            return fault switch
            {
                "Missing" => null, "Executable" => value with { Executable = "wrong" },
                "Arguments" => value with { Arguments = value.Arguments.Append("wrong").ToArray() }, "Shell" => value with { Shell = true },
                "Events" => value with { Events = [] },
                "EventOrder" => value with { Events = cancel ? value.Events.Reverse().ToArray() : ["helper_started", "command_constructed"] },
                "Pid" => value with { StartedPid = cancel ? 0 : 42 }, "Helpers" => value with { LiveHelpers = 1 },
                "Descendants" => value with { LiveDescendants = 1 }, "Cache" => value with { CacheEntries = 1 },
                "Sends" => value with { Sends = 1 }, "Exit" => value with { Exit = value.Exit + 1 },
                "Code" => value with { Code = "wrong" }, "Stdout" => value with { Stdout = "raw output" },
                "Stderr" => value with { Stderr = value.Stderr + "raw error" }, "Reads" => value with { PolicyReads = value.PolicyReads + 1 },
                "Global" => value with { GlobalWrites = 1 }, "Machine" => value with { MachineWrites = 1 },
                _ => throw new InvalidOperationException("Unknown lifecycle fault.")
            };
        }
    }
}
