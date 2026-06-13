/**
 * Ported from mcpserver-cline-v2-plugin/tests/workspace.test.ts. Imports map
 * 1:1 onto the canonical core (tools/workspace.js). Covers workspace_ensure
 * register/init, the already-registered short circuit, and the create
 * failsafe.
 */
import { handleWorkspaceTool } from '../src/tools/workspace.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  responses: ReplResponse[] = [];

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    return this.responses.shift() ?? { type: 'result', payload: { ok: true } };
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

describe('workspace_ensure', () => {
  test('registers and initializes a missing untrusted workspace with failsafe-protected mutations', async () => {
    const fake = new FakeBridge();
    const workspacePath = 'F:\\GitHub\\NewWorkspace';
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-workspace-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
    fake.responses = [
      { type: 'result', payload: { result: { items: [], totalCount: 0 } } },
      { type: 'result', payload: { result: { success: true, workspace: { workspacePath } } } },
      { type: 'result', payload: { result: { success: true, filesCreated: ['AGENTS-README-FIRST.yaml'] } } },
    ];

    try {
      const result = await handleWorkspaceTool(
        'workspace_ensure',
        { workspacePath, name: 'NewWorkspace' },
        asBridge(fake),
      );

      expect(result).toMatchObject({
        trusted: false,
        registered: true,
        initialized: true,
        created: true,
        workspacePath,
        markerReloadRequired: true,
      });
      expect(fake.calls.map((call) => call.method)).toEqual([
        'client.Workspace.ListAsync',
        'client.Workspace.CreateAsync',
        'client.Workspace.InitAsync',
      ]);
      expect(fake.calls[1].params).toEqual({
        request: { workspacePath, name: 'NewWorkspace' },
      });
      expect(fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'))).toHaveLength(0);
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('does not create when the workspace is already registered', async () => {
    const fake = new FakeBridge();
    const workspacePath = 'F:\\GitHub\\ExistingWorkspace';
    fake.responses = [
      { type: 'result', payload: { result: { items: [{ workspacePath }], totalCount: 1 } } },
      { type: 'result', payload: { result: { success: true, filesCreated: [] } } },
    ];

    const result = await handleWorkspaceTool(
      'workspace_ensure',
      { workspacePath },
      asBridge(fake),
    );

    expect(result).toMatchObject({ created: false, initialized: true });
    expect(fake.calls.map((call) => call.method)).toEqual([
      'client.Workspace.ListAsync',
      'client.Workspace.InitAsync',
    ]);
  });

  test('keeps a YAML failsafe when workspace creation fails', async () => {
    const fake = new FakeBridge();
    const workspacePath = 'F:\\GitHub\\FailWorkspace';
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-workspace-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;
    fake.responses = [
      { type: 'result', payload: { result: { items: [], totalCount: 0 } } },
      { type: 'error', payload: { code: 'offline', message: 'server unavailable' } },
    ];

    try {
      await expect(handleWorkspaceTool('workspace_ensure', { workspacePath }, asBridge(fake))).rejects.toThrow(
        /Local failsafe saved:/,
      );
      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      expect(fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8')).toContain('client.Workspace.CreateAsync');
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });
});
