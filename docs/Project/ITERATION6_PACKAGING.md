# Iteration 6: McpServer.Repl.Host Tool Packaging

## Overview

Iteration 6 completes the packaging of `McpServer.Repl.Host` as a .NET global tool with the package ID `SharpNinja.McpServer.Repl` and command name `mcpserver-repl`. The implementation includes full interactive mode with Spectre.Console wizards for common workflows.

## Package Configuration

### .csproj Properties

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>mcpserver-repl</ToolCommandName>
<PackageId>SharpNinja.McpServer.Repl</PackageId>
<Version>6.0.0</Version>
<Authors>SharpNinja</Authors>
<Description>MCP Server REPL Host - Interactive and STDIO modes for Model Context Protocol integration with workspace session logs, TODO management, and requirements tracking.</Description>
<PackageTags>mcp;repl;model-context-protocol;session-log;todo;requirements;cli</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<RepositoryUrl>https://github.com/SharpNinja/McpServer</RepositoryUrl>
<PackageProjectUrl>https://github.com/SharpNinja/McpServer</PackageProjectUrl>
```

## Interactive Mode Features

The `--interactive` mode provides a rich terminal UI using Spectre.Console with the following workflows:

### 1. Bootstrap Session
- Prompts for agent name (default: "Tonkotsu")
- Auto-generates session ID or accepts custom ID
- Captures model name (default: "claude-3-5-sonnet-20241022")
- Records session purpose
- Creates initial session log entry

### 2. Begin Turn
- Prompts for agent name and session ID
- Auto-generates request ID or accepts custom ID
- Captures interpretation and response
- Creates turn entry in session log

### 3. Create TODO
- Structured TODO item creation wizard
- ID validation (e.g., IMPL-MCP-001)
- Section selection: Planning, In-Progress, Done, Blocked
- Priority selection: P0-Critical, P1-High, P2-Medium, P3-Low
- Optional estimate and description
- Displays created TODO in formatted table

### 4. List Requirements
- Three requirement types:
  - Functional Requirements (FR)
  - Technical Requirements (TR)
  - Testing Requirements (TEST)
- Formatted table display with:
  - ID column
  - Title/Description column
  - Additional metadata columns
  - Total count summary

### 5. Switch Workspace
- Lists all registered workspaces
- Selection prompt with workspace paths
- Updates active workspace context

## Scripts

### Pack-ReplTool.ps1

Builds, packs, and publishes the tool to the local NuGet feed:

```powershell
.\scripts\Pack-ReplTool.ps1 [-Clean] [-SkipBuild]
```

Parameters:
- `-Clean`: Clean the project before building
- `-SkipBuild`: Skip build and only pack

Output:
- Creates NuGet package in `./local-packages`
- Displays installation instructions

### Install-ReplTool.ps1

Installs, updates, or uninstalls the global tool:

```powershell
# Install
.\scripts\Install-ReplTool.ps1

# Update
.\scripts\Install-ReplTool.ps1 -Update

# Uninstall
.\scripts\Install-ReplTool.ps1 -Uninstall
```

Features:
- Automatic version verification after installation
- Displays available commands

## Installation Workflow

### 1. Pack the Tool

```powershell
# From solution root
.\scripts\Pack-ReplTool.ps1
```

This will:
1. Build the project in Release configuration
2. Create NuGet package in `./local-packages/`
3. Display installation instructions

### 2. Install Globally

```powershell
# Using helper script
.\scripts\Install-ReplTool.ps1

# Or manually
dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### 3. Verify Installation

```powershell
mcpserver-repl --version
```

Expected output:
```
mcpserver-repl version 6.0.0
```

## Usage

### Interactive Mode

```bash
mcpserver-repl --interactive
```

Launches an interactive wizard with:
- Figlet ASCII art header
- Workspace selection menu
- Action selection menu
- Workflow wizards for each action

### Agent STDIO Mode

```bash
mcpserver-repl --agent-stdio
```

Runs in STDIO mode for:
- MCP protocol communication
- AI agent integration
- Automated workflows

### Version Check

```bash
mcpserver-repl --version
```

Displays version information from `AssemblyInformationalVersionAttribute`.

## Configuration

### Environment Variables

- `MCP_SERVER_URL`: MCP server base URL (default: `http://localhost:5000`)

Example:
```powershell
$env:MCP_SERVER_URL = "http://localhost:5000"
mcpserver-repl --interactive
```

## NuGet Configuration

Updated `NuGet.config` to include the package in local-packages source mapping:

```xml
<packageSource key="local-packages">
  <package pattern="MarkdownServer" />
  <package pattern="SharpNinja.McpServer.Repl" />
</packageSource>
```

## .gitignore Updates

Added `local-packages/` directory to .gitignore to exclude generated NuGet packages from version control.

## Architecture

### Dependencies

- **System.CommandLine**: Command-line parsing and routing
- **Spectre.Console**: Rich terminal UI components
- **McpServer.Client**: MCP server REST API client
- **Microsoft.Extensions.Hosting**: DI container and hosting

### Components

1. **Program.cs**
   - Entry point
   - Command routing
   - DI configuration
   - Version handling

2. **InteractiveHandler.cs**
   - Interactive mode implementation
   - Spectre.Console wizards
   - Workspace selection
   - Workflow execution

3. **AgentStdioHandler.cs**
   - STDIO protocol handler
   - Envelope processing
   - Error handling

4. **ServiceCollectionExtensions.cs**
   - DI service registration
   - Client configuration

## Validation Steps

1. **Pack Tool**
   ```powershell
   .\scripts\Pack-ReplTool.ps1
   ```
   - Verify package created in `./local-packages/`
   - Check package filename: `SharpNinja.McpServer.Repl.6.0.0.nupkg`

2. **Install Tool**
   ```powershell
   dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages
   ```
   - Verify successful installation message
   - Check tool is in global tools list: `dotnet tool list -g`

3. **Verify Version**
   ```powershell
   mcpserver-repl --version
   ```
   - Should display: `mcpserver-repl version 6.0.0` (or current version)

4. **Test Interactive Mode**
   ```powershell
   mcpserver-repl --interactive
   ```
   - Verify Figlet header displays
   - Check workspace selection menu appears
   - Test workflow wizards

5. **Test STDIO Mode**
   ```powershell
   echo '{"test":"input"}' | mcpserver-repl --agent-stdio
   ```
   - Verify STDIO processing (if server is running)

## Troubleshooting

### Tool Not Found After Installation

```powershell
# Ensure .NET tools path is in PATH
dotnet tool list -g

# Reinstall with verbose output
dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages --verbosity detailed
```

### Version Mismatch

```powershell
# Update to latest version
dotnet tool update --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### Package Not Found

```powershell
# Verify package exists
ls ./local-packages/*.nupkg

# Re-pack if needed
.\scripts\Pack-ReplTool.ps1 -Clean
```

## Future Enhancements

Potential additions for future iterations:
- Command history and autocomplete
- Configuration file support
- Batch operation mode
- Export/import session data
- Custom workflow templates
- Plugin system for extensions
