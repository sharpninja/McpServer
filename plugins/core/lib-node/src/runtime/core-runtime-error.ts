import { classifyOperationFailure } from './operation-failure.js';

/** TR-QB-TRUST-001: stable lifecycle failure categories for governed hosts. */
export type CoreRuntimeFailureCode = 'bootstrap_failed' | 'failsafe_replay_failed' | 'session_failed';

/** A governed lifecycle operation failed; callers must block continuation. */
export class CoreRuntimeError extends Error {
  /** Machine-readable operation category, independent of diagnostic wording. */
  readonly code: CoreRuntimeFailureCode;
  readonly detailCode: string;
  readonly retryable: boolean;
  readonly reportedRetryable?: boolean;

  /** Preserve the underlying cause for trusted diagnostics without copying it into the public message. */
  constructor(code: CoreRuntimeFailureCode, cause: unknown) {
    super(`Governed core operation failed: ${code}`, { cause });
    this.name = 'CoreRuntimeError';
    this.code = code;
    const failure = classifyOperationFailure(cause);
    this.detailCode = failure.detailCode;
    this.retryable = failure.retryable;
    this.reportedRetryable = failure.reportedRetryable;
  }
}
