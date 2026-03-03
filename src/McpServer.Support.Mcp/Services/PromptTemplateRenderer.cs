using System;
using System.Collections.Generic;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Renders prompt templates using Handlebars and validates required variables.
/// Thread-safe; compiled templates are cached by content hash.
/// </summary>
public sealed class PromptTemplateRenderer
{
    private static readonly IHandlebars s_handlebars = Handlebars.Create();
    private readonly Dictionary<int, HandlebarsTemplate<object, object>> _cache = new();
    private readonly object _cacheLock = new();
    private readonly ILogger<PromptTemplateRenderer> _logger;

    /// <summary>Initializes a new instance of the <see cref="PromptTemplateRenderer"/> class.</summary>
    /// <param name="logger">Logger instance.</param>
    public PromptTemplateRenderer(ILogger<PromptTemplateRenderer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Renders a template string with the given variable context.
    /// </summary>
    /// <param name="templateContent">Handlebars template content.</param>
    /// <param name="variables">Variable values for the template context.</param>
    /// <returns>The rendered output string.</returns>
    /// <exception cref="HandlebarsCompilerException">Thrown if the template has syntax errors.</exception>
    public string Render(string templateContent, Dictionary<string, object?> variables)
    {
        var compiled = GetOrCompile(templateContent);
        return compiled(variables).ReplaceLineEndings("\n");
    }

    /// <summary>
    /// Validates that all required variables are present in the provided context.
    /// </summary>
    /// <param name="declaredVariables">Variable definitions from the template.</param>
    /// <param name="providedVariables">Variable values supplied for rendering.</param>
    /// <returns>List of missing required variable names, or empty if all present.</returns>
    public static List<string> ValidateRequiredVariables(
        IReadOnlyList<Models.TemplateVariable> declaredVariables,
        Dictionary<string, object?>? providedVariables)
    {
        var missing = new List<string>();
        foreach (var v in declaredVariables)
        {
            if (!v.Required) continue;
            if (providedVariables is null ||
                !providedVariables.TryGetValue(v.Name, out var value) ||
                value is null ||
                (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                missing.Add(v.Name);
            }
        }

        return missing;
    }

    private HandlebarsTemplate<object, object> GetOrCompile(string templateContent)
    {
        var hash = templateContent.GetHashCode(StringComparison.Ordinal);
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(hash, out var cached))
                return cached;
        }

        // Normalize line endings before compilation (CRLF confuses Handlebars)
        var normalized = templateContent.ReplaceLineEndings("\n");
        var compiled = s_handlebars.Compile(normalized);

        lock (_cacheLock)
        {
            _cache[hash] = compiled;
        }

        _logger.LogDebug("Compiled and cached Handlebars template (hash {Hash})", hash);
        return compiled;
    }
}
