namespace McpServer.Support.Mcp.Web;

/// <summary>
/// Inline HTML templates for the <c>/pair</c> web login flow.
/// Users authenticate with a configured username/password to view the server API key.
/// </summary>
internal static class PairingHtml
{
    /// <summary>Renders the login form. Shows an error banner when <paramref name="error"/> is <c>true</c>.</summary>
    public static string LoginPage(bool error = false)
    {
        var errorBanner = error
            ? "<div style='background:#fee;color:#c00;padding:10px 16px;border-radius:6px;margin-bottom:16px;border:1px solid #fcc'>Invalid username or password.</div>"
            : "";

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>MCP Server — Pair</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:system-ui,-apple-system,sans-serif;background:#f5f5f5;display:flex;align-items:center;justify-content:center;min-height:100vh}
            .card{background:#fff;border-radius:12px;box-shadow:0 2px 12px rgba(0,0,0,.08);padding:32px;width:100%;max-width:380px}
            h1{font-size:1.3rem;margin-bottom:4px;color:#111}
            .sub{font-size:.85rem;color:#666;margin-bottom:20px}
            label{display:block;font-size:.85rem;font-weight:600;margin-bottom:4px;color:#333}
            input[type=text],input[type=password]{width:100%;padding:10px 12px;border:1px solid #ddd;border-radius:6px;font-size:.95rem;margin-bottom:14px}
            input:focus{outline:none;border-color:#0969da;box-shadow:0 0 0 3px rgba(9,105,218,.15)}
            button{width:100%;padding:10px;background:#0969da;color:#fff;border:none;border-radius:6px;font-size:.95rem;font-weight:600;cursor:pointer}
            button:hover{background:#0860c4}
          </style>
        </head>
        <body>
          <div class="card">
            <h1>🔗 MCP Server Pairing</h1>
            <p class="sub">Sign in to view your API key.</p>
            {{errorBanner}}
            <form method="post" action="/pair">
              <label for="username">Username</label>
              <input type="text" id="username" name="username" autocomplete="username" required autofocus/>
              <label for="password">Password</label>
              <input type="password" id="password" name="password" autocomplete="current-password" required/>
              <button type="submit">Sign In</button>
            </form>
          </div>
        </body>
        </html>
        """;
    }

    /// <summary>Renders the API key display page.</summary>
    public static string KeyPage(string apiKey, string serverUrl)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>MCP Server — API Key</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:system-ui,-apple-system,sans-serif;background:#f5f5f5;display:flex;align-items:center;justify-content:center;min-height:100vh}
            .card{background:#fff;border-radius:12px;box-shadow:0 2px 12px rgba(0,0,0,.08);padding:32px;width:100%;max-width:480px}
            h1{font-size:1.3rem;margin-bottom:4px;color:#111}
            .sub{font-size:.85rem;color:#666;margin-bottom:20px}
            .key-box{background:#f6f8fa;border:1px solid #d0d7de;border-radius:6px;padding:14px 16px;font-family:'Cascadia Code','Fira Code',monospace;font-size:.95rem;word-break:break-all;margin-bottom:16px;position:relative}
            .copy-btn{position:absolute;top:8px;right:8px;background:#0969da;color:#fff;border:none;border-radius:4px;padding:4px 10px;font-size:.8rem;cursor:pointer}
            .copy-btn:hover{background:#0860c4}
            .section{margin-bottom:20px}
            .section h2{font-size:1rem;margin-bottom:8px;color:#333}
            pre{background:#f6f8fa;border:1px solid #d0d7de;border-radius:6px;padding:14px 16px;font-size:.82rem;overflow-x:auto;line-height:1.5}
            .warn{font-size:.8rem;color:#888;margin-top:12px}
          </style>
        </head>
        <body>
          <div class="card">
            <h1>🔑 Your API Key</h1>
            <p class="sub">Use this key to authenticate mutating API calls.</p>
            <div class="key-box">
              <span id="key">{{apiKey}}</span>
              <button class="copy-btn" onclick="navigator.clipboard.writeText(document.getElementById('key').textContent)">Copy</button>
            </div>
            <div class="section">
              <h2>MCP Client Config</h2>
              <pre>{
          "mcpServers": {
            "mcp-server": {
              "url": "{{serverUrl}}/mcp-transport"
            }
          }
        }</pre>
            </div>
            <div class="section">
              <h2>cURL Example</h2>
              <pre>curl {{serverUrl}}/mcpserver/workspace \
          -H "X-Api-Key: {{apiKey}}"</pre>
            </div>
            <p class="warn">Keep this key secret. It grants write access to workspace and tool endpoints.</p>
          </div>
        </body>
        </html>
        """;
    }

    /// <summary>Renders a page shown when pairing is not configured.</summary>
    public static string NotConfiguredPage()
    {
        return """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>MCP Server — Pairing Not Configured</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:system-ui,-apple-system,sans-serif;background:#f5f5f5;display:flex;align-items:center;justify-content:center;min-height:100vh}
            .card{background:#fff;border-radius:12px;box-shadow:0 2px 12px rgba(0,0,0,.08);padding:32px;width:100%;max-width:380px;text-align:center}
            h1{font-size:1.3rem;margin-bottom:12px;color:#111}
            p{color:#666;font-size:.9rem;line-height:1.5}
            code{background:#f6f8fa;padding:2px 6px;border-radius:4px;font-size:.85rem}
          </style>
        </head>
        <body>
          <div class="card">
            <h1>⚠️ Pairing Not Configured</h1>
            <p>To enable the pairing page, add one or more users to
            <code>Mcp:PairingUsers</code> in your configuration and set
            <code>Mcp:ApiKey</code> to a non-empty value.</p>
          </div>
        </body>
        </html>
        """;
    }
}
