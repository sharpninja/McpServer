// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nuke.Common.IO;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-SYNC-001 AC-002 (PLAN-SESSIONTRIAGEFIX-001 E1; triage-report-de98c84b and -f2684f90, BUG-BASELINE-012): after the vendor step
/// re-vendors the plugin core, a consumer's package-lock.json records the new tarball's SHA-512 for the dependency its package.json already
/// names, with package.json and every other lockfile line unchanged, through npm only. Build-infrastructure test, accountable through TR
/// only. Mock gate: the plan and lockfile assertions are proven against pinned argument lists and pinned lockfile texts, and reject a plain
/// install, the bare file: spec, the tarball's own package name, a missing --package-lock-only, an npm ci, an empty plan, a lockfile left
/// stale, a hand-reformatted lockfile, the second dependency the bare spec adds, an entry moved to the tarball's name and a second changed
/// line. Bindings: the npm arguments the sync plans for the consumer key found in a package.json, and the real vendor step run on a
/// temporary consumer whose vendored package is named differently from its dependency key. Fixtures: temporary directories; npm for pack,
/// the initial lockfile and npm ci, each with a temporary cache and no registry access.
/// </summary>
public sealed class NodeVendorLockfileTests
{
    private const string ConsumerKey = "@sharpninja/mcpserver-plugin-core";
    private const string TarballPackageName = "@qbrainai/qbrain-ai-plugin-core";
    private const string VendorSpec = "file:vendor/sharpninja-mcpserver-plugin-core.tgz";
    private static readonly string StaleIntegrity = "sha512-" + new string('A', 86) + "==";
    private static readonly string FreshIntegrity = "sha512-" + new string('B', 86) + "==";

    /// <summary>TR-MCP-SYNC-001 AC-002: the npm arguments install the vendored tarball under the consumer's own dependency name, lockfile only.</summary>
    private static void AssertVendorLockRefreshPlan(IReadOnlyList<string> arguments, string dependencyName)
    {
        Assert.NotEmpty(arguments);
        Assert.Equal("install", arguments[0]);
        Assert.Contains("--package-lock-only", arguments);
        Assert.Contains("--ignore-scripts", arguments);
        Assert.Equal(dependencyName + "@" + VendorSpec, arguments[^1]);
        Assert.Single(arguments, argument => argument.Contains("file:", StringComparison.Ordinal));
    }

    /// <summary>
    /// TR-MCP-SYNC-001 AC-002: the refreshed lockfile differs from the stale one in exactly one line, the integrity of the consumer's own
    /// entry, which now equals <paramref name="expectedIntegrity"/>.
    /// </summary>
    private static void AssertLockRecordsTarball(string staleLock, string refreshedLock, string dependencyName, string expectedIntegrity)
    {
        var entry = JsonNode.Parse(refreshedLock)!["packages"]?["node_modules/" + dependencyName];
        Assert.NotNull(entry);
        Assert.Equal(expectedIntegrity, (string?)entry["integrity"]);
        var before = staleLock.Split('\n');
        var after = refreshedLock.Split('\n');
        Assert.Equal(before.Length, after.Length);
        var changed = Enumerable.Range(0, before.Length).Where(index => !string.Equals(before[index], after[index], StringComparison.Ordinal)).ToArray();
        Assert.Single(changed);
        Assert.Contains("\"integrity\":", after[changed[0]], StringComparison.Ordinal);
    }

    /// <summary>
    /// A pinned npm lockfile (version 3) for a consumer that depends on the vendored core under <see cref="ConsumerKey"/>, as npm writes it;
    /// with <paramref name="secondDependency"/>, also the root dependency and entry the bare file: spec adds under the tarball's own name.
    /// </summary>
    private static string PinnedLock(string integrity, bool secondDependency = false)
    {
        var lines = new List<string>
        {
            "{",
            "  \"name\": \"consumer\",",
            "  \"version\": \"1.0.0\",",
            "  \"lockfileVersion\": 3,",
            "  \"requires\": true,",
            "  \"packages\": {",
            "    \"\": {",
            "      \"name\": \"consumer\",",
            "      \"version\": \"1.0.0\",",
            "      \"dependencies\": {",
        };
        if (secondDependency)
        {
            lines.Add("        \"" + TarballPackageName + "\": \"" + VendorSpec + "\",");
        }

        lines.Add("        \"" + ConsumerKey + "\": \"" + VendorSpec + "\"");
        lines.Add("      }");
        lines.Add("    },");
        if (secondDependency)
        {
            lines.Add("    \"node_modules/" + TarballPackageName + "\": {");
            lines.Add("      \"version\": \"0.2.0\",");
            lines.Add("      \"resolved\": \"" + VendorSpec + "\",");
            lines.Add("      \"integrity\": \"" + integrity + "\"");
            lines.Add("    },");
        }

        lines.Add("    \"node_modules/" + ConsumerKey + "\": {");
        lines.Add("      \"name\": \"" + TarballPackageName + "\",");
        lines.Add("      \"version\": \"0.2.0\",");
        lines.Add("      \"resolved\": \"" + VendorSpec + "\",");
        lines.Add("      \"integrity\": \"" + integrity + "\"");
        lines.Add("    }");
        lines.Add("  }");
        lines.Add("}");
        lines.Add(string.Empty);
        return string.Join('\n', lines);
    }

    /// <summary>
    /// TR-MCP-SYNC-001 AC-003: npm versioned the plugin and nothing else: package.json changed in its version line only and package-lock.json
    /// in its two version lines only, each now <paramref name="version"/>, and neither file gained a \u escape.
    /// </summary>
    private static void AssertNpmVersionedOnly(string packageBefore, string packageAfter, string lockBefore, string lockAfter, string version)
    {
        AssertOnlyVersionLinesChanged(packageBefore, packageAfter, version, expectedChangedLines: 1);
        AssertOnlyVersionLinesChanged(lockBefore, lockAfter, version, expectedChangedLines: 2);
    }

    /// <summary>The line-by-line difference between two JSON texts is exactly <paramref name="expectedChangedLines"/> "version" lines.</summary>
    private static void AssertOnlyVersionLinesChanged(string before, string after, string version, int expectedChangedLines)
    {
        var beforeLines = before.Split('\n');
        var afterLines = after.Split('\n');
        Assert.Equal(beforeLines.Length, afterLines.Length);
        var changed = Enumerable.Range(0, beforeLines.Length).Where(index => !string.Equals(beforeLines[index], afterLines[index], StringComparison.Ordinal)).ToArray();
        Assert.Equal(expectedChangedLines, changed.Length);
        Assert.All(changed, index => Assert.Matches("^\"version\": \"" + System.Text.RegularExpressions.Regex.Escape(version) + "\",?$", afterLines[index].Trim()));
        Assert.DoesNotMatch(@"\\u[0-9A-Fa-f]{4}", after);
    }

    /// <summary>TR-MCP-SYNC-001 AC-003: the version bump's own file plan holds no package.json and no package-lock.json.</summary>
    private static void AssertPlanLeavesNpmFiles(IEnumerable<string> plannedPaths)
    {
        var paths = plannedPaths.ToArray();
        Assert.NotEmpty(paths);
        Assert.DoesNotContain(paths, path => Path.GetFileName(path) is "package.json" or "package-lock.json");
    }

    /// <summary>
    /// A pinned package.json shaped like the real Node plugins: a description with an em dash and a plus sign, a script with &amp;&amp;, an
    /// engines range with &gt;=, and the vendored core under <see cref="ConsumerKey"/>.
    /// </summary>
    private static string PinnedPackageJson(string version) => string.Join(
        '\n',
        "{",
        "  \"name\": \"mcpserver-probe-plugin\",",
        "  \"version\": \"" + version + "\",",
        "  \"description\": \"Bridge — TODO management and per-turn + build-verification\",",
        "  \"private\": true,",
        "  \"scripts\": {",
        "    \"prepublishOnly\": \"npm run build && npm test\"",
        "  },",
        "  \"engines\": {",
        "    \"node\": \">=18.0.0\"",
        "  },",
        "  \"dependencies\": {",
        "    \"" + ConsumerKey + "\": \"" + VendorSpec + "\"",
        "  }",
        "}",
        string.Empty);

    /// <summary>A pinned npm lockfile (version 3) for <see cref="PinnedPackageJson"/> with its top-level and root-package versions.</summary>
    private static string PinnedVersionLock(string topVersion, string rootVersion) => string.Join(
        '\n',
        "{",
        "  \"name\": \"mcpserver-probe-plugin\",",
        "  \"version\": \"" + topVersion + "\",",
        "  \"lockfileVersion\": 3,",
        "  \"requires\": true,",
        "  \"packages\": {",
        "    \"\": {",
        "      \"name\": \"mcpserver-probe-plugin\",",
        "      \"version\": \"" + rootVersion + "\",",
        "      \"dependencies\": {",
        "        \"" + ConsumerKey + "\": \"" + VendorSpec + "\"",
        "      },",
        "      \"engines\": {",
        "        \"node\": \">=18.0.0\"",
        "      }",
        "    },",
        "    \"node_modules/" + ConsumerKey + "\": {",
        "      \"name\": \"" + TarballPackageName + "\",",
        "      \"version\": \"0.2.0\",",
        "      \"resolved\": \"" + VendorSpec + "\",",
        "      \"integrity\": \"" + StaleIntegrity + "\"",
        "    }",
        "  }",
        "}",
        string.Empty);

    /// <summary>A new empty temporary directory for one test.</summary>
    private static string NewTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>npm's integrity string for a file: sha512- and the base64 SHA-512 of its bytes, computed here independently of npm.</summary>
    private static string Sri(string path) => "sha512-" + Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(path)));

    /// <summary>
    /// Runs npm in <paramref name="directory"/> through BoundedProcess (TR-MCP-NUKE-009) with <paramref name="cache"/> as its cache,
    /// asserts exit 0, and returns its standard output. On Windows npm is the npm.cmd shim.
    /// </summary>
    private static string RunNpm(string directory, string cache, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(OperatingSystem.IsWindows() ? "npm.cmd" : "npm") { WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--cache");
        startInfo.ArgumentList.Add(cache);
        var result = BoundedProcess.Run(startInfo, TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
        Assert.True(result.ExitCode == 0, $"npm {string.Join(' ', arguments)} exited with code {result.ExitCode}: {result.Stderr}");
        return result.Stdout;
    }

    /// <summary>Runs git in <paramref name="directory"/> through BoundedProcess (TR-MCP-NUKE-009), asserts exit 0, returns its standard output.</summary>
    private static string RunGit(string directory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git") { WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var result = BoundedProcess.Run(startInfo, TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        Assert.True(result.ExitCode == 0, $"git {string.Join(' ', arguments)} exited with code {result.ExitCode}: {result.Stderr}");
        return result.Stdout;
    }

    /// <summary>Deletes a temporary tree, clearing the read-only attribute git gives its object files first.</summary>
    private static void DeleteTree(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(root, recursive: true);
    }

    /// <summary>Packs <paramref name="packageRoot"/> with npm and returns the path of the tarball it wrote there.</summary>
    private static string Pack(string packageRoot, string cache)
    {
        var name = RunNpm(packageRoot, cache, "pack", "--silent")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Last(line => line.EndsWith(".tgz", StringComparison.Ordinal));
        return Path.Combine(packageRoot, name);
    }

    /// <summary>
    /// Mock gate for TR-MCP-SYNC-001 AC-002: the plan assertion accepts install --package-lock-only of the vendored tarball under the
    /// consumer's key and rejects a plain install, the bare file: spec, the tarball's own package name, a missing --package-lock-only, an
    /// npm ci and an empty plan; the lockfile assertion accepts a lockfile whose only change is the consumer entry's integrity and rejects a
    /// stale lockfile, a hand-reformatted one, the second dependency the bare spec adds, an entry moved to the tarball's name and a second
    /// changed line. Data: pinned argument lists and pinned lockfile texts; no npm, no build code.
    /// </summary>
    [Fact]
    public void VendorLockRefreshAssertion_AcceptsKeyedSpecAndRejectsPlainBareHandOrMissingRefresh()
    {
        AssertVendorLockRefreshPlan(["install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund", ConsumerKey + "@" + VendorSpec], ConsumerKey);
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan(["install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund"], ConsumerKey));
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan(["install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund", VendorSpec], ConsumerKey));
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan(["install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund", TarballPackageName + "@" + VendorSpec], ConsumerKey));
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan(["install", "--ignore-scripts", "--no-audit", "--no-fund", ConsumerKey + "@" + VendorSpec], ConsumerKey));
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan(["ci", "--package-lock-only", "--ignore-scripts", ConsumerKey + "@" + VendorSpec], ConsumerKey));
        Assert.ThrowsAny<Exception>(() => AssertVendorLockRefreshPlan([], ConsumerKey));

        var stale = PinnedLock(StaleIntegrity);
        var refreshed = PinnedLock(FreshIntegrity);
        AssertLockRecordsTarball(stale, refreshed, ConsumerKey, FreshIntegrity);
        Assert.ThrowsAny<Exception>(() => AssertLockRecordsTarball(stale, stale, ConsumerKey, FreshIntegrity));
        Assert.ThrowsAny<Exception>(() => AssertLockRecordsTarball(stale, JsonNode.Parse(refreshed)!.ToJsonString(), ConsumerKey, FreshIntegrity));
        Assert.ThrowsAny<Exception>(() => AssertLockRecordsTarball(stale, PinnedLock(FreshIntegrity, secondDependency: true), ConsumerKey, FreshIntegrity));
        Assert.ThrowsAny<Exception>(() => AssertLockRecordsTarball(stale, refreshed.Replace("node_modules/" + ConsumerKey, "node_modules/" + TarballPackageName, StringComparison.Ordinal), ConsumerKey, FreshIntegrity));
        Assert.ThrowsAny<Exception>(() => AssertLockRecordsTarball(stale, refreshed.Replace("\"version\": \"0.2.0\"", "\"version\": \"0.2.1\"", StringComparison.Ordinal), ConsumerKey, FreshIntegrity));
    }

    /// <summary>
    /// Mock gate for TR-MCP-SYNC-001 AC-003: the npm-version assertion accepts a package.json whose only change is its version line and a
    /// lockfile whose only changes are its two version lines, and rejects untouched files, a package.json the bump re-escaped (&amp;&amp; as
    /// &&, as the 2026-10-15 C3 sync wrote it), a re-serialized lockfile, a lockfile with one version line left behind, a lockfile
    /// with a further changed line, and the wrong version; the plan assertion accepts a plan of .version and plugin.json and rejects one that
    /// holds package.json or package-lock.json, and an empty plan. Data: pinned texts and pinned path lists; no npm, no build code.
    /// </summary>
    [Fact]
    public void NpmVersionAssertion_AcceptsVersionLinesOnlyAndRejectsEscapedReformattedOrUntouchedFiles()
    {
        var packageBefore = PinnedPackageJson("1.119.0");
        var packageAfter = PinnedPackageJson("1.120.0");
        var lockBefore = PinnedVersionLock("1.119.0", "1.119.0");
        var lockAfter = PinnedVersionLock("1.120.0", "1.120.0");
        AssertNpmVersionedOnly(packageBefore, packageAfter, lockBefore, lockAfter, "1.120.0");
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, packageBefore, lockBefore, lockBefore, "1.120.0"));
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, packageAfter.Replace("&&", "\\u0026\\u0026", StringComparison.Ordinal), lockBefore, lockAfter, "1.120.0"));
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, packageAfter, lockBefore, JsonNode.Parse(lockAfter)!.ToJsonString(), "1.120.0"));
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, packageAfter, lockBefore, PinnedVersionLock("1.120.0", "1.119.0"), "1.120.0"));
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, packageAfter, lockBefore, lockAfter.Replace("\"lockfileVersion\": 3", "\"lockfileVersion\": 2", StringComparison.Ordinal), "1.120.0"));
        Assert.ThrowsAny<Exception>(() => AssertNpmVersionedOnly(packageBefore, PinnedPackageJson("1.121.0"), lockBefore, PinnedVersionLock("1.121.0", "1.121.0"), "1.120.0"));

        AssertPlanLeavesNpmFiles([".version", ".codex-plugin/plugin.json"]);
        Assert.ThrowsAny<Exception>(() => AssertPlanLeavesNpmFiles([".version", "package.json"]));
        Assert.ThrowsAny<Exception>(() => AssertPlanLeavesNpmFiles([".version", "package-lock.json"]));
        Assert.ThrowsAny<Exception>(() => AssertPlanLeavesNpmFiles([]));
    }

    /// <summary>
    /// Binding for TR-MCP-SYNC-001 AC-003: the version bump's own plan for a Node plugin root holds .version and plugin.json and leaves
    /// package.json and package-lock.json to npm. Data: a temporary plugin root holding all four files; no npm.
    /// </summary>
    [Fact]
    public void PlanPluginVersionUpdates_LeavesNpmFilesToNpm()
    {
        var root = NewTempDirectory("vendor-version-plan-");
        try
        {
            var pluginRoot = Path.Combine(root, "mcpserver-test-plugin");
            Directory.CreateDirectory(Path.Combine(pluginRoot, ".codex-plugin"));
            File.WriteAllText(Path.Combine(pluginRoot, ".version"), "1.3.5\n");
            File.WriteAllText(Path.Combine(pluginRoot, ".codex-plugin", "plugin.json"), """{"name":"mcpserver","version":"1.2.0"}""");
            File.WriteAllText(Path.Combine(pluginRoot, "package.json"), PinnedPackageJson("1.2.9"));
            File.WriteAllText(Path.Combine(pluginRoot, "package-lock.json"), PinnedVersionLock("1.2.9", "1.2.9"));

            var paths = Build.PlanPluginVersionUpdates([(AbsolutePath)pluginRoot], "1.4.0").Select(update => update.Path).ToArray();

            AssertPlanLeavesNpmFiles(paths);
            Assert.Contains(paths, path => path.EndsWith(".version", StringComparison.Ordinal));
            Assert.Contains(paths, path => path.EndsWith(".codex-plugin/plugin.json", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Binding for TR-MCP-SYNC-001 AC-003: Build.VersionNodePluginPackage versions a temporary Node plugin shaped like the real ones (an em
    /// dash, &amp;&amp;, &gt;= and + in its package.json, the vendored core under its key, a git repository with an uncommitted file):
    /// package.json changes in its version line only and package-lock.json in its two version lines only, the vendored integrity stays,
    /// git gets no commit or tag, the uncommitted file stays, and npm ci with an empty cache succeeds. Data: the pinned package.json and a
    /// lockfile written by npm.
    /// </summary>
    [Fact]
    public void NpmVersion_NodePlugin_ChangesOnlyVersionLines()
    {
        var root = NewTempDirectory("vendor-version-npm-");
        try
        {
            var core = Path.Combine(root, "core");
            var plugin = Path.Combine(root, "mcpserver-probe-plugin");
            Directory.CreateDirectory(core);
            Directory.CreateDirectory(Path.Combine(plugin, "vendor"));
            File.WriteAllText(Path.Combine(core, "package.json"), JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["name"] = TarballPackageName,
                ["version"] = "0.2.0",
                ["files"] = new[] { "README.md" },
            }));
            File.WriteAllText(Path.Combine(core, "README.md"), "core\n");
            File.Copy(Pack(core, Path.Combine(root, "cache-pack")), Path.Combine(plugin, "vendor", "sharpninja-mcpserver-plugin-core.tgz"));
            File.WriteAllText(Path.Combine(plugin, "package.json"), PinnedPackageJson("1.119.0"));
            RunNpm(plugin, Path.Combine(root, "cache-lock"), "install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund", "--offline");
            RunGit(plugin, "init", "-q");
            RunGit(plugin, "add", "-A");
            RunGit(plugin, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "-c", "commit.gpgsign=false", "commit", "-q", "-m", "base");
            File.WriteAllText(Path.Combine(plugin, "uncommitted.txt"), "work in progress\n");
            var packagePath = Path.Combine(plugin, "package.json");
            var lockPath = Path.Combine(plugin, "package-lock.json");
            var packageBefore = File.ReadAllText(packagePath);
            var lockBefore = File.ReadAllText(lockPath);
            var integrityBefore = (string?)JsonNode.Parse(lockBefore)!["packages"]?["node_modules/" + ConsumerKey]?["integrity"];

            Build.VersionNodePluginPackage((AbsolutePath)plugin, "1.120.0");

            var lockAfter = File.ReadAllText(lockPath);
            AssertNpmVersionedOnly(packageBefore, File.ReadAllText(packagePath), lockBefore, lockAfter, "1.120.0");
            Assert.False(string.IsNullOrEmpty(integrityBefore));
            Assert.Equal(integrityBefore, (string?)JsonNode.Parse(lockAfter)!["packages"]?["node_modules/" + ConsumerKey]?["integrity"]);
            Assert.Equal("1", RunGit(plugin, "rev-list", "--count", "HEAD").Trim());
            Assert.Equal(string.Empty, RunGit(plugin, "tag").Trim());
            Assert.True(File.Exists(Path.Combine(plugin, "uncommitted.txt")));
            RunNpm(plugin, Path.Combine(root, "cache-ci"), "ci", "--ignore-scripts", "--no-audit", "--no-fund", "--offline");
        }
        finally
        {
            DeleteTree(root);
        }
    }

    /// <summary>
    /// Binding for TR-MCP-SYNC-001 AC-002: for a consumer package.json shaped like the three real consumers (the vendored core under
    /// <see cref="ConsumerKey"/> beside registry dependencies), the sync finds exactly that key and plans the npm refresh under it. Data: a
    /// package.json written to a temporary directory; no npm.
    /// </summary>
    [Fact]
    public void VendorLockRefresh_PlannedCommand_InstallsVendoredSpecUnderConsumerKey()
    {
        var root = NewTempDirectory("vendor-lock-plan-");
        try
        {
            var packageJson = Path.Combine(root, "package.json");
            File.WriteAllText(packageJson, JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["name"] = "consumer",
                ["version"] = "1.0.0",
                ["dependencies"] = new Dictionary<string, string> { [ConsumerKey] = VendorSpec, ["js-yaml"] = "^4.1.0" },
                ["devDependencies"] = new Dictionary<string, string> { ["typescript"] = "^5.6.0" },
            }));

            var names = Build.GetVendorDependencyNames(packageJson);
            Assert.Equal(new[] { ConsumerKey }, names);
            foreach (var name in names)
            {
                AssertVendorLockRefreshPlan(Build.GetVendorLockRefreshArguments(name), name);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Binding for TR-MCP-SYNC-001 AC-002: the real vendor step (Build.VendorNodeCoreIntoConsumer) re-vendors a repacked core into a
    /// temporary consumer whose vendored package (named @qbrainai/qbrain-ai-plugin-core) sits under the key @sharpninja/mcpserver-plugin-core,
    /// and the consumer's lockfile then records the new tarball's SHA-512 in that entry and nothing else, package.json is unchanged, and npm
    /// ci with an empty cache succeeds. Data: a two-file package packed twice with different bytes; the initial lockfile is written by npm.
    /// </summary>
    [Fact]
    public void VendorLockRefresh_OfflineConsumer_RecordsNewTarballIntegrity()
    {
        var root = NewTempDirectory("vendor-lock-refresh-");
        try
        {
            var core = Path.Combine(root, "core");
            var consumer = Path.Combine(root, "consumer");
            var vendor = Path.Combine(consumer, "vendor");
            Directory.CreateDirectory(core);
            Directory.CreateDirectory(vendor);
            File.WriteAllText(Path.Combine(core, "package.json"), JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["name"] = TarballPackageName,
                ["version"] = "0.2.0",
                ["files"] = new[] { "README.md" },
            }));
            File.WriteAllText(Path.Combine(core, "README.md"), "first\n");
            var vendored = Path.Combine(vendor, "sharpninja-mcpserver-plugin-core.tgz");
            File.Copy(Pack(core, Path.Combine(root, "cache-pack-1")), vendored);
            var packageJson = Path.Combine(consumer, "package.json");
            File.WriteAllText(packageJson, JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["name"] = "consumer",
                ["version"] = "1.0.0",
                ["private"] = true,
                ["dependencies"] = new Dictionary<string, string> { [ConsumerKey] = VendorSpec },
            }));
            RunNpm(consumer, Path.Combine(root, "cache-lock"), "install", "--package-lock-only", "--ignore-scripts", "--no-audit", "--no-fund", "--offline");

            File.WriteAllText(Path.Combine(core, "README.md"), "second, different bytes\n");
            var repacked = Pack(core, Path.Combine(root, "cache-pack-2"));
            var lockPath = Path.Combine(consumer, "package-lock.json");
            var staleLock = File.ReadAllText(lockPath);
            var packageJsonBefore = File.ReadAllText(packageJson);
            Assert.NotEqual(Sri(repacked), Sri(vendored));

            Build.VendorNodeCoreIntoConsumer((AbsolutePath)repacked, (AbsolutePath)vendor);

            Assert.Equal(File.ReadAllBytes(repacked), File.ReadAllBytes(vendored));
            AssertLockRecordsTarball(staleLock, File.ReadAllText(lockPath), ConsumerKey, Sri(vendored));
            Assert.Equal(packageJsonBefore, File.ReadAllText(packageJson));
            RunNpm(consumer, Path.Combine(root, "cache-ci"), "ci", "--ignore-scripts", "--no-audit", "--no-fund", "--offline");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
