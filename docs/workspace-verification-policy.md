# Workspace verification policy

FR-MCP-VERIFYPOLICY-001 / TR-MCP-VERIFYPOLICY-001 / TEST-MCP-VERIFYPOLICY-001 implement QB-AGENT-001 dependency item 12. This contract is implemented in the supporting worktree; deployment, pinned consumer integration and phase acceptance remain pending.

## Registration and selection

Administrators register policy using the existing admin-JWT protected PATCH /qbrainai/configuration API, which persists flattened configuration keys and reloads configuration. Workspace API keys do not grant that administrator role. The policy read feature adds no mutation endpoint.

Each QBrainAi:VerificationPolicies array element contains WorkspacePath and Policy. Legacy Mcp:VerificationPolicies is read only when the canonical section is absent. Reads require exactly one entry matching the resolved workspace. Missing, duplicate or invalid configuration returns HTTP 503; anonymous/default workspace credentials return 403; malformed challenge returns 400. No default policy is substituted.

Policy schema version 1 contains continuationLimit (1–3), checks and phaseGates. Each check has a unique id, literal ordered argv, relative cwd, code/doc triggers, explicit required boolean, suiteKind (build/typecheck/unit/integration/e2e/packaging/other), scope (turn/phaseGate) and optional relative pathGlobs. Each phase gate has a unique todoId, taskPrefixes and onDone. A gate must select a task prefix or the done mutation. Paths cannot be absolute or contain parent traversal. Runtime physical-path containment and command execution remain host responsibilities.

The configuration API accepts full colon-delimited property paths, including numeric array indices, such as QBrainAi:VerificationPolicies:0:Policy:Checks:0:Argv:0. Register the entire approved workspace command and phase-selector map before governed completion relies on it. An explicitly empty canonical section leaves reads unavailable even when legacy policy exists.

## Read and attestation

WorkspaceClient.GetVerificationPolicyAsync generates a fresh random 32-byte hexadecimal challenge and calls GET /qbrainai/workspace/verification-policy?challenge=... using explicit workspace routing and its marker key. The server returns format, payloadBase64, policyDigest, challenge and signature. The format is workspace-verification-policy-v1. Payload is canonical UTF-8 camelCase JSON of workspacePath and policy. Checks and selector sets have deterministic ordering; argv order and empty arguments are preserved. Digest is uppercase SHA256 of the exact decoded payload bytes.

Signature is HMAC-SHA256 with the current marker key over format + LF + challenge + LF + policyDigest + LF. The typed client validates challenge, signature, digest, schema, canonical bytes and workspace before returning a snapshot. A policy digest remains stable across fresh challenges; key rotation invalidates an old attestation under the new key. Generated JSON metadata and typed REPL task dispatch include all new DTO roots.

Supported REPL read: client.Workspace.GetVerificationPolicyAsync with empty arguments. Canonical Node core exports readWorkspaceVerificationPolicy(bridge, binding). Its caller supplies host-owned workspacePath, agentName, pluginId and apiKey from trusted bootstrap. The reader requires a governed, context-bound bridge, invokes the typed read, rechecks retained HMAC/digest/workspace, and exposes a deeply frozen document decoded from the signed bytes. It ignores the duplicate transport document field. Errors produce a generic policy-unavailable diagnostic without remote error strings or credentials. Reads do not reuse cached success after a transport failure.

Freshness is verified by the typed client inside the trusted pinned REPL, not independently by the Node reader. Schema validation also resides in that typed client/server contract. The Node reader must therefore only receive the trusted host bridge. A party with the full on-disk marker key can generate an HMAC; this does not establish independent operator authorization against such a party.

## Verification and remaining integration

Focused codec tests: 23 passed. Server and actual typed REPL passthrough tests: 14 passed, including corrupted HTTP-response rejection and fresh challenge generation. Full canonical Node core: 230 passed, including 15 policy tests. All have zero skips. Client/server builds and TypeScript build passed.

These are focused boundary and regression results, not full HTTP middleware, deployed service, native trimmed-bundle or cross-platform acceptance. A cross-language golden fixture and fresh artifact tests remain to be added. No product host currently obtains its mandatory completion obligations from this reader. Registering the approved map, syncing/pinning the plugin and REPL, host integration, cumulative phase tests and closing HV remain required.