import { ReplBridge } from '../transport/ReplBridge';
/**
 * High-level client that the three plugins (and future ones) should use.
 * All common workflow logic lives here.
 */
export declare class McpAgentClient {
    private workspacePath;
    private bridge;
    constructor(workspacePath: string);
    ensureConnected(): Promise<void>;
    private send;
    get session(): {
        beginTurn: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
    };
    get todo(): {
        create: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
    };
    get memory(): {
        list: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
        get: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
        add: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
        update: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
        remove: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
    };
    get requirements(): {
        createTest: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
    };
    get graphrag(): {
        query: (params: any) => Promise<import("../transport/ReplBridge").ReplResponse>;
    };
    get raw(): ReplBridge;
}
