# ngrok Tunnel for Single-Port MCP Remote/Mobile Access

This runbook documents how to expose an MCP server through `ngrok` using the built-in
`NgrokTunnelProvider` (`Mcp:Tunnel:Provider = "ngrok"`).

This is the preferred managed-tunnel path when you need a single public endpoint quickly and
do not want to self-host FRP.

## Strategy Decision (Current)

For `ngrok` in single-public-port environments, MCP exposes only the primary host endpoint
(typically `:7147`) through the tunnel.

Current behavior and scope:

- Publicly exposed: primary MCP host routes on the tunneled port (`/health`, `/auth/*`, primary-host `/mcpserver/*`)
- Not publicly exposed: child workspace listeners on `7147+`
- If you need direct remote access to child workspace ports, use FRP TCP mode or implement a future primary-host workspace proxy/gateway feature

This runbook documents the primary-only model and the validation steps around it.

## Architecture

1. MCP server runs locally (for example on `http://localhost:7147`)
2. MCP starts the built-in `NgrokTunnelProvider`
3. The provider launches `ngrok http {Mcp:Tunnel:Port}`
4. ngrok creates a public HTTPS URL and forwards traffic to local MCP
5. Remote/mobile clients call the ngrok URL and stay on one public host/port

## Prerequisites

- `ngrok` CLI installed and available on `PATH`
- ngrok account (recommended)
- MCP server running on the host
- Optional ngrok auth token (recommended for stable account usage and higher limits)

## Configure MCP

Update your MCP `appsettings.json` (Windows service: `C:\ProgramData\McpServer\appsettings.json`) with:

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "ngrok",
      "Port": 7147,
      "Ngrok": {
        "AuthToken": "YOUR_NGROK_AUTH_TOKEN",
        "Region": "us"
      }
    }
  }
}
```

Optional settings:

- `Mcp:Tunnel:Ngrok:Subdomain` (requires a plan that supports subdomains)
- `Mcp:Tunnel:Ngrok:Region` (for example `us`, `eu`, `ap`)

Notes:

- `Mcp:Tunnel:Provider` selects the provider. `Ngrok:Enabled` is not the selector.
- The provider passes the auth token via the `NGROK_AUTHTOKEN` environment variable (not a CLI arg), which reduces secret exposure in process listings.
- If `AuthToken` is omitted, ngrok may still work if the runtime account already has ngrok configured locally.

## Remote/Mobile Access Model (Single Port)

Use the ngrok public URL for:

- `GET /health` (connectivity smoke test)
- `GET /auth/config`, `POST /auth/device`, `POST /auth/token`, `GET/POST /auth/ui/*` (OIDC device-flow + browser proxy on the same host)
- Primary-host REST endpoints under `/mcpserver/*` (with `X-Api-Key`)

Do not assume the ngrok URL exposes child workspaces on `7147+`.

If a remote client needs a non-primary workspace endpoint:

- Prefer FRP TCP mode (`Mcp:Tunnel:Provider = "frp"`) with explicit/range port mappings
- Or add a future primary-host reverse proxy/gateway feature and document the route contract

## Start and Verify

1. Start or restart MCP.
1. Check logs for ngrok startup (`ngrok started (PID ...)`).
1. Confirm the provider reports a public URL in logs, or inspect the local ngrok API on the host (`http://127.0.0.1:4040/api/tunnels`).
1. Validate the public URL:

   ```text
   https://<your-ngrok-host>/health
   ```

   Expected result: HTTP 200 with a healthy JSON response.

1. Validate OIDC proxy discovery (if auth is enabled):

   ```text
   https://<your-ngrok-host>/auth/config
   ```

1. Validate an authenticated MCP endpoint (replace API key):

   ```bash
   curl https://<your-ngrok-host>/mcpserver/workspace -H "X-Api-Key: <workspace-api-key>"
   ```

## Troubleshooting

### `ngrok CLI not found`

- Install ngrok and ensure `ngrok` is on `PATH` for the account running MCP.

### ngrok starts but MCP does not report a public URL

Common causes:

- ngrok local API not ready yet (`127.0.0.1:4040`)
- `curl` not available on the host/runtime account (provider currently uses `curl` to query the local ngrok API)
- ngrok startup/auth failure before tunnel creation

Check MCP logs and run `ngrok version` / `ngrok http 7147` manually on the host for isolation.

### `--subdomain` rejected / tunnel start fails

- ngrok account plan may not support the requested subdomain
- Subdomain may already be in use

Try without `Subdomain` first.

### Public URL works for `/health` but remote workspace access is incomplete

- Expected in the current strategy if the client is trying to reach child workspace ports (`7147+`)
- Use the primary host endpoints only, or switch to FRP for multi-port exposure

## Validation Checklist

- [ ] `ngrok` CLI installed on the MCP host
- [ ] MCP starts ngrok without immediate exit
- [ ] ngrok public URL is visible in MCP logs (or ngrok local API)
- [ ] `GET /health` works through the public ngrok URL
- [ ] `GET /auth/config` works through the public ngrok URL (when auth enabled)
- [ ] Authenticated `GET /mcpserver/workspace` works with `X-Api-Key`
- [ ] Team understands current scope: primary host only, child workspace ports remain private

## Provider Hardening Status

Implemented in `NgrokTunnelProvider`:

- Startup timeout polling (8s) while waiting for the ngrok local API to return a tunnel URL
- Clearer startup-failure diagnostics including last ngrok API query error and last stdout/stderr lines
- Process-exit monitoring and crash-aware `GetStatusAsync` error reporting (startup vs post-start exit)
- Better URL selection from ngrok local API (prefers HTTPS tunnel when multiple entries exist)

Potential future improvements (optional):

- Replace the `curl` dependency for ngrok local API polling with direct HTTP calls
- Add structured parsing of ngrok JSON log lines for richer diagnostics/status
