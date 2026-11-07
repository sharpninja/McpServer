using System.Net;
using System.Text;

namespace QBrainAi.TestSupport.Handoff;

/// <summary>
/// TEST-HANDOFF-006: an in-memory HTTP handler for the handoff client: it records each request's method, path and body
/// and answers every request with one successful handoff response, so the handoff skill tests assert on the calls the
/// typed client sends without a server. Shared by link (tests/Directory.Build.targets) between the unit and the
/// integration halves of HandoffSkillDelegationTests.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from the private nested class HandoffSkillDelegationTests.RecordingHandler of QBrainAi.Repl.Core.Tests when that class split into a unit class there and an integration class in QBrainAi.Repl.IntegrationTests (slice S3, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); behavior unchanged.</remarks>
internal sealed class RecordingHandler : HttpMessageHandler
{
    /// <summary>Every request the handler received, in order: its method, absolute path and body (null when it has none).</summary>
    public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = [];

    /// <summary>Records the request and answers 200 OK with a successful handoff response body.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancels reading the request body.</param>
    /// <returns>The successful handoff response.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        Requests.Add((request.Method, request.RequestUri!.AbsolutePath, body));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"created":false,"replayed":false,"requiresReview":false,"diagnostics":[]}""",
                Encoding.UTF8,
                "application/json"),
        };
    }
}
