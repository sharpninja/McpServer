# TEST-MCP-ACID

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-ACID

### TEST-MCP-ACID-001

Baseline full ACID turn-transaction lifecycle with key server and subscriber mocked in-process and the coordinator as system under test; happy commit, mutation-abort+rollback, subscriber-unavailable degraded+rollback, and all published-message rejections.


### TEST-MCP-ACID-002

Same lifecycle commits with key server and subscriber as real spun-up WebApplicationFactory hosts torn down after the test; coordinator drives sign and commit over the HTTP transports.


### TEST-MCP-ACID-003

SignManifest success plus rejections - unknown party, unknown key, replay nonce, stale sequence.


### TEST-MCP-ACID-004

VerifyManifest valid plus rejections relying parties act on - signature mismatch, wrong subscriber.


### TEST-MCP-ACID-005

Commit, idempotent re-commit, and rejections - signature mismatch, encrypted-body mismatch, plaintext mismatch, stale sequence, wrong subscriber, decrypt-required failure. Subscriber validates the key server verification result.


### TEST-MCP-ACID-006

Committed, bypassed (disabled/non-mutating), aborted, rejected on key-server sign failure fail-closed before mutation, rejected on subscriber commit rejection with rollback, degraded on subscriber unavailable. Coordinator validates key server and subscriber results.
