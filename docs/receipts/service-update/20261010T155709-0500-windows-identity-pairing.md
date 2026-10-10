# Windows service identity pairing gate

Timestamp: 2026-10-10T15:57:09-05:00
Requirements: TR-MCP-QBRAIN-006, FR-MCP-SERVICEUPDATE-001, TR-MCP-SERVICEUPDATE-001, TEST-MCP-SERVICEUPDATE-001
Base commit: 407a80272b6c90861ea5994ca6c648d9204e14cc

## Observed failure

The default resolver independently selected the registered legacy service name and the existing canonical directory. This produced the split identity McpServer plus C:/ProgramData/QBrainAi. The intact live configuration remained at C:/ProgramData/McpServer, while the deployed service used a baseline configuration and an empty Global memory inventory.

## BDPv4 evidence

- Mocks-first paired-identity contract: 1 passed, 0 failed, 0 skipped.
- Production-bound affected gate: 28 passed, 0 failed, 0 skipped.
- git diff --check: clean.

## Implemented behavior

- Registered canonical service selects the canonical directory.
- Registered legacy service selects the legacy directory, even when stale canonical files exist.
- Explicit service or install-path overrides remain unchanged.
- With no registered service, the existing directory fallback remains available.

## Source hashes before commit

- build/ServiceUpdatePlatform.cs: B471889EFB14DB79765F9937B9A75521AA376719D36729B8DA7FA13EF65B7E46
- tests/Build.Tests/ServiceUpdateTests.cs: FF0482482314F04898CA736ECE028B105831C622C25BF7B23E985CB15BE635CB

Runtime redeployment and Global-memory readback remain pending.
