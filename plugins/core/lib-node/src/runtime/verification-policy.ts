// SPDX-License-Identifier: Apache-2.0
import { createHash, createHmac, timingSafeEqual } from 'crypto';
import { isAbsolute, resolve } from 'path';
import type { ReplBridge } from '../transport/repl-bridge.js';

/** TR-MCP-VERIFYPOLICY-001: authenticated command data; argv order is significant. */
export interface VerificationPolicyCheck {
  id: string;
  argv: string[];
  cwd: string;
  triggers: ('code' | 'doc')[];
  required: boolean;
  suiteKind: 'build' | 'typecheck' | 'unit' | 'integration' | 'e2e' | 'packaging' | 'other';
  scope: 'turn' | 'phaseGate';
  pathGlobs: string[];
}

/** Administrator-owned policy returned by the validating typed client. */
export interface WorkspaceVerificationPolicyDocument {
  workspacePath: string;
  policy: {
    schemaVersion: 1;
    continuationLimit: number;
    checks: VerificationPolicyCheck[];
    phaseGates: { todoId: string; taskPrefixes: string[]; onDone: boolean }[];
  };
}

type DeepReadonly<T> = T extends object ? { readonly [K in keyof T]: DeepReadonly<T[K]> } : T;

/** Verified data and digest to bind downstream verification evidence. */
export interface VerifiedWorkspacePolicy {
  readonly document: DeepReadonly<WorkspaceVerificationPolicyDocument>;
  readonly policyDigest: string;
}

/** Trusted, host-owned identity; never populate this from model tool arguments. */
export interface VerificationPolicyBinding {
  workspacePath: string;
  agentName: string;
  pluginId: string;
  apiKey: string;
}

type PolicyBridge = Pick<ReplBridge, 'invoke' | 'assertHostBinding' | 'hasWorkspaceContext'>;
const format = 'workspace-verification-policy-v1';
const invalid = () => new Error('Workspace verification policy is unavailable or invalid');

function record(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw invalid();
  return value as Record<string, unknown>;
}

function sameWorkspace(left: string, right: string): boolean {
  if (!isAbsolute(left) || !isAbsolute(right)) return false;
  const a = resolve(left), b = resolve(right);
  return process.platform === 'win32' ? a.toLowerCase() === b.toLowerCase() : a === b;
}

function freeze<T>(value: T): DeepReadonly<T> {
  if (value && typeof value === 'object') {
    for (const child of Object.values(value)) freeze(child);
    Object.freeze(value);
  }
  return value as DeepReadonly<T>;
}

/**
 * FR-MCP-VERIFYPOLICY-001: read via the pinned, workspace-bound REPL. Its typed
 * client creates/verifies the fresh challenge and validates the complete schema.
 * Recheck the retained attestation here, then use only its signed payload, never
 * the duplicate document field from the transport. No cache or permissive fallback.
 */
export async function readWorkspaceVerificationPolicy(
  bridge: PolicyBridge, binding: VerificationPolicyBinding,
): Promise<VerifiedWorkspacePolicy> {
  // Capture before yielding: a caller changing its object cannot retarget this read.
  const owner = { ...binding };
  try {
    if (!owner.apiKey || !bridge.hasWorkspaceContext) throw invalid();
    bridge.assertHostBinding(owner);
    const response = await bridge.invoke('client.Workspace.GetVerificationPolicyAsync', {});
    if (response.type !== 'result') throw invalid();
    const result = record(response.payload.result);
    const envelope = record(result.attestation);
    const { payloadBase64, policyDigest, challenge, signature } = envelope;
    if (envelope.format !== format || typeof payloadBase64 !== 'string' || payloadBase64.length > 8 * 1024 * 1024
      || typeof policyDigest !== 'string' || !/^[A-F0-9]{64}$/.test(policyDigest)
      || typeof challenge !== 'string' || !/^[A-Fa-f0-9]{64}$/.test(challenge)
      || typeof signature !== 'string' || !/^[A-Fa-f0-9]{64}$/.test(signature)
      || result.policyDigest !== policyDigest) throw invalid();
    const bytes = Buffer.from(payloadBase64, 'base64');
    if (bytes.toString('base64') !== payloadBase64) throw invalid();
    const digest = createHash('sha256').update(bytes).digest();
    const expectedSignature = createHmac('sha256', owner.apiKey)
      .update(`${format}\n${challenge}\n${policyDigest}\n`).digest();
    if (!timingSafeEqual(digest, Buffer.from(policyDigest, 'hex'))
      || !timingSafeEqual(expectedSignature, Buffer.from(signature, 'hex'))) throw invalid();
    const document = record(JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes)));
    if (typeof document.workspacePath !== 'string' || !sameWorkspace(document.workspacePath, owner.workspacePath)) throw invalid();
    const policy = record(document.policy);
    if (policy.schemaVersion !== 1 || !Number.isInteger(policy.continuationLimit)
      || (policy.continuationLimit as number) < 1 || (policy.continuationLimit as number) > 3
      || !Array.isArray(policy.checks) || !Array.isArray(policy.phaseGates)) throw invalid();
    return Object.freeze({ document: freeze(document as unknown as WorkspaceVerificationPolicyDocument), policyDigest });
  } catch {
    // Neither server diagnostics nor binding credentials belong in model-visible failures.
    throw invalid();
  }
}