/**
 * Ported from mcpserver-cline-v2-plugin/tests/memory.test.ts. Imports map
 * 1:1 onto the canonical core (tools/memory.js). Covers the workflow.memory.*
 * routing, the request-wrapper unwrap + mutation audit (appendActions), and
 * the read/mutation failsafe split.
 */
import { jest } from '@jest/globals';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ReplBridge } from '../src/transport/repl-bridge.js';

describe('memory tool handlers', () => {
  let failsafeDir: string;

  beforeEach(() => {
    failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-memory-test-'));
    process.env.MCP_FAILSAFE_DIR = failsafeDir;
  });

  afterEach(() => {
    delete process.env.MCP_FAILSAFE_DIR;
    fs.rmSync(failsafeDir, { recursive: true, force: true });
  });

  test('canHandleMemoryTool covers all memory names', async () => {
    const { canHandleMemoryTool } = await import('../src/tools/memory.js');
    expect(canHandleMemoryTool('memory_list')).toBe(true);
    expect(canHandleMemoryTool('memory_get')).toBe(true);
    expect(canHandleMemoryTool('memory_add')).toBe(true);
    expect(canHandleMemoryTool('memory_update')).toBe(true);
    expect(canHandleMemoryTool('memory_remove')).toBe(true);
    expect(canHandleMemoryTool('memory_missing')).toBe(false);
  });

  test('memory_list routes through workflow.memory.list', async () => {
    const { handleMemoryTool, memoryTools } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'result',
        payload: { result: { items: [{ id: 'MEMORY-REQ-001', text: 'Keep exact wording.' }] } },
      })),
    } as unknown as ReplBridge;

    const result = await handleMemoryTool('memory_list', { scope: 'Effective' }, bridge);

    expect(memoryTools.map((tool) => tool.name)).toContain('memory_add');
    expect(bridge.invoke).toHaveBeenCalledWith('workflow.memory.list', { scope: 'Effective' });
    expect(result).toEqual({ result: { items: [{ id: 'MEMORY-REQ-001', text: 'Keep exact wording.' }] } });
  });

  test('memory_add unwraps request args and clears mutation failsafe after success', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'result',
        payload: { result: { id: 'MEMORY-REQ-001', success: true } },
      })),
    } as unknown as ReplBridge;

    const result = await handleMemoryTool(
      'memory_add',
      { request: { id: 'MEMORY-REQ-001', category: 'REQ', text: 'Keep exact wording.', scope: 'Workspace' } },
      bridge,
    );

    expect(bridge.invoke).toHaveBeenNthCalledWith(1, 'workflow.memory.add', {
      id: 'MEMORY-REQ-001',
      category: 'REQ',
      text: 'Keep exact wording.',
      scope: 'Workspace',
    });
    expect(bridge.invoke).toHaveBeenNthCalledWith(2, 'workflow.sessionlog.appendActions', {
      actions: [
        {
          description: 'Memory add MEMORY-REQ-001',
          type: 'edit',
          status: 'completed',
        },
      ],
    });
    expect(result).toEqual({ result: { id: 'MEMORY-REQ-001', success: true } });
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('memory_update reports bridge failures with failsafe path', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => {
        throw new Error('offline');
      }),
    } as unknown as ReplBridge;

    await expect(handleMemoryTool('memory_update', { id: 'MEMORY-REQ-001', text: 'Updated' }, bridge))
      .rejects.toThrow(/offline Local failsafe saved:/);
    expect(fs.readdirSync(failsafeDir).length).toBe(1);
  });

  test('memory_remove reports workflow errors with failsafe path', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'error',
        payload: { code: 'not_found', message: 'Missing memory' },
      })),
    } as unknown as ReplBridge;

    await expect(handleMemoryTool('memory_remove', { id: 'MEMORY-REQ-001' }, bridge))
      .rejects.toThrow(/not_found: Missing memory Local failsafe saved:/);
    expect(fs.readdirSync(failsafeDir).length).toBe(1);
  });

  test('memory_get reports bridge failures without failsafe for read operations', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => {
        throw 'transport unavailable';
      }),
    } as unknown as ReplBridge;

    await expect(handleMemoryTool('memory_get', { id: 'MEMORY-REQ-001' }, bridge))
      .rejects.toThrow('transport unavailable');
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('memory_get reports default workflow error text when payload omits details', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'error',
        payload: {},
      })),
    } as unknown as ReplBridge;

    await expect(handleMemoryTool('memory_get', { id: 'MEMORY-REQ-001' }, bridge))
      .rejects.toThrow('error: Unknown error');
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('unknown memory tool fails before invoking bridge', async () => {
    const { handleMemoryTool } = await import('../src/tools/memory.js');
    const bridge = { invoke: jest.fn() } as unknown as ReplBridge;

    await expect(handleMemoryTool('memory_unknown', {}, bridge)).rejects.toThrow('Unknown memory tool: memory_unknown');
    expect(bridge.invoke).not.toHaveBeenCalled();
  });
});
