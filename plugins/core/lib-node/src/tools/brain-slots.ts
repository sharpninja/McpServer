import type { ToolDescriptor as Tool } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';

const BRAIN_SLOT_ROLES = ['LeftHemisphere', 'RightHemisphere', 'CuriosityEngine', 'ArbiterOfTruth'] as const;
const BRAIN_SLOT_PROVIDERS = ['OpenAI', 'OpenAICompatible'] as const;

export const brainSlotTools: Tool[] = [
  {
    name: 'brain_slot_list',
    description: 'List external brain-slot definitions for a workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
      },
      required: ['workspacePath'],
    },
  },
  {
    name: 'brain_slot_get',
    description: 'Get an external brain-slot definition by slot id.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
      },
      required: ['workspacePath', 'slotId'],
    },
  },
  {
    name: 'brain_slot_upsert',
    description: 'Create or update an external brain-slot definition. Use credentialReference, never a raw API key.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
        role: { type: 'string', enum: [...BRAIN_SLOT_ROLES], description: 'Quad role' },
        providerKind: { type: 'string', enum: [...BRAIN_SLOT_PROVIDERS], description: 'Provider kind' },
        modelId: { type: 'string', description: 'Provider model identifier' },
        credentialReference: { type: 'string', pattern: '^(env|config|file):.+', description: 'Credential reference' },
        partyId: { type: 'string', description: 'Trusted party id' },
        displayName: { type: 'string', description: 'Display name' },
        endpoint: { type: 'string', description: 'Provider endpoint URI' },
        enabled: { type: 'boolean', description: 'Whether the slot is enabled' },
        replaceExisting: { type: 'boolean', description: 'Replace an existing enabled slot for the same role' },
        timeoutSeconds: { type: 'number', description: 'Timeout in seconds' },
        maxOutputTokens: { type: 'number', description: 'Maximum output tokens' },
        systemPrompt: { type: 'string', description: 'Optional system prompt' },
      },
      required: ['workspacePath', 'slotId', 'role', 'providerKind', 'modelId', 'credentialReference'],
    },
  },
  {
    name: 'brain_slot_delete',
    description: 'Soft-delete and disable an external brain-slot definition.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
      },
      required: ['workspacePath', 'slotId'],
    },
  },
  {
    name: 'brain_slot_enable',
    description: 'Enable an external brain-slot definition.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
        replaceExisting: { type: 'boolean', description: 'Replace an existing enabled slot for the same role' },
      },
      required: ['workspacePath', 'slotId'],
    },
  },
  {
    name: 'brain_slot_disable',
    description: 'Disable an external brain-slot definition.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
      },
      required: ['workspacePath', 'slotId'],
    },
  },
  {
    name: 'brain_slot_status',
    description: 'Get external brain-slot readiness status for a workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
      },
      required: ['workspacePath'],
    },
  },
  {
    name: 'brain_slot_invoke',
    description: 'Invoke an external brain slot with transaction-gated output return.',
    inputSchema: {
      type: 'object',
      properties: {
        workspacePath: { type: 'string', description: 'Workspace path' },
        slotId: { type: 'string', description: 'Brain slot id' },
        input: { type: 'string', description: 'Input prompt' },
        turnId: { type: 'string', description: 'Owning session-log turn id' },
        admitToGraphRag: { type: 'boolean', description: 'Admit committed Curiosity output to GraphRAG/context' },
        metadataJson: { type: 'string', description: 'JSON object of string metadata' },
      },
      required: ['workspacePath', 'slotId', 'input'],
    },
  },
];

const mutatingBrainSlotTools = new Set([
  'brain_slot_upsert',
  'brain_slot_delete',
  'brain_slot_enable',
  'brain_slot_disable',
  'brain_slot_invoke',
]);

export function canHandleBrainSlotTool(name: string): boolean {
  return brainSlotTools.some((tool) => tool.name === name);
}

export async function handleBrainSlotTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
) {
  if (!canHandleBrainSlotTool(name)) throw new Error(`Unknown brain slot tool: ${name}`);

  const failsafePath = mutatingBrainSlotTools.has(name) ? await cacheWrite(name, args) : undefined;
  let response: ReplResponse;
  try {
    response = await bridge.invoke(name, args);
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
  return response.payload;
}
