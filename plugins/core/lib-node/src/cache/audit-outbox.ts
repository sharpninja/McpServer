// SPDX-License-Identifier: Apache-2.0
import { createHash, randomUUID } from 'crypto';
import { promises as fs } from 'fs';
import { basename, isAbsolute, join, resolve } from 'path';
import { dump, load } from 'js-yaml';
import { classifyOperationFailure, type OperationFailure } from '../runtime/operation-failure.js';

/** TR-QB-AUDIT-001: immutable context carried by every durable audit event. */
export interface AuditContext {
  /** Verified execution workspace identity. */
  workspacePath: string;
  /** Actual agent identity. */
  agentIdentity: string;
  /** Conversation-linked MCP session identifier. */
  mcpSessionId: string;
}

/** Event identity and exact payload bytes persisted before submission. */
export interface AuditEvent extends AuditContext {
  /** Owning turn. */
  requestId: string;
  /** Globally unique idempotency key. */
  eventId: string;
  /** Durable, monotonically increasing order within the turn. */
  order: number;
  /** Audit record category. */
  kind: 'action' | 'decision' | 'dialog' | 'metadata';
  /** Exact JSON document whose UTF-8 bytes are hashed. */
  payloadJson: string;
  /** Lowercase SHA-256 of payloadJson. */
  payloadHash: string;
}

/** Server acknowledgement, checked before pending evidence is retired. */
export interface AuditReceipt extends AuditContext {
  /** Receipt state; only verified persisted data satisfies completion. */
  code: 'persisted' | 'queued' | 'unchanged' | 'rejected' | 'lost';
  /** Echoed owning turn. */
  requestId: string;
  /** Echoed event idempotency key. */
  eventId: string;
  /** Echoed event ordering. */
  order: number;
  /** Echoed payload digest. */
  payloadHash: string;
  /** Positive committed server revision. */
  revision?: number;
  /** Diagnostic without secret credentials. */
  reason?: string;
  /** Existing server turn status when a terminal turn rejects this event. */
  storedStatus?: string;
}

/** Supported plugin/REPL operations supplied by the transport adapter. */
export interface AuditTransport {
  /** Submit one immutable event; the server deduplicates by eventId. */
  submit(event: AuditEvent): Promise<AuditReceipt>;
  /** Read back a persisted event before accepting an unchanged acknowledgement. */
  read(event: AuditEvent): Promise<AuditReceipt>;
}

/** Outcome of one durable local enqueue. */
export interface AuditEnqueueResult {
  /** Queued means the event is durably on disk; lost blocks execution. */
  code: 'queued' | 'lost';
  /** Present only when the immutable event was saved. */
  event?: AuditEvent;
  /** Nonsecret diagnostic on failure. */
  reason?: string;
}

/** Replay summary; any pending, quarantined or rejected record blocks completion. */
export interface AuditFlushResult {
  /** Number acknowledged and retained in the archive this flush. */
  persisted: number;
  /** Number still awaiting acknowledgement. */
  pending: number;
  /** Number held for explicit recovery. */
  quarantined: number;
  /** Retained terminal rejections, including those from previous flushes. */
  rejected: number;
  /** Ordered submission outcomes. */
  receipts: AuditReceipt[];
}

/** Host-owned storage location and runtime facilities; never selected from environment variables. */
export interface AuditOutboxOptions {
  /** Absolute root under the host's isolated storage. */
  rootDirectory: string;
  /** Verified identity bound for this outbox's lifetime. */
  context: AuditContext;
  /** Required for alias consumers; isolates concurrent host sessions. */
  hostSessionKey?: string;
  /** Constructor-only clock injection for deterministic retry tests. */
  sleep?: (milliseconds: number) => Promise<void>;
}

/** C2 alias shape supplied by the context-bound A8 journal. */
export interface AuditAliasRecord {
  workspacePath: string;
  agent: string;
  mcpSessionId: string;
  hostSessionKey: string;
  ingressNonce: string;
  pendingId: string;
  requestId: string;
  aliasedAtUtc: string;
  acks: string[];
}

type AppliedAlias = Omit<AuditAliasRecord, 'acks'>;

interface StoredEvent {
  version: 1;
  event: AuditEvent;
  attempts: number;
  nextAttemptAt?: number;
  receipt?: AuditReceipt;
  failure?: OperationFailure;
}

const sha256 = (value: string): string => createHash('sha256').update(value, 'utf8').digest('hex');
const delay = (milliseconds: number): Promise<void> => new Promise(resolveDelay => setTimeout(resolveDelay, milliseconds));
const retryDelays = [1000, 2000, 4000, 8000, 16000, 30000];
const kinds = new Set(['action', 'decision', 'dialog', 'metadata']);
const receiptCodes = new Set(['persisted', 'queued', 'unchanged', 'rejected', 'lost']);
const nonempty = (value: unknown): value is string => typeof value === 'string' && value.trim().length > 0;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/** Storage failure preserves evidence and must block governed continuation. */
export class AuditStorageError extends Error {
  /** No successful persistence assertion may be inferred from this error. */
  readonly code = 'lost';
  /** Supply only a nonsecret operation diagnostic; retain the cause for trusted diagnostics. */
  constructor(message: string, cause?: unknown) {
    super(message, { cause });
    this.name = 'AuditStorageError';
  }
}

/** TR-QB-AUDIT-001: context-bound atomic event journal with checked replay. */
export class AuditOutbox {
  private readonly context: Readonly<AuditContext>;
  private readonly directory: string;
  private readonly sleep: (milliseconds: number) => Promise<void>;
  private readonly hostSessionKey?: string;
  private aliases: AppliedAlias[] = [];

  /** Construct a journal bound to one immutable audit context. */
  constructor(options: AuditOutboxOptions) {
    if (!isAbsolute(options.rootDirectory) || !isAbsolute(options.context.workspacePath) ||
      !nonempty(options.context.agentIdentity) || !nonempty(options.context.mcpSessionId)) {
      throw new AuditStorageError('Audit outbox requires an absolute root and explicit context');
    }
    this.context = Object.freeze({ ...options.context, workspacePath: resolve(options.context.workspacePath) });
    if (options.hostSessionKey !== undefined && !nonempty(options.hostSessionKey)) throw new AuditStorageError('Host session key is invalid');
    this.hostSessionKey = options.hostSessionKey;
    const identity = [this.context.workspacePath, this.context.agentIdentity, this.context.mcpSessionId];
    if (this.hostSessionKey) identity.push(this.hostSessionKey);
    const key = sha256(JSON.stringify(identity));
    this.directory = join(options.rootDirectory, key);
    this.sleep = options.sleep ?? delay;
  }

  private async initialize(): Promise<void> {
    for (const dir of [this.directory, ...['pending', 'archive', 'quarantine', 'rejected'].map(folder => join(this.directory, folder))]) {
      await fs.mkdir(dir, { recursive: true, mode: 0o700 });
      if ((await fs.lstat(dir)).isSymbolicLink()) throw new AuditStorageError('Outbox directories cannot be symbolic links');
    }
  }

  private async withLock<T>(operation: () => Promise<T>): Promise<T> {
    await this.initialize();
    const lockPath = join(this.directory, '.lock');
    let handle: Awaited<ReturnType<typeof fs.open>> | undefined;
    for (let attempt = 0; attempt < 500; attempt++) {
      try {
        handle = await fs.open(lockPath, 'wx', 0o600);
        break;
      } catch (error) {
        if ((error as NodeJS.ErrnoException).code !== 'EEXIST') throw error;
        await delay(10);
      }
    }
    if (!handle) throw new AuditStorageError('Audit outbox lock is held; recovery is required if its owner exited');
    try {
      await handle.writeFile(JSON.stringify({ pid: process.pid, lockId: randomUUID() }));
      await handle.sync();
      this.aliases = await this.readAliases();
      await this.reconcileAliases();
      return await operation();
    } finally {
      await handle.close();
      await fs.unlink(lockPath);
    }
  }

  /**
   * Explicit recovery only: retire a lock after the OS proves its owner exited.
   * Live, inaccessible, malformed or ambiguous owners are never displaced.
   * A recovery mutex prevents two recoverers from unlinking a new owner's lock.
   */
  async recoverAbandonedLock(): Promise<boolean> {
    await this.initialize();
    const recoveryPath = join(this.directory, '.recovery.lock');
    let recovery: Awaited<ReturnType<typeof fs.open>>;
    try { recovery = await fs.open(recoveryPath, 'wx', 0o600); }
    catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'EEXIST') return false;
      throw new AuditStorageError('Unable to lock audit recovery', error);
    }
    try {
      const lockPath = join(this.directory, '.lock');
      let contents: string;
      let owner: unknown;
      try {
        if ((await fs.lstat(lockPath)).isSymbolicLink()) return false;
        contents = await fs.readFile(lockPath, 'utf8');
        owner = JSON.parse(contents);
      } catch { return false; }
      if (!isRecord(owner) || !Number.isSafeInteger(owner.pid) || Number(owner.pid) <= 0 || !nonempty(owner.lockId)) return false;
      try { process.kill(Number(owner.pid), 0); return false; }
      catch (error) { if ((error as NodeJS.ErrnoException).code !== 'ESRCH') return false; }
      if (await fs.readFile(lockPath, 'utf8') !== contents) return false;
      await this.atomicWrite(join(this.directory, 'archive', `recovered-lock-${randomUUID()}.yaml`), {
        version: 1, owner, recoveredAt: new Date().toISOString(), reason: 'owner_process_exited',
      });
      await fs.unlink(lockPath);
      await this.syncDirectory(this.directory);
      return true;
    } finally {
      await recovery.close();
      await fs.unlink(recoveryPath);
    }
  }

  private async syncDirectory(directory: string): Promise<void> {
    // Windows does not expose POSIX directory fsync through Node. File contents
    // are still flushed before the atomic same-directory rename there.
    if (process.platform === 'win32') return;
    const handle = await fs.open(directory, 'r');
    try { await handle.sync(); } finally { await handle.close(); }
  }

  private async atomicWrite(file: string, value: unknown): Promise<void> {
    const temporary = `${file}.${randomUUID()}.tmp`;
    const handle = await fs.open(temporary, 'wx', 0o600);
    try {
      await handle.writeFile(dump(value, { noRefs: true }), 'utf8');
      await handle.sync();
    } finally { await handle.close(); }
    // Failed publishes leave the temporary evidence for quarantine/recovery.
    await fs.rename(temporary, file);
    await this.syncDirectory(resolve(file, '..'));
  }

  private async names(folder: string): Promise<string[]> {
    return (await fs.readdir(join(this.directory, folder))).sort();
  }

  private canonicalRequestId(id: string): string {
    return this.aliases.find(alias => alias.pendingId === id)?.requestId ?? id;
  }

  private logicalRequestId(id: string): string {
    return this.aliases.find(alias => alias.requestId === id)?.pendingId ?? id;
  }

  private checkedAlias(value: unknown): AppliedAlias {
    if (!isRecord(value) || !this.hostSessionKey || value.workspacePath !== this.context.workspacePath ||
      value.agent !== this.context.agentIdentity || value.mcpSessionId !== this.context.mcpSessionId ||
      value.hostSessionKey !== this.hostSessionKey || !nonempty(value.ingressNonce) ||
      value.pendingId !== `pending-${value.ingressNonce}` || !nonempty(value.requestId) || value.requestId.startsWith('pending-') ||
      typeof value.aliasedAtUtc !== 'string' || !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(value.aliasedAtUtc) ||
      !Number.isFinite(Date.parse(value.aliasedAtUtc))) throw new AuditStorageError('Alias identity or binding is invalid');
    return { workspacePath: value.workspacePath, agent: value.agent, mcpSessionId: value.mcpSessionId,
      hostSessionKey: value.hostSessionKey, ingressNonce: value.ingressNonce, pendingId: String(value.pendingId),
      requestId: value.requestId, aliasedAtUtc: value.aliasedAtUtc };
  }

  private async readAliases(): Promise<AppliedAlias[]> {
    const file = join(this.directory, 'applied-aliases.yaml');
    try {
      if ((await fs.lstat(file)).isSymbolicLink()) throw new AuditStorageError('Alias application journal cannot be a symbolic link');
      const state: unknown = load(await fs.readFile(file, 'utf8'));
      if (!isRecord(state) || state.version !== 1 || !Array.isArray(state.aliases)) throw new AuditStorageError('Alias application journal is invalid');
      const aliases = state.aliases.map(alias => this.checkedAlias(alias));
      if (new Set(aliases.map(alias => alias.pendingId)).size !== aliases.length ||
        new Set(aliases.map(alias => alias.requestId)).size !== aliases.length) throw new AuditStorageError('Alias application journal is ambiguous');
      return aliases;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') return [];
      throw error;
    }
  }

  /** Called under the interprocess lock before any further enqueue or replay. */
  private async reconcileAliases(): Promise<void> {
    if (this.aliases.length === 0) return;
    for (const folder of ['pending', 'archive', 'quarantine', 'rejected']) {
      for (const name of await this.names(folder)) {
        // Match the stable logical filename before decoding the event. Unrelated
        // malformed evidence is still handled by the existing flush quarantine.
        if (!this.aliases.some(alias => name.startsWith(`${sha256(alias.pendingId)}-`)) || !name.endsWith('.event.yaml')) continue;
        const file = join(this.directory, folder, name);
        if ((await fs.lstat(file)).isSymbolicLink()) throw new AuditStorageError('Aliased event cannot be a symbolic link');
        const record: unknown = load(await fs.readFile(file, 'utf8'));
        if (!isRecord(record) || record.version !== 1 || !this.validEvent(record.event) ||
          this.eventFilename(record.event) !== name) throw new AuditStorageError('Aliased event is invalid');
        const requestId = this.canonicalRequestId(record.event.requestId);
        if (requestId === record.event.requestId) continue;
        if (folder !== 'pending' || record.attempts !== 0 || record.receipt !== undefined || record.failure !== undefined) {
          throw new AuditStorageError('Cannot rebind already submitted or rejected evidence');
        }
        // Keep filename, eventId, order and payload bytes unchanged. The durable
        // alias makes both IDs resolve to this same logical filename/counter.
        await this.atomicWrite(file, { ...record, event: { ...record.event, requestId } });
      }
    }
  }

  /** Apply a committed A8 alias durably before the consumer acknowledges it. */
  async applyAlias(value: AuditAliasRecord): Promise<void> {
    // Copy external input before awaiting so a notification object cannot be
    // mutated between validation and persistence. Acks belong to A8, not here.
    const alias = this.checkedAlias(value);
    if (!Array.isArray(value.acks) || value.acks.some(ack => !nonempty(ack))) throw new AuditStorageError('Alias acknowledgements are invalid');
    await this.withLock(async () => {
      const existing = this.aliases.find(item => item.pendingId === alias.pendingId || item.requestId === alias.requestId);
      if (existing) {
        if (JSON.stringify(existing) !== JSON.stringify(alias)) throw new AuditStorageError('Conflicting alias application');
        return;
      }
      // A late alias must never combine independently allocated canonical orders.
      // The host reconciles aliases before accepting writes under minted IDs.
      const canonicalKey = sha256(alias.requestId);
      for (const folder of ['pending', 'archive', 'quarantine', 'rejected']) {
        if ((await this.names(folder)).some(name => name.startsWith(`${canonicalKey}-`))) throw new AuditStorageError('Canonical turn already has independent evidence');
      }
      try {
        const sequenceFile = join(this.directory, 'sequence.yaml');
        if ((await fs.lstat(sequenceFile)).isSymbolicLink()) throw new AuditStorageError('Sequence file cannot be a symbolic link');
        const sequence: unknown = load(await fs.readFile(sequenceFile, 'utf8'));
        if (!isRecord(sequence) || sequence.version !== 1 || !isRecord(sequence.orders)) throw new AuditStorageError('Durable audit sequence is corrupt');
        if (Object.prototype.hasOwnProperty.call(sequence.orders, canonicalKey)) throw new AuditStorageError('Canonical turn already has an independent sequence');
      } catch (error) { if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error; }
      const aliases = [...this.aliases, alias];
      await this.atomicWrite(join(this.directory, 'applied-aliases.yaml'), { version: 1, aliases });
      this.aliases = aliases;
      await this.reconcileAliases();
    });
  }

  /** Read either ID against durable state, completing interrupted application. */
  async resolveRequestId(id: string): Promise<string> {
    if (!nonempty(id)) throw new AuditStorageError('Request id is missing');
    return this.withLock(async () => this.canonicalRequestId(id));
  }
  private async reserveOrder(requestId: string): Promise<number> {
    const sequencePath = join(this.directory, 'sequence.yaml');
    let orders: Record<string, number> = {};
    try {
      if ((await fs.lstat(sequencePath)).isSymbolicLink()) throw new AuditStorageError('Sequence file cannot be a symbolic link');
      const state: unknown = load(await fs.readFile(sequencePath, 'utf8'));
      if (!isRecord(state) || state.version !== 1 || !isRecord(state.orders) ||
        Object.entries(state.orders).some(([key, value]) => !/^[a-f0-9]{64}$/.test(key) || !Number.isSafeInteger(value) || Number(value) < 0)) {
        throw new AuditStorageError('Durable audit sequence is corrupt');
      }
      orders = state.orders as Record<string, number>;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error;
      if ((await this.names('pending')).length || (await this.names('archive')).length || (await this.names('quarantine')).length || (await this.names('rejected')).length) {
        throw new AuditStorageError('Durable audit sequence is missing beside existing evidence');
      }
    }
    const key = sha256(this.logicalRequestId(requestId));
    const order = (orders[key] ?? 0) + 1;
    if (!Number.isSafeInteger(order)) throw new AuditStorageError('Audit sequence exhausted');
    // TR-QB-AUDIT-001: a structurally valid but stale sequence must not reuse
    // an order retained in pending, acknowledged or quarantined evidence.
    // Check filenames without decoding potentially corrupt event payloads.
    const filenamePattern = new RegExp(`^${key}-(\\d{16})-[a-f0-9-]{36}\\.event\\.yaml$`);
    for (const folder of ['pending', 'archive', 'quarantine', 'rejected']) {
      for (const name of await this.names(folder)) {
        if (!name.startsWith(`${key}-`) || !name.endsWith('.event.yaml')) continue;
        const match = filenamePattern.exec(name);
        const existingOrder = match ? Number(match[1]) : NaN;
        if (!Number.isSafeInteger(existingOrder) || existingOrder < 1 || existingOrder >= order) {
          throw new AuditStorageError('Durable audit sequence conflicts with retained evidence');
        }
      }
    }
    orders[key] = order;
    await this.atomicWrite(sequencePath, { version: 1, orders });
    return order;
  }

  private eventFilename(event: AuditEvent): string {
    return `${sha256(this.logicalRequestId(event.requestId))}-${String(event.order).padStart(16, '0')}-${event.eventId}.event.yaml`;
  }

  /** Persist identity and order before allowing a network submission. */
  async enqueue(requestId: string, kind: AuditEvent['kind'], payload: Record<string, unknown>): Promise<AuditEnqueueResult> {
    try {
      if (!nonempty(requestId) || !kinds.has(kind) || !isRecord(payload)) throw new AuditStorageError('Invalid audit event');
      const payloadJson = JSON.stringify(payload);
      return await this.withLock(async () => {
        requestId = this.canonicalRequestId(requestId);
        const order = await this.reserveOrder(requestId);
        const event: AuditEvent = Object.freeze({ ...this.context, requestId, eventId: randomUUID(), order, kind, payloadJson, payloadHash: sha256(payloadJson) });
        await this.atomicWrite(join(this.directory, 'pending', this.eventFilename(event)), { version: 1, event, attempts: 0 });
        return { code: 'queued', event };
      });
    } catch {
      return { code: 'lost', reason: 'Audit event could not be durably queued; local recovery is required' };
    }
  }

  private validEvent(value: unknown): value is AuditEvent {
    if (!isRecord(value)) return false;
    if (value.workspacePath !== this.context.workspacePath || value.agentIdentity !== this.context.agentIdentity || value.mcpSessionId !== this.context.mcpSessionId ||
      !nonempty(value.requestId) || typeof value.eventId !== 'string' || !/^[a-f0-9-]{36}$/.test(value.eventId) ||
      !Number.isSafeInteger(value.order) || Number(value.order) < 1 || !kinds.has(String(value.kind)) ||
      typeof value.payloadJson !== 'string' || value.payloadHash !== sha256(value.payloadJson)) return false;
    try { return isRecord(JSON.parse(value.payloadJson)); } catch { return false; }
  }

  private validReceipt(value: unknown, event: AuditEvent, requireRevision: boolean): value is AuditReceipt {
    if (!isRecord(value) || !receiptCodes.has(String(value.code))) return false;
    const fields = ['workspacePath', 'agentIdentity', 'mcpSessionId', 'requestId', 'eventId', 'order', 'payloadHash'] as const;
    return fields.every(field => value[field] === event[field]) &&
      (!requireRevision || (Number.isSafeInteger(value.revision) && Number(value.revision) > 0));
  }

  private async quarantine(file: string): Promise<void> {
    await fs.rename(file, join(this.directory, 'quarantine', basename(file)));
    await this.syncDirectory(join(this.directory, 'quarantine'));
    await this.syncDirectory(resolve(file, '..'));
  }

  private rejected(event: AuditEvent, reason: string): AuditReceipt {
    return { ...event, code: 'rejected', reason };
  }

  private terminalReceipt(value: unknown, event: AuditEvent): boolean {
    return this.validReceipt(value, event, false) && value.code === 'rejected' &&
      value.reason === 'skipped_terminal' && nonempty(value.storedStatus);
  }

  private async retainRejection(file: string): Promise<void> {
    await fs.rename(file, join(this.directory, 'rejected', basename(file)));
    await this.syncDirectory(join(this.directory, 'rejected'));
    await this.syncDirectory(join(this.directory, 'pending'));
  }

  /** Replay in durable order and require matching server acknowledgements. */
  async flush(transport: AuditTransport): Promise<AuditFlushResult> {
    try {
      return await this.withLock(async () => {
        const receipts: AuditReceipt[] = [];
        let persisted = 0;
        // Crash-left temporary files are evidence, not garbage to silently drop.
        for (const folder of ['', 'pending', 'archive', 'rejected']) {
          for (const name of await this.names(folder)) {
            if (name.endsWith('.tmp')) await this.quarantine(join(this.directory, folder, name));
          }
        }
        if ((await this.names('quarantine')).length === 0) {
          for (const name of await this.names('pending')) {
            const file = join(this.directory, 'pending', name);
            let record: StoredEvent;
            try {
              if ((await fs.lstat(file)).isSymbolicLink()) throw new Error('Symbolic event');
              const parsed: unknown = load(await fs.readFile(file, 'utf8'));
              if (!isRecord(parsed) || parsed.version !== 1 || !this.validEvent(parsed.event) ||
                this.eventFilename(parsed.event) !== name ||
                !Number.isSafeInteger(parsed.attempts) || Number(parsed.attempts) < 0 || Number(parsed.attempts) > 10 ||
                (Number(parsed.attempts) > 0 && (!Number.isSafeInteger(parsed.nextAttemptAt) || Number(parsed.nextAttemptAt) < 0)) ||
                (parsed.failure !== undefined && (!isRecord(parsed.failure) ||
                  typeof parsed.failure.detailCode !== 'string' || typeof parsed.failure.retryable !== 'boolean' ||
                  (parsed.failure.reportedRetryable !== undefined && typeof parsed.failure.reportedRetryable !== 'boolean') ||
                  classifyOperationFailure(parsed.failure).detailCode !== parsed.failure.detailCode ||
                  classifyOperationFailure(parsed.failure).retryable !== parsed.failure.retryable)) ||
                (parsed.receipt !== undefined && !this.terminalReceipt(parsed.receipt, parsed.event))) throw new Error('Invalid event');
              record = parsed as unknown as StoredEvent;
            } catch {
              await this.quarantine(file);
              break;
            }
            // An offline ingress has no server turn yet. Preserve it locally
            // without spending retry attempts or accidentally creating a turn.
            if (record.event.requestId.startsWith('pending-')) continue;
            // A final failure is durably recorded before moving its file. A crash
            // between these steps must never turn a final attempt into a retry.
            if (record.failure && !record.failure.retryable) {
              await this.quarantine(file);
              break;
            }
            const event = Object.freeze(record.event);
            // A receipt is committed before the move: restart completes only
            // the local move, never resubmits an already rejected event.
            if (record.receipt) {
              await this.retainRejection(file);
              receipts.push(record.receipt);
              continue;
            }
            let acknowledgement: AuditReceipt | undefined;
            if (record.attempts > 0 && record.attempts < 10 && record.nextAttemptAt! > Date.now()) {
              await this.sleep(record.nextAttemptAt! - Date.now());
            }
            while (record.attempts < 10) {
              record.attempts++;
              record.nextAttemptAt = Date.now() + retryDelays[Math.min(record.attempts - 1, retryDelays.length - 1)];
              await this.atomicWrite(file, record);
              try {
                const response = await transport.submit(event);
                if (!this.validReceipt(response, event, response?.code === 'persisted' || response?.code === 'unchanged')) {
                  acknowledgement = this.rejected(event, 'Receipt identity, payload or revision mismatch');
                  break;
                }
                if (response.code === 'unchanged') {
                  const readback = await transport.read(event);
                  if (!this.validReceipt(readback, event, true) || readback.code !== 'persisted' || readback.revision !== response.revision) {
                    acknowledgement = this.rejected(event, 'Unchanged receipt lacks matching persisted readback');
                  } else acknowledgement = readback;
                  break;
                }
                if (response.code !== 'queued') { acknowledgement = response; break; }
              } catch (error) {
                record.failure = classifyOperationFailure(error);
                await this.atomicWrite(file, record);
                if (!record.failure.retryable) {
                  acknowledgement = this.rejected(event, record.failure.detailCode);
                  break;
                }
                // Only classified transient failure replays the same eventId.
              }
              if (record.attempts < 10) await this.sleep(retryDelays[Math.min(record.attempts - 1, retryDelays.length - 1)]);
            }
            acknowledgement ??= this.rejected(event, 'Retry budget exhausted');
            receipts.push(acknowledgement);
            if (this.terminalReceipt(acknowledgement, event)) {
              await this.atomicWrite(file, { ...record, receipt: acknowledgement });
              await this.retainRejection(file);
              continue;
            }
            if (acknowledgement.code !== 'persisted') {
              await this.quarantine(file);
              break;
            }
            await this.atomicWrite(join(this.directory, 'archive', name), { ...record, receipt: acknowledgement });
            await fs.unlink(file);
            await this.syncDirectory(join(this.directory, 'pending'));
            persisted++;
          }
        }
        return { persisted, pending: (await this.names('pending')).length, quarantined: (await this.names('quarantine')).length, rejected: (await this.names('rejected')).length, receipts };
      });
    } catch (error) {
      throw new AuditStorageError('Audit replay could not safely update durable evidence', error);
    }
  }
}
