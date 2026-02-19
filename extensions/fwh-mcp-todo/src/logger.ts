/**
 * Output channel and trace logger for FWH MCP Todo extension.
 * All interactions are logged so they appear in Output → FWH MCP Todo.
 * Copilot interactions are logged to a separate Output → FWH Copilot channel.
 */

import * as vscode from 'vscode';

const channelName = 'FWH MCP Todo';
let _channel: vscode.OutputChannel | undefined;

const copilotChannelName = 'FWH Copilot';
let _copilotChannel: vscode.OutputChannel | undefined;

function channel(): vscode.OutputChannel {
  if (!_channel) {
    _channel = vscode.window.createOutputChannel(channelName);
  }
  return _channel;
}

function copilotChannel(): vscode.OutputChannel {
  if (!_copilotChannel) {
    _copilotChannel = vscode.window.createOutputChannel(copilotChannelName);
  }
  return _copilotChannel;
}

function timestamp(): string {
  return new Date().toISOString();
}

export function log(message: string, ...args: unknown[]): void {
  const line = args.length ? `${timestamp()} ${message} ${args.map((a) => JSON.stringify(a)).join(' ')}` : `${timestamp()} ${message}`;
  const ch = channel();
  ch.appendLine(line);
  // Also echo to console so it appears in Extension Host output if Output panel is on wrong channel
  console.log(`[FWH MCP Todo] ${line}`);
}

export function show(preserveFocus = false): void {
  channel().show(preserveFocus);
}

export function copilotLog(message: string): void {
  const ch = copilotChannel();
  ch.appendLine(`[${timestamp()}] ${message}`);
}

export function showCopilot(): void {
  copilotChannel().show();
}
