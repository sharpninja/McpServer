/** TEST-QB-003/005: governed transports use pinned executables and private workspace credentials. */
import { spawn, type ChildProcess } from 'child_process';
import { EventEmitter } from 'events';
import { mkdtempSync, rmSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { join } from 'path';
import { PassThrough } from 'stream';
import { ReplBridge } from '../src/transport/repl-bridge.js';
import { dispatchSessionTool, SessionShim } from '../src/tools/session-shim.js';

jest.mock('child_process', () => ({ spawn: jest.fn() }));
const spawnMock = spawn as jest.MockedFunction<typeof spawn>;

describe('governed REPL launch', () => {
  const environment = { ...process.env };
  let workspace: string;
  let command: string;
  beforeEach(() => {
    workspace = mkdtempSync(join(tmpdir(), 'qb-repl-'));
    command = join(workspace, 'pinned-repl');
    writeFileSync(command, 'test fixture; spawn is mocked');
    process.env.MCPSERVER_REPL_BIN = '/untrusted/override';
    process.env.MCPSERVER_REPL_ARGS = '--agent WrongAgent --untrusted-argument';
    process.env.MCPSERVER_API_KEY = 'other-context-key';
    spawnMock.mockReset();
    spawnMock.mockImplementation(() => Object.assign(new EventEmitter(), {
      stdin: new PassThrough(), stdout: new PassThrough(), stderr: new PassThrough(),
      exitCode: null, killed: false, kill: jest.fn(),
    }) as unknown as ChildProcess);
  });
  afterEach(() => {
    process.env = { ...environment };
    rmSync(workspace, { recursive: true, force: true });
  });

  function bridge() {
    const options = { governed: true, command, workspacePath: workspace, agentName: 'ClineQb', pluginId: 'cline-qb' };
    return new ReplBridge(options);
  }
  function bind(instance: ReplBridge, apiKey = 'own-context-key') {
    instance.configureContext({ markerFile: join(workspace, 'AGENTS-README-FIRST.yaml'), baseUrl: 'https://own.invalid', apiKey, workspace: 'own', workspacePath: workspace, port: '443' });
  }

  test('uses only the pinned executable and host-owned arguments', async () => {
    const instance = bridge();
    bind(instance);
    const before = { ...process.env };
    await instance.ensure();
    expect(spawnMock).toHaveBeenCalledWith(command, ['--agent-stdio', '--agent', 'ClineQb'], expect.objectContaining({
      cwd: workspace,
      env: expect.objectContaining({ MCPSERVER_API_KEY: 'own-context-key', MCPSERVER_BASE_URL: 'https://own.invalid', MCP_WORKSPACE_PATH: workspace }),
    }));
    expect(process.env).toEqual(before);
  });

  test('refuses to start without an explicitly bound workspace context', async () => {
    await expect(bridge().ensure()).rejects.toThrow(/context/i);
    expect(spawnMock).not.toHaveBeenCalled();
  });

  test('refuses a relative executable instead of consulting PATH', async () => {
    const options = { governed: true, command: 'qbrain-ai-repl', workspacePath: workspace, agentName: 'ClineQb' };
    const instance = new ReplBridge(options);
    bind(instance);
    await expect(instance.ensure()).rejects.toThrow(/absolute|pinned/i);
    expect(spawnMock).not.toHaveBeenCalled();
  });

  test('two transports launch with different credentials without changing ambient values', async () => {
    const a = bridge();
    const b = bridge();
    bind(a, 'alpha-key');
    bind(b, 'beta-key');
    await Promise.all([a.ensure(), b.ensure()]);
    expect(spawnMock.mock.calls[0][2]?.env?.MCPSERVER_API_KEY).toBe('alpha-key');
    expect(spawnMock.mock.calls[1][2]?.env?.MCPSERVER_API_KEY).toBe('beta-key');
    expect(process.env.MCPSERVER_API_KEY).toBe('other-context-key');
  });

  test('passes only runtime essentials and context-owned values to the governed child', async () => {
    const forbidden = ['MCPSERVER_REPL_ARGS', 'MCPSERVER_REPL_BIN', 'MCP_SERVER_URL',
      'MCP_WORKSPACE_START_DIR', 'MCPSERVER_FAILSAFE_DIR', 'QBRAIN_API_KEY',
      'ANTHROPIC_API_KEY', 'OPENAI_API_KEY', 'UNRELATED_PRIVATE_TOKEN', 'NODE_OPTIONS', 'LD_PRELOAD'];
    for (const key of forbidden) process.env[key] = 'foreign-context-value';
    process.env.HTTPS_PROXY = 'http://proxy.invalid:3128';
    process.env.SSL_CERT_FILE = '/runtime/cert.pem';
    const before = { ...process.env };
    const instance = bridge();
    bind(instance);
    await instance.ensure();
    const child = spawnMock.mock.calls[0][2]?.env;
    for (const key of forbidden) expect(child).not.toHaveProperty(key);
    expect(child).toMatchObject({ HTTPS_PROXY: process.env.HTTPS_PROXY, SSL_CERT_FILE: process.env.SSL_CERT_FILE,
      PLUGIN_AGENT_NAME: 'ClineQb', PLUGIN_AGENT_DEFAULT: 'ClineQb', MCP_AGENT_NAME: 'ClineQb',
      MCP_AGENT_ID: 'ClineQb', MCP_SESSION_AGENT: 'ClineQb', CT2R_SOURCE_TYPE: 'ClineQb',
      PLUGIN_TAG: 'cline-qb', MCP_SESSION_MODEL: 'cline-qb', CT2R_MODEL: 'cline-qb', CT2R_TAGS: 'cline-qb' });
    expect(process.env).toEqual(before);
  });

  test.each(['agentName', 'pluginId'] as const)('rejects missing explicit %s instead of inheriting identity', async (key) => {
    const options = { governed: true, command, workspacePath: workspace, agentName: 'ClineQb', pluginId: 'cline-qb' };
    const instance = new ReplBridge({ ...options, [key]: undefined });
    bind(instance);
    await expect(instance.ensure()).rejects.toThrow(/identity/i);
    expect(spawnMock).not.toHaveBeenCalled();
  });

  test('queries history through the bound transport even when ambient HTTP credentials exist', async () => {
    process.env.MCPSERVER_BASE_URL = 'https://foreign.invalid';
    process.env.MCP_WORKSPACE_PATH = '/foreign';
    const originalFetch = globalThis.fetch;
    const fetchMock = jest.fn().mockResolvedValue({ ok: true, text: async () => '{}', headers: new Headers() });
    globalThis.fetch = fetchMock;
    try {
      const instance = bridge();
      bind(instance);
      const expected = { type: 'result' as const, payload: { result: { sessions: [] } } };
      const invoke = jest.spyOn(instance, 'invoke').mockResolvedValue(expected);
      const args = { agent: 'ClineQb', limit: 10 };
      await expect(dispatchSessionTool(new SessionShim(), instance, 'session_query_history', args)).resolves.toEqual(expected);
      expect(invoke).toHaveBeenCalledWith('client.SessionLog.QueryAsync', args);
      expect(fetchMock).not.toHaveBeenCalled();
    } finally {
      globalThis.fetch = originalFetch;
    }
  });
});
