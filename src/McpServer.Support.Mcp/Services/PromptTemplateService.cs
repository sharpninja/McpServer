// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HandlebarsDotNet;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// YAML file-backed implementation of <see cref="IPromptTemplateService"/>.
/// Registered as a global singleton. Write operations are serialized via <see cref="SemaphoreSlim"/>.
/// </summary>
public sealed class PromptTemplateService : IPromptTemplateService, IDisposable
{
    private static readonly IDeserializer s_deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer s_serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private readonly string _filePath;
    private readonly PromptTemplateRenderer _renderer;
    private readonly ILogger<PromptTemplateService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="PromptTemplateService"/> class.</summary>
    /// <param name="options">Template storage configuration.</param>
    /// <param name="renderer">Template rendering engine.</param>
    /// <param name="logger">Logger instance.</param>
    public PromptTemplateService(
        IOptions<TemplateStorageOptions> options,
        PromptTemplateRenderer renderer,
        ILogger<PromptTemplateService> logger)
    {
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _filePath = Path.IsPathRooted(opts.FilePath)
            ? opts.FilePath
            : Path.Combine(AppContext.BaseDirectory, opts.FilePath);

        _logger.LogInformation("PromptTemplateService using file: {FilePath}", _filePath);
    }

    /// <inheritdoc />
    public async Task<PromptTemplateQueryResult> QueryAsync(
        string? category = null,
        string? tag = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var templates = await ReadAllAsync(cancellationToken).ConfigureAwait(false);

        IEnumerable<PromptTemplate> filtered = templates;

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(t =>
                string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filtered = filtered.Where(t =>
                t.Tags.Any(tg => string.Equals(tg, tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filtered = filtered.Where(t =>
                (t.Id?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var result = filtered.ToList();
        return new PromptTemplateQueryResult(result, result.Count);
    }

    /// <inheritdoc />
    public async Task<PromptTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var templates = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        return templates.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<PromptTemplateMutationResult> CreateAsync(
        PromptTemplateCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return new PromptTemplateMutationResult(false, "Id is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            return new PromptTemplateMutationResult(false, "Title is required.");
        if (string.IsNullOrWhiteSpace(request.Category))
            return new PromptTemplateMutationResult(false, "Category is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            return new PromptTemplateMutationResult(false, "Content is required.");

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templates = await ReadAllAsync(cancellationToken).ConfigureAwait(false);

            if (templates.Any(t => string.Equals(t.Id, request.Id, StringComparison.OrdinalIgnoreCase)))
                return new PromptTemplateMutationResult(false, $"Template '{request.Id}' already exists.");

            var template = new PromptTemplate
            {
                Id = request.Id,
                Title = request.Title,
                Category = request.Category,
                Content = request.Content,
                Tags = request.Tags?.ToList() ?? [],
                Description = request.Description,
                Engine = request.Engine ?? "handlebars",
                Variables = request.Variables?.ToList() ?? [],
            };

            var all = templates.ToList();
            all.Add(template);
            await WriteAllAsync(all, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created template '{Id}'", request.Id);
            return new PromptTemplateMutationResult(true, Item: template);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PromptTemplateMutationResult> UpdateAsync(
        string id,
        PromptTemplateUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templates = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
            var all = templates.ToList();
            var idx = all.FindIndex(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                return new PromptTemplateMutationResult(false, $"Template '{id}' not found.");

            var existing = all[idx];
            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Category = request.Category ?? existing.Category,
                Content = request.Content ?? existing.Content,
                Tags = request.Tags?.ToList() ?? existing.Tags,
                Description = request.Description ?? existing.Description,
                Engine = request.Engine ?? existing.Engine,
                Variables = request.Variables?.ToList() ?? existing.Variables,
            };

            all[idx] = updated;
            await WriteAllAsync(all, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated template '{Id}'", id);
            return new PromptTemplateMutationResult(true, Item: updated);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PromptTemplateMutationResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templates = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
            var all = templates.ToList();
            var idx = all.FindIndex(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                return new PromptTemplateMutationResult(false, $"Template '{id}' not found.");

            var removed = all[idx];
            all.RemoveAt(idx);
            await WriteAllAsync(all, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted template '{Id}'", id);
            return new PromptTemplateMutationResult(true, Item: removed);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PromptTemplateTestResult> TestAsync(
        string id,
        PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (template is null)
            return new PromptTemplateTestResult { Success = false, Error = $"Template '{id}' not found." };

        return RenderTemplate(template.Content, template.Variables, request.Variables);
    }

    /// <inheritdoc />
    public Task<PromptTemplateTestResult> TestInlineAsync(
        PromptTemplateTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InlineTemplate))
        {
            return Task.FromResult(new PromptTemplateTestResult
            {
                Success = false,
                Error = "InlineTemplate is required for inline testing.",
            });
        }

        var result = RenderTemplate(request.InlineTemplate, [], request.Variables);
        return Task.FromResult(result);
    }

    /// <summary>Disposes the file lock.</summary>
    public void Dispose()
    {
        _fileLock.Dispose();
    }

    private PromptTemplateTestResult RenderTemplate(
        string content,
        IReadOnlyList<TemplateVariable> declaredVariables,
        Dictionary<string, object?>? variables)
    {
        var missing = PromptTemplateRenderer.ValidateRequiredVariables(declaredVariables, variables);
        if (missing.Count > 0)
        {
            return new PromptTemplateTestResult
            {
                Success = false,
                Error = $"Missing required variables: {string.Join(", ", missing)}",
                MissingVariables = missing,
            };
        }

        try
        {
            var rendered = _renderer.Render(content, variables ?? []);
            return new PromptTemplateTestResult { Success = true, RenderedContent = rendered };
        }
        catch (HandlebarsCompilerException ex)
        {
            _logger.LogWarning("Handlebars compilation error: {Error}", ex.ToString());
            return new PromptTemplateTestResult
            {
                Success = false,
                Error = $"Template compilation error: {ex.Message}",
            };
        }
        catch (HandlebarsRuntimeException ex)
        {
            _logger.LogWarning("Handlebars runtime error: {Error}", ex.ToString());
            return new PromptTemplateTestResult
            {
                Success = false,
                Error = $"Template rendering error: {ex.Message}",
            };
        }
    }

    private async Task<IReadOnlyList<PromptTemplate>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogDebug("Template file not found at {Path}, returning empty list", _filePath);
            return [];
        }

        try
        {
            var yaml = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            var file = s_deserializer.Deserialize<TemplateFileRoot>(yaml);
            if (file?.Templates is null)
                return [];

            return file.Templates.Select(kvp => new PromptTemplate
            {
                Id = kvp.Key,
                Title = kvp.Value.Title ?? kvp.Key,
                Category = kvp.Value.Category ?? "custom",
                Tags = kvp.Value.Tags?.ToList() ?? [],
                Description = kvp.Value.Description,
                Engine = kvp.Value.Engine ?? "handlebars",
                Variables = kvp.Value.Variables?.Select(v => new TemplateVariable
                {
                    Name = v.Name ?? string.Empty,
                    Description = v.Description,
                    Required = v.Required,
                    Example = v.Example,
                    DefaultValue = v.DefaultValue,
                }).ToList() ?? [],
                Content = kvp.Value.Content ?? string.Empty,
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read template file {Path}: {Error}", _filePath, ex.ToString());
            return [];
        }
    }

    private async Task WriteAllAsync(IReadOnlyList<PromptTemplate> templates, CancellationToken cancellationToken)
    {
        var root = new TemplateFileRoot
        {
            Templates = templates.ToDictionary(
                t => t.Id,
                t => new TemplateFileEntry
                {
                    Title = t.Title,
                    Category = t.Category,
                    Tags = t.Tags.ToList(),
                    Description = t.Description,
                    Engine = t.Engine,
                    Variables = t.Variables.Select(v => new TemplateFileVariable
                    {
                        Name = v.Name,
                        Description = v.Description,
                        Required = v.Required,
                        Example = v.Example,
                        DefaultValue = v.DefaultValue,
                    }).ToList(),
                    Content = t.Content,
                }),
        };

        var yaml = s_serializer.Serialize(root);
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(_filePath, yaml, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Wrote {Count} templates to {Path}", templates.Count, _filePath);
    }

    /// <summary>Root YAML structure: templates map.</summary>
    internal sealed class TemplateFileRoot
    {
        /// <summary>Map of template ID to entry.</summary>
        public Dictionary<string, TemplateFileEntry> Templates { get; set; } = new();
    }

    /// <summary>A single template entry in the YAML file.</summary>
    internal sealed class TemplateFileEntry
    {
        /// <summary>Template title.</summary>
        public string? Title { get; set; }

        /// <summary>Template category.</summary>
        public string? Category { get; set; }

        /// <summary>Template tags.</summary>
        public List<string>? Tags { get; set; }

        /// <summary>Template description.</summary>
        public string? Description { get; set; }

        /// <summary>Template engine.</summary>
        public string? Engine { get; set; }

        /// <summary>Template variables.</summary>
        public List<TemplateFileVariable>? Variables { get; set; }

        /// <summary>Template content.</summary>
        public string? Content { get; set; }
    }

    /// <summary>A variable entry in the YAML file.</summary>
    internal sealed class TemplateFileVariable
    {
        /// <summary>Variable name.</summary>
        public string? Name { get; set; }

        /// <summary>Variable description.</summary>
        public string? Description { get; set; }

        /// <summary>Whether the variable is required.</summary>
        public bool Required { get; set; }

        /// <summary>Example value.</summary>
        public string? Example { get; set; }

        /// <summary>Default value.</summary>
        [YamlMember(Alias = "default-value")]
        public string? DefaultValue { get; set; }
    }
}
