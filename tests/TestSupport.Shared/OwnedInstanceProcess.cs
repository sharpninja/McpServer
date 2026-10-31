using System.Diagnostics;

namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>
/// TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): starts the process of a temporary instance, or of a REPL
/// child started against one, with an environment no inherited product setting reaches. Linked into every project
/// whose integration cases start such a process (the stdio host case of QBrainAi.Support.Mcp.Tests, ScratchQBrainAi and
/// its REPL children); the rule itself is the pure <see cref="TemporaryInstanceChecks"/> builder and check, proved in
/// QBrainAi.Repl.Core.Tests.
/// </summary>
internal static class OwnedInstanceProcess
{
    /// <summary>
    /// Replaces the environment of <paramref name="startInfo"/> with the one
    /// <see cref="TemporaryInstanceChecks.BuildOwnedInstanceEnvironment"/> builds from the inherited environment and
    /// <paramref name="settings"/>, and throws before the process starts when
    /// <see cref="TemporaryInstanceChecks.CheckOwnedInstanceEnvironment"/> refuses it.
    /// </summary>
    /// <param name="startInfo">The start information whose environment is replaced.</param>
    /// <param name="settings">The process's own variables.</param>
    /// <param name="ownedRoot">The temporary instance's root folder.</param>
    internal static void ApplyOwnedEnvironment(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string> settings, string ownedRoot)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ReplaceEnvironment(startInfo.Environment, settings, ownedRoot);
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: validates an explicit environment before changing its target dictionary.
    /// The process adapter uses this same boundary, so unit tests need no ambient process environment.
    /// </summary>
    /// <param name="environment">The inherited values to replace only after validation succeeds.</param>
    /// <param name="settings">The owned instance's values.</param>
    /// <param name="ownedRoot">The absolute owned data root.</param>
    internal static void ReplaceEnvironment(IDictionary<string, string?> environment, IReadOnlyDictionary<string, string> settings, string ownedRoot)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var inherited = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in environment)
        {
            inherited[key] = value;
        }

        var child = TemporaryInstanceChecks.BuildOwnedInstanceEnvironment(inherited, settings);
        var violation = TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(child, settings, ownedRoot);
        if (violation is not null)
        {
            throw new InvalidOperationException(violation);
        }

        environment.Clear();
        foreach (var (key, value) in child)
        {
            environment[key] = value;
        }
    }
}
