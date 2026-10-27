using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001 / TEST-MCP-143 physical-artifact tests for
/// the machine-readable session-life gate validator. R2 tightens the r1 matrix
/// for classified rejection, per-command exits, freshness and run binding,
/// present-report identity, and a nested .mcpServer ancestor.
/// </summary>
public sealed class SessionLifeUnitGateValidatorTests
{
    /// <summary>
    /// Gets invalid-artifact mutations shared by both unit and provider lanes.
    /// Command exits are parameterized separately so each required command is challenged.
    /// </summary>
    public static TheoryData<SessionLifeInvalidFixture> SharedInvalidFixtures => new()
    {
        SessionLifeInvalidFixture.MissingInventory,
        SessionLifeInvalidFixture.MissingProjectReport,
        SessionLifeInvalidFixture.DuplicateProject,
        SessionLifeInvalidFixture.DuplicateReportFile,
        SessionLifeInvalidFixture.WrongRunId,
        SessionLifeInvalidFixture.WrongRunSourceManifest,
        SessionLifeInvalidFixture.WrongRunCommandResults,
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
        SessionLifeInvalidFixture.StalePesterReport,
        SessionLifeInvalidFixture.StalePesterNativeResult,
        SessionLifeInvalidFixture.MissingBuildTestsReport,
        SessionLifeInvalidFixture.NonPassingBuildTestsReport,
        SessionLifeInvalidFixture.StaleBuildTestsReport,
    };

    /// <summary>
    /// Gets every lane name, required command, and command-result defect.
    /// Unit covers pester, nuke-test, and build-tests. Provider covers nuke-provider.
    /// Lane names stay public so the internal scope enum is not part of the test signature.
    /// </summary>
    public static TheoryData<string, string, SessionLifeCommandDefect> RequiredCommandDefects
    {
        get
        {
            var data = new TheoryData<string, string, SessionLifeCommandDefect>();
            AddLane(data, "unit", SessionLifeUnitGateScope.Unit);
            AddLane(data, "provider", SessionLifeUnitGateScope.Provider);
            return data;
        }
    }

    /// <summary>Adds every command defect for one lane.</summary>
    /// <param name="data">The theory rows under construction.</param>
    /// <param name="lane">The public lane name.</param>
    /// <param name="scope">The internal scope used to read required commands.</param>
    private static void AddLane(
        TheoryData<string, string, SessionLifeCommandDefect> data,
        string lane,
        SessionLifeUnitGateScope scope)
    {
        foreach (var command in SessionLifeGateFixture.RequiredCommands(scope))
        {
            data.Add(lane, command, SessionLifeCommandDefect.Missing);
            data.Add(lane, command, SessionLifeCommandDefect.NonZero);
            data.Add(lane, command, SessionLifeCommandDefect.MissingExitCode);
            data.Add(lane, command, SessionLifeCommandDefect.MalformedExitCode);
        }
    }

    /// <summary>Maps a public lane name back to the internal scope.</summary>
    /// <param name="lane">The lane name from theory data.</param>
    /// <returns>The matching scope.</returns>
    private static SessionLifeUnitGateScope ParseScope(string lane) =>
        lane switch
        {
            "unit" => SessionLifeUnitGateScope.Unit,
            "provider" => SessionLifeUnitGateScope.Provider,
            _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Unknown session-life lane."),
        };

    /// <summary>
    /// Requires a valid unit lane to succeed without any provider artifacts.
    /// Uses a fresh physical unit fixture and validates FR-MCP-107 lane independence.
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
    /// Uses a fresh physical provider fixture and validates FR-MCP-107 lane independence.
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
    /// Uses the unit fixture and a post-manifest log under TestResults. Validates FR-MCP-107-AC004.
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
    /// Requires a valid unit fixture nested under an ancestor named .mcpServer to be accepted.
    /// Exclusions stay repository-relative. Validates FR-MCP-107-AC004 and TR-MCP-PLAN-001-AC004.
    /// </summary>
    [Fact]
    public void Validate_NestedQBrainAiAncestor_AcceptsValidUnitArtifacts()
    {
        using var fixture = SessionLifeGateFixture.CreateUnderQBrainAiAncestor(SessionLifeUnitGateScope.Unit);
        fixture.AssertNestedQBrainAiAncestor();
        fixture.AssertRepositoryRelativeExclusions();

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));

        Assert.Null(exception);
    }

    /// <summary>
    /// Requires a report-only write under a .mcpServer ancestor to leave source identity valid.
    /// The extra file stays under the repository-relative TestResults exclusion. Validates TEST-MCP-143-AC003.
    /// </summary>
    [Fact]
    public void Validate_NestedQBrainAiAncestor_ReportOnlyWrite_DoesNotCauseSourceDrift()
    {
        using var fixture = SessionLifeGateFixture.CreateUnderQBrainAiAncestor(SessionLifeUnitGateScope.Unit);
        fixture.AssertNestedQBrainAiAncestor();
        fixture.AssertRepositoryRelativeExclusions();
        var reportPath = fixture.WriteReportOnlyArtifact();
        Assert.StartsWith(fixture.RepositoryRoot, reportPath, StringComparison.Ordinal);
        Assert.Contains($"{Path.DirectorySeparatorChar}TestResults{Path.DirectorySeparatorChar}", reportPath, StringComparison.Ordinal);

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));

        Assert.Null(exception);
    }

    /// <summary>
    /// Requires included-source edit, add, and delete to be rejected under a .mcpServer ancestor.
    /// Expectations stay repository-relative. Validates FR-MCP-107-AC004 and TEST-MCP-143-AC003.
    /// </summary>
    /// <param name="invalidFixture">The single source mutation applied to the nested valid baseline.</param>
    [Theory]
    [InlineData(SessionLifeInvalidFixture.EditedIncludedSource)]
    [InlineData(SessionLifeInvalidFixture.AddedIncludedSource)]
    [InlineData(SessionLifeInvalidFixture.DeletedIncludedSource)]
    public void Validate_NestedQBrainAiAncestor_IncludedSourceDrift_IsRejected(
        SessionLifeInvalidFixture invalidFixture)
    {
        using var fixture = SessionLifeGateFixture.CreateUnderQBrainAiAncestor(SessionLifeUnitGateScope.Unit);
        fixture.AssertNestedQBrainAiAncestor();
        fixture.AssertRepositoryRelativeExclusions();
        AssertImplementedRejection(fixture, invalidFixture);
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
    /// Requires unit-only Pester, freshness, and explicit Build.Tests mutations to be rejected.
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
    /// Requires each required command defect to be rejected for its lane and command name.
    /// Later commands retain exit code 0 so an earlier failure stays visible.
    /// Validates FR-MCP-107-AC003, TR-MCP-PLAN-001-AC004, and TEST-MCP-143-AC006.
    /// </summary>
    /// <param name="lane">The public lane name whose required commands are under test.</param>
    /// <param name="commandName">The single command that is missing or malformed.</param>
    /// <param name="defect">The command-result defect applied to that command.</param>
    [Theory]
    [MemberData(nameof(RequiredCommandDefects))]
    public void Validate_RequiredCommandDefect_IsRejected(
        string lane,
        string commandName,
        SessionLifeCommandDefect defect)
    {
        var scope = ParseScope(lane);
        using var fixture = SessionLifeGateFixture.Create(scope);
        try
        {
            fixture.ApplyCommandDefect(commandName, defect);
        }
        catch (Exception setupFailure)
        {
            Assert.Fail(
                $"Fixture setup failed before validation and cannot count as rejection: {setupFailure.GetType().FullName}: {setupFailure.Message}");
        }

        fixture.AssertUnaffectedCommandsRemainSuccessful(commandName, defect);
        var expectation = fixture.DescribeCommand(commandName, defect);
        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));
        AssertClassifiedRejection(exception, expectation.Reason, expectation.Affected);
    }

    /// <summary>
    /// Requires <see cref="InvalidDataException"/> with the mutation's stable reason and affected token.
    /// Setup exceptions are failed before <c>Validate</c> and cannot satisfy the oracle.
    /// </summary>
    /// <param name="fixture">The valid baseline fixture.</param>
    /// <param name="invalidFixture">The single mutation to apply.</param>
    private static void AssertImplementedRejection(
        SessionLifeGateFixture fixture,
        SessionLifeInvalidFixture invalidFixture)
    {
        try
        {
            fixture.Apply(invalidFixture);
        }
        catch (Exception setupFailure)
        {
            Assert.Fail(
                $"Fixture setup failed before validation and cannot count as rejection: {setupFailure.GetType().FullName}: {setupFailure.Message}");
        }

        var expectation = fixture.Describe(invalidFixture);
        if (invalidFixture is SessionLifeInvalidFixture.EditedIncludedSource
            or SessionLifeInvalidFixture.AddedIncludedSource
            or SessionLifeInvalidFixture.DeletedIncludedSource)
        {
            Assert.DoesNotContain(".mcpServer", expectation.Affected, StringComparison.OrdinalIgnoreCase);
            Assert.False(Path.IsPathRooted(expectation.Affected));
        }

        var exception = Record.Exception(() => fixture.Validator.Validate(fixture.Request));
        AssertClassifiedRejection(exception, expectation.Reason, expectation.Affected);
    }

    /// <summary>
    /// Asserts a classified <see cref="InvalidDataException"/> whose message carries the stable tokens.
    /// </summary>
    /// <param name="exception">The exception recorded from <c>Validate</c>.</param>
    /// <param name="reason">The stable reason token.</param>
    /// <param name="affected">The affected artifact, project, or command token.</param>
    private static void AssertClassifiedRejection(Exception? exception, string reason, string affected)
    {
        if (exception is not InvalidDataException)
        {
            var actual = exception is null ? "no exception" : exception.GetType().FullName;
            Assert.Fail(
                $"Expected InvalidDataException with reason={reason} affected={affected}. Actual: {actual}: {exception?.Message}");
        }

        var data = (InvalidDataException)exception;
        if (!data.Message.Contains($"reason={reason}", StringComparison.Ordinal)
            || !data.Message.Contains($"affected={affected}", StringComparison.Ordinal))
        {
            Assert.Fail($"Expected reason={reason} and affected={affected} in '{data.Message}'.");
        }
    }

    /// <summary>
    /// Creates and mutates one real on-disk gate fixture without implementing validation logic.
    /// </summary>
    private sealed class SessionLifeGateFixture : IDisposable
    {
        /// <summary>The stable run identifier used by the fixture.</summary>
        private const string RunId = "sessionlife-red-001";

        /// <summary>The alternate run identifier used by wrong-run mutations.</summary>
        private const string DifferentRunId = "different-run";

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
            "src/QBrainAi.Services/SessionLogService.cs",
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
        /// <param name="nestRepositoryUnderQBrainAiAncestor">
        /// When true, places the repository under a GUID-owned <c>.mcpServer</c> ancestor.
        /// </param>
        private SessionLifeGateFixture(SessionLifeUnitGateScope scope, bool nestRepositoryUnderQBrainAiAncestor)
        {
            Scope = scope;
            OwnedRoot = Path.Combine(ResolveCleanFixtureTempRoot(), "QBrainAi-SessionLifeUnitGate", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            RepositoryRoot = nestRepositoryUnderQBrainAiAncestor
                ? Path.Combine(OwnedRoot, ".mcpServer", "worktrees", "candidate")
                : Path.Combine(OwnedRoot, "repository");
            ToolRoot = Path.Combine(OwnedRoot, "tools");
            ResultsRoot = Path.Combine(RepositoryRoot, "TestResults", RunId);
            SourceManifestPath = Path.Combine(ResultsRoot, "source-manifest.json");
            _projects = scope == SessionLifeUnitGateScope.Unit
                ? new[] { "QBrainAi.Client.Tests", "QBrainAi.Services.Tests" }
                : new[] { "QBrainAi.Support.Mcp.Tests", "Build.Tests" };

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
        /// <summary>Gets the GUID-owned parent containing the repository and tool roots.</summary>
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

        /// <summary>Returns the required command names for a lane, in execution order.</summary>
        /// <param name="scope">The lane whose commands are required.</param>
        /// <returns>The ordered command names.</returns>
        internal static IReadOnlyList<string> RequiredCommands(SessionLifeUnitGateScope scope) =>
            scope == SessionLifeUnitGateScope.Unit
                ? new[] { "pester", "nuke-test", "build-tests" }
                : new[] { "nuke-provider" };

        /// <summary>Creates a valid physical fixture for the requested lane.</summary>
        /// <param name="scope">The independent lane to create.</param>
        /// <returns>A disposable real-artifact fixture.</returns>
        internal static SessionLifeGateFixture Create(SessionLifeUnitGateScope scope) =>
            new(scope, nestRepositoryUnderQBrainAiAncestor: false);

        /// <summary>Creates a valid fixture whose repository has a <c>.mcpServer</c> ancestor.</summary>
        /// <param name="scope">The independent lane to create.</param>
        /// <returns>A disposable real-artifact fixture rooted under the owned <c>.mcpServer</c> directory.</returns>

        /// <summary>
        /// Returns a temp root that does not already contain an ambient .mcpServer path segment.
        /// Some Legion layouts redirect TMP under the repo .mcpServer/tmp, which would otherwise
        /// make ToolRoot fail the nested-ancestor assertions.
        /// </summary>
        private static string ResolveCleanFixtureTempRoot()
        {
            var temp = Path.GetTempPath();
            var marker = Path.DirectorySeparatorChar + ".mcpServer" + Path.DirectorySeparatorChar;
            var normalized = temp.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.TrimEnd(Path.DirectorySeparatorChar)
                    .EndsWith(".mcpServer", StringComparison.OrdinalIgnoreCase))
            {
                var root = Path.GetPathRoot(normalized);
                if (string.IsNullOrWhiteSpace(root))
                {
                    root = Path.GetPathRoot(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                }

                temp = Path.Combine(root!, "QBrainAiGateTemp");
                Directory.CreateDirectory(temp);
            }

            return temp;
        }
        internal static SessionLifeGateFixture CreateUnderQBrainAiAncestor(SessionLifeUnitGateScope scope) =>
            new(scope, nestRepositoryUnderQBrainAiAncestor: true);

        /// <summary>Applies one invalid mutation to the valid baseline.</summary>
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
                    MutateInventory(inventory => inventory["runId"] = DifferentRunId);
                    AssertArtifactRunId(SourceManifestPath, RunId);
                    AssertArtifactRunId(_commandResultsPath, RunId);
                    break;
                case SessionLifeInvalidFixture.WrongRunSourceManifest:
                    MutateJson(SourceManifestPath, document => document["runId"] = DifferentRunId);
                    AssertArtifactRunId(_inventoryPath, RunId);
                    AssertArtifactRunId(_commandResultsPath, RunId);
                    break;
                case SessionLifeInvalidFixture.WrongRunCommandResults:
                    MutateJson(_commandResultsPath, document => document["runId"] = DifferentRunId);
                    AssertArtifactRunId(_inventoryPath, RunId);
                    AssertArtifactRunId(SourceManifestPath, RunId);
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
                    ApplyProjectReportMismatch();
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
                case SessionLifeInvalidFixture.StalePesterReport:
                    ApplyStalePesterReport();
                    break;
                case SessionLifeInvalidFixture.StalePesterNativeResult:
                    ApplyStalePesterNativeResult();
                    break;
                case SessionLifeInvalidFixture.MissingBuildTestsReport:
                    File.Delete(_buildTestsReportPath);
                    break;
                case SessionLifeInvalidFixture.NonPassingBuildTestsReport:
                    SetRequiredTrxCounters(_buildTestsReportPath, 2, 2, 1, 1, 0);
                    break;
                case SessionLifeInvalidFixture.StaleBuildTestsReport:
                    WritePassingTrx(_buildTestsReportPath, RunStart.AddMinutes(-2), RunStart.AddMinutes(-1));
                    File.SetLastWriteTimeUtc(_buildTestsReportPath, RunStart.AddMinutes(-1).UtcDateTime);
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

        /// <summary>Applies one command-result defect while leaving every other command successful.</summary>
        /// <param name="commandName">The required command to mutate.</param>
        /// <param name="defect">The defect to apply.</param>
        internal void ApplyCommandDefect(string commandName, SessionLifeCommandDefect defect)
        {
            MutateJson(_commandResultsPath, document =>
            {
                var commands = document["commands"]!.AsArray();
                var index = FindCommandIndex(commands, commandName);
                if (index < 0)
                    throw new InvalidOperationException($"Fixture setup could not find command '{commandName}'.");

                var command = commands[index]!.AsObject();
                switch (defect)
                {
                    case SessionLifeCommandDefect.Missing:
                        commands.RemoveAt(index);
                        break;
                    case SessionLifeCommandDefect.NonZero:
                        command["exitCode"] = 1;
                        break;
                    case SessionLifeCommandDefect.MissingExitCode:
                        command.Remove("exitCode");
                        break;
                    case SessionLifeCommandDefect.MalformedExitCode:
                        command["exitCode"] = "not-an-integer";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(defect), defect, "Unknown command defect.");
                }
            });
        }

        /// <summary>
        /// Returns the stable reason and affected token a future validator must report for this mutation.
        /// </summary>
        /// <param name="invalidFixture">The mutation already applied.</param>
        /// <returns>The classified rejection expectation.</returns>
        internal SessionLifeRejectionExpectation Describe(SessionLifeInvalidFixture invalidFixture)
        {
            var lane = GetLaneName(Scope);
            var project = _projects[0];
            var projectReport = $"{project}|{lane}/{project}/{project}.trx";
            var inventory = $"{lane}/selected-projects.json";
            return invalidFixture switch
            {
                SessionLifeInvalidFixture.MissingInventory => new("missing-inventory", inventory),
                SessionLifeInvalidFixture.MissingProjectReport => new("missing-project-report", projectReport),
                SessionLifeInvalidFixture.DuplicateProject => new("duplicate-project", project),
                SessionLifeInvalidFixture.DuplicateReportFile => new("duplicate-report-file", $"{lane}/{project}/duplicate.trx"),
                SessionLifeInvalidFixture.WrongRunId => new("wrong-run-id", inventory),
                SessionLifeInvalidFixture.WrongRunSourceManifest => new("wrong-run-source-manifest", "source-manifest.json"),
                SessionLifeInvalidFixture.WrongRunCommandResults => new("wrong-run-command-results", "command-results.json"),
                SessionLifeInvalidFixture.ScopeMismatch => new("scope-mismatch", inventory),
                SessionLifeInvalidFixture.StaleInventory => new("stale-inventory", inventory),
                SessionLifeInvalidFixture.StaleProjectReport => new("stale-project-report", projectReport),
                SessionLifeInvalidFixture.ProjectReportMismatch => new("project-report-identity", $"{project}|{lane}/{project}/Wrong.Tests.trx"),
                SessionLifeInvalidFixture.MissingTrxCounter => new("missing-trx-counter", projectReport),
                SessionLifeInvalidFixture.MalformedTrxCounter => new("malformed-trx-counter", projectReport),
                SessionLifeInvalidFixture.NegativeTrxCounter => new("negative-trx-counter", projectReport),
                SessionLifeInvalidFixture.InconsistentTrxCounters => new("inconsistent-trx-counters", projectReport),
                SessionLifeInvalidFixture.ZeroDiscoveredTests => new("zero-discovered-tests", projectReport),
                SessionLifeInvalidFixture.FailedTrxTests => new("failed-trx-tests", projectReport),
                SessionLifeInvalidFixture.NotExecutedTrxTests => new("not-executed-trx-tests", projectReport),
                SessionLifeInvalidFixture.OptionalSkippedTrxTests => new("optional-skipped-trx-counter", projectReport),
                SessionLifeInvalidFixture.OptionalAdverseTrxTests => new("optional-adverse-trx-counter", projectReport),
                SessionLifeInvalidFixture.InvalidTrxTimes => new("invalid-trx-times", projectReport),
                SessionLifeInvalidFixture.IncompleteTrxOutcome => new("incomplete-trx-outcome", projectReport),
                SessionLifeInvalidFixture.MissingPesterReport => new("missing-pester-report", "pester/results.xml"),
                SessionLifeInvalidFixture.MissingPesterNativeCounter => new("missing-pester-native-counter", "pester/native-run.json"),
                SessionLifeInvalidFixture.FailedPesterTests => new("failed-pester-tests", "pester/native-run.json"),
                SessionLifeInvalidFixture.SkippedPesterTests => new("skipped-pester-tests", "pester/native-run.json"),
                SessionLifeInvalidFixture.InconclusivePesterTests => new("inconclusive-pester-tests", "pester/native-run.json"),
                SessionLifeInvalidFixture.NotRunPesterTests => new("not-run-pester-tests", "pester/native-run.json"),
                SessionLifeInvalidFixture.FailedPesterBlocks => new("failed-pester-blocks", "pester/native-run.json"),
                SessionLifeInvalidFixture.FailedPesterContainers => new("failed-pester-containers", "pester/native-run.json"),
                SessionLifeInvalidFixture.StalePesterReport => new("stale-pester-report", "pester/results.xml"),
                SessionLifeInvalidFixture.StalePesterNativeResult => new("stale-pester-native-result", "pester/native-run.json"),
                SessionLifeInvalidFixture.MissingBuildTestsReport => new("missing-build-tests-report", "Build.Tests|build-tests/Build.Tests.trx"),
                SessionLifeInvalidFixture.NonPassingBuildTestsReport => new("nonpassing-build-tests-report", "Build.Tests|build-tests/Build.Tests.trx"),
                SessionLifeInvalidFixture.StaleBuildTestsReport => new("stale-build-tests-report", "Build.Tests|build-tests/Build.Tests.trx"),
                SessionLifeInvalidFixture.EditedIncludedSource => new("edited-included-source", _sourceRelativePaths[0]),
                SessionLifeInvalidFixture.AddedIncludedSource => new("added-included-source", "src/AddedAfterManifest.cs"),
                SessionLifeInvalidFixture.DeletedIncludedSource => new("deleted-included-source", _sourceRelativePaths[1]),
                SessionLifeInvalidFixture.ToolPathDrift => new("tool-path-drift", "dotnet"),
                SessionLifeInvalidFixture.ToolVersionDrift => new("tool-version-drift", "dotnet"),
                SessionLifeInvalidFixture.ToolByteHashDrift => new("tool-byte-hash-drift", "dotnet"),
                _ => throw new ArgumentOutOfRangeException(nameof(invalidFixture), invalidFixture, "Unknown fixture mutation."),
            };
        }

        /// <summary>Returns the stable reason and affected command token for a command defect.</summary>
        /// <param name="commandName">The required command.</param>
        /// <param name="defect">The defect applied to that command.</param>
        /// <returns>The classified rejection expectation.</returns>
        internal SessionLifeRejectionExpectation DescribeCommand(string commandName, SessionLifeCommandDefect defect)
        {
            var reason = defect switch
            {
                SessionLifeCommandDefect.Missing => "missing-required-command",
                SessionLifeCommandDefect.NonZero => "nonzero-command-exit",
                SessionLifeCommandDefect.MissingExitCode => "missing-command-exit-code",
                SessionLifeCommandDefect.MalformedExitCode => "malformed-command-exit-code",
                _ => throw new ArgumentOutOfRangeException(nameof(defect), defect, "Unknown command defect."),
            };
            return new SessionLifeRejectionExpectation(reason, $"{GetLaneName(Scope)}:{commandName}");
        }

        /// <summary>
        /// Proves the fixture keeps every unaffected command at exit code 0, including later commands.
        /// </summary>
        /// <param name="commandName">The command that carries the defect.</param>
        /// <param name="defect">The defect applied to that command.</param>
        internal void AssertUnaffectedCommandsRemainSuccessful(string commandName, SessionLifeCommandDefect defect)
        {
            var document = ReadJsonObject(_commandResultsPath);
            Assert.Equal(RunId, document["runId"]!.GetValue<string>());
            Assert.Equal(GetLaneName(Scope), document["scope"]!.GetValue<string>());
            var commands = document["commands"]!.AsArray();
            JsonObject? target = null;
            foreach (var node in commands)
            {
                var command = node!.AsObject();
                var name = command["name"]!.GetValue<string>();
                if (string.Equals(name, commandName, StringComparison.Ordinal))
                {
                    target = command;
                    continue;
                }

                Assert.Equal(0, command["exitCode"]!.GetValue<int>());
            }

            var required = RequiredCommands(Scope);
            var index = -1;
            for (var i = 0; i < required.Count; i++)
            {
                if (string.Equals(required[i], commandName, StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }

            Assert.True(index >= 0, $"Command '{commandName}' is not required for {Scope}.");
            if (index < required.Count - 1)
            {
                var laterName = required[index + 1];
                var laterPresent = false;
                foreach (var node in commands)
                {
                    if (string.Equals(node!["name"]!.GetValue<string>(), laterName, StringComparison.Ordinal))
                    {
                        laterPresent = true;
                        Assert.Equal(0, node["exitCode"]!.GetValue<int>());
                    }
                }

                Assert.True(laterPresent, $"Later command '{laterName}' must remain successful.");
            }

            switch (defect)
            {
                case SessionLifeCommandDefect.Missing:
                    Assert.Null(target);
                    break;
                case SessionLifeCommandDefect.NonZero:
                    Assert.NotNull(target);
                    Assert.Equal(1, target!["exitCode"]!.GetValue<int>());
                    break;
                case SessionLifeCommandDefect.MissingExitCode:
                    Assert.NotNull(target);
                    Assert.False(target!.ContainsKey("exitCode"));
                    break;
                case SessionLifeCommandDefect.MalformedExitCode:
                    Assert.NotNull(target);
                    Assert.Equal("not-an-integer", target!["exitCode"]!.GetValue<string>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(defect), defect, "Unknown command defect.");
            }
        }

        /// <summary>Proves the repository has an ancestor directory named exactly <c>.mcpServer</c>.</summary>
        internal void AssertNestedQBrainAiAncestor()
        {
            var found = false;
            for (var directory = new DirectoryInfo(RepositoryRoot).Parent; directory is not null; directory = directory.Parent)
            {
                if (string.Equals(directory.Name, ".mcpServer", StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found, $"Repository '{RepositoryRoot}' has no .mcpServer ancestor.");
            Assert.StartsWith(OwnedRoot, RepositoryRoot, StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"{Path.DirectorySeparatorChar}.mcpServer{Path.DirectorySeparatorChar}",
                ToolRoot,
                StringComparison.Ordinal);
        }

        /// <summary>Proves source exclusions are repository-relative and do not name <c>.mcpServer</c>.</summary>
        internal void AssertRepositoryRelativeExclusions()
        {
            var document = ReadJsonObject(SourceManifestPath);
            var exclusions = document["sourcePolicy"]!["exclusions"]!.AsArray();
            Assert.NotEmpty(exclusions);
            foreach (var exclusion in exclusions)
            {
                var value = exclusion!.GetValue<string>();
                Assert.False(Path.IsPathRooted(value), $"Exclusion '{value}' must stay repository-relative.");
                Assert.DoesNotContain(".mcpServer", value, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Writes an extra report beneath the excluded results root after manifest capture.</summary>
        /// <returns>The absolute path of the report-only artifact.</returns>
        internal string WriteReportOnlyArtifact()
        {
            var path = Path.Combine(ResultsRoot, "diagnostics", "post-manifest.log");
            WriteBytes(path, "report output only\n");
            return path;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(OwnedRoot))
                Directory.Delete(OwnedRoot, recursive: true);
        }

        /// <summary>Moves one present report onto a non-canonical path and points the inventory at it.</summary>
        private void ApplyProjectReportMismatch()
        {
            var project = _projects[0];
            var lane = GetLaneName(Scope);
            var source = _projectReportPaths[0];
            var relative = $"{lane}/{project}/Wrong.Tests.trx";
            var destination = Path.Combine(ResultsRoot, lane, project, "Wrong.Tests.trx");
            File.Move(source, destination);
            MutateInventory(inventory => inventory["projects"]![0]!["reportPath"] = relative);

            if (!File.Exists(destination) || File.Exists(source))
                throw new InvalidOperationException("Project/report mismatch setup did not leave exactly the moved report.");

            var trxCount = Directory.GetFiles(Path.Combine(ResultsRoot, lane), "*.trx", SearchOption.AllDirectories).Length;
            if (trxCount != _projects.Length)
                throw new InvalidOperationException($"Project/report mismatch setup changed the TRX count to {trxCount}.");

            var inventoryCount = ReadJsonObject(_inventoryPath)["projects"]!.AsArray().Count;
            if (inventoryCount != _projects.Length)
                throw new InvalidOperationException($"Project/report mismatch setup changed the inventory count to {inventoryCount}.");
        }

        /// <summary>Stales only the Pester NUnit report and leaves the native result on the run start.</summary>
        private void ApplyStalePesterReport()
        {
            var stale = RunStart.AddMinutes(-5);
            var document = XDocument.Load(_pesterReportPath);
            document.Root!.SetAttributeValue("date", stale.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            document.Root.SetAttributeValue("time", stale.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            WriteXml(_pesterReportPath, document);
            File.SetLastWriteTimeUtc(_pesterReportPath, stale.UtcDateTime);

            var native = ReadJsonObject(_pesterNativePath);
            if (!string.Equals(native["RunId"]!.GetValue<string>(), RunId, StringComparison.Ordinal)
                || !string.Equals(native["ExecutedAt"]!.GetValue<string>(), RunStart.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Stale Pester report setup also changed the native companion.");
            }
        }

        /// <summary>Stales only the Pester native result and leaves the NUnit report on the run start.</summary>
        private void ApplyStalePesterNativeResult()
        {
            var stale = RunStart.AddMinutes(-5);
            MutateJson(_pesterNativePath, document =>
                document["ExecutedAt"] = stale.ToString("O", CultureInfo.InvariantCulture));
            File.SetLastWriteTimeUtc(_pesterNativePath, stale.UtcDateTime);

            var report = XDocument.Load(_pesterReportPath);
            var date = report.Root!.Attribute("date")?.Value;
            var time = report.Root.Attribute("time")?.Value;
            if (!string.Equals(date, RunStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                || !string.Equals(time, RunStart.ToString("HH:mm:ss", CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Stale Pester native setup also changed the XML companion.");
            }
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
                        new XAttribute("date", RunStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                        new XAttribute("time", RunStart.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                        new XAttribute("total", 2),
                        new XAttribute("failures", 0),
                        new XAttribute("not-run", 0),
                        new XAttribute("inconclusive", 0),
                        new XAttribute("skipped", 0))));
            WriteJson(_pesterNativePath, new JsonObject
            {
                ["RunId"] = RunId,
                ["ExecutedAt"] = RunStart.ToString("O", CultureInfo.InvariantCulture),
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
            foreach (var name in RequiredCommands(Scope))
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

        /// <summary>Reads a JSON object artifact.</summary>
        /// <param name="path">The JSON path.</param>
        /// <returns>The parsed object.</returns>
        private static JsonObject ReadJsonObject(string path) =>
            JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!.AsObject();

        /// <summary>Fails setup when a companion artifact is no longer bound to the expected run.</summary>
        /// <param name="path">The companion artifact.</param>
        /// <param name="expectedRunId">The run identifier that companion must still carry.</param>
        private static void AssertArtifactRunId(string path, string expectedRunId)
        {
            var actual = ReadJsonObject(path)["runId"]!.GetValue<string>();
            if (!string.Equals(actual, expectedRunId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Fixture setup run binding drifted for '{path}': '{actual}'.");
        }

        /// <summary>Finds a command by name.</summary>
        /// <param name="commands">The command array.</param>
        /// <param name="commandName">The command name.</param>
        /// <returns>The index, or -1 when the command is absent.</returns>
        private static int FindCommandIndex(JsonArray commands, string commandName)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                if (string.Equals(commands[i]!["name"]!.GetValue<string>(), commandName, StringComparison.Ordinal))
                    return i;
            }

            return -1;
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
/// Names the stable reason and affected artifact, project, or command a rejection must carry.
/// </summary>
/// <param name="Reason">The stable reason token.</param>
/// <param name="Affected">The affected artifact, project, or command token.</param>
public sealed record SessionLifeRejectionExpectation(string Reason, string Affected);

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
    /// <summary>The source manifest is bound to another run identifier.</summary>
    WrongRunSourceManifest,
    /// <summary>The command-results artifact is bound to another run identifier.</summary>
    WrongRunCommandResults,
    /// <summary>The inventory is bound to the wrong validation lane.</summary>
    ScopeMismatch,
    /// <summary>The inventory predates the requested run.</summary>
    StaleInventory,
    /// <summary>A project report predates the requested run.</summary>
    StaleProjectReport,
    /// <summary>A present report is bound to the wrong project path.</summary>
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
    /// <summary>The Pester NUnit report predates the requested run while the native result stays fresh.</summary>
    StalePesterReport,
    /// <summary>The Pester native result predates the requested run while the NUnit report stays fresh.</summary>
    StalePesterNativeResult,
    /// <summary>The explicit Build.Tests report is absent.</summary>
    MissingBuildTestsReport,
    /// <summary>The explicit Build.Tests report is not passing.</summary>
    NonPassingBuildTestsReport,
    /// <summary>The explicit Build.Tests report predates the requested run.</summary>
    StaleBuildTestsReport,
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

/// <summary>
/// Names a single required-command result defect.
/// </summary>
public enum SessionLifeCommandDefect
{
    /// <summary>The required command entry is absent.</summary>
    Missing,
    /// <summary>The required command recorded a nonzero exit code.</summary>
    NonZero,
    /// <summary>The required command omitted <c>exitCode</c>.</summary>
    MissingExitCode,
    /// <summary>The required command recorded an unparseable <c>exitCode</c>.</summary>
    MalformedExitCode,
}
