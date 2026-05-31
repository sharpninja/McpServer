import { spawn, ChildProcess } from 'child_process';
import { createInterface } from 'readline';
import * as yaml from 'js-yaml';

export interface ReplResponse {
  type: 'result' | 'error' | 'event';
  payload: Record<string, unknown>;
}

interface PendingRequest {
  resolve: (value: ReplResponse) => void;
  reject: (reason: Error) => void;
  events: ReplResponse[];
  onEvent?: (event: ReplResponse) => void;
  timer?: ReturnType<typeof setTimeout>;
}

/**
 * Persistent bridge to mcpserver-repl --agent-stdio.
 * Multiplexes concurrent JSON-over-STDIO requests by requestId.
 */
export class ReplBridge {
  private proc: ChildProcess | null = null;
  private pending = new Map<string, PendingRequest>();
  private buffer = '';
  private docBuffer = '';

  /** Generate a request ID matching ^req-\d{8}T\d{6}Z-[a-z0-9]+$ */
  static generateRequestId(slug = 'req'): string {
    const now = new Date();
    const ts =
      now.getUTCFullYear().toString() +
      (now.getUTCMonth() + 1).toString().padStart(2, '0') +
      now.getUTCDate().toString().padStart(2, '0') +
      'T' +
      now.getUTCHours().toString().padStart(2, '0') +
      now.getUTCMinutes().toString().padStart(2, '0') +
      now.getUTCSeconds().toString().padStart(2, '0') +
      'Z';
    const rand = Math.floor(Math.random() * 0xffff)
      .toString(16)
      .padStart(4, '0');
    // Ensure slug only contains lowercase alphanumeric characters
    const safeSlug = slug.toLowerCase().replace(/[^a-z0-9]/g, '') || 'req';
    return `req-${ts}-${safeSlug}-${rand}`;
  }

  /** Ensure the REPL process is running, restarting if it crashed. */
  async ensure(): Promise<void> {
    if (this.proc && this.proc.exitCode === null && !this.proc.killed) {
      return;
    }
    this.proc = spawn('mcpserver-repl', ['--agent-stdio'], {
      stdio: ['pipe', 'pipe', 'pipe'],
      env: { ...process.env },
    });

    this.proc.stderr?.on('data', (data: Buffer) => {
      process.stderr.write(`[repl] ${data}`);
    });

    const rl = createInterface({ input: this.proc.stdout! });
    rl.on('line', (line: string) => this.onLine(line));

    this.proc.on('exit', (code) => {
      process.stderr.write(`[repl] mcpserver-repl exited with code ${code}\n`);
      // Reject all pending requests
      for (const [, req] of this.pending) {
        if (req.timer) clearTimeout(req.timer);
        req.reject(new Error(`mcpserver-repl exited with code ${code}`));
      }
      this.pending.clear();
      this.proc = null;
    });
  }

  private terminateAfterTimeout(message: string, exceptRequestId?: string): void {
    const proc = this.proc;
    this.proc = null;
    this.docBuffer = '';

    for (const [requestId, req] of this.pending) {
      if (requestId === exceptRequestId) continue;
      if (req.timer) clearTimeout(req.timer);
      req.reject(new Error(message));
    }
    this.pending.clear();

    if (!proc || proc.exitCode !== null || proc.killed) {
      return;
    }

    proc.kill('SIGTERM');
    setTimeout(() => {
      if (proc.exitCode === null && !proc.killed) {
        proc.kill('SIGKILL');
      }
    }, 2000).unref();
  }

  private onLine(line: string): void {
    if (line === '---') {
      // YAML document separator — parse accumulated buffer
      if (this.docBuffer.trim()) {
        this.parseDocument(this.docBuffer);
      }
      this.docBuffer = '';
    } else {
      this.docBuffer += line + '\n';
    }
  }

  private parseDocument(raw: string): void {
    let doc: Record<string, unknown>;
    try {
      doc = yaml.load(raw) as Record<string, unknown>;
    } catch {
      process.stderr.write(`[repl] Failed to parse REPL response: ${raw}\n`);
      return;
    }

    const type = doc.type as string;
    const payload = doc.payload as Record<string, unknown>;
    if (!payload) return;

    const requestId = payload.requestId as string | undefined;
    const response: ReplResponse = { type: type as ReplResponse['type'], payload };

    if (!requestId) {
      // Broadcast event with no specific request ID
      return;
    }

    const pending = this.pending.get(requestId);
    if (!pending) return;

    if (type === 'event') {
      pending.events.push(response);
      pending.onEvent?.(response);
    } else {
      // Final result or error
      this.pending.delete(requestId);
      if (pending.timer) clearTimeout(pending.timer);
      pending.resolve(response);
    }
  }

  /**
   * Send a single-line JSON envelope and await the matching result/error envelope.
   */
  async invoke(
    method: string,
    params?: Record<string, unknown>,
  ): Promise<ReplResponse> {
    await this.ensure();

    const requestId = ReplBridge.generateRequestId(
      method.split('.').pop() ?? 'req',
    );

    return new Promise<ReplResponse>((resolve, reject) => {
      const timeoutMs = Number(process.env.MCPSERVER_REPL_TIMEOUT_MS ?? '15000');
      const timer = setTimeout(() => {
        this.pending.delete(requestId);
        const message = `mcpserver-repl timed out after ${timeoutMs}ms for ${method}`;
        this.terminateAfterTimeout(message, requestId);
        reject(new Error(message));
      }, timeoutMs);
      this.pending.set(requestId, { resolve, reject, events: [], timer });

      const envelope: Record<string, unknown> = {
        type: 'request',
        payload: {
          requestId,
          method,
          ...(params ? { params } : {}),
        },
      };

      this.proc!.stdin!.write(`${JSON.stringify(envelope)}\n`);
    });
  }

  /**
   * Invoke a streaming method, calling onEvent for each progress event.
   */
  async invokeStreaming(
    method: string,
    params: Record<string, unknown>,
    onEvent: (event: ReplResponse) => void,
  ): Promise<ReplResponse> {
    await this.ensure();

    const requestId = ReplBridge.generateRequestId(
      method.split('.').pop() ?? 'stream',
    );

    return new Promise<ReplResponse>((resolve, reject) => {
      const timeoutMs = Number(process.env.MCPSERVER_REPL_TIMEOUT_MS ?? '15000');
      const timer = setTimeout(() => {
        this.pending.delete(requestId);
        const message = `mcpserver-repl timed out after ${timeoutMs}ms for ${method}`;
        this.terminateAfterTimeout(message, requestId);
        reject(new Error(message));
      }, timeoutMs);
      this.pending.set(requestId, { resolve, reject, events: [], onEvent, timer });

      const envelope: Record<string, unknown> = {
        type: 'request',
        payload: { requestId, method, params },
      };

      this.proc!.stdin!.write(`${JSON.stringify(envelope)}\n`);
    });
  }

  /** Gracefully terminate the REPL process. */
  async close(): Promise<void> {
    if (this.proc) {
      this.proc.stdin?.end();
      await new Promise<void>((resolve) => {
        this.proc!.on('exit', () => resolve());
        setTimeout(resolve, 2000);
      });
      if (!this.proc?.killed) this.proc?.kill();
      this.proc = null;
    }
  }
}
