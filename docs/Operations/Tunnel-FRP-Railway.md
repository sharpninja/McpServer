# FRP Tunnel on Railway (`frps`)

This runbook documents how to expose a local MCP server through FRP using:

- local `frpc` managed by `McpServer.Support.Mcp` (`Mcp:Tunnel:Provider = "frp"`)
- self-hosted `frps` running on Railway

This is the recommended path when you want a self-hosted reverse proxy instead of ngrok or cloudflared.

## Architecture

1. MCP server runs locally (for example on `http://localhost:7147`)
2. MCP starts the built-in FRP tunnel provider (`FrpTunnelProvider`)
3. The provider launches local `frpc`
4. `frpc` connects to Railway-hosted `frps`
5. Public HTTP traffic reaches Railway `frps`, which forwards to local MCP through FRP

## Prerequisites

- `frpc` installed on the MCP host and available on `PATH`
- Railway account/project
- Docker-based deployment from `infra/frp/railway/`
- A strong shared FRP token

## Deploy `frps` to Railway

Use the assets in:

- `infra/frp/railway/Dockerfile`
- `infra/frp/railway/entrypoint.sh`
- `infra/frp/railway/frps.toml.template`

### Railway service variables

Set these on the Railway service:

- `FRP_TOKEN` (required)
- `FRPS_BIND_PORT` (default `7000`) - FRP control connection (`frpc` -> `frps`)
- `FRPS_VHOST_HTTP_PORT` (default `8080`) - public HTTP vhost port handled by `frps`
- `FRPS_LOG_LEVEL` (default `info`)
- `FRPS_SUBDOMAIN_HOST` (optional, only if using FRP HTTP subdomains)
- `FRPS_ALLOW_PORTS_START` / `FRPS_ALLOW_PORTS_END` (optional, recommended for TCP range mode; example `7147` / `7160`)

### Railway networking

Configure both of these (HTTP mode):

1. TCP Proxy for `FRPS_BIND_PORT`
2. Public HTTP exposure for `FRPS_VHOST_HTTP_PORT` (Railway domain or custom domain)

Record:

- TCP Proxy host and port (used by local MCP `Frp:ServerAddress` and `Frp:ServerPort`)
- Public HTTP domain (used by local MCP `Frp:PublicBaseUrl` or `Frp:CustomDomain`)

### Railway networking for TCP range mode (`7147-7160`)

If you want MCP/workspace ports exposed as raw TCP (1:1), configure:

1. TCP Proxy for `FRPS_BIND_PORT` (control connection)
2. TCP Proxy mappings for each FRP remote port in the range (for example `7147` through `7160`) to the `frps` service

Recommendation:

- Set `FRPS_ALLOW_PORTS_START=7147` and `FRPS_ALLOW_PORTS_END=7160` on the `frps` service to restrict remote TCP ports to the expected range.

## Configure MCP (local `frpc`)

Update your MCP `appsettings.json` (service deployment config) with a tunnel section like this:

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "frp",
      "Port": 7147,
      "Frp": {
        "ServerAddress": "your-railway-tcp-proxy-host",
        "ServerPort": 443,
        "Token": "same-strong-token",
        "ProxyType": "http",
        "PublicBaseUrl": "https://your-public-railway-domain",
        "StartupTimeoutSeconds": 8
      }
    }
  }
}
```

Notes:

- `Port` must match the local MCP HTTP listener you want to expose.
- `ProxyType` supports `http` and `tcp`.
- `PublicBaseUrl` is recommended on Railway so status reporting returns the correct public URL.

### MCP TCP range configuration example (`7147-7160`)

This lets MCP generate `frpc` mappings and define what FRP exposes:

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "frp",
      "Frp": {
        "ServerAddress": "your-railway-frps-control-proxy-host",
        "ServerPort": 443,
        "Token": "same-strong-token",
        "ProxyType": "tcp",
        "TcpPortRangeStart": 7147,
        "TcpPortRangeEnd": 7160,
        "StartupTimeoutSeconds": 8
      }
    }
  }
}
```

Notes:

- This is a 1:1 mapping range (local port `7147` maps to remote port `7147`, etc.).
- Railway must still expose TCP proxies for each remote port in the range.

## Start and verify

1. Start/restart MCP server.
2. Check MCP logs for FRP provider startup messages.
3. Confirm the tunnel provider reports a public URL.
4. Hit the public URL health endpoint:

```text
https://your-public-railway-domain/health
```

Expected result: HTTP 200 with healthy JSON.

## Local parity test (before Railway)

You can run a local `frps` instance using:

- `infra/frp/local/docker-compose.frps.yml`

Then point MCP `Frp:ServerAddress=127.0.0.1` and `Frp:ServerPort=7000` for local smoke testing.

## Troubleshooting

### `frpc CLI not found`

- Install `frpc` on the MCP host.
- Ensure the executable is available on `PATH` for the account running the service.

### `frpc exited during startup`

Common causes:

- `FRP_TOKEN` mismatch between local MCP config and Railway `frps`
- wrong Railway TCP Proxy host/port for `Frp:ServerAddress` / `Frp:ServerPort`
- outbound firewall restrictions from the MCP host
- unsupported `ProxyType`
- missing Railway TCP proxy for one of the requested TCP ports

Check MCP logs for the `frpc` exit message and captured stderr/stdout snippet.

### Public URL returns error / timeout

- Verify Railway is exposing `FRPS_VHOST_HTTP_PORT`
- Verify `Mcp:Tunnel:Port` matches the local MCP port
- Verify local MCP `/health` is reachable before testing FRP

### Wrong URL shown in tunnel status

- Set `Mcp:Tunnel:Frp:PublicBaseUrl` explicitly (recommended on Railway)

### `Subdomain` and `CustomDomain` both set

- Only set one of them; the provider rejects both at the same time

## Validation checklist

- [ ] Railway `frps` service deploys successfully
- [ ] Railway TCP Proxy for `FRPS_BIND_PORT` is reachable
- [ ] Railway public HTTP endpoint for `FRPS_VHOST_HTTP_PORT` is reachable
- [ ] MCP starts `frpc` without startup exit
- [ ] MCP tunnel status reports a public URL
- [ ] `GET /health` works through the public FRP URL
