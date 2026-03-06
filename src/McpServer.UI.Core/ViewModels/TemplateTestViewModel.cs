using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for testing templates by ID or inline content.
/// </summary>
public sealed partial class TemplateTestViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<TemplateTestViewModel> _logger;

    /// <summary>Initializes a new template test ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TemplateTestViewModel(Dispatcher dispatcher, ILogger<TemplateTestViewModel> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    [ObservableProperty]
    private string? _templateId;

    [ObservableProperty]
    private string? _inlineTemplate;

    [ObservableProperty]
    private string _variablesJson = "{}";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private TemplateTestOutcome? _result;

    /// <summary>Executes a template test query with current input fields.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(TemplateId) && string.IsNullOrWhiteSpace(InlineTemplate))
        {
            Result = null;
            ErrorMessage = null;
            IsLoading = false;
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            var query = new TestTemplateQuery
            {
                TemplateId = string.IsNullOrWhiteSpace(TemplateId) ? null : TemplateId,
                InlineTemplate = string.IsNullOrWhiteSpace(InlineTemplate) ? null : InlineTemplate,
                VariablesJson = string.IsNullOrWhiteSpace(VariablesJson) ? "{}" : VariablesJson,
            };

            var dispatchResult = await _dispatcher.QueryAsync(query, ct).ConfigureAwait(true);
            if (!dispatchResult.IsSuccess)
            {
                ErrorMessage = dispatchResult.Error ?? "Unknown error running template test.";
                return;
            }

            Result = dispatchResult.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
