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
export type McpEnvelope = {
    type: 'request';
    payload: McpRequest;
} | {
    type: 'result';
    payload: McpResult;
} | {
    type: 'error';
    payload: McpError;
} | {
    type: 'event';
    payload: McpEvent;
};
export interface ReplResponse {
    success: boolean;
    requestId: string;
    output: string;
    parsed?: McpResult | McpError | McpEvent;
    error?: McpError;
}
export declare const WorkflowMethods: {
    readonly sessionlog: {
        readonly openSession: "workflow.sessionlog.openSession";
        readonly beginTurn: "workflow.sessionlog.beginTurn";
        readonly updateTurn: "workflow.sessionlog.updateTurn";
        readonly completeTurn: "workflow.sessionlog.completeTurn";
        readonly failTurn: "workflow.sessionlog.failTurn";
        readonly appendDialog: "workflow.sessionlog.appendDialog";
        readonly appendActions: "workflow.sessionlog.appendActions";
        readonly queryHistory: "workflow.sessionlog.queryHistory";
    };
    readonly todo: {
        readonly query: "workflow.todo.query";
        readonly get: "workflow.todo.get";
        readonly create: "workflow.todo.create";
        readonly update: "workflow.todo.update";
        readonly delete: "workflow.todo.delete";
        readonly select: "workflow.todo.select";
        readonly updateSelected: "workflow.todo.updateSelected";
        readonly streamStatus: "workflow.todo.streamStatus";
        readonly streamPlan: "workflow.todo.streamPlan";
        readonly streamImplement: "workflow.todo.streamImplement";
        readonly analyzeRequirements: "workflow.todo.analyzeRequirements";
    };
    readonly memory: {
        readonly list: "workflow.memory.list";
        readonly get: "workflow.memory.get";
        readonly add: "workflow.memory.add";
        readonly update: "workflow.memory.update";
        readonly remove: "workflow.memory.remove";
    };
    readonly requirements: {
        readonly listFr: "workflow.requirements.listFr";
        readonly getFr: "workflow.requirements.getFr";
        readonly createFr: "workflow.requirements.createFr";
    };
    readonly graphrag: {
        readonly status: "workflow.graphrag.status";
        readonly index: "workflow.graphrag.index";
        readonly query: "workflow.graphrag.query";
        readonly ingest: "workflow.graphrag.ingest";
        readonly doc_list: "workflow.graphrag.documents.list";
        readonly doc_chunks: "workflow.graphrag.documents.chunks";
        readonly doc_delete: "workflow.graphrag.documents.delete";
        readonly entity_create: "workflow.graphrag.entities.create";
        readonly entity_list: "workflow.graphrag.entities.list";
        readonly entity_get: "workflow.graphrag.entities.get";
        readonly entity_update: "workflow.graphrag.entities.update";
        readonly entity_delete: "workflow.graphrag.entities.delete";
        readonly rel_create: "workflow.graphrag.relationships.create";
        readonly rel_list: "workflow.graphrag.relationships.list";
        readonly rel_get: "workflow.graphrag.relationships.get";
        readonly rel_update: "workflow.graphrag.relationships.update";
        readonly rel_delete: "workflow.graphrag.relationships.delete";
    };
};
export type WorkflowMethod = typeof WorkflowMethods[keyof typeof WorkflowMethods][keyof typeof WorkflowMethods[keyof typeof WorkflowMethods]];
