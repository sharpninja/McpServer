# Weighted Context Projection — Addendum: Retrospective Linking and Goal Metrics

**Document:** addendum-retrospective-linking-and-goal-metrics-v0.1.md
**Version:** v0.1.5
**Status:** Draft. Codex findings from merged PR #56 are incorporated below. Not operator-approved. Proposes a capability beyond the v0.1.x scope of the whitepaper and the proposed implementation; nothing here is approved or scheduled.
**Companion to:** `whitepaper-weighted-context-projection-v0.1.md` (v0.1.9) and `proposed-implementation-weighted-context-projection-v0.1.md` (v0.1.4)
**Code baseline:** `main` @ `e7c43a125e1bb4837b5b9b9d4021ae2b592f931f`
**Date:** 2026-09-20 (revised 2026-09-25)
**Grounding:** Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`), the plan artifacts in `docs/plans/`, and `scripts/Validate-RequirementsTraceability.ps1`. §6 is written against that process rather than proposing a parallel one: the plan is the goal, and its declared requirements and acceptance criteria are the scope.

**Origin:** Operator design session, 2026-09-20. Five proposals, recorded here in the order raised: a retrospective link index (§2–§4), continuous link revalidation (§5), per-goal scope and progress metrics (§6), per-turn result-with-explanation records (§7), and Hostile Validation as the adjudicator of those records rather than the acting agent (§6.3) — an operator correction to this draft's first version, which had the actor grading itself.

**Cross-reference convention.** Bare `§N` refers to a section of this document. `WP §N` refers to the whitepaper. `IMPL §N` refers to the proposed implementation.

**Scope.** This addendum is deliberately separate. The whitepaper and the proposed implementation are in operator review at v0.1.9 / v0.1.4; folding a new capability into them would blur what is being approved. If the ideas here are accepted, §2, §3 and §6.1 belong in the whitepaper as design, and the rest belongs in the implementation proposal.

**Status of every claim here.** Design argument and mechanism, not measured result. §11 states the non-claims explicitly. Code references were read against the baseline commit; they are a snapshot and could be wrong.

---

## 1. Why this exists: weight-triggered re-admit does not recover joint value

WP Principle 4 makes re-admit first-class: "if a quiet constraint suddenly matters, the scorer must be allowed to pull the full turn back from SessionLog." WP §6 step 7 gives it exactly one trigger — an omitted turn's `weight` rising enough on a later reweight pass.

That trigger is not blocked by the turn's absence from the assembled projection. WP §6 step 1 loads every SessionLog turn for the session before weights are refreshed, and WP §5.2's v2 judge is given the goal, the recent projection, and candidate-turn stubs. An omitted turn is absent from the model-facing assembly and present, as a stub, in the reweighting input. The scorer can compare that stub with the current failure and raise the turn's weight. An earlier draft of this section claimed re-admit cannot fire because omission hides the turn from the scorer. That claim contradicts the design this addendum extends, and it is retracted.

What remains is a representation limit, not an invisibility limit.

The scorer sees `summaryText`, not the sealed payload (WP §4.2, WP §6 step 5). A stub that dropped the quiet fact — an operator constraint, the exact error text, an environment condition (WP §5.1) — cannot suggest the relevance of that fact, and the v2 judge has no other view of an omitted turn. Locally obvious relevance that survives in the stub (a repeated identifier, the same file) can still raise weight. Relevance that exists only as a relation between turns cannot: `score(turn, currentWork)` is still a per-turn scalar (§2), and nothing in the stub encodes that two turns are jointly load-bearing.

So re-admit fires for turns whose stub still carries the signal. It does not fire for turns whose value is joint, or whose signal was summarized away. Those are the cases re-admit was introduced to recover. WP §10 risk 2 ("bad scorer drops quiet constraints") names the symptom; its stated mitigation is a constraint detector at write time, which helps only for constraints recognizable as constraints when written, and only if that detector's output is retained in the stub (§7.3).

The link index below is the proposal for the joint-value hole. It does not replace the scorer's existing path from stub to weight. Nothing in the current scoring model closes the joint-value hole. It is a structural limit of the representation, not a tuning problem.

For provenance: the reinforcement idea this whole design rests on is stated in BDPv4's monitoring list, where a steering message that re-surfaces workspace instructions after compaction "reinforces the weight applied to those requirements, which over time help the compaction algorithm to retain such instructions" (`docs/Development-Process-draft-v4.md`, Implementation). Weighted projection is a mechanization of that observation, and §6 is the part that supplies the progress measurement the observation assumes.

---

## 2. The representational limit: a scalar cannot express joint value

The deeper issue is what `weight` can represent.

Consider a session where turn 4 records an environment constraint in passing and turn 60 hits a failure the constraint explains. Scored individually against current work, turn 4 is unremarkable — it mentions no current identifier, it is old, it reads as background. Turn 60 is obviously relevant. The information that resolves the situation is not in either turn; it is in the *pair*.

A per-turn scalar has no way to encode that. Scoring functions of the form `score(turn, currentWork)` are evaluated independently per turn, so they cannot express "these two turns are jointly worth more than the sum of their parts." No improvement to the scoring function reaches this, because the limitation is in the shape of the representation rather than in the quality of the estimate.

Expressing joint value requires a relation between turns as a first-class object. That is the argument for a link index, and it is a design-level claim: the whitepaper's scoring model is incomplete in a way that cannot be fixed inside the scoring model.

This is a generalization of the pattern already accepted elsewhere in the design: supersession and `tokenEstimate` both turned out to be *relations* or *derived functions* rather than intrinsic properties of a turn (IMPL §3.4, §5.4). Links are the same move applied to relevance.

---

## 3. Retrospective link index

**Proposal.** A service that periodically examines sealed turns and records typed, directed, weighted edges between turns whose connection is discoverable only in hindsight. The projector may then admit one endpoint because the other is load-bearing, rather than only because its own score rose, and only in the direction that edge type allows (§3.1). A link is not a license to pull either end into the assembly.

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

The list is not closed. A new type is admissible only when traversing it changes what work should happen next, and only after §3.1 has a row for which endpoint may be admitted. "These are about the same topic" fails that test. There is no default admission direction for a type that lacks a row.

**Every edge carries its evidence.** An edge records the turns it connects, its type, a confidence value, and a reference to what justifies it — which acceptance criterion, which identifier, which recorded outcome, which sealed facts. An edge whose justification cannot be inspected cannot be audited when it turns out to be wrong, and §4 argues wrong edges are the primary hazard. Shared membership in a criterion is a candidate (§5.4), not this justification, and not a type.

### 3.1 Direction-specific admission

Directed storage records which turn is the source and which is the target. It does not say which endpoint may re-admit the other. A blanket rule — if either end is load-bearing, admit the other — injects false context even when the edge is correct. When a load-bearing source `invalidates` an older target, admitting the target injects the obsolete result the edge identifies. When a source `constrains` a target, the direction that matters is the opposite: the constraint is what the projection was missing.

Admission is defined per type. An edge whose type is not in this table is not traversable for admission until its row exists. There is no default direction.

| Type | Consult the edge when | Admit | Do not admit by this edge |
|---|---|---|---|
| `invalidates` | The target is in the assembly or is a plausible retry, or the source is load-bearing and names a result the actor might follow | The source, as the invalidating decision, expanded or stubbed. If the target is already present, force it to a marked stub: the obsolete result, and that this edge invalidates it | The target's full payload. That is the obsolete result |
| `constrains` | The target is load-bearing | The source, the commitment that limits what the target may do | The target. The constraint does not make the constrained work current |
| `explains` | The target, the observed outcome, is load-bearing | The source, the recorded cause | The target. It is already why the edge was consulted |
| `depends_on` | The target is load-bearing | The source, the precondition the target's result is contingent on | The target. Later contingent work is not pulled in because its precondition is in view |
| `shares_root_cause` | Either endpoint is load-bearing | The other endpoint. Storage direction is arbitrary; admission is symmetric, and neither end is obsolete by this type alone | Nothing is retired by this type. A separate `invalidates` edge is required before a payload is forced to a stub |

Every link-admitted turn is marked as such (§4). The link sub-cap still applies. Tombstoned endpoints are ineligible on both sides (§8).

---

## 4. Why false links are worse than false weights

A weighting error omits something useful. The budget invariant still holds, the omitted turn is still recoverable per WP §6, and the projection remains internally honest — it simply lacks something.

A link error *injects* content, and injects it carrying an implied causal claim: this material bears on your current situation. A model receiving a wrongly linked turn is not merely carrying extra tokens; it has been handed a false lead with an institutional endorsement, and it will reason from it. The failure is active rather than passive.

Three consequences:

1. **Precision dominates recall.** A link index at 60% precision is worse than no index, because a wrong lead costs more than a missing one. Tune for few, strong edges.
2. **Link-admitted turns need a sub-cap inside `budgetTokens`.** Otherwise a burst of spurious edges can crowd out pinned turns and current work, converting a relevance error into a budget failure. The WP §6 hard budget remains the outer bound; link admissions get a fraction of it.
3. **Marking matters.** A turn admitted by link should be identifiable as such in the assembly, so a reader — human or model — can discount it. Silent injection removes the ability to tell a retrieved lead from established context.

### 4.1 Prior art: the adversarial link graph

Web search solved a structurally identical problem — a link graph whose edges are cheap to fabricate and profitable to fabricate — and the public record is worth reading before designing an indexer, because it does not vindicate the §4 position as stated.

**Links are weighted, not counted.** Google's "reasonable surfer" work models a user who follows some links more often than others, weighting each link by the probability it is actually clicked given position, prominence and context, rather than treating every link on a page as an equal vote ([US 8,117,209 B1](https://patents.google.com/patent/US8117209B1/en)). This is the nearest public analogue to typed, weighted edges over uniform ones. It is not a relation taxonomy.

**Bad edges are neutralized, not punished, and the credit is not recoverable.** Google describes using its spam system "to neutralize the impact of unnatural links on search results," with ranking changing "as spammy links are neutralized and any credit passed by these unnatural links are lost" ([Google Search Central, December 2022](https://developers.google.com/search/blog/2022/12/december-22-link-spam-update)), and states that once "our systems remove the effects spammy links may have... Any potential ranking benefits generated by those links cannot be regained" ([Google Search Central documentation](https://developers.google.com/search/docs/appearance/spam-updates)). That is §5.5's retraction lifecycle: withdraw the edge's contribution, keep the record.

**Edge annotations are hints, not directives — explicitly so information is not lost.** Google treats `sponsored`, `ugc` and `nofollow` "as hints about which links to consider or exclude within Search," reasoning that "by shifting to a hint model, we no longer lose this important information, while still allowing site owners to indicate that some links shouldn't be given the weight of a first-party endorsement" ([Google Search Central, 2019](https://developers.google.com/search/blog/2019/09/evolving-nofollow-new-ways-to-identify)). An edge marked untrustworthy is retained and downweighted rather than deleted — the same choice §5.5 makes.

**Trust is propagated from a human-evaluated seed set, as a counter-bias rather than a score.** TrustRank selects "a small set of seed pages to be evaluated by an expert," propagates trust along links, and is offered as "a counter-bias to be applied when results are ranked, in order to discount possible boosts achieved by spam," noting that plain PageRank "does not incorporate any knowledge about the quality of a site, nor does it explicitly penalize badness" ([Gyöngyi, Garcia-Molina and Pedersen, VLDB 2004](https://www.vldb.org/conf/2004/RS15P3.PDF)). This is the one result here that transfers cleanly: §6.4's passing tests and §6.3's HV verdicts are an expert-evaluated seed set, and edge confidence should decay with distance from a verified referent rather than being asserted per edge.

**The uncomfortable part.** The load-bearing signal in web search turned out not to be the link graph at all. A Google engineer's internal presentation, entered as a trial exhibit, states: "We do not understand documents. We fake it... Beyond some basic stuff, we hardly look at documents. We look at people. If a document gets a positive reaction, we figure it is good" ([UPX0203, U.S. v. Google](https://www.justice.gov/d9/2023-11/417516.pdf)). Google staff have since said links are not among the top three ranking signals and that "we need very few links to rank pages" ([Search Engine Land](https://searchengineland.com/links-google-search-ranking-factor-gary-illyes-432422)).

So the honest reading of the prior art is that the declared graph was never made trustworthy — it was **demoted in favour of an independent signal the ranking system did not author**: recorded reactions from millions of parties with no stake in any particular document's rank. A single agent session has no such population. §6's insistence that progress be read from acceptance-criteria state, and §6.3's insistence that the verdict come from a party that did not do the work, are the only available substitutes, and they are far weaker than a billion independent observations.

Two further disanalogies constrain how much of this transfers:

- **Opacity is not available.** Web search can partly defend a signal by not publishing it, and even that eroded — the 2024 Content Warehouse documents contradicted public statements about whether site-level authority and click signals existed. Here the actor is the same class of model, reading the same repository, including this document. Any anti-gaming property that depends on the actor not knowing the rule is void by construction.
- **The adversary is inside the system.** Web spam is third parties with a profit motive, and the remedy is demotion of the document. Here the misaligned party is the actor itself, and the closest available remedy is the one BDPv4 already prescribes for a session whose trust has broken: end it and start over (§6.4).

---

## 5. Continuous revalidation

**Proposal.** Every edge is checked after every turn. The cheap check is the §5.2 gate: both endpoints non-deleted (§8), both seal sequences still the ones the latest judgment named, and no endpoint weight, projection state, or goal attribution moved. An edge that fails the tombstone or seal check is ineligible until it is re-judged or retracted. A model is asked only for the ordered subset in §5.2, and a new judgment still has to clear the §5.3 hysteresis band and cooldown before it flips admission. The pass finds missing links as new turns arrive and retracts links that no longer hold. Links become per-generation hypotheses rather than stored facts, which is how §4's precision bar is pursued continuously instead of being demanded of the initial judgment. Whether that bar is met is measured with independent labels (§9), not by how often edges are retracted.

Three constraints make the difference between this working and this entrenching errors.

### 5.1 Evaluation must be blind to the projection it influenced

If a false edge admits turn 4, and the next evaluation judges that edge while turn 4 is present in the assembled context, the evaluator sees a coherent narrative and confirms the edge. The edge has become its own evidence. Each generation then raises confidence in a false link, and revalidation — the mechanism introduced to remove false links — entrenches them instead.

**Requirement:** edges are judged against sealed payloads and recorded outcomes directly, out of band, never against the projection they contributed to. The convenient implementation is to ask the model about the context it already has; that implementation has the failure built in.

### 5.2 Evaluate the decision boundary, not the graph

Re-judging every edge after every turn is `O(E)` model judgments per turn. With candidate generation producing even a sparse `k` edges per turn, `E` grows with session length, so cost per turn grows and total session cost is superlinear — while competing with the live turn the index exists to help. WP §10 risk 6 already flags per-turn re-score cost as a live concern; this multiplies it.

Only edges that could change a decision this generation are worth a model call. An edge between two turns that are both currently omitted, with confidence well away from the admission threshold, cannot alter the projection no matter how it is judged.

The requirement graph also bounds which pairs are eligible in the first place — see §5.4, which is the cheaper half of this problem. It generates candidates. It does not supply the causal type.

**Judgments are bound to seal sequences.** Each confirmed or retracted judgment records the seal sequence of both endpoints it was judged against. IMPL §5.4 derives the current seal as the row with the highest sequence. A later judgment against a different pair of sequences is a new transition (§5.5), not an edit of the old one. An edge is eligible for admission only while both endpoints' current sequences match the sequences on its latest confirming judgment.

**Reseal invalidates.** IMPL §5.1 permits a post-terminal mutation — `ReplaceTurnAsync`, section replace, item delete, title set, or a dialog append on an already terminal turn — to produce a new seal without changing `weight`, `projectionState`, or goal attribution. Those three are not a sufficient change detector. If either endpoint's current seal sequence differs from the sequence the edge was last judged against, the prior judgment is void and the edge is ineligible for admission until it is re-judged against the new payloads. The superseded payload is not evidence for the current edge. A reseal is an invalidation trigger ahead of any model call, in the same class as the ordering below, and it outranks "nothing else moved, skip the model."

Ordering:

1. Edges currently load-bearing — those admitting a turn now.
2. Edges whose endpoint seal sequence changed since the last judgment, including reseal after post-terminal mutation (IMPL §5.1, §5.4).
3. Edges with an endpoint whose `weight`, `projectionState`, or goal attribution moved this generation.
4. Edges near the admission threshold in either direction.
5. Stale edges, by generations since last evaluation.

Cheap invalidation gates the expensive judgment: if neither endpoint's seal sequence changed, neither endpoint's weight, projection state, or goal attribution changed, and nothing near the boundary moved, no model is asked anything. The count of judgments per generation is capped by configuration, so revalidation cost is bounded by design rather than emergent from graph size.

### 5.3 Edge churn, and why the plan bounds it

Thrashing — oscillating scores, churning projection, dead prompt cache — reappears here in a harder-to-see form, because a single edge flipping can swing several turns in or out at once. Edges need the discipline weights already have: a confidence hysteresis band rather than a single threshold (compare `Δweight` in WP §5.2), and a cooldown keyed to a generation counter, mirroring `lastReAdmitGeneration` in WP §4.2. Edge flip rate is a metric in its own right (§9).

**Hysteresis and cooldown stay primary until turn density is actually bounded.** A BDPv4 phase has a declared, finite acceptance-criteria set (§6.4), and those criteria are pinned by construction under WP §4.2's **acceptance** pin class. That bounds which criteria are in scope. It does not bound the volatile turn set or the edge set. One criterion can accumulate arbitrarily many retry turns, and the edges among them can swap those turns through the residual budget while the criterion itself stays pinned. Pinning the criterion does not pin the turns. v0.1.4 treated the finite set as the first line of defence and hysteresis as a refinement over a bounded residual. That overstated what the anchor bounds. Until a per-criterion density cap is defined — what it counts, the numeric limit, and what the projector does at the cap — hysteresis and cooldown are the primary defence against projection and cache instability. The criteria set remains a scope constraint on which pairs §5.4 may propose, not a substitute for those dampers.

Criteria themselves still move slowly, which is a useful property and not a churn bound on turns. BDPv4 expects requirement refinement every iteration, through approved diffs to plan artifacts at human cadence, not per turn. That rate applies to the criterion list. It does not apply to the turns attributed to one criterion.

Three things this does not cover, each of which the dampers still have to hold:

- **Phase boundaries are not thrash.** When a phase or slice completes, the relevant criteria set turns over wholesale and the projection legitimately changes with it. That is a scheduled flush — one predictable cache loss at a known point, which is cheaper than continuous churn — and the §9 flip-rate metric must be conditioned on phase boundaries or it will alarm loudest exactly when the system is behaving correctly.
- **Unattributed turns stay unbounded.** Turns that bear on no acceptance criterion — exploration, environment fights, debugging that led nowhere — fall outside the criteria set, and churn among them is bounded only by hysteresis and cooldown.
- **Bounded criteria do not bound turns per criterion.** A loop that retries the same criterion twenty times produces twenty turns all legitimately attributed to it, and the edges among them are dense, mutually confirming, and nearly worthless. This is why the density cap has to exist before the plan can be described as a churn bound. A criterion accumulating turns without a progress change is the §10 open question 11 deadlock signal rather than a linking opportunity. Until the cap is specified, open question 13 records it as unset, and §5.3 does not assume it.

### 5.4 Candidate generation is a traversal of the requirement graph

The hardest engineering problem in §2–§4 is which pairs to consider at all, and a plan-anchored design answers it without a vector index. Acceptance criteria carry stable identifiers that bind to requirements, test ids, named test methods, and slices (§6.1), and requirement families already have a human-authored relation structure recorded in the traceability mapping and matrix (§6.4). Turns inherit those identifiers by attribution. So candidates are turns reachable through shared or related criteria, not turns that resemble each other.

Three consequences, in descending order of how much they matter:

- **No quadratic pairing and no embedding dependency.** §8 records that the only embedding column at the code baseline is on `Chunks`, so an index over turns would have to be built. Traversing identifiers avoids the question entirely for the first cut.
- **Shared criteria generate candidates, not edge types.** "Both turns bear on `AC-FR-MCP-MEMORY-010-02`" is common membership. It does not establish `constrains`, `invalidates`, `explains`, `depends_on`, or `shares_root_cause`, and it does not establish direction. Treating membership as one of those types would be the `similar_to` relation §3 rejects, under a checkable name. The requirement artifact may propose the pair. The causal type and the direction still require evidence in the sealed payloads or the outcome records, or an explicit judgment against that evidence (§3, §3.1, §5.1). The checkable referent §7.1 demands is the evidence for the typed edge, not the shared identifier by itself. The cheap identifier removes the temptation to use similarity as the only signal; it does not remove the judgment.
- **Candidate pairing is narrower than topical similarity; edge precision is not free.** §4 argues a 60%-precision index is worse than none. Criteria-derived *pairs* are wrong mainly when attribution is wrong, which is a narrower and more auditable failure than topical drift. A correct pair with an invented type or the wrong direction is still a false lead (§3.1). Precision of admitted edges is measured with independent labels (§9). It is not assumed from the candidate generator, and it is not the retraction rate.

**Bounding by phase would reintroduce the hole this addendum exists to open.** The valuable links are frequently long-range — §1's omitted constraint and §2's joint-value pair typically sit in different phases. Time is not the bound. The bound is the plan structure that is actually recorded, which at the baseline supports three traversals:

- The same acceptance criterion across any phase, where a catalog binds that criterion id.
- Sibling criteria under one requirement, where the catalog or the requirement document groups them.
- Co-membership of an FR with its primary TRs and `TEST-*` ids, from `docs/Project/TR-per-FR-Mapping.md`.

**A requirement-to-requirement dependency graph is not in those artifacts.** `docs/Project/TR-per-FR-Mapping.md` maps each FR to primary TRs and tests. `docs/Project/Requirements-Matrix.md` maps each requirement to a status and source files. Neither records an edge between requirements, a direction, or what following that edge would mean. An earlier draft named "requirement-to-requirement dependencies from the matrix" as a traversal this design could execute. It cannot, and the long-range candidates that exist only as cross-requirement dependencies are absent until an artifact declares them. That artifact would still be candidate generation under the rule above: a dependency between requirements proposes pairs, and the turn-level edge type and direction still need payload or outcome evidence. Semantics required before any such traversal runs: what the requirement edge asserts, which direction it has, and that it is a hint rather than an admission. Open question 12 records the gap. Inventing the graph inside the indexer would recreate the unsupported causal edge this section exists to avoid.

This does not cover turns bearing on no criterion, which is where a similarity fallback would have to earn its place separately, under the §4 precision bar and with the `similar_to` objection answered rather than ignored.

### 5.5 Lifecycle is append-only

An edge's history is a sequence of recorded transitions — proposed, confirmed, retracted, re-proposed — each with its reason and generation, not a mutated confidence column. Two reasons:

- **Auditability.** When a projection changes between generations, the operator can ask why, and the answer is a readable record rather than a diff of vanished state. Each transition also records the endpoint seal sequences it was judged against (§5.2), so a reseal is visible as an invalidation rather than as a silent reuse of the old judgment.
- **Free churn measurement, not free precision.** The lifecycle yields a retraction rate and a flip rate per generation with no extra harness. Those are churn metrics. They are not precision. A false edge may stay confirmed indefinitely, and a correct edge may be retracted because a reseal or a later turn changed its applicability. Precision is `true positives / proposed positives`, and the labels have to come from a party that did not propose the edge — operator review, or another adjudicator judging the sealed payloads. Using retraction rate as that ratio would make the §9 report invalid. See §9.

IMPL §3.2.1 applies: if edge state is flipped through set-based updates, those writes bypass the audit ledger. Edge transitions should go through the tracked save path.

---

## 6. Goal metrics: scope and progress

### 6.1 The measurement that weight has been missing

WP §5 scores weight as relevance "to current work progress," and nothing in the design measures progress. The central quantity is defined against an unmeasured one.

**The goal is the plan, not the turn.** This is the unit correction that makes the rest of §6 tractable, and an earlier draft got it wrong by treating each turn as a thing to be scored. A turn has no goal of its own; it either moves the plan or it does not. Byrd Development Process v4 already fixes the unit: "Completion is not a definitive end state, simply a declaration of a set of requirements and acceptance criteria and proof of achievement of both" (`docs/Development-Process-draft-v4.md`, Planning). A plan declares the criteria; a turn contributes proof.

**Proposal.** Track two values per plan:

- **Scope** — the declared requirement and acceptance-criteria set as currently understood. Rises when the plan grows or proves more complex than believed.
- **Progress** — acceptance criteria with proof of achievement.

This gives §6 the units it otherwise lacked. BDPv4 plans in this repository carry Functional, Technical and Testing requirements with stable identifiers, and `docs/plans/mcp-memory-002-ac-catalog.json` enumerates 283 acceptance criteria that each bind a statement to a requirement, a `TEST-*` id, a named test method, and a slice. Scope is the count of declared criteria; progress is the count whose bound test method passes. Commensurate by construction — the §6.3 commensurability requirement is satisfied by the artifact rather than by convention — and BDPv4's phase-exit gate ("the full test suite for the current increment plus all prior work must be green") reads that plan-level progress from a test run rather than from anyone's judgment. The reading is the checkpoint tier in §6.3. It is not a per-turn verdict, and `Validate-RequirementsTraceability.ps1` does not compute it (§6.4).

Two independently moving lines, rather than a single percent-complete, which is the property that makes the pair robust to a goal changing underneath the measurement. A completion ratio alone cannot distinguish "advanced" from "target moved." This is the burn-up / earned-value shape, and that lineage should be cited if this enters the whitepaper — WP §12 is frozen for v0.1.x, so it is named here as a concept and needs a reference in v0.2 rather than a citation added now.

Both values belong to the plan. A turn's reading is the pair of plan-level deltas attributed to it:

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

BDPv4 states this outcome as an expectation rather than a hazard, under "Resolving Defective Requirements": writing tests against acceptance criteria surfaces "paradoxes created by mismatched priorities, ambiguity and incorrect rules," and "Expect to refine requirements in each iteration... Expect to touch previously written code... This is not a failure of the process, but validation that the core philosophy of iterative improvement is alive and working." The argument below is therefore a restatement of existing process doctrine in metric terms, not a new finding, and it is cited here as precedent rather than presented as a discovery.

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

One direction of divergence is the harm case in §6.2: the actor claims a criterion was met and HV finds it was not, detected without anyone having to trust the actor. Agreement is not a failure mode. When the claim is accurate and the work is straightforward, a competent independent validator should agree. Near-zero divergence is compatible with a healthy batch and with rubber-stamping; the rate alone cannot tell them apart, and treating agreement as capture would reward false adverse verdicts (§9).

**HV is not ground truth either.** It is an adversary with its own error modes, false accusation among them, and it may be wrong in the other direction. So verdicts are evidence rather than fact: append-only (§6.2), and anchored to checkable referents rather than to HV's unsupported opinion. WP §4.2's `pin` classes already include **acceptance**, which supplies the anchor — HV's job is to check acceptance criteria, tests, builds and artifacts, and to label any interpolation between them as interpolation rather than as measurement.

**Where the gaming surface moves.** HV closes self-reporting; it does not by itself stop an actor from arranging favorable criteria. §6.4 closes the part planning can close: criteria precede the turn, and dropping an `FR-` (and, in strict mode, a `TR-` or `TEST-`) from the requirements documents fails the traceability script. It leaves the catalog-to-test surface open. Deleting an acceptance criterion, or weakening the test bound to it while the matrix row remains, is not a script failure.

**Cost and cadence.** Plan-level progress in §6.1 is criteria with passing tests. BDPv4 requires the full suite for the current increment plus all prior work to be green before an implementation phase exits (`docs/Development-Process-draft-v4.md`, Implementation). It does not require that suite after an ordinary turn inside the phase. A phase-exit run is one reading for the phase. It cannot attribute a pass or a failure to an individual turn: every turn since the previous green reading is a candidate cause. Reusing that eventual run as a per-turn verdict would invent attribution the runner does not have. Running the full suite after every turn would be a new cost, and it would contradict any claim that per-turn readings are free.

Per-turn HV verdicts use three tiers. The cost sits with the tier that actually ran:

1. **Intent only, between runs.** After an ordinary turn the actor's intent claim is recorded and the HV verdict is `pending`. No suite is launched. This tier does not fill §6's scope/progress table and it is not a progress delta.
2. **Batched attribution, at a checkpoint.** The next scheduled run — a slice checkpoint the plan already names, or the phase-exit suite — produces one scope/progress reading for the batch. HV attributes the delta to turns inside the batch. The candidate set for a criterion whose bound test changed state is the turns, since the previous green reading, that modified that test or the code the test exercises. Turns outside that set stay unattributed for that criterion. Attribution is a judgment over that candidate set (§6.2, introduced versus revealed), not a property of the test runner. The suite is charged once per batch on the §9 HV adjudication cost, not once per turn, and not described as free.
3. **Targeted tests, when a turn-level verdict cannot wait.** A harm flag, link evidence that cites a criterion, or the §10 deadlock threshold may request the tests named by the affected criteria, not the full suite. That request is a cost BDPv4's phase-exit gate does not already pay. It is reported per request in the §9 HV adjudication cost. It is not the default tier.

Model adjudication remains what tests cannot decide: whether a passing test demonstrates its criterion, and the introduced-versus-revealed split inside a batch. That ordering follows WP §10 risk 6 ("v1 rules by default; judge on schedule or on budget pressure"). An earlier draft of this section called the phase-exit suite the per-turn base tier and said most readings are free. That collapsed a phase aggregate into a per-turn verdict. The phase-exit suite remains the BDPv4 gate and the plan-level reading in §6.1. It does not yield per-turn verdicts. If targeted runs or full-suite runs grow with turn count, adjudication has become the default tier. Nothing here claims HV is cheap or currently available; the existing document-level Perplexity HV remains blocked on an API key and must not be described as done.

**Commensurability.** Scope and progress must share units, or independent movement means nothing. If scope counts known acceptance criteria, progress counts criteria met. If scope is in estimated units of work, progress must be in the same units. Mixing "complexity" with "criteria completed" produces two series whose deltas cannot be compared.

**Descoping.** Removing scope inflates any completion ratio without work being done. Every scope change records direction, reason, and justification, and the ratio must never be a reward signal on its own. A scope reduction is a claim that work is unnecessary, and that claim is exactly as checkable as a progress claim. BDPv4 places that decision outside the actor entirely: "stakeholders need the flexibility to iteratively approve or deny continued resource expenditure towards completion." Descoping is a stakeholder act, and an actor-initiated one is a request, not a scope change.

### 6.4 Planning closes the criteria surface

Criteria authorship is not left to the turn that will be judged by it. BDPv4 requires the criteria to exist before implementation: "Planning results in a set of artifacts that capture Functional Requirements... Technical Requirements... Testing Requirements... and Iterative Phases," and "System components need to be discovered, designed and all public interfaces documented before writing implementation code." Plans in `docs/plans/` name their requirement families in the header and withhold `Done` until independent gates pass — `PLAN-LLMSTRATEGY-001-bdpv4.md` records "leave `Done: false` until Codex READY, tests Failed 0/Skipped 0, and hostile AGREE." Three gates, none of which the acting agent controls.

Two mechanical properties do the work that §6.3's adjudication cannot:

- **Criteria precede the turn.** An actor cannot mint a favorable criterion mid-turn, because the criterion set is a plan artifact authored before implementation and versioned with it. Scope changes are visible as diffs to that artifact.
- **Identifier coverage is machine-checked. Catalog-to-test coverage is not, and that surface stays open.** `scripts/Validate-RequirementsTraceability.ps1` compares identifiers in `Functional-Requirements.md`, `Technical-Requirements.md`, and `Testing-Requirements.md` with `docs/Project/TR-per-FR-Mapping.md` and `docs/Project/Requirements-Matrix.md`. It exits non-zero when an `FR-` heading is missing from the mapping or the matrix, and in strict mode when a `TR-` or `TEST-` identifier is missing from the matrix. It does not read `docs/plans/*-ac-catalog.json`, it does not resolve named test methods, and it does not compare an acceptance-criterion statement with the test that is supposed to demonstrate it. Quietly dropping an `FR-` from those requirement documents fails the script. Deleting an acceptance criterion from a catalog, or deleting or weakening its bound test while the corresponding `TEST-*` row remains in the matrix, still passes. That is the criteria-gaming surface this section is about, and the script leaves it open. Closing it would take a separate catalog-to-test check this addendum does not claim exists. Until that check exists, a catalog edit or a bound-test edit is visible to a reader of the diff and invisible to this gate.

**The real gaming mode is narrower than criteria invention, and it is already documented.** BDPv4's monitoring list names it: an agent stuck in "unresolved loops of failing tests... can lead to the agent marking a test as invalid so it can keep moving forward while trying to be a useful assistant, losing its identity as a precise software engineer in the process." That is criteria gaming as actually observed — not authoring an easy criterion, but invalidating a criterion that is bound to a test and blocking. A reader of the diff can see a test method named in the acceptance catalog changing state to skipped, deleted, or rewritten. `Validate-RequirementsTraceability.ps1` will not. This is why BDPv4's phase-exit gates count skips (`Failed 0/Skipped 0`) rather than only failures, and why a test-state change touching a catalogued method should be treated as a scope change requiring stakeholder approval rather than as ordinary refactoring. The gate that would fail the build on that diff does not exist yet; the stakeholder rule is the control that does.

**The residual is human.** BDPv4 does not claim automation closes this — its mitigations for all three monitored behaviors are an experienced human steering in real time, and for a session whose trust is broken, abandonment: "its impossible to fix the trust and get the model to behave correctly. Simply end that session, close that agent, and start over." Any claim that this addendum removes the operator from the loop would contradict the process it depends on.

---

## 7. Turn outcome records: result plus explanation

**Proposal.** Each turn carries a record of its result together with an explanation of why that result occurred. The explanation, not a bare number, is the signal that determines whether the turn advanced or regressed the goal.

This is what makes §6 operable. A progress delta on its own cannot distinguish introduced from revealed regression (§6.2), nor justify a scope change (§6.3). The explanation supplies the attribution that both need.

Per §6.3, the explanation is an **HV output, not an actor output**. The actor contributes its intent claim; the adversarial validator accounts for what actually happened. An earlier draft of this section had the actor explaining its own result, which reintroduced the exact gaming surface §6.3 exists to close — and free-text self-justification is *worse* than a self-reported number, because prose can persuade in ways a number cannot.

### 7.1 Explanations must be structured claims, not prose

Moving authorship to HV removes the incentive to flatter; it does not by itself make an explanation checkable. An adversary can also be confidently wrong, and a fluent accusation is as unfalsifiable as a fluent excuse.

So explanations — from either side — are constrained to verifiable referents: which acceptance criterion changed state, which test moved, which prior turn is contradicted, which file or identifier is implicated, which goal is affected and in which direction. Then the account is checkable rather than merely persuasive, and one citing nothing checkable is visibly weak on its face. This applies symmetrically to the actor's intent claim and to HV's verdict; neither gets to assert an outcome it cannot point at.

#### 7.1.1 The explanation's destination is the plan, not just the metric

BDPv4 already uses explanations this way, and for a purpose stronger than scoring: "ask the model what caused it to go on a tangent, and how the requirements and workspace guidelines could have guided it towards the correct path to take. Then have **THAT** model update the documentation and guidelines." The explanation is an input to criteria refinement.

That closes a loop §6.4 would otherwise leave open. Criteria precede the turn, so an actor cannot revise them to suit its own result — but criteria are also frequently the thing at fault, which is BDPv4's "Resolving Defective Requirements" case. Routing explanations into requirement and guideline revision lets defective criteria be fixed without letting the actor fix them *for its own verdict*: the revision is a scope change to a plan artifact, visible as a diff and subject to the stakeholder approval in §6.3.

### 7.2 Explanations are append-only evidence

An explanation is written at the time, from what was then known, but the signal it feeds is retrospective. Later turns may show it was mistaken. It must therefore be preserved and reinterpreted, never rewritten: the record is evidence-at-the-time. This is the same append-only requirement that §5.5 places on edges and §6.2 places on the progress series, and for the same reason — retrospection needs an unedited past.

### 7.3 Consequence: `summaryText` keeps residual payload facts

WP §4.2 carries `summaryText` as the mid-tier stub, produced by summarizing the payload. The outcome record is an input to that stub. It is not a sufficient source for it.

An earlier draft said the pairing of intent claim and HV verdict should replace the payload summary: it is already compressed, it avoids a second summarization pass, it is stable across regenerations, and each half has an author the payload summary lacks. That replacement drops facts that are neither the attempted criterion nor the outcome. WP §5.1 treats those facts as high-weight inputs: an operator constraint that has not been externalized to memory, the exact error text of an unresolved blocker, an environment condition, the latest tool result the next action depends on. WP §5.2's v2 judge re-scores omitted turns from candidate stubs, not from full payloads (§1). A stub that kept only intent and verdict makes those facts invisible to scoring and to the live projection, which removes the quiet constraints this addendum is trying to keep recoverable.

**Requirement.** `summaryText` is derived from the outcome record and from residual payload facts. The outcome record contributes what was attempted, what HV judged, and the checkable referent (§7.1). The payload contributes facts the outcome record does not state. A derivation that cannot point at those facts in the sealed payload is incomplete. Intent plus verdict is the outcome half of the stub, not the stub.

The cost and stability arguments still apply to the outcome half. They do not justify omitting the payload half. Inside the outcome half, both roles still matter: an HV verdict alone loses why the approach was chosen, which is what stops the same reasoning being retried; an actor claim alone is the self-assessment §6.3 rejects.

This is a design change to the summarized tier and should be evaluated as one, not adopted by assumption.

### 7.4 Consequence: a harmful turn is a new projection state

WP §4.2 allows `expanded`, `summarized`, `omitted`. None expresses "this turn is actively misleading." Once harm is detectable (§6.2), a turn that introduced a regression is badly served by all three: expanded, the model follows it; omitted, the failed approach is silently available to be retried.

The useful form is a marked stub — the approach, the fact that it regressed progress, and the explanation — which is cheap in tokens and high in value, since it prevents a repeat rather than merely recording history. Whether this is a fourth `projectionState` or an annotation on `summarized` is an open question (§10).

### 7.5 Consequence: explanations are an indexing substrate, not a sufficient one

Explanations name causes, so they are a useful index for the admissible edge types, which are causal or dependency assertions. `shares_root_cause` in particular is close to a comparison of two explanations. They are not a sufficient index. Residual payload facts that never entered the outcome record (§7.3) are the constraints a link may need to cite, and discovery over explanations alone will miss them. Candidate evidence is the outcome record plus those residual facts, still judged out of band against the seals (§5.1), not against the projection. Cheaper than indexing every byte of every payload is not the same as indexing only the verdict.

---

## 8. Storage notes against the baseline

**Existing precedent for edge shape.** `GraphRelationshipEntity` (`src/McpServer.Storage/Entities/GraphRelationshipEntity.cs`) is a directed, typed, workspace-scoped edge with a `RelationshipType` string, a `Weight` double, an optional `Description`, and a `Metadata` JSON blob. That is close to the shape §3 needs.

**But it is not reusable as-is.** Its endpoints are `GraphEntityEntity` nodes via `SourceEntityId` / `TargetEntityId` foreign keys, not session turns. Turn-to-turn edges need their own table modeled on this precedent rather than rows in this one.

**No embeddings over turns.** The only embedding column found at the baseline is a nullable `Embedding` BLOB added to `Chunks` (migration `20260216171000_AddEmbeddingColumn`). Candidate generation cannot assume a vector index over turns exists. §5.4 takes the non-vector route deliberately rather than as a fallback: traversal over requirement and acceptance-criteria identifiers needs no embeddings, so a first cut requires no new index and no migration for one.

**Tombstone registration is mandatory, per IMPL §3.2.3.** Every table introduced here — edges, edge transitions, goals, scope/progress series, outcome records — is subject to the trap that section documents: `SoftDeleteTurnRowsAsync` and the revival path name their targets explicitly and discover nothing, so new tables keep `IsDeleted = false` when their turn is deleted.

This is more dangerous for links than for the sealed-turn table. An edge is *traversed automatically*. An unregistered edge table becomes a side channel that silently re-admits content from a deleted turn into a live projection — the operator deletes a session and the material keeps arriving through the index. IMPL §3.2.3's two remedies apply to every new table: register it on both the tombstone path and the revival path, and join reads so visibility follows the turn rather than a flag a future table might forget to set.

For an edge the join is stricter than the single-turn join IMPL §3.2.3 states for seal and projection-state rows. Traversal, link admission, and projection reads treat an edge as ineligible unless **both** the source turn and the target turn are non-deleted. Filtering one endpoint leaves the side channel open. A live source pointing at a tombstoned target can still inspect or re-admit that target; a live target reached from a tombstoned source is the same hole in the other direction. §3.1 admits a specific endpoint, so either end can be the turn the projector reads. An edge with either endpoint tombstoned is not a candidate and not an admission, regardless of type, confidence, or which end is live.

**Audit path.** Per IMPL §3.2.1, set-based updates bypass the ledger. Edge transitions, scope and progress revisions, and outcome records should be written through the tracked save path so the history that §5.5 and §6.2 depend on is actually auditable.

---

## 9. Evaluation hooks

This capability must not be adopted on the argument that it sounds right. Hooks into WP §9:

- **Re-admit usefulness (WP §9.1 metric 4)** already exists and is the natural home: when a turn is admitted *by link*, did the subsequent action correctly use it? Report link-admitted and score-admitted separately, or the index hides inside an existing number.
- **Edge retraction rate** — retractions per generation, from §5.5's append-only lifecycle. This is a churn metric. It is not precision. A rising rate means edges are being withdrawn. It does not say whether the withdrawn edges were false, or whether the edges that remained confirmed were true. A false edge can sit confirmed forever; a correct edge can be retracted because its applicability changed.
- **Edge precision** — on a sample of proposed edges, the fraction an independent labeler marks true. The labeler is not the component that proposed the edge, and the label is against the sealed payloads current at judgment time, not against later retraction. Without those labels this cell is unmeasured. It is not `1 − retraction rate`.
- **Edge flip rate** — edges oscillating between confirmed and retracted, as a churn metric parallel to the `projectionState` flip rate in WP §9.1 metric 2. Must be reported per phase and excluded across phase boundaries (§5.3), or the metric peaks when the system is behaving correctly. Distinct from precision: flip rate measures instability, retraction rate measures withdrawals, precision measures correctness.
- **Criteria-derived candidate share** — what fraction of proposed edges came from requirement-graph traversal (§5.4) versus any fallback. A falling share means the plan anchor is not covering the work, which is a planning signal before it is an indexer signal.
- **Edge density per criterion** — edges among turns attributed to one criterion (§5.3). High density with flat progress is a retry loop, not a discovery.
- **Harm detection accuracy** — of turns flagged as introducing a regression, how many did on operator review, and how many introduced-versus-revealed classifications (§6.2) were correct?
- **Explanation verifiability** — fraction of explanations citing at least one checkable referent (§7.1). A falling fraction means explanations are drifting into narration.
- **Claim-verdict divergence rate** — how often the actor's intent claim and a non-pending HV verdict disagree (§6.3). The rate is descriptive. Agreement is the expected outcome when the claim is accurate and the work is straightforward, so near-zero divergence is not validator failure and is not evidence of capture. Disagreement is not success either. An uncalibrated target rate would reward false adverse verdicts. Do not alert on agreement, and do not alert on disagreement, until an expected disagreement rate has been calibrated on the labeled set below.
- **Adjudication accuracy** — of HV verdicts later checked by operator review, the fraction that matched the reviewed outcome. This is the metric that can distinguish a competent validator from a captured one. Divergence rate may be compared with it only after that calibration exists.
- **HV adjudication cost** — validator calls, targeted test runs, batched suite runs, and latency, reported separately from §5.2's revalidation cost and not averaged into a per-turn figure that hides batching. Per §6.3 an ordinary turn carries a `pending` verdict and does not launch the suite; the phase-exit suite is one batch charge; a targeted run is an explicit extra. If validator calls or suite runs grow with turn count, adjudication has become the default tier, which is the condition WP §10 risk 6 says to avoid.
- **Revalidation cost** — model judgments per turn and their share of turn latency, against the §5.2 cap. WP §10 risk 6 makes this a first-class concern, not a footnote.

---

## 10. Open questions

1. **How are turns attributed to criteria?** §5.4 moves candidate generation onto the requirement graph, which removes the vector-index problem but relocates it: the traversal is only as good as the binding between a turn and the criteria it bears on. Is attribution declared by the actor at write time, derived from the files and test methods a turn touched, or asserted by HV with the verdict? A turn's attribution is now load-bearing for linking, not just for §6's metrics.
2. **Is a harmful turn a fourth `projectionState`** or an annotation on `summarized` (§7.4)? The former is a schema change and touches WP §4.2; the latter risks being ignored by the projector.
3. **How do plan phases aggregate?** BDPv4 decomposes a plan into iterative phases and slices (the acceptance catalog carries a `slice` field), so scope and progress exist at both levels. How do slice-level readings roll up without double-counting, and is the weight-facing reading the slice or the plan?
4. **How are turns attributed** when one turn advances criteria in several slices, or in several plans at once?
5. **What is the admission threshold** for a link-driven re-admit, and does it differ from the WP §6 step 7 weight threshold?
6. **Does revalidation run on seal, on a schedule, or under budget pressure** (compare WP §10 risk 6's mitigation for re-score cost)? The trigger in §5.2 is already decided for one case: an endpoint reseal invalidates the prior judgment immediately, whatever the schedule is. The open part is when non-invalidated edges are re-judged.
7. **Is the link sub-cap (§4) fixed or adaptive**, and what happens when link admissions and pins together approach `budgetTokens`?
8. **Can an explanation be revised** if it was honestly wrong, given §7.2's append-only requirement? Presumably by appending a correction that supersedes it — mirroring the supersession-by-sequence resolution in IMPL §5.4.
9. **Does a passing test prove its criterion, and does the traceability script know the criterion exists?** §6.4's script does not read acceptance catalogs or named test methods. "Covered" in that script means an `FR-` / `TR-` / `TEST-` identifier appears in the mapping or matrix, not that a catalog row names a test, and not that the test is honest. A test bound to `AC-FR-MCP-MEMORY-010-02` can pass while asserting less than the criterion states, and the catalog row can disappear without the script failing. Catalog-to-test validation is an open surface (§6.4), not a closed gate. Test honesty remains the place §6.3's adjudication is load-bearing rather than supplementary, and it does not close the missing catalog check.
10. **What keeps HV independent in practice?** §6.3 requires a validator blind to the actor's account, but a validator sharing the same model, prompt lineage, or projection may reproduce the actor's blind spots and rubber-stamp them. The hostile-review store records model and effort per run, so independence is at least auditable after the fact; whether a *different* model is required, and what that costs, is unsettled.
11. **When does a deadlock become session abandonment?** BDPv4's answer for a session whose trust has broken is to end it rather than retry (§6.4). A metric loop needs a threshold: how many actor-versus-HV rejection cycles on the same criterion, with scope constant, before the correct action is to discard the session rather than record another regression?
12. **Where does a requirement-to-requirement dependency graph come from?** §5.4 can traverse same-criterion pairs, sibling criteria under one requirement, and FR–TR–TEST co-membership with artifacts that exist at the baseline (`docs/plans/*-ac-catalog.json` where a plan has one, `docs/Project/TR-per-FR-Mapping.md`, `docs/Project/Requirements-Matrix.md`). None of those artifacts records a dependency between requirements. Until one does, and until it declares direction and that the edge is a candidate hint rather than an admission, cross-requirement traversal is out of scope. The absence is a missing input, not an empty result the indexer should paper over.
13. **What is the per-criterion turn-density cap?** §5.3 keeps hysteresis and cooldown as the primary churn defence until this cap exists. The open definition is what is counted (turns, edges, or both), the numeric limit, and the projector's behavior at the cap (refuse new link admissions, stub, or hand the criterion to the deadlock rule in open question 11). A note that density "needs a cap" is not the cap.

---

## 11. Non-claims

Stated explicitly, because the underlying proposition — that retrospective linking improves outcomes — is attractive enough to be assumed rather than tested.

- **No efficacy claim.** That linked context produces more correct results is a **hypothesis**, not a finding. Nothing here has been built or measured. WP Principle 8 bars presenting it as a benefit until §9's metrics say so.
- **No token-savings claim.** Nothing here is offered as reducing token cost. §5.2 says the opposite: revalidation adds model calls, and the cost must be bounded and reported.
- **No claim that precision is achievable.** §4 argues an imprecise index is worse than none. Whether the required precision is reachable in practice is unknown, and a negative result is a legitimate outcome.
- **No claim of code readiness.** §8's references are a snapshot of one baseline commit, read for shape rather than verified by building anything.
- **No claim that self-reported signals are trustworthy.** §6.3 removes the actor from adjudication and §7.1 constrains both sides to checkable referents, precisely because the unconstrained versions are not trustworthy. Those constraints are reasoned, not demonstrated.
- **No claim that Hostile Validation is available, cheap, or complete.** §6.3 proposes HV as a runtime adjudicator; that is a design proposal. The existing document-level Perplexity HV remains blocked on an API key after box reseed and has **not** been run. Per the standing rule in the README and IMPL §9, HV must never be described as done.
- **No claim about undisclosed search internals.** §4.1 cites patents, Google's own published posts and documentation, a court exhibit, and a peer-reviewed paper. It does not claim to describe how any production ranking system currently works, and the mapping from web spam to agent context is an analogy — §4.1 states where it breaks rather than where it holds. No claim is made that any of this prior art was designed for, or validates, the design in this addendum.
- **No claim about how BDPv4 is practiced.** §6 and §6.4 quote `docs/Development-Process-draft-v4.md`, the plan artifacts in `docs/plans/`, and `scripts/Validate-RequirementsTraceability.ps1` as written. Whether those gates are applied consistently, whether the traceability script runs in CI, and whether the acceptance catalogs stay current were not checked. The process is cited as documented intent, not as observed practice.
- **No claim that scope and progress can be read automatically today.** §6.1 derives them from acceptance-criteria state, which assumes a machine-readable catalog bound to test results. `docs/plans/mcp-memory-002-ac-catalog.json` shows the shape exists for one plan; no such binding was verified across plans, and nothing computes these values.
- **No claim that an adversarial validator is correct.** §6.3 treats HV verdicts as evidence with their own error modes, including false accusation, and open question 10 records that a validator sharing the actor's model or lineage may simply reproduce its blind spots.
- **No claim that retraction rate measures edge precision.** §5.5 and §9 separate churn (retractions, flips) from precision, which needs labels the indexer did not assign.
- **No claim that traceability validation closes criteria gaming.** `scripts/Validate-RequirementsTraceability.ps1` checks identifier presence in the mapping and matrix. It does not read acceptance catalogs or bound tests. That surface is open (§6.4, open question 9).
- **No claim that a requirement-to-requirement dependency graph exists.** §5.4 names the traversals the baseline artifacts support and the cross-requirement traversal they do not (open question 12).
- **No claim that actor/HV agreement is capture, or that disagreement is success.** §9 refuses both readings until adjudication accuracy has calibrated an expected disagreement rate.
- **No claim that per-turn progress readings are free.** §6.3's ordinary turn carries a pending verdict. The phase-exit suite is a batch charge and does not attribute itself to turns.

---

## 12. Document control

| Version | Date | Change |
|---|---|---|
| v0.1.5 | 2026-09-25 | Codex review on merged PR #56. Design claims corrected. P1: traversal, link admission, and projection reads require both endpoints non-deleted (§8); retraction rate is churn, and precision needs independent true/false labels (§5.5, §9); catalog-to-test coverage left open because `Validate-RequirementsTraceability.ps1` does not read acceptance catalogs (§6.3, §6.4, open question 9); edge judgments bind to seal sequences and reseal invalidates (§5.2); stubs keep residual payload facts as well as the outcome record (§7.3, §7.5). P2: §1 retracts the claim that omission hides turns from the scorer — WP §6 loads every turn and WP §5.2 scores stubs; the remaining hole is joint value and facts the stub dropped. Per-turn HV verdicts are pending, batched at a checkpoint, or targeted, and are not a free reading of the phase-exit suite (§6.3). Admission direction is per edge type (§3.1). Shared criteria generate candidates only; type and direction still need evidence (§5.4). Cross-requirement dependency traversal acknowledged missing (open question 12). Hysteresis and cooldown returned to the primary churn defence until a per-criterion density cap is defined (§5.3, open question 13). Actor/HV agreement is not treated as validator failure (§6.3, §9). README index version synchronized to this document |
| v0.1.4 | 2026-09-20 | Operator correction: the bounded acceptance-criteria set of a BDPv4 phase is the primary defence against edge churn, so §5.3's thrashing risk was overstated — hysteresis and cooldown are refinements over a finite, slowly-moving anchor rather than the only protection. Follows the same anchor into new §5.4: candidate generation is a traversal of the requirement graph, which drops the quadratic-pairing and embedding-index problem, makes edge types declared rather than inferred, and gives the §4 task-progress rule a concrete form. Records that bounding traversal by phase would reopen §1's hole, so lineage rather than time is the bound; and that phase-boundary turnover is a scheduled flush, not thrash. Old §5.4 (append-only lifecycle) renumbered to §5.5 with references updated. Open question 1 replaced — candidate generation is no longer the load-bearing unknown; turn-to-criterion attribution is. Three metrics added. Superseded in part by v0.1.5: the churn-priority claim and the "edge types become declared" claim were wrong, and are corrected there rather than rewritten out of this row |
| v0.1.3 | 2026-09-20 | Adds §4.1, prior art on adversarial link graphs, in answer to an operator question about public attribution: reasonable-surfer per-link weighting, neutralize-not-penalize link spam handling, link attributes as hints so information is not lost, and TrustRank's expert-evaluated seed set as a counter-bias — the last being the one result that transfers. Records the unflattering finding that the declared link graph was demoted in favour of recorded user reaction, a signal with no analogue in a single session, and the two disanalogies that follow: opacity is unavailable when the actor reads this document, and the misaligned party is inside the system. Corresponding non-claim added |
| v0.1.2 | 2026-09-20 | Operator correction: **the plan is the goal, not the turn**, and planning — not adjudication — closes the criteria surface. §6.1 re-scoped to the plan and given real units from BDPv4's requirement/acceptance-criteria artifacts; new §6.4 on criteria preceding implementation, machine-checked traceability, and the narrower gaming mode BDPv4 already documents (invalidating a blocking test); §6.3's cost tier corrected — the base reading is a test run, not a model call; descoping assigned to stakeholders per BDPv4; new §7.1.1 routing explanations into requirement refinement. §6.2's introduced-versus-revealed argument **retracted as novel** and re-credited to BDPv4 "Resolving Defective Requirements." Open questions 3, 4, 9, 10 and 11 rewritten — criteria authorship is no longer open; test honesty is what survives |
| v0.1.1 | 2026-09-20 | Operator correction before first review: progress, regression and explanation records are outputs of **Hostile Validation**, not of the acting agent. §6.3 rewritten around actor intent claim versus HV verdict, with claim-verdict divergence as the harm signal; §7 and §7.3 reworked so the outcome record pairs both halves; the blindness constraint from §5.1 generalized into a single rule covering both link revalidation and progress adjudication. Records where gaming relocates to (criteria authorship) and adds open questions 9–11 on criteria provenance, validator independence, and actor/HV deadlock |
| v0.1.0 | 2026-09-20 | Initial draft. Captures four operator proposals from the 2026-09-20 design session: retrospective link index, continuous revalidation, per-goal scope and progress, and result-with-explanation records. Adds the re-admit circularity argument (§1), the joint-value representational limit (§2), the self-confirmation constraint on revalidation (§5.1), the introduced-versus-revealed correction to the harmful-turn rule (§6.2), and the traversal hazard that IMPL §3.2.3 creates for an unregistered edge table (§8) |
