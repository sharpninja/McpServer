# MCP Server Quad-Model Transactional Diffgram Plan

Source: https://drive.google.com/file/d/1jX9E298FRvo6gjEDDNFZHNrgpfNWHyFO/view?usp=drivesdk

Version: 1.0

Status: Imported implementation contract

## Implementation Ordering

The imported document is implemented in this repository with this fixed order:

1. Build the keyserver first.
2. Build the subscriber second.
3. Integrate MCP Server turn transactions third.
4. Keep quad-model orchestration, Curiosity execution, AoT reconciliation execution, and weight update execution disabled until later requirements authorize those slices.

## Imported Executive Summary

This document describes a quad-model AI system hosted inside the MCP Server, combined with a strong transactional model, three-party cryptographic trust, and comprehensive security controls.

The system features:

- Four specialized models: Left Hemisphere, Right Hemisphere, Curiosity Engine, Arbiter of Truth.
- Transaction-per-turn model with diffgrams and ACID guarantees.
- Strong user directive supremacy.
- Self-improving capabilities with rigorous safety gates.
- Pragmatic security hardening: cryptographic protocol, graceful degradation, and weight update controls.

## Imported Architecture Overview

The imported model roles are:

- Left Hemisphere: analytical, sequential, structured logic, planning, validation, and rules.
- Right Hemisphere: holistic, associative, generative fluency, context synthesis, and voice.
- Curiosity Engine: research, monitoring, self-improvement, gap detection, code experiments, external escalation, and frustration intervention.
- Arbiter of Truth: oversight, routing, final gatekeeping, quality/safety arbitration, reconciliation, and directive enforcement.

All four models interact through the Shared Corpus Callosum Cache.

## Imported Transactional Model

- Every user turn is a transaction.
- State changes are expressed as diffgrams.
- Activity is published to an external ACID-compliant pub-sub.
- The subscriber must commit before any response is returned.
- The AoT must approve the final response diffgram.
- Cancellation by subscriber triggers rollback except audit logging.
- Three-party cryptographic chain of custody is required for every diffgram.

## Imported Diagrams

The Mermaid source in this section is preserved from the imported document. Repo-specific branch IDs and implementation notes follow each imported block.

### AD-TXN-001 Normal Turn Transaction

Imported section: 3.1 Normal Turn Transaction.

```mermaid
flowchart TD
    A[User Request / Turn Starts] --> B{Active Subscriber Present?}
    B -->|No| C[Enter Degraded Mode]
    B -->|Yes| D[Start Transaction + Transaction Manifest]
    D --> E[3PKS Signs Manifest]
    E --> F[Left + Right + Curiosity Process]
    F --> G[AoT Arbitrates]
    G --> H{AoT Approves Final Diffgram?}
    H -->|No| I[Reconciliation Process]
    I --> J{All 3 Models Agree?}
    J -->|Yes| K[Combined Argument to AoT]
    J -->|No| L[Abort + Rollback]
    K --> H
    H -->|Yes| M[Final Diffgram Published]
    M --> N[Subscriber Verifies + Commits]
    N --> O[Transaction Committed]
    O --> P[Response Returned]
```

Repo annotations:

- `AD-TXN-001-BR-NO-SUBSCRIBER`: `B -> C`; in scope for degraded-mode tests.
- `AD-TXN-001-BR-START`: `B -> D -> E`; in scope for keyserver-first tests.
- `AD-TXN-001-BR-AOT-APPROVED`: `H -> M -> N -> O -> P`; in scope for MCP transaction gating tests after keyserver and subscriber are green.
- `AD-TXN-001-BR-AOT-REJECTED`: `H -> I -> J`; documented now, executable quad reconciliation deferred.
- `AD-TXN-001-BR-ROLLBACK`: `J -> L`; rollback audit behavior in scope, model disagreement execution deferred.

### AD-CURIOSITY-001 Curiosity Engine Flow

Imported section: 3.2 Curiosity Engine Flow.

```mermaid
flowchart TD
    A[Monitor Cache + Logs + Traces] --> B{Frustration Detected?}
    B -->|Yes| C[Targeted Research]
    B -->|No| D{Struggle or Novel Concept?}
    D -->|Yes| E[Value Ranking]
    E --> F{High Value?}
    F -->|Yes| G[Research + Code Experiments]
    G --> H{External Help Needed?}
    H -->|Yes| I[Call External + Inject Context]
    H -->|No| J[Local Research]
    I --> K[Judgment + AoT Review]
    J --> K
    K --> L{Approved?}
    L -->|Yes| M[Inject into Cache + GraphRAG]
    L -->|No| N[Discard]
    M --> O{Weight Update?}
    O -->|Yes| P[Dual-Control Weight Update]
```

Repo annotations:

- `AD-CURIOSITY-001-BR-MONITOR`: documented for future cache/log/trace monitoring.
- `AD-CURIOSITY-001-BR-FRUSTRATION`: deferred; must remain disabled in this slice.
- `AD-CURIOSITY-001-BR-EXTERNAL`: deferred; requires separate external-escalation requirements.
- `AD-CURIOSITY-001-BR-INJECT`: deferred; GraphRAG injection is not enabled by this slice.
- `AD-CURIOSITY-001-BR-WEIGHT`: deferred and disabled; covered by future weight-update requirements.

### SD-DIFFGRAM-001 Three-Party Diffgram Exchange

Imported section: 3.3 Three-Party Diffgram Exchange.

```mermaid
sequenceDiagram
    participant P as Publisher (MCP Server)
    participant T as 3PKS
    participant S as Subscriber

    P->>T: Public Keys + Diffgram SHA
    T->>T: Sign Transaction Manifest
    T-->>S: Encrypted SHA + Encrypted Diffgram

    S->>S: Decrypt SHA (3PKS Pub + Own Priv)
    S->>S: Verify SHA
    alt Invalid
        S->>S: Abort Transaction
    else Valid
        S->>S: Decrypt Diffgram (Publisher Pub + Own Priv)
        S->>S: Commit
    end
```

Repo annotations:

- `SD-DIFFGRAM-001-MSG-PUBLISHER-KEYS`: MCP Server supplies party IDs, public key IDs, and diffgram hashes to keyserver.
- `SD-DIFFGRAM-001-MSG-SIGN`: keyserver signs canonical transaction manifests.
- `SD-DIFFGRAM-001-MSG-VERIFY-HASH`: subscriber verifies encrypted and plaintext hashes.
- `SD-DIFFGRAM-001-BR-INVALID`: invalid hash/signature/decrypt path aborts and audits.
- `SD-DIFFGRAM-001-BR-VALID`: valid manifest and payload commits durably.

### AD-AOT-001 AoT Reconciliation Process

Imported section: 3.4 AoT Reconciliation Process.

```mermaid
flowchart TD
    A[AoT Rejects] --> B[Open Reconciliation]
    B --> C[Model Refines + Additional Reasoning]
    C --> D[Other Two Models Weigh In]
    D --> E{All Three Agree?}
    E -->|Yes| F[Combined Argument to AoT]
    E -->|No| G[Rejection Stands]
    F --> H{AoT Accepts?}
    H -->|Yes| I[Approve Revised Result]
    H -->|No| G
```

Repo annotations:

- `AD-AOT-001-BR-REJECT`: documented and disabled until quad execution exists.
- `AD-AOT-001-BR-AGREE`: future reconciliation branch.
- `AD-AOT-001-BR-DISAGREE`: future rejection branch.
- `AD-AOT-001-BR-ACCEPT`: future revised-result branch.

### AD-WEIGHT-001 Weight Redistribution Safety

Imported section: 3.5 Weight Redistribution Safety.

```mermaid
flowchart TD
    A[Curiosity Proposes Update] --> B[AoT Review]
    B --> C{AoT Approves?}
    C -->|Yes| D{Human/Admin Approval?}
    D -->|Yes| E[Create Snapshot]
    E --> F[Run Safety Gates]
    F --> G{Gates Passed?}
    G -->|Yes| H[Apply Update + Quarantine]
    G -->|No| I[Reject]
    D -->|No| I
    C -->|No| I
```

Repo annotations:

- `AD-WEIGHT-001-BR-PROPOSE`: deferred and disabled.
- `AD-WEIGHT-001-BR-AOT`: deferred and disabled.
- `AD-WEIGHT-001-BR-HUMAN`: deferred and disabled.
- `AD-WEIGHT-001-BR-SNAPSHOT`: future implementation must define snapshot/rollback contracts.
- `AD-WEIGHT-001-BR-GATES`: future implementation must define safety gates before any weight update code.
- `AD-WEIGHT-001-BR-QUARANTINE`: future implementation must define quarantine semantics.

### ARCH-QUAD-001 High-Level System Architecture

Imported section: 3.6 High-Level System Architecture.

```mermaid
flowchart TB
    subgraph MCP Server
        LM[Left Hemisphere]
        RM[Right Hemisphere]
        CM[Curiosity Engine]
        AM[Arbiter of Truth]
        Cache[Shared Cache]
    end

    subgraph External
        PubSub[ACID Pub-Sub]
        KeyServer[3PKS]
        Sub[Subscriber]
    end

    User --> AM
    AM --> LM & RM & CM
    LM & RM & CM --> Cache
    Cache --> PubSub
    KeyServer --> PubSub
    Sub --> PubSub
```

Repo annotations:

- `ARCH-QUAD-001-COMP-MCP`: existing `src/McpServer.Support.Mcp`, including compatibility keyserver/subscriber controllers and turn transaction coordinator wiring over the shared transaction-security core.
- `ARCH-QUAD-001-COMP-CLIENT`: existing `src/McpServer.Client`, including public transaction DTO/client contracts.
- `ARCH-QUAD-001-COMP-KEYSERVER`: separate `src/McpServer.KeyServer` host exposes keyserver trust endpoints over the shared transaction-security core.
- `ARCH-QUAD-001-COMP-SUBSCRIBER`: separate `src/McpServer.Subscriber` host exposes subscriber commit/status/abort endpoints and verifies manifests through an HTTP-backed keyserver client.
- `ARCH-QUAD-001-COMP-PUBSUB`: represented in this slice by subscriber commit/coordinator contracts; durable DB commit storage and full external pub-sub remain deferred.
- `ARCH-QUAD-001-COMP-QUAD`: documented scaffolding only; execution disabled.

## Imported Security Remediation

The implementation follows the imported hardened option:

- Hybrid authenticated encryption: ECDH plus AEAD. Protected subscriber diffgram envelopes are implemented with ECDH P-256, HKDF-SHA256, and AES-256-GCM, and the coordinator can hand off protected envelopes for configured subscriber keys; external key material management, key rotation, and global mutation-adapter encrypted handoff remain deferred.
- Nonces, sequence numbers, timestamps, and expiry on diffgrams/manifests. Keyserver signing/verification and subscriber commit now enforce replay nonce and monotonic sequence scopes; broader recovery, adapter, and operational stress coverage remains deferred.
- 3PKS signed transaction manifest at start.
- Strict SHA verification and signature checks on every diffgram. Current coverage validates manifest signatures, encrypted-body SHA-256 before decrypt, and plaintext SHA-256 after decrypt for protected subscriber envelopes.
- Immediate abort on chain-of-custody failure.
- Graceful degradation instead of hard rejection when subscriber dependency is unavailable.
- Dual-control, versioning, rollback, safety gates, provenance, and LoRA preference for future weight updates.

## Repo Implementation Map

- `mcpserver`: existing MCP Server host plus `Mcp:TurnTransactions`, compatibility keyserver/subscriber controllers under `src/McpServer.Support.Mcp`, and the shared transaction coordinator from `src/McpServer.TransactionSecurity`.
- Separate keyserver host: `src/McpServer.KeyServer`.
- Separate subscriber host: `src/McpServer.Subscriber`.
- Shared transaction-security core: `src/McpServer.TransactionSecurity`.
- Shared client contracts: existing `src/McpServer.Client`.
- Focused first-slice tests: MCP support/client test projects plus `tests/McpServer.TransactionSecurity.IntegrationTests`.
- Deferred adapters: external key material management, key rotation, full external pub-sub, global mutation-adapter encrypted handoff, and global mutation adapters for all write surfaces.
