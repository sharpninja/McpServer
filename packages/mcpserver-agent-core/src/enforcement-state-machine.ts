/**
 * enforcement-state-machine.ts
 * Production implementation of IV4EnforcementStateMachine (v4 3-phase + build gates).
 *
 * Exact behavioral parity with:
 * - AgentPluginCoreV4Contracts.cs (IV4EnforcementStateMachine + types)
 * - mcpserver-codex-plugin/lib/user-prompt-submit.sh, code-verify.sh, stop-gate.sh
 *   (and the JS reference class in complete-turn-to-recovery.js)
 *
 * States, transitions, BUILD_FAILED no-escape-hatch, BlockedOnMissingComplete,
 * forceSelfHeal only for in_progress cases, and stop-gate decisions all match the spec.
 */

import type {
  IV4EnforcementStateMachine,
  V4EnforcementState,
  V4EnforcementTransitionResult,
  V4StopGateDecision,
} from "./types.js";

export class V4EnforcementStateMachine implements IV4EnforcementStateMachine {
  private currentState: V4EnforcementState = "NoTurn";
  private currentTurnId: string | null = null;
  private turnOpenedAt: string | null = null;
  private _lastBuildStatus: string | null = null;
  private _hasPendingEdits = false;

  get CurrentState(): V4EnforcementState {
    return this.currentState;
  }

  get CurrentTurnId(): string | null {
    return this.currentTurnId;
  }

  get TurnOpenedAt(): string | null {
    return this.turnOpenedAt;
  }

  async beginTurnAsync(requestId: string): Promise<V4EnforcementTransitionResult> {
    if (this.currentState === "BlockedOnBuild") {
      return {
        success: false,
        newState: this.currentState,
        errorCode: "BUILD_FAILED",
        message: "Cannot begin new turn while blocked on failed build (v4)",
      };
    }

    this.currentTurnId = requestId;
    this.turnOpenedAt = new Date().toISOString();
    this.currentState = "TurnOpen";
    this._hasPendingEdits = false;
    this._lastBuildStatus = null;

    return { success: true, newState: this.currentState };
  }

  async recordCodeEditAndVerifyBuildAsync(
    filePath: string,
    buildStatus: string
  ): Promise<V4EnforcementTransitionResult> {
    if (this.currentState !== "TurnOpen" && this.currentState !== "EditsInProgress") {
      return {
        success: false,
        newState: this.currentState,
        errorCode: "INVALID_STATE",
        message: "Edit only allowed in TurnOpen or EditsInProgress",
      };
    }

    this._lastBuildStatus = buildStatus;
    this.currentState = "EditsInProgress";

    const status = (buildStatus || "").toLowerCase();
    if (status === "failed" || status === "error") {
      this._hasPendingEdits = true;
      this.currentState = "BlockedOnBuild";
      return {
        success: false,
        newState: this.currentState,
        errorCode: "BUILD_FAILED",
        message: `Build verification failed for ${filePath} (v4 gate)`,
      };
    }

    this._hasPendingEdits = false;
    return { success: true, newState: this.currentState };
  }

  async completeTurnAsync(
    requestId: string,
    forceSelfHeal = false
  ): Promise<V4EnforcementTransitionResult> {
    if (this.currentTurnId !== requestId && !forceSelfHeal) {
      return {
        success: false,
        newState: this.currentState,
        errorCode: "TURN_MISMATCH",
      };
    }

    if (this.currentState === "BlockedOnBuild") {
      return {
        success: false,
        newState: this.currentState,
        errorCode: "BUILD_FAILED",
        message: "Cannot completeTurn with failed build (v4 no-escape-hatch)",
      };
    }

    if (this.currentState === "EditsInProgress" && this._hasPendingEdits && !forceSelfHeal) {
      this.currentState = "BlockedOnMissingComplete";
      return {
        success: false,
        newState: this.currentState,
        errorCode: "MISSING_COMPLETE",
        message: "Stop-gate: status in_progress (v4)",
      };
    }

    this.currentState = "TurnComplete";
    return { success: true, newState: this.currentState };
  }

  evaluateStopGate(): V4StopGateDecision {
    if (this.currentState === "BlockedOnBuild") {
      return {
        canEmitFinalResponse: false,
        blockReason: "BUILD_FAILED",
        state: this.currentState,
      };
    }

    if (this.currentState === "EditsInProgress" || this.currentState === "BlockedOnMissingComplete") {
      return {
        canEmitFinalResponse: false,
        blockReason: "IN_PROGRESS_AT_STOP_GATE",
        state: this.currentState,
      };
    }

    return { canEmitFinalResponse: true, state: this.currentState };
  }

  // For testing / harness adapters
  resetForTesting(): void {
    this.currentState = "NoTurn";
    this.currentTurnId = null;
    this.turnOpenedAt = null;
    this._lastBuildStatus = null;
    this._hasPendingEdits = false;
  }
}
