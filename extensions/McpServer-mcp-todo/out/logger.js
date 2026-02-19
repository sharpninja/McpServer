"use strict";
/**
 * Output channel and trace logger for McpServer MCP Todo extension.
 * All interactions are logged so they appear in Output → McpServer MCP Todo.
 * Copilot interactions are logged to a separate Output → McpServer Copilot channel.
 */
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
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.log = log;
exports.show = show;
exports.copilotLog = copilotLog;
exports.showCopilot = showCopilot;
const vscode = __importStar(require("vscode"));
const channelName = 'McpServer MCP Todo';
let _channel;
const copilotChannelName = 'McpServer Copilot';
let _copilotChannel;
function channel() {
    if (!_channel) {
        _channel = vscode.window.createOutputChannel(channelName);
    }
    return _channel;
}
function copilotChannel() {
    if (!_copilotChannel) {
        _copilotChannel = vscode.window.createOutputChannel(copilotChannelName);
    }
    return _copilotChannel;
}
function timestamp() {
    return new Date().toISOString();
}
function log(message, ...args) {
    const line = args.length ? `${timestamp()} ${message} ${args.map((a) => JSON.stringify(a)).join(' ')}` : `${timestamp()} ${message}`;
    const ch = channel();
    ch.appendLine(line);
    // Also echo to console so it appears in Extension Host output if Output panel is on wrong channel
    console.log(`[McpServer MCP Todo] ${line}`);
}
function show(preserveFocus = false) {
    channel().show(preserveFocus);
}
function copilotLog(message) {
    const ch = copilotChannel();
    ch.appendLine(`[${timestamp()}] ${message}`);
}
function showCopilot() {
    copilotChannel().show();
}
//# sourceMappingURL=logger.js.map