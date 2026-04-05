using Nuke.Common;
using Serilog;

partial class Build
{
    /// <summary>Validate MCP appsettings instance configuration.</summary>
    public Target ValidateConfig => _ => _
        .Executes(() =>
        {
            string[] candidatePaths =
            [
                SourceDirectory / "McpServer.Support.Mcp" / "appsettings.yaml",
                SourceDirectory / "McpServer.Support.Mcp" / "appsettings.yml",
                SourceDirectory / "McpServer.Support.Mcp" / "appsettings.json",
            ];

            var configPath = candidatePaths.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("No appsettings file found.");

            var lines = File.ReadAllLines(configPath);
            var instances = ConfigValidator.ParseInstances(lines)
                ?? throw new InvalidOperationException("Missing 'Mcp' section in config.");

            if (instances.Count == 0)
            {
                Log.Information("No Mcp:Instances configured. Validation passed.");
                return;
            }

            var errors = ConfigValidator.Validate(instances);
            foreach (var error in errors)
                Log.Error(error);

            if (errors.Count > 0)
                throw new InvalidOperationException($"Config validation failed with {errors.Count} error(s).");

            Log.Information("MCP config validation passed for {Count} instances.", instances.Count);
        });
}
