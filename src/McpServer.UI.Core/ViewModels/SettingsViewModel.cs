using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Settings view model for speech filter phrase management.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISpeechFilterService _speechFilter;

    [ObservableProperty]
    private string _speechFilterWords = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Initializes settings state from the persisted speech filter store.
    /// </summary>
    public SettingsViewModel(ISpeechFilterService speechFilter)
    {
        _speechFilter = speechFilter ?? throw new ArgumentNullException(nameof(speechFilter));
        _speechFilterWords = _speechFilter.FilterText;
    }

    /// <summary>Saves the current filter phrase list to persistence.</summary>
    protected void SaveFilterWords()
    {
        _speechFilter.FilterText = SpeechFilterWords;
        StatusMessage = $"Saved {_speechFilter.GetFilterWords().Count} filter phrase(s).";
    }

    /// <summary>Reverts the editor text to the last saved values.</summary>
    protected void RevertFilterWords()
    {
        SpeechFilterWords = _speechFilter.FilterText;
        StatusMessage = "Reverted to saved values.";
    }

    /// <summary>
    /// Imports phrases from file content. Supports plain text (one per line),
    /// JSON (string array), and YAML (dash-prefixed list).
    /// </summary>
    public void ImportFromFileContent(string content, string fileName)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            StatusMessage = "Import file was empty.";
            return;
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        List<string> phrases;

        try
        {
            phrases = ext switch
            {
                ".json" => ParseJsonPhrases(content),
                ".yaml" or ".yml" => ParseYamlPhrases(content),
                _ => ParsePlainTextPhrases(content)
            };
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            return;
        }

        if (phrases.Count == 0)
        {
            StatusMessage = "No phrases found in the imported data.";
            return;
        }

        var existing = _speechFilter.FilterText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var merged = existing
            .Concat(phrases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var newCount = merged.Count - existing.Count;
        SpeechFilterWords = string.Join(Environment.NewLine, merged);
        _speechFilter.FilterText = SpeechFilterWords;
        StatusMessage = $"Imported {newCount} new phrase(s) ({merged.Count} total). Saved.";
    }

    private List<string> ParseJsonPhrases(string content) => _speechFilter.ParseJsonPhraseList(content);

    private static List<string> ParseYamlPhrases(string content)
    {
        return content
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("- ", StringComparison.Ordinal))
            .Select(l => l[2..].Trim().Trim('"', '\''))
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static List<string> ParsePlainTextPhrases(string content)
    {
        return content
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
