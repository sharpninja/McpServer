/**
 * Ported from mcpserver-cline-v2-plugin/tests/todo.test.ts.
 *
 * Adaptation note: the canonical core reads the internal-TODO env override
 * from MCPSERVER_INTERNAL_TODO first, then the codex-prefixed back-compat
 * aliases (MCP_CODEX_INTERNAL_TODO / MCPSERVER_CODEX_INTERNAL_TODO /
 * CODEX_MCP_TODO) - see tools/todo.ts internalTodoModeValue() (Patch B). The
 * cleared-env list below is widened to include MCPSERVER_INTERNAL_TODO and
 * MCPSERVER_CODEX_INTERNAL_TODO so a stray ambient value cannot leak into the
 * "defaults to local tracking" / "environment override" specs. Everything
 * else ports unchanged.
 */
import { handleTodoTool } from '../src/tools/todo.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  nextResponse: ReplResponse = { type: 'result', payload: { ok: true } };

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    return this.nextResponse;
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

function restoreEnv(key: string, value: string | undefined): void {
  if (value === undefined) delete process.env[key];
  else process.env[key] = value;
}

describe('handleTodoTool internal TODO tracking toggle', () => {
  const envKeys = [
    'MCPSERVER_INTERNAL_TODO',
    'MCP_CODEX_INTERNAL_TODO',
    'MCPSERVER_CODEX_INTERNAL_TODO',
    'CODEX_MCP_TODO',
    'MCPSERVER_PLUGIN_CACHE_DIR',
    'MCP_PLUGIN_CACHE_DIR',
    'MCPSERVER_CACHE_DIR',
    'MCP_CACHE_DIR',
    'MCPSERVER_INTERNAL_TODO_STATE_FILE',
  ];
  let oldEnv: Record<string, string | undefined>;
  let cacheDir: string;

  beforeEach(() => {
    oldEnv = Object.fromEntries(envKeys.map((key) => [key, process.env[key]]));
    for (const key of envKeys) delete process.env[key];
    cacheDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-internal-todo-'));
    process.env.MCPSERVER_PLUGIN_CACHE_DIR = cacheDir;
  });

  afterEach(() => {
    for (const key of envKeys) restoreEnv(key, oldEnv[key]);
    fs.rmSync(cacheDir, { recursive: true, force: true });
  });

  test('defaults to local internal tracking without calling the bridge', async () => {
    const fake = new FakeBridge();

    const result = await handleTodoTool('todo_internal_status', {}, asBridge(fake));

    expect(result.result).toEqual({
      enabled: false,
      source: 'default',
      stateFile: path.join(cacheDir, 'internal-todo.yaml'),
    });
    expect(fake.calls).toHaveLength(0);
  });

  test('enable, status, and disable persist the cached mode', async () => {
    const fake = new FakeBridge();

    const enabled = await handleTodoTool('todo_internal_enable', {}, asBridge(fake));
    expect(enabled.result).toMatchObject({ enabled: true, source: 'cache' });
    expect(fs.readFileSync(path.join(cacheDir, 'internal-todo.yaml'), 'utf8')).toContain('enabled: true');

    const status = await handleTodoTool('todo_internal_status', {}, asBridge(fake));
    expect(status.result).toMatchObject({ enabled: true, source: 'cache' });

    const disabled = await handleTodoTool('todo_internal_disable', {}, asBridge(fake));
    expect(disabled.result).toMatchObject({ enabled: false, source: 'cache' });
    expect(fs.readFileSync(path.join(cacheDir, 'internal-todo.yaml'), 'utf8')).toContain('enabled: false');
    expect(fake.calls).toHaveLength(0);
  });

  test('environment override wins over cached mode', async () => {
    const fake = new FakeBridge();

    await handleTodoTool('todo_internal_disable', {}, asBridge(fake));
    process.env.MCP_CODEX_INTERNAL_TODO = 'on';

    const result = await handleTodoTool('todo_internal_status', {}, asBridge(fake));

    expect(result.result).toMatchObject({ enabled: true, source: 'environment' });
    expect(fake.calls).toHaveLength(0);
  });

  test('generic tracking tool accepts explicit mode aliases', async () => {
    const fake = new FakeBridge();

    const result = await handleTodoTool('todo_internal_tracking', { mode: 'mcp' }, asBridge(fake));

    expect(result.result).toMatchObject({ enabled: true, source: 'cache' });
    expect(fake.calls).toHaveLength(0);
  });
});

describe('handleTodoTool failsafe cache', () => {
  test('keeps local failsafe when todo_create returns an error', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'error',
      payload: { code: 'offline', message: 'server unavailable' },
    };
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-todo-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    try {
      await expect(
        handleTodoTool(
          'todo_create',
          {
            id: 'MCP-FAILSAFE-001',
            title: 'Failsafe TODO',
            section: 'Backlog',
            priority: 'high',
          },
          asBridge(fake),
        ),
      ).rejects.toThrow(/Local failsafe saved:/);

      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      const content = fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8');
      expect(content).toContain('workflow.todo.create');
      expect(content).toContain('MCP-FAILSAFE-001');
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('clears local failsafe after todo_update succeeds', async () => {
    const fake = new FakeBridge();
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-todo-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    try {
      await handleTodoTool(
        'todo_update',
        {
          id: 'MCP-FAILSAFE-001',
          remaining: 'none',
        },
        asBridge(fake),
      );

      expect(fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'))).toHaveLength(0);
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('uses marker-auth HTTP fallback for todo_query before the REPL bridge', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const oldApiKey = process.env.MCPSERVER_API_KEY;
    const oldWorkspacePath = process.env.MCPSERVER_WORKSPACE_PATH;
    const oldBaseUrl = process.env.MCPSERVER_BASE_URL;

    process.env.MCPSERVER_API_KEY = 'test-api-key';
    process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\FeatureFlags';
    process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      text: async () => '{"items":[],"totalCount":0}',
    })) as unknown as typeof fetch;

    try {
      const result = await handleTodoTool(
        'todo_query',
        { id: 'MCP-TODO-001', status: 'open' },
        asBridge(fake),
      );

      expect(fake.calls).toHaveLength(0);
      expect(result.result).toEqual({ items: [], totalCount: 0 });
      const call = (globalThis.fetch as jest.Mock).mock.calls[0];
      expect(String(call[0])).toBe(
        'http://127.0.0.1:8765/mcpserver/todo?id=MCP-TODO-001&done=false',
      );
      expect(call[1]).toEqual({
        headers: {
          'X-Api-Key': 'test-api-key',
          'X-Workspace-Path': 'F:\\GitHub\\FeatureFlags',
        },
      });
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', oldApiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', oldWorkspacePath);
      restoreEnv('MCPSERVER_BASE_URL', oldBaseUrl);
    }
  });

  test('normalizes todo_create request wrapper through HTTP fallback and clears failsafe', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const oldApiKey = process.env.MCPSERVER_API_KEY;
    const oldWorkspacePath = process.env.MCPSERVER_WORKSPACE_PATH;
    const oldBaseUrl = process.env.MCPSERVER_BASE_URL;
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-todo-failsafe-'));

    process.env.MCPSERVER_API_KEY = 'test-api-key';
    process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\FeatureFlags';
    process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      status: 201,
      headers: { get: () => 'application/json' },
      text: async () => '{"success":true}',
    })) as unknown as typeof fetch;

    try {
      await handleTodoTool(
        'todo_create',
        {
          request: {
            id: 'MCP-CLINE-001',
            title: 'Create through Cline fallback',
            section: 'Architecture',
            priority: 'low',
            technicalDetails: 'detail',
            implementationTasks: ['task one'],
          },
        },
        asBridge(fake),
      );

      expect(fake.calls).toHaveLength(0);
      expect(fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'))).toHaveLength(0);
      const call = (globalThis.fetch as jest.Mock).mock.calls[0];
      expect(String(call[0])).toBe('http://127.0.0.1:8765/mcpserver/todo');
      expect(JSON.parse(call[1].body)).toEqual({
        id: 'MCP-CLINE-001',
        title: 'Create through Cline fallback',
        priority: 'low',
        section: 'Backlog',
        technicalDetails: ['detail'],
        implementationTasks: [{ task: 'task one', done: false }],
      });
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', oldApiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', oldWorkspacePath);
      restoreEnv('MCPSERVER_BASE_URL', oldBaseUrl);
      restoreEnv('MCPSERVER_FAILSAFE_DIR', oldFailsafeDir);
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('preserves TODO HTTP error bodies and keeps the local failsafe', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const oldApiKey = process.env.MCPSERVER_API_KEY;
    const oldWorkspacePath = process.env.MCPSERVER_WORKSPACE_PATH;
    const oldBaseUrl = process.env.MCPSERVER_BASE_URL;
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-todo-failsafe-'));

    process.env.MCPSERVER_API_KEY = 'test-api-key';
    process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\FeatureFlags';
    process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
    globalThis.fetch = jest.fn(async () => ({
      ok: false,
      status: 400,
      headers: { get: () => 'application/json' },
      text: async () => '{"error":"bad section"}',
    })) as unknown as typeof fetch;

    try {
      await expect(
        handleTodoTool(
          'todo_update',
          { id: 'MCP-CLINE-001', section: 'Architecture' },
          asBridge(fake),
        ),
      ).rejects.toThrow(/bad section.*Local failsafe saved:/);

      expect(fake.calls).toHaveLength(0);
      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      expect(fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8')).toContain('workflow.todo.update');
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', oldApiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', oldWorkspacePath);
      restoreEnv('MCPSERVER_BASE_URL', oldBaseUrl);
      restoreEnv('MCPSERVER_FAILSAFE_DIR', oldFailsafeDir);
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });
});
