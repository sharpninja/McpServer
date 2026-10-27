import { spawn, ChildProcess } from 'child_process';
import { classifyOperationFailure, type OperationFailure } from '../runtime/operation-failure.js';
import { existsSync, statSync } from 'fs';
import { delimiter, isAbsolute, join, resolve } from 'path';
import { createInterface } from 'readline';
import * as yaml from 'js-yaml';
import type { MarkerContext } from '../discovery/marker-resolver.js';

/** TR-QB-TRUST-001: launch settings owned by one host instance. */
export interface ReplBridgeOptions {
  /** Require a pinned executable and verified context; ignore ambient launch overrides. */
  governed?: boolean;
  /** Explicit executable selected by the host. */
  command?: string;
  /** Owning workspace, independent of other host contexts. */
  workspacePath?: string;
  /** Audit agent identity for this transport. */
  agentName?: string;
  /** Host plugin identity for child-local cache routing. */
  pluginId?: string;
  /** Host-owned request deadline; governed mode never reads an environment override. */
  timeoutMs?: number;
}

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

function isDirectory(path: string): boolean {
  try {
    return existsSync(path) && statSync(path).isDirectory();
  } catch {
    return false;
  }
}

/** Prefer an explicit override, then qbrain-ai-repl, then the 1.x mcpserver-repl command. */
export function resolveReplCommand(): string {
  const explicit = (process.env.MCPSERVER_REPL_BIN || process.env.MCPSERVER_REPL_COMMAND || '').trim();
  if (explicit) return explicit;
  return findInstalledRepl('qbrain-ai-repl') || findInstalledRepl('mcpserver-repl') || 'qbrain-ai-repl';
}

function findInstalledRepl(name: string): string | undefined {
  const entries = (process.env.PATH || '').split(delimiter).filter(Boolean);
  const extensions = process.platform === 'win32' ? ['.cmd', '.exe', '.bat', ''] : [''];
  for (const dir of entries) {
    for (const ext of extensions) {
      if (existsSync(join(dir, name + ext))) return name;
    }
  }
  return undefined;
}

export function resolveReplWorkingDirectory(): string {
  const candidates = [
    process.env.MCP_WORKSPACE_PATH,
    process.env.MCPSERVER_WORKSPACE_PATH,
    process.env.MCP_WORKSPACE_START_DIR,
    process.env.CLAUDE_PROJECT_DIR,
    process.env.PWD,
  ];

  for (const candidate of candidates) {
    if (candidate && candidate.trim().length > 0 && isDirectory(candidate)) {
      return candidate;
    }
  }

  return process.cwd();
}

/**
 * Persistent bridge to qbrain-ai-repl --agent-stdio.
 * Multiplexes concurrent YAML-over-STDIO requests by requestId.
 */
export class ReplBridge {
  private readonly options: Readonly<ReplBridgeOptions>;
  private markerContext: Readonly<MarkerContext> | undefined;
  private proc: ChildProcess | null = null;
  private pending = new Map<string, PendingRequest>();
  private buffer = '';
  private docBuffer = '';

  /** Capture host launch configuration without changing process-wide state. */
  constructor(options: ReplBridgeOptions = {}) {
    this.options = Object.freeze({ ...options });
  }

  /** Install an already-verified marker for this bridge before it starts. */
  configureContext(marker: MarkerContext): void {
    if (this.proc) throw new Error('Close the REPL bridge before changing its workspace credentials');
    if (this.options.governed && (!marker.apiKey || !marker.baseUrl || !marker.workspacePath || !this.options.workspacePath || resolve(marker.workspacePath) !== resolve(this.options.workspacePath))) {
      throw new Error('Governed REPL context must match the configured workspace and contain credentials');
    }
    this.markerContext = Object.freeze({ ...marker });
  }

  /** Nonsecret identity from this bridge's verified context; never consults ambient routing. */
  get verifiedWorkspace(): Readonly<Pick<MarkerContext, 'workspacePath' | 'workspace' | 'markerFile'>> | undefined {
    if (!this.governed || !this.markerContext) return undefined;
    const { workspacePath, workspace, markerFile } = this.markerContext;
    return Object.freeze({ workspacePath, workspace, markerFile });
  }

  /** Whether the transport is bound to a verified workspace rather than ambient routing. */
  get hasWorkspaceContext(): boolean {
    return this.markerContext !== undefined;
  }

  /** TR-QB-TRUST-001: governed callers must never fall back to ambient routing. */
  get governed(): boolean {
    return this.options.governed === true;
  }

  /** TR-QB-TRUST-001: reject a supplied transport belonging to another host context. */
  assertHostBinding(binding: { workspacePath: string; agentName: string; pluginId: string }): void {
    if (!this.governed || !this.options.workspacePath ||
      resolve(this.options.workspacePath) !== resolve(binding.workspacePath) ||
      this.options.agentName !== binding.agentName || this.options.pluginId !== binding.pluginId) {
      throw new Error('Governed REPL binding does not match the owning host');
    }
  }

  private childEnvironment(): NodeJS.ProcessEnv {
    if (!this.governed) return { ...process.env };
    // Preserve platform/runtime facilities, never arbitrary inherited credentials,
    // workspace selectors, failsafe paths, or executable injection settings.
    const allowed = /^(PATH|HOME|USERPROFILE|HOMEDRIVE|HOMEPATH|SYSTEMROOT|WINDIR|COMSPEC|PATHEXT|TEMP|TMP|TMPDIR|LANG|LC_[A-Z_]+|TZ|HTTP_PROXY|HTTPS_PROXY|ALL_PROXY|NO_PROXY|SSL_CERT_FILE|SSL_CERT_DIR)$/i;
    return Object.fromEntries(Object.entries(process.env).filter(([key]) => allowed.test(key)));
  }

  private timeoutMilliseconds(): number {
    const value = this.options.timeoutMs ?? (this.options.governed ? 15000 : Number(process.env.MCPSERVER_REPL_TIMEOUT_MS ?? '15000'));
    if (!Number.isFinite(value) || value <= 0) throw new Error('REPL timeout must be positive and finite');
    return value;
  }

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
    if (this.options.governed) {
      if (!this.markerContext) throw new Error('Governed REPL requires a verified workspace context');
      if (!this.options.command || !isAbsolute(this.options.command) || !existsSync(this.options.command) || isDirectory(this.options.command)) {
        throw new Error('Governed REPL requires an existing absolute pinned executable');
      }
      if (!this.options.agentName?.trim() || !this.options.pluginId?.trim()) {
        throw new Error('Governed REPL requires explicit agent and plugin identity');
      }
    }
    const replCommand = this.options.command || resolveReplCommand();
    let replArgs = !this.options.governed && process.env.MCPSERVER_REPL_ARGS
      ? process.env.MCPSERVER_REPL_ARGS.split(' ').filter(Boolean)
      : ['--agent-stdio'];
    // Ensure --agent is passed on every invocation for per-agent cache isolation (Codex vs Claude etc.).
    const agent = this.options.agentName || process.env.MCP_AGENT_NAME || process.env.PLUGIN_AGENT_NAME || process.env.PLUGIN_AGENT_DEFAULT || process.env.MCP_PLUGIN_HOST || 'default';
    if (!replArgs.includes('--agent')) {
      replArgs = [...replArgs, '--agent', agent];
    }
    const environment = this.childEnvironment();
    if (this.options.agentName) {
      for (const key of ['PLUGIN_AGENT_NAME', 'PLUGIN_AGENT_DEFAULT', 'MCP_AGENT_NAME', 'MCP_AGENT_ID', 'MCP_SESSION_AGENT', 'CT2R_SOURCE_TYPE']) environment[key] = agent;
    }
    if (this.options.pluginId) {
      for (const key of ['PLUGIN_TAG', 'MCP_SESSION_MODEL', 'CT2R_MODEL', 'CT2R_TAGS']) environment[key] = this.options.pluginId;
    }
    const workspacePath = this.markerContext?.workspacePath || this.options.workspacePath;
    if (workspacePath) {
      environment.MCPSERVER_WORKSPACE_PATH = workspacePath;
      environment.MCP_WORKSPACE_PATH = workspacePath;
    }
    if (this.markerContext) {
      environment.MCPSERVER_BASE_URL = this.markerContext.baseUrl;
      environment.MCPSERVER_API_KEY = this.markerContext.apiKey;
      environment.MCPSERVER_WORKSPACE = this.markerContext.workspace;
    }
    const proc = spawn(replCommand, replArgs, {
      stdio: ['pipe', 'pipe', 'pipe'],
      env: environment,
      cwd: workspacePath || resolveReplWorkingDirectory(),
    });
    this.proc = proc;

    proc.stderr?.on('data', (data: Buffer) => {
      if (this.proc === proc) process.stderr.write(`[repl] ${data}`);
    });

    const rl = createInterface({ input: proc.stdout! });
    // Spawn failures need not emit exit; pipe failures can arrive after a
    // replacement starts. Handle every error emitter without letting an old
    // child reject the replacement's requests or crash the host process.
    const fail = (error: Error): void => {
      rl.close();
      if (this.proc !== proc) return;
      this.proc = null;
      this.docBuffer = '';
      this.rejectPending(`qbrain-ai-repl transport failed: ${error.message}`, undefined, classifyOperationFailure(error));
      this.terminateProcess(proc);
    };
    proc.on('error', fail);
    proc.stdin?.on('error', fail);
    // Keep a direct listener after readline.close() detaches its forwarding
    // listener, since a retired child's pipe can still fail later.
    proc.stdout?.on('error', fail);
    proc.stderr?.on('error', fail);
    rl.on('error', fail);
    // A timed-out child may still emit output or exit after its replacement
    // starts. Only the current process owns framing and pending requests.
    rl.on('line', (line: string) => { if (this.proc === proc) this.onLine(line); });

    proc.on('exit', (code) => {
      rl.close();
      if (this.proc !== proc) return;
      process.stderr.write(`[repl] qbrain-ai-repl exited with code ${code}\n`);
      this.rejectPending(`qbrain-ai-repl exited with code ${code}`);
      this.docBuffer = '';
      this.proc = null;
    });
  }

  private rejectPending(message: string, exceptRequestId?: string, failure: OperationFailure = { detailCode: 'transport_unavailable', retryable: true }): void {
    for (const [requestId, req] of this.pending) {
      if (req.timer) clearTimeout(req.timer);
      if (requestId !== exceptRequestId) req.reject(Object.assign(new Error(message), failure));
    }
    this.pending.clear();
  }

  private terminateProcess(proc: ChildProcess): void {
    if (proc.exitCode !== null || proc.signalCode != null) return;
    proc.kill('SIGTERM');
    const escalation = setTimeout(() => {
      // killed means a signal was delivered, not that the child has exited.
      if (proc.exitCode === null && proc.signalCode == null) {
        proc.kill('SIGKILL');
      }
    }, 2000).unref();
    proc.once('exit', () => clearTimeout(escalation));
  }

  private terminateAfterTimeout(message: string, exceptRequestId?: string): void {
    const proc = this.proc;
    this.proc = null;
    this.docBuffer = '';
    this.rejectPending(message, exceptRequestId, { detailCode: 'transport_timeout', retryable: true });
    if (proc) this.terminateProcess(proc);
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
      process.stderr.write(`[repl] Failed to parse YAML: ${raw}\n`);
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
   * Send a YAML envelope and await the matching result/error envelope.
   */
  async invoke(
    method: string,
    params?: Record<string, unknown>,
  ): Promise<ReplResponse> {
    await this.ensure();
    const proc = this.proc;
    if (!proc?.stdin) throw Object.assign(new Error('qbrain-ai-repl transport unavailable'), { detailCode: 'transport_unavailable', retryable: true });

    const requestId = ReplBridge.generateRequestId(
      method.split('.').pop() ?? 'req',
    );

    return new Promise<ReplResponse>((resolve, reject) => {
      const timeoutMs = this.timeoutMilliseconds();
      const timer = setTimeout(() => {
        this.pending.delete(requestId);
        const message = `qbrain-ai-repl timed out after ${timeoutMs}ms for ${method}`;
        this.terminateAfterTimeout(message, requestId);
        reject(Object.assign(new Error(message), { detailCode: 'transport_timeout', retryable: true }));
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

      // Object then serialize (JSON preferred to avoid any YAML construction issues)
      const jsonStr = JSON.stringify(envelope);
      proc.stdin!.write(jsonStr + '\n');
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
    const proc = this.proc;
    if (!proc?.stdin) throw Object.assign(new Error('qbrain-ai-repl transport unavailable'), { detailCode: 'transport_unavailable', retryable: true });

    const requestId = ReplBridge.generateRequestId(
      method.split('.').pop() ?? 'stream',
    );

    return new Promise<ReplResponse>((resolve, reject) => {
      const timeoutMs = this.timeoutMilliseconds();
      const timer = setTimeout(() => {
        this.pending.delete(requestId);
        const message = `qbrain-ai-repl timed out after ${timeoutMs}ms for ${method}`;
        this.terminateAfterTimeout(message, requestId);
        reject(Object.assign(new Error(message), { detailCode: 'transport_timeout', retryable: true }));
      }, timeoutMs);
      this.pending.set(requestId, { resolve, reject, events: [], onEvent, timer });

      const envelope: Record<string, unknown> = {
        type: 'request',
        payload: { requestId, method, params },
      };

      // Object then serialize to JSON
      const jsonStr = JSON.stringify(envelope);
      proc.stdin!.write(jsonStr + '\n');
    });
  }

  /** Gracefully terminate the REPL process. */
  async close(): Promise<void> {
    const proc = this.proc;
    if (!proc) return;
    this.proc = null;
    this.docBuffer = '';
    this.rejectPending('qbrain-ai-repl bridge closed', undefined, { detailCode: 'transport_closed', retryable: false });
    await new Promise<void>((resolveClosed) => {
      const exited = () => { clearTimeout(deadline); resolveClosed(); };
      const deadline = setTimeout(() => {
        proc.removeListener('exit', exited);
        this.terminateProcess(proc);
        resolveClosed();
      }, 2000);
      proc.once('exit', exited);
      proc.stdin?.end();
      if (proc.exitCode !== null || proc.signalCode != null) exited();
    });
  }
}
