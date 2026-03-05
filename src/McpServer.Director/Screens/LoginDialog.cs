using McpServer.Director.Auth;
using McpServer.Director.Handlers;
using McpServer.Director.Helpers;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui dialog for Keycloak Device Authorization Flow login.
/// Shows authority input, initiates device flow, displays user code, and polls for completion.
/// </summary>
internal sealed class LoginDialog : Dialog
{
    private readonly Action<string>? _onLoginSuccess;
    private readonly IBrowserLauncher _browserLauncher;
    private readonly LoginDialogAuthConfigHandler _authConfigHandler = new();
    private TextView _statusLabel = null!;
    private TextField _codeField = null!;
    private TextField _uriField = null!;

    /// <summary>Server-discovered auth config, if available.</summary>
    private AuthConfigResponse? _serverConfig;

    /// <summary>Current user code for clipboard copy.</summary>
    private string? _currentUserCode;

    /// <summary>Current verification URI for clipboard copy.</summary>
    private string? _currentVerificationUri;

    public LoginDialog(IBrowserLauncher browserLauncher, Action<string>? onLoginSuccess = null)
    {
        _browserLauncher = browserLauncher;
        _onLoginSuccess = onLoginSuccess;
        Title = "Login — Keycloak Device Authorization";
        Width = 70;
        Height = 18;
        BuildUi();
    }

    private void BuildUi()
    {
        // Pre-populate from server auto-discovery
        var defaultAuthority = Environment.GetEnvironmentVariable("MCP_AUTH_AUTHORITY") ?? "";
        var defaultClientId = "mcp-director";

        var authorityLabel = new Label { X = 1, Y = 1, Text = "Authority URL:" };
        var authorityField = new TextField
        {
            X = 17, Y = 1, Width = 40,
            Text = defaultAuthority,
        };
        Add(authorityLabel, authorityField);

        var clientIdLabel = new Label { X = 1, Y = 2, Text = "Client ID:" };
        var clientIdField = new TextField { X = 17, Y = 2, Width = 40, Text = defaultClientId };
        Add(clientIdLabel, clientIdField);

        _codeField = new TextField { X = 1, Y = 4, Width = Dim.Fill(2), Text = "", ReadOnly = true };
        _uriField = new TextField { X = 1, Y = 5, Width = Dim.Fill(2), Text = "", ReadOnly = true };
        _statusLabel = new TextView
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_codeField, _uriField, _statusLabel);

        // Whoami section
        var whoamiFrame = new FrameView
        {
            Title = "Current User",
            X = 1, Y = 9,
            Width = Dim.Fill(2), Height = 4,
        };
        var whoamiLabel = new Label { X = 0, Y = 0, Width = Dim.Fill(), Text = GetWhoamiText() };
        whoamiFrame.Add(whoamiLabel);
        Add(whoamiFrame);

        var loginBtn = new Button { Text = "Login" };
        loginBtn.Accepting += (_, _) =>
        {
            var authority = authorityField.Text ?? "";
            var clientId = clientIdField.Text ?? "mcp-director";
            if (string.IsNullOrWhiteSpace(authority))
            {
                _statusLabel.Text = "✗ Authority URL is required";
                return;
            }
            loginBtn.Enabled = false;
            _ = Task.Run(async () =>
            {
                var options = new DirectorAuthOptions { Authority = authority, ClientId = clientId };

                // Apply server-discovered endpoints (device auth, token) if available
                if (_serverConfig is not null && _serverConfig.Enabled)
                    options.PopulateFrom(_serverConfig);

                using var authService = new OidcAuthService(options);

                var result = await authService.LoginAsync((userCode, verificationUri, verificationUriComplete) =>
                {
                    var targetUrl = verificationUriComplete ?? verificationUri;
                    _browserLauncher.TryOpenUrl(targetUrl);

                    Application.Invoke(() =>
                    {
                        _currentUserCode = userCode;
                        _currentVerificationUri = targetUrl;
                        _codeField.Text = $"User Code: {userCode}  (select & Ctrl+C to copy)";
                        _uriField.Text = $"{_currentVerificationUri}";
                        _statusLabel.Text = "⏳ Browser opened — complete authentication there...";
                    });
                }).ConfigureAwait(false);

                Application.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        _statusLabel.Text = $"✓ Logged in as {result.Username}";
                        whoamiLabel.Text = GetWhoamiText();
                        _onLoginSuccess?.Invoke(result.Username ?? "unknown");
                        Application.RequestStop();
                    }
                    else
                    {
                        _statusLabel.Text = $"✗ {result.Error}";
                    }
                    loginBtn.Enabled = true;
                });
            });
        };

        var logoutBtn = new Button { Text = "Logout" };
        logoutBtn.Accepting += (_, _) =>
        {
            OidcAuthService.Logout();
            _statusLabel.Text = "✓ Logged out";
            whoamiLabel.Text = "Not logged in";
            _onLoginSuccess?.Invoke("");
        };

        var browserBtn = new Button { Text = "Open Browser" };
        browserBtn.Accepting += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_currentVerificationUri))
            {
                _browserLauncher.TryOpenUrl(_currentVerificationUri);
                _statusLabel.Text = "🌐 Browser opened — complete authentication there...";
            }
            else
            {
                _statusLabel.Text = "✗ No verification URL yet — click Login first";
            }
        };

        var closeBtn = new Button { Text = "Close" };
        closeBtn.Accepting += (_, _) => Application.RequestStop();

        AddButton(loginBtn);
        AddButton(browserBtn);
        AddButton(logoutBtn);
        AddButton(closeBtn);

        QueueAuthConfigDiscovery(authorityField, clientIdField);

        // Clipboard hotkeys: Ctrl+Y copies user code, Ctrl+U copies verification URL
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == (KeyCode.Y | KeyCode.CtrlMask) && _currentUserCode is not null)
            {
                Clipboard.TrySetClipboardData(_currentUserCode);
                _statusLabel.Text = "📋 User code copied to clipboard!";
                e.Handled = true;
            }
            else if (e.KeyCode == (KeyCode.U | KeyCode.CtrlMask) && _currentVerificationUri is not null)
            {
                Clipboard.TrySetClipboardData(_currentVerificationUri);
                _statusLabel.Text = "📋 Verification URL copied to clipboard!";
                e.Handled = true;
            }
        };
    }

    private void QueueAuthConfigDiscovery(TextField authorityField, TextField clientIdField)
    {
        _ = Task.Run(async () =>
        {
            var config = await _authConfigHandler.DiscoverAuthConfigAsync().ConfigureAwait(false);
            if (config is null || !config.Enabled)
                return;

            Application.Invoke(() =>
            {
                _serverConfig = config;
                if (string.IsNullOrWhiteSpace(authorityField.Text?.ToString()))
                    authorityField.Text = config.Authority;
                var currentClientId = clientIdField.Text?.ToString();
                if (!string.IsNullOrWhiteSpace(config.ClientId)
                    && (string.IsNullOrWhiteSpace(currentClientId)
                        || string.Equals(currentClientId, "mcp-director", StringComparison.Ordinal)))
                {
                    clientIdField.Text = config.ClientId;
                }
            });
        });
    }

    private static string GetWhoamiText()
    {
        var user = OidcAuthService.GetCurrentUser();
        if (user is null) return "Not logged in";
        var status = user.IsExpired ? "EXPIRED" : "Valid";
        return $"{user.Username} ({user.Email ?? "no email"}) [{status}] Expires: {user.ExpiresAtUtc:yyyy-MM-dd HH:mm UTC}";
    }
}
