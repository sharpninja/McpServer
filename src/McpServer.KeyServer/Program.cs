using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddTransactionKeyServer(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapPost(
    "/mcpserver/keyserver/parties",
    async (PartyRegistrationRequest request, IKeyServerPartyRegistry registry, CancellationToken cancellationToken)
        => Results.Ok(await registry.RegisterPartyAsync(request, cancellationToken).ConfigureAwait(false)));

app.MapGet(
    "/mcpserver/keyserver/parties/{partyId}/keys/{keyId}",
    async (string partyId, string keyId, IKeyServerPartyRegistry registry, CancellationToken cancellationToken) =>
    {
        var key = await registry.GetPartyKeyAsync(partyId, keyId, cancellationToken).ConfigureAwait(false);
        return key is null
            ? Results.NotFound(new { error = $"Key '{keyId}' for party '{partyId}' was not found." })
            : Results.Ok(key);
    });

app.MapPost(
    "/mcpserver/keyserver/manifests/sign",
    async (TransactionManifestSignRequest request, IKeyServerManifestService manifests, CancellationToken cancellationToken) =>
    {
        var response = await manifests.SignManifestAsync(request, cancellationToken).ConfigureAwait(false);
        return response.Success ? Results.Ok(response) : Results.BadRequest(response);
    });

app.MapPost(
    "/mcpserver/keyserver/manifests/verify",
    async (TransactionManifestVerifyRequest request, IKeyServerManifestService manifests, CancellationToken cancellationToken) =>
    {
        var response = await manifests.VerifyManifestAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsValid ? Results.Ok(response) : Results.BadRequest(response);
    });

app.Run();

/// <summary>Marker type for keyserver WebApplicationFactory integration tests.</summary>
public sealed class KeyServerEntryPoint
{
}
