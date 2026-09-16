import { pathToFileURL } from 'node:url';
import { mkdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

function argValue(name) {
  const index = process.argv.indexOf(name);
  if (index < 0 || index + 1 >= process.argv.length) {
    throw new Error('Missing ' + name);
  }
  return process.argv[index + 1];
}

function failureText(result) {
  if (result == null) {
    return 'empty result';
  }
  if (typeof result === 'string') {
    return result;
  }
  if (typeof result === 'object') {
    if (typeof result.error === 'string' && result.error.length > 0) {
      return result.error;
    }
    if (typeof result.output === 'string' && result.output.length > 0) {
      return result.output;
    }
  }
  return JSON.stringify(result);
}

function isFailure(result) {
  if (result == null) {
    return true;
  }
  if (typeof result === 'object') {
    if (typeof result.error === 'string' && result.error.length > 0) {
      return true;
    }
    if (typeof result.output === 'string' && /Error:\s/i.test(result.output)) {
      return true;
    }
  }
  return /Session not found/i.test(failureText(result));
}

const pluginRoot = resolve(argValue('--pluginRoot'));
const workspacePath = resolve(argValue('--workspacePath'));
const agentName = process.argv.includes('--agentName') ? argValue('--agentName') : 'Cline';
const href = pathToFileURL(join(pluginRoot, 'dist', 'index.js')).href;

process.env.MCP_WORKSPACE_PATH = workspacePath;
process.env.MCPSERVER_WORKSPACE_PATH = workspacePath;
process.env.MCP_CACHE_DIR_OVERRIDE = join(workspacePath, '.mcpServer', 'cache');
process.env.MCPSERVER_FAILSAFE_DIR = join(workspacePath, '.mcpServer', 'failsafe');
process.env.MCP_FAILSAFE_DIR = process.env.MCPSERVER_FAILSAFE_DIR;
delete process.env.MCPSERVER_BASE_URL;
delete process.env.MCPSERVER_API_KEY;
mkdirSync(process.env.MCP_CACHE_DIR_OVERRIDE, { recursive: true });
mkdirSync(process.env.MCPSERVER_FAILSAFE_DIR, { recursive: true });
process.chdir(workspacePath);

if (process.argv.includes('--flushOnly')) {
  const coreHref = pathToFileURL(
    join(pluginRoot, 'node_modules', '@sharpninja', 'mcpserver-plugin-core', 'dist', 'index.js'),
  ).href;
  const core = await import(coreHref);
  const bridge = new core.ReplBridge();
  try {
    const result = await core.cacheFlush(bridge);
    process.stdout.write(JSON.stringify({ ok: true, result }) + '\n');
    await bridge.close?.();
    process.exit(0);
  } catch (error) {
    process.stderr.write((error instanceof Error ? error.message : String(error)) + '\n');
    process.exit(1);
  }
}

const calls = JSON.parse(readFileSync(argValue('--callsPath'), 'utf8'));

const mod = await import(href);
const factory = mod.createMcpServerPlugin;
if (typeof factory !== 'function') {
  throw new Error('createMcpServerPlugin is not exported from ' + href);
}

const created = await Promise.resolve(
  factory({
    workspacePath,
    agentName,
    autoBootstrap: true,
    autoFlushCache: false,
  }),
);

async function runExecute(execute, call) {
  try {
    return await execute(call.arguments ?? {}, { workspacePath });
  } catch (error) {
    return { error: error instanceof Error ? error.message : String(error) };
  }
}

const results = [];
if (created && typeof created.setup === 'function') {
  const registered = [];
  created.setup(
    {
      registerTool(tool) {
        registered.push(tool);
      },
    },
    { workspaceInfo: { rootPath: workspacePath, workspacePath } },
  );
  for (const call of calls) {
    const tool = registered.find((item) => item && item.name === call.name);
    if (!tool || typeof tool.execute !== 'function') {
      results.push({ error: 'Tool missing: ' + call.name });
      continue;
    }
    results.push(await runExecute(tool.execute.bind(tool), call));
  }
} else if (created && created.tool) {
  for (const call of calls) {
    const tool = created.tool[call.name];
    if (!tool || typeof tool.execute !== 'function') {
      results.push({ error: 'Tool missing: ' + call.name });
      continue;
    }
    results.push(await runExecute(tool.execute.bind(tool), call));
  }
} else {
  throw new Error('Unsupported plugin export shape from ' + href);
}

const serialized = JSON.stringify({ ok: true, results });
process.stdout.write(serialized + '\n');

const closeIndex = calls.findLastIndex((call) => call && call.name === 'session_close');
const closeResult = closeIndex >= 0 ? results[closeIndex] : results.at(-1);
if (isFailure(closeResult)) {
  process.stderr.write('session_close failed: ' + failureText(closeResult) + '\n');
  process.exit(1);
}
process.exit(0);
