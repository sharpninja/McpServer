/**
 * cache-manager.test.ts
 * Tests for V4CacheManager (cache integration increment, PLAN-AGENTPARITY-001 Core Package Integration wave).
 *
 * Byrd v4 TDD: written FIRST (red until cache-manager.ts exists), then made green via implementation.
 * Contract: TR-MCP-AGENT-PARITY-013 - scoped layout, YAML pending queue, 3-retry flush, idempotent recovery.
 * Behavioral parity with V4CacheManager.cs in tests/AgentPluginCore/Stubs/V4CoreStubs.cs.
 */

import { describe, it, before, after, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, rm, readdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { V4CacheManager } from '../src/cache-manager.js';
import type { IV4ReplBridge, V4ReplEnvelope, V4ReplResponse } from '../src/types.js';

// Minimal mock bridge for tests (no framework needed - simple class)
class MockReplBridge implements IV4ReplBridge {
  readonly calls: V4ReplEnvelope[] = [];
  shouldSucceed = true;

  async SendEnvelopeAsync(envelope: V4ReplEnvelope): Promise<V4ReplResponse> {
    this.calls.push(envelope);
    if (!this.shouldSucceed) {
      return { Success: false, ErrorCode: 'MOCK_FAIL', ErrorMessage: 'Simulated bridge failure' };
    }
    return { Success: true, Result: { ok: true, replayed: envelope.RequestId } };
  }
}

describe('V4CacheManager (core package - TR-MCP-AGENT-PARITY-013)', () => {
  let tmpBase: string;

  before(async () => {
    tmpBase = await mkdtemp(join(tmpdir(), 'v4cache-tests-'));
  });

  after(async () => {
    try { await rm(tmpBase, { recursive: true, force: true }); } catch {}
  });

  describe('GetScopedCachePath', () => {
    it('produces v4 failsafe/agent/workspaces layout', () => {
      const cm = new V4CacheManager();
      const path = cm.GetScopedCachePath('C:\\work\\myapp', 'claude-code');
      assert.ok(path.includes('failsafe/claude-code'), `expected failsafe/claude-code in: ${path}`);
      assert.ok(path.includes('workspaces/'), `expected workspaces/ in: ${path}`);
    });

    it('uses base64url of workspace key (matches C# V4CacheManager)', () => {
      const cm = new V4CacheManager();
      const ws = 'F:\\GitHub\\TestApp';
      const path = cm.GetScopedCachePath(ws, 'codex');
      // base64url: replace + with -, / with _, strip =
      const expectedSafe = Buffer.from(ws).toString('base64')
        .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
      assert.ok(path.includes(expectedSafe), `expected ${expectedSafe} in: ${path}`);
    });

    it('handles empty workspace key and agent gracefully', () => {
      const cm = new V4CacheManager();
      const path = cm.GetScopedCachePath('', '');
      assert.ok(typeof path === 'string');
      assert.ok(path.includes('failsafe/'));
    });
  });

  describe('WritePendingAsync', () => {
    it('creates pending yaml file with correct structure', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'write-test');
      await cm.WritePendingAsync(ws, 'test-agent', 'e1', { type: 'sessionlog.turn', data: 'x' });

      const scoped = cm.GetScopedCachePath(ws, 'test-agent');
      const pendingDir = join(scoped, 'pending');
      const files = await readdir(pendingDir);
      const yamls = files.filter(f => f.endsWith('.yaml'));
      assert.equal(yamls.length, 1, 'one pending yaml created');
    });

    it('creates both pending and failed dirs on write', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'dirs-test');
      const { stat } = await import('node:fs/promises');

      await cm.WritePendingAsync(ws, 'agent1', 'e1', { x: 1 });
      const scoped = cm.GetScopedCachePath(ws, 'agent1');
      const pendingStat = await stat(join(scoped, 'pending'));
      const failedStat = await stat(join(scoped, 'failed'));
      assert.ok(pendingStat.isDirectory());
      assert.ok(failedStat.isDirectory());
    });

    it('sequences multiple writes with incrementing names', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'seq-test');
      await cm.WritePendingAsync(ws, 'agent2', 'e1', { a: 1 });
      await cm.WritePendingAsync(ws, 'agent2', 'e2', { b: 2 });
      await cm.WritePendingAsync(ws, 'agent2', 'e3', { c: 3 });

      const scoped = cm.GetScopedCachePath(ws, 'agent2');
      const files = await readdir(join(scoped, 'pending'));
      assert.equal(files.filter(f => f.endsWith('.yaml')).length, 3, '3 yamls created');
    });
  });

  describe('FlushPendingAsync', () => {
    it('empty pending returns success with zeros', async () => {
      const cm = new V4CacheManager();
      const result = await cm.FlushPendingAsync(join(tmpBase, 'empty-flush'), 'no-agent', 3);
      assert.equal(result.Success, true);
      assert.equal(result.RetriesUsed, 0);
      assert.equal(result.MovedToFailed, 0);
    });

    it('flush after write succeeds within retry limit', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'flush-ok');
      await cm.WritePendingAsync(ws, 'agent3', 'e1', { type: 'sessionlog', data: 'x' });

      const result = await cm.FlushPendingAsync(ws, 'agent3', 3);
      assert.equal(result.Success, true);
      assert.ok(result.RetriesUsed <= 3, `retries ${result.RetriesUsed} should be <= 3`);
      assert.equal(result.MovedToFailed, 0);
    });

    it('maxRetries=0 moves entries to failed', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'flush-fail');
      await cm.WritePendingAsync(ws, 'agent4', 'f1', { x: 1 });

      const result = await cm.FlushPendingAsync(ws, 'agent4', 0);
      assert.ok(result.MovedToFailed >= 1, 'should move to failed when retries exceeded');
      assert.equal(result.Success, false);
      assert.ok(result.Error?.includes('Retries exceeded'), `error should mention retries: ${result.Error}`);
    });

    it('pending dir empty after successful flush', async () => {
      const cm = new V4CacheManager();
      const ws = join(tmpBase, 'flush-empty-after');
      await cm.WritePendingAsync(ws, 'agent5', 'e1', { a: 1 });
      await cm.FlushPendingAsync(ws, 'agent5', 3);

      const scoped = cm.GetScopedCachePath(ws, 'agent5');
      const files = await readdir(join(scoped, 'pending'));
      assert.equal(files.filter(f => f.endsWith('.yaml')).length, 0, 'pending empty after flush');
    });
  });

  describe('RecoverAndReplayAsync', () => {
    it('replays pending entries via bridge and clears queue', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-basic');

      await cm.WritePendingAsync(ws, 'codex', 'turn-42', { action: 'beginTurn' });
      await cm.WritePendingAsync(ws, 'codex', 'todo-7', { action: 'createTodo' });

      const rec = await cm.RecoverAndReplayAsync(ws, 'codex', bridge);

      assert.equal(rec.Success, true);
      assert.equal(rec.EntriesReplayed, 2, 'both entries replayed');
      assert.ok(rec.ProducedArtifacts.some(a => a.includes('replayed:')));
      assert.equal(bridge.calls.length, 2, 'bridge called twice');
      assert.ok(bridge.calls.every(c => c.Type === 'workflow.cache.replay'));
    });

    it('is idempotent - second call replays 0 entries', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-idem');

      await cm.WritePendingAsync(ws, 'codex', 'log-1', { type: 'sessionlog.beginTurn' });

      const r1 = await cm.RecoverAndReplayAsync(ws, 'codex', bridge);
      assert.equal(r1.EntriesReplayed, 1);

      const r2 = await cm.RecoverAndReplayAsync(ws, 'codex', bridge);
      assert.equal(r2.EntriesReplayed, 0, 'idempotent: second call returns 0');
    });

    it('recovers entries from failed dir too', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-failed');

      await cm.WritePendingAsync(ws, 'codex', 'e-fail', { action: 'test' });
      await cm.FlushPendingAsync(ws, 'codex', 0); // force move to failed/

      const rec = await cm.RecoverAndReplayAsync(ws, 'codex', bridge);
      assert.equal(rec.Success, true);
      assert.equal(rec.EntriesReplayed, 1, 'recovers from failed dir');
    });

    it('handles empty dirs gracefully (no error)', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-empty');

      const rec = await cm.RecoverAndReplayAsync(ws, 'codex', bridge);
      assert.equal(rec.Success, true);
      assert.equal(rec.EntriesReplayed, 0);
    });

    it('envelope uses workflow.cache.replay type (golden replay contract)', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-envelope');

      await cm.WritePendingAsync(ws, 'grok', 'req-001', { action: 'beginTurn', req: 'req-001' });
      await cm.RecoverAndReplayAsync(ws, 'grok', bridge);

      assert.ok(bridge.calls.length === 1);
      assert.equal(bridge.calls[0].Type, 'workflow.cache.replay', 'golden contract: type must be workflow.cache.replay');
      assert.equal(bridge.calls[0].AgentId, 'grok', 'agentId must be passed through');
    });

    it('produces replayed:ID artifacts matching C# golden contract', async () => {
      const cm = new V4CacheManager();
      const bridge = new MockReplBridge();
      const ws = join(tmpBase, 'recover-artifacts');

      await cm.WritePendingAsync(ws, 'parity-agent', 'log-1', { type: 'sessionlog.beginTurn' });
      await cm.WritePendingAsync(ws, 'parity-agent', 'todo-9', { type: 'todo.create', id: 'T-009' });

      const rec = await cm.RecoverAndReplayAsync(ws, 'parity-agent', bridge);
      assert.equal(rec.EntriesReplayed, 2);
      assert.ok(rec.ProducedArtifacts.every(a => a.startsWith('replayed:')));
    });
  });
});
