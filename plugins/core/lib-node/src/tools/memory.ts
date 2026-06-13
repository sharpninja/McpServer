import type { ToolDescriptor } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';

const MEMORY_ID_PATTERN = '^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$';
const MEMORY_SCOPE_ENUM = ['Global', 'Workspace'] as const;
const MEMORY_LIST_SCOPE_ENUM = ['Effective', 'Global', 'Workspace'] as const;

export const memoryTools: ToolDescriptor[] = [
  {
    name: 'memory_list',
    description: 'List MCP memories visible to the current workspace. Defaults to Effective scope.',
    inputSchema: {
      type: 'object',
      properties: {
        scope: { type: 'string', enum: [...MEMORY_LIST_SCOPE_ENUM], description: 'Effective, Global, or Workspace memory scope' },
        category: { type: 'string', description: 'Optional memory category filter' },
        keyword: { type: 'string', description: 'Optional keyword filter across memory category and text' },
      },
    },
  },
  {
    name: 'memory_get',
    description: 'Fetch a single MCP memory by ID.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', pattern: MEMORY_ID_PATTERN, description: 'Memory ID, e.g. MEMORY-REQ-001' },
      },
      required: ['id'],
    },
  },
  {
    name: 'memory_add',
    description: 'Create an MCP memory in Global or Workspace scope.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', pattern: MEMORY_ID_PATTERN, description: 'Optional explicit memory ID' },
        category: { type: 'string', description: 'Memory category, e.g. REQ or USER' },
        text: { type: 'string', description: 'Raw memory text to preserve and inject verbatim' },
        scope: { type: 'string', enum: [...MEMORY_SCOPE_ENUM], description: 'Global or Workspace' },
        updatedBy: { type: 'string', description: 'Agent/user identity writing the memory' },
      },
      required: ['category', 'text'],
    },
  },
  {
    name: 'memory_update',
    description: 'Update an existing MCP memory.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', pattern: MEMORY_ID_PATTERN, description: 'Memory ID to update' },
        category: { type: 'string', description: 'Replacement category' },
        text: { type: 'string', description: 'Replacement raw memory text' },
        scope: { type: 'string', enum: [...MEMORY_SCOPE_ENUM], description: 'Global or Workspace' },
        updatedBy: { type: 'string', description: 'Agent/user identity updating the memory' },
      },
      required: ['id'],
    },
  },
  {
    name: 'memory_remove',
    description: 'Remove an MCP memory by ID.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', pattern: MEMORY_ID_PATTERN, description: 'Memory ID to remove' },
      },
      required: ['id'],
    },
  },
];

const toolMethodMap: Record<string, string> = {
  memory_list: 'workflow.memory.list',
  memory_get: 'workflow.memory.get',
  memory_add: 'workflow.memory.add',
  memory_update: 'workflow.memory.update',
  memory_remove: 'workflow.memory.remove',
};

const mutatingMemoryTools = new Set(['memory_add', 'memory_update', 'memory_remove']);

export function canHandleMemoryTool(name: string): boolean {
  return name in toolMethodMap;
}

function unwrapRequest(args: Record<string, unknown>): Record<string, unknown> {
  const request = args.request;
  if (request && typeof request === 'object' && !Array.isArray(request)) {
    return request as Record<string, unknown>;
  }
  return args;
}

function memoryMutationDescription(name: string, args: Record<string, unknown>): string {
  const operation = name.replace(/^memory_/, '');
  const id = typeof args.id === 'string' && args.id.startsWith('MEMORY-') ? ` ${args.id}` : '';
  return `Memory ${operation}${id}`;
}

async function appendMemoryMutationAction(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
): Promise<void> {
  if (!mutatingMemoryTools.has(name)) return;

  try {
    await bridge.invoke('workflow.sessionlog.appendActions', {
      actions: [
        {
          description: memoryMutationDescription(name, args),
          type: 'edit',
          status: 'completed',
        },
      ],
    });
  } catch {
    // Mutation audit is best-effort so absent turn state cannot undo a memory write.
  }
}

export async function handleMemoryTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
) {
  const method = toolMethodMap[name];
  if (!method) throw new Error(`Unknown memory tool: ${name}`);
  const normalizedArgs = unwrapRequest(args);

  const failsafePath = mutatingMemoryTools.has(name) ? await cacheWrite(method, normalizedArgs) : undefined;
  let response: ReplResponse;
  try {
    response = await bridge.invoke(method, normalizedArgs);
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
  await appendMemoryMutationAction(name, normalizedArgs, bridge);
  return response.payload;
}
