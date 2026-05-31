# @sharpninja/mcp-repl - Shared TypeScript Surface

Canonical shared TypeScript package for McpServer agent plugins (Cline, Cline V2, OpenCode, and future TS plugins).

## Location

- Source: `tools/typescript/mcp-repl-ts` in the main McpServer repository
- Published: [@sharpninja/mcp-repl on npm](https://www.npmjs.com/package/@sharpninja/mcp-repl)

## What it provides

- Typed message entities aligned with the PowerShell `McpRepl` module
- `ReplBridge` and `McpAgentClient` for communicating with `mcpserver-repl --agent-stdio`
- Marker resolution and cache/failsafe helpers
- Common workflow method name registry

## Usage

```ts
import { McpAgentClient, WorkflowMethods } from '@sharpninja/mcp-repl';

const client = new McpAgentClient(workspacePath);
await client.ensureConnected();

// Example
const result = await client.raw.send('workflow.todo.create', { ... });
```

## Publishing

Published automatically by the `publish_shared_modules` job in the main `azure-pipelines.yml` (only on `main`).

Manual:

```bash
cd tools/typescript/mcp-repl-ts
npm ci
npm run build
npm publish --access public
```

## Development

The package is referenced via `file:` paths during local development from the three TS plugins.
