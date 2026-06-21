import { spawn, ChildProcess } from 'child_process';
import * as yaml from 'js-yaml';
import { McpRequest, McpResult, McpError, McpEvent, ReplResponse } from '../types';

/**
 * Shared ReplClient for talking to mcpserver-repl --agent-stdio.
 * Provides typed request/response and automatic envelope (de)serialization.
 * This is the core of the shared TS surface.
 */
export class ReplClient {
  private proc: ChildProcess | null = null;
  private readonly timeoutMs: number;

  constructor(private readonly workspacePath: string, timeoutMs = 45000) {
    this.timeoutMs = timeoutMs;
  }

  async connect(): Promise<void> {
    if (this.proc) return;

    this.proc = spawn('mcpserver-repl', ['--agent-stdio'], {
      cwd: this.workspacePath,
      stdio: ['pipe', 'pipe', 'pipe'],
      env: { ...process.env, MCP_WORKSPACE_PATH: this.workspacePath },
    });

    // Basic stderr logging (non-blocking)
    this.proc.stderr?.on('data', (d) => {
      // In production, route to proper logger
      if (process.env.DEBUG_REPL) console.error('[mcp-repl stderr]', d.toString());
    });
  }

  async sendRequest<TParams = any, TResult = any>(
    method: string,
    params?: TParams
  ): Promise<ReplResponse> {
    await this.connect();

    const requestId = `req-${new Date().toISOString().replace(/[-:]/g, '').split('.')[0]}Z-${Math.random().toString(16).slice(2, 6)}`;

    const envelope: any = {
      type: 'request',
      payload: {
        requestId,
        method,
        ...(params ? { params } : {}),
      },
    };

    const yamlStr = yaml.dump(envelope, { noRefs: true, skipInvalid: true });

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`REPL request ${method} timed out after ${this.timeoutMs}ms`));
      }, this.timeoutMs);

      let stdout = '';
      const onData = (data: Buffer) => {
        stdout += data.toString();
        // Heuristic: responses are usually one document
        if (stdout.includes('type:') && (stdout.includes('result:') || stdout.includes('error:'))) {
          this.proc?.stdout?.off('data', onData);
          clearTimeout(timeout);
          try {
            const parsed = yaml.load(stdout) as any;
            const success = parsed?.type === 'result' || !parsed?.error;
            resolve({
              success,
              requestId,
              output: stdout,
              parsed: success ? (parsed.payload as McpResult) : (parsed.payload as McpError),
            });
          } catch (e) {
            resolve({ success: false, requestId, output: stdout });
          }
        }
      };

      this.proc?.stdout?.on('data', onData);
      this.proc?.stdin?.write(yamlStr + '\n');
    });
  }

  async close(): Promise<void> {
    if (this.proc) {
      this.proc.kill();
      this.proc = null;
    }
  }
}

// Convenience factory (matches the style of the PS New-McpRequest + Invoke)
export async function invokeMcpMethod<TParams = any, TResult = any>(
  workspacePath: string,
  method: string,
  params?: TParams
): Promise<McpResult<TResult> | McpError> {
  const client = new ReplClient(workspacePath);
  try {
    const resp = await client.sendRequest<TParams, TResult>(method, params);
    if (!resp.success) return resp.parsed as McpError;
    return resp.parsed as McpResult<TResult>;
  } finally {
    await client.close();
  }
}