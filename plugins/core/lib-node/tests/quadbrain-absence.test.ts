/**
 * Absence coverage for the QuadBrain removal from the shared Node plugin core.
 *
 * Ruling: nothing about QuadBrain may be exposed to the agent plugins at all.
 * Not gated, not identity-filtered: absent. QuadBrain stays reachable only as
 * the OpenAI-compatible model endpoint that QBAgent calls directly, so the
 * shared plugin core must not carry brain-slot tool descriptors, dispatch
 * branches, or public re-exports.
 *
 * Fixtures: the in-memory FakeBridge below (no network, no marker bootstrap)
 * plus the real published surface of ../src/index.js.
 *
 * Validates: the agent-facing plugin core exposes no 'brain_slot*' tool, the
 * dispatcher treats every such name as an unknown tool, and the package index
 * re-exports nothing brain-slot shaped.
 */
import * as coreIndex from '../src/index.js';
import { createMcpServerPluginCore, allToolDescriptors } from '../src/index.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';

/** Minimal in-memory ReplBridge stand-in: records calls, never touches a transport. */
class FakeBridge {
  /** Methods the core attempted to send over the bridge. */
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];

  /** Records the invocation and returns a benign success payload. */
  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    return { type: 'result', payload: { ok: true } };
  }

  /** No-op close: nothing is held open. */
  async close(): Promise<void> {
    /* no-op */
  }
}

/** Every brain-slot tool name the core used to publish, plus the family prefix probe. */
const removedBrainSlotToolNames = [
  'brain_slot_list',
  'brain_slot_get',
  'brain_slot_upsert',
  'brain_slot_delete',
  'brain_slot_enable',
  'brain_slot_disable',
  'brain_slot_status',
  'brain_slot_invoke',
  'brain_slot_orchestrate',
  'brain_slot_aot_reconcile',
  'brain_slot_weight_update',
];

function newCore(fake: FakeBridge) {
  return createMcpServerPluginCore({
    agentName: 'Cline',
    pluginId: 'cline-v2',
    bridge: fake as unknown as ReplBridge,
    workspacePath: 'F:\\GitHub\\McpServer',
    autoBootstrap: false,
    autoFlushCache: false,
  });
}

describe('QuadBrain absence in the shared plugin core', () => {
  test('allToolDescriptors publishes no descriptor whose name starts with brain_slot', () => {
    const offenders = allToolDescriptors
      .map((tool) => tool.name)
      .filter((name) => name.startsWith('brain_slot'));
    expect(offenders).toEqual([]);
  });

  test('no tool descriptor mentions brain slots in its name or description', () => {
    const offenders = allToolDescriptors.filter(
      (tool) => /brain[\s_-]?slot|quad[\s_-]?brain/i.test(`${tool.name} ${tool.description}`),
    );
    expect(offenders.map((tool) => tool.name)).toEqual([]);
  });

  test('dispatchTool rejects every removed brain_slot tool name as an unknown tool', async () => {
    for (const name of removedBrainSlotToolNames) {
      const fake = new FakeBridge();
      const context = newCore(fake);
      await expect(
        context.dispatchTool(name, { workspacePath: 'F:\\GitHub\\McpServer' }),
      ).rejects.toThrow(new RegExp(`Unknown tool: ${name}`));
      // Nothing reached the transport: the name is not routed anywhere.
      expect(fake.calls).toEqual([]);
    }
  });

  test('the package index re-exports nothing brain-slot shaped', () => {
    const exported = Object.keys(coreIndex).filter((key) => /brainslot|quadbrain/i.test(key));
    expect(exported).toEqual([]);
  });
});
