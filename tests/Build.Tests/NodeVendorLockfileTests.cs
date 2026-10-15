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
