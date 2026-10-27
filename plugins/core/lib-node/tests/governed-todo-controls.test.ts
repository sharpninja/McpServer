// SPDX-License-Identifier: Apache-2.0
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { handleTodoTool } from '../src/tools/todo.js';
import { ReplBridge } from '../src/transport/repl-bridge.js';
import { classifyOperationFailure } from '../src/runtime/operation-failure.js';

describe('TEST-QB-002/005: governed contexts cannot use global legacy tracking controls', () => {
  const keys = ['MCPSERVER_INTERNAL_TODO_STATE_FILE', 'MCPSERVER_INTERNAL_TODO'];
  let old: (string | undefined)[];
  let dir: string;
  let state: string;
  beforeEach(() => {
    old = keys.map(key => process.env[key]);
    dir = fs.mkdtempSync(path.join(os.tmpdir(), 'governed-todo-'));
    state = path.join(dir, 'foreign-state.yaml');
    fs.writeFileSync(state, 'enabled: false\n');
    process.env.MCPSERVER_INTERNAL_TODO_STATE_FILE = state;
    process.env.MCPSERVER_INTERNAL_TODO = 'false';
  });
  afterEach(() => {
    keys.forEach((key, i) => { if (old[i] === undefined) delete process.env[key]; else process.env[key] = old[i]; });
    fs.rmSync(dir, { recursive: true, force: true });
    jest.restoreAllMocks();
  });
  test.each([
    ['todo_internal_status', {}],
    ['todo_internal_enable', {}],
    ['todo_internal_disable', {}],
    ['todo_internal_tracking', {}],
    ['todo_internal_tracking', { enabled: false }],
    ['todo_internal_tracking', { enabled: true }],
    ['todo_internal_tracking', { request: { mode: 'local' } }],
    ['todo_internal_tracking', { enabled: 'invalid' }],
  ] as [string, Record<string, unknown>][])('%s %j rejects before reading or modifying another context', async (name, args) => {
    const bridge = new ReplBridge({ governed: true, workspacePath: dir, agentName: 'ClineQb', pluginId: 'test', command: '/fixture/repl' });
    const invoke = jest.spyOn(bridge, 'invoke');
    const read = jest.spyOn(jest.requireActual<typeof import('fs')>('fs'), 'readFileSync');
    const write = jest.spyOn(jest.requireActual<typeof import('fs')>('fs'), 'writeFileSync');
    let caught: unknown;
    try { await handleTodoTool(name, args, bridge); } catch (error) { caught = error; }
    expect(classifyOperationFailure(caught)).toMatchObject({ detailCode: 'governed_control_unsupported', retryable: false });
    expect(read).not.toHaveBeenCalled(); expect(write).not.toHaveBeenCalled(); expect(invoke).not.toHaveBeenCalled();
    read.mockRestore(); write.mockRestore();
    expect(fs.readFileSync(state, 'utf8')).toBe('enabled: false\n');
    expect(process.env.MCPSERVER_INTERNAL_TODO).toBe('false');
  });
  test('legacy tracking controls remain usable outside governed mode', async () => {
    delete process.env.MCPSERVER_INTERNAL_TODO;
    const bridge = new ReplBridge();
    await expect(handleTodoTool('todo_internal_enable', {}, bridge)).resolves.toMatchObject({ result: { enabled: true, source: 'cache' } });
    await expect(handleTodoTool('todo_internal_disable', {}, bridge)).resolves.toMatchObject({ result: { enabled: false, source: 'cache' } });
  });
});