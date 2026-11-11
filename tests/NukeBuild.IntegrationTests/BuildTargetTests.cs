using Nuke.Common.IO;
using System.Xml.Linq;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-NUKE-001 (historical): Verifies that the Nuke _build project compiles and the Build class
/// is defined with the expected targets. Since NukeBuild requires specific runtime
/// initialization (assembly name = "_build"), we verify via reflection rather than
/// direct instantiation.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence or text: TR-MCP-NUKE-002 AC-002 (BuildTargets_UseGitVersionForCompileAndReplTool),
/// TR-MCP-NUKE-007 AC-001 (InstallReplTool_ParsesInstalledGlobalToolVersion) and TR-MCP-SYNC-001 AC-003
/// (PlanPluginVersionUpdates_BumpsMinorAndSkipsNodeModules).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-NUKE-002 AC-002, TR-MCP-NUKE-007 AC-001 and TR-MCP-SYNC-001 AC-003: cites the criteria these cases prove, so the criterion text and evidence no longer name the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TR-MCP-NUKE-002 AC-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of BuildTargetTests, split from its infrastructure cases in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). Its eleven checkout-reading cases and FindRepositoryRoot, which only they use, moved unchanged; the halves share no private helper. Of the coverage named above, BuildTargets_UseGitVersionForCompileAndReplTool (TR-MCP-NUKE-002 AC-002) is in this half and the other two cases stay in Build.Tests; only the namespace changed.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class BuildTargetTests
{
    /// <summary>Sync restores lib-node with npm ci when typescript/bin/tsc is missing, before npm run build.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_EnsuresNodeCoreNpmCiBeforeNpmRunBuild()
    {
        var repoRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.SyncAgentPlugins.cs"));
        const string ensureCall = "EnsureNodePluginCoreDependencies(nodeCoreRoot.ToString());";
        const string buildCall = "ProcessTasks.StartProcess(\"npm\", \"run build\", workingDirectory: nodeCoreRoot.ToString())";

        Assert.Contains(ensureCall, source, StringComparison.Ordinal);
        Assert.Contains("npm\", \"ci\"", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf(ensureCall, StringComparison.Ordinal) < source.IndexOf(buildCall, StringComparison.Ordinal),
            "npm ci must restore typescript/bin/tsc before npm run build.");
    }

    /// <summary>TEST-MCP-PLUGIN-PSONLY-001 (historical): Plugin sync refreshes Node core vendor packages before installed caches are refreshed.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_RefreshesNodeCoreVendorPackageBeforeCaches()
    {
        var repoRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.SyncAgentPlugins.cs"));
        const string refreshCall = "RefreshNodePluginCoreVendorPackages(RootDirectory, pluginRoots);";
        const string cacheCall = "RefreshKnownPluginCaches(pluginRoots, nextVersion);";

        Assert.Contains(refreshCall, source, StringComparison.Ordinal);
        Assert.Contains("sharpninja-mcpserver-plugin-core.tgz", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf(refreshCall, StringComparison.Ordinal) < source.IndexOf(cacheCall, StringComparison.Ordinal),
            "Node plugin core vendor packages must be refreshed before installed plugin caches are copied.");
    }

    /// <summary>MCP-PLUGIN-SYNC-001: Plugin sync refreshes the workspace staged package before optional sibling plugin repositories.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SyncAgentPlugins_RefreshesWorkspaceStagedPackageBeforeSiblingDiscovery()
    {
        var repoRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.SyncAgentPlugins.cs"));
        const string stagedSync = "SyncPluginCorePackage(RootDirectory, stagedRoot, syncScript, wrapperScript, \"claude-code\");";
        const string discoverCall = "var pluginRoots = DiscoverAgentPluginRoots(RootDirectory, AgentPluginParent);";

        Assert.Contains(stagedSync, source, StringComparison.Ordinal);
        Assert.Contains(discoverCall, source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf(stagedSync, StringComparison.Ordinal) < source.IndexOf(discoverCall, StringComparison.Ordinal),
            "The ignored workspace staged package must be refreshed before optional sibling plugin discovery can return early.");
    }

    /// <summary>TEST-MCP-QBAGENTTOOL-001 (historical): QBAgent is configured as a .NET global tool package.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void QBAgentProject_IsConfiguredAsDotNetTool()
    {
        var repoRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repoRoot, "src", "QBrainAi.QBAgent", "QBrainAi.QBAgent.csproj");
        var properties = XDocument.Load(projectPath)
            .Descendants("PropertyGroup")
            .Elements()
            .ToDictionary(element => element.Name.LocalName, element => element.Value, StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Exe", properties["OutputType"]);
        Assert.Equal("true", properties["IsPackable"]);
        Assert.Equal("true", properties["PackAsTool"]);
        Assert.Equal("qbagent", properties["ToolCommandName"]);
        Assert.Equal("QBrainAI.QBAgent", properties["PackageId"]);
    }

    /// <summary>
    /// PackQBAgentTool must pack QBrainAi.QBAgent.csproj only. Depending on solution Compile
    /// builds every test project and fails when testhosts lock those outputs.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PackQBAgentTool_DoesNotDependOnSolutionCompile()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.PackQBAgentTool.cs"));
        Assert.DoesNotContain("DependsOn(Compile)", source, StringComparison.Ordinal);
        Assert.Contains("QBrainAi.QBAgent.csproj", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TestsDirectory", source, StringComparison.Ordinal);
    }

    /// <summary>DeployQBAgentTool installs the packed tool; it must not pull solution Compile.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void DeployQBAgentTool_DependsOnPackQBAgentTool_NotCompile()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.DeployQBAgentTool.cs"));
        Assert.Contains("DependsOn(PackQBAgentTool)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DependsOn(Compile)", source, StringComparison.Ordinal);
    }

    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ResolveNuGetPackageVersion_ReturnsExplicitParameter()
    {
        var repoRoot = FindRepositoryRoot();
        var gitVersionPath = (AbsolutePath)Path.Combine(repoRoot, "GitVersion.yml");

        var version = Build.ResolveNuGetPackageVersion(" 2.3.4 ", gitVersionPath);

        Assert.Equal("2.3.4", version);
    }

    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Build_SourceProjects_DoNotPinDivergentVersionMetadata()
    {
        var repoRoot = FindRepositoryRoot();
        var packageProjects = Directory.GetFiles(
            Path.Combine(repoRoot, "src"),
            "*.csproj",
            SearchOption.AllDirectories);

        Assert.NotEmpty(packageProjects);

        foreach (var project in packageProjects)
        {
            var content = File.ReadAllText(project);

            Assert.DoesNotContain("<Version>", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<PackageVersion>", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void BuildTargets_UseGitVersionForCompileAndReplTool()
    {
        var repoRoot = FindRepositoryRoot();
        var compileTarget = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.Compile.cs"));
        var packReplTarget = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.PackReplTool.cs"));
        var installReplTarget = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.InstallReplTool.cs"));

        Assert.Contains("ResolveNuGetPackageVersion(PackageVersion, RootDirectory / \"GitVersion.yml\")", compileTarget, StringComparison.Ordinal);
        Assert.Contains("EnumerateFiles(sourceDirectory, \"*.csproj\", SearchOption.AllDirectories)", compileTarget, StringComparison.Ordinal);
        Assert.Contains("EnumerateFiles(testsDirectory, \"*.csproj\", SearchOption.AllDirectories)", compileTarget, StringComparison.Ordinal);
        Assert.Contains("!path.EndsWith(Path.Combine(\"Build.Tests\", \"Build.Tests.csproj\"), StringComparison.OrdinalIgnoreCase)", compileTarget, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetProjectFile(Solution)", compileTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"PackageVersion\", buildVersion)", compileTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"Version\", buildVersion)", compileTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"InformationalVersion\", buildVersion)", compileTarget, StringComparison.Ordinal);

        Assert.Contains("ResolveNuGetPackageVersion(PackageVersion, RootDirectory / \"GitVersion.yml\")", packReplTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"PackageVersion\", packageVersion)", packReplTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"Version\", packageVersion)", packReplTarget, StringComparison.Ordinal);
        Assert.Contains(".SetProperty(\"InformationalVersion\", packageVersion)", packReplTarget, StringComparison.Ordinal);

        Assert.Contains("ResolveNuGetPackageVersion(PackageVersion, RootDirectory / \"GitVersion.yml\")", installReplTarget, StringComparison.Ordinal);
        Assert.Contains("GetInstalledGlobalToolVersion(installedTools, packageId)", installReplTarget, StringComparison.Ordinal);
        Assert.Contains("tool uninstall --global {packageId}", installReplTarget, StringComparison.Ordinal);
        Assert.Contains("--version {packageVersion}", installReplTarget, StringComparison.Ordinal);
    }

    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void CopyBrainSlotRuntimeConfig_CopiesAssignmentFileToDeploymentConfigPath()
    {
        var repoRoot = FindRepositoryRoot();
        var destination = Path.Combine(Path.GetTempPath(), $"mcpserver-build-test-{Guid.NewGuid():N}");

        try
        {
            var copied = Build.CopyBrainSlotRuntimeConfig((AbsolutePath)repoRoot, destination);

            Assert.True(File.Exists(copied));
            Assert.EndsWith(
                Path.Combine("config", Build.BrainSlotConfigDirectoryName, Build.BrainSlotConfigFileName),
                copied,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains("slotId: brain-slot-arbiter-of-truth-grok-build", File.ReadAllText(copied), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
        }
    }

    /// <summary>Windows UpdateService validates a complete stage before it stops the live service.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void UpdateService_StagesBaselineConfigurationBeforeStoppingWindowsService()
    {
        var repoRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.UpdateService.cs"));
        const string stageCall = "WindowsServiceHelper.EnsureBaselineConfiguration(stageDir, baselineConfigPath);";
        const string stopCall = "WindowsServiceHelper.StopService(serviceName, serviceProcessName);";
        const string backupCall = "WindowsServiceHelper.BackupPreservedState(installPath, backupDir, archivePath);";
        const string copyCall = "WindowsServiceHelper.CopyDirectory(stageDir, installPath);";

        Assert.Contains(stageCall, source, StringComparison.Ordinal);
        Assert.Contains(stopCall, source, StringComparison.Ordinal);
        Assert.Contains(backupCall, source, StringComparison.Ordinal);
        Assert.Contains(copyCall, source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf(stageCall, StringComparison.Ordinal) < source.IndexOf(stopCall, StringComparison.Ordinal),
            "The complete publish stage must be validated before the service is stopped.");
        Assert.True(
            source.IndexOf(stopCall, StringComparison.Ordinal) < source.IndexOf(backupCall, StringComparison.Ordinal),
            "The live service must stop before its mutable state is backed up.");
        Assert.True(
            source.IndexOf(backupCall, StringComparison.Ordinal) < source.IndexOf(copyCall, StringComparison.Ordinal),
            "Backup must complete before published files replace the live installation.");
    }

    /// <summary>UpdateService publishes the server as ReadyToRun for both supported operating-system paths.</summary>
    /// <remarks>
    /// 2026-11-10, TR-MCP-SERVICEUPDATE-001 ready-to-run-publish and TEST-MCP-SERVICEUPDATE-001
    /// ready-to-run-publish: requires the ReadyToRun property independently in the Windows and Linux server publish
    /// blocks while rejecting it from the launcher-only block.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void UpdateService_PublishesServerAsReadyToRunOnWindowsAndLinux()
    {
        var repoRoot = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(repoRoot, "build", "Build.UpdateService.cs"));
        const string readyToRun = ".SetProperty(\"PublishReadyToRun\", \"true\")";

        var windowsStart = source.IndexOf("// Publish main server", StringComparison.Ordinal);
        var launcherStart = source.IndexOf("// Publish launcher sidecar", windowsStart, StringComparison.Ordinal);
        var launcherEnd = source.IndexOf("var baselineConfigPath", launcherStart, StringComparison.Ordinal);
        var linuxStart = source.IndexOf("DotNetPublish(settings => settings", launcherEnd, StringComparison.Ordinal);
        var linuxEnd = source.IndexOf("var helper = new LinuxServiceHelper", linuxStart, StringComparison.Ordinal);

        Assert.True(windowsStart >= 0 && launcherStart > windowsStart, "Windows service publish block was not found.");
        Assert.True(launcherEnd > launcherStart, "Launcher publish block was not found.");
        Assert.True(linuxStart > launcherEnd && linuxEnd > linuxStart, "Linux service publish block was not found.");

        var windowsPublish = source[windowsStart..launcherStart];
        var launcherPublish = source[launcherStart..launcherEnd];
        var linuxPublish = source[linuxStart..linuxEnd];

        Assert.Contains(readyToRun, windowsPublish, StringComparison.Ordinal);
        Assert.Contains(readyToRun, linuxPublish, StringComparison.Ordinal);
        Assert.DoesNotContain(readyToRun, launcherPublish, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "config",
                Build.BrainSlotConfigDirectoryName,
                Build.BrainSlotConfigFileName);
            if (File.Exists(candidate))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing brain-slot configuration.");
    }
}
