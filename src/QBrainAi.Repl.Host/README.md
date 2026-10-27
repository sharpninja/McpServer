# QBrainAi.Repl.Host

A command-line REPL (Read-Eval-Print Loop) host for interacting with the Model Context Protocol (MCP) server. Provides both interactive and agent STDIO modes for workspace management, session logging, TODO tracking, and requirements management.

## Installation

### As a .NET Global Tool

```powershell
# Pack the tool (from solution root)
./build.ps1 PackReplTool

# Install globally
./build.ps1 InstallReplTool

# Or install manually
dotnet tool install --global QBrainAI.Repl --add-source ./local-packages
```

### Update Existing Installation

```powershell
./build.ps1 InstallReplTool --update-tool

# Or manually
dotnet tool update --global QBrainAI.Repl --add-source ./local-packages
```

### Uninstall

```powershell
./build.ps1 InstallReplTool --uninstall-tool

# Or manually
dotnet tool uninstall --global QBrainAI.Repl
```

### Self-contained application bundles

From the repository root, using the .NET SDK selected by `global.json`:

```powershell
# Build all six supported platforms.
./build.ps1 BuildReplBundles --configuration Release

# Build only the current Linux x64 platform during development.
./build.ps1 BuildReplBundles --configuration Release --repl-runtime-identifiers linux-x64
```

Supported runtimes are `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`,
`win-x64`, and `win-arm64`. Each run creates a separate directory under
`artifacts/repl-bundles/`, with one `.tar.gz` per selected runtime and a
`manifest.json` containing source commit, dirty-source status, version, the project
license file path, archive SHA256, and per-file lengths and SHA256 digests. Archives retain
all published supporting files. `LICENSE.QBrainAI.txt` copies the repository's
root `LICENSE` verbatim; `LICENSE.NET.txt` and `THIRD-PARTY-NOTICES.NET.txt`
come from the exact resolved runtime package. Unix executable
permissions are recorded in the archive. A failed run cannot overwrite an
earlier snapshot and does not produce a completion manifest.

The root `LICENSE` currently contains Apache 2.0 while the REPL project's existing
package metadata declares MIT. The bundle manifest references the copied file
without assigning a conflicting license identifier. Resolve this existing
metadata conflict before publishing a distribution.

The target publishes a self-contained `qbrain-ai-repl` application
(`qbrain-ai-repl.exe` on Windows), with tool packing and trimming disabled.
Application naming is scoped to the REPL host project, preserving referenced
assembly names and the existing global-tool build. Bundle users do not install
.NET; the target platform's native OS libraries remain prerequisites.

When a selected runtime matches the build host, the target also runs the built
executable with `--version`, without a discoverable shared .NET installation.
Other runtimes still need execution tests on their own hosts. This version smoke
does not verify MCP bootstrap, server compatibility, or an approved artifact pin.
Use a reviewed source commit and the manifest digests when advancing a host pin.
This target builds local artifacts only; it does not install or deploy them.

## Usage

### Check Version

```bash
qbrain-ai-repl --version
```

### Interactive Mode

Interactive mode provides a user-friendly wizard interface for common MCP workflows:

```bash
qbrain-ai-repl --interactive
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
qbrain-ai-repl --agent-stdio --agent Codex
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
qbrain-ai-repl --interactive
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
- **QBrainAi.Client** - MCP server REST API client
- **Microsoft.Extensions.Hosting** - Dependency injection and hosting

## Development

### Build

```powershell
./build.ps1 Compile --configuration Release
# or: dotnet build src/QBrainAi.Repl.Host/QBrainAi.Repl.Host.csproj --configuration Release
```

### Pack

```powershell
./build.ps1 PackReplTool
# or: dotnet pack src/QBrainAi.Repl.Host/QBrainAi.Repl.Host.csproj --configuration Release --output ./local-packages
```

### Run Locally (Without Installing)

```powershell
dotnet run --project src/QBrainAi.Repl.Host -- --interactive
```

## License

MIT

## Author

SharpNinja

## Repository

https://github.com/SharpNinja/McpServer
