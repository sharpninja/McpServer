using System.Text;
using Xunit;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC-003 (PLAN-SESSIONTRIAGEFIX-001 E4, triage-report-fbac91dc): the harness writes to every plugin host process's
/// stdin as UTF-8 without a byte order mark, so a Node MCP stdio host's first JSON-RPC line (initialize) is plain JSON its SDK parses. Root
/// cause measured 2026-10-15: with StandardInputEncoding Encoding.UTF8, .NET 10 writes EF BB BF before the first line on a pipe, and the MCP
/// SDK drops a line that does not parse, so Cline v1's initialize never got an answer.
/// Mock gate: the first-bytes assertion and the child-report decoding are proven against pinned bytes, pinned child reports and a
/// BOM-writing encoding, and reject a BOM-prefixed line, a non-JSON first byte and an empty read. Bindings: the one stdin encoding both
/// process launchers use, and the bytes a real pwsh child reads from RealPluginProcessRunner's stdin. Fixture: in-memory streams and one
/// pwsh child; the Cline rows of the deterministic matrix are the end-to-end proof for NodeMcpStdioSession.
/// </summary>
[Trait("PluginInt", "Deterministic")]
[Trait("Category", "Integration")]
public sealed class PluginStdinEncodingTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    private const string InitializeLine = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\"}";

    /// <summary>The stdin probe child's command: reads up to three raw stdin bytes and prints them as hexadecimal.</summary>
    private const string StdinProbeCommand =
        "$stdin = [Console]::OpenStandardInput(); $buffer = [byte[]]::new(3); $count = $stdin.Read($buffer, 0, 3); [Convert]::ToHexString($buffer, 0, $count)";

    /// <summary>TEST-MCP-PLUGININT-001 AC-003: the bytes a host reads start with the JSON-RPC object, not a byte order mark.</summary>
    private static void AssertJsonFirstByte(byte[] received)
    {
        Assert.NotEmpty(received);
        Assert.False(received.Length >= Bom.Length && received.AsSpan(0, Bom.Length).SequenceEqual(Bom), "stdin starts with a UTF-8 byte order mark");
        Assert.Equal((byte)'{', received[0]);
    }

    /// <summary>The bytes one JSON-RPC line produces through a StreamWriter with <paramref name="encoding"/>, as the harness writes stdin.</summary>
    private static byte[] WriteLineThrough(Encoding encoding)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true))
        {
            writer.WriteLine(InitializeLine);
            writer.Flush();
        }

        return stream.ToArray();
    }

    /// <summary>Decodes the hexadecimal report the stdin probe child prints (for example 7B226A) into the bytes it read.</summary>
    private static byte[] FromChildReport(string report) => Convert.FromHexString(report.Trim());

    /// <summary>
    /// Mock gate for TEST-MCP-PLUGININT-001 AC-003: the assertion accepts pinned BOM-free JSON bytes, a line written with
    /// UTF8Encoding(false) and a BOM-free child report, and rejects a BOM-prefixed line, a line written with Encoding.UTF8 (the encoding the
    /// harness used), an empty write, a child report of a byte order mark, a child report whose first byte is not '{' and an empty child
    /// report. Data: pinned byte arrays, pinned child reports and in-memory writes; no harness class.
    /// </summary>
    [Fact]
    public void StdinEncodingAssertion_AcceptsBomFreeAndRejectsBomPrefixed()
    {
        AssertJsonFirstByte("{\"jsonrpc\":\"2.0\"}\n"u8.ToArray());
        AssertJsonFirstByte(WriteLineThrough(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)));
        AssertJsonFirstByte(FromChildReport("7B226A" + Environment.NewLine));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte([0xEF, 0xBB, 0xBF, (byte)'{', (byte)'}']));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte(WriteLineThrough(Encoding.UTF8)));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte([]));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte(FromChildReport("EFBBBF")));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte(FromChildReport("5B7B22")));
        Assert.ThrowsAny<Exception>(() => AssertJsonFirstByte(FromChildReport(string.Empty)));
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-003: the one stdin encoding NodeMcpStdioSession and RealPluginProcessRunner give their child
    /// processes has no preamble and writes a JSON-RPC line whose first byte is '{'. Data: the pinned initialize line.
    /// </summary>
    [Fact]
    public void PluginHostStdin_Encoding_HasNoPreamble()
    {
        Assert.Empty(PluginHostStdin.Encoding.GetPreamble());
        AssertJsonFirstByte(WriteLineThrough(PluginHostStdin.Encoding));
    }

    /// <summary>
    /// Binding for TEST-MCP-PLUGININT-001 AC-003: a real pwsh child started by RealPluginProcessRunner reads the initialize line's first
    /// bytes from its stdin and reports them; they start with '{', not a byte order mark. Data: the pinned initialize line; fixture: one
    /// pwsh child running the stdin probe command (no plugin, no server).
    /// </summary>
    [Fact]
    public async Task RealPluginProcessRunner_ChildStdin_StartsWithJson()
    {
        var result = await new RealPluginProcessRunner().RunAsync(
            new PluginProcessLaunchRequest
            {
                Executable = "pwsh",
                Arguments = ["-NoProfile", "-NonInteractive", "-Command", StdinProbeCommand],
                StandardInput = InitializeLine + "\n",
                Environment = new Dictionary<string, string>(StringComparer.Ordinal),
                WorkingDirectory = AppContext.BaseDirectory,
                Timeout = TimeSpan.FromSeconds(60),
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.ExitCode == 0, "the stdin probe child failed: " + result.StandardError);
        AssertJsonFirstByte(FromChildReport(result.StandardOutput));
    }
}
