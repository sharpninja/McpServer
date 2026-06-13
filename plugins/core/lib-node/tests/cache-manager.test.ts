/**
 * New coverage for cache/cache-manager.ts. The cline-v2 plugin exercised
 * the cache only indirectly (via the session-shim failsafe specs); the
 * canonical core owns Patch A (cline v1's retry cap ported into the async
 * flush) and deserves direct tests for write/delete/status and the
 * retry/flush state machine.
 */
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as yaml from 'js-yaml';
import {
  cacheWrite,
  cacheDelete,
  cacheFlush,
  cacheStatus,
} from '../src/cache/cache-manager.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  nextResponse: ReplResponse = { type: 'result', payload: { ok: true } };
  responses: ReplResponse[] = [];

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    if (this.responses.length > 0) return this.responses.shift()!;
    return this.nextResponse;
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

function listYaml(dir: string): string[] {
  return fs.readdirSync(dir).filter((file) => file.endsWith('.yaml')).sort();
}

describe('cache-manager', () => {
  let failsafeDir: string;
  let oldFailsafeDir: string | undefined;

  beforeEach(() => {
    oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-cache-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
  });

  afterEach(() => {
    if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
    else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
    fs.rmSync(failsafeDir, { recursive: true, force: true });
  });

  test('cacheWrite persists a YAML entry with method, params, and retryCount 0', async () => {
    const filePath = await cacheWrite('workflow.todo.create', { id: 'MCP-001' });
    expect(filePath.startsWith(failsafeDir)).toBe(true);
    const entry = yaml.load(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
    expect(entry).toMatchObject({
      method: 'workflow.todo.create',
      params: { id: 'MCP-001' },
      retryCount: 0,
    });
    expect(typeof entry.id).toBe('string');
    expect(typeof entry.timestamp).toBe('string');
  });

  test('cacheStatus counts pending YAML entries and cacheDelete removes them', async () => {
    expect(await cacheStatus()).toBe(0);
    const a = await cacheWrite('m.one', { x: 1 });
    await cacheWrite('m.two', { x: 2 });
    expect(await cacheStatus()).toBe(2);
    await cacheDelete(a);
    expect(await cacheStatus()).toBe(1);
  });

  test('cacheDelete is a no-op when the file is already gone', async () => {
    await expect(cacheDelete(path.join(failsafeDir, 'missing.yaml'))).resolves.toBeUndefined();
  });

  test('cacheFlush replays a pending entry and deletes it on success', async () => {
    await cacheWrite('client.Workspace.CreateAsync', { request: { name: 'x' } });
    const fake = new FakeBridge();

    const result = await cacheFlush(asBridge(fake));

    expect(result).toEqual({ flushed: 1, failed: 0, pending: 0 });
    expect(fake.calls).toEqual([
      { method: 'client.Workspace.CreateAsync', params: { request: { name: 'x' } } },
    ]);
    expect(listYaml(failsafeDir)).toHaveLength(0);
  });

  test('cacheFlush keeps the entry and bumps retryCount when the bridge returns an error', async () => {
    const filePath = await cacheWrite('workflow.todo.create', { id: 'MCP-002' });
    const fake = new FakeBridge();
    fake.nextResponse = { type: 'error', payload: { code: 'offline', message: 'down' } };

    const result = await cacheFlush(asBridge(fake));

    expect(result).toEqual({ flushed: 0, failed: 1, pending: 1 });
    const entry = yaml.load(fs.readFileSync(filePath, 'utf8')) as { retryCount: number };
    expect(entry.retryCount).toBe(1);
  });

  test('cacheFlush skips poison entries once retryCount reaches the cap (Patch A)', async () => {
    // Hand-write an entry already at the retry cap (3). It must be skipped:
    // neither flushed nor failed, and never sent to the bridge.
    fs.writeFileSync(
      path.join(failsafeDir, 'poison-workflow-todo-create.yaml'),
      yaml.dump({
        id: 'poison',
        timestamp: new Date().toISOString(),
        method: 'workflow.todo.create',
        params: { id: 'MCP-POISON' },
        retryCount: 3,
      }),
    );
    const fake = new FakeBridge();

    const result = await cacheFlush(asBridge(fake));

    expect(fake.calls).toHaveLength(0);
    expect(result).toEqual({ flushed: 0, failed: 0, pending: 1 });
    expect(listYaml(failsafeDir)).toHaveLength(1);
  });

  test('cacheFlush escalates an erroring entry across repeated flushes until it is capped', async () => {
    await cacheWrite('workflow.todo.create', { id: 'MCP-003' });
    const fake = new FakeBridge();
    fake.nextResponse = { type: 'error', payload: { code: 'offline' } };

    // 1 -> 2 -> 3 bumps, then capped (skipped, no further bridge call).
    expect(await cacheFlush(asBridge(fake))).toEqual({ flushed: 0, failed: 1, pending: 1 });
    expect(await cacheFlush(asBridge(fake))).toEqual({ flushed: 0, failed: 1, pending: 1 });
    expect(await cacheFlush(asBridge(fake))).toEqual({ flushed: 0, failed: 1, pending: 1 });
    expect(fake.calls).toHaveLength(3);

    const capped = await cacheFlush(asBridge(fake));
    expect(capped).toEqual({ flushed: 0, failed: 0, pending: 1 });
    expect(fake.calls).toHaveLength(3); // not retried after the cap
  });

  test('cacheFlush counts a malformed (method-less) entry as failed', async () => {
    fs.writeFileSync(
      path.join(failsafeDir, 'broken.yaml'),
      yaml.dump({ id: 'x', timestamp: 'now', retryCount: 0 }),
    );
    const fake = new FakeBridge();

    const result = await cacheFlush(asBridge(fake));

    expect(fake.calls).toHaveLength(0);
    expect(result.failed).toBe(1);
  });

  test('cacheFlush on an empty/absent directory returns zeroed counts', async () => {
    fs.rmSync(failsafeDir, { recursive: true, force: true });
    const fake = new FakeBridge();
    const result = await cacheFlush(asBridge(fake));
    expect(result).toEqual({ flushed: 0, failed: 0, pending: 0 });
  });
});
