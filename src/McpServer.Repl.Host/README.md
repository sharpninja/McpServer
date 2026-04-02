# McpServer.Repl.Host

A command-line REPL (Read-Eval-Print Loop) host for interacting with the Model Context Protocol (MCP) server. Provides both interactive and agent STDIO modes for workspace management, session logging, TODO tracking, and requirements management.

## Installation

### As a .NET Global Tool

```powershell
# Pack the tool (from solution root)
./build.ps1 PackReplTool

# Install globally
./build.ps1 InstallReplTool

# Or install manually
dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### Update Existing Installation

```powershell
./build.ps1 InstallReplTool --update-tool

# Or manually
dotnet tool update --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### Uninstall

```powershell
./build.ps1 InstallReplTool --uninstall-tool

# Or manually
dotnet tool uninstall --global SharpNinja.McpServer.Repl
```

## Usage

### Check Version

```bash
mcpserver-repl --version
```

### Interactive Mode

Interactive mode provides a user-friendly wizard interface for common MCP workflows:

```bash
mcpserver-repl --interactive
```

#### Available Workflows

1. **Bootstrap Session** - Create a new session log with initial metadata
2. **Begin Turn** - Start a new turn in an existing session
3. **Create TODO** - Add a new TODO item to the workspace
4. **List Requirements** - View functional, technical, or testing requirements
5. **Switch Workspace** - Change the active workspace
6. **Exit** - Close the interactive session

### Agent STDIO Mode

Agent STDIO mode implements the MCP protocol over standard input/output for programmatic integration:

```bash
mcpserver-repl --agent-stdio
```

This mode is designed for:
- AI agent integration
- Automated workflows
- MCP protocol compliance testing
- Batch processing

## Configuration

The tool connects to the MCP server using the `MCP_SERVER_URL` environment variable. If not set, it defaults to `http://localhost:5000`.

```powershell
# Set custom server URL
$env:MCP_SERVER_URL = "http://localhost:5000"
mcpserver-repl --interactive
```

## Features

### Session Management
- Create and bootstrap new sessions
- Add turns to existing sessions
- Track session metadata (agent, model, timestamps)
- Append processing dialog items

### TODO Management
- Create TODO items with structured metadata
- Select section (Planning, In-Progress, Done, Blocked)
- Assign priority (P0-Critical, P1-High, P2-Medium, P3-Low)
- Add estimates and descriptions

### Requirements Tracking
- List functional requirements (FR)
- List technical requirements (TR)
- List testing requirements (TEST)
- View requirement details in formatted tables

### Workspace Operations
- List available workspaces
- Switch between workspaces
- Workspace-scoped operations

## Architecture

### Project Structure
- `Program.cs` - Entry point with System.CommandLine integration
- `InteractiveHandler.cs` - Interactive mode with Spectre.Console wizards
- `AgentStdioHandler.cs` - STDIO protocol handler for MCP agents
- `ServiceCollectionExtensions.cs` - Dependency injection configuration

### Dependencies
- **System.CommandLine** - Command-line parsing
- **Spectre.Console** - Rich terminal UI
- **McpServer.Client** - MCP server REST API client
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting

## Development

### Build

```powershell
./build.ps1 Compile --configuration Release
# or: dotnet build src/McpServer.Repl.Host/McpServer.Repl.Host.csproj --configuration Release
```

### Pack

```powershell
./build.ps1 PackReplTool
# or: dotnet pack src/McpServer.Repl.Host/McpServer.Repl.Host.csproj --configuration Release --output ./local-packages
```

### Run Locally (Without Installing)

```powershell
dotnet run --project src/McpServer.Repl.Host -- --interactive
```

## License

MIT

## Author

SharpNinja

## Repository

https://github.com/SharpNinja/McpServer
