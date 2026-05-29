# Proposed Wording Changes to Reduce Drift from Fowler TDD while Preserving Byrd Intent

**Date**: 2026-05-28
**Goal**: Make Byrd's TDD description closer to Martin Fowler's canonical definition (small incremental tests, explicit Red-Green-Refactor cycle) while keeping the AI-safety mechanisms (mocks gate, strong regression protection) that are core to Byrd.

## Changes Applied (via search_replace)

### 1. In docs/Development-Process-draft-v4.md (Core Byrd Definition)

**Original (line 62):**
> Once planning is complete... the implementation starts with the AI creating the unit tests that cover the full spectrum of acceptance criteria in the current iteration phase. Using mocking tools... the acceptance criteria-based unit tests are validated with mocks that make them pass. Only once all unit tests are validated for correctness does implementation turn to code...

**New:**
> Once planning is complete and the iterative phases are specified, the implementation follows a Test-Driven Development cycle (in the sense of Martin Fowler: write a test for the next small piece of desired behavior, make it pass, then refactor). Within each phase the AI writes unit tests covering the acceptance criteria for the next increment of work. Using mocking tools appropriate for the tech stack, these tests are first validated with mocks. Only after the tests for that increment are passing against mocks does implementation turn to the real code that makes the tests pass without mocks. Refactoring of both tests and production code occurs as part of the cycle to keep the design clean.

**Rationale**: Shifts from "full spectrum upfront" to incremental slices (Fowler-aligned) while explicitly keeping the mocks-as-gate for AI safety.

### 2. In docs/Development-Process-draft-v4.md (Validation section)

**Original:**
> To even exit the Implementation phase requires the entire unit test suite for the iteration, as well as previous iterations, to be completely passing.

**New:**
> Before exiting an Implementation phase, the full test suite for the current increment plus all prior work must be green. This ensures the current work is correct and has not broken previous behavior. Refactoring continues throughout to keep both tests and production code clean.

**Rationale**: Keeps the strong regression protection (Byrd intent) but frames it less rigidly and adds explicit refactoring language.

### 3. In docs/plans/plan-agent-plugin-operational-parity-v1.0.md (Core Principle section)

**Original:**
> **Core Principle (Byrd):** 
> - Write the tests covering full acceptance criteria first.
> - Make them pass using only mocks/stubs.
> - Only then implement the real code.
> - The entire test suite (current phase + all previous + ecosystem) must be green to exit a phase.

**New:**
> **Byrd Augmentations to Canonical TDD (Fowler):**
> While following the Red → Green → Refactor cycle, Byrd adds AI-specific process gates for safety and auditability:
> - Within a phase, write small, focused tests for the next increment of behavior (following Fowler).
> - Validate those tests green using mocks/stubs before writing the corresponding real implementation code.
> - Only after the mocks-validated tests pass do we implement the production behavior.
> - Before a phase is considered complete, the full relevant test suite (current increment + prior work) must be green.

**Rationale**: Explicitly positions Byrd as *extending* Fowler rather than redefining TDD. Uses "increment" language instead of "full phase".

### 4. In the Detailed TDD Test Plan section (multiple small cleanups)

- Changed language from "full acceptance criteria first" to "small, focused tests for the next increment of behavior (per Fowler)".
- Added clearer framing that the mocks gate and full-suite regression check are *Byrd augmentations*.

## Remaining Tension (Honest Note)
The mocks-before-real-code gate and the requirement that the full historical suite be green to exit a phase are still present. These are core to Byrd's risk model for AI work and were not removed. They represent the largest remaining philosophical difference from pure Fowler TDD.

If even these are considered too much drift, further relaxation would be needed (e.g., allowing minimal production code in the Green step, or treating the full-suite green check as a recommendation rather than a hard phase exit gate).

## Files Modified
- docs/Development-Process-draft-v4.md
- docs/plans/plan-agent-plugin-operational-parity-v1.0.md

A full side-by-side drift analysis is available in docs/plans/TDD-Byrd-vs-Fowler-Analysis.md (created earlier in this session).

