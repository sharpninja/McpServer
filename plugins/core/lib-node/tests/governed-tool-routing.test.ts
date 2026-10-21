// SPDX-License-Identifier: Apache-2.0
import { resolve } from 'path';
import { ReplBridge, type ReplResponse } from '../src/transport/repl-bridge.js';
import { handleTodoTool } from '../src/tools/todo.js';
import { handleGraphragTool } from '../src/tools/graphrag.js';
import { handleRequirementsTool } from '../src/tools/requirements.js';
import { handleMemoryTool } from '../src/tools/memory.js';
import { handleUseCaseTool } from '../src/tools/usecase.js';
import { handleWorkspaceTool } from '../src/tools/workspace.js';
import { fullBootstrap } from '../src/discovery/marker-resolver.js';
import { cacheWrite, cacheDelete } from '../src/cache/cache-manager.js';

jest.mock('../src/discovery/marker-resolver.js', () => ({ fullBootstrap: jest.fn() }));
jest.mock('../src/cache/cache-manager.js', () => ({ cacheWrite: jest.fn(async () => '/fixture/foreign-cache.yaml'), cacheDelete: jest.fn(async () => undefined) }));

const envKeys = ['MCPSERVER_API_KEY', 'MCPSERVER_BASE_URL', 'MCPSERVER_WORKSPACE_PATH', 'MCP_WORKSPACE_PATH', 'MCPSERVER_WORKSPACE', 'MCPSERVER_FAILSAFE_DIR'];
const workspaceA = resolve('/fixture/workspace-a');
const workspaceB = resolve('/fixture/workspace-b');
const marker = (workspacePath: string) => ({ workspacePath, workspace: workspacePath, apiKey: 'fixture-key', baseUrl: 'https://bound.invalid', markerFile: resolve(workspacePath, 'AGENTS-README-FIRST.yaml'), port: '443' });
function bridge(workspacePath = workspaceA, configured = true) {
  const result = new ReplBridge({ governed: true, workspacePath, agentName: 'ClineQb', pluginId: 'test', command: '/fixture/pinned-repl' });
  if (configured) result.configureContext(marker(workspacePath));
  return result;
}
const reply = (origin = 'bound'): ReplResponse => ({ type: 'result', payload: { result: { origin, items: [], contentBase64: 'UEsDBA==', contentType: 'application/zip', fileName: 'wiki.zip' } } });

describe('TEST-QB-004/005: governed tools use their bound workspace only', () => {
  let saved: (string | undefined)[];
  let originalFetch: typeof fetch;
  beforeEach(() => {
    saved = envKeys.map(key => process.env[key]); originalFetch = globalThis.fetch;
    process.env.MCPSERVER_API_KEY = 'foreign-fixture-key';
    process.env.MCPSERVER_BASE_URL = 'https://foreign.invalid';
    process.env.MCPSERVER_WORKSPACE_PATH = workspaceB;
    process.env.MCP_WORKSPACE_PATH = workspaceB;
    process.env.MCPSERVER_FAILSAFE_DIR = '/fixture/foreign-cache';
    globalThis.fetch = jest.fn().mockResolvedValue(new Response(JSON.stringify({ origin: 'foreign' }), { status: 200, headers: { 'content-type': 'application/json' } }));
    jest.mocked(fullBootstrap).mockImplementation(async workspacePath => marker(workspacePath ?? workspaceA));
    jest.mocked(cacheWrite).mockClear(); jest.mocked(cacheDelete).mockClear(); jest.mocked(fullBootstrap).mockClear();
  });
  afterEach(() => {
    envKeys.forEach((key, index) => { if (saved[index] === undefined) delete process.env[key]; else process.env[key] = saved[index]; });
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });
  const reads: [string, (b: ReplBridge) => Promise<unknown>][] = [
    ['todo', b => handleTodoTool('todo_query', {}, b)],
    ['graphrag', b => handleGraphragTool('graphrag_doc_list', {}, b)],
    ['wiki', b => handleRequirementsTool('req_generate_document', { docType: 'all', format: 'wiki' }, b)],
  ];
  test.each(reads)('%s read ignores foreign ambient credentials and uses its bound bridge', async (_name, invoke) => {
    const a = bridge(workspaceA), b = bridge(workspaceB);
    const callA = jest.spyOn(a, 'invoke').mockResolvedValue(reply('A'));
    const callB = jest.spyOn(b, 'invoke').mockResolvedValue(reply('B'));
    const [first, second] = await Promise.all([invoke(a), invoke(b)]);
    expect(first).toMatchObject({ result: { origin: 'A' } });
    expect(second).toMatchObject({ result: { origin: 'B' } });
    expect(callA).toHaveBeenCalledTimes(1); expect(callB).toHaveBeenCalledTimes(1);
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
  test.each(reads)('%s failed read never probes an ambient endpoint', async (_name, invoke) => {
    const b = bridge();
    jest.spyOn(b, 'invoke').mockRejectedValue(new Error('bound endpoint unavailable'));
    await expect(invoke(b)).rejects.toThrow('bound endpoint unavailable');
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
  test.each([null, {}, undefined])('missing wiki content fails without alternate calls: %j', result => {
    const b = bridge();
    const call = jest.spyOn(b, 'invoke').mockResolvedValue({ type: 'result', payload: { result } });
    return expect(handleRequirementsTool('req_generate_document', { format: 'wiki' }, b)).rejects.toThrow().then(() => {
      expect(call).toHaveBeenCalledTimes(1); expect(globalThis.fetch).not.toHaveBeenCalled();
    });
  });
  const mutations: [string, (b: ReplBridge) => Promise<unknown>][] = [
    ['todo', b => handleTodoTool('todo_update', { id: 'TEST' }, b)],
    ['graphrag', b => handleGraphragTool('graphrag_entity_create', { name: 'test' }, b)],
    ['requirements', b => handleRequirementsTool('req_update_fr', { id: 'FR-TEST-001' }, b)],
    ['memory', b => handleMemoryTool('memory_add', { category: 'test', text: 'fixture' }, b)],
    ['usecase', b => handleUseCaseTool('usecase_create', { title: 'fixture' }, b)],
  ];
  test.each(mutations)('%s mutation never enters the foreign legacy command queue', async (_name, invoke) => {
    const b = bridge();
    const call = jest.spyOn(b, 'invoke').mockResolvedValue(reply());
    await invoke(b);
    expect(call).toHaveBeenCalledTimes(1);
    expect(cacheWrite).not.toHaveBeenCalled(); expect(cacheDelete).not.toHaveBeenCalled();
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
  test('workspace_ensure uses verified identity without bootstrap, registration or global environment writes', async () => {
    const b = bridge(); const call = jest.spyOn(b, 'invoke');
    const envBefore = envKeys.map(key => process.env[key]);
    const result = await handleWorkspaceTool('workspace_ensure', {}, b, workspaceA);
    expect(result).toMatchObject({ trusted: true, workspacePath: workspaceA, created: false });
    expect(JSON.stringify(result)).not.toContain('fixture-key');
    expect(envKeys.map(key => process.env[key])).toEqual(envBefore);
    expect(fullBootstrap).not.toHaveBeenCalled(); expect(call).not.toHaveBeenCalled();
  });
  test.each([{ workspacePath: workspaceB }, { path: workspaceB }, { workspacePath: workspaceA, path: workspaceB }])('workspace_ensure rejects foreign selector %j before any probing', async args => {
    const b = bridge(); const call = jest.spyOn(b, 'invoke');
    await expect(handleWorkspaceTool('workspace_ensure', args, b, workspaceA)).rejects.toThrow();
    expect(fullBootstrap).not.toHaveBeenCalled(); expect(call).not.toHaveBeenCalled();
  });
  test('missing verified workspace is blocked without registration or HTTP fallback', async () => {
    const b = bridge(workspaceA, false); const call = jest.spyOn(b, 'invoke');
    await expect(handleWorkspaceTool('workspace_ensure', {}, b, workspaceA)).rejects.toThrow();
    expect(fullBootstrap).not.toHaveBeenCalled(); expect(call).not.toHaveBeenCalled();
    expect(globalThis.fetch).not.toHaveBeenCalled();
  });
});