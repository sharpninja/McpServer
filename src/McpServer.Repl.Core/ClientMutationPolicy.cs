// FR-MCP-120: Transaction gating for generic REPL client passthrough mutations.
// TR-MCP-TXN-001: Known unsafe client mutations fail closed while required transactions are active.
// TEST-MCP-161: Generic REPL client passthrough policy blocks uncompensated mutation methods.

namespace McpServer.Repl.Core;

/// <summary>
/// Evaluates whether a generic <c>client.*</c> REPL passthrough request may invoke the target client method.
/// </summary>
public interface IClientMutationPolicy
{
    /// <summary>
    /// Evaluates the supplied client method request.
    /// </summary>
    /// <param name="clientName">The sub-client name from the <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c> request.</param>
    /// <param name="methodName">The method name from the <c>client.&lt;clientName&gt;.&lt;methodName&gt;</c> request.</param>
    /// <param name="arguments">The normalized method argument dictionary.</param>
    /// <returns>A policy decision that either allows invocation or rejects it with a stable error code and message.</returns>
    ClientMutationPolicyDecision Evaluate(
        string clientName,
        string methodName,
        IReadOnlyDictionary<string, object?> arguments);
}

/// <summary>
/// Represents the result of a generic client mutation policy evaluation.
/// </summary>
/// <param name="Allowed">Whether the request may continue to the generic client passthrough.</param>
/// <param name="ErrorCode">Stable rejection error code when <paramref name="Allowed"/> is <see langword="false"/>.</param>
/// <param name="Message">Human-readable rejection message when <paramref name="Allowed"/> is <see langword="false"/>.</param>
public sealed record ClientMutationPolicyDecision(bool Allowed, string? ErrorCode = null, string? Message = null)
{
    /// <summary>
    /// Creates an allow decision.
    /// </summary>
    /// <returns>An allow decision.</returns>
    public static ClientMutationPolicyDecision Allow() => new(true);

    /// <summary>
    /// Creates a reject decision.
    /// </summary>
    /// <param name="errorCode">Stable rejection error code.</param>
    /// <param name="message">Human-readable rejection message.</param>
    /// <returns>A reject decision.</returns>
    public static ClientMutationPolicyDecision Reject(string errorCode, string message) => new(false, errorCode, message);
}

/// <summary>
/// Exception thrown when a direct generic client passthrough invocation is rejected by <see cref="IClientMutationPolicy"/>.
/// </summary>
public sealed class ClientMutationPolicyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientMutationPolicyException"/> class.
    /// </summary>
    /// <param name="clientName">The rejected client name.</param>
    /// <param name="methodName">The rejected method name.</param>
    /// <param name="decision">The policy rejection decision.</param>
    public ClientMutationPolicyException(
        string clientName,
        string methodName,
        ClientMutationPolicyDecision decision)
        : base(string.IsNullOrWhiteSpace(decision.Message)
            ? $"Client method client.{clientName}.{methodName} is blocked by mutation policy."
            : decision.Message)
    {
        ClientName = clientName;
        MethodName = methodName;
        ErrorCode = string.IsNullOrWhiteSpace(decision.ErrorCode)
            ? "mutation_not_transactional"
            : decision.ErrorCode;
    }

    /// <summary>Gets the rejected client name.</summary>
    public string ClientName { get; }

    /// <summary>Gets the rejected method name.</summary>
    public string MethodName { get; }

    /// <summary>Gets the stable policy error code.</summary>
    public string ErrorCode { get; }
}

/// <summary>
/// Describes the current transaction-gating state used by <see cref="KnownUnsafeClientMutationPolicy"/>.
/// </summary>
/// <param name="RequiredForMutations">Whether mutation transactions are currently required for mutating methods.</param>
/// <param name="Degraded">Whether the transaction coordinator is degraded and cannot safely evaluate mutating methods.</param>
/// <param name="Message">Optional coordinator status message.</param>
public sealed record ClientMutationPolicyState(
    bool RequiredForMutations,
    bool Degraded = false,
    string? Message = null);

/// <summary>
/// Blocks known unsafe generic client passthrough mutations while transaction gating is required.
/// </summary>
public sealed class KnownUnsafeClientMutationPolicy : IClientMutationPolicy
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReadMethods =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["context"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SearchAsync",
                "PackAsync",
                "ListSourcesAsync",
                "GraphRagStatusAsync",
                "GraphRagQueryAsync",
                "GraphRagListDocumentsAsync",
                "GraphRagGetDocumentChunksAsync",
                "GraphRagListEntitiesAsync",
                "GraphRagGetEntityAsync",
                "GraphRagListRelationshipsAsync",
                "GraphRagGetRelationshipAsync",
            },
            ["graphRag"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "StatusAsync",
                "QueryAsync",
                "ListDocumentsAsync",
                "GetDocumentChunksAsync",
                "ListEntitiesAsync",
                "GetEntityAsync",
                "ListRelationshipsAsync",
                "GetRelationshipAsync",
            },
            ["todo"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "QueryAsync",
                "GetAsync",
                "GetAuditAsync",
                "GetProjectionStatusAsync",
                "GetActiveTodoAsync",
                "GetNextReadyTodoAsync",
                "GetExecutionContextAsync",
                "GetDeltaContextAsync",
            },
            ["federation"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetStatusAsync",
                "ListProxiesAsync",
                "ListWorkspacesAsync",
                "GetQueueStatusAsync",
                "ListConflictsAsync",
                "GetAdapterCoverageAsync",
                "GetSyncItemsAsync",
                "ListTargetsAsync",
            },
            ["keyServer"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetManifestAsync",
                "GetManifestReportAsync",
                "GetPartyKeyAsync",
            },
            ["subscriber"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetTransactionStatusAsync",
            },
        };

    private static readonly IReadOnlySet<string> ServiceGatedClientNamespaces =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "agentPool",
            "gitHub",
            "memory",
            "repo",
            "requirements",
            "sessionLog",
            "template",
            "tools",
            "voice",
        };

    private readonly Func<ClientMutationPolicyState> _stateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnownUnsafeClientMutationPolicy"/> class.
    /// </summary>
    /// <param name="stateProvider">Provides the current transaction-gating state.</param>
    public KnownUnsafeClientMutationPolicy(Func<ClientMutationPolicyState> stateProvider)
    {
        _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
    }

    /// <inheritdoc />
    public ClientMutationPolicyDecision Evaluate(
        string clientName,
        string methodName,
        IReadOnlyDictionary<string, object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(arguments);

        if (ReadMethods.TryGetValue(clientName, out var methods) && methods.Contains(methodName))
            return ClientMutationPolicyDecision.Allow();

        var state = _stateProvider();
        if (state.Degraded)
        {
            return ClientMutationPolicyDecision.Reject(
                "transaction_degraded",
                string.IsNullOrWhiteSpace(state.Message)
                    ? "Turn transaction coordinator is degraded."
                    : state.Message);
        }

        if (!state.RequiredForMutations)
            return ClientMutationPolicyDecision.Allow();

        if (!ReadMethods.ContainsKey(clientName) && ServiceGatedClientNamespaces.Contains(clientName))
            return ClientMutationPolicyDecision.Allow();

        return ClientMutationPolicyDecision.Reject(
            "mutation_not_transactional",
            $"Client method client.{clientName}.{methodName} is blocked because required turn transactions are active and this protected client method is not classified as a safe read.");
    }
}
