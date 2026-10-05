namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-QBRAIN-001 through TEST-MCP-QBRAIN-008: Phase 1 rebrand acceptance checks
/// against the repository tree. Fixtures are the committed solution, projects, inventory TSV,
/// and the live README. Requirement ids FR-MCP-QBRAIN-001..005 and TR-MCP-QBRAIN-001..008.
/// </summary>
public sealed class QBrainAiRebrandPhase1Tests
{
    private const string LegacyRoot = "Mcp" + "Server";
    private const string LegacyPackagePrefix = "SharpNinja." + "Mcp" + "Server.";
    private const string LegacyRoute = "mcp" + "server/todo";
    private const string LegacyTool = "mcp" + "server-repl";
    private const string LegacyRepoUrlSegment = "SharpNinja/" + "Mcp" + "Server";

    /// <summary>
    /// TEST-MCP-QBRAIN-007: Every inventory row has a class and a closed action.
    /// Reads docs/receipts/qbrain-ai-rebrand-phase1-inventory.tsv.
    /// </summary>
    [Fact]
    public void Inventory_EveryRowHasClosedAction()
    {
        var path = Path.Combine(FindRepositoryRoot(), "docs", "receipts", "qbrain-ai-rebrand-phase1-inventory.tsv");
        Assert.True(File.Exists(path));
        var lines = File.ReadAllLines(path);
        Assert.Equal("path\tline\tpattern\tclass\taction", lines[0]);
        var open = new HashSet<string>(StringComparer.Ordinal) { "", "open", "unclassified", "review" };
        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split('\t');
            Assert.Equal(5, parts.Length);
            Assert.False(string.IsNullOrWhiteSpace(parts[3]));
            Assert.DoesNotContain(parts[4], open);
        }
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-002: The solution root token is QBrainAi. Build.Tests and _build stay.
    /// </summary>
    [Fact]
    public void SolutionAndProjects_UseQBrainAiRoot()
    {
        var root = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root, "QBrainAi.sln")));
        Assert.False(File.Exists(Path.Combine(root, LegacyRoot + ".sln")));

        foreach (var parent in new[] { "src", "tests" })
        {
            var parentPath = Path.Combine(root, parent);
            if (!Directory.Exists(parentPath))
            {
                continue;
            }

            foreach (var directory in Directory.GetDirectories(parentPath))
            {
                var name = Path.GetFileName(directory);
                Assert.False(name.StartsWith(LegacyRoot + ".", StringComparison.Ordinal), name);
                Assert.False(name.Equals(LegacyRoot, StringComparison.Ordinal), name);
            }
        }

        var buildTests = File.ReadAllText(Path.Combine(root, "tests", "Build.Tests", "Build.Tests.csproj"));
        Assert.Contains("<RootNamespace>NukeBuild.Tests</RootNamespace>", buildTests, StringComparison.Ordinal);
        var nuke = File.ReadAllText(Path.Combine(root, "build", "_build.csproj"));
        Assert.Contains("<RootNamespace>_build</RootNamespace>", nuke, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, "QBrainAi")));
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-001: Namespaces use QBrainAi, package ids use QBrainAI, and QBAgent stays.
    /// </summary>
    [Fact]
    public void Tokens_KeepNamespaceAndPackageSpellingsApart()
    {
        var root = FindRepositoryRoot();
        var qbAgent = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.QBAgent", "QBrainAi.QBAgent.csproj"));
        Assert.Contains("<PackageId>QBrainAI.QBAgent</PackageId>", qbAgent, StringComparison.Ordinal);
        Assert.Contains("<ToolCommandName>qbagent</ToolCommandName>", qbAgent, StringComparison.Ordinal);
        Assert.Contains("<RootNamespace>QBrainAi.QBAgent</RootNamespace>", qbAgent, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace QBrainAI", qbAgent, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace QBrain.AI", Directory.GetFiles(Path.Combine(root, "src", "QBrainAi.QBAgent"), "*.cs").Select(File.ReadAllText).First(), StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-003: New package ids and 1.x facades, plus the repl command alias.
    /// </summary>
    [Fact]
    public void Packages_NewIdsAndLegacyFacades()
    {
        var root = FindRepositoryRoot();
        var expected = new[]
        {
            ("src/QBrainAi.Client/QBrainAi.Client.csproj", "QBrainAI.Client"),
            ("src/QBrainAi.Cqrs/QBrainAi.Cqrs.csproj", "QBrainAI.Cqrs"),
            ("src/QBrainAi.Cqrs.Mvvm/QBrainAi.Cqrs.Mvvm.csproj", "QBrainAI.Cqrs.Mvvm"),
            ("src/QBrainAi.McpAgent/QBrainAi.McpAgent.csproj", "QBrainAI.McpAgent"),
            ("src/QBrainAi.Repl.Core/QBrainAi.Repl.Core.csproj", "QBrainAI.Repl.Core"),
            ("src/QBrainAi.Repl.Host/QBrainAi.Repl.Host.csproj", "QBrainAI.Repl"),
            ("src/QBrainAi.QBAgent/QBrainAi.QBAgent.csproj", "QBrainAI.QBAgent"),
        };
        foreach (var (relative, packageId) in expected)
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains($"<PackageId>{packageId}</PackageId>", text, StringComparison.Ordinal);
            Assert.DoesNotContain(LegacyPackagePrefix, text, StringComparison.Ordinal);
        }

        var repl = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Repl.Host", "QBrainAi.Repl.Host.csproj"));
        Assert.Contains("<ToolCommandName>qbrain-ai-repl</ToolCommandName>", repl, StringComparison.Ordinal);

        foreach (var packageId in new[] { "Client", "Cqrs", "Cqrs.Mvvm", "McpAgent", "Repl.Core", "Repl", "QBAgent" })
        {
            var facadeDir = Path.Combine(root, "src", "Compatibility", LegacyPackagePrefix + packageId);
            // Folder name is SharpNinja.QBrainAi.<Component>, which still contains the legacy token.
            var facade = Directory.GetFiles(Path.Combine(root, "src", "Compatibility"), "*.csproj", SearchOption.AllDirectories)
                .Select(File.ReadAllText)
                .FirstOrDefault(text => text.Contains($"<PackageId>{LegacyPackagePrefix}{packageId}</PackageId>", StringComparison.Ordinal));
            Assert.False(facade is null, packageId);
            _ = facadeDir;
        }

        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "build.yml"));
        Assert.Contains("QBRAINAI_NUGET_PUBLISH", workflow, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-004: RepositoryUrl still names the live GitHub repository.
    /// </summary>
    [Fact]
    public void RepositoryUrl_KeepsLiveGitHubName()
    {
        var root = FindRepositoryRoot();
        var client = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Client", "QBrainAi.Client.csproj"));
        Assert.Contains("github.com/" + LegacyRepoUrlSegment, client, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-005: Todo is declared on both product prefixes and /mcp-transport is unchanged.
    /// </summary>
    [Fact]
    public void Routes_KeepProtocolEndpointAndAliasProductPrefix()
    {
        var root = FindRepositoryRoot();
        var todo = FindFile(root, "TodoController.cs");
        Assert.Contains("qbrainai/todo", todo, StringComparison.Ordinal);
        Assert.Contains(LegacyRoute, todo, StringComparison.Ordinal);
        var mapped = Directory.EnumerateFiles(root, "Program.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}receipts{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .Any(text => text.Contains("MapMcp(\"/mcp-transport\")", StringComparison.Ordinal));
        Assert.True(mapped);

        var protocolTool = "Mcp" + "ServerTool";
        var tools = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Support.Mcp", "McpStdio", "QBrainAiMcpTools.cs"));
        Assert.Contains("[" + protocolTool + "Type]", tools, StringComparison.Ordinal);
        Assert.DoesNotContain("[" + "QBrainAi" + "Tool", tools, StringComparison.Ordinal);
        var program = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Support.Mcp", "Program.cs"));
        Assert.Contains(".Add" + LegacyRoot + "()", program, StringComparison.Ordinal);
        Assert.Contains("https://docs." + LegacyRoute.Replace("/todo", "/errors/invalid-body"), program, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-001 and TEST-MCP-QBRAIN-006: README title, Docker slug, and service defaults.
    /// </summary>
    [Fact]
    public void DisplayDockerAndServiceDefaults_UseQBrainNames()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        Assert.StartsWith("# QBrain.AI", readme.TrimStart(), StringComparison.Ordinal);
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.mcp.yml"));
        Assert.Contains("image: qbrain-ai:latest", compose, StringComparison.Ordinal);
        Assert.Contains("qbrain-ai-network", compose, StringComparison.Ordinal);
        Assert.Contains("qbrain-ai-data", compose, StringComparison.Ordinal);
        var service = File.ReadAllText(Path.Combine(root, "scripts", "Manage-McpService.ps1"));
        Assert.Contains("[string]$ServiceName = 'QBrainAi'", service, StringComparison.Ordinal);
        Assert.Contains("[string]$DisplayName = 'QBrain.AI'", service, StringComparison.Ordinal);
        Assert.Contains(@"[string]$InstallPath = 'C:\ProgramData\QBrainAi'", service, StringComparison.Ordinal);
        var pipelines = File.ReadAllText(Path.Combine(root, "azure-pipelines.yml"));
        Assert.Contains("AllowLegionDeploy", pipelines, StringComparison.Ordinal);
        Assert.Contains("PAYTON-LEGION2", pipelines, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-007: Persisted names and the Omarchy database identity stay.
    /// </summary>
    [Fact]
    public void PersistedStateAndHostNames_Stay()
    {
        var root = FindRepositoryRoot();
        var appsettings = File.ReadAllText(Path.Combine(root, "appsettings.yaml"));
        Assert.Contains("mcp.db", appsettings, StringComparison.Ordinal);
        var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
        Assert.Contains(".mcpServer/", gitignore, StringComparison.Ordinal);
        var context = FindFile(root, "McpDbContext.cs");
        Assert.Contains("class McpDbContext", context, StringComparison.Ordinal);
        var installer = File.ReadAllText(Path.Combine(root, "docs", "setup", "install-local-service.ps1"));
        Assert.Contains("Mcp" + "Server_Omarchy", installer, StringComparison.Ordinal);
        Assert.Contains("/opt/mcp" + "server", installer, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-003: Shipped triage migrations keep the original column, and a later migration renames it.
    /// The checkout workspace path stays the McpServer folder.
    /// </summary>
    [Fact]
    public void ReviewAliases_KeepLegacyColumnAndCheckoutPath()
    {
        var root = FindRepositoryRoot();
        var appsettings = File.ReadAllText(Path.Combine(root, "appsettings.yaml"));
        Assert.Contains(@"E:\github\McpServer", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain(@"E:\github\QBrainAi", appsettings, StringComparison.Ordinal);
        var create = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Storage.SqliteMigrations", "Migrations", "20260625061830_AddTriageStorage.cs"));
        Assert.Contains("IsMcpServerRelated", create, StringComparison.Ordinal);
        Assert.DoesNotContain("IsQBrainAiRelated", create, StringComparison.Ordinal);
        var rename = File.ReadAllText(Path.Combine(root, "src", "QBrainAi.Storage.SqliteMigrations", "Migrations", "20261005170000_RenameTriageIsMcpServerRelatedColumn.cs"));
        Assert.Contains("newName: \"IsQBrainAiRelated\"", rename, StringComparison.Ordinal);
        var ensure = File.ReadAllText(Path.Combine(root, "plugins", "core", "lib-sh", "ensure-repl.sh"));
        Assert.Contains("mcpserver-repl", ensure, StringComparison.Ordinal);
        var replBin = File.ReadAllText(Path.Combine(root, "plugins", "core", "lib-sh", "repl-bin.sh"));
        Assert.Contains("mcpserver-repl", replBin, StringComparison.Ordinal);
        Assert.Contains("qbrain-ai-repl", replBin, StringComparison.Ordinal);
        foreach (var caller in new[]
        {
            Path.Combine(root, "plugins", "core", "lib-sh", "repl-invoke.sh"),
            Path.Combine(root, "plugins", "core", "lib-sh", "repl-persistent.sh"),
            Path.Combine(root, "plugins", "core", "lib-sh", "hook-lib.sh"),
            Path.Combine(root, "plugins", "core", "lib-sh", "mcp-status.sh"),
        })
        {
            Assert.Contains("repl-bin.sh", File.ReadAllText(caller), StringComparison.Ordinal);
        }

        Assert.Contains("mcpserver-repl", File.ReadAllText(Path.Combine(root, "plugins", "core", "lib-ps", "repl-invoke.ps1")), StringComparison.Ordinal);
        foreach (var relative in new[] { "Dockerfile", "docker-compose.mcp.yml" })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("QBrainAi__", text, StringComparison.Ordinal);
            Assert.Contains("Mcp__RepoRoot=/workspace", text, StringComparison.Ordinal);
            Assert.Contains("Mcp__Port=7147", text, StringComparison.Ordinal);
            Assert.Contains("Mcp__DataDirectory=/data", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: Node REPL callers accept the installed 1.x command when the new command is absent.
    /// </summary>
    [Fact]
    public void ReplNodeCallers_ResolveLegacyCommand()
    {
        var root = FindRepositoryRoot();
        foreach (var relative in new[]
        {
            Path.Combine("plugins", "core", "lib-node", "src", "transport", "repl-bridge.ts"),
            Path.Combine("tools", "typescript", "mcp-repl-ts", "src", "repl-command.ts"),
        })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("mcpserver-repl", text, StringComparison.Ordinal);
            Assert.Contains("qbrain-ai-repl", text, StringComparison.Ordinal);
        }

        foreach (var relative in new[]
        {
            Path.Combine("tools", "typescript", "mcp-repl-ts", "src", "client", "ReplClient.ts"),
            Path.Combine("tools", "typescript", "mcp-repl-ts", "src", "transport", "ReplBridge.ts"),
        })
        {
            Assert.Contains("resolveReplCommand()", File.ReadAllText(Path.Combine(root, relative)), StringComparison.Ordinal);
        }
    }

    private static string FindFile(string root, string fileName)
    {
        var match = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)
            .FirstOrDefault(path => !path.Contains($"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}receipts{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        Assert.False(match is null, fileName);
        return File.ReadAllText(match!);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln"))
                || File.Exists(Path.Combine(directory.FullName, LegacyRoot + ".sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
