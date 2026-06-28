import type { ToolDescriptor as Tool } from './tool-descriptor.js';
import type { ReplBridge, ReplResponse } from '../transport/repl-bridge.js';
import { cacheDelete, cacheWrite } from '../cache/cache-manager.js';
import * as yaml from 'js-yaml';

const STATUS_ENUM = ['pending', 'in_progress', 'completed', 'deferred'] as const;
const PRIORITY_ENUM = ['critical', 'high', 'medium', 'low'] as const;

const ACCEPTANCE_CRITERION_SCHEMA = {
  type: 'object',
  properties: {
    id: { type: 'string' },
    text: { type: 'string' },
    isSatisfied: { type: 'boolean' },
    evidence: { type: 'string' },
  },
  required: ['text'],
} as const;
const ACCEPTANCE_CRITERIA_ARRAY = { type: 'array', items: ACCEPTANCE_CRITERION_SCHEMA } as const;
const REQUIREMENT_SCOPE_FIELDS = {
  scopeStartLayerKey: {
    type: 'string',
    description: 'First requirement layer where this requirement applies.',
  },
  scopeEndLayerKey: {
    type: 'string',
    description: 'Optional last requirement layer where this requirement applies.',
  },
} as const;

function batchInputSchema(itemSchema: Record<string, unknown>) {
  return {
    type: 'object',
    properties: {
      records: {
        oneOf: [
          { type: 'array', minItems: 1, items: itemSchema },
          { type: 'string', minLength: 1 },
        ],
      },
    },
    required: ['records'],
  } as const;
}

// Per-record constraints reinstated from the cline v1 inline batch schemas;
// the oneOf(array|string) top shape matches parseRecordsValue(), which
// yaml-loads string payloads.
const FR_CREATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'FR ID (pattern: FR-AREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id', 'title'],
  anyOf: [{ required: ['description'] }, { required: ['body'] }],
} as const;

const FR_UPDATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'FR ID (pattern: FR-AREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id'],
} as const;

const TR_CREATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'TR ID (pattern: TR-AREA-SUBAREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id', 'title'],
  anyOf: [{ required: ['description'] }, { required: ['body'] }],
} as const;

const TR_UPDATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'TR ID (pattern: TR-AREA-SUBAREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id'],
} as const;

const TEST_CREATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'TEST ID (pattern: TEST-AREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    condition: { type: 'string' },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id', 'title'],
  anyOf: [{ required: ['description'] }, { required: ['condition'] }, { required: ['body'] }],
} as const;

const TEST_UPDATE_ITEM = {
  type: 'object',
  properties: {
    id: { type: 'string', description: 'TEST ID (pattern: TEST-AREA-###)' },
    title: { type: 'string' },
    description: { type: 'string' },
    body: { type: 'string' },
    condition: { type: 'string' },
    status: { type: 'string', enum: [...STATUS_ENUM] },
    priority: { type: 'string', enum: [...PRIORITY_ENUM] },
    notes: { type: 'string' },
    acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
    ...REQUIREMENT_SCOPE_FIELDS,
  },
  required: ['id'],
} as const;

// Mixed-type batches accept any of the create/update record shapes.
const MIXED_BATCH_ITEM = { type: 'object' } as const;

export const requirementsTools: Tool[] = [
  // --- Functional Requirements ---
  {
    name: 'req_list_fr',
    description: 'List workspace-scoped functional requirements from the database source of truth.',
    inputSchema: {
      type: 'object',
      properties: {
        area: { type: 'string', description: 'Filter by area (e.g. MCP, AUTH)' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
      },
    },
  },
  {
    name: 'req_get_fr',
    description: 'Fetch a single workspace-scoped functional requirement by ID.',
    inputSchema: {
      type: 'object',
      properties: { id: { type: 'string', description: 'FR ID (pattern: FR-AREA-###)' } },
      required: ['id'],
    },
  },
  {
    name: 'req_create_fr',
    description: 'Create a new workspace-scoped functional requirement in the database source of truth.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'FR ID (pattern: FR-AREA-###)' },
        title: { type: 'string' },
        description: { type: 'string' },
        priority: { type: 'string', enum: [...PRIORITY_ENUM] },
        area: { type: 'string' },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id', 'title', 'description', 'priority', 'area'],
    },
  },
  {
    name: 'req_update_fr',
    description: 'Modify an existing functional requirement.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string' },
        title: { type: 'string' },
        description: { type: 'string' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
        priority: { type: 'string', enum: [...PRIORITY_ENUM] },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id'],
    },
  },
  {
    name: 'req_delete_fr',
    description: 'Remove a functional requirement.',
    inputSchema: {
      type: 'object',
      properties: { id: { type: 'string' } },
      required: ['id'],
    },
  },
  // --- Technical Requirements ---
  {
    name: 'req_list_tr',
    description: 'List workspace-scoped technical requirements from the database source of truth.',
    inputSchema: {
      type: 'object',
      properties: {
        area: { type: 'string' },
        subarea: { type: 'string' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
      },
    },
  },
  {
    name: 'req_create_tr',
    description: 'Create a new technical requirement. TR IDs require both area and subarea (TR-AREA-SUBAREA-###).',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'TR ID (pattern: TR-AREA-SUBAREA-###)' },
        title: { type: 'string' },
        description: { type: 'string' },
        priority: { type: 'string', enum: [...PRIORITY_ENUM] },
        area: { type: 'string' },
        subarea: { type: 'string' },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id', 'title', 'description', 'priority', 'area', 'subarea'],
    },
  },
  {
    name: 'req_update_tr',
    description: 'Modify an existing technical requirement.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string' },
        title: { type: 'string' },
        description: { type: 'string' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id'],
    },
  },
  {
    name: 'req_delete_tr',
    description: 'Remove a technical requirement.',
    inputSchema: {
      type: 'object',
      properties: { id: { type: 'string' } },
      required: ['id'],
    },
  },
  // --- Test Requirements ---
  {
    name: 'req_list_test',
    description: 'List workspace-scoped test requirements from the database source of truth.',
    inputSchema: {
      type: 'object',
      properties: {
        area: { type: 'string' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
      },
    },
  },
  {
    name: 'req_create_test',
    description: 'Create a new test requirement.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string', description: 'TEST ID (pattern: TEST-AREA-###)' },
        title: { type: 'string' },
        description: { type: 'string' },
        priority: { type: 'string', enum: [...PRIORITY_ENUM] },
        area: { type: 'string' },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id', 'title', 'description', 'priority', 'area'],
    },
  },
  {
    name: 'req_update_test',
    description: 'Modify an existing test requirement.',
    inputSchema: {
      type: 'object',
      properties: {
        id: { type: 'string' },
        title: { type: 'string' },
        description: { type: 'string' },
        status: { type: 'string', enum: [...STATUS_ENUM] },
        notes: { type: 'string' },
        acceptanceCriteria: ACCEPTANCE_CRITERIA_ARRAY,
        ...REQUIREMENT_SCOPE_FIELDS,
      },
      required: ['id'],
    },
  },
  {
    name: 'req_delete_test',
    description: 'Remove a test requirement.',
    inputSchema: {
      type: 'object',
      properties: { id: { type: 'string' } },
      required: ['id'],
    },
  },
  {
    name: 'req_create_fr_batch',
    description: 'Create multiple workspace-scoped functional requirements atomically from a records array.',
    inputSchema: batchInputSchema(FR_CREATE_ITEM),
  },
  {
    name: 'req_update_fr_batch',
    description: 'Update multiple workspace-scoped functional requirements atomically from a records array.',
    inputSchema: batchInputSchema(FR_UPDATE_ITEM),
  },
  {
    name: 'req_create_tr_batch',
    description: 'Create multiple workspace-scoped technical requirements atomically from a records array.',
    inputSchema: batchInputSchema(TR_CREATE_ITEM),
  },
  {
    name: 'req_update_tr_batch',
    description: 'Update multiple workspace-scoped technical requirements atomically from a records array.',
    inputSchema: batchInputSchema(TR_UPDATE_ITEM),
  },
  {
    name: 'req_create_test_batch',
    description: 'Create multiple workspace-scoped test requirements atomically from a records array.',
    inputSchema: batchInputSchema(TEST_CREATE_ITEM),
  },
  {
    name: 'req_update_test_batch',
    description: 'Update multiple workspace-scoped test requirements atomically from a records array.',
    inputSchema: batchInputSchema(TEST_UPDATE_ITEM),
  },
  {
    name: 'req_create_batch',
    description: 'Create mixed FR/TR/TEST requirements atomically from a records array.',
    inputSchema: batchInputSchema(MIXED_BATCH_ITEM),
  },
  {
    name: 'req_update_batch',
    description: 'Update mixed FR/TR/TEST requirements atomically from a records array.',
    inputSchema: batchInputSchema(MIXED_BATCH_ITEM),
  },
  // --- Mappings ---
  {
    name: 'req_copy_acceptance_criteria_from_todo',
    description: 'Copy structured acceptance criteria from an execution TODO onto an FR, TR, or TEST requirement.',
    inputSchema: {
      type: 'object',
      properties: {
        kind: { type: 'string', enum: ['fr', 'tr', 'test', 'functional', 'technical', 'testing'] },
        id: { type: 'string', description: 'Requirement ID to receive the TODO acceptance criteria.' },
        todoId: { type: 'string', description: 'Execution TODO ID that supplies acceptanceCriteria.' },
      },
      required: ['kind', 'id', 'todoId'],
    },
  },
  {
    name: 'req_list_mappings',
    description: 'Query workspace-scoped FR to TR and FR to TEST traceability links.',
    inputSchema: {
      type: 'object',
      properties: {
        frId: { type: 'string', description: 'Filter by FR ID' },
        trId: { type: 'string', description: 'Filter by TR ID' },
        testId: { type: 'string', description: 'Filter by TEST ID' },
      },
    },
  },
  {
    name: 'req_create_mapping',
    description: 'Link one FR to one or more TR and TEST requirements in the active workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        frId: { type: 'string' },
        trId: { type: 'string', description: 'Legacy single TR ID. Prefer trIds.' },
        trIds: {
          type: 'array',
          items: { type: 'string' },
          description: 'TR IDs to link to the FR.',
        },
        testId: { type: 'string', description: 'Legacy single TEST ID. Prefer testIds.' },
        testIds: {
          type: 'array',
          items: { type: 'string' },
          description: 'TEST IDs to link to the FR.',
        },
        notes: { type: 'string' },
      },
      required: ['frId'],
    },
  },
  {
    name: 'req_delete_mapping',
    description: 'Remove workspace-scoped traceability links by FR, TR, and/or TEST filter.',
    inputSchema: {
      type: 'object',
      properties: {
        frId: { type: 'string' },
        trId: { type: 'string' },
      },
      required: ['frId', 'trId'],
    },
  },
  // --- Scope Layers ---
  {
    name: 'req_list_layers',
    description: 'List ordered requirement scope layers for the active workspace.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'req_create_layer',
    description: 'Create a requirement scope layer for the active workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        key: { type: 'string', description: 'Stable layer key, such as layer-2.' },
        order: { type: 'integer', description: 'Layer ordering number.' },
        name: { type: 'string' },
        description: { type: 'string' },
        scopeEndLayerKey: {
          type: 'string',
          description: 'Optional last layer where this layer remains applicable.',
        },
      },
      required: ['key', 'order', 'name'],
    },
  },
  {
    name: 'req_update_layer',
    description: 'Update mutable requirement scope layer metadata.',
    inputSchema: {
      type: 'object',
      properties: {
        key: { type: 'string', description: 'Layer key to update.' },
        name: { type: 'string' },
        description: { type: 'string' },
        scopeEndLayerKey: {
          type: 'string',
          description: 'Optional last layer where this layer remains applicable.',
        },
      },
      required: ['key'],
    },
  },
  {
    name: 'req_effective',
    description: 'Query requirements effective at the active workspace layer or an explicit preview layer.',
    inputSchema: {
      type: 'object',
      properties: {
        layerKey: {
          type: 'string',
          description: 'Optional layer key to preview instead of the active workspace layer.',
        },
      },
    },
  },
  // --- Documents ---
  {
    name: 'req_generate_document',
    description: 'Render workspace-limited requirements from the database as Markdown/YAML or write wiki/all documents directly to the workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        format: { type: 'string', enum: ['markdown', 'yaml', 'wiki'], description: 'Output format' },
        docType: { type: 'string', enum: ['matrix', 'functional', 'technical', 'testing', 'all'] },
      },
      required: ['docType'],
    },
  },
  {
    name: 'req_ingest_document',
    description: 'Import Markdown/YAML requirements or Azure/GitHub wiki documents into the workspace database source of truth.',
    inputSchema: {
      type: 'object',
      properties: {
        content: { type: 'string', description: 'Markdown or YAML content with FR/TR/TEST sections' },
        format: { type: 'string', enum: ['markdown', 'yaml', 'wiki'] },
        sourceFormat: {
          type: 'string',
          enum: ['auto', 'canonical', 'wiki'],
          description: 'Source document shape. Use auto unless forcing canonical or wiki handling.',
        },
        preferredWikiFormat: {
          type: 'string',
          enum: ['azure', 'github'],
          description: 'Tie-breaker when wiki manifest and file modified timestamps disagree.',
        },
        documents: {
          type: 'object',
          additionalProperties: {
            oneOf: [
              { type: 'string' },
              {
                type: 'object',
                properties: {
                  content: { type: 'string' },
                  contentBase64: { type: 'string' },
                  lastModifiedUtc: {
                    type: 'string',
                    description: 'File or ZIP entry modified time in UTC.',
                  },
                },
              },
            ],
          },
          description: 'Path-keyed document map for wiki imports, including per-document timestamps when available.',
        },
      },
    },
  },
];

const workflowMethodMap: Record<string, string> = {
  req_list_fr: 'workflow.requirements.listFr',
  req_get_fr: 'workflow.requirements.getFr',
  req_create_fr: 'workflow.requirements.createFr',
  req_create_fr_batch: 'workflow.requirements.createFrBatch',
  req_update_fr: 'workflow.requirements.updateFr',
  req_update_fr_batch: 'workflow.requirements.updateFrBatch',
  req_delete_fr: 'workflow.requirements.deleteFr',
  req_list_tr: 'workflow.requirements.listTr',
  req_create_tr: 'workflow.requirements.createTr',
  req_create_tr_batch: 'workflow.requirements.createTrBatch',
  req_update_tr: 'workflow.requirements.updateTr',
  req_update_tr_batch: 'workflow.requirements.updateTrBatch',
  req_delete_tr: 'workflow.requirements.deleteTr',
  req_list_test: 'workflow.requirements.listTest',
  req_create_test: 'workflow.requirements.createTest',
  req_create_test_batch: 'workflow.requirements.createTestBatch',
  req_update_test: 'workflow.requirements.updateTest',
  req_update_test_batch: 'workflow.requirements.updateTestBatch',
  req_delete_test: 'workflow.requirements.deleteTest',
  req_create_batch: 'workflow.requirements.createBatch',
  req_update_batch: 'workflow.requirements.updateBatch',
  req_copy_acceptance_criteria_from_todo: 'workflow.requirements.copyAcceptanceCriteriaFromTodo',
  req_list_mappings: 'workflow.requirements.listMappings',
  req_create_mapping: 'workflow.requirements.createMapping',
  req_delete_mapping: 'workflow.requirements.deleteMapping',
  req_list_layers: 'workflow.requirements.listLayers',
  req_create_layer: 'workflow.requirements.createLayer',
  req_update_layer: 'workflow.requirements.updateLayer',
  req_effective: 'workflow.requirements.effective',
  req_generate_document: 'workflow.requirements.generateDocument',
  req_ingest_document: 'workflow.requirements.ingestDocument',
};

const typedMethodMap: Record<string, string> = {
  req_list_fr: 'client.Requirements.ListFrAsync',
  req_get_fr: 'client.Requirements.GetFrAsync',
  req_create_fr: 'client.Requirements.CreateFrAsync',
  req_create_fr_batch: 'client.Requirements.CreateFrBatchAsync',
  req_update_fr: 'client.Requirements.UpdateFrAsync',
  req_update_fr_batch: 'client.Requirements.UpdateFrBatchAsync',
  req_delete_fr: 'client.Requirements.DeleteFrAsync',
  req_list_tr: 'client.Requirements.ListTrAsync',
  req_create_tr: 'client.Requirements.CreateTrAsync',
  req_create_tr_batch: 'client.Requirements.CreateTrBatchAsync',
  req_update_tr: 'client.Requirements.UpdateTrAsync',
  req_update_tr_batch: 'client.Requirements.UpdateTrBatchAsync',
  req_delete_tr: 'client.Requirements.DeleteTrAsync',
  req_list_test: 'client.Requirements.ListTestAsync',
  req_create_test: 'client.Requirements.CreateTestAsync',
  req_create_test_batch: 'client.Requirements.CreateTestBatchAsync',
  req_update_test: 'client.Requirements.UpdateTestAsync',
  req_update_test_batch: 'client.Requirements.UpdateTestBatchAsync',
  req_delete_test: 'client.Requirements.DeleteTestAsync',
  req_create_batch: 'client.Requirements.CreateBatchAsync',
  req_update_batch: 'client.Requirements.UpdateBatchAsync',
  req_list_mappings: 'client.Requirements.ListMappingsAsync',
  req_create_mapping: 'client.Requirements.UpsertMappingAsync',
  req_delete_mapping: 'client.Requirements.DeleteMappingAsync',
  req_list_layers: 'client.Requirements.ListRequirementLayersAsync',
  req_create_layer: 'client.Requirements.CreateRequirementLayerAsync',
  req_update_layer: 'client.Requirements.UpdateRequirementLayerAsync',
  req_effective: 'client.Requirements.GetEffectiveRequirementsAsync',
  req_generate_document: 'client.Requirements.GenerateAsync',
  req_ingest_document: 'client.Requirements.IngestAsync',
};

const workflowOnlyRequirementsTools = new Set(['req_copy_acceptance_criteria_from_todo']);

const batchRequirementsTools = new Set([
  'req_create_fr_batch',
  'req_update_fr_batch',
  'req_create_tr_batch',
  'req_update_tr_batch',
  'req_create_test_batch',
  'req_update_test_batch',
  'req_create_batch',
  'req_update_batch',
]);

const mutatingRequirementsTools = new Set([
  'req_create_fr',
  'req_create_fr_batch',
  'req_update_fr',
  'req_update_fr_batch',
  'req_delete_fr',
  'req_create_tr',
  'req_create_tr_batch',
  'req_update_tr',
  'req_update_tr_batch',
  'req_delete_tr',
  'req_create_test',
  'req_create_test_batch',
  'req_update_test',
  'req_update_test_batch',
  'req_delete_test',
  'req_create_batch',
  'req_update_batch',
  'req_copy_acceptance_criteria_from_todo',
  'req_create_mapping',
  'req_delete_mapping',
  'req_create_layer',
  'req_update_layer',
  'req_generate_document',
  'req_ingest_document',
]);

const acceptanceCriteriaMutationTools = new Set([
  'req_create_fr',
  'req_update_fr',
  'req_create_tr',
  'req_update_tr',
  'req_create_test',
  'req_update_test',
]);

export function canHandleRequirementsTool(name: string): boolean {
  return name in workflowMethodMap;
}

function stringArg(args: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = args[key];
    if (typeof value === 'string' && value.length > 0) return value;
  }
  return '';
}

function workflowDocType(value: unknown): string {
  switch (value) {
    case 'functional':
      return 'fr';
    case 'technical':
      return 'tr';
    case 'testing':
      return 'test';
    case 'mapping':
      return 'matrix';
    case undefined:
    case '':
      return 'all';
    default:
      return String(value);
  }
}

function typedDocType(value: unknown): string {
  switch (value) {
    case 'fr':
      return 'functional';
    case 'tr':
      return 'technical';
    case 'test':
      return 'testing';
    case 'matrix':
      return 'mapping';
    case undefined:
    case '':
      return 'all';
    default:
      return String(value);
  }
}

function workflowParams(name: string, args: Record<string, unknown>): Record<string, unknown> {
  if (name !== 'req_generate_document') return args;
  return {
    format: typeof args.format === 'string' ? args.format : 'markdown',
    docType: workflowDocType(args.docType),
  };
}

function idParam(args: Record<string, unknown>): Record<string, unknown> {
  return { id: stringArg(args, 'id') };
}

function requestParam(request: Record<string, unknown>): Record<string, unknown> {
  return { request };
}

function optionalScopeParams(args: Record<string, unknown>): Record<string, unknown> {
  return {
    ...(typeof args.scopeStartLayerKey === 'string'
      ? { scopeStartLayerKey: args.scopeStartLayerKey }
      : {}),
    ...(typeof args.scopeEndLayerKey === 'string' ? { scopeEndLayerKey: args.scopeEndLayerKey } : {}),
  };
}

function parseRecordsValue(value: unknown): unknown {
  if (Array.isArray(value)) return value;
  if (typeof value !== 'string' || value.trim().length === 0) return value;

  let parsed: unknown;
  try {
    parsed = yaml.load(value);
  } catch {
    return value;
  }

  if (Array.isArray(parsed)) return parsed;
  if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
    const records = (parsed as { records?: unknown }).records;
    if (Array.isArray(records)) return records;
  }

  return value;
}

function normalizeRequirementArgs(name: string, args: Record<string, unknown>): Record<string, unknown> {
  if (!batchRequirementsTools.has(name)) return args;
  const records = parseRecordsValue(args.records);
  return records === args.records ? args : { ...args, records };
}

function batchRequestParam(args: Record<string, unknown>): Record<string, unknown> {
  return requestParam({ records: Array.isArray(args.records) ? args.records : [] });
}

function listParam(
  args: Record<string, unknown>,
  pluralKey: string,
  singleKey: string,
): string[] {
  const plural = args[pluralKey];
  if (Array.isArray(plural)) return plural.filter((value): value is string => typeof value === 'string');
  const single = args[singleKey];
  return typeof single === 'string' && single.length > 0 ? [single] : [];
}

function typedParams(name: string, args: Record<string, unknown>): Record<string, unknown> {
  switch (name) {
    case 'req_list_fr':
    case 'req_list_tr':
    case 'req_list_test':
    case 'req_list_mappings':
    case 'req_list_layers':
      return {};

    case 'req_get_fr':
    case 'req_get_tr':
    case 'req_get_test':
    case 'req_delete_fr':
    case 'req_delete_tr':
    case 'req_delete_test':
      return idParam(args);

    case 'req_create_fr_batch':
    case 'req_update_fr_batch':
    case 'req_create_tr_batch':
    case 'req_update_tr_batch':
    case 'req_create_test_batch':
    case 'req_update_test_batch':
    case 'req_create_batch':
    case 'req_update_batch':
      return batchRequestParam(args);

    case 'req_create_fr':
    case 'req_create_tr':
      return requestParam({
        id: stringArg(args, 'id'),
        title: stringArg(args, 'title'),
        body: stringArg(args, 'description', 'body'),
        acceptanceCriteria: args.acceptanceCriteria,
        ...optionalScopeParams(args),
      });

    case 'req_update_fr':
    case 'req_update_tr':
      return {
        id: stringArg(args, 'id'),
        request: {
          title: stringArg(args, 'title'),
          body: stringArg(args, 'description', 'body'),
          acceptanceCriteria: args.acceptanceCriteria,
          ...optionalScopeParams(args),
        },
      };

    case 'req_create_test':
      return requestParam({
        id: stringArg(args, 'id'),
        condition: stringArg(args, 'description', 'condition'),
        acceptanceCriteria: args.acceptanceCriteria,
        ...optionalScopeParams(args),
      });

    case 'req_update_test':
      return {
        id: stringArg(args, 'id'),
        request: {
          condition: stringArg(args, 'description', 'condition'),
          acceptanceCriteria: args.acceptanceCriteria,
          ...optionalScopeParams(args),
        },
      };

    case 'req_copy_acceptance_criteria_from_todo':
      return {
        kind: stringArg(args, 'kind'),
        id: stringArg(args, 'id'),
        todoId: stringArg(args, 'todoId'),
      };

    case 'req_create_mapping':
      return {
        frId: stringArg(args, 'frId'),
        request: {
          trIds: listParam(args, 'trIds', 'trId'),
          testIds: listParam(args, 'testIds', 'testId'),
        },
      };

    case 'req_delete_mapping':
      return { frId: stringArg(args, 'frId') };

    case 'req_create_layer':
      return requestParam({
        key: stringArg(args, 'key'),
        order: args.order,
        name: stringArg(args, 'name'),
        description: stringArg(args, 'description'),
        ...(typeof args.scopeEndLayerKey === 'string'
          ? { scopeEndLayerKey: args.scopeEndLayerKey }
          : {}),
      });

    case 'req_update_layer':
      return {
        key: stringArg(args, 'key'),
        request: {
          name: stringArg(args, 'name'),
          description: stringArg(args, 'description'),
          ...(typeof args.scopeEndLayerKey === 'string'
            ? { scopeEndLayerKey: args.scopeEndLayerKey }
            : {}),
        },
      };

    case 'req_effective':
      return typeof args.layerKey === 'string' ? { layerKey: args.layerKey } : {};

    case 'req_generate_document':
      return {
        doc: typedDocType(args.docType),
        format: typeof args.format === 'string' ? args.format : 'markdown',
      };

    case 'req_ingest_document': {
      const documents = args.documents;
      if (documents && typeof documents === 'object' && !Array.isArray(documents)) {
        return requestParam({
          sourceFormat: typeof args.sourceFormat === 'string' ? args.sourceFormat : 'wiki',
          ...(typeof args.preferredWikiFormat === 'string'
            ? { preferredWikiFormat: args.preferredWikiFormat }
            : {}),
          documents,
        });
      }
      const content = stringArg(args, 'content');
      return requestParam({
        functionalMarkdown: content,
        technicalMarkdown: content,
        testingMarkdown: content,
        mappingMarkdown: content,
      });
    }

    default:
      return args;
  }
}

function isEmptyResult(response: ReplResponse): boolean {
  if (!Object.prototype.hasOwnProperty.call(response.payload, 'result')) return false;
  const result = (response.payload as { result?: unknown }).result;
  return (
    result === undefined ||
    result === null ||
    (typeof result === 'object' && !Array.isArray(result) && Object.keys(result).length === 0)
  );
}

function hasSuppliedAcceptanceCriteria(name: string, args: Record<string, unknown>): boolean {
  return acceptanceCriteriaMutationTools.has(name) && Array.isArray(args.acceptanceCriteria);
}

function valueHasExplicitEmptyAcceptanceCriteria(value: unknown): boolean {
  if (!value || typeof value !== 'object') return false;
  if (Array.isArray(value)) return value.some(valueHasExplicitEmptyAcceptanceCriteria);

  const record = value as Record<string, unknown>;
  if (
    Object.prototype.hasOwnProperty.call(record, 'acceptanceCriteria') &&
    Array.isArray(record.acceptanceCriteria) &&
    record.acceptanceCriteria.length === 0
  ) {
    return true;
  }

  return ['result', 'item', 'data', 'requirement'].some((key) =>
    valueHasExplicitEmptyAcceptanceCriteria(record[key]),
  );
}

function assertAcceptanceCriteriaCaptured(
  name: string,
  args: Record<string, unknown>,
  response: ReplResponse,
): void {
  if (!hasSuppliedAcceptanceCriteria(name, args)) return;
  if (!valueHasExplicitEmptyAcceptanceCriteria(response.payload)) return;

  throw new Error(
    'requirements_acceptance_criteria_not_captured: acceptanceCriteria was supplied but the mutation response returned an empty acceptanceCriteria list.',
  );
}

function normalizeGenerateResponse(response: ReplResponse): ReplResponse {
  const result = (response.payload as { result?: Record<string, unknown> }).result;
  if (!result) return response;

  const content = result.content;
  if (!Array.isArray(content) || !content.every((value) => typeof value === 'number')) {
    return response;
  }

  const bytes = Buffer.from(content as number[]);
  const contentType = typeof result.contentType === 'string' ? result.contentType : 'text/markdown';
  const fileName = typeof result.fileName === 'string' ? result.fileName : '';
  const isZip = /zip/i.test(contentType) || /\.zip$/i.test(fileName);

  return {
    type: 'result',
    payload: {
      ...response.payload,
      result: {
        ...result,
        ...(isZip
          ? {
              contentBase64: bytes.toString('base64'),
              fileName: fileName || 'requirements-documents.zip',
            }
          : { content: bytes.toString('utf8') }),
        contentType,
        format: typeof result.format === 'string' ? result.format : 'markdown',
        docType: typeof result.docType === 'string' ? result.docType : 'all',
        generatedAt:
          typeof result.generatedAt === 'string'
            ? result.generatedAt
            : new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'),
      },
    },
  };
}

function isWikiGenerate(name: string, args: Record<string, unknown>): boolean {
  return name === 'req_generate_document' && (typeof args.format === 'string' ? args.format : 'markdown') === 'wiki';
}

function hasZipContent(response: ReplResponse): boolean {
  const result = (response.payload as { result?: Record<string, unknown> }).result;
  if (!result) return false;
  const contentType = typeof result.contentType === 'string' ? result.contentType : '';
  const fileName = typeof result.fileName === 'string' ? result.fileName : '';
  return typeof result.contentBase64 === 'string' && (/zip/i.test(contentType) || /\.zip$/i.test(fileName));
}

async function generateDocumentHttpFallback(
  args: Record<string, unknown>,
): Promise<ReplResponse | null> {
  const format = typeof args.format === 'string' ? args.format : 'markdown';
  if (format !== 'wiki') return null;

  const fetchFn = globalThis.fetch;
  const apiKey = process.env.MCPSERVER_API_KEY;
  const workspacePath = process.env.MCPSERVER_WORKSPACE_PATH ?? process.env.MCP_WORKSPACE_PATH;
  const baseUrl = process.env.MCPSERVER_BASE_URL ?? process.env.MCP_SERVER_URL;
  if (typeof fetchFn !== 'function' || !apiKey || !workspacePath || !baseUrl) return null;

  const docType = typedDocType(args.docType);
  const url = `${baseUrl.replace(/\/$/, '')}/mcpserver/requirements/generate?doc=${encodeURIComponent(
    docType,
  )}&format=${encodeURIComponent(format)}`;

  const response = await fetchFn(url, {
    headers: {
      'X-Api-Key': apiKey,
      'X-Workspace-Path': workspacePath,
    },
  });
  if (!response.ok) {
    const body = await response.text().catch(() => '');
    return {
      type: 'error',
      payload: {
        code: 'http_error',
        message: `requirements generate HTTP fallback returned HTTP ${response.status}${body ? `: ${body}` : ''}`,
      },
    };
  }

  const contentType = response.headers.get('content-type')?.split(';')[0] || 'application/zip';
  const contentBase64 = Buffer.from(await response.arrayBuffer()).toString('base64');
  return {
    type: 'result',
    payload: {
      result: {
        contentBase64,
        contentType,
        ...(/zip/i.test(contentType) ? { fileName: 'requirements-wiki-documents.zip' } : {}),
        format,
        docType,
        generatedAt: new Date().toISOString().replace(/\.\d{3}Z$/, 'Z'),
      },
    },
  };
}

async function invokeRequirementsTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
): Promise<ReplResponse> {
  const workflowMethod = workflowMethodMap[name];
  if (!workflowMethod) throw new Error(`Unknown requirements tool: ${name}`);

  if (isWikiGenerate(name, args)) {
    const httpResponse = await generateDocumentHttpFallback(args);
    if (httpResponse) return httpResponse;
  }

  const workflowResponse = await bridge.invoke(workflowMethod, workflowParams(name, args));
  if (workflowResponse.type !== 'error' && !isEmptyResult(workflowResponse)) {
    if (isWikiGenerate(name, args)) {
      const normalized = normalizeGenerateResponse(workflowResponse);
      if (hasZipContent(normalized)) return normalized;
    } else {
      assertAcceptanceCriteriaCaptured(name, args, workflowResponse);
      return workflowResponse;
    }
  }

  if (workflowOnlyRequirementsTools.has(name)) {
    return workflowResponse;
  }

  const typedMethod = typedMethodMap[name];
  const typedResponse = await bridge.invoke(typedMethod, typedParams(name, args));
  if (typedResponse.type !== 'error' && !isEmptyResult(typedResponse)) {
    if (name === 'req_generate_document') {
      const normalized = normalizeGenerateResponse(typedResponse);
      if (!isWikiGenerate(name, args) || hasZipContent(normalized)) return normalized;
    } else {
      assertAcceptanceCriteriaCaptured(name, args, typedResponse);
      return typedResponse;
    }
  }

  if (name === 'req_generate_document') {
    const httpResponse = await generateDocumentHttpFallback(args);
    if (httpResponse) return httpResponse;
  }

  return typedResponse;
}

export async function handleRequirementsTool(
  name: string,
  args: Record<string, unknown>,
  bridge: ReplBridge,
) {
  const method = workflowMethodMap[name];
  const normalizedArgs = normalizeRequirementArgs(name, args);
  const failsafePath = mutatingRequirementsTools.has(name) && method ? await cacheWrite(method, normalizedArgs) : undefined;
  let response: ReplResponse;
  try {
    response = await invokeRequirementsTool(name, normalizedArgs, bridge);
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
