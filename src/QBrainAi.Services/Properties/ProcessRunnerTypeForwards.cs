using System.Runtime.CompilerServices;
using QBrainAi.Support.Mcp.Services;

// TEST-MCP-TRIM-001: preserve existing assembly-qualified names after moving shared process execution.
[assembly: TypeForwardedTo(typeof(IProcessRunner))]
[assembly: TypeForwardedTo(typeof(ProcessRunner))]
[assembly: TypeForwardedTo(typeof(ProcessRunnerOptions))]
[assembly: TypeForwardedTo(typeof(ProcessRunRequest))]
[assembly: TypeForwardedTo(typeof(ProcessRunResult))]
