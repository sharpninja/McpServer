using System.Text.Json;
using McpServer.PluginIntegration.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        var caseName = args.Length > 0 ? args[0] : "real";
        var sandbox = args.Length > 1 ? args[1] : "";
        try
        {
            if (caseName == "real")
            {
                var rows = PluginSessionLogCatalog.LoadAndValidate(@"F:\GitHub\McpServer");
                Console.WriteLine("SUCCESS count=" + rows.Count);
                return 0;
            }

            var fakeRepo = Path.Combine(sandbox, "McpServer");
            Directory.CreateDirectory(Path.Combine(fakeRepo, "tests", "McpServer.PluginIntegration.Tests", "scenarios"));
            var catalogPath = Path.Combine(fakeRepo, "tests", "McpServer.PluginIntegration.Tests", "scenarios", "plugin-sessionlog-scenarios.json");
            var livePath = @"F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json";
            var live = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(livePath));
            var rows = live.GetProperty("scenarios").EnumerateArray().Select(e => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(e.GetRawText())!).ToList();

            void WriteCatalog(IEnumerable<Dictionary<string, JsonElement>> selected)
            {
                var payload = JsonSerializer.Serialize(new { scenarios = selected });
                File.WriteAllText(catalogPath, payload);
            }

            Dictionary<string, JsonElement> Clone(int i) => new(rows[i]);

            JsonElement S(string value) => JsonSerializer.SerializeToElement(value);
            JsonElement A(string[] value) => JsonSerializer.SerializeToElement(value);
            JsonElement B(bool value) => JsonSerializer.SerializeToElement(value);

            switch (caseName)
            {
                case "missing-catalog":
                    break;
                case "missing-repo":
                    {
                        var row = Clone(0);
                        row["repositoryName"] = S("mcpserver-missing-repo-plugin");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                        EnsurePlugin(sandbox, "mcpserver-claude-code-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-claude-cowork-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-copilot-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-grok-plugin", "lib/Invoke-McpPlugin.ps1", true);
                        EnsurePlugin(sandbox, "mcpserver-cline-plugin", "dist/index.js", true);
                        EnsurePlugin(sandbox, "mcpserver-cline-v2-plugin", "dist/index.js", true);
                        EnsurePlugin(sandbox, "mcpserver-opencode-plugin", "dist/index.js", true);
                    }
                    break;
                case "missing-entrypoint":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(0);
                        row["entrypoint"] = S("no-such-entrypoint.ps1");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                    }
                    break;
                case "missing-version":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, i != 0);
                    }
                    WriteCatalog(Enumerable.Range(0, 8).Select(Clone));
                    break;
                case "missing-ac1":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(0);
                        row["cacheFolder"] = S("");
                        WriteCatalog(new[] { row }.Concat(Enumerable.Range(1, 7).Select(Clone)));
                    }
                    break;
                case "duplicate-key":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(6);
                        row["cacheFolder"] = S("cline");
                        WriteCatalog(Enumerable.Range(0, 6).Select(Clone).Concat(new[] { row }).Concat(new[] { Clone(7) }));
                    }
                    break;
                case "enabled-count":
                    foreach (var i in Enumerable.Range(0, 8))
                    {
                        var name = rows[i]["repositoryName"].GetString()!;
                        var ep = rows[i]["entrypoint"].GetString()!;
                        EnsurePlugin(sandbox, name, ep, true);
                    }
                    {
                        var row = Clone(7);
                        row["enabled"] = B(false);
                        WriteCatalog(Enumerable.Range(0, 7).Select(Clone).Concat(new[] { row }));
                    }
                    break;
                default:
                    Console.WriteLine("UNKNOWN_CASE " + caseName);
                    return 2;
            }

            if (caseName != "missing-catalog")
            {
                // catalog already written except missing-catalog
            }

            var loaded = PluginSessionLogCatalog.LoadAndValidate(fakeRepo);
            Console.WriteLine("UNEXPECTED_SUCCESS count=" + loaded.Count);
            return 3;
        }
        catch (Exception ex)
        {
            Console.WriteLine("THROW " + ex.GetType().FullName + " :: " + ex.Message);
            return 1;
        }
    }

    private static void EnsurePlugin(string sandbox, string repoName, string entrypoint, bool withVersion)
    {
        var root = Path.Combine(sandbox, repoName);
        var epPath = Path.Combine(root, entrypoint.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(epPath)!);
        if (!File.Exists(epPath))
        {
            File.WriteAllText(epPath, "probe");
        }
        if (withVersion)
        {
            File.WriteAllText(Path.Combine(root, ".version"), "0.0.0-probe");
        }
    }
}
