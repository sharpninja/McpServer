// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// BUG-BASELINE-002, TR-MCP-USECASE-018 AC-006: explicit virtual Windows-syntax data, identical on every host.
/// No operating-system selection, native combination or filesystem access supplies unit-test inputs. These paths
/// are identifiers and mocked executable paths; physical tests must allocate their own temporary folders instead.
/// </summary>
internal static class UnitTestPaths
{
    /// <summary>The fixed virtual root, using the lab's forward-slash Windows spelling.</summary>
    public const string Root = "Q:/__mcp_unit_test__";

    /// <summary>Returns a fully qualified fake path under <see cref="Root"/>.</summary>
    /// <param name="segments">Path segments below the root.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(params string[] segments) =>
        string.Join("/", new[] { Root }.Concat(segments.Select(segment => segment.Replace('\\', '/'))));
}
