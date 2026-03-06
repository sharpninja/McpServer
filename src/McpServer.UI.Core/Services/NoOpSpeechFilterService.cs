using System.Collections.Generic;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op speech filter service used when host does not supply one.
/// </summary>
public sealed class NoOpSpeechFilterService : ISpeechFilterService
{
    private string _filterText = string.Empty;

    /// <inheritdoc />
    public string FilterText
    {
        get => _filterText;
        set => _filterText = value ?? string.Empty;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetFilterWords() => [];

    /// <inheritdoc />
    public List<string> ParseJsonPhraseList(string content) => [];
}
