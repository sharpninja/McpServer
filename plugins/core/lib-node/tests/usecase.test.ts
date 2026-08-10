/**
 * Use Case plugin-core tools: descriptors, client.UseCases.* routing, mutation failsafe.
 */
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  usecaseTools,
  canHandleUseCaseTool,
  handleUseCaseTool,
  buildUseCaseParams,
} from '../src/tools/usecase.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  nextResponse: ReplResponse = { type: 'result', payload: { result: { ok: true } } };
  responses: ReplResponse[] = [];

  async invoke(method: string, params?: Record<string, unknown>): Promise<ReplResponse> {
    this.calls.push({ method, params });
    if (this.responses.length > 0) {
      return this.responses.shift()!;
    }
    return this.nextResponse;
  }
}

function asBridge(fake: FakeBridge): ReplBridge {
  return fake as unknown as ReplBridge;
}

function tool(name: string) {
  const found = usecaseTools.find((candidate) => candidate.name === name);
  if (!found) throw new Error(`Missing tool ${name}`);
  return found;
}

describe('usecase tool schemas', () => {
  test('exports core usecase_* tools matching server MCP surface', () => {
    const names = usecaseTools.map((t) => t.name);
    expect(names).toEqual(
      expect.arrayContaining([
        'usecase_list',
        'usecase_get',
        'usecase_create',
        'usecase_update',
        'usecase_delete',
        'usecase_link_fr',
        'usecase_from_fr',
        'usecase_diagram',
        'usecase_coverage',
        'usecase_set_approval',
        'usecase_set_product',
        'usecase_list_by_product',
      ]),
    );
  });

  test('usecase_create requires title', () => {
    const schema = tool('usecase_create').inputSchema as { required?: string[] };
    expect(schema.required).toContain('title');
  });

  test('usecase_set_approval status enum matches server', () => {
    const schema = tool('usecase_set_approval').inputSchema as {
      properties: { status: { enum?: string[] } };
    };
    expect(schema.properties.status.enum).toEqual(['Draft', 'Submitted', 'Approved', 'Rejected']);
  });
});

describe('buildUseCaseParams', () => {
  test('create wraps body in request for client.UseCases.CreateAsync', () => {
    expect(
      buildUseCaseParams('usecase_create', {
        title: 'Login',
        createBasicFlow: true,
        frId: 'FR-MCP-001',
      }),
    ).toEqual({
      request: {
        title: 'Login',
        createBasicFlow: true,
        frId: 'FR-MCP-001',
      },
    });
  });

  test('coverage has empty params', () => {
    expect(buildUseCaseParams('usecase_coverage', {})).toEqual({});
  });
});

describe('usecase tool handlers', () => {
  let failsafeDir: string;

  beforeEach(() => {
    failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-usecase-test-'));
    process.env.MCP_FAILSAFE_DIR = failsafeDir;
  });

  afterEach(() => {
    delete process.env.MCP_FAILSAFE_DIR;
    fs.rmSync(failsafeDir, { recursive: true, force: true });
  });

  test('canHandleUseCaseTool covers all names', () => {
    expect(canHandleUseCaseTool('usecase_list')).toBe(true);
    expect(canHandleUseCaseTool('usecase_coverage')).toBe(true);
    expect(canHandleUseCaseTool('usecase_set_approval')).toBe(true);
    expect(canHandleUseCaseTool('usecase_missing')).toBe(false);
  });

  test('usecase_list routes through client.UseCases.ListAsync', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: [{ useCaseId: 1, title: 'Login' }] },
    };

    const result = await handleUseCaseTool('usecase_list', { title: 'Login' }, asBridge(fake));

    expect(fake.calls[0]).toEqual({
      method: 'client.UseCases.ListAsync',
      params: { title: 'Login' },
    });
    expect(result).toEqual({ result: [{ useCaseId: 1, title: 'Login' }] });
  });

  test('usecase_coverage routes through client.UseCases.GetCoverageAsync with live DTO shape', async () => {
    const fake = new FakeBridge();
    const coverage = {
      totalUseCases: 1,
      totalFunctionalRequirements: 1,
      linkedUseCases: 1,
      linkedFunctionalRequirements: 1,
      useCasesWithoutRealizesLink: [],
      functionalRequirementsWithoutRealizesUseCase: [],
    };
    fake.nextResponse = { type: 'result', payload: { result: coverage } };

    const result = await handleUseCaseTool('usecase_coverage', {}, asBridge(fake));

    expect(fake.calls[0].method).toBe('client.UseCases.GetCoverageAsync');
    expect(result).toEqual({ result: coverage });
  });

  test('usecase_create routes CreateAsync with request body and clears failsafe', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: { useCaseId: 3, title: 'Create user', versionNumber: 1, approvalStatus: 'Draft' } },
    };

    const result = await handleUseCaseTool(
      'usecase_create',
      { title: 'Create user', createBasicFlow: true },
      asBridge(fake),
    );

    expect(fake.calls[0]).toEqual({
      method: 'client.UseCases.CreateAsync',
      params: { request: { title: 'Create user', createBasicFlow: true } },
    });
    expect(fake.calls.some((c) => c.method === 'workflow.sessionlog.appendActions')).toBe(true);
    expect(result.result).toMatchObject({ useCaseId: 3, approvalStatus: 'Draft' });
    expect(fs.readdirSync(failsafeDir)).toEqual([]);
  });

  test('usecase_set_approval posts status via SetApprovalAsync', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: { useCaseId: 3, approvalStatus: 'Approved', versionNumber: 2 } },
    };

    await handleUseCaseTool(
      'usecase_set_approval',
      { useCaseId: 3, status: 'Approved' },
      asBridge(fake),
    );

    expect(fake.calls[0]).toEqual({
      method: 'client.UseCases.SetApprovalAsync',
      params: { useCaseId: 3, request: { status: 'Approved' } },
    });
  });
});
