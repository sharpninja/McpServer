import { jest } from '@jest/globals';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ReplBridge } from '../src/transport/repl-bridge.js';

describe('brain slot tool handlers', () => {
  let failsafeDir: string;

  beforeEach(() => {
    failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-brain-slot-test-'));
    process.env.MCP_FAILSAFE_DIR = failsafeDir;
  });

  afterEach(() => {
    delete process.env.MCP_FAILSAFE_DIR;
    fs.rmSync(failsafeDir, { recursive: true, force: true });
  });

  test('canHandleBrainSlotTool covers all brain slot names', async () => {
    const { canHandleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    expect(canHandleBrainSlotTool('brain_slot_list')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_get')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_upsert')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_delete')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_enable')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_disable')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_status')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_invoke')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_orchestrate')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_aot_reconcile')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_weight_update')).toBe(true);
    expect(canHandleBrainSlotTool('brain_slot_missing')).toBe(false);
  });

  test('brain_slot_upsert exposes constrained role/provider schemas and routes through the same tool name', async () => {
    const { brainSlotTools, handleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'result',
        payload: { result: { slotId: 'curiosity-main', role: 'CuriosityEngine' } },
      })),
    } as unknown as ReplBridge;

    const upsert = brainSlotTools.find((tool) => tool.name === 'brain_slot_upsert');
    const properties = upsert?.inputSchema.properties as Record<string, unknown> | undefined;
    expect(properties?.role).toMatchObject({
      enum: ['Creativity', 'Logic', 'CuriosityEngine', 'ArbiterOfTruth'],
    });
    expect(properties?.providerKind).toMatchObject({
      enum: ['OpenAI', 'OpenAICompatible'],
    });
    expect(properties?.credentialReference).toMatchObject({
      pattern: '^(env|config|file):.+',
    });

    const args = {
      workspacePath: 'F:\\GitHub\\McpServer',
      slotId: 'curiosity-main',
      role: 'CuriosityEngine',
      providerKind: 'OpenAI',
      modelId: 'gpt-test',
      credentialReference: 'env:OPENAI_API_KEY',
      enabled: true,
      replaceExisting: true,
    };
    const result = await handleBrainSlotTool('brain_slot_upsert', args, bridge);

    expect(bridge.invoke).toHaveBeenCalledWith('brain_slot_upsert', args);
    expect(result).toEqual({ result: { slotId: 'curiosity-main', role: 'CuriosityEngine' } });
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('brain_slot_weight_update exposes approval gates and routes through the same tool name', async () => {
    const { brainSlotTools, handleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    const bridge = {
      invoke: jest.fn(async () => ({
        type: 'result',
        payload: { result: { status: 'committed' } },
      })),
    } as unknown as ReplBridge;

    const weightUpdate = brainSlotTools.find((tool) => tool.name === 'brain_slot_weight_update');
    expect(weightUpdate?.inputSchema.required).toEqual([
      'workspacePath',
      'roleWeightsJson',
      'reasonText',
      'aotApproved',
      'adminApproved',
      'safetyGatesPassed',
    ]);

    const args = {
      workspacePath: 'F:\\GitHub\\McpServer',
      roleWeightsJson: '{"Creativity":1.2}',
      reasonText: 'approved',
      aotApproved: true,
      adminApproved: true,
      safetyGatesPassed: true,
    };
    const result = await handleBrainSlotTool('brain_slot_weight_update', args, bridge);

    expect(bridge.invoke).toHaveBeenCalledWith('brain_slot_weight_update', args);
    expect(result).toEqual({ result: { status: 'committed' } });
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('brain_slot_orchestrate bridge failures create failsafe files', async () => {
    const { handleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    const bridge = {
      invoke: jest.fn(async () => {
        throw new Error('offline');
      }),
    } as unknown as ReplBridge;

    await expect(handleBrainSlotTool('brain_slot_orchestrate', {
      workspacePath: 'F:\\GitHub\\McpServer',
      input: 'decide',
    }, bridge)).rejects.toThrow(/offline Local failsafe saved:/);
    expect(fs.readdirSync(failsafeDir).length).toBe(1);
  });

  test('brain_slot_invoke reports bridge failures with failsafe path', async () => {
    const { handleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    const bridge = {
      invoke: jest.fn(async () => {
        throw new Error('offline');
      }),
    } as unknown as ReplBridge;

    await expect(handleBrainSlotTool('brain_slot_invoke', {
      workspacePath: 'F:\\GitHub\\McpServer',
      slotId: 'curiosity-main',
      input: 'find gaps',
    }, bridge)).rejects.toThrow(/offline Local failsafe saved:/);
    expect(fs.readdirSync(failsafeDir).length).toBe(1);
  });

  test('brain_slot_status read failures do not create failsafe files', async () => {
    const { handleBrainSlotTool } = await import('../src/tools/brain-slots.js');
    const bridge = {
      invoke: jest.fn(async () => {
        throw 'transport unavailable';
      }),
    } as unknown as ReplBridge;

    await expect(handleBrainSlotTool('brain_slot_status', {
      workspacePath: 'F:\\GitHub\\McpServer',
    }, bridge)).rejects.toThrow('transport unavailable');
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });
});
