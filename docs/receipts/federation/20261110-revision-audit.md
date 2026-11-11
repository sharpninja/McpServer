# Federation PDF revision audit

Audited 2026-11-10 (America/Chicago CST).

- **Original:** 01M73DTMB70XDGA1PCT3EGRGH4.pdf. 76 pages, created 11-10 13:00 CST. Text is in /workspace/fed-original.txt.
- **Revision:** a6070225...c45.pdf. 48 pages, 846,076 bytes, created 11-10 19:24 CST. Text is in /workspace/fed-revised.txt.
- **Method:** I extracted both files with pdftotext -layout. Then I compared them section by section, workflow by workflow, phase by phase and requirement ID by requirement ID. I also extracted every test and acceptance criterion name, rendered the diagram pages, and checked the link targets.
- **Page numbers:** they are the printed footer numbers. "o.N" means original page N, and "r.N" means revised page N.
- **Limitation:** I couldn't see the full text of the 17 findings Codex was sent, so I worked from the summary list in the brief.

## Where the 28 pages went

Original layout:
- Cover and guide: pp.1-2
- Architecture: pp.3-12
- BDPv4 plan: pp.13-19
- Workflows and inventory: pp.20-25
- Visual walkthrough: pp.26-50 (12 workflows, 45 steps)
- FR records: pp.51-57
- TR records: pp.58-65
- TEST records: pp.66-73
- FR to TR mapping: p.74
- Requirements matrix: p.75
- HV review status: p.76

Revised layout:
- Cover and guide: pp.1-2
- Architecture: pp.3-15 (now has Structurizr container and deployment views, plus new sections 11 to 14)
- BDPv4 plan: pp.16-23
- Workflows and inventory: pp.24-30 (U01 to U15)
- Illustrated examples: pp.31-32 (2 steps)
- Current contracts and acceptance: pp.33-46
- Current-scope coverage: pp.47-48

Net effect:
- **Walkthrough:** about 23 pages removed. Of the 45 step states, 43 are gone. This is real content loss (see B1).
- **Requirements:** 25 pages of FR, TR, TEST, mapping and matrix became 14 pages of contracts. That is mostly compaction. The original TR and TEST bodies repeated the FR text, and every acceptance line read "Prove X with positive and negative-control evidence". The revision replaces them with one concrete Given/When/Then criterion per test, 91 in total.
- **HV review status page:** 1 page removed.
- **Word count went up:** about 17,600 words in the original, about 18,900 in the revision.

Requirement and test ID checks:
- **All IDs kept:** every ID in the original is still in the revision. That covers FR-MCP-174 to 189, TR-MCP-FEDERATION-001 to 016, TEST-MCP-222 to 237, FR-MCP-103, TR-MCP-FED-001, TEST-MCP-136 and FR-MCP-AUDIT-001.
- **Test names:** every named acceptance test survives, with one exception. CloneOmitsAuditAndSecrets was renamed CloneOmitsSecrets. That rename is intentional, because finding 1 means audit records now go to the hub.
- **Test count:** I counted exactly 91 unique ac- criteria, which matches the revision's own claim of 91.
- **Mapping:** the 16 FR to TR to TEST mappings survive. They now appear in each contract header and on r.47 instead of on standalone pages.

## A. Finding verdicts

Totals: 15 ADDRESSED, 2 PARTIAL, 0 NOT ADDRESSED.

1. **ADDRESSED.** Audit records now flow to the hub.
   - r.6 amends both FR-MCP-AUDIT-001 and TR-MCP-AUDIT-004: "preserve permanent local audit AND deliver it append-only to the hub unversioned audit store".
   - r.14 section 11 adds POST audit/batches, an append-only hub ledger, and says "TODO audit history and generic audit history are migration-backfill domains".
   - r.38-39 add TR-011 / TEST-232 with AuditReachesHub, AuditRetryIdempotent and AuditBackfillVerified (100 TODO plus 20 generic rows, all with hub receipts).
   - r.26 adds a new workflow, U13.
   - Caveat: one stale sentence remains, at r.12 (see D2).
2. **ADDRESSED.** FR-MCP-103, TR-MCP-FED-001 and TEST-MCP-136 are amended.
   - r.47: they "now require local execution with committed-history exchange".
   - r.47-48: "replaces ... LocalProxy business-command forwarding/replay".
   - r.44-45 add new acceptance criteria: FR-MCP-103/review20261110-1 and -2, TR-MCP-FED-001/review20261110-1 ("LocalProxy middleware is not invoked") and -2 (explicit DirectProxy regression).
3. **ADDRESSED.** The existing IdentityServer integration is now the base.
   - r.15-16 section 13, "Existing IdentityServer is the authority": the integrated Duende server, fed.node.{NodeId} clients bound to an ownerSubjectId, reference tokens, revocation through IdentityServer, and gateway introspection.
   - The same model appears in B5 (r.18), TR-012 (r.40) and the deployment view.
4. **ADDRESSED, with notes.**
   - The Figure 1 box drawing is gone, and with it the contradiction. Original Figure 1 (o.4) showed H's worker wired directly to the shared remote, against o.6 "same authenticated remote endpoint".
   - r.3 says "The authoritative model is Structurizr workspace". r.4 has a rendered Structurizr Container View and r.5 a Deployment View. Neither uses Mermaid, and the gateway path is shown.
   - Note: the DSL is only a link to file:///home/sharpninja/github/McpServer/docs/plans/exports/federation/workspace.dsl on LAB-OMARCHY. It is not embedded, so I couldn't check it from the box.
   - Note: the rendered views are very small in print.
5. **ADDRESSED.** Ph8 no longer needs a third machine.
   - r.22 Ph8: "Real-host acceptance requires LAB-OMARCHY(10.42.0.214/Linux) and PAYTON-LEGION2(10.42.0.143/Windows) ... It does not depend on an unprovisioned third machine". Ph8 also adds an ephemeral FED-ATLAS-H/A/B fixture on loopback, and "An absent third physical host is not a blocker".
   - The deployment view (r.5) shows only the two hosts.
6. **ADDRESSED.** The TR and TEST records are now testable.
   - All 16 TRs now carry concrete schemas, routes, status codes and stable error codes, plus an appendix (r.38-39).
   - Every TEST is now a specific Given/When/Then with values. For example: poll desired=7 / reported=6, age warning at 900 s, 100 alternating pooled reads, and a 100+20 audit backfill.
   - r.39 adds fixture rules: symbols tied to captured hashes, and a fake clock.
7. **ADDRESSED.** Join no longer comes before review and publication.
   - r.16 and r.20: Ph3 is now "seed migration, audit-backfill preparation and verified TODO recovery export" and "never invokes publication or reviewed join functionality that does not yet exist".
   - r.21: join moved to Ph6 "After Ph4 commit/branch and Ph5 publication services exist".
   - A new test, SeedPhaseRejectsJoin, covers this.
8. **ADDRESSED.** Aliases and reserved human IDs are covered.
   - r.15 section 12 adds detection of the same work under different IDs, permanent origin-scoped aliases, ambiguous_legacy_id, and hub reservation of human IDs (IMPL-UNITGREEN-001) with offline provisional IDs.
   - r.36-37 TR-009 adds DifferentIdsSameTodoDetected, SimultaneousHumanIdReservation and OfflineHumanIdPublicationDenied.
   - r.26 adds a new workflow, U14.
9. **ADDRESSED.** The needs_review queue is now visible.
   - r.15: "Review visibility is immediate in server status, all plugins and a durable operator inbox". It warns at 15 min, escalates at 24 h and repeats at most hourly.
   - The status DTO gains oldestReviewAgeSeconds and waitingLocalWorkAgeSeconds (r.37).
   - ReviewAgeVisible is defined at r.35.
10. **ADDRESSED.** LEGION on Windows is covered.
    - r.13: a checksum-pinned native Windows dolt.exe "supervised as a child of the existing McpServer Windows service; no assumed Linux container, WSL, Docker Desktop", with F: paths, FileShare.None plus a named mutex, and "Ph0 B1-B12 execute on both real OSs".
    - r.18: every B receipt has separate LAB and LEGION rows. r.42 adds WindowsWriterLockAndRestart.
    - See D6 for an assumption this relies on.
11. **PARTIAL.** Rollback is only reversible for TODOs.
    - r.13 and r.43 add a verified Dolt-to-new-SQLite recovery export, which is real progress. The revision also adds one-node-first cutover with a 48-hour soak before the second host, and keeps the sources read-only.
    - But it is explicitly TODO-only: "This is not a full-database reverse migration: non-TODO project changes need an explicit reconciliation decision" (r.13). Requirements, use cases, memories and session snapshots written after cutover still have no rollback path. This is ADDRESSED only if Payton accepts a TODO-only reverse path.
12. **ADDRESSED.** The network section is fixed.
    - r.13: "Bind every SQL listener only to 127.0.0.1:3306". Remotesapi is loopback-only on LAB port 7448, behind a gateway at 10.42.0.214:7447. No new inbound listener is opened on LEGION. The LEGION address 10.42.0.143 and the 10.42.0.0/24 link are both stated.
    - SqlPortUnreachableAcrossLink (r.41) tests both cross-host 3306 probes.
13. **ADDRESSED, with a note.** Receipts can no longer leak credentials.
    - r.13 states the credential rule verbatim. Receipts omit secret-bearing fields. DOLT_REMOTE_PASSWORD goes only through ProcessStartInfo.Environment. Canary scans run on both hosts, and ReceiptsAndLogsContainNoCredentials is defined at r.40.
    - Note: the canary scope lists "argv, receipts, logs, chat/audit payloads and generated files". It does not name the memory store or git/Dolt commits, even though the rule does. Dolt commits are partly covered by CommitPreviewBlocksSecret.
14. **ADDRESSED.** The HV rules are fixed.
    - The cover no longer claims 98/98 (r.1: "Substantive revision awaiting independent review").
    - Ph0 no longer names Claude (r.18: "independent HV by an agent family other than the author").
    - r.23: "at least 98 each on the exact current architecture, plan, workflow and contract packet".
15. **ADDRESSED.** DEPLOY HOLD is named.
    - It appears on the cover (r.1), at r.14 ("must be explicitly lifted by the operator for the exact Ph8 rollout. A passing test, plan approval or old HV verdict does not lift it"), and in Ph8, section 14 and TR-014 (r.22, r.23, r.42).
16. **PARTIAL.** The wrong walkthrough states were fixed by deleting them, not by correcting them.
    - The narratives are corrected:
      - U06 (r.25): only the A/B link fails, H publishes locally, and the chain is A merges H1+A1 into M1, then B merges M1+B1 into M2. The original (o.37-38) showed H itself "Disconnected" and skipped M1.
      - U10 (r.25): both lab stores are SQLite. The original o.45 card labeled A "SQL Server" and B "PostgreSQL".
      - U11 (r.26): B merges S1 with its retained B1. The original o.48 card shows B simply taking S1.
    - But the step cards were deleted rather than corrected. Only U04 step 5/5 (r.31) and U05 step 4/4 (r.32) remain, and both are unchanged from the original. The new U13-U15 have no illustration. See B1.
17. **ADDRESSED.** A hub restore can no longer roll back generations.
    - r.9: "Generation ordering is scoped to a non-reused HubEpoch". HubEpoch is now in SyncSubscription, and hub_epoch_changed is defined.
    - r.14: restore keeps ingress disabled, uses a fresh random epoch, reconciles retained heads and audit, and nodes resend audit.
    - Covered by U15 (r.27), TR-006/008 with HubRestoreFromOlderBackup (generation 90 vs 40, r.35-36), and FR-179/181.

## B. Unexplained content losses

These losses are not justified by any finding.

1. **The visual walkthrough was cut from 45 steps to 2** (o.26-50, 25 pages).
   - The cover (o.1) promised "the interactive walkthrough is expanded into all 45 steps across 12 workflows". The revision keeps only U04 5/5 and U05 4/4.
   - Lost entirely: all step cards for U01, U02, U03, U06, U07, U08, U09, U10, U11 and U12, and steps 1-4 of U04 and 1-3 of U05. That includes each card's H/A/B local head, working-set state, remote head and control column, plus its caption. Examples: "A saves a draft. No history commit or sync request", "B fetches history but waits; its draft stays untouched", "A restarts and checks the remote head and durable receipt".
   - The contents entry changed from "Visual walkthrough: all 12 workflows / 45 steps" to "Illustrated examples".
   - Finding 16 asked for the wrong states to be corrected, not removed. Roughly 35 correct states were deleted along with them.
2. **The per-FR "Server:" and "Plugin:" obligations were cut** (FR inventory o.22-24, repeated o.51-57 and o.58-65). Each FR is now one sentence (r.27-29). Most server detail moved into the TR contracts, but these plugin obligations no longer appear anywhere:
   - FR-176: "Plugin: project discovery/selection and enrollment diagnostics". Only U01's "discover/select project" remains; the diagnostics are gone.
   - FR-182: "Plugin: ... expose provenance/collision diagnostics".
   - FR-185: "Plugin: least-privilege capabilities and explicit denial".
   - FR-188: "Plugin: distinguish data synchronization from authorized external operations".
   - FR-189: "Plugin: show editing owner, request/release context and support another branch".
   - FR-181: "Plugin: resume/status/cancel without false rollback claims". This is only partly kept, through safe cancellation in architecture section 7.
   - Each original TR also ended with "preserve existing explicit legacy modes". That is now carried only generically by FR-MCP-077 and the DirectProxy criterion.
3. **Ph8 acceptance is weaker** (o.18 P8).
   - Gone: "partition/reconnect all links, restart each component, and verify branches, histories, refs, audit exclusions and control outcomes", "Collect all-provider regression and migration receipts", "using disposable project data", and "Existing unrelated lab work is not a substitute for isolated staging".
   - The Ph6 test list still covers restarts and disconnects at unit or integration level, but the two-host acceptance no longer requires partitioning every link and restarting each component.
4. **The deployment paragraph lost several specifics** (o.11, section 10). Gone:
   - "persistent /var/lib/dolt" (now just "persistent data")
   - "The MySQL SQL endpoint alone is not the Dolt history-transfer endpoint"
   - "test restore" from the backup instruction (r.14 lists what is backed up but never says to test the restore)
   - Of the credential-containment paragraph, "control API's node-scoped credential type" was replaced by IdentityServer, which is fine. Its other points are scattered across B5, section 3 and LocalAppUserCannotPush, but the explicit "editing sessions/plugins never receive those credentials or arbitrary SQL access" no longer appears in the architecture prose.
5. **Donor handling was genericized** (o.14, plan section 3).
   - Gone: "The prior PR83/P1.5 work is a donor for relevant security, identity, cross-platform paths and regression fixes only", "Do not merge old backfill, forwarding, read-switch or custom database synchronization machinery wholesale" and "C4 p4/*, namespace/analyzer work and historical worktree cleanup are not dependencies".
   - It now says only "Reuse existing changes only when they satisfy an in-scope ... requirement".
   - PR83 is no longer named anywhere.
6. **The HV history and review-status page were removed** (o.19 and o.76).
   - Gone: the HV remediation record (Rounds 1-3: 89/84, 95/94, 97/97; F01-F15, N1-N11, R3-M1, R3-L1-L5), the review-status receipt (artifact hashes, readback JSON, visualization validation of 45 states, reviewer limitations, runtime model), "Review infrastructure incidents are recorded separately from substantive scores" and "using full request/response receipts".
   - Dropping the 98/98 claim is justified by finding 14. Dropping the history itself is not; the revision only says results are "retained under docs/receipts/hv" (r.16).
   - The two open R4 items were fixed in the text. R4-L1 is met by Ph8 owning DeploymentPreflightBlocksDirtyOrUnattestedBranch and by Ph7 listing maintenance preflight/attest/status. R4-L2 is met by "owns live-branch writes" (r.6).
7. **Smaller losses:**
   - o.18: "48 records, atomic create succeeded" and "first remediation updated all 48 records successfully" became "registration receipt establishes whether persistence succeeded".
   - o.18: "export canonical requirements ... without overwriting unrelated working-tree changes" is no longer stated in the traceability section. It is still implied by plan section 3.

Compaction, not loss:
- The standalone FR to TR mapping (o.74) and requirements matrix (o.75) are folded into the contract headers and r.47.
- The repeated "Prove X with positive and negative-control evidence" lines are replaced by concrete criteria. The Byrd gate on r.17 still requires negative controls.
- Phase names P0-P8 became Ph0-Ph8, which matches the agreed naming.

## C. Intentional changes tied to findings

- **Audit (finding 1):** FR-MCP-AUDIT-001 and TR-MCP-AUDIT-004 amended, section 11, U13, TR-011 rewritten, CloneOmitsAuditAndSecrets renamed CloneOmitsSecrets, three audit tests added.
- **LocalProxy (finding 2):** FR-MCP-103, TR-MCP-FED-001 and TEST-MCP-136 reconciled in the coverage index, with new criteria.
- **Identity (finding 3):** section 13, B5 rewritten, TR-012 rewritten. "hub revocation is enforced on reconnect" became IdentityServer revocation with no positive cache.
- **Diagrams (finding 4):** Figure 1 replaced by Structurizr container and deployment views.
- **Third machine (finding 5):** NODE-B became a logical peer and FED-ATLAS fixture; Ph8 retitled "two-host acceptance, three-peer fixture and held cutover".
- **Testability (finding 6):** all TR and TEST text rewritten into contracts plus Given/When/Then.
- **Phase ordering (finding 7):** Ph3 limited to seed and export; join moved to Ph6.
- **IDs (finding 8):** the legacy-collision text changed from "rewrites all references together" (o.5) to permanent aliases with historical text never rewritten; section 12 and U14 added.
- **Visibility (finding 9):** review ageing and operator inbox added.
- **Windows (finding 10):** native Windows Dolt, Ph0 runs on both OSs, Windows lock test.
- **Rollback (finding 11):** "automated reverse migration ... out of scope" (o.11, o.18) replaced by the TODO recovery export and the soak.
- **Network (finding 12):** "SQL port 3306 on private network" (o.11) replaced by loopback binding and the gateway.
- **Credentials (finding 13):** credential paragraph and canary test added.
- **HV (finding 14):** cover claim removed, Claude no longer named, agent-family rule added.
- **DEPLOY HOLD (finding 15):** named throughout.
- **Walkthrough (finding 16):** U06, U10 and U11 narratives corrected.
- **Hub restore (finding 17):** HubEpoch, U15, restore tests.

Net-new content not tied to any finding:
- **Gap-check sections:** "Applicable compatibility and migration acceptance" (r.44-46, 34 criteria) and "Federation current-scope coverage" (r.47-48). These pull in FR-MCP-077, FR-MCP-125, FR-MCP-173, FR-MCP-MEMORY-008, FR/TR/TEST-MCP-FED-ROUTE/WSSYNC/BACKFILL/MEMORY/SESSIONLOG/PATH/PATHNORM and TR-MCP-TXNCOMPAT-001, plus a fixture-coverage block.
- **Phase-exit gate:** Nuke Test, MigrationIntegrationTests, the full Build.Tests and ValidateTraceability on both LAB and LEGION (r.17).
- **TLS:** the proposed "QBrain Lab Federation Root CA" with SPKI pinning, plus UnknownCertificatePinRejected (r.13, r.41).
- **Other:** a new contents structure, "Phase identifiers ... do not denote priority levels" (r.16), and a QuadBrain keyserver bypass test (FederationBypassesQuadBrainKeyserver).

## D. New issues introduced by the revision

1. **Error-code contradictions inside the revision:**
   - publication_required is 409 in the appendix (r.38-39) but 403 in RawRemoteCannotBypassPublicationPolicy (r.41).
   - unsupported_capability is 501 in the appendix (r.39) but 409 in SeedPhaseRejectsJoin (r.43).
   - project_required (400, FR-MCP-FED-ROUTE-001 criterion, r.45) is not in the stable failure mapping, which says missing fields return 400 invalid_request.
   - The architecture section 7 error list (r.9) is shorter than the appendix.
2. **A stale audit sentence** (r.12, section 9): "Local audit remains local in control storage or a retained readable source archive according to the manifest." It was carried over unchanged from o.10 and reads as the opposite of the amended hub backfill. It should say audit is retained locally AND backfilled to the hub.
3. **Inconsistent naming of the second node:**
   - r.3 still has "B = NODE-B (illustrative second node...)" and "NODE-B in U01-U12 is a logical test peer".
   - The workflows use FED-ATLAS-B and run U01-U15.
   - BDPv4 section 1 (r.16) still says "LAB-OMARCHY and two nodes must independently edit".
4. **Walkthrough loss** (see B1). Contents item 4 was renamed and the cover claim removed, so the deletion reads as deliberate. It was not requested.
5. **Structurizr source can't be verified from the PDF.** The workspace.dsl and the SVG links are file:// paths on LAB-OMARCHY. Also, the r.15 links point to docs/plans/Project/federation-dolt/Technical-Requirements.md and Testing-Requirements.md, but the original's source path was docs/Project/federation-dolt/. Those links may be broken; worth checking on LAB.
6. **An unverified assumption about LEGION:** r.13 assumes "the existing McpServer Windows service" on LEGION and supervises dolt.exe as its child. It is caveated as "Deployment target, not observed active configuration", but that service's existence and fitness should go into Ph0 evidence.
7. **A version gap in the Duende reference:** the revocation link targets Duende v8 docs, while r.15 notes the repository is on Duende 7.4. This is caveated, but it is a gap to close in Ph0.
8. **HubRestoreFromOlderBackup is owned by three phases** (Ph5, Ph6 and Ph8; r.19, r.21, r.22). The phase that owns it first should be stated.
