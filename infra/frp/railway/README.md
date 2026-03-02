# FRP (`frps`) on Railway

This folder contains deployment assets for running a self-hosted FRP server (`frps`) on Railway.

## Purpose

- Host `frps` publicly on Railway
- Allow a local MCP service to run `frpc` (`Mcp:Tunnel:Provider = "frp"`)
- Tunnel the local MCP HTTP port (for example `7147`) through FRP

## Files

- `Dockerfile`: builds a minimal `frps` container image
- `entrypoint.sh`: generates `frps.toml` from environment variables and starts `frps`
- `frps.toml.template`: base FRP server config template
- `.env.example`: example environment variables

## Railway Setup (MVP HTTP mode)

1. Create a new Railway service from this folder (`infra/frp/railway`).
2. Set Railway service variables:
   - `FRP_TOKEN` (required)
   - `FRPS_BIND_PORT` (default `7000`)
   - `FRPS_VHOST_HTTP_PORT` (default `8080`)
   - `FRPS_LOG_LEVEL` (default `info`)
   - `FRPS_SUBDOMAIN_HOST` (optional)
3. Expose ports:
   - Add a Railway TCP Proxy for `FRPS_BIND_PORT` so local `frpc` can connect to `frps`
   - Expose `FRPS_VHOST_HTTP_PORT` publicly (Railway domain or custom domain) for FRP HTTP traffic
4. Record:
   - Railway TCP proxy host + port for `frpc` (`ServerAddress`, `ServerPort`)
   - Railway public HTTP domain for MCP public URL (`PublicBaseUrl` or `CustomDomain`)

## TCP range mode (for MCP/workspace ports like `7147-7160`)

If you want FRP to expose a 1:1 TCP port range (for example MCP + workspace ports), add these Railway service variables:

- `FRPS_ALLOW_PORTS_START=7147`
- `FRPS_ALLOW_PORTS_END=7160`

Then create Railway TCP proxies mapping each external port to the same internal `frps` service port:

- external `7147` -> service `frps` port `7147`
- ...
- external `7160` -> service `frps` port `7160`

On the MCP host, configure `Mcp:Tunnel:Frp` with:

- `ProxyType = "tcp"`
- `TcpPortRangeStart = 7147`
- `TcpPortRangeEnd = 7160`

This makes MCP generate the `frpc` config for the range so the server controls what FRP exposes.

## MCP (local `frpc`) configuration example

Set in your MCP server `appsettings.json`:

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "frp",
      "Port": 7147,
      "Frp": {
        "ServerAddress": "your-railway-tcp-proxy-host",
        "ServerPort": 443,
        "Token": "same-token-as-frps",
        "ProxyType": "http",
        "PublicBaseUrl": "https://your-railway-public-domain"
      }
    }
  }
}
```

Adjust `ServerPort` to the actual Railway TCP Proxy port, and `PublicBaseUrl` to the public domain serving `FRPS_VHOST_HTTP_PORT`.

### MCP TCP range example (`7147-7160`)

```json
{
  "Mcp": {
    "Tunnel": {
      "Provider": "frp",
      "Frp": {
        "ServerAddress": "your-railway-frps-tcp-proxy-host",
        "ServerPort": 443,
        "Token": "same-token-as-frps",
        "ProxyType": "tcp",
        "TcpPortRangeStart": 7147,
        "TcpPortRangeEnd": 7160
      }
    }
  }
}
```

## Local smoke test (before Railway)

You can run `frps` locally with Docker Compose (see `../local/docker-compose.frps.yml`) and then point MCP `Frp` settings at `127.0.0.1:7000`.

## Troubleshooting

- `frpc CLI not found`: install `frpc` on the MCP host (the local tunnel client)
- `frpc exited during startup`: check token mismatch, blocked outbound port, or wrong Railway TCP proxy target port
- Public URL reachable but MCP not responding: verify `Mcp:Tunnel:Port` matches the local MCP listening port
- Wrong public URL in status: set `Mcp:Tunnel:Frp:PublicBaseUrl`
