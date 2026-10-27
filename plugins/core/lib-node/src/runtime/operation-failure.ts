// SPDX-License-Identifier: Apache-2.0
/** C9 / TR-QB-AUDIT-001: detailed failure survives broad host error categories. */
export interface OperationFailure {
  readonly detailCode: string;
  /** Effective retry permission; unknown and final codes never retry. */
  readonly retryable: boolean;
  /** Original explicit flag, when present, retained independently of policy. */
  readonly reportedRetryable?: boolean;
}

const persistence = new Set(['backend_unavailable', 'persistence_error', 'timeout']);
const transport = new Set(['transport_unavailable', 'transport_timeout', 'ECONNRESET', 'ECONNREFUSED', 'ETIMEDOUT', 'EPIPE', 'EHOSTUNREACH', 'ENETUNREACH', 'EAI_AGAIN']);
/** Shared final-code inventory. Unknown codes and every review_* code are final too. */
export const FINAL_OPERATION_CODES = Object.freeze([
  'turn_immutable', 'skipped_terminal', 'host_session_unknown', 'host_session_ambiguous',
  'request_ambiguous', 'request_unknown', 'turn_logging_unsupported', 'mint_requires_host_ingress',
  'nonce_source_missing', 'session_turn_completion_gate', 'request_class_invalid',
  'ingress_nonce_invalid', 'mint_collision_exhausted',
] as const);

/** Classify structured codes only, never status numbers or diagnostic prose. */
export function classifyOperationFailure(value: unknown): OperationFailure {
  const visited = new Set<unknown>();
  for (let depth = 0; depth < 8 && value && typeof value === 'object' && !visited.has(value); depth++) {
    visited.add(value);
    const error = value as Record<string, unknown>;
    const code = error.detailCode ?? error.code;
    if ((error.detailCode !== undefined || error.code !== undefined) &&
      (typeof code !== 'string' || !/^[A-Za-z][A-Za-z0-9_]{0,127}$/.test(code))) {
      return Object.freeze({ detailCode: 'unknown_error', retryable: false });
    }
    if (typeof code === 'string') {
      const flag = typeof error.retryable === 'boolean' ? error.retryable : undefined;
      const reported = typeof error.reportedRetryable === 'boolean' ? error.reportedRetryable : flag;
      const flagsValid = (error.retryable === undefined || typeof error.retryable === 'boolean') &&
        (error.reportedRetryable === undefined || typeof error.reportedRetryable === 'boolean');
      const retryable = flagsValid && (transport.has(code) ? flag !== false : persistence.has(code) && flag === true);
      return Object.freeze({ detailCode: code, retryable, ...(reported === undefined ? {} : { reportedRetryable: reported }) });
    }
    // Fetch failures can carry a native I/O code on their cause. An explicit
    // unknown outer code above stays final even when its nested cause is transient.
    value = error.cause;
  }
  return Object.freeze({ detailCode: 'unknown_error', retryable: false });
}

/** Safe public diagnostic with structured fields; raw cause stays private to host diagnostics. */
export class CoreOperationError extends Error implements OperationFailure {
  readonly detailCode: string;
  readonly retryable: boolean;
  readonly reportedRetryable?: boolean;
  constructor(detailCode: string, retryable: boolean, cause?: unknown) {
    const failure = classifyOperationFailure({ code: detailCode, retryable });
    super(`Core operation failed: ${failure.detailCode}`, { cause });
    this.name = 'CoreOperationError';
    this.detailCode = failure.detailCode;
    this.retryable = failure.retryable;
    this.reportedRetryable = failure.reportedRetryable;
  }
}