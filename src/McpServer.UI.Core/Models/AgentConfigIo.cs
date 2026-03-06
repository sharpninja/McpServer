using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.UI.Core.Models;

/// <summary>Path and content for the agent config file (instructions/context fed to the AI assistant on startup and with context updates). Supports YAML front matter for settings like model.</summary>
public static class AgentConfigIo
{
    private const string ConfigFileName = "agent_config.md";
    private const string FrontMatterDelimiter = "---";

    public static string GetFilePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpServerManager");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, ConfigFileName);
    }

    /// <summary>Ensures the config file exists; creates it with default content if missing.</summary>
    public static void EnsureExists()
    {
        var path = GetFilePath();
        if (File.Exists(path)) return;
        File.WriteAllText(path, GetDefaultContent());
    }

    /// <summary>Reads the config file content, or empty string if missing/unreadable.</summary>
    public static string ReadContent()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path)) return "";
            return File.ReadAllText(path);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>Parses YAML front matter and returns the "model" value if present.</summary>
    public static string? GetModelFromConfig()
    {
        var content = ReadContent();
        if (string.IsNullOrWhiteSpace(content)) return null;
        var (_, frontMatter) = SplitFrontMatter(content);
        if (string.IsNullOrWhiteSpace(frontMatter)) return null;
        return ParseFrontMatterValue(frontMatter, "model");
    }

    /// <summary>Updates the "model" key in the config file's YAML front matter, creating front matter if missing.</summary>
    public static void SetModelInConfig(string? model)
    {
        var path = GetFilePath();
        EnsureExists();
        var content = File.ReadAllText(path);
        var newContent = SetFrontMatterValue(content, "model", model ?? "");
        File.WriteAllText(path, newContent);
    }

    private static readonly char[] LineEndings = new[] { '\r', '\n' };

    private static (string body, string? frontMatter) SplitFrontMatter(string content)
    {
        // Normalize to LF so we don't introduce stray CR or double newlines when rewriting
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        if (lines.Length < 2 || lines[0].Trim() != FrontMatterDelimiter)
            return (content, null);
        var endIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == FrontMatterDelimiter)
            {
                endIndex = i;
                break;
            }
        }
        if (endIndex < 0) return (content, null);
        var body = new StringBuilder();
        for (int i = endIndex + 1; i < lines.Length; i++)
            AppendLineLf(body, lines[i]);
        var frontMatter = new StringBuilder();
        for (int i = 1; i < endIndex; i++)
            AppendLineLf(frontMatter, lines[i]);
        return (body.ToString().TrimEnd(LineEndings), frontMatter.ToString().TrimEnd(LineEndings));
    }

    private static void AppendLineLf(StringBuilder sb, string line)
    {
        sb.Append(line.TrimEnd(LineEndings));
        sb.Append('\n');
    }

    private static string? ParseFrontMatterValue(string frontMatter, string key)
    {
        var keyPattern = new Regex(@"^\s*" + Regex.Escape(key) + @"\s*:\s*(.*)$", RegexOptions.Multiline);
        var m = keyPattern.Match(frontMatter);
        if (!m.Success) return null;
        var value = m.Groups[1].Value.Trim();
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
            value = value[1..^1].Replace("\\\"", "\"");
        if (value.StartsWith('\'') && value.EndsWith('\'') && value.Length >= 2)
            value = value[1..^1];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string SetFrontMatterValue(string content, string key, string value)
    {
        var (body, frontMatter) = SplitFrontMatter(content);
        var keyLinePattern = new Regex(@"^\s*" + Regex.Escape(key) + @"\s*:\s*.*$", RegexOptions.Multiline);
        var valueYaml = string.IsNullOrEmpty(value) ? "" : (value.Contains(':') || value.Contains('\n') ? $"\"{value.Replace("\"", "\\\"")}\"" : value);
        string newFrontMatter;
        if (string.IsNullOrWhiteSpace(frontMatter))
        {
            newFrontMatter = $"{key}: {valueYaml}";
        }
        else if (keyLinePattern.IsMatch(frontMatter))
        {
            newFrontMatter = keyLinePattern.Replace(frontMatter, $"{key}: {valueYaml}");
        }
        else
        {
            newFrontMatter = frontMatter.TrimEnd() + "\n" + $"{key}: {valueYaml}";
        }
        return $"{FrontMatterDelimiter}\n{newFrontMatter.TrimEnd()}\n{FrontMatterDelimiter}\n\n{body}";
    }

    private static string GetDefaultContent()
    {
        // Use explicit \n so saved file has consistent line endings (no stray CRLF).
        return "---\nmodel: llama3:latest\n---\n\n# Agent configuration\n\nInstructions and context for the AI assistant. This file is sent to the agent when you open the chat and when context is refreshed.\n\n- Add project-specific instructions here.\n- Describe how you want the agent to interpret the request log (e.g. focus on errors, summarize by day).\n\nPrompt templates are in prompt_templates.yaml (same folder). Context is sent to the model with each query; single-click to fill input, double-click to send.\n";
    }
}

