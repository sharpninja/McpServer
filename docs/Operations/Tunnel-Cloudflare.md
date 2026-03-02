# Cloudflare Tunnel for Single-Port MCP Remote/Mobile Access

This runbook documents how to expose an MCP server through `cloudflared` using the built-in
`CloudflareTunnelProvider` (`Mcp:Tunnel:Provider = "cloudflare"`).

This is the preferred no-open-port path when Cloudflare Tunnel and DNS are available.

## Strategy Decision (Current)

For `cloudflare` in single-public-port environments, MCP exposes only the primary host endpoint
(typically `:7147`) through the tunnel.

Current behavior and scope:

- Publicly exposed: primary MCP host routes on the tunneled port (`/health`, `/auth/*`, primary-host `/mcpserver/*`)
- Not publicly exposed: child workspace listeners on `7147+`
- If you need direct remote access to child workspace ports, use FRP TCP mode or implement a future primary-host workspace proxy/gateway feature

## Provider Modes Supported Today

The current provider supports two `cloudflared` launch modes:

- Quick tunnel: `cloudflared tunnel --url http://localhost:{port}` (temporary `*.trycloudflare.com` URL)
- Named tunnel: `cloudflared tunnel run {TunnelName}` (stable tunnel identity; hostname/DNS routing is managed outside MCP)

Implementation note:

- The provider captures quick-tunnel URLs by reading `cloudflared` stderr.
- In named-tunnel mode, a public URL may not be emitted to stderr, so tunnel status may show running with no captured URL. Validate using your known hostname.

## Prerequisites

- `cloudflared` CLI installed and available on `PATH`
- MCP server running on the host
- For quick tunnels: no Cloudflare zone setup required (testing/dev use)
- For named tunnels (recommended for stable deployments):
  - Cloudflare account + DNS zone
  - `cloudflared` authenticated for the runtime environment/account
  - Named tunnel created ahead of time
  - DNS hostname route created for the tunnel (dashboard or `cloudflared` command)
  - Credentials/config available to the account running MCP

## Configure MCP

Update your MCP `appsettings.json` (Windows service: `C:\ProgramData\McpServer\appsettings.json`) with one of the following.

### Quick Tunnel (Dev/Ad Hoc)

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "cloudflare",
      "Port": 7147,
      "Cloudflare": {}
    }
  }
}
```

Optional quick-tunnel hostname setting (only if your `cloudflared` workflow supports it):

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "cloudflare",
      "Port": 7147,
      "Cloudflare": {
        "Hostname": "mcp.example.com"
      }
    }
  }
}
```

### Named Tunnel (Recommended for Stable Hostnames)

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "cloudflare",
      "Port": 7147,
      "Cloudflare": {
        "TunnelName": "mcp-prod"
      }
    }
  }
}
```

Notes:

- `Mcp:Tunnel:Provider` selects the provider. `Cloudflare:Enabled` is not the selector.
- In the current provider implementation, `Cloudflare:Hostname` is only passed when `TunnelName` is not set (quick-tunnel launch path).
- For named tunnels, configure hostname routing outside MCP (Cloudflare dashboard or `cloudflared tunnel route dns ...`).

## Remote/Mobile Access Model (Single Hostname)

Use the Cloudflare hostname for:

- `GET /health` (connectivity smoke test)
- `GET /auth/config`, `POST /auth/device`, `POST /auth/token`, `GET/POST /auth/ui/*` (OIDC device-flow + browser proxy on the same host)
- Primary-host REST endpoints under `/mcpserver/*` (with `X-Api-Key`)

Do not assume the Cloudflare hostname exposes child workspaces on `7147+`.

If a remote client needs a non-primary workspace endpoint:

- Prefer FRP TCP mode (`Mcp:Tunnel:Provider = "frp"`) with explicit/range port mappings
- Or add a future primary-host reverse proxy/gateway feature and document the route contract

## Start and Verify

1. Start or restart MCP.
1. Check logs for cloudflared startup (`cloudflared started (PID ...)`).
1. Determine the public hostname:
   - Quick tunnel: capture `https://*.trycloudflare.com` from logs
   - Named tunnel: use the preconfigured Cloudflare hostname
1. Validate the public endpoint:

   ```text
   https://<your-cloudflare-host>/health
   ```

   Expected result: HTTP 200 with a healthy JSON response.

1. Validate OIDC proxy discovery (if auth is enabled):

   ```text
   https://<your-cloudflare-host>/auth/config
   ```

1. Validate an authenticated MCP endpoint (replace API key):

   ```bash
   curl https://<your-cloudflare-host>/mcpserver/workspace -H "X-Api-Key: <workspace-api-key>"
   ```

## Troubleshooting

### `cloudflared CLI not found`

- Install `cloudflared` and ensure it is on `PATH` for the account running MCP.

### cloudflared starts but no public URL is captured in MCP status/logs

This can be expected in named-tunnel mode.

- Confirm the process is running
- Validate using the known configured hostname
- Check cloudflared logs outside MCP if needed

### Named tunnel fails to start / authentication error

Common causes:

- `cloudflared` not authenticated for the runtime account
- Missing/incorrect tunnel credentials/config
- `TunnelName` does not exist

Verify `cloudflared tunnel list` and `cloudflared tunnel run <TunnelName>` manually under the same account.

### Public hostname routes but MCP endpoints fail

- Verify the Cloudflare tunnel route points to the local MCP port (`7147`)
- Verify local `http://localhost:7147/health` succeeds before testing the public hostname

### Public hostname works for `/health` but remote workspace access is incomplete

- Expected in the current strategy if the client is trying to reach child workspace ports (`7147+`)
- Use the primary host endpoints only, or switch to FRP for multi-port exposure

## Validation Checklist

- [ ] `cloudflared` CLI installed on the MCP host
- [ ] MCP starts cloudflared without immediate exit
- [ ] Public hostname is known (quick tunnel URL or named tunnel DNS hostname)
- [ ] `GET /health` works through the public Cloudflare hostname
- [ ] `GET /auth/config` works through the public Cloudflare hostname (when auth enabled)
- [ ] Authenticated `GET /mcpserver/workspace` works with `X-Api-Key`
- [ ] Team understands current scope: primary host only, child workspace ports remain private

## Provider Hardening Status

Implemented in `CloudflareTunnelProvider`:

- Startup timeout polling (8s) for quick tunnels waiting on URL emission
- Process output capture (stdout/stderr) for diagnostics and URL extraction
- Process-exit monitoring and crash-aware `GetStatusAsync` error reporting (startup vs post-start exit)
- Named tunnel mode handling that allows successful start without auto-captured URL and directs operators to use the configured hostname

Potential future improvements (optional):

- Emit/track a configured public hostname in status for named tunnels when `Cloudflare:TunnelName` is used
- Add more structured cloudflared log parsing and readiness detection beyond URL capture heuristics
