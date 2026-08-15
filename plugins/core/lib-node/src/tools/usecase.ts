import type { ToolDescriptor as Tool } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';

/**
 * TR-MCP-USECASE-005 / FR-MCP-USECASE-001..010: Plugin-core Use Case tools.
 * Routes through REPL client passthrough: client.UseCases.*
 */
export const usecaseTools: Tool[] = [
  {
    name: 'usecase_list',
    description: 'List use cases in the workspace. Optional title filter.',
    inputSchema: {
      type: 'object',
      properties: {
        title: { type: 'string', description: 'Optional title filter' },
      },
    },
  },
  {
    name: 'usecase_get',
    description: 'Get a use case by id including actors, flows, steps, FR links, approval, and product fields.',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number', description: 'Use case id' },
      },
      required: ['useCaseId'],
    },
  },
  {
    name: 'usecase_create',
    description: 'Create a use case. Optional frId creates a Realizes link; createBasicFlow seeds a Basic flow.',
    inputSchema: {
      type: 'object',
      properties: {
        title: { type: 'string' },
        briefDescription: { type: 'string' },
        precondition: { type: 'string' },
        postcondition: { type: 'string' },
        scope: { type: 'string' },
        priority: { type: 'number' },
        frId: { type: 'string', description: 'Optional FR id to link with Realizes' },
        linkType: { type: 'string', description: 'Link type when frId is set (default Realizes)' },
        createBasicFlow: { type: 'boolean' },
      },
      required: ['title'],
    },
  },
  {
    name: 'usecase_update',
    description: 'Update use case header fields (title, brief, pre/postcondition, scope, priority).',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
        title: { type: 'string' },
        briefDescription: { type: 'string' },
        precondition: { type: 'string' },
        postcondition: { type: 'string' },
        scope: { type: 'string' },
        priority: { type: 'number' },
      },
      required: ['useCaseId'],
    },
  },
  {
    name: 'usecase_delete',
    description: 'Soft-delete a use case and its durable child rows.',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
      },
      required: ['useCaseId'],
    },
  },
  {
    name: 'usecase_link_fr',
    description: 'Link a use case to a functional requirement (default LinkType Realizes).',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
        frId: { type: 'string' },
        linkType: { type: 'string' },
        linkOrder: { type: 'number' },
        notes: { type: 'string' },
      },
      required: ['useCaseId', 'frId'],
    },
  },
  {
    name: 'usecase_from_fr',
    description: 'Create a shell use case from an FR with an automatic Realizes link.',
    inputSchema: {
      type: 'object',
      properties: {
        frId: { type: 'string' },
        title: { type: 'string' },
        briefDescription: { type: 'string' },
      },
      required: ['frId'],
    },
  },
  {
    name: 'usecase_diagram',
    description: 'Generate a diagram for a use case (format: mermaid or plantuml).',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
        format: { type: 'string', description: 'mermaid (default) or plantuml' },
      },
      required: ['useCaseId'],
    },
  },
  {
    name: 'usecase_coverage',
    description: 'Report use cases and FRs missing Realizes links in the workspace.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'usecase_set_approval',
    description: 'Set use case approval status (Draft, Submitted, Approved, Rejected). Approving increments version.',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
        status: { type: 'string', enum: ['Draft', 'Submitted', 'Approved', 'Rejected'] },
      },
      required: ['useCaseId', 'status'],
    },
  },
  {
    name: 'usecase_set_product',
    description: 'Set or clear product membership key for multi-workspace sharing hooks.',
    inputSchema: {
      type: 'object',
      properties: {
        useCaseId: { type: 'number' },
        productKey: { type: 'string', description: 'Product key, or omit/empty to clear' },
      },
      required: ['useCaseId'],
    },
  },
  {
    name: 'usecase_list_by_product',
    description: 'List use cases sharing a product key.',
    inputSchema: {
      type: 'object',
      properties: {
        productKey: { type: 'string' },
      },
      required: ['productKey'],
    },
  },
];

const toolMethodMap: Record<string, string> = {
  usecase_list: 'client.UseCases.ListAsync',
  usecase_get: 'client.UseCases.GetAsync',
  usecase_create: 'client.UseCases.CreateAsync',
  usecase_update: 'client.UseCases.UpdateAsync',
  usecase_delete: 'client.UseCases.DeleteAsync',
  usecase_link_fr: 'client.UseCases.LinkFrAsync',
  usecase_from_fr: 'client.UseCases.CreateFromFrAsync',
  usecase_diagram: 'client.UseCases.GetDiagramAsync',
  usecase_coverage: 'client.UseCases.GetCoverageAsync',
  usecase_set_approval: 'client.UseCases.SetApprovalAsync',
  usecase_set_product: 'client.UseCases.SetProductAsync',
  usecase_list_by_product: 'client.UseCases.ListByProductAsync',
};

const mutatingUseCaseTools = new Set([
  'usecase_create',
  'usecase_update',
  'usecase_delete',
  'usecase_link_fr',
  'usecase_from_fr',
  'usecase_set_approval',
  'usecase_set_product',
]);

export function canHandleUseCaseTool(name: string): boolean {
  return name in toolMethodMap;
}

function unwrapRequest(args: Record<string, unknown>): Record<string, unknown> {
  const request = args.request;
  if (request && typeof request === 'object' && !Array.isArray(request)) {
    return request as Record<string, unknown>;
  }
  return args;
}

function numberArg(args: Record<string, unknown>, key: string): number | undefined {
  const value = args[key];
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim().length > 0) {
    const n = Number(value);
    if (Number.isFinite(n)) return n;
  }
  return undefined;
}

function stringArg(args: Record<string, unknown>, key: string): string | undefined {
  const value = args[key];
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : undefined;
}

function pickDefined(args: Record<string, unknown>, keys: string[]): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  for (const key of keys) {
    if (args[key] !== undefined && args[key] !== null && args[key] !== '') {
      body[key] = args[key];
    }
  }
  return body;
}

/** Map plugin tool args onto UseCaseClient method parameter names. */
export function buildUseCaseParams(name: string, rawArgs: Record<string, unknown>): Record<string, unknown> {
  const args = unwrapRequest(rawArgs);
  const useCaseId = numberArg(args, 'useCaseId');

  switch (name) {
    case 'usecase_list':
      return pickDefined(args, ['title']);
    case 'usecase_get':
      return { useCaseId };
    case 'usecase_create':
      return {
        request: pickDefined(args, [
          'title',
          'briefDescription',
          'precondition',
          'postcondition',
          'scope',
          'priority',
          'frId',
          'linkType',
          'createBasicFlow',
        ]),
      };
    case 'usecase_update':
      return {
        useCaseId,
        request: pickDefined(args, [
          'title',
          'briefDescription',
          'precondition',
          'postcondition',
          'scope',
          'priority',
        ]),
      };
    case 'usecase_delete':
      return { useCaseId };
    case 'usecase_link_fr':
      return {
        useCaseId,
        request: pickDefined(args, ['frId', 'linkType', 'linkOrder', 'notes']),
      };
    case 'usecase_from_fr':
      return {
        frId: stringArg(args, 'frId'),
        request: pickDefined(args, ['title', 'briefDescription']),
      };
    case 'usecase_diagram':
      return {
        useCaseId,
        format: stringArg(args, 'format') ?? 'mermaid',
      };
    case 'usecase_coverage':
      return {};
    case 'usecase_set_approval':
      return {
        useCaseId,
        request: { status: stringArg(args, 'status') },
      };
    case 'usecase_set_product':
      return {
        useCaseId,
        request: { productKey: args.productKey ?? null },
      };
    case 'usecase_list_by_product':
      return { productKey: stringArg(args, 'productKey') };
    default:
      return args;
  }
}

function mutationDescription(name: string, args: Record<string, unknown>): string {
  const id = numberArg(args, 'useCaseId');
  const idSuffix = id !== undefined ? ` ${id}` : '';
  return `UseCase ${name.replace(/^usecase_/, '')}${idSuffix}`;
}

async function appendMutationAction(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
): Promise<void> {
  if (!mutatingUseCaseTools.has(name)) return;
  try {
    await bridge.invoke('workflow.sessionlog.appendActions', {
      actions: [
        {
          description: mutationDescription(name, args),
          type: 'edit',
          status: 'completed',
        },
      ],
    });
  } catch {
    // Audit is best-effort.
  }
}

export async function handleUseCaseTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
): Promise<Record<string, unknown>> {
  const method = toolMethodMap[name];
  if (!method) throw new Error(`Unknown use case tool: ${name}`);

  const params = buildUseCaseParams(name, args);
  const failsafePath = mutatingUseCaseTools.has(name) ? await cacheWrite(method, params) : undefined;

  let response: ReplResponse;
  try {
    response = await bridge.invoke(method, params);
  } catch (error) {
    const suffix = failsafePath ? ` Local failsafe saved: ${failsafePath}` : '';
    throw new Error(`${error instanceof Error ? error.message : String(error)}${suffix}`);
  }

  if (response.type === 'error') {
    const payload = response.payload as { message?: string; code?: string };
    const suffix = failsafePath ? ` Local failsafe saved: ${failsafePath}` : '';
    throw new Error(`${payload.code ?? 'error'}: ${payload.message ?? 'Unknown error'}${suffix}`);
  }

  if (failsafePath) await cacheDelete(failsafePath);
  await appendMutationAction(name, unwrapRequest(args), bridge);

  const payload = response.payload as { result?: unknown };
  if (payload && typeof payload === 'object' && 'result' in payload) {
    return { result: payload.result };
  }
  return (payload as Record<string, unknown>) ?? { result: null };
}
