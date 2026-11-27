using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.TestContracts;
using static QBrainAi.Support.Mcp.TestContracts.DefaultKeyRegistrationContract;

namespace QBrainAi.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Binds the proved Director registration matrix to the isolated QBrain HTTP pipeline.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Use the actual
/// issued key, actual transport details and both registry and marker observations.</remarks>
[Trait("Category", "Integration")]
public sealed class DefaultKeyRegistrationMatrixTests(
    CustomWebApplicationFactory fixture,
    DefaultKeyRegistrationLegacyHost legacyHost)
    : IClassFixture<CustomWebApplicationFactory>, IClassFixture<DefaultKeyRegistrationLegacyHost>
{
    /// <summary>Executes the same request and checks the same observations proved by the mock gate.</summary>
    [Theory]
    [MemberData(nameof(CaseIds), MemberType = typeof(DefaultKeyRegistrationContract))]
    public async Task IssuedKeyMatchesRegistrationContract(string caseId)
    {
        var scenario = Get(caseId);
        var ct = TestContext.Current.CancellationToken;
        if (scenario.Request.Socket)
        {
            await using var socketFixture = new CustomWebApplicationFactory();
            socketFixture.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            Verify(scenario, await ObserveAsync(socketFixture, scenario.Request, ct));
            return;
        }

        var testHost = scenario.Id.StartsWith("mcpserver/", StringComparison.Ordinal) ? legacyHost.Factory : fixture;
        Verify(scenario, await ObserveAsync(testHost, scenario.Request, ct));
    }

    /// <summary>Observes issued credentials, individual transport interactions and durable side effects.</summary>
    private static async Task<Evidence> ObserveAsync(
        CustomWebApplicationFactory factory, RequestShape input, CancellationToken ct)
    {
        using var client = factory.CreateClient();
        if (input.Socket)
            Assert.True(client.BaseAddress!.IsLoopback);

        using var issued = await client.GetAsync(new Uri("/api-key", UriKind.Relative), ct);
        var interactions = new List<string> { "GET /api-key" };
        var key = await ReadIssuedKeyAsync(issued, ct);
        var path = Path.Combine(factory.WorkspacePath, $"director-matrix-{Guid.NewGuid():N}");
        var existingPath = factory.WorkspacePath;
        var encodedExisting = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(existingPath));
        await using var scope = factory.Services.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var before = await ReadWorkspaceAsync(workspaces, path, ct);
        var baseline = await ReadExistingAsync(workspaces, existingPath, ct);
        interactions.Add("inspect-before");

        var target = input.Route.Replace("{key}", encodedExisting, StringComparison.Ordinal);
        if (input.Key == KeyPlacement.Query)
            target = QueryHelpers.AddQueryString(target, "api_key", key);

        using var packet = new HttpRequestMessage(new HttpMethod(input.Method), new Uri(client.BaseAddress!, target));
        var headers = input.Key switch
        {
            KeyPlacement.Header => new[] { key },
            KeyPlacement.Invalid => new[] { Guid.NewGuid().ToString("N") },
            KeyPlacement.Duplicate => new[] { key, key },
            _ => Array.Empty<string>()
        };
        if (headers.Length > 0)
            Assert.True(packet.Headers.TryAddWithoutValidation("X-Api-Key", headers));

        var body = BuildBody(input, path);
        if (body is not null)
        {
            packet.Content = new ByteArrayContent(body);
            packet.Content.Headers.ContentType = new("application/json");
        }

        var peer = input.Socket ? client.BaseAddress!.Host : input.Peer;
        var request = ObserveRequest(packet, body, peer, key, encodedExisting, input.Socket);
        var status = input.Socket
            ? await SendSocketAsync(client, packet, ct)
            : await SendTestServerAsync(factory, packet, body, peer, ct);
        interactions.Add("send");

        var after = await ReadWorkspaceAsync(workspaces, path, ct);
        var existingAfter = await ReadExistingAsync(workspaces, existingPath, ct);
        interactions.Add("inspect-after");
        return new(issued.StatusCode, !string.IsNullOrWhiteSpace(key), interactions, request,
            status, before, after, baseline == existingAfter);
    }

    /// <summary>Reads an issued key without including its value in assertion output.</summary>
    private static async Task<string> ReadIssuedKeyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode != HttpStatusCode.OK)
            return string.Empty;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return json.RootElement.GetProperty("apiKey").GetString() ?? string.Empty;
    }

    /// <summary>Serializes the shared test input through the real JSON request boundary.</summary>
    private static byte[]? BuildBody(RequestShape input, string path)
    {
        if (input.Method == "GET")
            return null;

        var document = new Dictionary<string, object?>
        {
            ["workspacePath"] = path,
            ["name"] = WorkspaceName
        };
        if (input.Enabled is { } enabled)
            document["isEnabled"] = enabled;
        if (input.ForbiddenField is { } field)
            document[field] = field == "isPrimary" ? false : null;
        return JsonSerializer.SerializeToUtf8Bytes(document);
    }

    /// <summary>Captures the outgoing HTTP shape without storing or printing an API key.</summary>
    private static RequestShape ObserveRequest(
        HttpRequestMessage packet, byte[]? body, string? peer, string issuedKey, string encodedExisting, bool socket)
    {
        var uri = packet.RequestUri!;
        var headers = packet.Headers.TryGetValues("X-Api-Key", out var values) ? values.ToArray() : [];
        var query = QueryHelpers.ParseQuery(uri.Query);
        var issuedHeaderCount = headers.Count(value => string.Equals(value, issuedKey, StringComparison.Ordinal));
        var placement = (headers.Length, issuedHeaderCount, query.TryGetValue("api_key", out var queryKey)) switch
        {
            (1, 1, false) => KeyPlacement.Header,
            (1, 0, false) => KeyPlacement.Invalid,
            (2, 2, false) => KeyPlacement.Duplicate,
            (0, 0, true) when queryKey.Count == 1 &&
                string.Equals(queryKey[0], issuedKey, StringComparison.Ordinal) => KeyPlacement.Query,
            _ => throw new InvalidOperationException("The registration request used an unexpected credential shape.")
        };

        var route = uri.AbsolutePath.Replace(encodedExisting, "{key}", StringComparison.Ordinal);
        if (body is null)
            return new(packet.Method.Method, route, peer, placement, Socket: socket);

        using var json = JsonDocument.Parse(body);
        bool? enabled = json.RootElement.TryGetProperty("isEnabled", out var value) ? value.GetBoolean() : null;
        var extra = json.RootElement.EnumerateObject().SingleOrDefault(property =>
            property.Name is not ("workspacePath" or "name" or "isEnabled"));
        var field = extra.Equals(default(JsonProperty)) ? null : extra.Name;
        return new(packet.Method.Method, route, peer, placement, field, enabled, socket);
    }

    /// <summary>Uses Kestrel over the same listener that issued the key.</summary>
    private static async Task<HttpStatusCode> SendSocketAsync(
        HttpClient client, HttpRequestMessage packet, CancellationToken ct)
    {
        using var response = await client.SendAsync(packet, ct);
        return response.StatusCode;
    }

    /// <summary>Sends through the real QBrain pipeline while controlling the test transport's peer.</summary>
    private static async Task<HttpStatusCode> SendTestServerAsync(
        CustomWebApplicationFactory factory, HttpRequestMessage packet, byte[]? body, string? peer, CancellationToken ct)
    {
        using var stream = body is null ? null : new MemoryStream(body);
        var uri = packet.RequestUri!;
        var response = await factory.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = peer is null ? null : IPAddress.Parse(peer);
            context.Request.Method = packet.Method.Method;
            context.Request.Path = uri.AbsolutePath;
            context.Request.QueryString = new QueryString(uri.Query);
            if (packet.Headers.TryGetValues("X-Api-Key", out var values))
                context.Request.Headers["X-Api-Key"] = new StringValues(values.ToArray());
            if (stream is not null)
            {
                context.Request.Body = stream;
                context.Request.ContentType = packet.Content!.Headers.ContentType!.MediaType;
                context.Request.ContentLength = stream.Length;
            }
        }, ct);
        return (HttpStatusCode)response.Response.StatusCode;
    }

    /// <summary>Reads registry and marker state separately from the HTTP response.</summary>
    private static async Task<WorkspaceState> ReadWorkspaceAsync(
        IWorkspaceService workspaces, string path, CancellationToken ct)
    {
        var workspace = await workspaces.GetAsync(path, ct);
        return new(workspace is not null, Directory.Exists(path), File.Exists(MarkerPath(path)),
            workspace?.Name, workspace?.IsEnabled, workspace?.IsPrimary);
    }

    /// <summary>Captures the existing workspace and its marker fingerprint to detect write side effects.</summary>
    private static async Task<(WorkspaceState State, string? MarkerDigest)> ReadExistingAsync(
        IWorkspaceService workspaces, string path, CancellationToken ct)
    {
        var state = await ReadWorkspaceAsync(workspaces, path, ct);
        Assert.True(state.Registered);
        if (!state.HasMarker)
            return (state, null);

        return (state, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(MarkerPath(path), ct))));
    }

    /// <summary>Locates only the existing isolated marker, without disclosing its contents.</summary>
    private static string MarkerPath(string path) => Path.Combine(path, "AGENTS-README-FIRST.yaml");
}


/// <summary>Owns a separate QBrain host so each alias stays within real key issuance limits.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Keep production throttling active.</remarks>
public sealed class DefaultKeyRegistrationLegacyHost : IAsyncDisposable
{
    /// <summary>Gets the isolated host for the legacy product prefix.</summary>
    internal CustomWebApplicationFactory Factory { get; } = new();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}