using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001 / TEST-MCP-143 physical-artifact tests for
/// the machine-readable session-life gate validator.
/// </summary>
public sealed class SessionLifeUnitGateValidatorTests
{
    /// <summary>
    /// Gets invalid-artifact mutations shared by both unit and provider lanes.
    /// </summary>
    public static TheoryData<SessionLifeInvalidFixture> SharedInvalidFixtures => new()
    {
        SessionLifeInvalidFixture.MissingInventory,
        SessionLifeInvalidFixture.MissingProjectReport,
        SessionLifeInvalidFixture.DuplicateProject,
        SessionLifeInvalidFixture.DuplicateReportFile,
        SessionLifeInvalidFixture.WrongRunId,
        SessionLifeInvalidFixture.ScopeMismatch,
        SessionLifeInvalidFixture.StaleInventory,
        SessionLifeInvalidFixture.StaleProjectReport,
        SessionLifeInvalidFixture.ProjectReportMismatch,
        SessionLifeInvalidFixture.MissingTrxCounter,
        SessionLifeInvalidFixture.MalformedTrxCounter,
        SessionLifeInvalidFixture.NegativeTrxCounter,
        SessionLifeInvalidFixture.InconsistentTrxCounters,
        SessionLifeInvalidFixture.ZeroDiscoveredTests,
        SessionLifeInvalidFixture.FailedTrxTests,
        SessionLifeInvalidFixture.NotExecutedTrxTests,
        SessionLifeInvalidFixture.OptionalSkippedTrxTests,
        SessionLifeInvalidFixture.OptionalAdverseTrxTests,
        SessionLifeInvalidFixture.InvalidTrxTimes,
        SessionLifeInvalidFixture.IncompleteTrxOutcome,
        SessionLifeInvalidFixture.MissingRequiredCommand,
        SessionLifeInvalidFixture.NonZeroCommandExit,
        SessionLifeInvalidFixture.EditedIncludedSource,
        SessionLifeInvalidFixture.AddedIncludedSource,
        SessionLifeInvalidFixture.DeletedIncludedSource,
        SessionLifeInvalidFixture.ToolPathDrift,
        SessionLifeInvalidFixture.ToolVersionDrift,
        SessionLifeInvalidFixture.ToolByteHashDrift,
    };

    /// <summary>
    /// Gets invalid-artifact mutations that apply only to the cumulative unit lane.
    /// </summary>
    public static TheoryData<SessionLifeInvalidFixture> UnitOnlyInvalidFixtures => new()
    {
        SessionLifeInvalidFixture.MissingPesterReport,
        SessionLifeInvalidFixture.MissingPesterNativeCounter,
        SessionLifeInvalidFixture.FailedPesterTests,
        SessionLifeInvalidFixture.SkippedPesterTests,
        SessionLifeInvalidFixture.InconclusivePesterTests,
        SessionLifeInvalidFixture.NotRunPesterTests,
        SessionLifeInvalidFixture.FailedPesterBlocks,
        SessionLifeInvalidFixture.FailedPesterContainers,
        SessionLifeInvalidFixture.MissingBuildTestsReport,
        SessionLifeInvalidFixture.NonPassingBuildTestsReport,
    };

    /// <summary>
    /// Requires a valid unit lane to succeed without any provider artifacts.
    /// </summary>
    [Fact]
    public void Validate_ValidUnitArtifacts_DoNotRequireProviderLane()
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Unit);
        Assert.False(Directory.Exists(Path.Combine(fixture.ResultsRoot, "provider")));

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));

        Assert.Null(exception);
    }

    /// <summary>
    /// Requires a valid provider lane to succeed without unit, Pester, or explicit Build.Tests artifacts.
    /// </summary>
    [Fact]
    public void Validate_ValidProviderArtifacts_DoNotRequireUnitLane()
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Provider);
        Assert.False(Directory.Exists(Path.Combine(fixture.ResultsRoot, "unit")));
        Assert.False(Directory.Exists(Path.Combine(fixture.ResultsRoot, "pester")));
        Assert.False(Directory.Exists(Path.Combine(fixture.ResultsRoot, "build-tests")));

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));

        Assert.Null(exception);
    }

    /// <summary>
    /// Requires report output beneath the excluded run root not to invalidate source identity.
    /// </summary>
    [Fact]
    public void Validate_ReportWrittenAfterManifest_DoesNotCauseSourceDrift()
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Unit);
        fixture.WriteReportOnlyArtifact();

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));

        Assert.Null(exception);
    }

    /// <summary>
    /// Requires every shared invalid mutation to be rejected in the unit lane.
    /// </summary>
    /// <param name="invalidFixture">The single invalid mutation applied to a valid unit baseline.</param>
    [Theory]
    [MemberData(nameof(SharedInvalidFixtures))]
    public void Validate_SharedInvalidArtifact_IsRejectedInUnitLane(
        SessionLifeInvalidFixture invalidFixture)
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Unit);
        AssertImplementedRejection(fixture, invalidFixture);
    }

    /// <summary>
    /// Requires every shared invalid mutation to be rejected in the provider lane.
    /// </summary>
    /// <param name="invalidFixture">The single invalid mutation applied to a valid provider baseline.</param>
    [Theory]
    [MemberData(nameof(SharedInvalidFixtures))]
    public void Validate_SharedInvalidArtifact_IsRejectedInProviderLane(
        SessionLifeInvalidFixture invalidFixture)
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Provider);
        AssertImplementedRejection(fixture, invalidFixture);
    }

    /// <summary>
    /// Requires unit-only Pester and explicit Build.Tests mutations to be rejected.
    /// </summary>
    /// <param name="invalidFixture">The unit-only invalid mutation.</param>
    [Theory]
    [MemberData(nameof(UnitOnlyInvalidFixtures))]
    public void Validate_UnitOnlyInvalidArtifact_IsRejected(
        SessionLifeInvalidFixture invalidFixture)
    {
        using var fixture = SessionLifeGateFixture.Create(SessionLifeUnitGateScope.Unit);
        AssertImplementedRejection(fixture, invalidFixture);
    }

    /// <summary>
    /// Requires rejection to come from implemented validation rather than the Red shell.
    /// </summary>
    /// <param name="fixture">The valid baseline fixture.</param>
    /// <param name="invalidFixture">The single mutation to apply.</param>
    private static void AssertImplementedRejection(
        SessionLifeGateFixture fixture,
        SessionLifeInvalidFixture invalidFixture)
    {
        fixture.Apply(invalidFixture);
        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));
        Assert.NotNull(exception);
        Assert.IsNotType<NotImplementedException>(exception);
    }

    /// <summary>
    /// Creates and mutates one real on-disk gate fixture without implementing validation logic.
    /// </summary>
    private sealed class SessionLifeGateFixture : IDisposable
    {
        /// <summary>The stable run identifier used by the fixture.</summary>
        private const string RunId = "sessionlife-red-001";

        /// <summary>The observed Visual Studio Test 2010 TRX namespace.</summary>
        private static readonly XNamespace TrxNamespace =
            "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        /// <summary>The deterministic run start used for freshness checks.</summary>
        private static readonly DateTimeOffset RunStart =
            new(2026, 9, 28, 7, 30, 0, TimeSpan.Zero);

        /// <summary>JSON options used to persist readable fixture artifacts.</summary>
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>Repository-relative source files represented in the pre-run manifest.</summary>
        private readonly string[] _sourceRelativePaths =
        {
            "build/Build.Test.cs",
            "src/McpServer.Services/SessionLogService.cs",
            "tests/Build.Tests/SessionLifeUnitGateValidatorTests.cs",
            "plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1",
        };

        /// <summary>Current executable paths returned by the injected tool probe.</summary>
        private readonly Dictionary<string, string> _toolPaths = new(StringComparer.Ordinal);

        /// <summary>Current versions returned by the injected tool probe.</summary>
        private readonly Dictionary<string, string> _toolVersions = new(StringComparer.Ordinal);

        /// <summary>Project names materialized into the authoritative lane inventory.</summary>
        private readonly string[] _projects;
        /// <summary>Absolute path to the lane inventory.</summary>
        private readonly string _inventoryPath;
        /// <summary>Absolute paths to project TRX reports named by the lane inventory.</summary>
        private readonly string[] _projectReportPaths;
        /// <summary>Absolute path to the Pester NUnit-compatible report.</summary>
        private readonly string _pesterReportPath;
        /// <summary>Absolute path to the Pester native counter report.</summary>
        private readonly string _pesterNativePath;
        /// <summary>Absolute path to the fixed explicit Build.Tests TRX.</summary>
        private readonly string _buildTestsReportPath;
        /// <summary>Absolute path to required command outcomes.</summary>
        private readonly string _commandResultsPath;

        /// <summary>
        /// Initializes a complete valid fixture for one independent validation lane.
        /// </summary>
        /// <param name="scope">The lane to materialize.</param>
        private SessionLifeGateFixture(SessionLifeUnitGateScope scope)
        {
            Scope = scope;
            OwnedRoot = Path.Combine(
                Path.GetTempPath(),
                "McpServer-SessionLifeUnitGate",
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            RepositoryRoot = Path.Combine(OwnedRoot, "repository");
            ToolRoot = Path.Combine(OwnedRoot, "tools");
            ResultsRoot = Path.Combine(RepositoryRoot, "TestResults", RunId);
            SourceManifestPath = Path.Combine(ResultsRoot, "source-manifest.json");
            _projects = scope == SessionLifeUnitGateScope.Unit
                ? new[] { "McpServer.Client.Tests", "McpServer.Services.Tests" }
                : new[] { "McpServer.Support.Mcp.Tests", "Build.Tests" };

            var laneName = GetLaneName(scope);
            _inventoryPath = Path.Combine(ResultsRoot, laneName, "selected-projects.json");
            _projectReportPaths = _projects
                .Select(project => Path.Combine(ResultsRoot, laneName, project, $"{project}.trx"))
                .ToArray();
            _pesterReportPath = Path.Combine(ResultsRoot, "pester", "results.xml");
            _pesterNativePath = Path.Combine(ResultsRoot, "pester", "native-run.json");
            _buildTestsReportPath = Path.Combine(ResultsRoot, "build-tests", "Build.Tests.trx");
            _commandResultsPath = Path.Combine(ResultsRoot, "command-results.json");

            Directory.CreateDirectory(RepositoryRoot);
            _toolPaths["dotnet"] = Path.Combine(ToolRoot, "dotnet.exe");
            _toolPaths["pwsh"] = Path.Combine(ToolRoot, "pwsh.exe");
            _toolVersions["dotnet"] = "10.0.401";
            _toolVersions["pwsh"] = "7.6.6";
            WriteBytes(_toolPaths["dotnet"], "fixture dotnet executable bytes\n");
            WriteBytes(_toolPaths["pwsh"], "fixture pwsh executable bytes\n");
            WriteSources();
            WriteSourceManifest();
            WriteInventory();
            foreach (var reportPath in _projectReportPaths)
                WritePassingTrx(reportPath);

            if (scope == SessionLifeUnitGateScope.Unit)
            {
                WritePesterArtifacts();
                WritePassingTrx(_buildTestsReportPath);
            }

            WriteCommandResults();
            Request = new SessionLifeUnitGateValidationRequest
            {
                Scope = scope,
                RunId = RunId,
                ResultsRoot = ResultsRoot,
                RunStartedAtUtc = RunStart,
                SourceManifestPath = SourceManifestPath,
            };
            Validator = new SessionLifeUnitGateValidator(
                RepositoryRoot,
                ProbeTool);
        }

        /// <summary>Gets the lane represented by this fixture.</summary>
        private SessionLifeUnitGateScope Scope { get; }
        /// <summary>Gets the GUID-owned parent containing sibling repository and tool roots.</summary>
        private string OwnedRoot { get; }
        /// <summary>Gets the tool root outside the synthetic repository source inventory.</summary>
        private string ToolRoot { get; }
        /// <summary>Gets the fixture repository root.</summary>
        internal string RepositoryRoot { get; }
        /// <summary>Gets the run-scoped results root.</summary>
        internal string ResultsRoot { get; }
        /// <summary>Gets the pre-run source and tool manifest path.</summary>
        internal string SourceManifestPath { get; }
        /// <summary>Gets the concrete production validator shell.</summary>
        internal SessionLifeUnitGateValidator Validator { get; }
        /// <summary>Gets the run-scoped validation request.</summary>
        internal SessionLifeUnitGateValidationRequest Request { get; }

        /// <summary>Creates a valid physical fixture for the requested lane.</summary>
        /// <param name="scope">The independent lane to create.</param>
        /// <returns>A disposable real-artifact fixture.</returns>
        internal static SessionLifeGateFixture Create(SessionLifeUnitGateScope scope) => new(scope);

        /// <summary>Applies one invalid mutation to the valid unit baseline.</summary>
        /// <param name="invalidFixture">The mutation to apply.</param>
        internal void Apply(SessionLifeInvalidFixture invalidFixture)
        {
            switch (invalidFixture)
            {
                case SessionLifeInvalidFixture.MissingInventory:
                    File.Delete(_inventoryPath);
                    break;
                case SessionLifeInvalidFixture.MissingProjectReport:
                    File.Delete(_projectReportPaths[0]);
                    break;
                case SessionLifeInvalidFixture.DuplicateProject:
                    MutateInventory(inventory =>
                    {
                        var projects = inventory["projects"]!.AsArray();
                        projects.Add(projects[0]!.DeepClone());
                    });
                    break;
                case SessionLifeInvalidFixture.DuplicateReportFile:
                    File.Copy(
                        _projectReportPaths[0],
                        Path.Combine(Path.GetDirectoryName(_projectReportPaths[0])!, "duplicate.trx"));
                    break;
                case SessionLifeInvalidFixture.WrongRunId:
                    MutateInventory(inventory => inventory["runId"] = "different-run");
                    break;
                case SessionLifeInvalidFixture.ScopeMismatch:
                    MutateInventory(inventory =>
                        inventory["scope"] = Scope == SessionLifeUnitGateScope.Unit ? "provider" : "unit");
                    break;
                case SessionLifeInvalidFixture.StaleInventory:
                    MutateInventory(inventory =>
                        inventory["generatedAtUtc"] = RunStart.AddMinutes(-1).ToString("O", CultureInfo.InvariantCulture));
                    break;
                case SessionLifeInvalidFixture.StaleProjectReport:
                    WritePassingTrx(_projectReportPaths[0], RunStart.AddMinutes(-2), RunStart.AddMinutes(-1));
                    File.SetLastWriteTimeUtc(_projectReportPaths[0], RunStart.AddMinutes(-1).UtcDateTime);
                    break;
                case SessionLifeInvalidFixture.ProjectReportMismatch:
                    MutateInventory(inventory =>
                        inventory["projects"]![0]!["reportPath"] = $"{GetLaneName(Scope)}/Wrong.Tests/Wrong.Tests.trx");
                    break;
                case SessionLifeInvalidFixture.MissingTrxCounter:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.Attribute("executed")!.Remove());
                    break;
                case SessionLifeInvalidFixture.MalformedTrxCounter:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.SetAttributeValue("total", "two"));
                    break;
                case SessionLifeInvalidFixture.NegativeTrxCounter:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.SetAttributeValue("failed", "-1"));
                    break;
                case SessionLifeInvalidFixture.InconsistentTrxCounters:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.SetAttributeValue("passed", "1"));
                    break;
                case SessionLifeInvalidFixture.ZeroDiscoveredTests:
                    SetRequiredTrxCounters(_projectReportPaths[0], 0, 0, 0, 0, 0);
                    break;
                case SessionLifeInvalidFixture.FailedTrxTests:
                    SetRequiredTrxCounters(_projectReportPaths[0], 2, 2, 1, 1, 0);
                    break;
                case SessionLifeInvalidFixture.NotExecutedTrxTests:
                    SetRequiredTrxCounters(_projectReportPaths[0], 2, 1, 1, 0, 1);
                    break;
                case SessionLifeInvalidFixture.OptionalSkippedTrxTests:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.SetAttributeValue("skipped", "1"));
                    break;
                case SessionLifeInvalidFixture.OptionalAdverseTrxTests:
                    MutateTrxCounters(_projectReportPaths[0], counters => counters.SetAttributeValue("error", "1"));
                    break;
                case SessionLifeInvalidFixture.InvalidTrxTimes:
                    MutateTrx(_projectReportPaths[0], document =>
                    {
                        var times = document.Root!.Element(TrxNamespace + "Times")!;
                        times.SetAttributeValue("start", RunStart.AddMinutes(2).ToString("O", CultureInfo.InvariantCulture));
                        times.SetAttributeValue("finish", RunStart.AddMinutes(1).ToString("O", CultureInfo.InvariantCulture));
                    });
                    break;
                case SessionLifeInvalidFixture.IncompleteTrxOutcome:
                    MutateTrx(_projectReportPaths[0], document =>
                        document.Root!.Element(TrxNamespace + "ResultSummary")!.SetAttributeValue("outcome", "InProgress"));
                    break;
                case SessionLifeInvalidFixture.MissingPesterReport:
                    File.Delete(_pesterReportPath);
                    break;
                case SessionLifeInvalidFixture.MissingPesterNativeCounter:
                    MutateJson(_pesterNativePath, document => document.Remove("FailedBlocks"));
                    break;
                case SessionLifeInvalidFixture.FailedPesterTests:
                    MutatePesterCounter("Failed", 1);
                    break;
                case SessionLifeInvalidFixture.SkippedPesterTests:
                    MutatePesterCounter("Skipped", 1);
                    break;
                case SessionLifeInvalidFixture.InconclusivePesterTests:
                    MutatePesterCounter("Inconclusive", 1);
                    break;
                case SessionLifeInvalidFixture.NotRunPesterTests:
                    MutatePesterCounter("NotRun", 1);
                    break;
                case SessionLifeInvalidFixture.FailedPesterBlocks:
                    MutatePesterCounter("FailedBlocks", 1);
                    break;
                case SessionLifeInvalidFixture.FailedPesterContainers:
                    MutatePesterCounter("FailedContainers", 1);
                    break;
                case SessionLifeInvalidFixture.MissingBuildTestsReport:
                    File.Delete(_buildTestsReportPath);
                    break;
                case SessionLifeInvalidFixture.NonPassingBuildTestsReport:
                    SetRequiredTrxCounters(_buildTestsReportPath, 2, 2, 1, 1, 0);
                    break;
                case SessionLifeInvalidFixture.MissingRequiredCommand:
                    MutateJson(_commandResultsPath, document => document["commands"]!.AsArray().RemoveAt(0));
                    break;
                case SessionLifeInvalidFixture.NonZeroCommandExit:
                    MutateJson(_commandResultsPath, document => document["commands"]![0]!["exitCode"] = 1);
                    break;
                case SessionLifeInvalidFixture.EditedIncludedSource:
                    WriteBytes(GetSourcePath(_sourceRelativePaths[0]), "edited source\r\n");
                    break;
                case SessionLifeInvalidFixture.AddedIncludedSource:
                    WriteBytes(Path.Combine(RepositoryRoot, "src", "AddedAfterManifest.cs"), "internal sealed class AddedAfterManifest { }\n");
                    break;
                case SessionLifeInvalidFixture.DeletedIncludedSource:
                    File.Delete(GetSourcePath(_sourceRelativePaths[1]));
                    break;
                case SessionLifeInvalidFixture.ToolPathDrift:
                    _toolPaths["dotnet"] = Path.Combine(ToolRoot, "alternate-dotnet.exe");
                    WriteBytes(_toolPaths["dotnet"], "fixture dotnet executable bytes\n");
                    break;
                case SessionLifeInvalidFixture.ToolVersionDrift:
                    _toolVersions["dotnet"] = "10.0.999";
                    break;
                case SessionLifeInvalidFixture.ToolByteHashDrift:
                    WriteBytes(_toolPaths["dotnet"], "changed dotnet executable bytes\n");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(invalidFixture), invalidFixture, "Unknown fixture mutation.");
            }
        }

        /// <summary>Writes an extra report beneath the excluded results root after manifest capture.</summary>
        internal void WriteReportOnlyArtifact()
        {
            WriteBytes(Path.Combine(ResultsRoot, "diagnostics", "post-manifest.log"), "report output only\n");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(OwnedRoot))
                Directory.Delete(OwnedRoot, recursive: true);
        }

        /// <summary>Converts a lane enum to its canonical artifact directory name.</summary>
        /// <param name="scope">The lane value.</param>
        /// <returns>The lowercase lane name.</returns>
        private static string GetLaneName(SessionLifeUnitGateScope scope) =>
            scope == SessionLifeUnitGateScope.Unit ? "unit" : "provider";

        /// <summary>Writes deterministic source bytes before the source manifest is captured.</summary>
        private void WriteSources()
        {
            foreach (var relativePath in _sourceRelativePaths)
                WriteBytes(GetSourcePath(relativePath), $"fixture source: {relativePath}\r\n");
        }

        /// <summary>Resolves a slash-normalized repository-relative fixture source path.</summary>
        /// <param name="relativePath">The repository-relative source path.</param>
        /// <returns>The absolute source path beneath the fixture repository.</returns>
        private string GetSourcePath(string relativePath) =>
            Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>Writes the canonical pre-run source/tool manifest from exact source bytes.</summary>
        private void WriteSourceManifest()
        {
            var sources = new JsonArray();
            foreach (var relativePath in _sourceRelativePaths)
            {
                var bytes = File.ReadAllBytes(GetSourcePath(relativePath));
                sources.Add(new JsonObject
                {
                    ["path"] = relativePath,
                    ["length"] = bytes.LongLength,
                    ["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)),
                    ["provenance"] = "tracked-or-relevant-untracked",
                });
            }

            var tools = new JsonArray();
            foreach (var toolName in _toolPaths.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                tools.Add(new JsonObject
                {
                    ["name"] = toolName,
                    ["observedValue"] = ProbeTool(toolName),
                });
            }

            WriteJson(SourceManifestPath, new JsonObject
            {
                ["schemaVersion"] = 1,
                ["runId"] = RunId,
                ["capturedAtUtc"] = RunStart.AddSeconds(-1).ToString("O", CultureInfo.InvariantCulture),
                ["sourcePolicy"] = new JsonObject
                {
                    ["mode"] = "git-tracked-plus-relevant-untracked",
                    ["roots"] = new JsonArray("build", "src", "tests", "plugins/core/test-fixtures/pester"),
                    ["exclusions"] = new JsonArray(
                        "TestResults/**",
                        "docs/receipts/sessionlife-completion/**/test-results/**"),
                },
                ["sources"] = sources,
                ["tools"] = tools,
            });
        }

        /// <summary>Returns the current path, version, and executable-byte hash for a fixed tool.</summary>
        /// <param name="toolName">The tool name.</param>
        /// <returns>The canonical current tool observation.</returns>
        private string ProbeTool(string toolName)
        {
            var path = _toolPaths[toolName];
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            return $"{path}|{_toolVersions[toolName]}|{hash}";
        }

        /// <summary>Writes the authoritative Nuke-produced inventory for the selected lane.</summary>
        private void WriteInventory()
        {
            var laneName = GetLaneName(Scope);
            var projects = new JsonArray();
            foreach (var project in _projects)
            {
                projects.Add(new JsonObject
                {
                    ["name"] = project,
                    ["projectPath"] = $"tests/{project}/{project}.csproj",
                    ["reportPath"] = $"{laneName}/{project}/{project}.trx",
                });
            }

            WriteJson(_inventoryPath, new JsonObject
            {
                ["schemaVersion"] = 1,
                ["runId"] = RunId,
                ["scope"] = laneName,
                ["generatedAtUtc"] = RunStart.AddSeconds(1).ToString("O", CultureInfo.InvariantCulture),
                ["projects"] = projects,
            });
        }

        /// <summary>Writes valid Pester NUnit-compatible and native counter artifacts.</summary>
        private void WritePesterArtifacts()
        {
            WriteXml(
                _pesterReportPath,
                new XDocument(
                    new XElement(
                        "test-results",
                        new XAttribute("total", 2),
                        new XAttribute("failures", 0),
                        new XAttribute("not-run", 0),
                        new XAttribute("inconclusive", 0),
                        new XAttribute("skipped", 0))));
            WriteJson(_pesterNativePath, new JsonObject
            {
                ["Total"] = 2,
                ["Passed"] = 2,
                ["Failed"] = 0,
                ["Skipped"] = 0,
                ["Inconclusive"] = 0,
                ["NotRun"] = 0,
                ["FailedBlocks"] = 0,
                ["FailedContainers"] = 0,
            });
        }

        /// <summary>Writes the required lane-specific command outcomes.</summary>
        private void WriteCommandResults()
        {
            var commands = new JsonArray();
            var names = Scope == SessionLifeUnitGateScope.Unit
                ? new[] { "pester", "nuke-test", "build-tests" }
                : new[] { "nuke-provider" };
            foreach (var name in names)
            {
                commands.Add(new JsonObject
                {
                    ["name"] = name,
                    ["exitCode"] = 0,
                    ["startedAtUtc"] = RunStart.ToString("O", CultureInfo.InvariantCulture),
                    ["finishedAtUtc"] = RunStart.AddSeconds(10).ToString("O", CultureInfo.InvariantCulture),
                });
            }

            WriteJson(_commandResultsPath, new JsonObject
            {
                ["schemaVersion"] = 1,
                ["runId"] = RunId,
                ["scope"] = GetLaneName(Scope),
                ["commands"] = commands,
            });
        }

        /// <summary>Writes a passing TRX report with only the plan-backed required counters.</summary>
        /// <param name="path">The report path.</param>
        /// <param name="start">Optional report start.</param>
        /// <param name="finish">Optional report finish.</param>
        private static void WritePassingTrx(
            string path,
            DateTimeOffset? start = null,
            DateTimeOffset? finish = null)
        {
            var document = new XDocument(
                new XElement(
                    TrxNamespace + "TestRun",
                    new XAttribute("id", "11111111-1111-1111-1111-111111111111"),
                    new XElement(
                        TrxNamespace + "Times",
                        new XAttribute("start", (start ?? RunStart.AddSeconds(2)).ToString("O", CultureInfo.InvariantCulture)),
                        new XAttribute("finish", (finish ?? RunStart.AddSeconds(8)).ToString("O", CultureInfo.InvariantCulture))),
                    new XElement(
                        TrxNamespace + "ResultSummary",
                        new XAttribute("outcome", "Completed"),
                        new XElement(
                            TrxNamespace + "Counters",
                            new XAttribute("total", 2),
                            new XAttribute("executed", 2),
                            new XAttribute("passed", 2),
                            new XAttribute("failed", 0),
                            new XAttribute("notExecuted", 0)))));
            WriteXml(path, document);
        }

        /// <summary>Replaces the plan-backed required counters in a TRX report.</summary>
        /// <param name="path">The report path.</param>
        /// <param name="total">The total counter.</param>
        /// <param name="executed">The executed counter.</param>
        /// <param name="passed">The passed counter.</param>
        /// <param name="failed">The failed counter.</param>
        /// <param name="notExecuted">The not-executed counter.</param>
        private static void SetRequiredTrxCounters(
            string path,
            int total,
            int executed,
            int passed,
            int failed,
            int notExecuted)
        {
            MutateTrxCounters(path, counters =>
            {
                counters.SetAttributeValue("total", total);
                counters.SetAttributeValue("executed", executed);
                counters.SetAttributeValue("passed", passed);
                counters.SetAttributeValue("failed", failed);
                counters.SetAttributeValue("notExecuted", notExecuted);
            });
        }

        /// <summary>Mutates the required-counter element of an existing TRX report.</summary>
        /// <param name="path">The report path.</param>
        /// <param name="mutation">The counter mutation.</param>
        private static void MutateTrxCounters(string path, Action<XElement> mutation)
        {
            MutateTrx(
                path,
                document => mutation(
                    document.Root!
                        .Element(TrxNamespace + "ResultSummary")!
                        .Element(TrxNamespace + "Counters")!));
        }

        /// <summary>Mutates and persists an existing TRX document.</summary>
        /// <param name="path">The report path.</param>
        /// <param name="mutation">The document mutation.</param>
        private static void MutateTrx(string path, Action<XDocument> mutation)
        {
            var document = XDocument.Load(path);
            mutation(document);
            WriteXml(path, document);
        }

        /// <summary>Mutates and persists the lane inventory.</summary>
        /// <param name="mutation">The inventory mutation.</param>
        private void MutateInventory(Action<JsonObject> mutation) => MutateJson(_inventoryPath, mutation);

        /// <summary>Replaces one Pester native counter.</summary>
        /// <param name="counterName">The counter name.</param>
        /// <param name="value">The replacement value.</param>
        private void MutatePesterCounter(string counterName, int value) =>
            MutateJson(_pesterNativePath, document => document[counterName] = value);

        /// <summary>Mutates and persists a JSON object artifact.</summary>
        /// <param name="path">The JSON path.</param>
        /// <param name="mutation">The object mutation.</param>
        private static void MutateJson(string path, Action<JsonObject> mutation)
        {
            var document = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!.AsObject();
            mutation(document);
            WriteJson(path, document);
        }

        /// <summary>Serializes a JSON node as UTF-8 without a byte-order mark.</summary>
        /// <param name="path">The JSON path.</param>
        /// <param name="document">The JSON node.</param>
        private static void WriteJson(string path, JsonNode document) =>
            WriteBytes(path, document.ToJsonString(JsonOptions));

        /// <summary>Serializes an XML document as UTF-8 without a byte-order mark.</summary>
        /// <param name="path">The XML path.</param>
        /// <param name="document">The XML document.</param>
        private static void WriteXml(string path, XDocument document) =>
            WriteBytes(path, document.ToString(SaveOptions.DisableFormatting));

        /// <summary>Writes exact UTF-8 bytes after creating the containing directory.</summary>
        /// <param name="path">The destination path.</param>
        /// <param name="content">The text encoded as exact UTF-8 bytes.</param>
        private static void WriteBytes(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content));
        }
    }
}

/// <summary>
/// Names each approved single-mutation rejection fixture for the P1 behavior Red.
/// </summary>
public enum SessionLifeInvalidFixture
{
    /// <summary>The Nuke-selected project inventory is absent.</summary>
    MissingInventory,
    /// <summary>A project named by the inventory has no report.</summary>
    MissingProjectReport,
    /// <summary>The inventory names the same project more than once.</summary>
    DuplicateProject,
    /// <summary>The lane contains an extra duplicate TRX report.</summary>
    DuplicateReportFile,
    /// <summary>The inventory is bound to another run identifier.</summary>
    WrongRunId,
    /// <summary>The inventory is bound to the wrong validation lane.</summary>
    ScopeMismatch,
    /// <summary>The inventory predates the requested run.</summary>
    StaleInventory,
    /// <summary>A project report predates the requested run.</summary>
    StaleProjectReport,
    /// <summary>The inventory report path disagrees with its project.</summary>
    ProjectReportMismatch,
    /// <summary>A plan-backed required TRX counter is absent.</summary>
    MissingTrxCounter,
    /// <summary>A plan-backed required TRX counter is not numeric.</summary>
    MalformedTrxCounter,
    /// <summary>A plan-backed required TRX counter is negative.</summary>
    NegativeTrxCounter,
    /// <summary>The required TRX counters are internally inconsistent.</summary>
    InconsistentTrxCounters,
    /// <summary>The TRX report discovered zero tests.</summary>
    ZeroDiscoveredTests,
    /// <summary>The TRX report contains a failed test.</summary>
    FailedTrxTests,
    /// <summary>The TRX report contains a not-executed test.</summary>
    NotExecutedTrxTests,
    /// <summary>An optional nonstandard skipped counter is present and nonzero.</summary>
    OptionalSkippedTrxTests,
    /// <summary>An optional adverse TRX counter is present and nonzero.</summary>
    OptionalAdverseTrxTests,
    /// <summary>The TRX start and finish timestamps are invalid.</summary>
    InvalidTrxTimes,
    /// <summary>The TRX outcome is not completed.</summary>
    IncompleteTrxOutcome,
    /// <summary>The Pester NUnit-compatible report is absent.</summary>
    MissingPesterReport,
    /// <summary>A required Pester native counter is absent.</summary>
    MissingPesterNativeCounter,
    /// <summary>Pester reports failed tests.</summary>
    FailedPesterTests,
    /// <summary>Pester reports skipped tests.</summary>
    SkippedPesterTests,
    /// <summary>Pester reports inconclusive tests.</summary>
    InconclusivePesterTests,
    /// <summary>Pester reports tests that did not run.</summary>
    NotRunPesterTests,
    /// <summary>Pester reports failed blocks.</summary>
    FailedPesterBlocks,
    /// <summary>Pester reports failed containers.</summary>
    FailedPesterContainers,
    /// <summary>The explicit Build.Tests report is absent.</summary>
    MissingBuildTestsReport,
    /// <summary>The explicit Build.Tests report is not passing.</summary>
    NonPassingBuildTestsReport,
    /// <summary>A required orchestration command result is absent.</summary>
    MissingRequiredCommand,
    /// <summary>A required orchestration command has a nonzero exit code.</summary>
    NonZeroCommandExit,
    /// <summary>An included source file changed after manifest capture.</summary>
    EditedIncludedSource,
    /// <summary>A relevant source file was added after manifest capture.</summary>
    AddedIncludedSource,
    /// <summary>An included source file was deleted after manifest capture.</summary>
    DeletedIncludedSource,
    /// <summary>The current tool path differs from the pre-run observation.</summary>
    ToolPathDrift,
    /// <summary>The current tool version differs from the pre-run observation.</summary>
    ToolVersionDrift,
    /// <summary>The current executable bytes differ from the pre-run observation.</summary>
    ToolByteHashDrift,
}
