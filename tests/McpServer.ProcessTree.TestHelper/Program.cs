using System.Diagnostics;
using System.Globalization;

namespace McpServer.ProcessTree.TestHelper;

/// <summary>
/// Creates a native descendant immediately and records its process identifier before exiting.
/// </summary>
internal static class Program
{
    /// <summary>Runs the immediate-descendant probe.</summary>
    /// <param name="arguments">Single output path for the descendant process identifier.</param>
    /// <returns>Zero after the descendant identifier is durably recorded.</returns>
    private static int Main(string[] arguments)
    {
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
