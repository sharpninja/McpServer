# Phase 0 current-definition readiness integration

Author: Codex. Date: 2026-11-19. Group: GRP-FEDPH0-001. These reviews apply Task-Workflow-Architecture section 3.5.11 to the simulated MCP TODOs. Ready means the current design and discriminating mock gate qualify; it does not mean Success, native feasibility, B-output acceptance or HV. Every task remains not done. Implementation waits for all nine members and completed prerequisites.

## Task003

The full review is in 2026-11-19-federation-task003-readiness.md. The six-class paired gate passed 9,770 tests per host with no failures or skips. Evidence commit 3a926c96620b922ab44ea8d669e2fae5cff59fb3 and live TODO readback support its Ready disposition. The selected narrow Dolt patch remains unimplemented. Its Task001 prerequisite remains incomplete.

## Task006: complete B6-B9 design review

Reviewed current title, description, ordinal 6, Task002 dependency, FR-MCP-180/186/187/189, TR-MCP-FEDERATION-007/013/014/016 and TEST-MCP-228/234/235/237. The four selected ACs do not replace the complete B6-B9 body. The binding design is 2026-11-19-federation-task006-design-mock-binding.md, source b7cfbf30302d492c12e8366fccde9b78b1074a6c, integrated through candidate a5e38eb09037ffb37e52e4a1e58a5aab71deec0b.

- B6 reuses complete merge/domain validation, review and candidate-retention assertions. The supplement defines default versus private conflict transactions, unique/FK/not-null/check diagnostics, rollback, global/live/fresh settings, retained ordinary refs and unrelated roots without a fabricated ancestor.
- B7 reuses schema readiness and evolution assertions. The supplement defines the successful single-author expand and fenced contract paths, inventory-change refusal, no startup EF migration and compatible worker negotiation.
- B8 reuses typed migration and seed-capability assertions. The supplement defines seed/join/recovery manifests, source accounting, aliases and explicit divergence decisions; complete B2 table/domain/branch/draft projection; exact audit identity/payload-hash membership; verified restoration without manufactured history commits.
- B9 reuses stale-lease safety assertions. The supplement defines independent before/held/after/reopen observations for worker fast-forward/merge/resolve/schema/recovery versus admitted edits, stale fences/previews, dirty takeover, OS ownership restart and three separate requestId crash windows.

IPhase0Task006SupplementProbe and its command/fact/frame records are test-owned observation contracts. The native binding recipe requires independent connections or restarted processes, actual graph/ref/typed-row/control/audit evidence, injective immutable bindings from fixture symbols to measured hashes, and source retention. A constant fixture label cannot substitute for native payload bytes. Native command spellings must still be validated against the pinned engine during implementation; no feasibility result is invented here.

The complete current member gate ran through Nuke TestGroups at integrated source 39cec6a7f4edce45ffa8d18c916c676a00d8f5e6:

- Windows run 20261119T103300-fedph0-task006-definition: 22,071 passed, 0 failed, 0 skipped, exit 0.
- Linux run 20261119T103644-fedph0-task006-definition-linux: 22,071 passed, 0 failed, 0 skipped, Nuke and SSH exit 0.
- Class counts: CandidateRetention 1,312; MergeValidation 746; MigrationSafety 3,153; ReviewOperations 188; SafetyBoundary 56; SchemaEvolution 1,490; SchemaReadiness 1,290; Task006Supplement 13,836.
- All 16 direct assertion/mock source files match after LF normalization. The full ordinal-sorted case-name multiset, preserving duplicates, matches F6275A83C95384EDF54DB5C068CEAFD086C0AE25C4E67974AEF03548640E86FF.

The source-pinned child supplement contains 64 correct script permutations, 13,771 rejected wrong observations and one inventory check. The integrated complete gate also executes every reused class; it does not add counts from older source revisions. Complete native observers must invoke the same assertions without deriving expected results from implementation output.

Portable evidence is in docs/receipts/federation/20261119-task006-readiness/readiness.json and paired-trx-and-logs.zip. Raw TRX/log originals remain retained; archive entries were read back and SHA256 matched. The behavioral definition hash is 06FCD1215185E01EDFDE613B626AF5B25DA20D7F37FA2EDFB7B1ABF6467F8E4C. These simulated TODOs have no deployed WorkTask Version; the hash excludes only status/eligibility metadata, and the full reviewed snapshot is retained separately.

Disposition: this complete current Task006 definition meets design/mock eligibility for Planning to Ready after the evidence commit. done remains false. The all-nine implementation barrier and Task002 dependency still block implementation. Real Dolt merge/constraint/schema/migration/gate/crash experiments, B6-B9 acceptance, final both-host full gates and independent overall/per-task HV remain open.

## Task005 and Task007 integration boundaries

Task005 candidate 255502cada08437e562ab772142512560619177f is integrated. Independent current-definition review found no finite Task005 design/mock gap and verified its exact paired 2,733-case receipts and source/package pins. Its final Ready disposition waits for the explicit sibling pins and portable raw-TRX receipt checkpoint. The actual restored Duende packages are IdentityServer/Storage 7.4.7 and ASP.NET Identity/EF/EF.Storage 7.4.6, not the unsupported 7.4.3 value mistakenly placed in the parent review brief. No dependency was changed from that brief.

Task007 candidate 9455bf86c30929475252728da0dd21715b1a5b4e is integrated and remains Planning. Its independent review found one finite whole-definition B12 gap: cross-host SQL refusal plus each host's local health requires actual observation contracts, beyond listener configuration. An isolated background agent is adding only those test-owned contracts/mocks and native binding design. No connectivity probe is run before the group barrier.

## Task005: parent current-definition readiness disposition

This checkpoint supersedes the pending parent-readiness statement above. The reviewed current title, ordinal 5, no prerequisites, two assigned FR-MCP-185 ACs, full B5 description, TR-MCP-FEDERATION-012 and TEST-MCP-233 match the live effective records. The full design and all four contracts were read, including their independent command, durable-state, reopen, scope and clock observations. Independent reviewer Codex-20261119T154121Z-plugin-session / req-20261119T162634Z-turn-0208 reported no finite local design/mock gap after inspecting all source/package/receipt pins; its complete review turn was read back.

The complete current Task005 gate passed 2,733 cases per host with zero failures/skips at 74bbc8120757467372821a40af998594b2b5c55f, comprising platform 203, management 1,102, worker permission 348 and admission 1,080. All eight current C# source LF hashes match the paired receipt. The design hash in that receipt names the tested revision; Git shows the only later design change was the appended paired-evidence checkpoint. The original TRX bytes are preserved in the four-entry Task005 ZIP under 20261119-portable-trx, SHA256 679A779CDD03AF0353B097E29FEADC47355696596823F84A165624C08698D9B9. Parent independently rehashed every archived entry and retained original. Existing LF-normalized checkout copies are not used for original-byte claims.

Shared responsibilities are explicitly pinned rather than implied: Task003 transport and selected client source f56c3111/evidence 3a926c96, complete paired gate 9,770 per host; Task006 worker-owning-gate source 39cec6a7/evidence 95392d0a, complete paired gate 22,071 per host; and Task007 RecoverySecurity contract/mock at 9455bf86 for the retained credential-capture/TLS assertions. Whole Task007 B12 SQL isolation readiness is separate and was not inferred from these reused security assertions. Parent-readiness.json pins all six reused contract/mock files and records the complete reviewed TODO and live canonical requirements.

The current behavior-only definition SHA256 is 6FC8309525D8F7291F9E92A9B512DDE56DEE8D0C3CA2CDCFF7886EA25CA7593D. The simulated TODO has no deployed WorkTask Version. Only state/eligibility and historical notes are excluded from this behavioral hash. Exact package pins remain core/Storage 7.4.7 and ASP.NET Identity/EF/EF.Storage 7.4.6; fixture token lifetime and safe aliases do not become production requirements.

Disposition: complete current Task005 design/mock eligibility supports Planning to Ready after this evidence commit, with done false. Actual protected stores and issuer restart, issuance/introspection/revocation/rotation, concrete SQL rights, gateway/header containment and full credential-channel capture still require native binding. No B5 acceptance, Success, requirement satisfaction, full-phase exit or HV is claimed. Implementation waits for all nine members to be Ready and the ordinary dependency rules.
