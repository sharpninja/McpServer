using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.UI.Core.Models;

/// <summary>Path and content for the prompt templates YAML file (name + template text per item; context is sent to the model with each query).</summary>
public static class PromptTemplatesIo
{
    private const string FileName = "prompt_templates.yaml";

    public static string GetFilePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpServerManager");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, FileName);
    }

    /// <summary>Ensures the file exists; creates it with default content if missing.</summary>
    public static void EnsureExists()
    {
        var path = GetFilePath();
        if (File.Exists(path)) return;
        File.WriteAllText(path, GetDefaultContent());
    }

    /// <summary>Reads and parses the prompt templates from the YAML file.</summary>
    public static IReadOnlyList<PromptTemplate> GetPrompts()
    {
        EnsureExists();
        try
        {
            var path = GetFilePath();
            var yamlText = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(yamlText)) return Array.Empty<PromptTemplate>();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var root = deserializer.Deserialize<PromptTemplatesRoot>(yamlText);
            if (root?.Prompts == null || root.Prompts.Count == 0) return Array.Empty<PromptTemplate>();
            var list = new List<PromptTemplate>();
            foreach (var p in root.Prompts)
            {
                if (string.IsNullOrWhiteSpace(p?.Name)) continue;
                list.Add(new PromptTemplate { Name = p.Name.Trim(), Template = (p.Template ?? "").Trim() });
            }
            return list;
        }
        catch
        {
            return Array.Empty<PromptTemplate>();
        }
    }

    private static string GetDefaultContent()
    {
        return @"# Prompt templates for the AI Assistant.
# Context is sent to the model with each query; templates are plain text.
# Single-click to fill input, double-click to send.

prompts:
  - name: Summarize errors
    template: |
      Summarize any errors or failures in the current log context.
  - name: List request IDs
    template: |
      List all request IDs and their status from the current log context.
  - name: Prompt 1
    template: ''
  - name: Prompt 2
    template: ''
  - name: Prompt 3
    template: ''
";
    }

    private sealed class PromptTemplatesRoot
    {
        public List<PromptTemplateYaml>? Prompts { get; set; }
    }

    private sealed class PromptTemplateYaml
    {
        public string Name { get; set; } = "";
        public string Template { get; set; } = "";
    }
}

