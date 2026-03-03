namespace McpServer.Support.Mcp.Web;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Renders pairing HTML pages from
/// <see cref="Services.IPromptTemplateService"/> templates, falling back
/// to the built-in <see cref="PairingHtml"/> inline strings when templates are not found.
/// </summary>
public sealed class PairingHtmlRenderer
{
    /// <summary>Well-known template ID for the login page.</summary>
    internal const string LoginPageId = "pairing-login-page";

    /// <summary>Well-known template ID for the API key page.</summary>
    internal const string KeyPageId = "pairing-key-page";

    /// <summary>Well-known template ID for the not-configured page.</summary>
    internal const string NotConfiguredPageId = "pairing-not-configured-page";

    private readonly Services.IPromptTemplateService _templateService;
    private readonly ILogger<PairingHtmlRenderer> _logger;

    /// <summary>Initializes a new instance of the <see cref="PairingHtmlRenderer"/> class.</summary>
    public PairingHtmlRenderer(Services.IPromptTemplateService templateService, ILogger<PairingHtmlRenderer> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>Renders the login form page. Shows an error banner when <paramref name="error"/> is <c>true</c>.</summary>
    public async Task<string> RenderLoginPageAsync(bool error = false, CancellationToken cancellationToken = default)
    {
        var errorBanner = error
            ? "<div style='background:#fee;color:#c00;padding:10px 16px;border-radius:6px;margin-bottom:16px;border:1px solid #fcc'>Invalid username or password.</div>"
            : "";

        var template = await GetTemplateContentAsync(LoginPageId, cancellationToken).ConfigureAwait(false);
        if (template is not null)
        {
            return template.Replace("{errorBanner}", errorBanner, StringComparison.Ordinal);
        }

        return PairingHtml.LoginPage(error);
    }

    /// <summary>Renders the API key display page.</summary>
    public async Task<string> RenderKeyPageAsync(string apiKey, string serverUrl, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateContentAsync(KeyPageId, cancellationToken).ConfigureAwait(false);
        if (template is not null)
        {
            return template
                .Replace("{apiKey}", apiKey, StringComparison.Ordinal)
                .Replace("{serverUrl}", serverUrl, StringComparison.Ordinal);
        }

        return PairingHtml.KeyPage(apiKey, serverUrl);
    }

    /// <summary>Renders the not-configured warning page.</summary>
    public async Task<string> RenderNotConfiguredPageAsync(CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateContentAsync(NotConfiguredPageId, cancellationToken).ConfigureAwait(false);
        if (template is not null)
        {
            return template;
        }

        return PairingHtml.NotConfiguredPage();
    }

    private async Task<string?> GetTemplateContentAsync(string templateId, CancellationToken cancellationToken)
    {
        try
        {
            var tmpl = await _templateService.GetByIdAsync(templateId, cancellationToken).ConfigureAwait(false);
            if (tmpl is not null && !string.IsNullOrWhiteSpace(tmpl.Content))
            {
                _logger.LogDebug("Loaded pairing HTML template '{Id}' from template store", templateId);
                return tmpl.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load pairing HTML template '{Id}': {Error}", templateId, ex.ToString());
        }

        _logger.LogDebug("Using built-in default for pairing HTML '{Id}'", templateId);
        return null;
    }
}
