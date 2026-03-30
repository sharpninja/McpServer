namespace McpServer.Repl.Core;

/// <summary>
/// Provides YAML serialization and deserialization for REPL protocol envelopes.
/// Implementations must handle type-safe envelope discrimination and payload mapping.
/// </summary>
public interface IYamlSerializer
{
    /// <summary>
    /// Serializes a YAML envelope to a YAML string.
    /// </summary>
    /// <param name="envelope">The envelope to serialize.</param>
    /// <returns>A YAML-formatted string representation of the envelope.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the envelope contains an invalid structure.</exception>
    string Serialize(IYamlEnvelope envelope);

    /// <summary>
    /// Deserializes a YAML string to a typed envelope.
    /// The returned envelope type depends on the "type" discriminator in the YAML.
    /// </summary>
    /// <param name="yaml">The YAML string to deserialize.</param>
    /// <returns>A typed envelope instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yaml"/> is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the YAML is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the envelope type is unknown or payload is invalid.</exception>
    IYamlEnvelope Deserialize(string yaml);

    /// <summary>
    /// Attempts to deserialize a YAML string to a typed envelope.
    /// Returns false if deserialization fails, without throwing exceptions.
    /// </summary>
    /// <param name="yaml">The YAML string to deserialize.</param>
    /// <param name="envelope">The deserialized envelope if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    bool TryDeserialize(string yaml, out IYamlEnvelope? envelope);

    /// <summary>
    /// Serializes multiple envelopes as a YAML document stream.
    /// Each envelope is written as a separate YAML document separated by "---".
    /// </summary>
    /// <param name="envelopes">The envelopes to serialize.</param>
    /// <returns>A YAML stream with multiple documents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelopes"/> is null.</exception>
    string SerializeStream(IEnumerable<IYamlEnvelope> envelopes);

    /// <summary>
    /// Deserializes a YAML document stream to multiple envelopes.
    /// Each document in the stream is parsed as a separate envelope.
    /// </summary>
    /// <param name="yamlStream">The YAML stream containing multiple documents.</param>
    /// <returns>A collection of deserialized envelopes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="yamlStream"/> is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when any document in the stream is malformed.</exception>
    IReadOnlyList<IYamlEnvelope> DeserializeStream(string yamlStream);
}
