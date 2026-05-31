/**
 * Shared TypeScript types for the McpServer REPL protocol.
 * Mirrors the JSON schema (schemas/repl-yaml-message.schema.json) and the PowerShell McpRepl entities.
 * Used as the common surface for Cline, Cline V2, OpenCode, and future TS plugins.
 */

export interface McpRequest<TParams = Record<string, any>> {
  requestId: string;
  method: string;
  params?: TParams;
}

export interface McpResult<TResult = any> {
  requestId: string;
  result: TResult;
}

export interface McpError {
  requestId: string;
  code: string;
  message: string;
  details?: any;
}

export interface McpEvent<TData = any> {
  event: string;
  data: TData;
  sequence?: number;
}

export type McpEnvelope =
  | { type: 'request'; payload: McpRequest }
  | { type: 'result'; payload: McpResult }
  | { type: 'error'; payload: McpError }
  | { type: 'event'; payload: McpEvent };

export interface ReplResponse {
  success: boolean;
  requestId: string;
  output: string;
  parsed?: McpResult | McpError | McpEvent;
  error?: McpError;
}

// Common workflow namespaces (extracted from the duplicated tool maps)
export const WorkflowMethods = {
  sessionlog: {
    openSession: 'workflow.sessionlog.openSession',
    beginTurn: 'workflow.sessionlog.beginTurn',
    updateTurn: 'workflow.sessionlog.updateTurn',
    completeTurn: 'workflow.sessionlog.completeTurn',
    failTurn: 'workflow.sessionlog.failTurn',
    appendDialog: 'workflow.sessionlog.appendDialog',
    appendActions: 'workflow.sessionlog.appendActions',
    queryHistory: 'workflow.sessionlog.queryHistory',
  },
  todo: {
    query: 'workflow.todo.query',
    get: 'workflow.todo.get',
    create: 'workflow.todo.create',
    update: 'workflow.todo.update',
    delete: 'workflow.todo.delete',
    select: 'workflow.todo.select',
    updateSelected: 'workflow.todo.updateSelected',
    streamStatus: 'workflow.todo.streamStatus',
    streamPlan: 'workflow.todo.streamPlan',
    streamImplement: 'workflow.todo.streamImplement',
    analyzeRequirements: 'workflow.todo.analyzeRequirements',
  },
  requirements: {
    listFr: 'workflow.requirements.listFr',
    getFr: 'workflow.requirements.getFr',
    createFr: 'workflow.requirements.createFr',
    // full set omitted for brevity in this initial shared surface
  },
  graphrag: {
    status: 'workflow.graphrag.status',
    index: 'workflow.graphrag.index',
    query: 'workflow.graphrag.query',
    ingest: 'workflow.graphrag.ingest',
    doc_list: 'workflow.graphrag.documents.list',
    doc_chunks: 'workflow.graphrag.documents.chunks',
    doc_delete: 'workflow.graphrag.documents.delete',
    entity_create: 'workflow.graphrag.entities.create',
    entity_list: 'workflow.graphrag.entities.list',
    entity_get: 'workflow.graphrag.entities.get',
    entity_update: 'workflow.graphrag.entities.update',
    entity_delete: 'workflow.graphrag.entities.delete',
    rel_create: 'workflow.graphrag.relationships.create',
    rel_list: 'workflow.graphrag.relationships.list',
    rel_get: 'workflow.graphrag.relationships.get',
    rel_update: 'workflow.graphrag.relationships.update',
    rel_delete: 'workflow.graphrag.relationships.delete',
  },
} as const;

export type WorkflowMethod = typeof WorkflowMethods[keyof typeof WorkflowMethods][keyof typeof WorkflowMethods[keyof typeof WorkflowMethods]];