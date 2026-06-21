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
var __exportStar = (this && this.__exportStar) || function(m, exports) {
    for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.invokeMcpMethod = exports.ReplClient = exports.McpAgentClient = exports.ReplBridge = void 0;
/**
 * @sharpninja/mcp-repl
 * The single shared TypeScript surface for McpServer agent plugins.
 * Used by Cline, Cline V2, OpenCode, and future TS-based plugins.
 */
__exportStar(require("./types"), exports);
var ReplBridge_1 = require("./transport/ReplBridge");
Object.defineProperty(exports, "ReplBridge", { enumerable: true, get: function () { return ReplBridge_1.ReplBridge; } });
__exportStar(require("./discovery/MarkerResolver"), exports);
__exportStar(require("./cache/CacheManager"), exports);
var McpAgentClient_1 = require("./client/McpAgentClient");
Object.defineProperty(exports, "McpAgentClient", { enumerable: true, get: function () { return McpAgentClient_1.McpAgentClient; } });
var ReplClient_1 = require("./client/ReplClient");
Object.defineProperty(exports, "ReplClient", { enumerable: true, get: function () { return ReplClient_1.ReplClient; } });
Object.defineProperty(exports, "invokeMcpMethod", { enumerable: true, get: function () { return ReplClient_1.invokeMcpMethod; } });
