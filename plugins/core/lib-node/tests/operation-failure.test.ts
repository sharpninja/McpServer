// SPDX-License-Identifier: Apache-2.0
import { classifyOperationFailure, CoreOperationError, FINAL_OPERATION_CODES } from '../src/runtime/operation-failure.js';
import { CoreRuntimeError } from '../src/runtime/core-runtime-error.js';
import type { ReplBridge } from '../src/transport/repl-bridge.js';
import { handleTodoTool } from '../src/tools/todo.js';
import { handleMemoryTool } from '../src/tools/memory.js';
import { handleRequirementsTool } from '../src/tools/requirements.js';
import { handleGraphragTool } from '../src/tools/graphrag.js';
import { handleUseCaseTool } from '../src/tools/usecase.js';
import { invokeOrThrow } from '../src/tools/workspace.js';

describe('C9 structured failure propagation', () => {
  test.each([...FINAL_OPERATION_CODES, 'review_future_code', 'unknown_future_code'])('final %s overrides optimistic retry flag and HTTP 500', code => {
    const error = new CoreRuntimeError('session_failed', { code, retryable: true, status: 500 });
    expect(error).toMatchObject({ code: 'session_failed', detailCode: code, retryable: false, reportedRetryable: true });
  });
  test.each(['backend_unavailable', 'persistence_error', 'timeout'])('server transient %s requires explicit true', code => {
    expect(classifyOperationFailure({ code, retryable: true }).retryable).toBe(true);
    expect(classifyOperationFailure({ code }).retryable).toBe(false);
    expect(classifyOperationFailure({ code, retryable: false }).retryable).toBe(false);
  });
  test('nested native timeout survives harmless wrappers and the broad host category', () => {
    const native = Object.assign(new Error('native'), { code: 'ETIMEDOUT' });
    expect(new CoreRuntimeError('session_failed', new Error('wrapper', { cause: native })))
      .toMatchObject({ detailCode: 'ETIMEDOUT', retryable: true });
  });
  test.each([
    { code: 'ETIMEDOUT', retryable: 'true' },
    { code: 'ETIMEDOUT', reportedRetryable: 'true' },
    { code: 500, cause: { code: 'ETIMEDOUT' } },
    { code: 'bad code', cause: { code: 'ETIMEDOUT' } },
  ])('malformed classification stays final: %j', error => {
    expect(classifyOperationFailure(error).retryable).toBe(false);
  });
  test('cause cycles terminate without granting retry', () => {
    const error: { cause?: unknown } = {}; error.cause = error;
    expect(classifyOperationFailure(error)).toEqual({ detailCode: 'unknown_error', retryable: false });
  });
  test('typed public diagnostic does not include raw cause text', () => {
    const error = new CoreOperationError('persistence_error', true, new Error('fixture-secret'));
    expect(error.message).not.toContain('fixture-secret');
    expect(classifyOperationFailure(error)).toMatchObject({ detailCode: 'persistence_error', retryable: true });
  });

  const originalFetch = globalThis.fetch;
  beforeEach(() => { globalThis.fetch = undefined as unknown as typeof fetch; });
  afterEach(() => { globalThis.fetch = originalFetch; });
  const handlers: [string, (bridge: ReplBridge) => Promise<unknown>][] = [
    ['todo', bridge => handleTodoTool('todo_query', {}, bridge)],
    ['memory', bridge => handleMemoryTool('memory_list', {}, bridge)],
    ['requirements', bridge => handleRequirementsTool('req_list_fr', {}, bridge)],
    ['graphrag', bridge => handleGraphragTool('graphrag_doc_list', {}, bridge)],
    ['usecase', bridge => handleUseCaseTool('usecase_list', {}, bridge)],
    ['workspace', bridge => invokeOrThrow(bridge, 'client.Workspace.ListAsync', {})],
  ];
  test.each(handlers)('%s wrapper preserves structured server failure without retrying a final response', async (_name, invoke) => {
    const bridge = { governed: true, invoke: jest.fn().mockResolvedValue({ type: 'error', payload: { code: 'mint_collision_exhausted', retryable: false, message: 'final' } }) };
    const error = await invoke(bridge as unknown as ReplBridge).catch(error => error);
    expect(classifyOperationFailure(error)).toMatchObject({ detailCode: 'mint_collision_exhausted', retryable: false });
    expect(bridge.invoke).toHaveBeenCalledTimes(1);
  });
  test.each(handlers)('%s wrapper preserves transient transport cause', async (_name, invoke) => {
    const bridge = { governed: true, invoke: jest.fn().mockRejectedValue(Object.assign(new Error('timeout'), { code: 'ETIMEDOUT' })) };
    const error = await invoke(bridge as unknown as ReplBridge).catch(error => error);
    expect(classifyOperationFailure(error)).toMatchObject({ detailCode: 'ETIMEDOUT', retryable: true });
    expect(bridge.invoke).toHaveBeenCalledTimes(1);
  });
});