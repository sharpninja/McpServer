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
    await this.bridge.ensure(this.workspacePath);
  }

  // Example namespaces (to be fully implemented with typed methods)
  get session() {
    return {
      beginTurn: (params: any) => this.bridge.send('workflow.sessionlog.beginTurn', params),
      // ... all other session methods
    };
  }

  get todo() {
    return {
      create: (params: any) => this.bridge.send('workflow.todo.create', params),
      // ...
    };
  }

  get requirements() {
    return {
      createTest: (params: any) => this.bridge.send('workflow.requirements.createTest', params),
      // ...
    };
  }

  get graphrag() {
    return {
      query: (params: any) => this.bridge.send('workflow.graphrag.query', params),
      // ...
    };
  }

  // Convenience for raw access during migration
  get raw() {
    return this.bridge;
  }
}