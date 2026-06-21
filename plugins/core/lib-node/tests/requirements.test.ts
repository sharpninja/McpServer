/**
 * Ported from mcpserver-cline-v2-plugin/tests/requirements.test.ts.
 * Imports map 1:1 onto the canonical core module layout. Covers the shared
 * batch schema (Patch C), the workflow-first / typed-fallback routing, the
 * acceptance-criteria threading, and the PowerShell-YAML / inline-JSON
 * record parsing.
 */
import { requirementsTools, handleRequirementsTool } from '../src/tools/requirements.js';
import type { ReplBridge, ReplResponse } from '../src/transport/repl-bridge.js';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

class FakeBridge {
  calls: Array<{ method: string; params?: Record<string, unknown> }> = [];
  nextResponse: ReplResponse = { type: 'result', payload: { ok: true } };
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
  const found = requirementsTools.find((candidate) => candidate.name === name);
  if (!found) throw new Error(`Missing tool ${name}`);
  return found;
}

describe('requirements tool schemas', () => {
  test('req_generate_document exposes wiki format and all docType', () => {
    const schema = tool('req_generate_document').inputSchema as unknown as {
      properties: {
        format: { enum: string[] };
        docType: { enum: string[] };
      };
    };

    expect(schema.properties.format.enum).toEqual(['markdown', 'yaml', 'wiki']);
    expect(schema.properties.docType.enum).toContain('all');
  });

  test('req_ingest_document exposes wiki source selection and document map fields', () => {
    const schema = tool('req_ingest_document').inputSchema as unknown as {
      properties: Record<string, unknown>;
      required?: string[];
    };

    expect(schema.properties).toHaveProperty('documents');
    expect(schema.properties).toHaveProperty('sourceFormat');
    expect(schema.properties).toHaveProperty('preferredWikiFormat');
    expect(schema.required).toBeUndefined();
  });

  test('FR/TR/TEST create+update tools expose acceptanceCriteria array schema', () => {
    const toolNames = [
      'req_create_fr',
      'req_update_fr',
      'req_create_tr',
      'req_update_tr',
      'req_create_test',
      'req_update_test',
    ];
    for (const name of toolNames) {
      const schema = tool(name).inputSchema as unknown as {
        properties: Record<string, { type?: string; items?: { type?: string; required?: string[] } }>;
      };
      const ac = schema.properties.acceptanceCriteria;
      expect(ac).toBeDefined();
      expect(ac.type).toBe('array');
      expect(ac.items?.type).toBe('object');
      expect(ac.items?.required).toEqual(['text']);
    }
  });
});

describe('handleRequirementsTool', () => {
  test('routes wiki generate arguments through workflow.requirements.generateDocument', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: {
        result: {
          contentBase64: 'UEsDBA==',
          contentType: 'application/zip',
          fileName: 'requirements-wiki-documents.zip',
        },
      },
    };
    await handleRequirementsTool(
      'req_generate_document',
      { format: 'wiki', docType: 'all' },
      asBridge(fake),
    );

    expect(fake.calls).toEqual([
      {
        method: 'workflow.requirements.generateDocument',
        params: { format: 'wiki', docType: 'all' },
      },
    ]);
  });

  test('routes wiki ingest documents through workflow.requirements.ingestDocument', async () => {
    const fake = new FakeBridge();
    const params = {
      format: 'wiki',
      sourceFormat: 'wiki',
      preferredWikiFormat: 'github',
      documents: {
        'github/Functional-Requirements.md': {
          content: '# Functional Requirements (MCP Server)',
          lastModifiedUtc: '2026-05-08T12:00:00Z',
        },
      },
    };

    await handleRequirementsTool('req_ingest_document', params, asBridge(fake));

    expect(fake.calls).toEqual([
      {
        method: 'workflow.requirements.ingestDocument',
        params,
      },
    ]);
  });

  test('falls back to typed list when workflow requirements route is missing', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      {
        type: 'error',
        payload: { code: 'method_not_found', message: 'not routed' },
      },
      {
        type: 'result',
        payload: { result: { items: [], totalCount: 0 } },
      },
    ];

    await handleRequirementsTool('req_list_fr', {}, asBridge(fake));

    expect(fake.calls).toEqual([
      { method: 'workflow.requirements.listFr', params: {} },
      { method: 'client.Requirements.ListFrAsync', params: {} },
    ]);
  });

  test('keeps local failsafe for mutating requirements when all routes fail', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      {
        type: 'error',
        payload: { code: 'offline', message: 'workflow unavailable' },
      },
      {
        type: 'error',
        payload: { code: 'offline', message: 'typed unavailable' },
      },
    ];
    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-req-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    try {
      await expect(
        handleRequirementsTool(
          'req_create_fr',
          {
            id: 'FR-FAILSAFE-001',
            title: 'Failsafe requirement',
            description: 'Preserve failed requirement writes',
            priority: 'high',
            area: 'MCP',
          },
          asBridge(fake),
        ),
      ).rejects.toThrow(/Local failsafe saved:/);

      const files = fs.readdirSync(failsafeDir).filter((file) => file.endsWith('.yaml'));
      expect(files).toHaveLength(1);
      const content = fs.readFileSync(path.join(failsafeDir, files[0]), 'utf8');
      expect(content).toContain('workflow.requirements.createFr');
      expect(content).toContain('FR-FAILSAFE-001');
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });

  test('falls back to typed wiki generate when workflow rejects wiki format', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      {
        type: 'error',
        payload: { code: 'invalid_argument', message: 'Invalid format: wiki' },
      },
      {
        type: 'result',
        payload: {
          result: {
            success: true,
            format: 'wiki',
            docType: 'all',
            outputRoot: 'F:\\GitHub\\TruckMate\\docs\\Project\\wiki',
          },
        },
      },
    ];

    await handleRequirementsTool(
      'req_generate_document',
      { format: 'wiki', docType: 'all' },
      asBridge(fake),
    );

    expect(fake.calls).toEqual([
      {
        method: 'workflow.requirements.generateDocument',
        params: { format: 'wiki', docType: 'all' },
      },
      {
        method: 'client.Requirements.GenerateAsync',
        params: { doc: 'all', format: 'wiki' },
      },
    ]);
  });

  test('threads acceptanceCriteria into typed create FR request when workflow returns empty', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      { type: 'result', payload: { result: {} } },
      { type: 'result', payload: { result: { id: 'FR-AC-001' } } },
    ];

    const acceptanceCriteria = [
      { text: 'Server returns 200', isSatisfied: false },
      { id: 'AC-2', text: 'Logs the request', evidence: 'see log line' },
    ];

    await handleRequirementsTool(
      'req_create_fr',
      {
        id: 'FR-AC-001',
        title: 'AC FR',
        description: 'AC body',
        priority: 'high',
        area: 'MCP',
        acceptanceCriteria,
      },
      asBridge(fake),
    );

    expect(fake.calls).toHaveLength(2);
    expect(fake.calls[1].method).toBe('client.Requirements.CreateFrAsync');
    const typedParams = fake.calls[1].params as { request: { acceptanceCriteria: unknown } };
    expect(typedParams.request.acceptanceCriteria).toEqual(acceptanceCriteria);
  });

  test('threads acceptanceCriteria into typed update TEST request when workflow returns empty', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      { type: 'result', payload: { result: {} } },
      { type: 'result', payload: { result: { id: 'TEST-AC-001' } } },
    ];

    const acceptanceCriteria = [{ text: 'Coverage > 80%' }];

    await handleRequirementsTool(
      'req_update_test',
      {
        id: 'TEST-AC-001',
        description: 'Updated condition',
        acceptanceCriteria,
      },
      asBridge(fake),
    );

    expect(fake.calls).toHaveLength(2);
    expect(fake.calls[1].method).toBe('client.Requirements.UpdateTestAsync');
    const typedParams = fake.calls[1].params as { id: string; request: { acceptanceCriteria: unknown } };
    expect(typedParams.id).toBe('TEST-AC-001');
    expect(typedParams.request.acceptanceCriteria).toEqual(acceptanceCriteria);
  });

  test('threads caller acceptanceCriteria into criteria-only typed update FR request', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      { type: 'result', payload: { result: {} } },
      { type: 'result', payload: { result: { id: 'FR-AC-201' } } },
    ];

    const acceptanceCriteria = [{ id: 'caller-ac-1', text: 'caller criterion text', isSatisfied: false }];

    await handleRequirementsTool(
      'req_update_fr',
      {
        id: 'FR-AC-201',
        acceptanceCriteria,
      },
      asBridge(fake),
    );

    expect(fake.calls).toHaveLength(2);
    expect(fake.calls[1].method).toBe('client.Requirements.UpdateFrAsync');
    const typedParams = fake.calls[1].params as { id: string; request: { acceptanceCriteria: unknown } };
    expect(typedParams.id).toBe('FR-AC-201');
    expect(typedParams.request.acceptanceCriteria).toEqual(acceptanceCriteria);
  });

  test('fails when supplied acceptanceCriteria returns empty', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      { type: 'result', payload: { result: {} } },
      {
        type: 'result',
        payload: { result: { success: true, item: { id: 'FR-AC-202', acceptanceCriteria: [] } } },
      },
    ];

    const oldFailsafeDir = process.env.MCPSERVER_FAILSAFE_DIR;
    const failsafeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'core-req-ac-failsafe-'));
    process.env.MCPSERVER_FAILSAFE_DIR = failsafeDir;

    try {
      await expect(
        handleRequirementsTool(
          'req_update_fr',
          {
            id: 'FR-AC-202',
            acceptanceCriteria: [{ id: 'caller-ac-1', text: 'caller criterion text' }],
          },
          asBridge(fake),
        ),
      ).rejects.toThrow(/requirements_acceptance_criteria_not_captured/);
    } finally {
      if (oldFailsafeDir === undefined) delete process.env.MCPSERVER_FAILSAFE_DIR;
      else process.env.MCPSERVER_FAILSAFE_DIR = oldFailsafeDir;
      fs.rmSync(failsafeDir, { recursive: true, force: true });
    }
  });


  test('req_copy_acceptance_criteria_from_todo maps to the workflow method', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: { copied: true } },
    };

    await handleRequirementsTool(
      'req_copy_acceptance_criteria_from_todo',
      { kind: 'fr', id: 'FR-AC-001', todoId: 'PLAN-MCP-001' },
      asBridge(fake),
    );

    expect(fake.calls).toEqual([
      {
        method: 'workflow.requirements.copyAcceptanceCriteriaFromTodo',
        params: { kind: 'fr', id: 'FR-AC-001', todoId: 'PLAN-MCP-001' },
      },
    ]);
  });

  test('req_update_fr_batch parses PowerShell YAML string records before invoking the bridge', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: { items: [] } },
    };

    const recordsYaml = `records:
- id: FR-LOC-001
  title: Monitor device location
  description: The system SHALL monitor the device location while tracking is enabled.
  priority: high
  status: pending
  area: LOC
  acceptanceCriteria:
  - id: FR-LOC-001-AC001
    text: Demonstrates behavior for FR-LOC-001.
    isSatisfied: false`;

    await handleRequirementsTool('req_update_fr_batch', { records: recordsYaml }, asBridge(fake));

    expect(fake.calls).toEqual([
      {
        method: 'workflow.requirements.updateFrBatch',
        params: {
          records: [
            {
              id: 'FR-LOC-001',
              title: 'Monitor device location',
              description: 'The system SHALL monitor the device location while tracking is enabled.',
              priority: 'high',
              status: 'pending',
              area: 'LOC',
              acceptanceCriteria: [
                {
                  id: 'FR-LOC-001-AC001',
                  text: 'Demonstrates behavior for FR-LOC-001.',
                  isSatisfied: false,
                },
              ],
            },
          ],
        },
      },
    ]);
  });

  test('req_create_batch parses inline JSON array records before invoking the bridge', async () => {
    const fake = new FakeBridge();
    fake.nextResponse = {
      type: 'result',
      payload: { result: { items: [] } },
    };

    const recordsJson = '[{"kind":"fr","id":"FR-LOC-001","title":"Monitor device location","description":"The system SHALL monitor the device location while tracking is enabled.","priority":"high","status":"pending","area":"LOC","acceptanceCriteria":[{"id":"FR-LOC-001-AC001","text":"Demonstrates behavior for FR-LOC-001.","isSatisfied":false}]}]';

    await handleRequirementsTool('req_create_batch', { records: recordsJson }, asBridge(fake));

    expect(fake.calls[0]).toEqual({
      method: 'workflow.requirements.createBatch',
      params: {
        records: [
          expect.objectContaining({
            kind: 'fr',
            id: 'FR-LOC-001',
            acceptanceCriteria: [
              expect.objectContaining({
                id: 'FR-LOC-001-AC001',
                isSatisfied: false,
              }),
            ],
          }),
        ],
      },
    });
  });

  test('uses HTTP wiki fallback when typed generate returns empty result', async () => {
    const fake = new FakeBridge();
    fake.responses = [
      {
        type: 'error',
        payload: { code: 'invalid_argument', message: 'Invalid format: wiki' },
      },
      {
        type: 'result',
        payload: { result: {} },
      },
    ];

    const oldFetch = globalThis.fetch;
    const oldApiKey = process.env.MCPSERVER_API_KEY;
    const oldWorkspacePath = process.env.MCPSERVER_WORKSPACE_PATH;
    const oldBaseUrl = process.env.MCPSERVER_BASE_URL;
    const bytes = new Uint8Array([0x50, 0x4b, 0x03, 0x04]);

    process.env.MCPSERVER_API_KEY = 'test-api-key';
    process.env.MCPSERVER_WORKSPACE_PATH = 'F:\\GitHub\\TruckMate';
    process.env.MCPSERVER_BASE_URL = 'http://127.0.0.1:8765';
    globalThis.fetch = jest.fn(async () => ({
      ok: true,
      headers: { get: () => 'application/zip' },
      arrayBuffer: async () => bytes.buffer,
    })) as unknown as typeof fetch;

    try {
      const result = await handleRequirementsTool(
        'req_generate_document',
        { format: 'wiki', docType: 'all' },
        asBridge(fake),
      );

      expect(JSON.stringify(result)).toContain('UEsDBA==');
      expect(globalThis.fetch).toHaveBeenCalledWith(
        'http://127.0.0.1:8765/mcpserver/requirements/generate?doc=all&format=wiki',
        {
          headers: {
            'X-Api-Key': 'test-api-key',
            'X-Workspace-Path': 'F:\\GitHub\\TruckMate',
          },
        },
      );
    } finally {
      globalThis.fetch = oldFetch;
      if (oldApiKey === undefined) delete process.env.MCPSERVER_API_KEY;
      else process.env.MCPSERVER_API_KEY = oldApiKey;
      if (oldWorkspacePath === undefined) delete process.env.MCPSERVER_WORKSPACE_PATH;
      else process.env.MCPSERVER_WORKSPACE_PATH = oldWorkspacePath;
      if (oldBaseUrl === undefined) delete process.env.MCPSERVER_BASE_URL;
      else process.env.MCPSERVER_BASE_URL = oldBaseUrl;
    }
  });
});
