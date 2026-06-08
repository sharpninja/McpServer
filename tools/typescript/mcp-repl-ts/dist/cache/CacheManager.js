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
exports.cacheWrite = cacheWrite;
exports.cacheDelete = cacheDelete;
exports.cacheStatus = cacheStatus;
exports.cacheFlush = cacheFlush;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const yaml = __importStar(require("js-yaml"));
const MAX_RETRIES = 3;
/**
 * Returns Base64URL encoding of workspacePath, matching V4CacheManager.GetScopedCachePath
 * in @sharpninja/mcpserver-agent-core (TR-MCP-AGENT-PARITY-013).
 */
function getWorkspaceKeyV4(workspacePath) {
    return Buffer.from(workspacePath)
        .toString('base64')
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}
function getPendingDir() {
    if (process.env.MCPSERVER_FAILSAFE_DIR) {
        return process.env.MCPSERVER_FAILSAFE_DIR;
    }
    const workspacePath = process.env.MCPSERVER_WORKSPACE_PATH ?? process.env.MCP_WORKSPACE_PATH;
    if (workspacePath) {
        const key = getWorkspaceKeyV4(workspacePath);
        return path.join(workspacePath, '.mcpServer', 'failsafe', 'cline', 'workspaces', key);
    }
    const cacheDir = process.env.MCPSERVER_CACHE_DIR ??
        path.join(process.cwd(), 'cache');
    return path.join(cacheDir, 'pending');
}
function ensurePendingDir(pendingDir) {
    fs.mkdirSync(pendingDir, { recursive: true });
}
/**
 * Write a pending REPL command before attempting the live MCP call.
 * Mirrors cache_write() in lib/cache-manager.sh.
 */
async function cacheWrite(method, params = {}) {
    const pendingDir = getPendingDir();
    ensurePendingDir(pendingDir);
    const existing = fs
        .readdirSync(pendingDir)
        .filter((f) => f.endsWith('.yaml'));
    const seq = (existing.length + 1).toString().padStart(3, '0');
    const slug = method.replace(/\./g, '-');
    const filename = `${seq}-${slug}.yaml`;
    const filepath = path.join(pendingDir, filename);
    const entry = {
        id: seq,
        timestamp: new Date().toISOString(),
        method,
        params,
        retryCount: 0,
    };
    fs.writeFileSync(filepath, yaml.dump(entry));
    return filepath;
}
/** Delete a pending command after the server acknowledges it. */
async function cacheDelete(filepath) {
    if (filepath && fs.existsSync(filepath)) {
        fs.unlinkSync(filepath);
    }
}
/**
 * Return the count of pending YAML files.
 * Mirrors cache_status() in lib/cache-manager.sh.
 */
async function cacheStatus() {
    const pendingDir = getPendingDir();
    ensurePendingDir(pendingDir);
    return fs.readdirSync(pendingDir).filter((f) => f.endsWith('.yaml')).length;
}
/**
 * Replay all pending commands via bridge.invoke(), deleting on success
 * and incrementing retryCount on failure (max MAX_RETRIES).
 * Mirrors cache_flush() in lib/cache-manager.sh.
 */
async function cacheFlush(bridge) {
    const pendingDir = getPendingDir();
    ensurePendingDir(pendingDir);
    const files = fs
        .readdirSync(pendingDir)
        .filter((f) => f.endsWith('.yaml'))
        .sort()
        .map((f) => path.join(pendingDir, f));
    let flushed = 0;
    let failed = 0;
    for (const file of files) {
        if (!fs.existsSync(file))
            continue;
        let entry;
        try {
            entry = yaml.load(fs.readFileSync(file, 'utf8'));
        }
        catch {
            continue;
        }
        if ((entry.retryCount ?? 0) >= MAX_RETRIES)
            continue;
        try {
            const response = await bridge.invoke(entry.method, entry.params);
            if (response.type === 'error') {
                const payload = response.payload;
                throw new Error(`${payload.code ?? 'error'}: ${payload.message ?? 'Unknown error'}`);
            }
            fs.unlinkSync(file);
            flushed++;
        }
        catch {
            entry.retryCount = (entry.retryCount ?? 0) + 1;
            fs.writeFileSync(file, yaml.dump(entry));
            failed++;
        }
    }
    const pending = await cacheStatus();
    return { flushed, failed, pending };
}
