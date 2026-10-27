/** TEST-QB-005: independently configured hosts must never share audit state or ambient credentials. */
import { HostContext } from '../src/runtime/host-context.js';
import * as markers from '../src/discovery/marker-resolver.js';
import { ReplBridge, type ReplResponse } from '../src/transport/repl-bridge.js';
import { tmpdir } from 'os';
import { join } from 'path';

jest.mock('../src/cache/cache-manager.js', () => ({
  cacheWrite: jest.fn(async () => '/isolated/audit.yaml'),
  cacheDelete: jest.fn(async () => undefined),
  cacheFlush: jest.fn(async () => ({ flushed: 0, failed: 0, pending: 0 })),
}));

class RecordingBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  configureContext = jest.fn();
  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params: params ? structuredClone(params) : undefined });
    return { type: 'result', payload: { ok: true } };
  }
}

describe('TEST-QB-005 host isolation', () => {
  const environment = { ...process.env };
  afterEach(() => {
    jest.restoreAllMocks();
    process.env = { ...environment };
  });

  test('interleaved hosts retain their own session, request and action counters', async () => {
    const a = new RecordingBridge();
    const b = new RecordingBridge();
    const alpha = new HostContext({ agentName: 'Alpha', workspacePath: '/alpha', bridge: a as unknown as ReplBridge });
    const beta = new HostContext({ agentName: 'Beta', workspacePath: '/beta', bridge: b as unknown as ReplBridge });
    await alpha.startSession({ prompt: 'alpha request' });
    await beta.startSession({ prompt: 'beta request' });
    await alpha.appendToolAction({ toolName: 'alpha_tool' }, 'completed');
    await beta.appendToolAction({ toolName: 'beta_tool' }, 'completed');
    for (const [bridge, agent, title] of [[a, 'Alpha', 'alpha request'], [b, 'Beta', 'beta request']] as const) {
      const payload = bridge.calls.at(-1)!.params as { agent: string; sessionId: string; turn: { queryTitle: string; actions: Array<{ order: number; description: string }> } };
      expect(payload.agent).toBe(agent);
      expect(payload.sessionId.startsWith(agent + '-')).toBe(true);
      expect(payload.turn.queryTitle).toBe(title);
      expect(payload.turn.actions).toHaveLength(1);
      expect(payload.turn.actions[0].order).toBe(1);
      expect(payload.turn.actions[0].description).toContain(agent);
    }
  });

  test('constructing and bootstrapping hosts leaves the parent environment unchanged', async () => {
    const marker = { markerFile: '/alpha/AGENTS-README-FIRST.yaml', baseUrl: 'https://alpha.invalid', apiKey: 'synthetic-alpha-key', workspace: 'alpha', workspacePath: '/alpha', port: '443' };
    jest.spyOn(markers, 'fullBootstrap').mockResolvedValue(marker);
    const bridge = new RecordingBridge();
    const before = { ...process.env };
    const context = new HostContext({ agentName: 'Alpha', pluginId: 'test-alpha', workspacePath: '/alpha', bridge: bridge as unknown as ReplBridge });
    await context.bootstrap();
    expect(process.env).toEqual(before);
    expect(bridge.configureContext).toHaveBeenCalledWith(marker);
  });

  test('host identity is a snapshot, not a mutable caller-owned config', () => {
    const config = { agentName: 'Alpha', pluginId: 'alpha' };
    const context = new HostContext(config);
    config.agentName = 'Beta';
    config.pluginId = 'beta';
    expect(context.agentName).toBe('Alpha');
    expect(context.pluginId).toBe('alpha');
  });

  const governedConfig = { governed: true, agentName: 'ClineQb', pluginId: 'cline-qb', workspacePath: join(tmpdir(), 'qb-owned-workspace') };

  test.each(['agentName', 'pluginId', 'workspacePath'] as const)('governed hosts require explicit %s', (key) => {
    process.env.PLUGIN_AGENT_NAME = 'Foreign';
    process.env.PLUGIN_TAG = 'foreign';
    process.env.MCP_WORKSPACE_PATH = '/foreign';
    expect(() => new HostContext({ ...governedConfig, [key]: undefined })).toThrow(/governed/i);
  });

  test.each(['autoBootstrap', 'autoFlushCache'] as const)('governed hosts cannot disable %s', (key) => {
    expect(() => new HostContext({ ...governedConfig, [key]: false })).toThrow(/governed/i);
  });

  test('a bridge cannot be shared between host contexts', () => {
    const bridge = new ReplBridge(governedConfig);
    new HostContext({ ...governedConfig, bridge });
    expect(() => new HostContext({ ...governedConfig, bridge })).toThrow(/bridge.*owned|bridge.*shared/i);
  });

  test('a governed host rejects an ungoverned transport', () => {
    expect(() => new HostContext({ ...governedConfig, bridge: new ReplBridge() })).toThrow(/governed/i);
  });

  test.each(['agentName', 'pluginId', 'workspacePath'] as const)('a governed host rejects a bridge with different %s', (key) => {
    const bridge = new ReplBridge({ ...governedConfig, [key]: key === 'workspacePath' ? join(tmpdir(), 'foreign') : 'Foreign' });
    expect(() => new HostContext({ ...governedConfig, bridge })).toThrow(/binding/i);
  });

  test('a governed workspace cannot be changed through runtime context or setup setters', () => {
    const context = new HostContext(governedConfig);
    const foreign = join(tmpdir(), 'qb-foreign-workspace');
    expect(() => context.setWorkspacePath(foreign)).toThrow(/workspace/i);
    expect(() => context.resolveWorkspacePath({ workspacePath: foreign })).toThrow(/workspace/i);
    expect(context.workspacePath).toBe(governedConfig.workspacePath);
  });
});
