import type { ToolDescriptor as Tool } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';

export const graphragTools: Tool[] = [
  // --- Status & Indexing ---
  {
    name: 'graphrag_status',
    description: 'Check whether GraphRAG is enabled, initialized, and indexed for the workspace.',
    inputSchema: { type: 'object', properties: {} },
  },
  {
    name: 'graphrag_index',
    description: 'Rebuild the GraphRAG index from the current corpus.',
    inputSchema: {
      type: 'object',
      properties: {
        force: { type: 'boolean', description: 'Force rebuild even if no corpus changes detected' },
      },
    },
  },
  // --- Query ---
  {
    name: 'graphrag_query',
    description: 'Run a natural-language query against the indexed knowledge graph.',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string', description: 'Natural-language query' },
        mode: { type: 'string', enum: ['local', 'global', 'drift'], description: 'Query mode (default: local)' },
        maxChunks: { type: 'number', description: 'Max text chunks to include (default: 10)' },
        includeContextChunks: { type: 'boolean' },
        maxEntities: { type: 'number' },
        maxRelationships: { type: 'number' },
        communityDepth: { type: 'number' },
        responseTokenBudget: { type: 'number' },
      },
      required: ['query'],
    },
  },
  // --- Ingestion ---
  {
    name: 'graphrag_ingest',
    description: 'Add raw text to the GraphRAG corpus without triggering a full reindex.',
    inputSchema: {
      type: 'object',
      properties: {
        content: { type: 'string', description: 'Text content to ingest' },
        title: { type: 'string', description: 'Human-readable document name' },
        sourceType: { type: 'string', description: 'Classification tag (default: adhoc-text)' },
        sourceKey: { type: 'string', description: 'Unique path/key for the document' },
        triggerReindex: { type: 'boolean', description: 'Trigger full reindex after ingestion' },
      },
      required: ['content'],
    },
  },
  // --- Document Management ---
  {
    name: 'graphrag_doc_list',
    description: 'Paginate corpus documents by sourceType.',
    inputSchema: {
      type: 'object',
      properties: {
        skip: { type: 'number', description: 'Pagination offset' },
        take: { type: 'number', description: 'Page size (default: 50)' },
        sourceType: { type: 'string', description: 'Filter by source type' },
      },
    },
  },
  {
    name: 'graphrag_doc_chunks',
    description: 'Inspect the text chunks of a specific document.',
    inputSchema: {
      type: 'object',
      properties: {
        documentId: { type: 'string', description: 'Document ID (e.g. doc-a1b2c3d4)' },
      },
      required: ['documentId'],
    },
  },
  {
    name: 'graphrag_doc_delete',
    description: 'Remove a document and all its chunks from the corpus.',
    inputSchema: {
      type: 'object',
      properties: {
        documentId: { type: 'string' },
      },
      required: ['documentId'],
    },
  },
  // --- Entity Management ---
  {
    name: 'graphrag_entity_create',
    description: 'Create a named graph entity (component, concept, person, etc.).',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string', description: 'Entity name' },
        entityType: { type: 'string', description: 'Entity type (e.g. component, concept, person)' },
        description: { type: 'string' },
        metadata: { type: 'string', description: 'JSON string of additional metadata' },
      },
      required: ['name', 'entityType'],
    },
  },
  {
    name: 'graphrag_entity_list',
    description: 'Paginate graph entities with optional type filter.',
    inputSchema: {
      type: 'object',
      properties: {
        skip: { type: 'number' },
        take: { type: 'number' },
        entityType: { type: 'string', description: 'Filter by entity type' },
      },
    },
  },
  {
    name: 'graphrag_entity_get',
    description: 'Fetch a single graph entity by ID.',
    inputSchema: {
      type: 'object',
      properties: {
        entityId: { type: 'string', description: 'Entity ID (e.g. ent-001)' },
      },
      required: ['entityId'],
    },
  },
  {
    name: 'graphrag_entity_update',
    description: 'Replace an entity (full body, not patch). Supply all fields.',
    inputSchema: {
      type: 'object',
      properties: {
        entityId: { type: 'string' },
        name: { type: 'string' },
        entityType: { type: 'string' },
        description: { type: 'string' },
        metadata: { type: 'string' },
      },
      required: ['entityId', 'name', 'entityType'],
    },
  },
  {
    name: 'graphrag_entity_delete',
    description: 'Remove a graph entity. Delete related relationships separately first.',
    inputSchema: {
      type: 'object',
      properties: {
        entityId: { type: 'string' },
      },
      required: ['entityId'],
    },
  },
  // --- Relationship Management ---
  {
    name: 'graphrag_rel_create',
    description: 'Create a directed relationship between two graph entities.',
    inputSchema: {
      type: 'object',
      properties: {
        sourceEntityId: { type: 'string' },
        targetEntityId: { type: 'string' },
        relationshipType: { type: 'string', description: 'Relationship type (e.g. validates, calls, implements)' },
        description: { type: 'string' },
        weight: { type: 'number', description: 'Edge weight (default: 1.0)' },
        metadata: { type: 'string', description: 'JSON string of additional metadata' },
      },
      required: ['sourceEntityId', 'targetEntityId', 'relationshipType'],
    },
  },
  {
    name: 'graphrag_rel_list',
    description: 'Paginate relationships with optional entityId or type filter.',
    inputSchema: {
      type: 'object',
      properties: {
        skip: { type: 'number' },
        take: { type: 'number' },
        entityId: { type: 'string', description: 'Filter to relationships involving this entity' },
        type: { type: 'string', description: 'Filter by relationship type' },
      },
    },
  },
  {
    name: 'graphrag_rel_get',
    description: 'Fetch a single relationship by ID.',
    inputSchema: {
      type: 'object',
      properties: {
        relationshipId: { type: 'string', description: 'Relationship ID (e.g. rel-001)' },
      },
      required: ['relationshipId'],
    },
  },
  {
    name: 'graphrag_rel_update',
    description: 'Replace a relationship (full body, not patch). Supply all fields.',
    inputSchema: {
      type: 'object',
      properties: {
        relationshipId: { type: 'string' },
        sourceEntityId: { type: 'string' },
        targetEntityId: { type: 'string' },
        relationshipType: { type: 'string' },
        description: { type: 'string' },
        weight: { type: 'number' },
        metadata: { type: 'string' },
      },
      required: ['relationshipId', 'sourceEntityId', 'targetEntityId', 'relationshipType'],
    },
  },
  {
    name: 'graphrag_rel_delete',
    description: 'Remove a graph relationship.',
    inputSchema: {
      type: 'object',
      properties: {
        relationshipId: { type: 'string' },
      },
      required: ['relationshipId'],
    },
  },
];

const toolMethodMap: Record<string, string> = {
  graphrag_status: 'workflow.graphrag.status',
  graphrag_index: 'workflow.graphrag.index',
  graphrag_query: 'workflow.graphrag.query',
  graphrag_ingest: 'workflow.graphrag.ingest',
  graphrag_doc_list: 'workflow.graphrag.documents.list',
  graphrag_doc_chunks: 'workflow.graphrag.documents.chunks',
  graphrag_doc_delete: 'workflow.graphrag.documents.delete',
  graphrag_entity_create: 'workflow.graphrag.entities.create',
  graphrag_entity_list: 'workflow.graphrag.entities.list',
  graphrag_entity_get: 'workflow.graphrag.entities.get',
  graphrag_entity_update: 'workflow.graphrag.entities.update',
  graphrag_entity_delete: 'workflow.graphrag.entities.delete',
  graphrag_rel_create: 'workflow.graphrag.relationships.create',
  graphrag_rel_list: 'workflow.graphrag.relationships.list',
  graphrag_rel_get: 'workflow.graphrag.relationships.get',
  graphrag_rel_update: 'workflow.graphrag.relationships.update',
  graphrag_rel_delete: 'workflow.graphrag.relationships.delete',
};

const mutatingGraphragTools = new Set([
  'graphrag_index',
  'graphrag_ingest',
  'graphrag_doc_delete',
  'graphrag_entity_create',
  'graphrag_entity_update',
  'graphrag_entity_delete',
  'graphrag_rel_create',
  'graphrag_rel_update',
  'graphrag_rel_delete',
]);

export function canHandleGraphragTool(name: string): boolean {
  return name in toolMethodMap;
}

function stringArg(args: Record<string, unknown>, key: string): string {
  const value = args[key];
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : '';
}

function numberArg(args: Record<string, unknown>, key: string, fallback: number): number {
  const value = args[key];
  return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
}

function addNumberQuery(url: URL, args: Record<string, unknown>, key: string, fallback: number): void {
  url.searchParams.set(key, String(numberArg(args, key, fallback)));
}

function addStringQuery(url: URL, args: Record<string, unknown>, key: string, queryKey = key): void {
  const value = stringArg(args, key);
  if (value) url.searchParams.set(queryKey, value);
}

function pickBody(args: Record<string, unknown>, keys: string[]): Record<string, unknown> {
  const body: Record<string, unknown> = {};
  for (const key of keys) {
    const value = args[key];
    if (value !== undefined && value !== null && value !== '') body[key] = value;
  }
  return body;
}

async function parseHttpResponse(response: Response): Promise<{ contentType: string; result: unknown; bodyText: string }> {
  const contentType = response.headers.get('content-type')?.split(';')[0] || 'application/json';
  const bodyText = await response.text().catch(() => '');
  if (!bodyText) return { contentType, result: { success: response.ok }, bodyText };

  if (/json/i.test(contentType)) {
    try {
      return { contentType, result: JSON.parse(bodyText), bodyText };
    } catch {
      return { contentType, result: bodyText, bodyText };
    }
  }

  return { contentType, result: bodyText, bodyText };
}

function jsonInit(method: string, headers: Record<string, string>, body: Record<string, unknown>): RequestInit {
  return {
    method,
    headers: { ...headers, 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  };
}

async function graphragHttpFallback(
  name: string,
  args: Record<string, unknown>,
): Promise<ReplResponse | null> {
  const fetchFn = globalThis.fetch;
  const apiKey = process.env.MCPSERVER_API_KEY;
  const workspacePath = process.env.MCPSERVER_WORKSPACE_PATH ?? process.env.MCP_WORKSPACE_PATH;
  const baseUrl = process.env.MCPSERVER_BASE_URL ?? process.env.MCP_SERVER_URL;
  if (typeof fetchFn !== 'function' || !apiKey || !workspacePath || !baseUrl) return null;

  const root = `${baseUrl.replace(/\/$/, '')}/mcpserver/graphrag`;
  const headers: Record<string, string> = {
    'X-Api-Key': apiKey,
    'X-Workspace-Path': workspacePath,
  };

  let url: string;
  let init: RequestInit = { headers };

  switch (name) {
    case 'graphrag_status':
      url = `${root}/status`;
      break;

    case 'graphrag_index':
      url = `${root}/index`;
      init = jsonInit('POST', headers, { force: args.force === true });
      break;

    case 'graphrag_query':
      url = `${root}/query`;
      init = jsonInit('POST', headers, pickBody(args, [
        'query',
        'mode',
        'maxChunks',
        'includeContextChunks',
        'maxEntities',
        'maxRelationships',
        'communityDepth',
        'responseTokenBudget',
      ]));
      break;

    case 'graphrag_ingest':
      url = `${root}/documents/ingest`;
      init = jsonInit('POST', headers, pickBody(args, [
        'content',
        'title',
        'sourceType',
        'sourceKey',
        'triggerReindex',
      ]));
      break;

    case 'graphrag_doc_list': {
      const docUrl = new URL(`${root}/documents`);
      addNumberQuery(docUrl, args, 'skip', 0);
      addNumberQuery(docUrl, args, 'take', 50);
      addStringQuery(docUrl, args, 'sourceType');
      url = docUrl.toString();
      break;
    }

    case 'graphrag_doc_chunks':
      url = `${root}/documents/${encodeURIComponent(stringArg(args, 'documentId'))}/chunks`;
      break;

    case 'graphrag_doc_delete':
      url = `${root}/documents/${encodeURIComponent(stringArg(args, 'documentId'))}`;
      init = { method: 'DELETE', headers };
      break;

    case 'graphrag_entity_create':
      url = `${root}/entities`;
      init = jsonInit('POST', headers, pickBody(args, ['name', 'entityType', 'description', 'metadata']));
      break;

    case 'graphrag_entity_list': {
      const entityUrl = new URL(`${root}/entities`);
      addNumberQuery(entityUrl, args, 'skip', 0);
      addNumberQuery(entityUrl, args, 'take', 50);
      addStringQuery(entityUrl, args, 'entityType');
      url = entityUrl.toString();
      break;
    }

    case 'graphrag_entity_get':
      url = `${root}/entities/${encodeURIComponent(stringArg(args, 'entityId'))}`;
      break;

    case 'graphrag_entity_update':
      url = `${root}/entities/${encodeURIComponent(stringArg(args, 'entityId'))}`;
      init = jsonInit('PUT', headers, pickBody(args, ['name', 'entityType', 'description', 'metadata']));
      break;

    case 'graphrag_entity_delete':
      url = `${root}/entities/${encodeURIComponent(stringArg(args, 'entityId'))}`;
      init = { method: 'DELETE', headers };
      break;

    case 'graphrag_rel_create':
      url = `${root}/relationships`;
      init = jsonInit('POST', headers, pickBody(args, [
        'sourceEntityId',
        'targetEntityId',
        'relationshipType',
        'description',
        'weight',
        'metadata',
      ]));
      break;

    case 'graphrag_rel_list': {
      const relUrl = new URL(`${root}/relationships`);
      addNumberQuery(relUrl, args, 'skip', 0);
      addNumberQuery(relUrl, args, 'take', 50);
      addStringQuery(relUrl, args, 'entityId');
      addStringQuery(relUrl, args, 'type');
      url = relUrl.toString();
      break;
    }

    case 'graphrag_rel_get':
      url = `${root}/relationships/${encodeURIComponent(stringArg(args, 'relationshipId'))}`;
      break;

    case 'graphrag_rel_update':
      url = `${root}/relationships/${encodeURIComponent(stringArg(args, 'relationshipId'))}`;
      init = jsonInit('PUT', headers, pickBody(args, [
        'sourceEntityId',
        'targetEntityId',
        'relationshipType',
        'description',
        'weight',
        'metadata',
      ]));
      break;

    case 'graphrag_rel_delete':
      url = `${root}/relationships/${encodeURIComponent(stringArg(args, 'relationshipId'))}`;
      init = { method: 'DELETE', headers };
      break;

    default:
      return null;
  }

  const response = await fetchFn(url, init);
  const parsed = await parseHttpResponse(response);
  if (!response.ok) {
    return {
      type: 'error',
      payload: {
        code: 'http_error',
        message: `GraphRAG HTTP fallback returned HTTP ${response.status} for ${name}${parsed.bodyText ? `: ${parsed.bodyText}` : ''}`,
      },
    };
  }

  return {
    type: 'result',
    payload: {
      result: parsed.result,
      contentType: parsed.contentType,
    },
  };
}

export async function handleGraphragTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
) {
  const method = toolMethodMap[name];
  if (!method) throw new Error(`Unknown graphrag tool: ${name}`);

  const failsafePath = mutatingGraphragTools.has(name) ? await cacheWrite(method, args) : undefined;
  let response: ReplResponse;
  try {
    response = (await graphragHttpFallback(name, args)) ?? (await bridge.invoke(method, args));
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
