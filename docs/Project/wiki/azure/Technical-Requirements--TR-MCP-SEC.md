# TR-MCP-SEC

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-SEC-001

**Per-Workspace Auth Tokens** — `WorkspaceResolutionMiddleware` resolves workspace identity per-request using a three-tier chain: (1) `X-Workspace-Path` header, (2) API key reverse lookup via `WorkspaceTokenService`, (3) default workspace from config. `WorkspaceAuthMiddleware` then validates the token against the resolved workspace. `WorkspaceTokenService` generates per-workspace cryptographic tokens (32-byte base64url) on startup and maintains reverse-lookup maps for API key → workspace resolution.
**Covered by:** FR: FR-MCP-013, FR-MCP-043
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-002

**Pairing Session Security** — `PairingSessionService` verifies passwords using SHA-256 with `CryptographicOperations.FixedTimeEquals` for constant-time comparison. Session state is stored in HttpOnly cookies with the Secure flag enabled on HTTPS. `PairingOptions` binds `Mcp:ApiKey` and `Mcp:PairingUsers` from configuration.
**Covered by:** FR: FR-MCP-014
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-003

**Signed Marker Bootstrap and Health Nonce Verification** — `MarkerFileService` SHALL render a top-level marker signature block and a top-level `trust_bootstrap` block into `AGENTS-README-FIRST.yaml` using a deterministic canonical payload. The rendered marker SHALL instruct agents to verify the signature first, generate a nonce for `/health`, require the response to echo the nonce exactly, and stop using MCP endpoints when verification fails. `McpSession`, `McpTodo`, and `McpContext` SHALL share the same trust-verification contract so bootstrap parity is preserved across the public PowerShell modules.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/MarkerFileService.cs`, `templates/prompt-templates.yaml`, `src/McpServer.ServiceDefaults/Extensions.cs`, `tools/powershell/McpSession.psm1`, `tools/powershell/McpTodo.psm1`, `tools/powershell/McpContext.psm1`
Scope: layer-1+

## TR-MCP-SEC-004

**Provider-Native At-Rest Encryption with No-Loss Transition Procedures** — The storage layer SHALL support optional at-rest encryption using only provider-native or provider-extension facilities: SQLite SEE, PostgreSQL `pg_tde` on Percona Server for PostgreSQL, and native SQL Server TDE. The implementation SHALL detect desired-versus-actual encryption state at startup, SHALL refuse to silently continue when the configured state and live state differ, and SHALL require explicit no-data-loss enable/disable/rotation procedures that preserve existing data when configuration changes. SQL Server LocalDB may be used for provider and migration coverage, but SQL Server TDE validation requires a non-LocalDB SQL Server target.
**Status:** ✅ In Progress

**Covered by:** `src/McpServer.Storage/Database/McpDatabaseProviderFactory.cs`, `src/McpServer.Storage/McpDbContextFactory.cs`, `src/McpServer.Storage/Database/SqliteMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/PostgreSqlMcpDatabaseProviderStrategy.cs`, `src/McpServer.Storage/Database/SqlServerMcpDatabaseProviderStrategy.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs`, `src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs`, `scripts/Invoke-McpDatabaseEncryptionTransition.ps1`, `src/McpServer.Storage.SqliteMigrations`, `src/McpServer.Storage.PostgreSqlMigrations`, `src/McpServer.Storage.SqlServerMigrations`
Scope: layer-1+

## TR-MCP-SEC-005

**Self-describing marker signature payload** — Implements FR-MCP-140. MarkerFileService SHALL declare an ordered SignaturePayloadFields array naming every canonical field used to build the marker-v1 HMAC-SHA256 payload, and BuildSignaturePayload SHALL derive its output order from that same array so the emitted list and the computed payload cannot diverge. The MarkerSignature model SHALL carry a fields array and a format string describing the key=value newline per-field encoding, the trailing LF on the final line, and UTF-8 encoding, and both SHALL be written into the signature block of AGENTS-README-FIRST.yaml. The conditional agentPlugins.policy and agentPlugins.contractDigest tail SHALL be represented faithfully: the fields array SHALL reflect the same conditional the payload builder applies for the marker being written. A test SHALL bind the documented field order in docs/REPL-AGENT-GUIDE.md to SignaturePayloadFields so the prose spec cannot drift from the code. Recovered from origin/claude/dreamy-brahmagupta; the CLAUDE.md session-start rewrite in that commit is deliberately NOT re-landed because it directs agents to PowerShell-module or hand-rolled verification, contradicting the current plugin-first trust contract.
**Covered by:** FR: FR-MCP-140; TEST: TEST-MCP-189
**Status:** pending
Scope: layer-1+

## TR-MCP-SEC-006

**Brain-slot signing keys follow a renamed party id** — CORRECTED 2026-07-20 after the original specification was proven wrong by test. Context unchanged: trusted parties and their keys live in the TransactionSecurity key store (KeyServerPartyEntity, KeyServerPartyKeyEntity), a separate SQLite database created with EnsureCreated, so the McpDbContext Creativity/Logic rename migration cannot reach them; BrainSlotRegistryService.RegisterPartyAsync registers the party only and never a key, while ValidateReadinessAsync requires an active signing key at "{partyId}:signing:1". On any migrated installation the renamed party therefore starts with no key and the quad reports NotReady. WHAT WAS WRONG: the first version of this requirement said to COPY the legacy party's signing key material forward. That is impossible and harmful. PartyKeyDescriptor and KeyServerPartyKeyEntity expose only PublicKeyPem, so only the public half can ever be read; InMemoryKeyServerService retains a private ECDsa handle only for a pair it generated itself, and CreateSigningKey given a public PEM with no private PEM stores an empty private-key set. The result passed every readiness gate and could never sign: turn transactions were rejected with TransactionFailureReason.UnknownKey after the model provider had already been invoked and billed, and persisting that row also suppressed TurnTransactionCoordinator.EnsureDefaultPartiesAsync, which mints a working pair only when no key row exists. CORRECT BEHAVIOR: when a brain slot's party has no active signing key and a legacy party id exists (brain-slot:left-hemisphere for Creativity, brain-slot:right-hemisphere for Logic), the runtime SHALL register the renamed party with NO key material so the keyserver mints a fresh pair and retains its private half, and SHALL leave the legacy party and key rows untouched. Historical verification is unaffected because a manifest carries its own PublisherPartyId and VerifyManifestAsync resolves the public key from that legacy party. Reconciliation remains idempotent, is a no-op when the new key already exists or no legacy party is found, and never rotates an existing key. Proven by TEST-MCP-192 and by BrainSlotAdoptedPartyKeySigningTests, which now assert a committed turn transaction under the renamed party and continued verification of a legacy-signed manifest.
**Status:** pending
Scope: layer-1+

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
