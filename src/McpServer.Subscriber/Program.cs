using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHttpTransactionSubscriber(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapPost(
    "/mcpserver/subscriber/diffgrams/commit",
    async (DiffgramCommitRequest request, ISubscriberCommitService subscriber, CancellationToken cancellationToken) =>
    {
        var response = await subscriber.CommitDiffgramAsync(request, cancellationToken).ConfigureAwait(false);
        return string.Equals(response.Status, "rejected", StringComparison.OrdinalIgnoreCase)
            ? Results.BadRequest(response)
            : Results.Ok(response);
    });

app.MapGet(
    "/mcpserver/subscriber/transactions/{transactionId}/status",
    async (string transactionId, ISubscriberCommitService subscriber, CancellationToken cancellationToken) =>
    {
        var response = await subscriber.GetTransactionStatusAsync(transactionId, cancellationToken).ConfigureAwait(false);
        return response is null
            ? Results.NotFound(new { error = $"Transaction '{transactionId}' was not found." })
            : Results.Ok(response);
    });

app.MapPost(
    "/mcpserver/subscriber/transactions/{transactionId}/abort",
    async (string transactionId, TransactionAbortRequest request, ISubscriberCommitService subscriber, CancellationToken cancellationToken)
        => Results.Ok(await subscriber.AbortTransactionAsync(transactionId, request, cancellationToken).ConfigureAwait(false)));

app.Run();

/// <summary>Marker type for subscriber WebApplicationFactory integration tests.</summary>
public sealed class SubscriberEntryPoint
{
}
