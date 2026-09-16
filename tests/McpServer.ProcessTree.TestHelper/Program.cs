using System.Diagnostics;
using System.Globalization;

namespace McpServer.ProcessTree.TestHelper;

/// <summary>
/// Creates a native descendant immediately and records its process identifier before exiting.
/// </summary>
internal static class Program
{
    /// <summary>Runs the immediate-descendant probe or argv-echo mode.</summary>
    /// <param name="arguments">Either <c>--argv-echo</c> plus payload, or a single descendant PID path.</param>
    /// <returns>Zero after echoing arguments or recording the descendant identifier.</returns>
    private static int Main(string[] arguments)
    {
        if (arguments.Length >= 1 &&
            string.Equals(arguments[0], "--argv-echo", StringComparison.Ordinal))
        {
            var payload = new string[arguments.Length - 1];
            Array.Copy(arguments, 1, payload, 0, payload.Length);
            Console.Out.Write(System.Text.Json.JsonSerializer.Serialize(payload));
            return 0;
        }

        if (arguments.Length != 1)
            return 64;

        var startInfo = new ProcessStartInfo(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "ping.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("31");
        startInfo.ArgumentList.Add("127.0.0.1");

        using var descendant = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to launch the native descendant.");
        File.WriteAllText(
            arguments[0],
            descendant.Id.ToString(CultureInfo.InvariantCulture));
        return 0;
    }
}
