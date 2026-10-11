/**
 * 1.x alias coverage for the QBrainAi rename of the shared Node plugin core.
 *
 * Requirement: TR-MCP-QBRAIN-003 (package and tool migration): the renamed
 * npm package keeps its 1.x public names as deprecated aliases through 1.x.
 * The rename (6de989f3) dropped createMcpServerPluginCore, which the cline-v2
 * and opencode plugins import, so vendoring the renamed core broke their
 * builds (BUG-PLUGINCORE-001).
 *
 * Fixtures: the real published surface of ../src/index.js, read through a
 * namespace import so a missing export fails an assertion rather than the
 * compile, and the in-memory FakeBridge below (no network, no marker
 * bootstrap, no failsafe replay).
 *
 * Validates: createMcpServerPluginCore is exported, is the QBrainAi factory
 * itself, and builds a HostContext.
 */
import * as coreIndex from '../src/index.js';
import { createQBrainAiPluginCore, HostContext } from '../src/index.js';
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

/** Identity variables the core publishes; restored after each test so tests stay independent. */
const identityEnvKeys = ['PLUGIN_AGENT_NAME', 'PLUGIN_AGENT_DEFAULT', 'PLUGIN_TAG'] as const;

/** The legacy factory as the published index exposes it, or undefined when it is missing. */
function legacyFactory(): unknown {
  return (coreIndex as Record<string, unknown>)['createMcpServerPluginCore'];
}

describe('1.x aliases of the renamed plugin core (TR-MCP-QBRAIN-003, BUG-PLUGINCORE-001)', () => {
  let snapshot: Record<string, string | undefined>;

  beforeEach(() => {
    snapshot = Object.fromEntries(identityEnvKeys.map((key) => [key, process.env[key]]));
  });

  afterEach(() => {
    for (const key of identityEnvKeys) {
      if (snapshot[key] === undefined) {
        delete process.env[key];
      } else {
        process.env[key] = snapshot[key];
      }
    }
  });

  it('exports createMcpServerPluginCore as the QBrainAi factory itself', () => {
    expect(typeof legacyFactory()).toBe('function');
    expect(legacyFactory()).toBe(createQBrainAiPluginCore);
  });

  it('builds a HostContext through the 1.x factory name', () => {
    const factory = legacyFactory() as typeof createQBrainAiPluginCore;
    const context = factory({
      agentName: 'OpenCode',
      pluginId: 'opencode',
      bridge: new FakeBridge() as unknown as ReplBridge,
      workspacePath: '/workspace/alias-check',
      autoBootstrap: false,
      autoFlushCache: false,
    });
    expect(context).toBeInstanceOf(HostContext);
  });
});
