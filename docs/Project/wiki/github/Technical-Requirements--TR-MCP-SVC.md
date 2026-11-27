# TR-MCP-SVC

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-SVC-001

**Windows Service Configuration** — `UseWindowsService(options => { options.ServiceName = "McpServer"; })` in `Program.cs` enables Windows Service hosting. The service is published as a self-contained single-file executable to `C:\ProgramData\McpServer`. The `Manage-McpService.ps1` script handles Install, Uninstall, Start, Stop, Restart, Status, and Publish operations with gsudo elevation. Recovery policy restarts the service on failure with a 60 s delay.
**Covered by:** FR: FR-MCP-017
**Status:** pending
Scope: layer-1+

## TR-MCP-SVC-002

**Executable resolution skips WindowsApps App-Execution-Alias stubs** — Implements FR-MCP-141. IProcessEnvironmentService.ResolveExecutable in src/McpServer.Common.AgentCli/ProcessEnvironmentService.cs SHALL skip any PATH directory whose path contains Microsoft\WindowsApps when probing for an executable, because those entries are zero-byte App-Execution-Alias reparse points that a service account cannot launch, failing with Win32Exception 1920. The guard SHALL be an exact containment test on Microsoft\WindowsApps and SHALL NOT match the genuine C:\Program Files\WindowsApps MSIX install root, which holds real executables such as the packaged PowerShell. This affects every ResolveExecutable caller including the Agent CLI client, the Codex, Grok, and OneShot execution strategies, and ProcessRunner, not only tunnel providers. Recovered from origin/claude/busy-dubinsky, where the file lived at src/McpServer.Common.Copilot/ProcessEnvironmentService.cs before that project was renamed to McpServer.Common.AgentCli.
**Covered by:** FR: FR-MCP-141; TEST: TEST-MCP-190, TEST-MCP-191
**Status:** pending
Scope: layer-1+

