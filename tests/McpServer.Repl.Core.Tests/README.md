# McpServer.Repl.Core.Tests

Unit test project for iteration 1 of the REPL Core component.

## Test Coverage

### YamlFramingTests
Tests YAML framing, parsing, serialization, and envelope validation:
- Envelope type discrimination (hello, request, event, result, error)
- Payload parsing for each envelope type
- Serialization of envelopes to YAML
- Malformed YAML handling
- Document stream parsing (multiple envelopes)
- Envelope validation (missing fields, unknown types)

### ProtocolHandshakeTests
Tests protocol handshake sequences:
- ConnectAsync with capabilities and metadata
- Server hello response handling
- Connection state management
- Version negotiation
- Disconnect handling
- Error conditions (already connected, timeout, network failure)

### MarkerFileTrustTests
Tests marker-file trust checks and signature validation:
- ReadAsync for valid and invalid marker files
- TryReadAsync for non-throwing reads
- Trust verification (registry cached, signature verified, user confirmed)
- Trust establishment with user prompts
- Trust registry operations (record, get, revoke, list)
- Marker file watching for real-time updates

### AuthRotationTests
Tests auth rotation rules and 401 recovery:
- UpdateAuthStateAsync when marker file changes
- Auth state refresh on server restart
- Auth change callbacks
- Token validation against server
- Clear auth state
- Multiple rotation cycles
- 401 recovery flow

### WorkspaceSelectionTests
Tests workspace selection logic:
- DiscoverWorkspacesAsync with default and custom search paths
- SelectWorkspaceAsync with trust verification
- SwitchWorkspaceAsync (deselect + select)
- DeselectWorkspaceAsync
- GetActiveMarkerData
- ValidateWorkspacePathAsync
- Active workspace tracking
- Force reselect handling

### RequestResponseCorrelationTests
Tests request/response correlation in the REPL protocol:
- SendRequestAsync with parameters
- Typed result handling
- Error response mapping to ReplProtocolException
- Request correlation with different request IDs
- Event handler registration/unregistration
- Connection state validation before requests

### McpServerClientIntegrationTests
Tests McpServerClient integration for auth rotation:
- ApiKey rotation updates all sub-clients
- WorkspacePath changes propagate
- Port retargeting
- Logout clearing credentials
- Multiple simultaneous auth rotations
- Bearer token rotation

## Test Strategy

All tests use **NSubstitute** to mock dependencies:
- `IYamlSerializer` for YAML operations
- `IReplProtocol` for protocol interactions
- `IMarkerFileReader` for marker file operations
- `IAuthRotationHandler` for auth state management
- `IWorkspaceSelector` for workspace operations
- `ITrustBootstrapService` for trust prompts
- File system abstractions

## Red Phase

All tests are written to **fail initially** (red phase) because:
1. No implementations exist yet for the interfaces
2. Tests mock the expected behavior
3. Tests validate acceptance criteria before implementation

## Next Steps

After implementation:
1. Replace mocks with real implementations
2. Verify all tests pass (green phase)
3. Refactor implementations as needed
