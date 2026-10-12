using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Xml.Linq;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Classifies the accepted session-life fixture matrix.
/// </summary>
internal sealed partial class SessionLifeUnitGateValidator
{
    private const string SourceManifestToken = "source-manifest.json";
    private const string CommandResultsToken = "command-results.json";
    private const string PesterReportToken = "pester/results.xml";
    private const string PesterNativeToken = "pester/native-run.json";
    private const string BuildTestsToken = "Build.Tests|build-tests/Build.Tests.trx";
    private const string BuildTestsRelative = "build-tests/Build.Tests.trx";

    private static readonly XNamespace TrxNamespace =
        "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    private static readonly string[] RequiredTrxCounters =
    {
        "total",
        "executed",
        "passed",
        "failed",
        "notExecuted",
    };

    private static readonly string[] RequiredPesterCounters =
    {
        "Total",
        "Passed",
        "Failed",
        "Skipped",
        "Inconclusive",
        "NotRun",
        "FailedBlocks",
        "FailedContainers",
    };

    private static readonly (string Name, string Reason)[] PesterFailureCounters =
    {
        ("Failed", "failed-pester-tests"),
        ("Skipped", "skipped-pester-tests"),
        ("Inconclusive", "inconclusive-pester-tests"),
        ("NotRun", "not-run-pester-tests"),
        ("FailedBlocks", "failed-pester-blocks"),
        ("FailedContainers", "failed-pester-containers"),
    };

    /// <summary>
    /// Names a plan-backed TRX defect before it is mapped to a lane-specific reason token.
    /// </summary>
    private enum TrxDefect
    {
        /// <summary>The report satisfies the accepted passing shape.</summary>
        None,

        /// <summary>A report timestamp or last write predates the run.</summary>
        Stale,

        /// <summary>Start and finish are missing, unparseable, or out of order.</summary>
        InvalidTimes,

        /// <summary>The result summary outcome is not completed.</summary>
        IncompleteOutcome,

        /// <summary>A required counter attribute is absent.</summary>
        MissingCounter,

        /// <summary>A required counter is not an integer.</summary>
        MalformedCounter,

        /// <summary>A required counter is negative.</summary>
        NegativeCounter,

        /// <summary>The required counters do not add up.</summary>
        Inconsistent,

        /// <summary>The report discovered zero tests.</summary>
        ZeroDiscovered,

        /// <summary>The failed counter is positive.</summary>
        Failed,

        /// <summary>The not-executed counter is positive.</summary>
        NotExecuted,

        /// <summary>An optional skipped counter is present and positive.</summary>
        OptionalSkipped,

        /// <summary>Another optional counter is present and positive.</summary>
        OptionalAdverse,
    }

    /// <inheritdoc />
    public void Validate(SessionLifeUnitGateValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lane = LaneName(request.Scope);
        var resultsRoot = Path.GetFullPath(request.ResultsRoot);

        ValidateSourceManifest(request);
        ValidateInventory(request, lane, resultsRoot);
        ValidateCommandResults(request, lane, resultsRoot);
        if (request.Scope == SessionLifeUnitGateScope.Unit)
        {
            ValidatePester(request, resultsRoot);
            ValidateBuildTests(request, resultsRoot);
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Checks run binding, included-source identity, and tool observations.</summary>
    /// <param name="request">The run-scoped validation request.</param>
    private void ValidateSourceManifest(SessionLifeUnitGateValidationRequest request)
    {
        var document = ReadJsonObject(request.SourceManifestPath);
        if (!string.Equals(ReadString(document, "runId"), request.RunId, StringComparison.Ordinal))
            Reject("wrong-run-source-manifest", SourceManifestToken);

        ValidateSourceDrift(document);
        ValidateTools(document);
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Rejects deleted, edited, and added included sources.</summary>
    /// <param name="manifest">The pre-run source manifest.</param>
    private void ValidateSourceDrift(JsonObject manifest)
    {
        string? deleted = null;
        string? edited = null;
        var recorded = new HashSet<string>(StringComparer.Ordinal);
        var sources = manifest["sources"]?.AsArray() ?? new JsonArray();
        foreach (var node in sources)
        {
            var source = node!.AsObject();
            var relative = NormalizeRelative(ReadString(source, "path"));
            recorded.Add(relative);
            var fullPath = ResolveInsideRepository(relative);
            if (fullPath is null || !File.Exists(fullPath))
            {
                deleted ??= relative;
                continue;
            }

            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
            if (!string.Equals(ReadString(source, "sha256"), actualHash, StringComparison.Ordinal))
                edited ??= relative;
        }

        if (deleted is not null)
            Reject("deleted-included-source", deleted);
        if (edited is not null)
            Reject("edited-included-source", edited);

        var added = new List<string>();
        foreach (var relative in EnumeratePolicySources(manifest))
        {
            if (!recorded.Contains(relative))
                added.Add(relative);
        }

        if (added.Count == 0)
            return;

        added.Sort(StringComparer.Ordinal);
        Reject("added-included-source", added[0]);
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Compares the current tool probe with the pre-run observation.</summary>
    /// <param name="manifest">The pre-run source manifest.</param>
    private void ValidateTools(JsonObject manifest)
    {
        var tools = manifest["tools"]?.AsArray();
        if (tools is null)
            return;

        foreach (var node in tools)
        {
            var tool = node!.AsObject();
            var name = ReadString(tool, "name");
            var observed = ReadString(tool, "observedValue");
            var current = _toolVersionProbe(name);
            if (string.Equals(current, observed, StringComparison.Ordinal))
                continue;

            var currentSplit = TrySplitObservation(current, out var currentPath, out var currentVersion, out var currentHash);
            var observedSplit = TrySplitObservation(observed, out var observedPath, out var observedVersion, out var observedHash);
            if (!currentSplit || !observedSplit)
                Reject("tool-path-drift", name);
            if (!string.Equals(currentPath, observedPath, StringComparison.Ordinal))
                Reject("tool-path-drift", name);
            if (!string.Equals(currentVersion, observedVersion, StringComparison.Ordinal))
                Reject("tool-version-drift", name);
            if (!string.Equals(currentHash, observedHash, StringComparison.Ordinal))
                Reject("tool-byte-hash-drift", name);
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Checks the Nuke inventory, report identity, and project TRX files.</summary>
    /// <param name="request">The run-scoped validation request.</param>
    /// <param name="lane">The canonical lane directory name.</param>
    /// <param name="resultsRoot">The absolute results root.</param>
    private void ValidateInventory(SessionLifeUnitGateValidationRequest request, string lane, string resultsRoot)
    {
        var inventoryToken = $"{lane}/selected-projects.json";
        var inventoryPath = CombineUnder(resultsRoot, inventoryToken);
        if (!File.Exists(inventoryPath))
            Reject("missing-inventory", inventoryToken);

        var inventory = ReadJsonObject(inventoryPath);
        if (!string.Equals(ReadString(inventory, "runId"), request.RunId, StringComparison.Ordinal))
            Reject("wrong-run-id", inventoryToken);
        if (!string.Equals(ReadString(inventory, "scope"), lane, StringComparison.Ordinal))
            Reject("scope-mismatch", inventoryToken);
        if (!TryParseTimestamp(ReadString(inventory, "generatedAtUtc"), out var generatedAt)
            || generatedAt < request.RunStartedAtUtc)
        {
            Reject("stale-inventory", inventoryToken);
        }

        var projects = inventory["projects"]?.AsArray() ?? new JsonArray();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var reports = new List<(string Name, string RelativePath, string AbsolutePath)>();
        foreach (var node in projects)
        {
            var project = node!.AsObject();
            var name = ReadString(project, "name");
            if (!seenNames.Add(name))
                Reject("duplicate-project", name);

            var relativePath = NormalizeRelative(ReadString(project, "reportPath"));
            var canonical = $"{lane}/{name}/{name}.trx";
            if (!string.Equals(relativePath, canonical, StringComparison.Ordinal))
                Reject("project-report-identity", $"{name}|{relativePath}");

            var absolutePath = CombineUnder(resultsRoot, relativePath);
            if (!File.Exists(absolutePath))
                Reject("missing-project-report", $"{name}|{relativePath}");

            reports.Add((name, relativePath, absolutePath));
        }

        RejectDuplicateReports(resultsRoot, lane, reports);
        foreach (var report in reports)
        {
            var defect = ClassifyTrx(report.AbsolutePath, request.RunStartedAtUtc);
            if (defect != TrxDefect.None)
                Reject(ProjectReportReason(defect), $"{report.Name}|{report.RelativePath}");
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Rejects a TRX that is present under the lane but not selected.</summary>
    /// <param name="resultsRoot">The absolute results root.</param>
    /// <param name="lane">The canonical lane directory name.</param>
    /// <param name="reports">The inventory reports already accepted as canonical.</param>
    private static void RejectDuplicateReports(
        string resultsRoot,
        string lane,
        List<(string Name, string RelativePath, string AbsolutePath)> reports)
    {
        var laneDirectory = Path.Combine(resultsRoot, lane);
        if (!Directory.Exists(laneDirectory))
            return;

        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var report in reports)
            expected.Add(report.RelativePath);

        var extras = new List<string>();
        foreach (var file in Directory.EnumerateFiles(laneDirectory, "*.trx", SearchOption.AllDirectories))
        {
            var relative = NormalizeRelative(Path.GetRelativePath(resultsRoot, file));
            if (!expected.Contains(relative))
                extras.Add(relative);
        }

        if (extras.Count == 0)
            return;

        extras.Sort(StringComparer.Ordinal);
        Reject("duplicate-report-file", extras[0]);
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Checks required command presence and exit codes for the lane.</summary>
    /// <param name="request">The run-scoped validation request.</param>
    /// <param name="lane">The canonical lane directory name.</param>
    /// <param name="resultsRoot">The absolute results root.</param>
    private static void ValidateCommandResults(
        SessionLifeUnitGateValidationRequest request,
        string lane,
        string resultsRoot)
    {
        var document = ReadJsonObject(CombineUnder(resultsRoot, CommandResultsToken));
        if (!string.Equals(ReadString(document, "runId"), request.RunId, StringComparison.Ordinal))
            Reject("wrong-run-command-results", CommandResultsToken);

        var commands = document["commands"]?.AsArray() ?? new JsonArray();
        foreach (var commandName in RequiredCommands(request.Scope))
        {
            var affected = $"{lane}:{commandName}";
            var command = FindCommand(commands, commandName);
            if (command is null)
                Reject("missing-required-command", affected);
            if (!command.ContainsKey("exitCode") || command["exitCode"] is null)
                Reject("missing-command-exit-code", affected);
            if (!TryReadJsonInt(command["exitCode"], out var exitCode))
                Reject("malformed-command-exit-code", affected);
            if (exitCode != 0)
                Reject("nonzero-command-exit", affected);
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Checks the unit-lane Pester XML report and native counters.</summary>
    /// <param name="request">The run-scoped validation request.</param>
    /// <param name="resultsRoot">The absolute results root.</param>
    private static void ValidatePester(SessionLifeUnitGateValidationRequest request, string resultsRoot)
    {
        var reportPath = CombineUnder(resultsRoot, PesterReportToken);
        if (!File.Exists(reportPath))
            Reject("missing-pester-report", PesterReportToken);

        var report = XDocument.Load(reportPath);
        var date = report.Root?.Attribute("date")?.Value;
        var time = report.Root?.Attribute("time")?.Value;
        if (!TryParsePesterClock(date, time, out var reportedAt)
            || reportedAt < request.RunStartedAtUtc
            || LastWriteUtc(reportPath) < request.RunStartedAtUtc)
        {
            Reject("stale-pester-report", PesterReportToken);
        }

        var nativePath = CombineUnder(resultsRoot, PesterNativeToken);
        if (!File.Exists(nativePath))
            Reject("missing-pester-native-counter", PesterNativeToken);

        var native = ReadJsonObject(nativePath);
        if (!TryParseTimestamp(ReadString(native, "ExecutedAt"), out var executedAt)
            || executedAt < request.RunStartedAtUtc
            || LastWriteUtc(nativePath) < request.RunStartedAtUtc)
        {
            Reject("stale-pester-native-result", PesterNativeToken);
        }

        foreach (var counterName in RequiredPesterCounters)
        {
            if (!TryReadJsonInt(native[counterName], out _))
                Reject("missing-pester-native-counter", PesterNativeToken);
        }

        foreach (var (name, reason) in PesterFailureCounters)
        {
            if (TryReadJsonInt(native[name], out var value) && value > 0)
                Reject(reason, PesterNativeToken);
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Checks the explicit unit-lane Build.Tests TRX.</summary>
    /// <param name="request">The run-scoped validation request.</param>
    /// <param name="resultsRoot">The absolute results root.</param>
    private static void ValidateBuildTests(SessionLifeUnitGateValidationRequest request, string resultsRoot)
    {
        var path = CombineUnder(resultsRoot, BuildTestsRelative);
        if (!File.Exists(path))
            Reject("missing-build-tests-report", BuildTestsToken);

        var defect = ClassifyTrx(path, request.RunStartedAtUtc);
        if (defect == TrxDefect.None)
            return;

        var reason = defect == TrxDefect.Stale
            ? "stale-build-tests-report"
            : "nonpassing-build-tests-report";
        Reject(reason, BuildTestsToken);
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Classifies one TRX report against the accepted counter and freshness rules.</summary>
    /// <param name="path">The absolute TRX path.</param>
    /// <param name="runStartedAtUtc">The UTC instant at which the run began.</param>
    /// <returns>The first defect, or <see cref="TrxDefect.None"/> when the report is accepted.</returns>
    private static TrxDefect ClassifyTrx(string path, DateTimeOffset runStartedAtUtc)
    {
        var document = XDocument.Load(path);
        var times = document.Root?.Element(TrxNamespace + "Times");
        var startText = times?.Attribute("start")?.Value;
        var finishText = times?.Attribute("finish")?.Value;
        if (!TryParseTimestamp(startText, out var start) || !TryParseTimestamp(finishText, out var finish))
            return TrxDefect.InvalidTimes;
        if (finish < start)
            return TrxDefect.InvalidTimes;
        if (start < runStartedAtUtc || finish < runStartedAtUtc || LastWriteUtc(path) < runStartedAtUtc)
            return TrxDefect.Stale;

        var summary = document.Root?.Element(TrxNamespace + "ResultSummary");
        if (!string.Equals(summary?.Attribute("outcome")?.Value, "Completed", StringComparison.Ordinal))
            return TrxDefect.IncompleteOutcome;

        var counters = summary?.Element(TrxNamespace + "Counters");
        if (counters is null)
            return TrxDefect.MissingCounter;

        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var counterName in RequiredTrxCounters)
        {
            var attribute = counters.Attribute(counterName);
            if (attribute is null)
                return TrxDefect.MissingCounter;
            if (!int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return TrxDefect.MalformedCounter;
            values[counterName] = parsed;
        }

        foreach (var value in values.Values)
        {
            if (value < 0)
                return TrxDefect.NegativeCounter;
        }

        var total = values["total"];
        var executed = values["executed"];
        var passed = values["passed"];
        var failed = values["failed"];
        var notExecuted = values["notExecuted"];
        if (passed + failed != executed || executed + notExecuted != total)
            return TrxDefect.Inconsistent;
        if (total == 0)
            return TrxDefect.ZeroDiscovered;
        if (failed > 0)
            return TrxDefect.Failed;
        if (notExecuted > 0)
            return TrxDefect.NotExecuted;

        var skipped = counters.Attribute("skipped");
        if (skipped is not null
            && int.TryParse(skipped.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var skippedCount)
            && skippedCount > 0)
        {
            return TrxDefect.OptionalSkipped;
        }

        foreach (var attribute in counters.Attributes())
        {
            if (IsRequiredTrxCounter(attribute.Name.LocalName) || attribute.Name.LocalName == "skipped")
                continue;
            if (int.TryParse(attribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var optional)
                && optional > 0)
            {
                return TrxDefect.OptionalAdverse;
            }
        }

        return TrxDefect.None;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Maps a project-report defect to its stable reason token.</summary>
    /// <param name="defect">The classified TRX defect.</param>
    /// <returns>The reason token.</returns>
    private static string ProjectReportReason(TrxDefect defect) =>
        defect switch
        {
            TrxDefect.Stale => "stale-project-report",
            TrxDefect.InvalidTimes => "invalid-trx-times",
            TrxDefect.IncompleteOutcome => "incomplete-trx-outcome",
            TrxDefect.MissingCounter => "missing-trx-counter",
            TrxDefect.MalformedCounter => "malformed-trx-counter",
            TrxDefect.NegativeCounter => "negative-trx-counter",
            TrxDefect.Inconsistent => "inconsistent-trx-counters",
            TrxDefect.ZeroDiscovered => "zero-discovered-tests",
            TrxDefect.Failed => "failed-trx-tests",
            TrxDefect.NotExecuted => "not-executed-trx-tests",
            TrxDefect.OptionalSkipped => "optional-skipped-trx-counter",
            TrxDefect.OptionalAdverse => "optional-adverse-trx-counter",
            _ => "incomplete-trx-outcome",
        };

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Returns the required command names for a lane, in execution order.</summary>
    /// <param name="scope">The lane whose commands are required.</param>
    /// <returns>The ordered command names.</returns>
    private static IReadOnlyList<string> RequiredCommands(SessionLifeUnitGateScope scope) =>
        scope == SessionLifeUnitGateScope.Unit
            ? new[] { "pester", "nuke-test", "build-tests" }
            : new[] { "nuke-provider" };

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Converts a lane enum to its canonical artifact directory name.</summary>
    /// <param name="scope">The lane value.</param>
    /// <returns>The lowercase lane name.</returns>
    private static string LaneName(SessionLifeUnitGateScope scope) =>
        scope == SessionLifeUnitGateScope.Unit ? "unit" : "provider";

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Enumerates policy roots and drops repository-relative exclusions.</summary>
    /// <param name="manifest">The pre-run source manifest.</param>
    /// <returns>Repository-relative included source paths.</returns>
    private IEnumerable<string> EnumeratePolicySources(JsonObject manifest)
    {
        var policy = manifest["sourcePolicy"]?.AsObject();
        var exclusions = ReadStringList(policy?["exclusions"]);
        foreach (var root in ReadStringList(policy?["roots"]))
        {
            var fullRoot = ResolveInsideRepository(NormalizeRelative(root));
            if (fullRoot is null)
                continue;

            if (File.Exists(fullRoot))
            {
                var fileRelative = NormalizeRelative(Path.GetRelativePath(_repositoryRoot, fullRoot));
                if (!IsExcluded(fileRelative, exclusions))
                    yield return fileRelative;
                continue;
            }

            if (!Directory.Exists(fullRoot))
                continue;

            foreach (var file in Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories))
            {
                var relative = NormalizeRelative(Path.GetRelativePath(_repositoryRoot, file));
                if (!IsExcluded(relative, exclusions))
                    yield return relative;
            }
        }
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Resolves a repository-relative path and refuses paths that escape the root.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <returns>The absolute path, or null when the path is rooted or escapes the repository.</returns>
    private string? ResolveInsideRepository(string relative)
    {
        if (Path.IsPathRooted(relative))
            return null;

        var combined = Path.GetFullPath(Path.Combine(
            _repositoryRoot,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _repositoryRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootPrefix, StringComparison.Ordinal)
            && !string.Equals(combined, _repositoryRoot, StringComparison.Ordinal))
        {
            return null;
        }

        return combined;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reports whether a repository-relative path matches a source exclusion.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="exclusions">The manifest exclusion globs.</param>
    /// <returns><see langword="true"/> when the path is excluded.</returns>
    private static bool IsExcluded(string relative, IReadOnlyList<string> exclusions)
    {
        foreach (var exclusion in exclusions)
        {
            if (GlobMatches(NormalizeRelative(exclusion), relative))
                return true;
        }

        return false;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Matches a slash-separated glob that may contain <c>**</c>.</summary>
    /// <param name="pattern">The exclusion pattern.</param>
    /// <param name="relativePath">The repository-relative path.</param>
    /// <returns><see langword="true"/> when the pattern matches the path.</returns>
    private static bool GlobMatches(string pattern, string relativePath) =>
        GlobMatches(SplitPath(pattern), 0, SplitPath(relativePath), 0);

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Matches one glob against one relative path from the given offsets.</summary>
    /// <param name="pattern">The pattern segments.</param>
    /// <param name="patternIndex">The current pattern segment.</param>
    /// <param name="path">The path segments.</param>
    /// <param name="pathIndex">The current path segment.</param>
    /// <returns><see langword="true"/> when the remainder matches.</returns>
    private static bool GlobMatches(string[] pattern, int patternIndex, string[] path, int pathIndex)
    {
        while (patternIndex < pattern.Length)
        {
            var token = pattern[patternIndex];
            if (token == "**")
            {
                if (patternIndex == pattern.Length - 1)
                    return true;

                for (var skip = pathIndex; skip <= path.Length; skip++)
                {
                    if (GlobMatches(pattern, patternIndex + 1, path, skip))
                        return true;
                }

                return false;
            }

            if (pathIndex >= path.Length || !string.Equals(token, path[pathIndex], StringComparison.Ordinal))
                return false;

            patternIndex++;
            pathIndex++;
        }

        return pathIndex == path.Length;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Splits a slash-normalized path into segments.</summary>
    /// <param name="value">The path or glob.</param>
    /// <returns>The non-empty segments.</returns>
    private static string[] SplitPath(string value) =>
        value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Normalizes a relative path to forward slashes.</summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The forward-slash path without a <c>./</c> prefix.</returns>
    private static string NormalizeRelative(string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal))
            normalized = normalized[2..];

        return normalized;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Combines an absolute root with a forward-slash relative path.</summary>
    /// <param name="root">The absolute root.</param>
    /// <param name="relative">The relative path.</param>
    /// <returns>The combined absolute path.</returns>
    private static string CombineUnder(string root, string relative) =>
        Path.Combine(root, NormalizeRelative(relative).Replace('/', Path.DirectorySeparatorChar));

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reads a JSON object artifact.</summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The parsed object.</returns>
    private static JsonObject ReadJsonObject(string path) =>
        JsonNode.Parse(File.ReadAllText(path))!.AsObject();

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reads a required string property.</summary>
    /// <param name="document">The object that owns the property.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The string value, or an empty string when the property is absent.</returns>
    private static string ReadString(JsonObject document, string name) =>
        document[name]?.GetValue<string>() ?? string.Empty;

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reads a JSON array of strings.</summary>
    /// <param name="node">The array node.</param>
    /// <returns>The string values.</returns>
    private static IReadOnlyList<string> ReadStringList(JsonNode? node)
    {
        if (node is not JsonArray array)
            return Array.Empty<string>();

        var values = new List<string>(array.Count);
        foreach (var item in array)
        {
            if (item is not null)
                values.Add(item.GetValue<string>());
        }

        return values;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Finds a command object by its ordinal name.</summary>
    /// <param name="commands">The command array.</param>
    /// <param name="commandName">The command name.</param>
    /// <returns>The command object, or null when the command is absent.</returns>
    private static JsonObject? FindCommand(JsonArray commands, string commandName)
    {
        foreach (var node in commands)
        {
            var command = node!.AsObject();
            if (string.Equals(ReadString(command, "name"), commandName, StringComparison.Ordinal))
                return command;
        }

        return null;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reads a JSON integer stored as a number.</summary>
    /// <param name="node">The JSON value.</param>
    /// <param name="value">The parsed integer.</param>
    /// <returns><see langword="true"/> when the node is an integer number.</returns>
    private static bool TryReadJsonInt(JsonNode? node, out int value)
    {
        value = 0;
        if (node is not JsonValue json)
            return false;
        if (json.TryGetValue(out int intValue))
        {
            value = intValue;
            return true;
        }

        if (json.TryGetValue(out long longValue) && longValue is >= int.MinValue and <= int.MaxValue)
        {
            value = (int)longValue;
            return true;
        }

        return false;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Splits a tool observation into path, version, and byte hash.</summary>
    /// <param name="observation">The <c>path|version|hash</c> observation.</param>
    /// <param name="path">The tool path.</param>
    /// <param name="version">The tool version.</param>
    /// <param name="hash">The executable byte hash.</param>
    /// <returns><see langword="true"/> when the observation has all three parts.</returns>
    private static bool TrySplitObservation(
        string observation,
        out string path,
        out string version,
        out string hash)
    {
        var last = observation.LastIndexOf('|');
        var second = last > 0 ? observation.LastIndexOf('|', last - 1) : -1;
        if (second <= 0 || last <= second)
        {
            path = string.Empty;
            version = string.Empty;
            hash = string.Empty;
            return false;
        }

        path = observation[..second];
        version = observation[(second + 1)..last];
        hash = observation[(last + 1)..];
        return path.Length > 0 && version.Length > 0 && hash.Length > 0;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Parses a round-trip timestamp.</summary>
    /// <param name="text">The timestamp text.</param>
    /// <param name="timestamp">The parsed timestamp.</param>
    /// <returns><see langword="true"/> when the text is a timestamp.</returns>
    private static bool TryParseTimestamp(string? text, out DateTimeOffset timestamp) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Parses Pester NUnit date and time attributes as UTC.</summary>
    /// <param name="date">The <c>yyyy-MM-dd</c> date.</param>
    /// <param name="time">The <c>HH:mm:ss</c> time.</param>
    /// <param name="timestamp">The parsed UTC timestamp.</param>
    /// <returns><see langword="true"/> when both attributes parse.</returns>
    private static bool TryParsePesterClock(string? date, string? time, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (date is null || time is null)
            return false;
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return false;
        if (!TimeSpan.TryParseExact(time, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var clock))
            return false;

        timestamp = new DateTimeOffset(day.Year, day.Month, day.Day, clock.Hours, clock.Minutes, clock.Seconds, TimeSpan.Zero);
        return true;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reads the UTC last-write time of an artifact.</summary>
    /// <param name="path">The artifact path.</param>
    /// <returns>The UTC last-write time.</returns>
    private static DateTimeOffset LastWriteUtc(string path) =>
        new(File.GetLastWriteTimeUtc(path));

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Reports whether a counter name is one of the plan-backed required counters.</summary>
    /// <param name="name">The counter attribute name.</param>
    /// <returns><see langword="true"/> when the counter is required.</returns>
    private static bool IsRequiredTrxCounter(string name)
    {
        foreach (var required in RequiredTrxCounters)
        {
            if (string.Equals(required, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>FR-MCP-107 / TR-MCP-PLAN-001: Throws the classified rejection for one accepted fixture.</summary>
    /// <param name="reason">The stable reason token.</param>
    /// <param name="affected">The affected artifact, project, or command token.</param>
    [DoesNotReturn]
    private static void Reject(string reason, string affected) =>
        throw new InvalidDataException(
            $"Session-life gate rejected the run. reason={reason} affected={affected}");
}
