// SPDX-License-Identifier: Apache-2.0
import { createHash } from 'crypto';
import * as fs from 'fs';
import { tmpdir } from 'os';
import { join } from 'path';
import { verifyPinnedPluginArtifact } from '../src/discovery/plugin-artifact.js';

const payload = Buffer.from('reviewed plugin artifact bytes\0including binary content');
const sha256 = createHash('sha256').update(payload).digest('hex');
let root: string;
let artifactPath: string;
function options() {
  return { artifactPath, binding: { agentName: 'ClineQb',
    pluginName: '@sharpninja/mcpserver-cline-v2-plugin', pluginVersion: '1.118.0', artifactSha256: sha256 } };
}
beforeEach(() => {
  root = fs.mkdtempSync(join(tmpdir(), 'qb-plugin-artifact-'));
  artifactPath = join(root, 'plugin.tgz');
  fs.writeFileSync(artifactPath, payload);
});
afterEach(() => fs.rmSync(root, { recursive: true, force: true }));

test('authenticates the exact artifact and returns frozen review metadata', () => {
  const artifact = verifyPinnedPluginArtifact(options());
  expect(artifact.byteLength).toBe(payload.length);
  expect(artifact.binding).toEqual({ ...options().binding, artifactSha256: sha256.toUpperCase() });
  expect(Object.isFrozen(artifact)).toBe(true);
  expect(Object.isFrozen(artifact.binding)).toBe(true);
  expect(artifact.readBytes()).toEqual(payload);
});

test('refuses changed artifact bytes even when name and version remain the same', () => {
  fs.writeFileSync(artifactPath, Buffer.from('replaced plugin contents'));
  expect(() => verifyPinnedPluginArtifact(options())).toThrow(/MCP_UNTRUSTED/);
});

test('retains only verified bytes after path replacement and caller mutation', () => {
  const configured = options();
  const artifact = verifyPinnedPluginArtifact(configured);
  fs.renameSync(artifactPath, join(root, 'old.tgz'));
  fs.writeFileSync(artifactPath, Buffer.from('replacement'));
  configured.binding.pluginVersion = 'different';
  configured.binding.artifactSha256 = '0'.repeat(64);
  const copy = artifact.readBytes();
  copy.fill(0);
  expect(artifact.readBytes()).toEqual(payload);
  expect(artifact.binding).toEqual({ ...options().binding, artifactSha256: sha256.toUpperCase() });
});

test.each(['missing', 'relative', 'directory', 'empty', 'symlink'])('refuses %s artifacts', kind => {
  const configured = options();
  if (kind === 'missing') fs.unlinkSync(artifactPath);
  if (kind === 'relative') configured.artifactPath = 'plugin.tgz';
  if (kind === 'directory') configured.artifactPath = root;
  if (kind === 'empty') fs.writeFileSync(artifactPath, '');
  if (kind === 'symlink') {
    fs.renameSync(artifactPath, join(root, 'target.tgz'));
    fs.symlinkSync(join(root, 'target.tgz'), artifactPath);
  }
  expect(() => verifyPinnedPluginArtifact(configured)).toThrow(/MCP_UNTRUSTED/);
});

test.each(['', 'invalid', 'A'.repeat(63), 'A'.repeat(64) + '\n'])('refuses malformed SHA-256 pin %p', pin => {
  const configured = options();
  configured.binding.artifactSha256 = pin;
  expect(() => verifyPinnedPluginArtifact(configured)).toThrow(/MCP_UNTRUSTED/);
});

test.each([0, NaN, Infinity, 513 * 1024 * 1024, 1])('refuses invalid or insufficient byte limit %p', maxBytes => {
  expect(() => verifyPinnedPluginArtifact({ ...options(), maxBytes })).toThrow(/MCP_UNTRUSTED/);
});

test('does not expose native errors or the artifact path in a failure', () => {
  fs.unlinkSync(artifactPath);
  expect(() => verifyPinnedPluginArtifact(options())).toThrow(
    'MCP_UNTRUSTED: Bundled plugin artifact does not match its reviewed pin');
  try { verifyPinnedPluginArtifact(options()); } catch (error) {
    expect(String(error)).not.toContain(root);
  }
});

test.each(['truncation', 'growth'])('rejects %s during archive capture', change => {
  const original = fs.readSync;
  const spy = jest.spyOn(require('fs'), 'readSync').mockImplementationOnce((...args: unknown[]) => {
    fs.writeFileSync(artifactPath, change === 'truncation'
      ? payload.subarray(0, 4) : Buffer.concat([payload, Buffer.from('extra')]));
    return (original as (...parameters: unknown[]) => number)(...args);
  });
  try {
    expect(() => verifyPinnedPluginArtifact(options())).toThrow(/MCP_UNTRUSTED/);
    expect(spy).toHaveBeenCalled();
  } finally { spy.mockRestore(); }
});