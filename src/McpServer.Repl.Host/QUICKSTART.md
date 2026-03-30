# McpServer.Repl.Host Quick Start Guide

## Installation (3 Steps)

### 1. Pack the Tool
```powershell
.\scripts\Pack-ReplTool.ps1
```

### 2. Install Globally
```powershell
.\scripts\Install-ReplTool.ps1
```

### 3. Verify
```powershell
mcpserver-repl --version
```

## Basic Usage

### Interactive Mode
```bash
mcpserver-repl --interactive
```

### Agent STDIO Mode
```bash
mcpserver-repl --agent-stdio
```

## Common Tasks

### Bootstrap a New Session
1. Run `mcpserver-repl --interactive`
2. Select workspace
3. Choose "Bootstrap Session"
4. Enter session details

### Create a TODO
1. Run `mcpserver-repl --interactive`
2. Select workspace
3. Choose "Create TODO"
4. Fill in TODO details

### List Requirements
1. Run `mcpserver-repl --interactive`
2. Select workspace
3. Choose "List Requirements"
4. Select requirement type (FR/TR/TEST)

## Update Tool

```powershell
.\scripts\Install-ReplTool.ps1 -Update
```

## Uninstall

```powershell
.\scripts\Install-ReplTool.ps1 -Uninstall
```

## Configuration

Set MCP server URL (optional):
```powershell
$env:MCP_SERVER_URL = "http://localhost:5000"
```

## Troubleshooting

### Tool Not Found
```powershell
# Check if installed
dotnet tool list -g

# Reinstall
.\scripts\Install-ReplTool.ps1 -Uninstall
.\scripts\Install-ReplTool.ps1
```

### Can't Connect to Server
```powershell
# Check server is running
curl http://localhost:5000/health

# Set correct URL
$env:MCP_SERVER_URL = "http://localhost:5000"
```

## More Information

See [README.md](README.md) for detailed documentation.
