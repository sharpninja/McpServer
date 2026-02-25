# McpServer Director (`director`)

`director` is the .NET global tool for administering an MCP Server instance.

It provides:

- CLI commands for health, workspaces, agents, sync, TODOs, and session logs
- OIDC login/logout (`director login`, `whoami`)
- `exec` for running MVVM/CQRS ViewModel commands
- Interactive TUI (`director ui`)

## Install

```bash
dotnet tool install --global SharpNinja.McpServer.Director
```

Development/local package install:

```bash
dotnet tool install --global SharpNinja.McpServer.Director --add-source <path-to-nupkg-folder> --ignore-failed-sources
```

## Connecting To MCP Server

### Preferred (workspace marker)

Run `director` inside a workspace that contains `AGENTS-README-FIRST.yaml`, or pass `--workspace <path>`.

The marker provides:

- MCP base URL
- workspace API key
- workspace path context

### Default URL (new system / outside a workspace)

Set a default MCP server URL once:

```bash
director config set-default-url http://localhost:7147
director config show
```

This enables non-workspace commands (for example `health`) when no marker file is present.

Note: workspace/TODO operations still require a workspace marker/API key.

## Common Commands

```bash
director health
director list
director agents defs
director agents ws --workspace E:\github\MyRepo
director sync status
director ui
```

## Auth Commands

```bash
director login
director whoami
director logout
```

## MVVM/CQRS Exec Mode

Discover available ViewModels/aliases:

```bash
director list-viewmodels
director list-viewmodels --filter todo
```

Run a ViewModel command:

```bash
director exec list-todos -i "{\"Section\":\"mvp-mcp\"}"
director exec get-workspace -i "{\"WorkspacePath\":\"E:\\github\\FunWasHad\"}"
```

## Interactive TUI

Launch the Terminal UI:

```bash
director ui
```

Current UI includes role-filtered tabs and auto-refresh on tab entry for supported tabs.

## Troubleshooting

- No marker found: set a default URL with `director config set-default-url <url>` or run inside a workspace.
- Permission denied for workspace/policy tabs: re-login and confirm your token has the `admin` role.
- Expired token: run `director login` again.

## Repository

- Source: <https://github.com/sharpninja/McpServer>
