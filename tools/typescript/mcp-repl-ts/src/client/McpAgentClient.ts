import { ReplBridge } from '../transport/ReplBridge';
import type { WorkflowMethods } from '../types';

/**
 * High-level client that the three plugins (and future ones) should use.
 * All common workflow logic lives here.
 */
export class McpAgentClient {
  private bridge: ReplBridge;

  constructor(private workspacePath: string) {
    this.bridge = new ReplBridge();
  }

  async ensureConnected() {
    await this.bridge.ensure();
  }

  private send(method: string, params: Record<string, unknown>) {
    return this.bridge.invoke(method, params);
  }

  // Example namespaces (to be fully implemented with typed methods)
  get session() {
    return {
      beginTurn: (params: any) => this.send('workflow.sessionlog.beginTurn', params),
      // ... all other session methods
    };
  }

  get todo() {
    return {
      create: (params: any) => this.send('workflow.todo.create', params),
      // ...
    };
  }

  get memory() {
    return {
      list: (params: any) => this.send('workflow.memory.list', params),
      get: (params: any) => this.send('workflow.memory.get', params),
      add: (params: any) => this.send('workflow.memory.add', params),
      update: (params: any) => this.send('workflow.memory.update', params),
      remove: (params: any) => this.send('workflow.memory.remove', params),
    };
  }

  get requirements() {
    return {
      createTest: (params: any) => this.send('workflow.requirements.createTest', params),
      // ...
    };
  }

  get graphrag() {
    return {
      query: (params: any) => this.send('workflow.graphrag.query', params),
      // ...
    };
  }

  // Convenience for raw access during migration
  get raw() {
    return this.bridge;
  }
}
