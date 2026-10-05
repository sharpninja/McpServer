"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.resolveReplCommand = resolveReplCommand;
const fs_1 = require("fs");
const path_1 = require("path");
/**
 * Prefer an explicit override, then qbrain-ai-repl, then the 1.x mcpserver-repl command.
 * Does not download either tool.
 */
function resolveReplCommand() {
    const explicit = (process.env.MCPSERVER_REPL_BIN || process.env.MCPSERVER_REPL_COMMAND || '').trim();
    if (explicit)
        return explicit;
    return findInstalledRepl('qbrain-ai-repl') || findInstalledRepl('mcpserver-repl') || 'qbrain-ai-repl';
}
function findInstalledRepl(name) {
    const entries = (process.env.PATH || '').split(path_1.delimiter).filter(Boolean);
    const extensions = process.platform === 'win32' ? ['.cmd', '.exe', '.bat', ''] : [''];
    for (const dir of entries) {
        for (const ext of extensions) {
            if ((0, fs_1.existsSync)((0, path_1.join)(dir, name + ext)))
                return name;
        }
    }
    return undefined;
}
