using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client.Tests;

/// <summary>Mock HTTP handler for unit testing client methods.</summary>
internal sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    private HttpRequestMessage? _lastRequest;
    private string? _lastRequestBody;

    public MockHttpHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    public HttpRequestMessage? LastRequest => _lastRequest;
    public string? LastRequestBody => _lastRequestBody;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _lastRequest = request;
        if (request.Content is not null)
            _lastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };
    }
}
