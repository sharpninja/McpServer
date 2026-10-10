# @qbrainai/qbrain-ai-repl - Shared TypeScript Surface

Canonical shared TypeScript package for QBrainAi agent plugins (Cline, Cline V2, OpenCode, and future TS plugins).

## Location

- Source: `tools/typescript/mcp-repl-ts` in the main QBrainAi repository
- Published: [@qbrainai/qbrain-ai-repl on npm](https://www.npmjs.com/package/@qbrainai/qbrain-ai-repl)

## What it provides

- Typed message entities aligned with the PowerShell `McpRepl` module
- `ReplBridge` and `McpAgentClient` for communicating with `qbrain-ai-repl --agent-stdio`
- Marker resolution and cache/failsafe helpers
- Common workflow method name registry

## Usage

```ts
import { McpAgentClient, WorkflowMethods } from '@qbrainai/qbrain-ai-repl';

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
