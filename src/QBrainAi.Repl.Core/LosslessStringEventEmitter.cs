// FR-QB-003/005, TR-QB-AUDIT-001, TEST-QB-069: lossless REPL response strings.
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace QBrainAi.Repl.Core;

/// <summary>
/// Emits escaped YAML strings when a literal or folded scalar would normalize
/// line breaks and change audited request, response, decision, or dialog text.
/// </summary>
internal sealed class LosslessStringEventEmitter : ChainedEventEmitter
{
    private static readonly char[] LineBreaks = ['\r', '\n', '\u0085', '\u2028', '\u2029'];

    /// <summary>Initializes the scalar adapter in the existing YAML emitter chain.</summary>
    /// <param name="nextEmitter">The next existing serializer emitter.</param>
    public LosslessStringEventEmitter(IEventEmitter nextEmitter)
        : base(nextEmitter)
    {
    }

    /// <inheritdoc />
    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(eventInfo);
        ArgumentNullException.ThrowIfNull(emitter);

        if (eventInfo.Source.Value is string text && text.IndexOfAny(LineBreaks) >= 0)
        {
            eventInfo.Style = ScalarStyle.DoubleQuoted;
        }

        base.Emit(eventInfo, emitter);
    }
}
