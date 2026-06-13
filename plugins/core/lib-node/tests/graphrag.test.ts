/**
 * Ported from mcpserver-cline-v2-plugin/tests/graphrag.test.ts. Imports map
 * 1:1 onto the canonical core (tools/graphrag.js). Covers the marker-auth
 * HTTP fallback (read + mutation), error-body preservation, and failsafe
 * clear-on-success.
 */
import { handleGraphragTool } from '../src/tools/graphrag.js';
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

function setMarkerEnv(): { apiKey?: string; workspacePath?: string; baseUrl?: string } {
  const old = {
    apiKey: process.env.MCPSERVER_API_KEY,
    workspacePath: process.env.MCPSERVER_WORKSPACE_PATH,
    baseUrl: process.env.MCPSERVER_BASE_URL,
  };
  process.env.MCPSERVER_API_KEY = 'test-api-key';
  process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\McpServer';
  process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
  return old;
}

describe('handleGraphragTool HTTP fallback', () => {
  test('uses marker-auth HTTP fallback for graphrag_status before REPL', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const old = setMarkerEnv();

    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      text: async () => '{"enabled":true,"indexed":false}',
    })) as unknown as typeof fetch;

    try {
      const result = await handleGraphragTool('graphrag_status', {}, asBridge(fake));

      expect(fake.calls).toHaveLength(0);
      expect(result.result).toEqual({ enabled: true, indexed: false });
      expect(globalThis.fetch).toHaveBeenCalledWith(
        'http://127.0.0.1:8765/mcpserver/graphrag/status',
        {
          headers: {
            'X-Api-Key': 'test-api-key',
            'X-Workspace-Path': 'F:\\GitHub\\McpServer',
          },
        },
      );
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', old.apiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', old.workspacePath);
      restoreEnv('MCPSERVER_BASE_URL', old.baseUrl);
    }
  });

  test('preserves GraphRAG HTTP error bodies and keeps mutating failsafe', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const old = setMarkerEnv();
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-graphrag-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    globalThis.fetch = jest.fn(async () => ({
      ok: false,
      status: 400,
      headers: { get: () => 'application/json' },
      text: async () => '{"error":"sourceEntityId is required"}',
    })) as unknown as typeof fetch;

    try {
      await expect(
        handleGraphragTool(
          'graphrag_rel_create',
          { targetEntityId: 'ent-2', relationshipType: 'validates' },
          asBridge(fake),
        ),
      ).rejects.toThrow(/sourceEntityId is required.*Local failsafe saved:/);

      expect(fake.calls).toHaveLength(0);
      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      const content = fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8');
      expect(content).toContain('workflow.graphrag.relationships.create');
      expect(content).toContain('targetEntityId: ent-2');
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', old.apiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', old.workspacePath);
      restoreEnv('MCPSERVER_BASE_URL', old.baseUrl);
      restoreEnv('MCPSERVER_FAILSAFE_DIR', oldFailsafeDir);
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('clears GraphRAG failsafe after mutating HTTP fallback succeeds', async () => {
    const fake = new FakeBridge();
    const oldFetch = globalThis.fetch;
    const old = setMarkerEnv();
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-graphrag-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      status: 201,
      headers: { get: () => 'application/json' },
      text: async () => '{"id":"ent-1","name":"McpServer"}',
    })) as unknown as typeof fetch;

    try {
      const result = await handleGraphragTool(
        'graphrag_entity_create',
        { name: 'McpServer', entityType: 'component', description: 'server' },
        asBridge(fake),
      );

      expect(result.result).toEqual({ id: 'ent-1', name: 'McpServer' });
      expect(fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'))).toHaveLength(0);
      const call = (globalThis.fetch as jest.Mock).mock.calls[0];
      expect(String(call[0])).toBe('http://127.0.0.1:8765/mcpserver/graphrag/entities');
      expect(JSON.parse(call[1].body)).toEqual({
        name: 'McpServer',
        entityType: 'component',
        description: 'server',
      });
    } finally {
      globalThis.fetch = oldFetch;
      restoreEnv('MCPSERVER_API_KEY', old.apiKey);
      restoreEnv('MCPSERVER_WORKSPACE_PATH', old.workspacePath);
      restoreEnv('MCPSERVER_BASE_URL', old.baseUrl);
      restoreEnv('MCPSERVER_FAILSAFE_DIR', oldFailsafeDir);
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });
});
