using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TEST-HANDOFF-001 / TEST-HANDOFF-006: REST rejects integer handoff enums.</summary>
[Trait("Category", "Integration")]
public sealed class HandoffHttpEnumIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>Creates the REST enum integration tests.</summary>
    public HandoffHttpEnumIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>P1-7: POST /mcpserver/handoff/ingest with mode 999 never creates and returns validation failure.</summary>
    [Fact]
    public async Task Ingest_NumericMode999_DoesNotCreate()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var content = new StringContent(
            """{"sourceKind":"Content","content":"numeric-enum","mode":999}""",
            Encoding.UTF8,
            "application/json");
        using var response = await client.PostAsync(new Uri("/mcpserver/handoff/ingest", UriKind.Relative), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("\"created\":true", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MCP-HANDOFF", body, StringComparison.Ordinal);
    }
}
