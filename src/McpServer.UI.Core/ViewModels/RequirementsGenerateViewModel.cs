using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for requirements document generation workflows.
/// </summary>
[ViewModelCommand("requirements-generate", Description = "Generate requirements markdown/zip outputs")]
public sealed partial class RequirementsGenerateViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<RequirementsGenerateViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirementsGenerateViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public RequirementsGenerateViewModel(
        Dispatcher dispatcher,
        ILogger<RequirementsGenerateViewModel> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        StatusMessage = "Ready.";
    }

    /// <summary>Logical area represented by this ViewModel.</summary>
    public McpArea Area => McpArea.Requirements;

    /// <summary>Selected doc selector (all/fr/tr/test/mapping/zip).</summary>
    [ObservableProperty]
    private string _docSelector = "all";

    /// <summary>Whether a generation request is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Latest status text.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Latest error text.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Last generated document metadata.</summary>
    [ObservableProperty]
    private GeneratedRequirementsDocument? _lastGenerated;

    /// <summary>
    /// Generates requirements output for the selected doc selector.
    /// </summary>
    /// <param name="doc">Optional selector override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated document on success, otherwise null.</returns>
    public async Task<GeneratedRequirementsDocument?> GenerateAsync(string? doc = null, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        var selector = string.IsNullOrWhiteSpace(doc) ? DocSelector : doc!;
        StatusMessage = $"Generating requirements doc '{selector}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GenerateRequirementsDocumentQuery(selector), ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Requirements generation failed.";
                StatusMessage = "Requirements generation failed.";
                return null;
            }

            LastGenerated = result.Value;
            StatusMessage = $"Generated '{selector}' ({result.Value.Content.Length} bytes, {result.Value.ContentType ?? "unknown type"}).";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Requirements generation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
