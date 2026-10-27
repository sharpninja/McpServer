// SPDX-License-Identifier: Apache-2.0
import { createHash, createHmac } from 'crypto';
import { resolve } from 'path';
import { readWorkspaceVerificationPolicy } from '../src/runtime/verification-policy.js';

const owner = { workspacePath: resolve('/tests/policy'), agentName: 'ClineQb', pluginId: 'test-plugin', apiKey: 'fixture-key-not-a-real-credential' };
function snapshot() {
  const document = { workspacePath: owner.workspacePath, policy: { schemaVersion: 1, continuationLimit: 3,
    checks: [{ id: 'build', argv: ['bun', 'run', 'build:sdk', 'literal; $(value)'], cwd: '.', triggers: ['code'], required: true, suiteKind: 'build', scope: 'turn', pathGlobs: [] }],
    phaseGates: [{ todoId: 'QB-AGENT-001', taskPrefixes: ['P1:'], onDone: true }] } };
  const bytes = Buffer.from(JSON.stringify(document));
  const policyDigest = createHash('sha256').update(bytes).digest('hex').toUpperCase();
  const challenge = 'A'.repeat(64), format = 'workspace-verification-policy-v1';
  return { document, policyDigest, attestation: { format, challenge, policyDigest,
    payloadBase64: bytes.toString('base64'),
    signature: createHmac('sha256', owner.apiKey).update(`${format}\n${challenge}\n${policyDigest}\n`).digest('hex').toUpperCase() } };
}
function bridge(result = snapshot()) {
  return { hasWorkspaceContext: true, assertHostBinding: jest.fn(),
    invoke: jest.fn().mockResolvedValue({ type: 'result', payload: { result } }) };
}

describe('TEST-MCP-VERIFYPOLICY-001: signed policy plugin read', () => {
  it('uses typed REPL and signed payload, preserving argv and freezing nested data', async () => {
    const source = snapshot();
    source.document.policy.checks[0].argv = ['unsigned-transport-override'];
    const transport = bridge(source);
    const result = await readWorkspaceVerificationPolicy(transport, owner);
    expect(transport.assertHostBinding).toHaveBeenCalledWith(owner);
    expect(transport.invoke).toHaveBeenCalledWith('client.Workspace.GetVerificationPolicyAsync', {});
    expect(result.document.policy.checks[0].argv).toEqual(['bun', 'run', 'build:sdk', 'literal; $(value)']);
    expect(result.document.policy.phaseGates[0].todoId).toBe('QB-AGENT-001');
    expect(Object.isFrozen(result.document.policy.checks[0].argv)).toBe(true);
    expect(JSON.stringify(result)).not.toContain(owner.apiKey);
  });

  it.each(['signature', 'payload', 'digest', 'challenge', 'format', 'missing'])('rejects malformed or changed %s', async failure => {
    const result = snapshot();
    if (failure === 'signature') result.attestation.signature = '0'.repeat(64);
    if (failure === 'payload') result.attestation.payloadBase64 = Buffer.from('{}').toString('base64');
    if (failure === 'digest') result.policyDigest = '0'.repeat(64);
    if (failure === 'challenge') result.attestation.challenge = 'B'.repeat(64);
    if (failure === 'format') result.attestation.format = 'unsigned';
    if (failure === 'missing') delete (result as Partial<typeof result>).attestation;
    await expect(readWorkspaceVerificationPolicy(bridge(result), owner)).rejects.toThrow('unavailable or invalid');
  });

  it.each(['workspace', 'key'])('rejects wrong %s binding', async failure => {
    const binding = { ...owner, ...(failure === 'workspace' ? { workspacePath: resolve('/tests/other') } : { apiKey: 'rotated-fixture-key' }) };
    await expect(readWorkspaceVerificationPolicy(bridge(), binding)).rejects.toThrow('unavailable or invalid');
  });

  it('requires trusted host binding before calling transport', async () => {
    const transport = bridge();
    transport.assertHostBinding.mockImplementation(() => { throw new Error('bad binding'); });
    await expect(readWorkspaceVerificationPolicy(transport, owner)).rejects.toThrow('unavailable or invalid');
    expect(transport.invoke).not.toHaveBeenCalled();
  });

  it('requires installed workspace context', async () => {
    const transport = bridge();
    transport.hasWorkspaceContext = false;
    await expect(readWorkspaceVerificationPolicy(transport, owner)).rejects.toThrow('unavailable or invalid');
    expect(transport.invoke).not.toHaveBeenCalled();
  });

  it.each(['error', 'event', 'rejected'])('fails closed on transport %s without exposing diagnostic secrets', async kind => {
    const transport = bridge();
    if (kind === 'rejected') transport.invoke.mockRejectedValue(new Error(owner.apiKey));
    else transport.invoke.mockResolvedValue({ type: kind, payload: { message: owner.apiKey } });
    await expect(readWorkspaceVerificationPolicy(transport, owner)).rejects.toThrow(/^Workspace verification policy is unavailable or invalid$/);
  });

  it('captures binding before async response and never caches previous policy', async () => {
    const binding = { ...owner };
    const transport = bridge();
    const pending = readWorkspaceVerificationPolicy(transport, binding);
    binding.apiKey = 'changed'; binding.workspacePath = resolve('/tests/other');
    await expect(pending).resolves.toMatchObject({ policyDigest: snapshot().policyDigest });
    transport.invoke.mockRejectedValue(new Error('offline'));
    await expect(readWorkspaceVerificationPolicy(transport, owner)).rejects.toThrow('unavailable or invalid');
    expect(transport.invoke).toHaveBeenCalledTimes(2);
  });
});