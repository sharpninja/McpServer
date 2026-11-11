# Federation correctness evaluation: is Dolt-versioned federation the CORRECT way to proceed?

Author: ClaudeCode. Date: 2026-11-11. Status: DRAFT decision-support (built while the plan-readiness HV iterated toward AGREE). This is not a requirement, not a plan, and not an approval. It is the case the operator asked for: after HV establishes a VALID way to proceed, decide whether it is the CORRECT way. The architecture decision is the operator's; this document builds the evidence, criteria, risks and a labeled recommendation so he can make it.

Observation vs inference is marked throughout. Receipts (web sources) are listed at the end.

## 1. The two different questions

- VALID (what HV answers): is the plan internally consistent, decision-complete, and free of rule violations, so a less-capable implementer could execute it without inventing unstated decisions? The R2->R4 HV loop answers this for the packet.
- CORRECT (this document): is Dolt-versioned-relational federation, with the independent-node + hub + guarded-publication architecture, the right thing to build for the operator's actual problem, versus the alternatives, given its external risks, costs and maturity? HV cannot answer this by being more internally consistent; the decisive facts are external to the packet.

## 2. What "correct" means here (decision criteria)

1. Problem-fit: does the design serve the operator's stated intent (independent nodes; bidirectional sync; Git-style branches IN; merge with mandatory conflict review; two-char node tags; PRs OUT) with low impedance mismatch?
2. Best-among-alternatives: among the surfaced options (library-options A-I), is Dolt the best fit for THOSE requirements?
3. Critical-dependency feasibility: can the single load-bearing dependency (a maintained EF Core 10 provider driving Dolt over the MySQL wire on net10.0) be satisfied without violating the operator's dependency rules?
4. Maturity and operational risk: is the engine production-sound for a small lab that may grow, and what operational burden does it add?
5. Complexity vs value (V-squared): is the build proportionate to the value, or over-engineered for two hosts plus a logical test peer?
6. Reversibility and lock-in: if the bet is wrong later, how cleanly can the operator get his data back out?
7. Sequencing and optionality: can the decision be de-risked by proving the uncertain parts before committing the irreversible ones?

## 3. The case FOR (points that this is correct)

- 3.1 Best structural fit for the stated requirements (inference, high confidence). The operator asked for branches, three-way merge with review, and retained history on project DATA. Dolt provides commit/branch/merge/clone/fetch/push/pull natively on relational tables. Among the surfaced options, nothing else gives native relational history+branching: Dotmim.Sync is row synchronization with a ServerWins default and no history/branch model; Fossil is a SQLite-backed DVCS but does not map the existing EF business tables; libgit2 SQL backends are a custom-development project, not library reuse. So for the operator's EXACT requirements, Dolt is the strongest fit the research found. (Receipt: library-options.md options D, F, G, H, I.)
- 3.2 The plan is structured to answer "correct" before committing (observation). Ph0 is a disposable feasibility harness with twelve blocking outputs (B1-B12), all initial status "unproven," and it explicitly forbids silent EF downgrade, a prerelease provider, or a bespoke provider; it requires reporting concrete alternatives if compatibility fails. Nothing in Ph1-Ph8 is authorized until Ph0 is accepted. So the plan already treats the engine/provider bet as a gated spike, not an assumption. (Receipt: 2026-11-10-federation-dolt-bdpv4.md section 5; architecture.md section 8.)
- 3.3 Lock-in is partially mitigated by design (observation). Ph3 delivers a verified all-domain Dolt-to-new-SQLite recovery export (every B2 domain, every branch, dirty working sets), with originals kept read-only and a one-node-first 48-hour soak before the second host. This is a real exit path, not a promise. (Receipt: architecture.md section 9; ac-FullProjectRecoveryPreservesAllDomains.)
- 3.4 Dolt is a real, committed engine (observation, from vendor and third parties). Dolt 1.0 shipped in 2023 with a committed storage format and self-describes as production-ready; it supports multi-writer concurrency via standard SQL transactions with a commit/merge layer on top. (Receipt: DoltHub productionizing/concurrency posts.)

## 4. The case AGAINST (risks that this may not be correct NOW)

- 4.1 The load-bearing dependency does not yet exist in an acceptable form (observation + inference; HIGH materiality). The repository targets net10.0 and EF Core 10.0.0. As of Oct/Nov 2026:
  - Pomelo.EntityFrameworkCore.MySql, the provider DoltHub itself uses, has NO GA EF Core 10 release. Its latest 9.0.0 constrains EF Core to the 9.0.x range; mixing any EF Core 10 package yields NU1107/NU1608. The EF10 port (PR 2031) is open and unmerged; the tracking issue has been open with a "soon" since Oct 2025. (Receipt: startdebugging.net NU1608/NU1107 post; Pomelo GitHub.)
  - The ONLY confirmed EF Core 10 build is the third-party Microting fork (10.0.7, built on MySqlConnector >= 2.5.0, assembly renamed from Pomelo). A renamed community fork is exactly the "maintained provider / maintainer provenance / deployment entitlement" risk the operator's dependency rules (compliance-rules.md; MEMORY-BESTPRACTICE-001 section 7, NuGet warnings may never be ignored) are strict about. (Receipt: Microting.EntityFrameworkCore.MySql 10.0.x on NuGet.)
  - Oracle's official MySQL Connector/NET EF provider: DoltHub reported "issues" using it, and its EF10 status is unconfirmed here.
  - Inference: at today's state the plan's own B1 gate is likely to FAIL on the "maintained EF10-compatible provider" criterion, forcing one of: (a) accept the Microting fork under explicit risk acceptance; (b) pin the Dolt project context to EF Core 9 while the rest of the solution stays EF Core 10, and absorb the solution-wide version tension; (c) wait for Pomelo EF10 (timeline unknown); or (d) reconsider the engine or the timing. This is the single most decision-relevant fact for "correct NOW."
- 4.2 Engine operational maturity adds burden (observation; MEDIUM). Dolt garbage collection is offline (stop sql-server, run dolt gc); historically it could not reject connections (admins set very high max-connections, improvement status unconfirmed); durability hardening is in progress (crash-recovery testing began Jan 2026 after a VM crash, no published guarantee yet); and the troubleshooting guide warns heavy read concurrency can consume all host CPU. For a two-host lab these are manageable but real, and they land on the operator to run. (Receipt: DoltHub troubleshooting, crash-recovery-testing, server-connections posts.)
- 4.3 Scope/complexity vs value for two hosts (inference; MEDIUM). The design is enterprise-grade: a fourth database provider with its own migration assembly, a full migration tool (plan/run/resume/verify/cutover/abort) across three source providers, a guarded HTTPS publication gateway, IdentityServer reference-token integration with per-node clients, conflict-review storage with ageing, append-only hub audit backfill, all-domain recovery export, and a staged soak cutover. Much of this is intrinsic to doing distributed project data SAFELY, and is to the plan's credit. But the fourth-provider + full-migration-tool slice is a large, mostly-irreversible lift whose value should be weighed against a narrower first increment. The deployment target is two machines (LAB-OMARCHY, PAYTON-LEGION2) plus a logical test peer.
- 4.4 Semantic gap in "Git-style merging" (observation; LOW, already accepted). The operator's mental model is Git. Dolt merges data cell-by-cell with relational conflict detection, not Git line-level text merge. The operator accepted this (U2, 2026-11-11), and the plan is honest about it; recorded here only so the acceptance is explicit in the correctness case.

## 5. Alternatives re-weighed against the STATED requirements

- Dolt (option G): best native fit for branches + relational merge + history; cost is the EF10 provider gap and engine operational maturity. 
- Dotmim.Sync (D): mature .NET sync, but row-synchronization with a ServerWins default and no native branch/history/merge-review model; would require building the branch/merge/review layer the operator wants on top. Worse fit for the stated requirements.
- Fossil (H): SQLite-backed DVCS proving relational storage can underpin DVCS, but it versions Fossil artifacts, not the existing EF business tables; would need a custom mapping layer. Worse fit.
- libgit2 SQL backends (I): most literal "Git objects in SQL," but a backend-component collection, not a federation platform; adopting it is a custom-development project. Worse fit.
- Status quo DirectProxy / forwarding: does not meet the core requirement (independent local execution + bidirectional history exchange); explicitly superseded for versioned projects.
- Inference: among options for the operator's EXACT requirements, Dolt is the best fit IF and ONLY IF the EF10 integration can be made sound within the dependency rules. The correctness decision therefore reduces largely to the B1 provider question plus a proportionality judgment on scope.

## 6. The decisive uncertainty

Inference (high confidence): "Is this correct NOW?" turns primarily on B1 - whether a maintained, rules-compliant EF Core 10 provider path to Dolt exists. Everything else (fit, lock-in mitigation, plan safety) points toward Dolt being a reasonable choice; the provider gap is what could make "now" the wrong time or the fork the wrong dependency.

## 7. Recommended decision framework (my inference; the decision is the operator's)

1. Make Ph0/B1 a hard go/no-go SPIKE on the provider question specifically, run FIRST and in isolation, before any Ph1-Ph8 commitment. Define its acceptance explicitly: a provider path that (a) supports EF Core 10 on net10.0, (b) satisfies the operator's maintainer-provenance/entitlement and NuGet-warning rules, and (c) passes the real Dolt schema/query/transaction/migration tests the plan already enumerates.
2. Pre-agree the fallbacks and their acceptance owners, so B1 is a decision point, not a dead end: (a) accept the Microting fork under an explicit, recorded risk acceptance; (b) pin the Dolt project context to EF Core 9 with the solution-version tension evaluated and accepted; (c) defer federation until Pomelo EF10 ships; (d) re-scope or re-select. Each is the operator's call; B1 should present them with evidence, not pick one silently (which the plan already requires).
3. Consider a reduced first increment to de-risk scope: one project, the two real hosts, a single versioned domain (for example TODOs) end-to-end through commit/publish/merge/review/recovery, BEFORE building the full fourth-provider + full-migration-tool surface. This proves the architecture against reality cheaply and keeps the irreversible lifts optional until the model is validated.
4. Define the "correct" exit test up front: what Ph0 evidence confirms Dolt-now is correct (a clean provider path + passing real-DB contracts), and what flips it (no rules-compliant provider path, or engine operational blockers on LEGION Windows hosting).

## 8. What would change this recommendation (kill criteria)

- If B1 shows no EF Core 10 provider path that satisfies the operator's dependency rules, and the operator will not accept the fork or an EF9-pin, then Dolt-NOW is not correct: wait for provider maturity or re-select. 
- If native Windows Dolt hosting on LEGION (B1/B9) cannot be made a supervised, crash-safe service, the two-host premise weakens and the deployment approach must change before proceeding.
- If, on reflection, the operator's real near-term need is narrower than full versioned federation, the proportionate first step is the reduced increment in 7.3, not the full Ph1-Ph8 build.

## 9. Bottom line (labeled inference, operator decides)

Dolt is, on the evidence, the best-fitting engine for the operator's stated federation requirements, and the plan is structured safely enough to find out before committing. The honest caveat is timing and dependency: as of Nov 2026 there is no mainstream, rules-compliant EF Core 10 provider for the MySQL wire, only a community fork, and Dolt's operational hardening is in progress. So the correct way to proceed is not "approve Ph1-Ph8 now" and not "abandon Dolt," but "run Ph0/B1 as an isolated, fallback-instrumented provider-and-hosting spike, with a reduced first increment in view, and let B1's evidence make the engine-and-timing call." That keeps the valid plan and makes the correctness decision on evidence rather than optimism.

## 10. Receipts (web, consulted 2026-11-11)

- Pomelo EF Core 10 version conflict (NU1608/NU1107), no EF10 GA release: https://startdebugging.net/2026/10/fix-nu1608-nu1107-version-conflict-pomelo-entityframeworkcore-mysql-ef-core-10/
- Pomelo project/compatibility overview: https://awesome.ecosyste.ms/projects/github.com%2FPomeloFoundation%2FPomelo.EntityFrameworkCore.MySql
- Microting EF Core 10 fork (10.0.x, MySqlConnector >= 2.5.0): https://packages.nuget.org/packages/Microting.EntityFrameworkCore.MySql/10.0.3
- DoltHub, Entity Framework Core with Dolt (uses Pomelo; 2023): https://www.dolthub.com/blog/2023-12-04-works-with-dolt-efcore
- DoltHub, productionizing Dolt: https://dolthub.com/blog/2024-11-27-productionizing-dolt
- DoltHub, crash-recovery testing (2026-01): https://www.dolthub.com/blog/2026-01-26-dolt-crash-recovery-testing
- DoltHub, server connection improvements (2025-03): https://dolthub.com/blog/2025-03-27-server-connections-improved
- DoltHub, server-mode concurrency: https://dolthub.com/blog/2021-03-12-dolt-sql-server-concurrency
- DoltHub, why people don't use Dolt (candid): https://dolthub.com/blog/2024-07-03-why-no-dolt
- Dolt server troubleshooting (GC offline, CPU under read concurrency): https://docs.dolthub.com/sql-reference/server/troubleshooting

## 11. Operator decision update (2026-11-11): accept the unfinished EF Core 10 driver

Operator decision (verbatim intent): "I am not afraid of using the unfinished efcore 10 driver and submitting patches for it if necessary."

Effect on this evaluation:

- Resolves the decisive uncertainty of section 6 in favor of proceeding. The provider risk in 4.1 is no longer a blocker: the accepted path is to use the in-progress EF Core 10 provider (the Pomelo EF10 port PR, or the Microting fork) and contribute upstream patches as needed. The "wait for a maintained GA provider" option is explicitly declined.
- It is a conscious override of the maintained-provider / prerelease-provider dependency posture. Per the operator's authority to relax a rule for a workspace, a prerelease or community-fork provider, patched by this project, is now an accepted dependency for the Dolt project context. The plan's B1 and architecture section 8 wording that forbids a prerelease or bespoke provider must be amended to record this exception (a between-rounds plan edit, deferred until the running HV round completes).
- B1 is reframed, not removed. Acceptance is no longer "a maintained EF10 provider exists" but "the chosen EF10 provider (port or fork), patched as needed, drives Dolt correctly on net10.0 and passes the real schema/query/transaction/migration contracts." Willingness to patch makes B1 a tractable engineering spike the operator owns; it is not yet proof that it works.

Two honest residuals the decision does NOT eliminate:

- Two separate surfaces. Patching the EF Core 10 PROVIDER fixes the .NET integration layer (C#, patchable by the operator). Dolt's own "not 100 percent MySQL compatible" gaps (query features, SQL translation edge cases) live in Dolt's Go engine; they are worked around or patched UPSTREAM in Dolt, not in the provider. Ph0 must probe both surfaces; the provider decision only de-risks the first.
- Engine operational maturity and scope. Section 4.2 (offline GC, durability hardening, read-concurrency CPU, Windows hosting) and the scope-proportionality judgment (4.3) stand unchanged. The reduced single-project/single-domain first increment (7.3) remains the recommended de-risk and is now the main lever, since the provider blocker is removed.

Updated bottom line: with the provider risk operator-accepted, Dolt-NOW is a defensible correct path. Proceed with Ph0/B1 reframed as a provider-ENABLEMENT spike (make the EF10 port/fork drive Dolt, patching as needed, and prove it against the real contracts), keep a reduced first increment in view, and treat Windows hosting (B1/B9) and Dolt-side SQL gaps as the remaining go/no-go evidence. The residual kill paths are narrow: an EF10-provider-plus-Dolt combination that no reasonable patching can make pass the required semantics, or LEGION Windows hosting that cannot be made crash-safe.
