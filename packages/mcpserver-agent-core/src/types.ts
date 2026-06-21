/**
 * types.ts
 * v4 Marker Trust contracts (TS port of IV4* from tests/AgentPluginCore/Contracts/AgentPluginCoreV4Contracts.cs)
 * + exact behavioral spec from mcpserver-codex-plugin/lib/marker-resolver.sh
 * Only marker slice for this minimal Phase 2 increment.
 */

export interface IV4MarkerData {
  WorkspacePath: string;
  ServerUrl: string;
  ApiKey: string;
  Signature: string | null;
  Nonce: string | null;
  Metadata: Readonly<Record<string, string>>;
}

export interface IV4TrustResult {
  IsTrusted: boolean;
  TrustMethod: string; // "signature_verified", "nonce_challenge", "MCP_UNTRUSTED", ...
  DenialReason?: string;
  MarkerData?: IV4MarkerData;
}

export interface IV4MarkerTrustService {
  FindMarkerFileAsync(startPath: string, ct?: AbortSignal): Promise<string | null>;
  VerifySignatureAndParseAsync(markerPath: string, ct?: AbortSignal): Promise<IV4MarkerData>;
  PerformNonceHealthChallengeAsync(marker: IV4MarkerData, options?: { fetcher?: typeof fetch }): Promise<boolean>;
  BootstrapTrustAsync(workspacePath: string, options?: { fetcher?: typeof fetch }): Promise<IV4TrustResult>;
}

// Options for DI / test mocks (fs + fetch)
export interface MarkerTrustOptions {
  fs?: {
    readFile(path: string, encoding: 'utf8'): Promise<string>;
    stat(path: string): Promise<{ isFile(): boolean }>;
  };
  fetcher?: typeof fetch;
}

// =============================================================================
// v4 Enforcement State Machine (TR-MCP-AGENT-PARITY-011)
// Ported from AgentPluginCoreV4Contracts.cs + proven codex shims
// =============================================================================

export type V4EnforcementState =
  | "NoTurn"
  | "TurnOpen"
  | "EditsInProgress"
  | "TurnComplete"
  | "BlockedOnBuild"
  | "BlockedOnMissingComplete";

export interface V4EnforcementTransitionResult {
  success: boolean;
  newState: V4EnforcementState;
  errorCode?: string;
  message?: string;
}

export interface V4StopGateDecision {
  canEmitFinalResponse: boolean;
  blockReason?: string;
  state: V4EnforcementState;
}

export interface IV4EnforcementStateMachine {
  readonly CurrentState: V4EnforcementState;
  readonly CurrentTurnId: string | null;
  readonly TurnOpenedAt: string | null;

  beginTurnAsync(requestId: string): Promise<V4EnforcementTransitionResult>;
  recordCodeEditAndVerifyBuildAsync(filePath: string, buildStatus: string): Promise<V4EnforcementTransitionResult>;
  completeTurnAsync(requestId: string, forceSelfHeal?: boolean): Promise<V4EnforcementTransitionResult>;
  evaluateStopGate(): V4StopGateDecision;
}

// =============================================================================
// v4 Cache / Failsafe (TR-MCP-AGENT-PARITY-013)
// =============================================================================

export interface IV4CacheManager {
  GetScopedCachePath(workspaceKey: string, agentId: string): string;
  WritePendingAsync(workspaceKey: string, agentId: string, entryId: string, payload: object): Promise<void>;
  FlushPendingAsync(workspaceKey: string, agentId: string, maxRetries?: number): Promise<V4CacheFlushResult>;
  RecoverAndReplayAsync(workspaceKey: string, agentId: string, replBridge: IV4ReplBridge): Promise<V4CacheRecoveryResult>;
}

export interface V4CacheFlushResult {
  Success: boolean;
  RetriesUsed: number;
  MovedToFailed: number;
  Error?: string;
}

export interface V4CacheRecoveryResult {
  Success: boolean;
  EntriesReplayed: number;
  ProducedArtifacts: string[];
  Error?: string;
}

// ReplBridge (support for cache recovery)
export interface IV4ReplBridge {
  SendEnvelopeAsync(envelope: V4ReplEnvelope, timeout?: number): Promise<V4ReplResponse>;
}

export interface V4ReplEnvelope {
  Type: string;
  RequestId: string;
  Payload: object;
  AgentId: string;
}

export interface V4ReplResponse {
  Success: boolean;
  Result?: object;
  ErrorCode?: string;
  ErrorMessage?: string;
}
