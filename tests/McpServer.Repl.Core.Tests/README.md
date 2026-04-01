# McpServer.Repl.Core.Tests

Unit test project for iteration 1 of the REPL Core component.

## Test Coverage - Iteration 1 (Mock-Based)

### Iteration1_IntegrationTests
End-to-end orchestration tests using mocks:
- Trust bootstrap orchestration (new workspace, cached trust)
- Auth rotation orchestration (marker file changes, 401 response)
- Marker file watch orchestration
- YAML serialization with trust bootstrap payloads
- Mock nonce validation
- Full trust bootstrap flow with all components

### FakeYamlSerializerTests
Tests for fake `IYamlSerializer` implementation using YamlDotNet:
- Serialize/deserialize hello, request, error envelopes
- Round-trip serialization
- Malformed YAML handling
- Missing type/payload validation
- TryDeserialize for non-throwing operations
- Stream serialization/deserialization (multiple documents)
- Null/empty input validation

### StubMarkerFileReaderTests
Tests for stubbed `IMarkerFileReader` with pre-canned trust-bootstrap payloads:
- Pre-canned payloads for trusted/untrusted/signature-verified workspaces
- ReadAsync with various workspace scenarios
- TryReadAsync for non-throwing reads
- VerifyTrustAsync with different trust methods (cached, user_confirmed, signature_verified)
- WatchAsync simulation with callback invocation
- Multiple scenario coverage

### MockTrustBootstrapServiceTests
Tests for mocked `ITrustBootstrapService` with nonce validation:
- PromptUserTrust with valid/invalid/missing nonce
- RecordTrustDecision for trusted/denied workspaces
- GetTrustDecision for trusted/denied/new workspaces
- RevokeTrust and registry removal
- ListTrustedWorkspaces with multiple entries
- ClearAllTrust operation
- Nonce validation with multiple attempts
- Trust workflow state maintenance
- Trusted workspace metadata

### StubAuthRotationHandlerTests
Tests for stubbed `IAuthRotationHandler` with state transitions:
- UpdateAuthState with marker data transitions
- RegisterAuthChangeCallback with single/multiple callbacks
- UnregisterAuthChangeCallback
- RefreshAuthState with new key generation
- ValidateAuthState for valid/expired tokens
- ClearAuthState invalidation
- Full auth state lifecycle transitions
- Server restart simulation

### OrchestrationRulesTests
Tests validating orchestration rules without real MCP client calls:
- Rule: Trust before auth (must verify trust before establishing auth)
- Rule: Nonce validation (must validate before trust confirmation)
- Rule: Cached trust bypass (skips user prompt for known workspace)
- Rule: 401 recovery (must refresh auth state)
- Rule: Marker file watch triggers auth rotation
- Rule: State consistency (auth state reflects latest marker data)
- Rule: Trust revocation clears auth state
- Rule: Signature verification bypasses user prompt
- Rule: Auth callbacks invoked on rotation
- Full orchestration from new workspace to trusted with auth rotation

### ContractCorrectnessTests
Tests validating interface contract correctness:
- All interfaces have required methods
- All data interfaces have required properties
- All interfaces are reference types
- All interfaces can be substituted with NSubstitute
- Async methods return Task<T>
- CancellationToken parameters are optional
- Nullable properties are correctly marked
- Required properties are non-nullable

### YamlFramingTests (Existing)
Tests YAML framing, parsing, serialization, and envelope validation:
- Envelope type discrimination (hello, request, event, result, error)
- Payload parsing for each envelope type
- Serialization of envelopes to YAML
- Malformed YAML handling
- Document stream parsing (multiple envelopes)
- Envelope validation (missing fields, unknown types)

### ProtocolHandshakeTests (Existing)
Tests protocol handshake sequences:
- ConnectAsync with capabilities and metadata
- Server hello response handling
- Connection state management
- Version negotiation
- Disconnect handling
- Error conditions (already connected, timeout, network failure)

### MarkerFileTrustTests (Existing)
Tests marker-file trust checks and signature validation:
- ReadAsync for valid and invalid marker files
- TryReadAsync for non-throwing reads
- Trust verification (registry cached, signature verified, user confirmed)
- Trust establishment with user prompts
- Trust registry operations (record, get, revoke, list)
- Marker file watching for real-time updates

### AuthRotationTests (Existing)
Tests auth rotation rules and 401 recovery:
- UpdateAuthStateAsync when marker file changes
- Auth state refresh on server restart
- Auth change callbacks
- Token validation against server
- Clear auth state
- Multiple rotation cycles
- 401 recovery flow

### WorkspaceSelectionTests (Existing)
Tests workspace selection logic:
- DiscoverWorkspacesAsync with default and custom search paths
- SelectWorkspaceAsync with trust verification
- SwitchWorkspaceAsync (deselect + select)
- DeselectWorkspaceAsync
- GetActiveMarkerData
- ValidateWorkspacePathAsync
- Active workspace tracking
- Force reselect handling

### RequestResponseCorrelationTests (Existing)
Tests request/response correlation in the REPL protocol:
- SendRequestAsync with parameters
- Typed result handling
- Error response mapping to ReplProtocolException
- Request correlation with different request IDs
- Event handler registration/unregistration
- Connection state validation before requests

### McpServerClientIntegrationTests (Existing)
Tests McpServerClient integration for auth rotation:
- ApiKey rotation updates all sub-clients
- WorkspacePath changes propagate
- Port retargeting
- Logout clearing credentials
- Multiple simultaneous auth rotations
- Bearer token rotation

## Test Strategy

### Iteration 1 - Mock-Based (Green Phase with Mocks)

All iteration 1 tests use **NSubstitute** mocks and **fakes/stubs**:
- `FakeYamlSerializer` - Uses real YamlDotNet for serialization
- `StubMarkerFileReader` - Pre-canned trust-bootstrap payloads
- Mock `ITrustBootstrapService` - Nonce validation logic
- `StubAuthRotationHandler` - State transition logic

These tests validate:
1. **Orchestration rules** - Component interactions work correctly
2. **Contract correctness** - Interfaces have required members
3. **Mock behavior** - Fakes/stubs behave as expected
4. **No real MCP client calls** - All tests pass without actual MCP server

### Existing Tests - Awaiting Implementation

All existing tests will transition from **RED** to **GREEN** after implementations:
1. No implementations exist yet for the interfaces
2. Tests mock the expected behavior
3. Tests validate acceptance criteria before implementation

## Next Steps

After iteration 1 (mock-based tests pass):
1. Implement concrete classes for all REPL Core interfaces
2. Replace mocks with real implementations in existing tests
3. Verify all tests pass (green phase with real implementations)
4. Refactor as needed while maintaining test coverage
