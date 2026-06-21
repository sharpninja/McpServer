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
exports.ReplBridge = void 0;
const child_process_1 = require("child_process");
const readline_1 = require("readline");
const yaml = __importStar(require("js-yaml"));
/**
 * Persistent bridge to mcpserver-repl --agent-stdio.
 * Multiplexes concurrent JSON-over-STDIO requests by requestId.
 */
class ReplBridge {
    proc = null;
    pending = new Map();
    buffer = '';
    docBuffer = '';
    /** Generate a request ID matching ^req-\d{8}T\d{6}Z-[a-z0-9]+$ */
    static generateRequestId(slug = 'req') {
        const now = new Date();
        const ts = now.getUTCFullYear().toString() +
            (now.getUTCMonth() + 1).toString().padStart(2, '0') +
            now.getUTCDate().toString().padStart(2, '0') +
            'T' +
            now.getUTCHours().toString().padStart(2, '0') +
            now.getUTCMinutes().toString().padStart(2, '0') +
            now.getUTCSeconds().toString().padStart(2, '0') +
            'Z';
        const rand = Math.floor(Math.random() * 0xffff)
            .toString(16)
            .padStart(4, '0');
        // Ensure slug only contains lowercase alphanumeric characters
        const safeSlug = slug.toLowerCase().replace(/[^a-z0-9]/g, '') || 'req';
        return `req-${ts}-${safeSlug}-${rand}`;
    }
    /** Ensure the REPL process is running, restarting if it crashed. */
    async ensure() {
        if (this.proc && this.proc.exitCode === null && !this.proc.killed) {
            return;
        }
        this.proc = (0, child_process_1.spawn)('mcpserver-repl', ['--agent-stdio'], {
            stdio: ['pipe', 'pipe', 'pipe'],
            env: { ...process.env },
        });
        this.proc.stderr?.on('data', (data) => {
            process.stderr.write(`[repl] ${data}`);
        });
        const rl = (0, readline_1.createInterface)({ input: this.proc.stdout });
        rl.on('line', (line) => this.onLine(line));
        this.proc.on('exit', (code) => {
            process.stderr.write(`[repl] mcpserver-repl exited with code ${code}\n`);
            // Reject all pending requests
            for (const [, req] of this.pending) {
                if (req.timer)
                    clearTimeout(req.timer);
                req.reject(new Error(`mcpserver-repl exited with code ${code}`));
            }
            this.pending.clear();
            this.proc = null;
        });
    }
    terminateAfterTimeout(message, exceptRequestId) {
        const proc = this.proc;
        this.proc = null;
        this.docBuffer = '';
        for (const [requestId, req] of this.pending) {
            if (requestId === exceptRequestId)
                continue;
            if (req.timer)
                clearTimeout(req.timer);
            req.reject(new Error(message));
        }
        this.pending.clear();
        if (!proc || proc.exitCode !== null || proc.killed) {
            return;
        }
        proc.kill('SIGTERM');
        setTimeout(() => {
            if (proc.exitCode === null && !proc.killed) {
                proc.kill('SIGKILL');
            }
        }, 2000).unref();
    }
    onLine(line) {
        if (line === '---') {
            // YAML document separator — parse accumulated buffer
            if (this.docBuffer.trim()) {
                this.parseDocument(this.docBuffer);
            }
            this.docBuffer = '';
        }
        else {
            this.docBuffer += line + '\n';
        }
    }
    parseDocument(raw) {
        let doc;
        try {
            doc = yaml.load(raw);
        }
        catch {
            process.stderr.write(`[repl] Failed to parse REPL response: ${raw}\n`);
            return;
        }
        const type = doc.type;
        const payload = doc.payload;
        if (!payload)
            return;
        const requestId = payload.requestId;
        const response = { type: type, payload };
        if (!requestId) {
            // Broadcast event with no specific request ID
            return;
        }
        const pending = this.pending.get(requestId);
        if (!pending)
            return;
        if (type === 'event') {
            pending.events.push(response);
            pending.onEvent?.(response);
        }
        else {
            // Final result or error
            this.pending.delete(requestId);
            if (pending.timer)
                clearTimeout(pending.timer);
            pending.resolve(response);
        }
    }
    /**
     * Send a single-line JSON envelope and await the matching result/error envelope.
     */
    async invoke(method, params) {
        await this.ensure();
        const requestId = ReplBridge.generateRequestId(method.split('.').pop() ?? 'req');
        return new Promise((resolve, reject) => {
            const timeoutMs = Number(process.env.MCPSERVER_REPL_TIMEOUT_MS ?? '15000');
            const timer = setTimeout(() => {
                this.pending.delete(requestId);
                const message = `mcpserver-repl timed out after ${timeoutMs}ms for ${method}`;
                this.terminateAfterTimeout(message, requestId);
                reject(new Error(message));
            }, timeoutMs);
            this.pending.set(requestId, { resolve, reject, events: [], timer });
            const envelope = {
                type: 'request',
                payload: {
                    requestId,
                    method,
                    ...(params ? { params } : {}),
                },
            };
            this.proc.stdin.write(`${JSON.stringify(envelope)}\n`);
        });
    }
    /**
     * Invoke a streaming method, calling onEvent for each progress event.
     */
    async invokeStreaming(method, params, onEvent) {
        await this.ensure();
        const requestId = ReplBridge.generateRequestId(method.split('.').pop() ?? 'stream');
        return new Promise((resolve, reject) => {
            const timeoutMs = Number(process.env.MCPSERVER_REPL_TIMEOUT_MS ?? '15000');
            const timer = setTimeout(() => {
                this.pending.delete(requestId);
                const message = `mcpserver-repl timed out after ${timeoutMs}ms for ${method}`;
                this.terminateAfterTimeout(message, requestId);
                reject(new Error(message));
            }, timeoutMs);
            this.pending.set(requestId, { resolve, reject, events: [], onEvent, timer });
            const envelope = {
                type: 'request',
                payload: { requestId, method, params },
            };
            this.proc.stdin.write(`${JSON.stringify(envelope)}\n`);
        });
    }
    /** Gracefully terminate the REPL process. */
    async close() {
        if (this.proc) {
            this.proc.stdin?.end();
            await new Promise((resolve) => {
                this.proc.on('exit', () => resolve());
                setTimeout(resolve, 2000);
            });
            if (!this.proc?.killed)
                this.proc?.kill();
            this.proc = null;
        }
    }
}
exports.ReplBridge = ReplBridge;
