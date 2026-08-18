using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Records role, model, endpoint, and output under one lock after CompleteAsync returns.
/// Split Record/RecordOutput across the await is the prior QuadBrain zip race.
/// </summary>
public sealed class AtomicBrainSlotInvocationRecorder
{
    private readonly object _gate = new();
    private readonly List<string> _roles = [];
    private readonly List<string> _outputs = [];

    /// <summary>Recorded roles in completion order.</summary>
    public IReadOnlyList<string> Roles
    {
        get
        {
            lock (_gate)
                return [.. _roles];
        }
    }

    /// <summary>Recorded outputs in completion order.</summary>
    public IReadOnlyList<string> Outputs
    {
        get
        {
            lock (_gate)
                return [.. _outputs];
        }
    }

    /// <summary>Wraps <paramref name="inner"/> so one completion writes role and output together.</summary>
    public IBrainSlotChatClient WrapAtomic(IBrainSlotChatClient inner)
        => new AtomicClient(this, inner);

    /// <summary>Prior race: records the role before the await and the output after.</summary>
    public IBrainSlotChatClient WrapSplit(IBrainSlotChatClient inner)
        => new SplitClient(this, inner);

    internal void RecordPair(string role, string output)
    {
        lock (_gate)
        {
            _roles.Add(role);
            _outputs.Add(output);
        }
    }

    internal void RecordRole(string role)
    {
        lock (_gate)
            _roles.Add(role);
    }

    internal void RecordOutput(string output)
    {
        lock (_gate)
            _outputs.Add(output);
    }

    private sealed class AtomicClient(AtomicBrainSlotInvocationRecorder owner, IBrainSlotChatClient inner) : IBrainSlotChatClient
    {
        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            var output = await inner.CompleteAsync(slot, input, temperature, cancellationToken).ConfigureAwait(false);
            owner.RecordPair(slot.Role, output);
            return output;
        }
    }

    private sealed class SplitClient(AtomicBrainSlotInvocationRecorder owner, IBrainSlotChatClient inner) : IBrainSlotChatClient
    {
        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            owner.RecordRole(slot.Role);
            var output = await inner.CompleteAsync(slot, input, temperature, cancellationToken).ConfigureAwait(false);
            owner.RecordOutput(output);
            return output;
        }
    }
}
