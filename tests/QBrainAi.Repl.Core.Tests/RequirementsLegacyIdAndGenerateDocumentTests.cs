using System.Collections.Generic;
using System.Net;
using System.Text;
using QBrainAi.Client;
using QBrainAi.Client.Models;
using QBrainAi.Repl.Core;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// PLAN-SESSIONTRIAGEFIX-001 slice B: B7 (triage-report-3bd514bb) stored FR and TEST records whose legacy ids fall
/// outside the canonical create format are readable, updatable and deletable through the REPL, as TR records already
/// are, while create keeps the canonical format; B3 (triage-report-c03d190f) generateDocument accepts every documented
/// format and docType; B2 (triage-report-b72cfaf4) TR and TEST items report their stored timestamps. Requirements: B7
/// FR-MCP-TRIAGEREQ-001 ac-2, TR-MCP-TRIAGEREQ-001 AC-001 and TEST-MCP-TRIAGEREQ-001 AC-001; B3 TR-MCP-REQEXPORT-003 AC-001 and
/// TEST-MCP-REQEXPORT-003 AC-001; B2 TR-MCP-REQ-003 AC-001. Data: a RequirementsWorkflow over a RequirementsClient whose handler
/// records requests and answers with a minimal entry; RequestPayload instances for the validator.
/// </summary>
public sealed class RequirementsLegacyIdAndGenerateDocumentTests
{
    /// <summary>B7 mock gate (characterization): a canonical FR id reaches the client's FR route.</summary>
    [Fact]
    public async Task GetFrAsync_CanonicalId_ReachesTheClientRoute()
    {
        var handler = new RecordingHandler("""{"id":"FR-MCP-001","title":"t","body":"b"}""");
        var workflow = BuildWorkflow(handler);

        var item = await workflow.GetFrAsync("FR-MCP-001", TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("FR-MCP-001", item.Id);
        AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-MCP-001");
    }

    /// <summary>
    /// B7 assertion proof: the route assertion rejects a workflow call that sent no request and one that reached another route.
    /// Data: a fresh RecordingHandler, then the canonical FR get.
    /// </summary>
    [Fact]
    public async Task RouteAssertion_RejectsNoRequestAndAnotherRoute()
    {
        var handler = new RecordingHandler("""{"id":"FR-MCP-001","title":"t","body":"b"}""");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-MCP-001"));

        await BuildWorkflow(handler).GetFrAsync("FR-MCP-001", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-SUPPORT-010A"));
    }

    /// <summary>
    /// B7 binding (FR-MCP-TRIAGEREQ-001 ac-2, TEST-MCP-TRIAGEREQ-001 AC-001; triage-report-3bd514bb): get, update and
    /// delete of a stored FR whose legacy id falls outside the canonical create format reach the FR route, as TR records already
    /// do. Data: RecordingHandler answering the legacy FR; FrUpdateRequestModel.
    /// </summary>
    [Fact]
    public async Task FrGetUpdateDelete_LegacyId_ReachTheFrRoute()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""{"id":"FR-SUPPORT-010A","title":"t","body":"b"}""");
        var workflow = BuildWorkflow(handler);

        Assert.Equal("FR-SUPPORT-010A", (await workflow.GetFrAsync("FR-SUPPORT-010A", ct).ConfigureAwait(true)).Id);
        AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-SUPPORT-010A");
        handler.Reset();
        await workflow.UpdateFrAsync(new FrUpdateRequestModel { Id = "FR-SUPPORT-010A", Title = "t" }, ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-SUPPORT-010A");
        handler.Reset();
        await workflow.DeleteFrAsync("FR-SUPPORT-010A", ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/fr/FR-SUPPORT-010A");
    }

    /// <summary>
    /// B7 binding (FR-MCP-TRIAGEREQ-001 ac-2, TEST-MCP-TRIAGEREQ-001 AC-001; triage-report-3bd514bb): get, update and delete of a stored TEST whose legacy id falls
    /// outside the canonical create format reach the TEST route. Data: RecordingHandler answering the legacy TEST;
    /// TestUpdateRequestModel.
    /// </summary>
    [Fact]
    public async Task TestGetUpdateDelete_LegacyId_ReachTheTestRoute()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""{"id":"TEST-SUPPORT-010A-1","title":"t","condition":"c"}""");
        var workflow = BuildWorkflow(handler);

        Assert.Equal("TEST-SUPPORT-010A-1", (await workflow.GetTestAsync("TEST-SUPPORT-010A-1", ct).ConfigureAwait(true)).Id);
        AssertReachedRoute(handler, "/qbrainai/requirements/test/TEST-SUPPORT-010A-1");
        handler.Reset();
        await workflow.UpdateTestAsync(new TestUpdateRequestModel { Id = "TEST-SUPPORT-010A-1", Title = "t" }, ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/test/TEST-SUPPORT-010A-1");
        handler.Reset();
        await workflow.DeleteTestAsync("TEST-SUPPORT-010A-1", ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/test/TEST-SUPPORT-010A-1");
    }

    /// <summary>
    /// B7 binding (triage-report-3bd514bb): update batches carrying legacy FR and TEST ids reach their batch routes with the ids in
    /// the body, as the TR update batch already does. Data: RecordingHandler; one-record FR and TEST batches and a mixed FR and TEST
    /// batch.
    /// </summary>
    [Fact]
    public async Task UpdateBatches_LegacyIds_ReachTheBatchRoutes()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""{"success":true}""");
        var workflow = BuildWorkflow(handler);

        await workflow.UpdateFrBatchAsync(new UpdateFrBatchRequest { Records = [new UpdateFrBatchRecord { Id = "FR-SUPPORT-010A", Title = "t" }] }, ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/fr/batch");
        Assert.Contains("FR-SUPPORT-010A", handler.LastBody, StringComparison.Ordinal);
        handler.Reset();
        await workflow.UpdateTestBatchAsync(new UpdateTestBatchRequest { Records = [new UpdateTestBatchRecord { Id = "TEST-SUPPORT-010A-1", Title = "t" }] }, ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/test/batch");
        Assert.Contains("TEST-SUPPORT-010A-1", handler.LastBody, StringComparison.Ordinal);
        handler.Reset();
        await workflow.UpdateBatchAsync(new UpdateRequirementsBatchRequest
        {
            Records =
            [
                new UpdateRequirementBatchRecord { Kind = "fr", Id = "FR-SUPPORT-010A", Title = "t" },
                new UpdateRequirementBatchRecord { Kind = "test", Id = "TEST-SUPPORT-010A-1", Title = "t" },
            ],
        }, ct).ConfigureAwait(true);
        AssertReachedRoute(handler, "/qbrainai/requirements/batch");
        Assert.Contains("TEST-SUPPORT-010A-1", handler.LastBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// B7 binding (create keeps the canonical format): a TEST create and a mixed create batch with legacy ids are refused before
    /// any request, and a get with an empty id is refused before any request. Data: RecordingHandler.
    /// </summary>
    [Fact]
    public async Task CreatesWithLegacyIdsAndEmptyIdLookups_AreRefusedBeforeAnyRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""{"id":"x"}""");
        var workflow = BuildWorkflow(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => workflow.CreateTestAsync(
            new TestCreateRequestModel { Id = "TEST-SUPPORT-010A-1", Title = "t", Description = "c" }, ct)).ConfigureAwait(true);
        await Assert.ThrowsAsync<ArgumentException>(() => workflow.CreateBatchAsync(new CreateRequirementsBatchRequest
        {
            Records = [new CreateRequirementBatchRecord { Kind = "fr", Id = "FR-SUPPORT-010A", Title = "t", Body = "b" }],
        }, ct)).ConfigureAwait(true);
        await Assert.ThrowsAsync<ArgumentException>(() => workflow.GetFrAsync(" ", ct)).ConfigureAwait(true);
        await Assert.ThrowsAsync<ArgumentException>(() => workflow.GetTestAsync(string.Empty, ct)).ConfigureAwait(true);
        Assert.Null(handler.LastRequest);
    }

    /// <summary>B7 mock gate (characterization, kept after the fix): create still refuses an id outside the canonical format before any request.</summary>
    [Fact]
    public async Task CreateFrAsync_LegacyId_IsRefusedBeforeAnyRequest()
    {
        var handler = new RecordingHandler("""{"id":"FR-SUPPORT-010A","title":"t","body":"b"}""");
        var workflow = BuildWorkflow(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => workflow.CreateFrAsync(
                new FrCreateRequestModel { Id = "FR-SUPPORT-010A", Title = "t", Description = "b" },
                TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        Assert.Null(handler.LastRequest);
    }

    /// <summary>B3 mock gate (characterization): formats and doc types that bind correctly today are valid; an unknown format is not.</summary>
    [Fact]
    public void GenerateDocument_YamlTrIsValidAndUnknownFormatIsInvalid()
    {
        Assert.True(ValidateGenerateDocument("yaml", "tr").IsValid);
        Assert.True(ValidateGenerateDocument("wiki", "all").IsValid);
        Assert.False(ValidateGenerateDocument("html", "tr").IsValid);
    }

    /// <summary>
    /// B3 binding (triage-report-c03d190f; TR-MCP-REQEXPORT-003 AC-001, TEST-MCP-REQEXPORT-003 AC-001): generateDocument accepts every documented format
    /// with every documented docType. Data: the three formats and nine doc types the REPL documents.
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentedFormatAndDocTypes))]
    public void GenerateDocument_EveryDocumentedFormatAndDocType_IsValid(string format, string docType)
    {
        var result = ValidateGenerateDocument(format, docType);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    /// <summary>
    /// B3 binding (triage-report-c03d190f): an unknown format or docType is refused with an error that names its own
    /// payload.params path and lists every documented value. Data: format html; docType epic.
    /// </summary>
    [Fact]
    public void GenerateDocument_UnknownFormatOrDocType_NamesThePathAndEveryValue()
    {
        var format = Assert.Single(ValidateGenerateDocument("html", "fr").Errors);
        Assert.Equal("payload.params.format must be one of: markdown, yaml, wiki.", format);
        var docType = Assert.Single(ValidateGenerateDocument("markdown", "epic").Errors);
        Assert.Equal("payload.params.docType must be one of: fr, tr, test, matrix, all, functional, technical, testing, mapping.", docType);
    }

    /// <summary>The documented generateDocument formats crossed with the documented doc types.</summary>
    public static TheoryData<string, string> DocumentedFormatAndDocTypes()
    {
        var data = new TheoryData<string, string>();
        foreach (var format in new[] { "markdown", "yaml", "wiki" })
        {
            foreach (var docType in new[] { "fr", "tr", "test", "matrix", "all", "functional", "technical", "testing", "mapping" })
                data.Add(format, docType);
        }

        return data;
    }

    /// <summary>
    /// B2 mock gate (triage-report-b72cfaf4; TR-MCP-REQ-003 AC-001): the stored-timestamp assertion accepts createdAt and updatedAt that
    /// are the stored values in round-trip format and rejects values that report the time of the read.
    /// Data: two fixed stored timestamps.
    /// </summary>
    [Fact]
    public void StoredTimestampAssertion_AcceptsStoredValuesAndRejectsReadTime()
    {
        var created = new DateTimeOffset(2026, 10, 10, 1, 2, 3, TimeSpan.Zero);
        var updated = new DateTimeOffset(2026, 10, 11, 4, 5, 6, TimeSpan.Zero);
        AssertStoredTimestamps(created.ToString("o", System.Globalization.CultureInfo.InvariantCulture), updated.ToString("o", System.Globalization.CultureInfo.InvariantCulture), created, updated);
        var readTime = DateTimeOffset.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertStoredTimestamps(readTime, readTime, created, updated));
    }

    /// <summary>
    /// B2 binding (triage-report-b72cfaf4, getTr and getTest; TR-MCP-REQ-003 AC-001): TR and TEST items read through the REPL report the createdAtUtc and
    /// updatedAtUtc the server sent, and an entry without them reports empty text, never the time of the read. Data: RecordingHandler
    /// answers carrying 2026-10-10T01:02:03Z and 2026-10-11T04:05:06Z, then a TR answer without timestamps.
    /// </summary>
    [Fact]
    public async Task GetItems_ReportTheStoredTimestamps()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = new DateTimeOffset(2026, 10, 10, 1, 2, 3, TimeSpan.Zero);
        var updated = new DateTimeOffset(2026, 10, 11, 4, 5, 6, TimeSpan.Zero);
        var stamps = "\"createdAtUtc\":\"" + created.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
            + "\",\"updatedAtUtc\":\"" + updated.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + "\"";

        var tr = await BuildWorkflow(new RecordingHandler("{\"id\":\"TR-066\",\"title\":\"t\",\"body\":\"b\"," + stamps + "}"))
            .GetTrAsync("TR-066", ct).ConfigureAwait(true);
        AssertStoredTimestamps(tr.CreatedAt, tr.UpdatedAt, created, updated);
        var test = await BuildWorkflow(new RecordingHandler("{\"id\":\"TEST-MCP-001\",\"title\":\"t\",\"condition\":\"c\"," + stamps + "}"))
            .GetTestAsync("TEST-MCP-001", ct).ConfigureAwait(true);
        AssertStoredTimestamps(test.CreatedAt, test.UpdatedAt, created, updated);

        var bare = await BuildWorkflow(new RecordingHandler("""{"id":"TR-066","title":"t","body":"b"}""")).GetTrAsync("TR-066", ct).ConfigureAwait(true);
        Assert.Equal(string.Empty, bare.CreatedAt);
        Assert.Equal(string.Empty, bare.UpdatedAt);
    }

    // B2: an item's createdAt and updatedAt are exactly the stored timestamps.
    private static void AssertStoredTimestamps(string createdAt, string updatedAt, DateTimeOffset created, DateTimeOffset updated)
    {
        Assert.Equal(created, DateTimeOffset.Parse(createdAt, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(updated, DateTimeOffset.Parse(updatedAt, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static ReplYamlMessageValidationResult ValidateGenerateDocument(string format, string docType)
        => ReplYamlMessageValidator.ValidateRequest(new RequestPayload
        {
            RequestId = "req-generate-document-001",
            Method = RequirementsCommandShapes.GenerateDocumentMethod,
            Params = new Dictionary<string, object?> { ["format"] = format, ["docType"] = docType },
        });

    private static RequirementsWorkflow BuildWorkflow(HttpMessageHandler handler)
        => new(new RequirementsClient(new HttpClient(handler), new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi",
        }));

    // B7: the workflow call reached exactly this client route.
    private static void AssertReachedRoute(RecordingHandler handler, string absolutePath)
    {
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(absolutePath, handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    private sealed class RecordingHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        public void Reset()
        {
            LastRequest = null;
            LastBody = string.Empty;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
