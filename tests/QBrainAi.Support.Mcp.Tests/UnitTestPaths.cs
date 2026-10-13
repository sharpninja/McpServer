// SPDX-License-Identifier: Apache-2.0
namespace QBrainAi.Support.Mcp.Tests;

/// <summary>
/// BUG-BASELINE-002: fake, fully qualified paths for unit tests on every OS. Windows keeps the suite's historical
/// <c>Q:\__mcp_unit_test__</c> root (a drive that does not exist); other OSes get <c>/__mcp_unit_test__</c> (a root directory that does
/// not exist). A drive-letter literal is a relative path on Linux and macOS, so production code that normalizes, scopes, or validates
/// paths treats it differently there; these paths behave the same on every OS. Nothing is ever created under the root.
/// </summary>
internal static class UnitTestPaths
{
    /// <summary>The fake root for the current OS.</summary>
    public static string Root { get; } = OperatingSystem.IsWindows() ? @"Q:\__mcp_unit_test__" : "/__mcp_unit_test__";

    /// <summary>Returns a fully qualified fake path under <see cref="Root"/>.</summary>
    /// <param name="segments">Path segments below the root.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(params string[] segments) => Path.Combine([Root, .. segments]);
}
