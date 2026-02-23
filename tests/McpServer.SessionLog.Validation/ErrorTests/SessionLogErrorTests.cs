using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.ErrorTests;

[Collection("SessionLogEndpoint")]
public sealed class SessionLogErrorTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SessionLogErrorTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Submit_MissingSourceType_Returns400()
    {
        var payload = new
        {
            sessionId = SessionLogEndpointFixture.GenerateSessionId(),
            title = "Missing source type",
            model = "test",
            status = "completed",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResult>(JsonOpts);
        Assert.Contains("SourceType", err!.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Submit_MissingSessionId_Returns400()
    {
        var payload = new
        {
            sourceType = "AuditTest",
            title = "Missing session ID",
            model = "test",
            status = "completed",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResult>(JsonOpts);
        Assert.Contains("SessionId", err!.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Submit_EmptySourceType_Returns400()
    {
        var payload = new
        {
            sourceType = "",
            sessionId = SessionLogEndpointFixture.GenerateSessionId(),
            title = "Empty source type",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_EmptySessionId_Returns400()
    {
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId = "",
            title = "Empty session ID",
            entryCount = 0
        };

        var response = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AppendDialog_EmptyItemsList_Returns400()
    {
        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/AuditTest/some-session/some-request/dialog";
        var emptyItems = Array.Empty<object>();
        var response = await _fixture.Client.PostAsJsonAsync(dialogRoute, emptyItems);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_InvalidJsonBody_Returns400()
    {
        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");
        var response = await _fixture.Client.PostAsync(SessionLogEndpointFixture.SessionLogRoute, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
