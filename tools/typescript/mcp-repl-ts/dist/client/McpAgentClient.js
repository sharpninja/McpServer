"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.McpAgentClient = void 0;
const ReplBridge_1 = require("../transport/ReplBridge");
/**
 * High-level client that the three plugins (and future ones) should use.
 * All common workflow logic lives here.
 */
class McpAgentClient {
    workspacePath;
    bridge;
    constructor(workspacePath) {
        this.workspacePath = workspacePath;
        this.bridge = new ReplBridge_1.ReplBridge();
    }
    async ensureConnected() {
        await this.bridge.ensure();
    }
    send(method, params) {
        return this.bridge.invoke(method, params);
    }
    // Example namespaces (to be fully implemented with typed methods)
    get session() {
        return {
            beginTurn: (params) => this.send('workflow.sessionlog.beginTurn', params),
            // ... all other session methods
        };
    }
    get todo() {
        return {
            create: (params) => this.send('workflow.todo.create', params),
            // ...
        };
    }
    get memory() {
        return {
            list: (params) => this.send('workflow.memory.list', params),
            get: (params) => this.send('workflow.memory.get', params),
            add: (params) => this.send('workflow.memory.add', params),
            update: (params) => this.send('workflow.memory.update', params),
            remove: (params) => this.send('workflow.memory.remove', params),
        };
    }
    get requirements() {
        return {
            createTest: (params) => this.send('workflow.requirements.createTest', params),
            // ...
        };
    }
    get graphrag() {
        return {
            query: (params) => this.send('workflow.graphrag.query', params),
            // ...
        };
    }
    // Convenience for raw access during migration
    get raw() {
        return this.bridge;
    }
}
exports.McpAgentClient = McpAgentClient;
