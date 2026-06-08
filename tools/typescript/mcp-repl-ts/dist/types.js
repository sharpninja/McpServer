"use strict";
/**
 * Shared TypeScript types for the McpServer REPL protocol.
 * Mirrors the JSON schema (schemas/repl-yaml-message.schema.json) and the PowerShell McpRepl entities.
 * Used as the common surface for Cline, Cline V2, OpenCode, and future TS plugins.
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.WorkflowMethods = void 0;
// Common workflow namespaces (extracted from the duplicated tool maps)
exports.WorkflowMethods = {
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
    memory: {
        list: 'workflow.memory.list',
        get: 'workflow.memory.get',
        add: 'workflow.memory.add',
        update: 'workflow.memory.update',
        remove: 'workflow.memory.remove',
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
};
