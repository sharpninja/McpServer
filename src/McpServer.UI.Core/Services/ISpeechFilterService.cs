using System.Collections.Generic;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Persists and parses speech filter phrases used by settings UI.
/// </summary>
public interface ISpeechFilterService
{
    /// <summary>Gets or sets raw phrase text (one phrase per line).</summary>
    string FilterText { get; set; }

    /// <summary>Returns parsed phrase list.</summary>
    IReadOnlyList<string> GetFilterWords();

    /// <summary>Parses phrase list from JSON content.</summary>
    List<string> ParseJsonPhraseList(string content);
}
