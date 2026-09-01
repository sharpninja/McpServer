using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// QuadBrain suite-load race: recording role before CompleteAsync and output after
/// can zip-mismatch under overlap. Atomic post-await recording cannot.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AtomicBrainSlotInvocationRecorderTests
{
    /// <summary>Prior race: overlapping completions pair the second output with the first role.</summary>
    [Fact]
    public async Task SplitRecord_OverlappingCompletions_ZipsMismatchedPairs()
    {
        var recorder = new AtomicBrainSlotInvocationRecorder();
        var firstRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = recorder.WrapSplit(new GatedClient("Creativity-out", firstStarted, firstRelease));
        var second = recorder.WrapSplit(new ImmediateClient("Logic-out"));

        var firstTask = first.CompleteAsync(Slot("Creativity"), "in", temperature: null, TestContext.Current.CancellationToken);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await second.CompleteAsync(Slot("Logic"), "in", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
        firstRelease.TrySetResult();
        await firstTask.ConfigureAwait(true);

        Assert.Equal(["Creativity", "Logic"], recorder.Roles);
        Assert.Equal(["Logic-out", "Creativity-out"], recorder.Outputs);
        Assert.NotEqual(recorder.Roles[0] + "-out", recorder.Outputs[0]);
    }

    /// <summary>Current contract: role and output are written together after CompleteAsync.</summary>
    [Fact]
    public async Task AtomicRecord_OverlappingCompletions_KeepsRoleOutputPairs()
    {
        var recorder = new AtomicBrainSlotInvocationRecorder();
        var firstRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = recorder.WrapAtomic(new GatedClient("Creativity-out", firstStarted, firstRelease));
        var second = recorder.WrapAtomic(new ImmediateClient("Logic-out"));

        var firstTask = first.CompleteAsync(Slot("Creativity"), "in", temperature: null, TestContext.Current.CancellationToken);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await second.CompleteAsync(Slot("Logic"), "in", temperature: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
        firstRelease.TrySetResult();
        await firstTask.ConfigureAwait(true);

        Assert.Equal(2, recorder.Roles.Count);
        Assert.Equal(recorder.Roles.Count, recorder.Outputs.Count);
        for (var i = 0; i < recorder.Roles.Count; i++)
            Assert.Equal(recorder.Roles[i] + "-out", recorder.Outputs[i]);
    }

    private static BrainSlotDefinitionEntity Slot(string role)
        => new()
        {
            SlotId = role.ToLowerInvariant(),
            Role = role,
            ProviderKind = "OpenAICompatible",
            ModelId = "test-model",
            Endpoint = "http://127.0.0.1:11434",
        };

    private sealed class ImmediateClient(string output) : IBrainSlotChatClient
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
            => Task.FromResult(output);
    }

    private sealed class GatedClient(
        string output,
        TaskCompletionSource started,
        TaskCompletionSource release) : IBrainSlotChatClient
    {
        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return output;
        }
    }
}
