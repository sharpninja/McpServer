using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class AuthConfigClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147")
    };

    [Fact]
    public async System.Threading.Tasks.Task RequestDeviceAuthorizationAsync_PostsFormBodyWithoutApiKey()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"device_code":"device-1","user_code":"ABCD","verification_uri":"http://localhost/auth/ui/device","expires_in":600,"interval":5}""");
        using var http = new HttpClient(handler);
        var client = new AuthConfigClient(http, DefaultOptions);

        var result = await client.RequestDeviceAuthorizationAsync(new AuthDeviceAuthorizationRequest
        {
            ClientId = "director",
            Scope = "openid offline_access"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/auth/device", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("client_id=director", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("scope=openid+offline_access", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.False(handler.LastRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal("device-1", result.DeviceCode);
        Assert.Equal(5, result.Interval);
    }

    [Fact]
    public async System.Threading.Tasks.Task RequestTokenAsync_PostsFormBodyWithoutApiKey()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"access_token":"access-1","token_type":"Bearer","expires_in":3600,"refresh_token":"refresh-1","scope":"openid offline_access"}""");
        using var http = new HttpClient(handler);
        var client = new AuthConfigClient(http, DefaultOptions);

        var result = await client.RequestTokenAsync(new AuthTokenRequest
        {
            GrantType = "urn:ietf:params:oauth:grant-type:device_code",
            ClientId = "director",
            DeviceCode = "device-1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/auth/token", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Adevice_code", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("client_id=director", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("device_code=device-1", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.False(handler.LastRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal("access-1", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
    }
}
