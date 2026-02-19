/**
 * Launches VS Code with an extension development path and workspace.
 * Usage: node launch-vscode-extension.js <workspaceFolder> <extensionDevelopmentPath>
 * Used by launch config "McpServer MCP Todo (Launch in VS Code)" so the Run dropdown opens VS Code.
 */
const path = require('path');
const { spawn } = require('child_process');

const workspace = process.argv[2];
const extPath = process.argv[3];
if (!workspace || !extPath) {
  console.error('Usage: node launch-vscode-extension.js <workspaceFolder> <extensionDevelopmentPath>');
  process.exit(1);
}

const codeExe = process.env.VSCODE_PATH ||
  path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Microsoft VS Code', 'Code.exe');

const child = spawn(codeExe, ['--extensionDevelopmentPath=' + extPath, workspace], {
  stdio: 'inherit',
  detached: true,
  shell: false,
});
child.unref();
process.exit(0);
