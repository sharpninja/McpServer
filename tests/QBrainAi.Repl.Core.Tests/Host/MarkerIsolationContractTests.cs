using System.Security.Cryptography;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Repl.Host;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.Host;

/// <summary>
/// FR-MCP-REPL-007 ac-1 through ac-4, FR-MCP-REPL-008 ac-4, slice M SRC-7/RINT-13:
/// marker traversal and trust use only a test-owned virtual temporary tree and its explicit cache boundary.
/// Fixed paths, marker bytes and cache interaction records are independent of TMPDIR, the host and real profiles.
/// </summary>
public abstract class MarkerIsolationContractTests
{
    /// <summary>Walk adapter used by the mock gate and real binding.</summary>
    private protected abstract string? Walk(string start, RecordingMarkerFileSystem files, out IReadOnlyList<string> searched);

    /// <summary>Diagnosed resolver adapter used by the mock gate and real binding.</summary>
    private protected abstract bool Resolve(RecordingMarkerFileSystem files, out QBrainAiClientOptions? options, out string error);

    /// <summary>A missing marker searches only the owned virtual tree, including its explicit root.</summary>
    [Fact]
    public void Walk_NoMarker_StopsAtOwnedVirtualRoot()
    {
        var files = new RecordingMarkerFileSystem();
        Assert.Null(Walk(RecordingMarkerFileSystem.Start, files, out var searched));
        Assert.Equal(RecordingMarkerFileSystem.Folders, searched);
    }

    /// <summary>The nearest marker wins over an owned parent marker and stops further searching.</summary>
    [Fact]
    public void Walk_NearestMarker_StopsBeforeParent()
    {
        var files = new RecordingMarkerFileSystem();
        files.Markers[files.GetMarkerPath(RecordingMarkerFileSystem.Start)] = [];
        files.Markers[files.GetMarkerPath(RecordingMarkerFileSystem.Root)] = [];
        Assert.Equal(files.GetMarkerPath(RecordingMarkerFileSystem.Start),
            Walk(RecordingMarkerFileSystem.Start, files, out var searched));
        Assert.Equal([RecordingMarkerFileSystem.Start], searched);
    }

    /// <summary>Valid signed bytes read and save verification only through the injected cache boundary.</summary>
    [Fact]
    public void ValidMarker_UsesOnlyInjectedCache()
    {
        var files = RecordingMarkerFileSystem.Signed(valid: true);
        Assert.True(Resolve(files, out var options, out var error), error);
        Assert.NotNull(options);
        Assert.Equal(RecordingMarkerFileSystem.Root, options.WorkspacePath);
        Assert.Equal("http://marker.example:7147/", options.BaseUrl.AbsoluteUri);
        Assert.Equal("fixture-key", options.ApiKey);
        Assert.Equal(["read", "load:codex", "save:codex"], files.CacheEvents);
    }

    /// <summary>An invalid signature is refused after the injected cache miss and is never saved.</summary>
    [Fact]
    public void InvalidMarker_IsNeverSaved()
    {
        var files = RecordingMarkerFileSystem.Signed(valid: false);
        Assert.False(Resolve(files, out var options, out var error));
        Assert.Null(options);
        Assert.Contains("signature", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["read", "load:codex"], files.CacheEvents);
    }

    /// <summary>An injected verified-cache hit supplies trust without saving another entry.</summary>
    [Fact]
    public void InjectedCacheHit_IsUsedWithoutWrite()
    {
        var files = RecordingMarkerFileSystem.Signed(valid: true);
        files.CacheHit = true;
        Assert.True(Resolve(files, out var options, out var error), error);
        Assert.NotNull(options);
        Assert.Equal(RecordingMarkerFileSystem.Root, options.WorkspacePath);
        Assert.Equal(["read", "load:codex"], files.CacheEvents);
    }

    /// <summary>Malformed marker bytes never reach any verification cache.</summary>
    [Fact]
    public void MalformedMarker_NeverReachesCache()
    {
        var files = new RecordingMarkerFileSystem();
        files.Markers[files.GetMarkerPath(RecordingMarkerFileSystem.Root)] = ["malformed"];
        Assert.False(Resolve(files, out var options, out var error));
        Assert.Null(options);
        Assert.Contains("malformed", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["read"], files.CacheEvents);
    }

    /// <summary>
    /// Owned virtual temporary filesystem. Navigation and cache methods are supplied before the interface adopts
    /// them; the independent mock invokes them directly. Marker bytes are signed from a pinned canonical payload.
    /// </summary>
    internal sealed class RecordingMarkerFileSystem : IMarkerFileSystem
    {
        internal const string Root = "/test-owned-temporary-marker-root";
        internal const string Start = Root + "/workspace/src";
        internal static readonly string[] Folders = [Start, Root + "/workspace", Root];
        internal Dictionary<string, string[]> Markers { get; } = new(StringComparer.Ordinal);
        internal List<string> CacheEvents { get; } = [];
        internal MarkerFileClientOptionsResolver.MarkerSettings? ExpectedMarker { get; private set; }
        internal bool SignatureValid { get; private set; }
        internal bool CacheHit { get; set; }

        /// <inheritdoc />
        public bool FileExists(string path) => Markers.ContainsKey(path);

        /// <inheritdoc />
        public string[] ReadAllLines(string path)
        {
            CacheEvents.Add("read");
            return Markers[path];
        }

        /// <summary>Preserves an explicit virtual absolute directory without consulting the native host.</summary>
        public string GetFullPath(string path) => path;

        /// <summary>Stops at the test-owned virtual temporary root rather than the machine's filesystem root.</summary>
        public string? GetParentPath(string path) => path == Root ? null : path[..path.LastIndexOf('/')];

        /// <summary>Combines virtual path syntax without host-dependent separators.</summary>
        public string GetMarkerPath(string directory) => directory + "/AGENTS-README-FIRST.yaml";

        /// <summary>Records an explicit owned-cache miss and verifies the marker's full cache identity.</summary>
        public bool TryUseCachedVerification(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent,
            out QBrainAiClientOptions? options)
        {
            AssertMarkerIdentity(marker);
            CacheEvents.Add("load:" + agent);
            options = CacheHit ? new() { BaseUrl = new Uri(marker.BaseUrl), ApiKey = marker.ApiKey, WorkspacePath = marker.WorkspacePath } : null;
            return CacheHit;
        }

        /// <summary>Records a save only in this injected boundary.</summary>
        public void SaveVerifiedCacheEntry(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent)
        {
            AssertMarkerIdentity(marker);
            CacheEvents.Add("save:" + agent);
        }

        private void AssertMarkerIdentity(MarkerFileClientOptionsResolver.MarkerSettings marker)
        {
            Assert.NotNull(ExpectedMarker);
            Assert.Equal(ExpectedMarker.Value.WorkspacePath, marker.WorkspacePath);
            Assert.Equal(ExpectedMarker.Value.ApiKey, marker.ApiKey);
            Assert.Equal(ExpectedMarker.Value.SignatureValue, marker.SignatureValue);
        }

        internal static RecordingMarkerFileSystem Signed(bool valid)
        {
            var files = new RecordingMarkerFileSystem { SignatureValid = valid };
            var values = new KeyValuePair<string, string>[]
            {
                new("canonicalization", "marker-v1"), new("port", "7147"), new("baseUrl", "http://marker.example:7147"),
                new("apiKey", "fixture-key"), new("workspace", "Fixture"), new("workspacePath", Root), new("pid", "12"),
                new("startedAt", "2026-01-01T00:00:00Z"), new("markerWrittenAtUtc", "2026-01-01T00:00:00Z"),
                new("serverStartedAtUtc", "2026-01-01T00:00:00Z"),
            };
            var endpointNames = new[] { "health", "swagger", "swaggerUi", "mcpTransport", "sessionLog", "sessionLogDialog",
                "contextSearch", "contextPack", "contextSources", "todo", "repo", "desktop", "gitHub", "tools", "workspace",
                "serverStartupUtc", "markerFileTimestamp" };
            var payload = string.Concat(values.Select(value => value.Key + "=" + value.Value + "\n"))
                + string.Concat(endpointNames.Select(name => "endpoints." + name + "=\n"));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("fixture-key"));
            var signature = valid ? Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))) : "invalid-signature";
            var lines = values.Skip(1).Select(value => value.Key + ": " + value.Value).ToList();
            lines.AddRange(["signature:", "  canonicalization: marker-v1", "  value: " + signature]);
            files.Markers[files.GetMarkerPath(Root)] = lines.ToArray();
            files.ExpectedMarker = new("7147", "http://marker.example:7147", "fixture-key", "Fixture", Root, "12",
                "2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z", "marker-v1", signature,
                new Dictionary<string, string>(), new Dictionary<string, string>());
            return files;
        }
    }
}

/// <summary>Independent scripted adapters and deliberately incorrect adapters prove every M2 assertion discriminates.</summary>
public sealed class MarkerIsolationMockTests : MarkerIsolationContractTests
{
    private readonly string? _fault;

    /// <summary>Creates the correct reference adapter.</summary>
    public MarkerIsolationMockTests() { }
    private MarkerIsolationMockTests(string fault) => _fault = fault;

    /// <inheritdoc />
    private protected override string? Walk(string start, RecordingMarkerFileSystem files, out IReadOnlyList<string> searched)
    {
        var folders = new List<string>();
        for (string? path = start; path is not null; path = files.GetParentPath(path))
        {
            folders.Add(path);
            var candidate = files.GetMarkerPath(path);
            if (files.FileExists(candidate)) { searched = folders; return candidate; }
        }
        if (_fault == "ambient-root") folders.Add("/");
        searched = folders;
        return null;
    }

    /// <inheritdoc />
    private protected override bool Resolve(RecordingMarkerFileSystem files, out QBrainAiClientOptions? options, out string error)
    {
        var path = files.GetMarkerPath(RecordingMarkerFileSystem.Root);
        files.ReadAllLines(path);
        options = null;
        if (files.ExpectedMarker is not { } marker) { error = "malformed"; return false; }
        if (_fault != "bypass-injected-cache" && files.TryUseCachedVerification(marker, _fault == "wrong-agent" ? "grok" : "codex", out options))
        { error = string.Empty; return true; }
        if (!files.SignatureValid && _fault != "ambient-cache-hit") { error = "signature failed"; return false; }
        if (_fault != "bypass-injected-cache") files.SaveVerifiedCacheEntry(marker, "codex");
        options = new() { BaseUrl = new Uri("http://marker.example:7147"), ApiKey = "fixture-key", WorkspacePath = RecordingMarkerFileSystem.Root };
        error = string.Empty;
        return true;
    }

    /// <summary>Wrong host-root traversal, cache bypass, ambient trust and cross-agent cache identity are rejected.</summary>
    [Fact]
    public void WrongAdapters_AreRejectedInsidePassingHarness()
    {
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new MarkerIsolationMockTests("ambient-root").Walk_NoMarker_StopsAtOwnedVirtualRoot()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new MarkerIsolationMockTests("bypass-injected-cache").ValidMarker_UsesOnlyInjectedCache()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new MarkerIsolationMockTests("ambient-cache-hit").InvalidMarker_IsNeverSaved()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new MarkerIsolationMockTests("wrong-agent").ValidMarker_UsesOnlyInjectedCache()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new MarkerIsolationMockTests("bypass-injected-cache").InjectedCacheHit_IsUsedWithoutWrite()));
    }
}
