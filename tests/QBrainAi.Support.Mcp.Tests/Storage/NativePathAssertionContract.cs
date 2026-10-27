using System;
using System.Collections.Generic;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>TEST-MCP-PATHNORM-001: unchanged ordinal path assertions with an explicit host input.</summary>
public static class NativePathAssertionContract
{
    /// <summary>Retains the original exact expected native spelling assertion.</summary>
    /// <param name="expected">Requirement-derived native spelling.</param>
    /// <param name="actual">Observed provider output.</param>
    public static void AssertPath(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException("Native path spelling changed.");
    }

    /// <summary>Exercises every original input and its rejecting wrong-output control against independent literal data.</summary>
    /// <returns>Positive cases and rejected controls.</returns>
    public static int[] VerifyMockGate()
    {
        // Platform zero is Windows, one is CaseSensitive, as pinned in WorkspacePathPlatform.
        var cases = new (string Path, int Platform, bool WindowsHost, string Expected)[]
        {
            ("C:/Users/kingd/repo", 0, true, @"C:\Users\kingd\repo"),
            ("C:/", 0, true, @"C:\"),
            ("//srv/share/x", 0, true, @"\\srv\share\x"),
            (@"C:\already\native", 0, true, @"C:\already\native"),
            ("/home/x/repo", 1, true, "/home/x/repo"),
            ("//srv/share/x", 1, true, "//srv/share/x"),
            ("C:/Users/kingd/repo", 0, false, "C:/Users/kingd/repo"),
        };
        var windowsSpellings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["C:/Users/kingd/repo"] = @"C:\Users\kingd\repo",
            ["C:/"] = @"C:\",
            ["//srv/share/x"] = @"\\srv\share\x",
            [@"C:\already\native"] = @"C:\already\native",
        };
        string Model(string path, int platform, bool windowsHost) =>
            windowsHost && platform == 0 ? windowsSpellings[path] : path;
        var rejected = 0;
        foreach (var item in cases)
        {
            var output = Model(item.Path, item.Platform, item.WindowsHost);
            AssertPath(item.Expected, output);
            try { AssertPath(item.Expected, output + "-wrong"); }
            catch (InvalidOperationException) { rejected++; continue; }
            throw new InvalidOperationException("Wrong native path mock output was accepted.");
        }
        if (rejected != 7) throw new InvalidOperationException("Native path control denominator changed.");
        return new[] { cases.Length, rejected };
    }
}
