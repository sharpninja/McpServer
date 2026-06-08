"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.ReplClient = void 0;
exports.invokeMcpMethod = invokeMcpMethod;
const child_process_1 = require("child_process");
const yaml = __importStar(require("js-yaml"));
/**
 * Shared ReplClient for talking to mcpserver-repl --agent-stdio.
 * Provides typed request/response and automatic envelope (de)serialization.
 * This is the core of the shared TS surface.
 */
class ReplClient {
    workspacePath;
    proc = null;
    timeoutMs;
    constructor(workspacePath, timeoutMs = 45000) {
        this.workspacePath = workspacePath;
        this.timeoutMs = timeoutMs;
    }
    async connect() {
        if (this.proc)
            return;
        this.proc = (0, child_process_1.spawn)('mcpserver-repl', ['--agent-stdio'], {
            cwd: this.workspacePath,
            stdio: ['pipe', 'pipe', 'pipe'],
            env: { ...process.env, MCP_WORKSPACE_PATH: this.workspacePath },
        });
        // Basic stderr logging (non-blocking)
        this.proc.stderr?.on('data', (d) => {
            // In production, route to proper logger
            if (process.env.DEBUG_REPL)
                console.error('[mcp-repl stderr]', d.toString());
        });
    }
    async sendRequest(method, params) {
        await this.connect();
        const requestId = `req-${new Date().toISOString().replace(/[-:]/g, '').split('.')[0]}Z-${Math.random().toString(16).slice(2, 6)}`;
        const envelope = {
            type: 'request',
            payload: {
                requestId,
                method,
                ...(params ? { params } : {}),
            },
        };
        const yamlStr = yaml.dump(envelope, { noRefs: true, skipInvalid: true });
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                reject(new Error(`REPL request ${method} timed out after ${this.timeoutMs}ms`));
            }, this.timeoutMs);
            let stdout = '';
            const onData = (data) => {
                stdout += data.toString();
                // Heuristic: responses are usually one document
                if (stdout.includes('type:') && (stdout.includes('result:') || stdout.includes('error:'))) {
                    this.proc?.stdout?.off('data', onData);
                    clearTimeout(timeout);
                    try {
                        const parsed = yaml.load(stdout);
                        const success = parsed?.type === 'result' || !parsed?.error;
                        resolve({
                            success,
                            requestId,
                            output: stdout,
                            parsed: success ? parsed.payload : parsed.payload,
                        });
                    }
                    catch (e) {
                        resolve({ success: false, requestId, output: stdout });
                    }
                }
            };
            this.proc?.stdout?.on('data', onData);
            this.proc?.stdin?.write(yamlStr + '\n');
        });
    }
    async close() {
        if (this.proc) {
            this.proc.kill();
            this.proc = null;
        }
    }
}
exports.ReplClient = ReplClient;
// Convenience factory (matches the style of the PS New-McpRequest + Invoke)
async function invokeMcpMethod(workspacePath, method, params) {
    const client = new ReplClient(workspacePath);
    try {
        const resp = await client.sendRequest(method, params);
        if (!resp.success)
            return resp.parsed;
        return resp.parsed;
    }
    finally {
        await client.close();
    }
}
