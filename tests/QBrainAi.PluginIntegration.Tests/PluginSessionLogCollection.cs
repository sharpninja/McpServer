using Xunit;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001: nonparallel collection for plugin Session Log harness tests.
/// </summary>
[CollectionDefinition("PluginSessionLog", DisableParallelization = true)]
public sealed class PluginSessionLogCollection
{
}
