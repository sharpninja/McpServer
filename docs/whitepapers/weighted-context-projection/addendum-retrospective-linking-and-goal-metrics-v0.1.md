# Weighted Context Projection — Addendum: Retrospective Linking and Goal Metrics

**Document:** addendum-retrospective-linking-and-goal-metrics-v0.1.md
**Version:** v0.1.1
**Status:** Draft. Not reviewed. Proposes a capability beyond the v0.1.x scope of the whitepaper and the proposed implementation; nothing here is approved or scheduled.
**Companion to:** `whitepaper-weighted-context-projection-v0.1.md` (v0.1.9) and `proposed-implementation-weighted-context-projection-v0.1.md` (v0.1.4)
**Code baseline:** `main` @ `e7c43a125e1bb4837b5b9b9d4021ae2b592f931f`
**Date:** 2026-09-20
**Origin:** Operator design session, 2026-09-20. Five proposals, recorded here in the order raised: a retrospective link index (§2–§4), continuous link revalidation (§5), per-goal scope and progress metrics (§6), per-turn result-with-explanation records (§7), and Hostile Validation as the adjudicator of those records rather than the acting agent (§6.3) — an operator correction to this draft's first version, which had the actor grading itself.

**Cross-reference convention.** Bare `§N` refers to a section of this document. `WP §N` refers to the whitepaper. `IMPL §N` refers to the proposed implementation.

**Scope.** This addendum is deliberately separate. The whitepaper and the proposed implementation are in operator review at v0.1.9 / v0.1.4; folding a new capability into them would blur what is being approved. If the ideas here are accepted, §2, §3 and §6.1 belong in the whitepaper as design, and the rest belongs in the implementation proposal.

**Status of every claim here.** Design argument and mechanism, not measured result. §11 states the non-claims explicitly. Code references were read against the baseline commit; they are a snapshot and could be wrong.

---

## 1. Why this exists: re-admit cannot currently fire

WP Principle 4 makes re-admit first-class: "if a quiet constraint suddenly matters, the scorer must be allowed to pull the full turn back from SessionLog." WP §6 step 7 gives it exactly one trigger — an omitted turn's `weight` rising enough on a later reweight pass.

That trigger is circular for the cases that matter most.

`weight` is a per-turn scalar scored against current work progress. For an omitted turn's weight to rise, the scorer must recognize the turn's relevance to the present situation. But omission happened *because* the scorer did not recognize that relevance, and once a turn is omitted it is no longer in the projection to suggest its own relevance. The scorer is asked to rediscover, from a context that no longer contains the turn, a connection it failed to see when the turn was in front of it.

So the mechanism works for turns whose relevance is locally obvious — recency, repeated identifiers, the same file mentioned again — and fails for turns whose relevance is only apparent in hindsight. Those are exactly the turns re-admit was introduced to recover. WP §10 risk 2 ("bad scorer drops quiet constraints") names the symptom; its stated mitigation is a constraint detector at write time, which helps only for constraints recognizable as constraints when written.

Nothing in the current design closes this. It is a structural hole, not a tuning problem.

---

## 2. The representational limit: a scalar cannot express joint value

The deeper issue is what `weight` can represent.

Consider a session where turn 4 records an environment constraint in passing and turn 60 hits a failure the constraint explains. Scored individually against current work, turn 4 is unremarkable — it mentions no current identifier, it is old, it reads as background. Turn 60 is obviously relevant. The information that resolves the situation is not in either turn; it is in the *pair*.

A per-turn scalar has no way to encode that. Scoring functions of the form `score(turn, currentWork)` are evaluated independently per turn, so they cannot express "these two turns are jointly worth more than the sum of their parts." No improvement to the scoring function reaches this, because the limitation is in the shape of the representation rather than in the quality of the estimate.

Expressing joint value requires a relation between turns as a first-class object. That is the argument for a link index, and it is a design-level claim: the whitepaper's scoring model is incomplete in a way that cannot be fixed inside the scoring model.

This is a generalization of the pattern already accepted elsewhere in the design: supersession and `tokenEstimate` both turned out to be *relations* or *derived functions* rather than intrinsic properties of a turn (IMPL §3.4, §5.4). Links are the same move applied to relevance.

---

## 3. Retrospective link index

**Proposal.** A service that periodically examines sealed turns and records typed, directed, weighted edges between turns whose connection is discoverable only in hindsight. The projector may then admit a turn because something it is linked to is load-bearing right now, rather than only because its own score rose.

**Why sealing makes this tractable.** IMPL §5 seals a turn's payload at terminal status. An index over immutable content is stable, cacheable, and safely recomputable: re-running the indexer over the same sealed turns cannot produce different inputs. Without the seal, every index entry would be suspect the moment a turn was edited.

**Where links live.** Edges are derived data and belong in the mutable projection layer alongside `weight` and `projectionState`, never in the seal. This follows the resolution already reached twice: IMPL §5.4 derives supersession rather than writing it into the immutable row, and IMPL §3.4 derives `tokenEstimate` per renderer rather than freezing one value. A link is a hypothesis about content, not part of the content.

**Link types must be task-progress relations, not similarity.** WP Principle 2 rejects raw self-information, recency alone, and "interestingness" as weighting bases. A `similar_to` edge reintroduces all three under a new name, and topical similarity is both the cheapest thing to compute and the wrong thing to act on — it will happily link every turn that mentions the same file. Admissible edge types are ones that assert something about work:

| Type | Asserts |
|---|---|
| `constrains` | A commitment recorded in the source turn limits what the target turn may do |
| `invalidates` | The source turn makes a decision or result in the target turn no longer valid |
| `explains` | The source turn supplies the cause of an outcome observed in the target turn |
| `shares_root_cause` | Two symptoms trace to one underlying condition |
| `depends_on` | The target's result is contingent on the source's result holding |

The list is illustrative, not settled. The test for admitting a new type is whether traversing it changes what work should happen next. "These are about the same topic" fails that test.

**Every edge carries its evidence.** An edge records the turns it connects, its type, a confidence value, and a reference to what justifies it — which acceptance criterion, which identifier, which recorded outcome. An edge whose justification cannot be inspected cannot be audited when it turns out to be wrong, and §4 argues wrong edges are the primary hazard.

---

## 4. Why false links are worse than false weights

A weighting error omits something useful. The budget invariant still holds, the omitted turn is still recoverable per WP §6, and the projection remains internally honest — it simply lacks something.

A link error *injects* content, and injects it carrying an implied causal claim: this material bears on your current situation. A model receiving a wrongly linked turn is not merely carrying extra tokens; it has been handed a false lead with an institutional endorsement, and it will reason from it. The failure is active rather than passive.

Three consequences:

1. **Precision dominates recall.** A link index at 60% precision is worse than no index, because a wrong lead costs more than a missing one. Tune for few, strong edges.
2. **Link-admitted turns need a sub-cap inside `budgetTokens`.** Otherwise a burst of spurious edges can crowd out pinned turns and current work, converting a relevance error into a budget failure. The WP §6 hard budget remains the outer bound; link admissions get a fraction of it.
3. **Marking matters.** A turn admitted by link should be identifiable as such in the assembly, so a reader — human or model — can discount it. Silent injection removes the ability to tell a retrieved lead from established context.

---

## 5. Continuous revalidation

**Proposal.** Every edge is re-evaluated after every turn. The pass finds missing links as new turns arrive and retracts links that no longer hold. Links become per-generation hypotheses rather than stored facts, which is what makes §4's precision requirement achievable: precision is maintained continuously instead of being demanded of the initial judgment.

Three constraints make the difference between this working and this entrenching errors.

### 5.1 Evaluation must be blind to the projection it influenced

If a false edge admits turn 4, and the next evaluation judges that edge while turn 4 is present in the assembled context, the evaluator sees a coherent narrative and confirms the edge. The edge has become its own evidence. Each generation then raises confidence in a false link, and revalidation — the mechanism introduced to remove false links — entrenches them instead.

**Requirement:** edges are judged against sealed payloads and recorded outcomes directly, out of band, never against the projection they contributed to. The convenient implementation is to ask the model about the context it already has; that implementation has the failure built in.

### 5.2 Evaluate the decision boundary, not the graph

Re-judging every edge after every turn is `O(E)` model judgments per turn. With candidate generation producing even a sparse `k` edges per turn, `E` grows with session length, so cost per turn grows and total session cost is superlinear — while competing with the live turn the index exists to help. WP §10 risk 6 already flags per-turn re-score cost as a live concern; this multiplies it.

Only edges that could change a decision this generation are worth a model call. An edge between two turns that are both currently omitted, with confidence well away from the admission threshold, cannot alter the projection no matter how it is judged.

Ordering:

1. Edges currently load-bearing — those admitting a turn now.
2. Edges with an endpoint whose `weight`, `projectionState`, or goal attribution moved this generation.
3. Edges near the admission threshold in either direction.
4. Stale edges, by generations since last evaluation.

Cheap invalidation gates the expensive judgment: if neither endpoint changed and nothing near the boundary moved, no model is asked anything. The count of judgments per generation is capped by configuration, so revalidation cost is bounded by design rather than emergent from graph size.

### 5.3 Edge churn is WP §10 risk 1 one level up

Thrashing — oscillating scores, churning projection, dead prompt cache — reappears here in a harder-to-see form, because a single edge flipping can swing several turns in or out at once. Edges need the discipline weights already have: a confidence hysteresis band rather than a single threshold (compare `Δweight` in WP §5.2), and a cooldown keyed to a generation counter, mirroring `lastReAdmitGeneration` in WP §4.2. Edge flip rate is a metric in its own right (§9).

### 5.4 Lifecycle is append-only

An edge's history is a sequence of recorded transitions — proposed, confirmed, retracted, re-proposed — each with its reason and generation, not a mutated confidence column. Two reasons:

- **Auditability.** When a projection changes between generations, the operator can ask why, and the answer is a readable record rather than a diff of vanished state.
- **Free precision measurement.** Retraction rate per generation directly measures indexer precision, without a separate evaluation harness. See §9.

IMPL §3.2.1 applies: if edge state is flipped through set-based updates, those writes bypass the audit ledger. Edge transitions should go through the tracked save path.

---

## 6. Goal metrics: scope and progress

### 6.1 The measurement that weight has been missing

WP §5 scores weight as relevance "to current work progress," and nothing in the design measures progress. The central quantity is defined against an unmeasured one.

**Proposal.** Track two values per goal:

- **Scope** — the size and complexity of the goal as currently understood. Rises when the goal grows or proves more complex than believed.
- **Progress** — work demonstrably completed against that goal.

Two independently moving lines, rather than a single percent-complete, which is the property that makes the pair robust to a goal changing underneath the measurement. A completion ratio alone cannot distinguish "advanced" from "target moved." This is the burn-up / earned-value shape, and that lineage should be cited if this enters the whitepaper — WP §12 is frozen for v0.1.x, so it is named here as a concept and needs a reference in v0.2 rather than a citation added now.

The per-turn reading is then the pair of deltas:

| Scope | Progress | Reading |
|---|---|---|
| flat | up | Genuinely useful turn |
| flat | flat | No movement; may still be legitimate (investigation) |
| up | flat | Discovery or decomposition — the goal was larger than believed. Not harmful |
| up | up | Work plus discovery |
| flat | **down** | Regression — but see §6.2 before calling it harm |
| **down** | up (ratio) | Descoping. Must be justified, never rewarded — see §6.3 |

### 6.2 Progress regression does not imply a harmful turn

The rule "progress regresses with scope constant ⇒ the turn was harmful" is not sound as stated, and the correction matters because the exception is high-value.

A turn that discovers earlier work was wrong reduces progress without changing scope. It is also frequently the most valuable turn in a session. Under the unqualified rule it is scored as harm, which optimizes directly against error detection.

The distinction is **introduced** versus **revealed**:

- *Introduced* — the turn made things worse. Progress genuinely fell. This is harm.
- *Revealed* — the turn discovered that a previous progress reading was overstated. Nothing got worse; the measurement was corrected.

This forces a useful admission: **progress is an estimate carrying error, not ground truth.** So the progress series must be append-only revisions with attribution, never a mutated current value. Overwriting destroys the only evidence that distinguishes the two cases after the fact.

Worked example from this document's own history: the whitepaper and implementation proposal absorbed five corrections in one session, each one reducing apparent progress — sections reopened, recommendations reversed, claims retracted — while raising actual correctness. A metric that scored those as harm would have taught the agent not to look.

### 6.3 Who measures progress: Hostile Validation, not the actor

The acting agent must never score its own progress. It is an unreliable narrator with a standing incentive to report advancement, and that is structurally the same defect WP Principle 8 exists to block — a self-produced number presented as an outcome.

**Progress and regression readings are outputs of Hostile Validation.** WP §11 defines HV as an accuracy and completeness gate. Today it is applied as a document-level review ritual; here it becomes a runtime component that adjudicates turns. The actor does the work; an adversarial validator, which did not do the work and is not rewarded for it having gone well, determines what was accomplished.

This is the same structural move as §5.1, and the two should be stated as one rule:

> **No component may validate using the artifact it produced.** Link revalidation is blind to the projection it influenced (§5.1); progress adjudication is blind to the actor's own account of its work (§6.3).

**Claim versus verdict.** The actor is still the best source of *intent* — what it was trying to do and why it believed the approach would work — and that knowledge is not recoverable from a transcript afterwards. So the two roles split:

- The actor records an **intent claim**: the goal targeted, the criterion it expected to satisfy.
- HV records the **verdict**: what actually resulted, and the progress or regression delta.

Divergence between the two is itself high-value signal. An actor claiming a criterion was met where HV finds it was not is precisely the harm case in §6.2, detected without anyone having to trust the actor.

**HV is not ground truth either.** It is an adversary with its own error modes, false accusation among them, and it may be wrong in the other direction. So verdicts are evidence rather than fact: append-only (§6.2), and anchored to checkable referents rather than to HV's unsupported opinion. WP §4.2's `pin` classes already include **acceptance**, which supplies the anchor — HV's job is to check acceptance criteria, tests, builds and artifacts, and to label any interpolation between them as interpolation rather than as measurement.

**Where the gaming surface moves.** HV closes self-reporting; it does not eliminate gaming, it relocates it to criteria authorship. An actor that can write its own trivially-satisfiable acceptance criteria can pass hostile validation honestly and advance nothing. Criteria provenance therefore matters as much as verdict provenance — see §10 open questions 3 and 9.

**Cost.** Per-turn hostile validation is the largest new cost in this addendum, larger than §5.2's revalidation. WP §10 risk 6 already flags per-turn model-judgment cost with the mitigation "v1 rules by default; judge on schedule or on budget pressure," and the same tiering applies: cheap checkable assertions — did the test pass, does the artifact exist — run always; expensive model adjudication runs on turns that assert progress, on seal, or under pressure. Nothing here should be read as claiming HV is cheap or currently available; the existing document-level Perplexity HV remains blocked on an API key and must not be described as done.

**Commensurability.** Scope and progress must share units, or independent movement means nothing. If scope counts known acceptance criteria, progress counts criteria met. If scope is in estimated units of work, progress must be in the same units. Mixing "complexity" with "criteria completed" produces two series whose deltas cannot be compared.

**Descoping.** Removing scope inflates any completion ratio without work being done. Every scope change records direction, reason, and justification, and the ratio must never be a reward signal on its own. A scope reduction is a claim that work is unnecessary, and that claim is exactly as checkable as a progress claim.

---

## 7. Turn outcome records: result plus explanation

**Proposal.** Each turn carries a record of its result together with an explanation of why that result occurred. The explanation, not a bare number, is the signal that determines whether the turn advanced or regressed the goal.

This is what makes §6 operable. A progress delta on its own cannot distinguish introduced from revealed regression (§6.2), nor justify a scope change (§6.3). The explanation supplies the attribution that both need.

Per §6.3, the explanation is an **HV output, not an actor output**. The actor contributes its intent claim; the adversarial validator accounts for what actually happened. An earlier draft of this section had the actor explaining its own result, which reintroduced the exact gaming surface §6.3 exists to close — and free-text self-justification is *worse* than a self-reported number, because prose can persuade in ways a number cannot.

### 7.1 Explanations must be structured claims, not prose

Moving authorship to HV removes the incentive to flatter; it does not by itself make an explanation checkable. An adversary can also be confidently wrong, and a fluent accusation is as unfalsifiable as a fluent excuse.

So explanations — from either side — are constrained to verifiable referents: which acceptance criterion changed state, which test moved, which prior turn is contradicted, which file or identifier is implicated, which goal is affected and in which direction. Then the account is checkable rather than merely persuasive, and one citing nothing checkable is visibly weak on its face. This applies symmetrically to the actor's intent claim and to HV's verdict; neither gets to assert an outcome it cannot point at.

### 7.2 Explanations are append-only evidence

An explanation is written at the time, from what was then known, but the signal it feeds is retrospective. Later turns may show it was mistaken. It must therefore be preserved and reinterpreted, never rewritten: the record is evidence-at-the-time. This is the same append-only requirement that §5.4 places on edges and §6.2 places on the progress series, and for the same reason — retrospection needs an unedited past.

### 7.3 Consequence: `summaryText` should be derived from the outcome record

WP §4.2 carries `summaryText` as the mid-tier stub, produced by summarizing the payload. The pairing of intent claim and HV verdict is a better source for that stub than the payload is:

- It is already the compressed form — what was attempted, what resulted, and why — which is what a summary tries to reconstruct at a loss.
- It is cheaper: no separate summarization pass over the full payload.
- It is stable across regenerations, avoiding a second source of the churn in WP §10 risk 1.
- It is more faithful in both directions, because intent comes from the actor that held it and outcome comes from a validator with no stake in the answer. A payload summary has access to neither.

Using both halves matters. An HV verdict alone loses why the approach was chosen, which is what stops the same reasoning being retried; an actor claim alone is the self-assessment §6.3 rejects.

This is a design change to the summarized tier and should be evaluated as one, not adopted by assumption.

### 7.4 Consequence: a harmful turn is a new projection state

WP §4.2 allows `expanded`, `summarized`, `omitted`. None expresses "this turn is actively misleading." Once harm is detectable (§6.2), a turn that introduced a regression is badly served by all three: expanded, the model follows it; omitted, the failed approach is silently available to be retried.

The useful form is a marked stub — the approach, the fact that it regressed progress, and the explanation — which is cheap in tokens and high in value, since it prevents a repeat rather than merely recording history. Whether this is a fourth `projectionState` or an annotation on `summarized` is an open question (§10).

### 7.5 Consequence: explanations are the right indexing substrate

Explanations name causes. Link discovery (§3) over explanations is therefore both cheaper than indexing full payloads and better aligned with the admissible edge types, which are all causal or dependency assertions. `shares_root_cause` in particular is close to a direct comparison of two explanations.

---

## 8. Storage notes against the baseline

**Existing precedent for edge shape.** `GraphRelationshipEntity` (`src/McpServer.Storage/Entities/GraphRelationshipEntity.cs`) is a directed, typed, workspace-scoped edge with a `RelationshipType` string, a `Weight` double, an optional `Description`, and a `Metadata` JSON blob. That is close to the shape §3 needs.

**But it is not reusable as-is.** Its endpoints are `GraphEntityEntity` nodes via `SourceEntityId` / `TargetEntityId` foreign keys, not session turns. Turn-to-turn edges need their own table modeled on this precedent rather than rows in this one.

**No embeddings over turns.** The only embedding column found at the baseline is a nullable `Embedding` BLOB added to `Chunks` (migration `20260216171000_AddEmbeddingColumn`). Candidate generation for §5.2 cannot assume a vector index over turns exists; either one is built or candidate generation uses non-vector signals.

**Tombstone registration is mandatory, per IMPL §3.2.3.** Every table introduced here — edges, edge transitions, goals, scope/progress series, outcome records — is subject to the trap that section documents: `SoftDeleteTurnRowsAsync` and the revival path name their targets explicitly and discover nothing, so new tables keep `IsDeleted = false` when their turn is deleted.

This is more dangerous for links than for the sealed-turn table. An edge is *traversed automatically*. An unregistered edge table becomes a side channel that silently re-admits content from a deleted turn into a live projection — the operator deletes a session and the material keeps arriving through the index. So both remedies from IMPL §3.2.3 apply, and the second is not optional here: register every new table in both paths, **and** require traversal and projection reads to join through the non-deleted source turn.

**Audit path.** Per IMPL §3.2.1, set-based updates bypass the ledger. Edge transitions, scope and progress revisions, and outcome records should be written through the tracked save path so the history that §5.4 and §6.2 depend on is actually auditable.

---

## 9. Evaluation hooks

This capability must not be adopted on the argument that it sounds right. Hooks into WP §9:

- **Re-admit usefulness (WP §9.1 metric 4)** already exists and is the natural home: when a turn is admitted *by link*, did the subsequent action correctly use it? Report link-admitted and score-admitted separately, or the index hides inside an existing number.
- **Edge precision** — retraction rate per generation, available free from §5.4's append-only lifecycle. A rising retraction rate means the indexer is proposing badly.
- **Edge flip rate** — edges oscillating between confirmed and retracted, as a churn metric parallel to the `projectionState` flip rate in WP §9.1 metric 2.
- **Harm detection accuracy** — of turns flagged as introducing a regression, how many did on operator review, and how many introduced-versus-revealed classifications (§6.2) were correct?
- **Explanation verifiability** — fraction of explanations citing at least one checkable referent (§7.1). A falling fraction means explanations are drifting into narration.
- **Claim-verdict divergence rate** — how often the actor's intent claim and HV's verdict disagree (§6.3). Near-zero divergence is suspicious rather than reassuring: it suggests HV is deferring to the actor instead of adjudicating, which is the failure that would make the whole arrangement decorative.
- **HV adjudication cost** — validator calls and latency per turn, reported separately from §5.2's revalidation cost, since §6.3 expects this to be the dominant new expense.
- **Revalidation cost** — model judgments per turn and their share of turn latency, against the §5.2 cap. WP §10 risk 6 makes this a first-class concern, not a footnote.

---

## 10. Open questions

1. **What generates edge candidates** without a vector index over turns (§8), and at what cost? Quadratic pairing is not viable; candidate generation is the load-bearing engineering problem, not the judgment step.
2. **Is a harmful turn a fourth `projectionState`** or an annotation on `summarized` (§7.4)? The former is a schema change and touches WP §4.2; the latter risks being ignored by the projector.
3. **Who owns goal decomposition?** Scope only means something relative to a goal boundary. If the agent may split goals freely, it can manufacture favorable scope deltas (§6.3).
4. **How are turns attributed to goals** when a turn touches several, and how do nested goals aggregate scope and progress without double-counting?
5. **What is the admission threshold** for a link-driven re-admit, and does it differ from the WP §6 step 7 weight threshold?
6. **Does revalidation run on seal, on a schedule, or under budget pressure** (compare WP §10 risk 6's mitigation for re-score cost)?
7. **Is the link sub-cap (§4) fixed or adaptive**, and what happens when link admissions and pins together approach `budgetTokens`?
8. **Can an explanation be revised** if it was honestly wrong, given §7.2's append-only requirement? Presumably by appending a correction that supersedes it — mirroring the supersession-by-sequence resolution in IMPL §5.4.
9. **Who authors acceptance criteria** (§6.3)? HV removes self-reported progress but relocates the gaming surface to criteria authorship, and an actor writing its own trivially-satisfiable criteria defeats adjudication without ever lying. Operator-authored criteria are the safe answer and the least scalable one.
10. **What keeps HV independent in practice?** §6.3 requires a validator blind to the actor's account, but a validator sharing the same model, prompt lineage, or projection may reproduce the actor's blind spots and rubber-stamp them. Does independence require a different model, and is that affordable per turn?
11. **What happens when HV and the actor deadlock** — the actor re-attempts, HV re-rejects, and progress oscillates without scope changing? This is §5.3's churn problem in the goal layer and probably needs an escalation path to the operator rather than another retry.

---

## 11. Non-claims

Stated explicitly, because the underlying proposition — that retrospective linking improves outcomes — is attractive enough to be assumed rather than tested.

- **No efficacy claim.** That linked context produces more correct results is a **hypothesis**, not a finding. Nothing here has been built or measured. WP Principle 8 bars presenting it as a benefit until §9's metrics say so.
- **No token-savings claim.** Nothing here is offered as reducing token cost. §5.2 says the opposite: revalidation adds model calls, and the cost must be bounded and reported.
- **No claim that precision is achievable.** §4 argues an imprecise index is worse than none. Whether the required precision is reachable in practice is unknown, and a negative result is a legitimate outcome.
- **No claim of code readiness.** §8's references are a snapshot of one baseline commit, read for shape rather than verified by building anything.
- **No claim that self-reported signals are trustworthy.** §6.3 removes the actor from adjudication and §7.1 constrains both sides to checkable referents, precisely because the unconstrained versions are not trustworthy. Those constraints are reasoned, not demonstrated.
- **No claim that Hostile Validation is available, cheap, or complete.** §6.3 proposes HV as a runtime adjudicator; that is a design proposal. The existing document-level Perplexity HV remains blocked on an API key after box reseed and has **not** been run. Per the standing rule in the README and IMPL §9, HV must never be described as done.
- **No claim that an adversarial validator is correct.** §6.3 treats HV verdicts as evidence with their own error modes, including false accusation, and open question 10 records that a validator sharing the actor's model or lineage may simply reproduce its blind spots.

---

## 12. Document control

| Version | Date | Change |
|---|---|---|
| v0.1.1 | 2026-09-20 | Operator correction before first review: progress, regression and explanation records are outputs of **Hostile Validation**, not of the acting agent. §6.3 rewritten around actor intent claim versus HV verdict, with claim-verdict divergence as the harm signal; §7 and §7.3 reworked so the outcome record pairs both halves; the blindness constraint from §5.1 generalized into a single rule covering both link revalidation and progress adjudication. Records where gaming relocates to (criteria authorship) and adds open questions 9–11 on criteria provenance, validator independence, and actor/HV deadlock |
| v0.1.0 | 2026-09-20 | Initial draft. Captures four operator proposals from the 2026-09-20 design session: retrospective link index, continuous revalidation, per-goal scope and progress, and result-with-explanation records. Adds the re-admit circularity argument (§1), the joint-value representational limit (§2), the self-confirmation constraint on revalidation (§5.1), the introduced-versus-revealed correction to the harmful-turn rule (§6.2), and the traversal hazard that IMPL §3.2.3 creates for an unregistered edge table (§8) |
