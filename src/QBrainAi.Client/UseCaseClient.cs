using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// TR-MCP-CLIENT-001 / TR-MCP-USECASE-005: Typed client for <c>/qbrainai/usecases</c> endpoints.
/// </summary>
/// <seealso cref="QBrainAiClient.UseCases"/>
public sealed class UseCaseClient : McpClientBase
{
    /// <inheritdoc />
    public UseCaseClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal UseCaseClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists use cases, optionally filtered by title.</summary>
    public async Task<IReadOnlyList<UseCaseSummary>> ListAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl("qbrainai/usecases", ("title", title));
        return await GetAsync<IReadOnlyList<UseCaseSummary>>(url, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Gets a use case aggregate by id.</summary>
    public async Task<UseCaseDetail> GetAsync(long useCaseId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<UseCaseDetail>($"qbrainai/usecases/{useCaseId}", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Creates a use case.</summary>
    public async Task<UseCaseDetail> CreateAsync(CreateUseCaseRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseDetail>("qbrainai/usecases", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates use case header fields.</summary>
    public async Task<UseCaseDetail> UpdateAsync(long useCaseId, UpdateUseCaseRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<UseCaseDetail>($"qbrainai/usecases/{useCaseId}", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Soft-deletes a use case.</summary>
    public async Task DeleteAsync(long useCaseId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(HttpMethod.Delete, $"qbrainai/usecases/{useCaseId}", null, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Adds a flow to a use case.</summary>
    public async Task<UseCaseFlow> AddFlowAsync(long useCaseId, AddUseCaseFlowRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseFlow>($"qbrainai/usecases/{useCaseId}/flows", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Adds a step to a flow.</summary>
    public async Task<UseCaseStep> AddStepAsync(long useCaseId, long flowId, AddUseCaseStepRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseStep>($"qbrainai/usecases/{useCaseId}/flows/{flowId}/steps", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Attaches an actor to a use case.</summary>
    public async Task<UseCaseActor> AttachActorAsync(long useCaseId, AttachUseCaseActorRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseActor>($"qbrainai/usecases/{useCaseId}/actors", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates an actor already associated with a use case.</summary>
    public async Task<UseCaseActor> UpdateActorAsync(
        long useCaseId,
        long actorId,
        UpdateUseCaseActorRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<UseCaseActor>(
            $"qbrainai/usecases/{useCaseId}/actors/{actorId}",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates a flow already owned by a use case.</summary>
    public async Task<UseCaseFlow> UpdateFlowAsync(
        long useCaseId,
        long flowId,
        UpdateUseCaseFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<UseCaseFlow>(
            $"qbrainai/usecases/{useCaseId}/flows/{flowId}",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates a step already owned by a use-case flow.</summary>
    public async Task<UseCaseStep> UpdateStepAsync(
        long useCaseId,
        long flowId,
        long stepId,
        UpdateUseCaseStepRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<UseCaseStep>(
            $"qbrainai/usecases/{useCaseId}/flows/{flowId}/steps/{stepId}",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Links a use case to a functional requirement.</summary>
    public async Task<UseCaseFrLink> LinkFrAsync(long useCaseId, LinkUseCaseToFrRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseFrLink>($"qbrainai/usecases/{useCaseId}/links", request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Unlinks a use case from a functional requirement.</summary>
    public async Task UnlinkFrAsync(long useCaseId, string frId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(
            HttpMethod.Delete,
            $"qbrainai/usecases/{useCaseId}/links/{Uri.EscapeDataString(frId)}",
            null,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Gets a diagram for a use case (sequence default, or kind=usecase for UML graph export).</summary>
    public async Task<UseCaseDiagram> GetDiagramAsync(
        long useCaseId,
        string format = "mermaid",
        string kind = "sequence",
        CancellationToken cancellationToken = default)
    {
        var url = BuildQueryUrl(
            $"qbrainai/usecases/{useCaseId}/diagram",
            ("format", format),
            ("kind", kind));
        return await GetAsync<UseCaseDiagram>(url, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-USECASE-012: Loads the UML diagram graph for the canvas editor.</summary>
    public async Task<UseCaseDiagramGraph> GetDiagramGraphAsync(long useCaseId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<UseCaseDiagramGraph>($"qbrainai/usecases/{useCaseId}/diagram-graph", cancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>FR-MCP-USECASE-012: Saves the UML diagram graph from the canvas editor.</summary>
    public async Task<UseCaseDiagramGraph> PutDiagramGraphAsync(
        long useCaseId,
        UseCaseDiagramGraph graph,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<UseCaseDiagramGraph>(
            $"qbrainai/usecases/{useCaseId}/diagram-graph",
            graph,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Creates a shell use case from a functional requirement.</summary>
    public async Task<UseCaseDetail> CreateFromFrAsync(string frId, CreateUseCaseFromFrRequest? request = null, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseDetail>(
            $"qbrainai/usecases/from-fr/{Uri.EscapeDataString(frId)}",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Reports Realizes UC↔FR coverage gaps.</summary>
    public async Task<UseCaseFrCoverage> GetCoverageAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<UseCaseFrCoverage>("qbrainai/usecases/coverage", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-USECASE-008: Sets approval status for a use case.</summary>
    public async Task<UseCaseDetail> SetApprovalAsync(
        long useCaseId,
        SetUseCaseApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseDetail>(
            $"qbrainai/usecases/{useCaseId}/approval",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-USECASE-009: Sets product membership key for a use case.</summary>
    public async Task<UseCaseDetail> SetProductAsync(
        long useCaseId,
        SetUseCaseProductRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<UseCaseDetail>(
            $"qbrainai/usecases/{useCaseId}/product",
            request,
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-USECASE-009: Lists use cases sharing a product key.</summary>
    public async Task<IReadOnlyList<UseCaseSummary>> ListByProductAsync(
        string productKey,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IReadOnlyList<UseCaseSummary>>(
            $"qbrainai/usecases/by-product/{Uri.EscapeDataString(productKey)}",
            cancellationToken).ConfigureAwait(true);
    }

    private static string BuildQueryUrl(string path, params (string Name, string? Value)[] query)
    {
        var parts = new List<string>();
        foreach (var (name, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }

        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }
}
