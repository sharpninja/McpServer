# McpServer.Repl.IntegrationTests

Integration tests for iteration 1 of the REPL functionality.

## Overview

This test project validates the following iteration 1 requirements:

1. **Child Process Launch**: Launching `mcpserver-repl --agent-stdio` as a child process
2. **YAML Handshake**: Sending and receiving YAML `hello` envelopes
3. **Trust Bootstrap Flow**: 
   - Health check with nonce validation
   - Signature validation
   - Nonce challenge/response
4. **Auth Key Acceptance**: Validating API keys in `X-Api-Key` headers
5. **Workspace Selection**: Using `X-Workspace-Path` header to select workspaces
6. **YAML Envelope Parsing**: Verifying envelope shapes for all message types

## Test Classes

### Iteration1IntegrationTests
Core integration tests covering child process lifecycle and basic YAML communication.

### TrustBootstrapFlowTests
Tests for the complete trust bootstrap flow including health checks, nonce challenges, and signature validation.

### AuthKeyAndWorkspaceTests
Tests for API key validation and workspace selection via headers and YAML requests.

### YamlEnvelopeShapeTests
Unit-style tests validating YAML serialization/deserialization for all envelope types.

## Running the Tests

```powershell
# Run all integration tests
dotnet test tests/McpServer.Repl.IntegrationTests

# Run specific test class
dotnet test tests/McpServer.Repl.IntegrationTests --filter "FullyQualifiedName~TrustBootstrapFlowTests"

# Run with verbose output
dotnet test tests/McpServer.Repl.IntegrationTests --logger "console;verbosity=detailed"
```

## Prerequisites

- The `mcpserver-repl` host project must be buildable via `dotnet run`
- For HTTP tests, the MCP server should be running at `http://localhost:5177` (optional)
- Tests are designed to be resilient to server unavailability

## Test Helpers

### ReplChildProcessHelper
Manages the lifecycle of child `mcpserver-repl` processes, capturing stdout/stderr and providing async communication methods.

### YamlEnvelopeBuilder
Factory methods for constructing well-formed YAML envelope objects for all message types.

## Notes

- Tests that require a running server handle `HttpRequestException` gracefully
- Child process tests verify process state and YAML communication patterns
- Envelope shape tests validate serialization correctness without requiring a running server
