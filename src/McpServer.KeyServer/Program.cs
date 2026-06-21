using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Hosting.WindowsServices;

if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
}

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddTransactionKeyServer(builder.Configuration);

if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "McpServerKeyServer";
    });
}

var app = builder.Build();
app.MapDefaultEndpoints();
await app.Services.ProvisionConfiguredTransactionKeysAsync(app.Lifetime.ApplicationStopping).ConfigureAwait(false);

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

app.MapGet(
    "/mcpserver/keyserver/manifests/report",
    async (
        string? publisherPartyId,
        string? subscriberPartyId,
        string? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? limit,
        IKeyServerManifestService manifests,
        CancellationToken cancellationToken) =>
    {
        var report = await manifests.GetManifestReportAsync(
            new TransactionManifestTraceReportRequest
            {
                PublisherPartyId = publisherPartyId,
                SubscriberPartyId = subscriberPartyId,
                Status = status,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Limit = limit,
            },
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(report);
    });

app.MapGet(
    "/mcpserver/keyserver/manifests/{transactionId}",
    async (string transactionId, IKeyServerManifestService manifests, CancellationToken cancellationToken) =>
    {
        var manifest = await manifests.GetManifestAsync(transactionId, cancellationToken).ConfigureAwait(false);
        return manifest is null
            ? Results.NotFound(new { error = $"Manifest '{transactionId}' was not found." })
            : Results.Ok(manifest);
    });

app.Run();

/// <summary>Marker type for keyserver WebApplicationFactory integration tests.</summary>
public sealed class KeyServerEntryPoint
{
}
