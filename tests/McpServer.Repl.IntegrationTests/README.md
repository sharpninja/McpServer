# McpServer.Repl.IntegrationTests

Integration tests for iteration 1 and iteration 2 of the REPL functionality.

## Overview

This test project validates the following requirements:

### Iteration 1 Requirements

1. **Child Process Launch**: Launching `mcpserver-repl --agent-stdio` as a child process
2. **YAML Handshake**: Sending and receiving YAML `hello` envelopes
3. **Trust Bootstrap Flow**: 
   - Health check with nonce validation
   - Signature validation
   - Nonce challenge/response
4. **Auth Key Acceptance**: Validating API keys in `X-Api-Key` headers
5. **Workspace Selection**: Using `X-Workspace-Path` header to select workspaces
6. **YAML Envelope Parsing**: Verifying envelope shapes for all message types

### Iteration 2 Requirements

1. **Session Log Workflow**: Full lifecycle via child-process YAML communication
   - Bootstrap subsystem
   - Open session
   - Begin turn
   - Append dialog items
   - Append actions
   - Update turn metadata
   - Complete turn
   - Fail turn
   - Query history
2. **State Persistence**: Verify session/turn state persists across commands
3. **Reconnect Scenarios**: Test session continuity across reconnects
4. **Error Handling**: Validate 401 auth recovery and error responses
5. **Identifier Validation**: Test canonical sessionId and requestId format enforcement
6. **Turn Immutability**: Verify completed/failed turns reject further updates

## Test Classes

### Iteration1IntegrationTests
Core integration tests covering child process lifecycle and basic YAML communication.

### TrustBootstrapFlowTests
Tests for the complete trust bootstrap flow including health checks, nonce challenges, and signature validation.

### AuthKeyAndWorkspaceTests
Tests for API key validation and workspace selection via headers and YAML requests.

### YamlEnvelopeShapeTests
Unit-style tests validating YAML serialization/deserialization for all envelope types.

### Iteration2IntegrationTests
Comprehensive integration tests for the Session Log workflow via child-process YAML communication:
- Full workflow from bootstrap through completion
- State persistence verification
- Dialog and action append operations
- Turn lifecycle state transitions
- Error handling for invalid identifiers
- Reconnect scenario validation
- Support for all dialog categories and action types

## Running the Tests

```powershell
# Run all integration tests
dotnet test tests/McpServer.Repl.IntegrationTests

# Run specific test class
dotnet test tests/McpServer.Repl.IntegrationTests --filter "FullyQualifiedName~TrustBootstrapFlowTests"

# Run iteration 2 session log tests
dotnet test tests/McpServer.Repl.IntegrationTests --filter "FullyQualifiedName~Iteration2IntegrationTests"

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
