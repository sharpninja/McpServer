// SPDX-License-Identifier: Apache-2.0
using System.Text;
using System.Text.Json;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// TEST-QB-069: observes actual parser/dispatcher dependency arguments for the common
/// route contract. Scripted dependencies record effects; they do not implement a lifecycle.
/// Native composition and canonical storage require their separate real bindings.
/// </summary>
public sealed class GovernedSessionRouteFixture
{
    private readonly Dictionary<string, string> _capture;
    private readonly bool _governed;
    private readonly List<object> _effects = [];
    private string _rawParams = "";

    /// <summary>Captures fixture-owned trusted roots separately from untrusted envelope parameters.</summary>
    /// <param name="trustedCaptureJson">Trusted constructor metadata supplied by the common runner.</param>
    /// <param name="governed">Whether the host constructor supplies a governed adapter.</param>
    public GovernedSessionRouteFixture(string trustedCaptureJson, bool governed)
    {
        _capture = JsonSerializer.Deserialize<Dictionary<string, string>>(trustedCaptureJson)
            ?? throw new ArgumentException("Trusted capture required", nameof(trustedCaptureJson));
        _governed = governed;
    }

    /// <summary>Runs the actual JSON-compatible YAML parser and actual dispatcher, retaining callback effects.</summary>
    /// <param name="method">Actual method from the common scenario.</param>
    /// <param name="paramsJson">Original exact JSON parameters from the common scenario.</param>
    /// <returns>The actual dispatcher result type; errors remain errors instead of manufactured effects.</returns>
    public async Task<string> InvokeAsync(string method, string paramsJson)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "request");
            writer.WriteStartObject("payload");
            writer.WriteString("requestId", "fixture-correlation");
            writer.WriteString("method", method);
            writer.WritePropertyName("params");
            writer.WriteRawValue(paramsJson);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var serializer = new YamlSerializer();
        var envelope = serializer.Deserialize(Encoding.UTF8.GetString(stream.ToArray()));
        _rawParams = (envelope.Payload as ILosslessRequestPayload)?.RawParamsJson
            ?? throw new InvalidOperationException("Actual parser did not retain original JSON parameters");
        var workflow = Substitute.For<ISessionLogWorkflow>();
        workflow.AppendActionsAsync(Arg.Any<IReadOnlyList<ISessionAction>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var items = call.Arg<IReadOnlyList<ISessionAction>>() ?? throw new InvalidOperationException("Actual action callback received null items");
                Observe("legacy", "workflow.sessionlog.appendActions", _rawParams,
                    items.Select(item => new Dictionary<string, object?>
                    {
                        ["description"] = item.Description, ["order"] = item.Order,
                        ["type"] = item.Type, ["status"] = item.Status, ["filePath"] = item.FilePath,
                    }).ToArray());
                return Task.CompletedTask;
            });
        workflow.AppendDialogAsync(Arg.Any<IReadOnlyList<IDialogItem>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var items = call.Arg<IReadOnlyList<IDialogItem>>() ?? throw new InvalidOperationException("Actual dialog callback received null items");
                Observe("legacy", "workflow.sessionlog.appendDialog", _rawParams,
                    items.Select(item => new Dictionary<string, object?>
                    {
                        ["content"] = item.Content, ["role"] = item.Role,
                        ["category"] = item.Category, ["timestamp"] = item.Timestamp,
                    }).ToArray());
                return Task.CompletedTask;
            });
        var dispatcher = new ReplCommandDispatcher(
            Substitute.For<IGenericClientPassthrough>(), workflow,
            governedSessionDispatch: _governed ? new CaptureDispatch(this) : null);
        var result = await dispatcher.DispatchAsync(envelope, CancellationToken.None).ConfigureAwait(false);
        if (result.Payload is IErrorPayload error)
            throw new InvalidOperationException($"Actual dispatcher error {error.Code}: {error.Message}");
        return result.Type;
    }

    /// <summary>Returns only effects actually observed at the selected dependency boundary.</summary>
    /// <returns>Native JSON observations consumed by the unchanged common assertions.</returns>
    public string InspectJson() => JsonSerializer.Serialize(_effects);

    /// <summary>Records actual callback arguments and independently captured constructor roots.</summary>
    private void Observe(string route, string method, string paramsJson, object[] typedBatch)
        => _effects.Add(new
        {
            route, method, paramsJson, captured = new Dictionary<string, string>(_capture), typedBatch,
        });

    /// <summary>Captures the actual governed dispatcher callback without a competing production lifecycle.</summary>
    private sealed class CaptureDispatch(GovernedSessionRouteFixture owner) : IGovernedSessionDispatch
    {
        /// <inheritdoc />
        public Task<object?> InvokeAsync(string method, string paramsJson, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(paramsJson);
            var values = document.RootElement;
            var batch = values.TryGetProperty("actions", out var actions) ? actions
                : values.TryGetProperty("dialogItems", out var dialogs) ? dialogs : default;
            object[] typed = batch.ValueKind == JsonValueKind.Array
                ? batch.EnumerateArray().Select(item => (object)item.Clone()).ToArray() : [];
            owner.Observe("governed", method, paramsJson, typed);
            return Task.FromResult<object?>(new { accepted = true });
        }
    }
}