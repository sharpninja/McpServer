# P4 review record: docs/Project/Quad-Model-Transactional-Diffgram-Plan.md (DocCode QMD)

PLAN-C4MODEL-001 P4, gate G4 for one document. Worktree /home/sharpninja/github/McpServer/.claude/worktrees/p4-qmd-20261019, branch p4/qmd-20261019, base 92e2c6c40ad61e80ef7ea486f189b0449acfe587 (origin/develop ace70daa is an ancestor). Reviewer: ClaudeCode, claude-opus-5-5, session ClaudeCode-20261019T141815Z-plugin-session, turn req-20261019T202146Z-p4-qmd-convert (parent request req-20261019T144237Z-scope-ruling-replaces-46b8).

Sources: model docs/architecture/c4/model/qmd.dsl, views docs/architecture/c4/views/qmd.dsl, two include lines in docs/architecture/c4/workspace.dsl, and the Start and End styles changed from shape circle to shape ellipse on the coordinator's instruction. Line numbers below are fence start lines (the opening mermaid fence) in the document at 92e2c6c4.

## Method

- Every node, edge, message and branch was mapped by hand to one element or relationship; Mermaid node ids appear only in the mermaidNode and mermaidEdge properties, never as identifiers (D4).
- D3: each element and relationship was checked against src before tagging. Planned marks only what has no code; as-built elements carry an asBuilt property with file and line anchors, and deferred parts carry a deferred property.
- View contents were read back from a pinned-CLI JSON export of the combined workspace (scratch c4-check, inspect exit 0 with 0 lines) and from the rendered SVG files.
- All 23 views were inspected as images (headless Chromium screenshots of the SVG renders).

## Fence 1: AD-TXN-001 Normal Turn Transaction (line 62, flowchart TD)

Views: AD-TXN-001, AD-TXN-001-part2, AD-TXN-001-BR-AOT-APPROVED, AD-TXN-001-BR-AOT-REJECTED (custom views).

- Nodes: source 16, views 16. A qmdTxnTurnStarts (Start), B qmdTxnSubscriberPresent (Decision), C qmdTxnDegraded (Error), D qmdTxnStartTransaction, E qmdTxnSignManifest, F qmdTxnModelsProcess, G qmdTxnAotArbitrates, H qmdTxnAotApproves (Decision), I qmdTxnReconciliation, J qmdTxnModelsAgree (Decision), K qmdCombinedArgument (shared with AD-AOT-001 F), L qmdTxnAbortRollback (Error), M qmdTxnDiffgramPublished, N qmdTxnSubscriberCommits, O qmdTxnCommitted, P qmdTxnResponseReturned (End). Boundary repeats: E in views 1 and 2, H in views 2, 3 and 4.
- Edges: source 16, views 16. View 1: A to B next, B to C No, B to D Yes, D to E next. View 2: E to F, F to G, G to H. BR-AOT-APPROVED: H to M Yes, M to N, N to O, O to P. BR-AOT-REJECTED: H to I No, I to J, J to K Yes, J to L No, K to H.
- Branch ids (5 of 5) as branchId on relationships: BR-NO-SUBSCRIBER on B to C; BR-START on B to D and D to E; BR-AOT-APPROVED on H to M, M to N, N to O, O to P; BR-AOT-REJECTED on H to I, I to J; BR-ROLLBACK on J to L.
- D3: nodes as-built 16 of 16 (TurnTransactionCoordinator.cs:117-123, 296-485; QuadBrainOrchestrationService.cs:55-211; TransactionSecurityServices.cs:284, 872-999). Edges Planned 1 of 16: J to L, because model-disagreement policy automation stays deferred and an invalid vote rejects the orchestration without a transaction rollback (QuadBrainOrchestrationService.cs:170-173). The view description notes that each role invocation runs its own turn transaction after the model call (BrainSlotInvocationService.cs:114-168).

## Fence 2: AD-CURIOSITY-001 Curiosity Engine Flow (line 94, flowchart TD)

Views: AD-CURIOSITY-001, AD-CURIOSITY-001-part2, AD-CURIOSITY-001-part3, AD-CURIOSITY-001-part4 (custom views).

- Nodes: source 16, views 16. A qmdCurMonitor (Start), B qmdCurFrustration (Decision), C qmdCurTargetedResearch, D qmdCurStruggleNovel (Decision), E qmdCurValueRanking, F qmdCurHighValue (Decision), G qmdCurResearchExperiments, H qmdCurExternalNeeded (Decision), I qmdCurCallExternal, J qmdCurLocalResearch, K qmdCurAotReview, L qmdCurApproved (Decision), M qmdCurInject, N qmdCurDiscard (End), O qmdCurWeightUpdate (Decision), P qmdCurDualControlUpdate (End). Boundary repeats: E, H and L.
- Edges: source 16, views 16. View 1: A to B, B to C Yes, B to D No, D to E Yes. Part 2: E to F, F to G Yes, G to H. Part 3: H to I Yes, H to J No, I to K, J to K, K to L. Part 4: L to M Yes, L to N No, M to O, O to P Yes. The source gives D, F and O only a Yes branch; no No branch was added.
- Branch ids (5 of 5) as branchId on the elements they label: BR-MONITOR on A, BR-FRUSTRATION on B, BR-EXTERNAL on I, BR-INJECT on M, BR-WEIGHT on P.
- D3: the autonomous Curiosity flow stays disabled (Implementation Ordering item 5). Nodes as-built 5 of 16: I (BrainSlotInvocationService.cs:88-89, 114-116, 145-168), K (QuadBrainOrchestrationService.cs:214-239, 522), M (BrainSlotInvocationService.cs:86-87, 184-194), O (QuadBrainOrchestrationService.cs:192-197), P (QuadBrainOrchestrationService.cs:288-373). Planned 11 of 16 (A, B, C, D, E, F, G, H, J, L, N), each with no code (grep of src and tests for frustration, novel concept, struggle, value rank, code experiment, quarantine found only the escalation prompt text at QuadBrainOrchestrationService.cs:513 and 829). Edges as-built 1 of 16 (O to P); I to K and M to O are Planned with a deferred note because the escalation path returns before them (QuadBrainOrchestrationService.cs:109-126).

## Fence 3: SD-DIFFGRAM-001 Three-Party Diffgram Exchange (line 126, sequenceDiagram)

Views: SD-DIFFGRAM-001, SD-DIFFGRAM-001-part2, SD-DIFFGRAM-001-part3, SD-DIFFGRAM-001-BR-INVALID, SD-DIFFGRAM-001-BR-VALID, SD-DIFFGRAM-001-BR-VALID-part2 (dynamic views of qbrain.server).

- Participants: source 3. P Publisher (QBrain.AI) is qbrain.server.txsec (reused, TurnTransactionCoordinator). T 3PKS is qbrain.server.qmdKeyServer, a new as-built component (InMemoryKeyServerService registered by AddInProcessTransactionSecurity, Program.cs:214). S Subscriber is qbrain.server.qmdSubscriber, a new as-built component (InMemorySubscriberCommitService). The default in-process parties were chosen because the server has no HTTP client for the standalone Key Server (TransactionSecurityServiceCollectionExtensions.cs:18-102; only AddHttpTransactionSubscriber at line 71 creates one).
- Messages: source 8, views 8, one relationship per message with the message text as description. View 1: 1 P to T Public Keys + Diffgram SHA, 2 T to T Sign Transaction Manifest. Part 2: 3 T to S Encrypted SHA + Encrypted Diffgram, 4 S to S Decrypt SHA (3PKS Pub + Own Priv). Part 3: 5 S to S Verify SHA. BR-INVALID: 6 Abort Transaction. BR-VALID: 7 Decrypt Diffgram (Publisher Pub + Own Priv). BR-VALID-part2: 8 Commit.
- Branches: source 2 (alt Invalid, else Valid), views 2 branch groups. Step numbers restart in each view, so the view titles name the source message numbers.
- Branch ids (5 of 5) as branchId on relationships: MSG-PUBLISHER-KEYS on message 1, MSG-SIGN on 2, MSG-VERIFY-HASH on 5, BR-INVALID on 6, BR-VALID on 7 and 8.
- D3: messages as-built 7 of 8 (TurnTransactionCoordinator.cs:342-345; TransactionSecurityServices.cs:284, 872-999). Planned 1 of 8: message 3, because the coordinator, not the key server, sends the manifest and encrypted diffgram to the subscriber through ITransactionPubSub (TurnTransactionCoordinator.cs:394-404).
- Split reasons: three participants in a row rendered 1730 units wide (run 1). Several self steps of one element are drawn on top of each other (inspected images of the earlier two-step views), so every view holds at most one self step per participant.

## Fence 4: AD-AOT-001 AoT Reconciliation Process (line 158, flowchart TD)

Views: AD-AOT-001, AD-AOT-001-part2 (custom views).

- Nodes: source 9, views 9. A qmdAotRejects (Start), B qmdAotOpenReconciliation, C qmdAotModelRefines, D qmdAotOthersWeighIn, E qmdAotAllAgree (Decision), F qmdCombinedArgument (shared with AD-TXN-001 K), G qmdAotRejectionStands (Error), H qmdAotAccepts (Decision), I qmdAotApproveRevised (End). Boundary repeat: E.
- Edges: source 9, views 9. View 1: A to B, B to C, C to D, D to E. Part 2: E to F Yes, E to G No, F to H, H to I Yes, H to G No.
- Branch ids (4 of 4): BR-REJECT on element A; BR-AGREE on E to F, BR-DISAGREE on E to G, BR-ACCEPT on H to I.
- D3: nodes as-built 9 of 9 and edges as-built 9 of 9 (QuadBrainOrchestrationService.cs:146-210).

## Fence 5: AD-WEIGHT-001 Weight Redistribution Safety (line 182, flowchart TD)

Views: AD-WEIGHT-001, AD-WEIGHT-001-part2 (custom views).

- Nodes: source 9, views 9. A qmdWgtProposes (Start), B qmdWgtAotReview, C qmdWgtAotApproves (Decision), D qmdWgtAdminApproval (Decision), E qmdWgtSnapshot, F qmdWgtRunGates, G qmdWgtGatesPassed (Decision), H qmdWgtApply (End), I qmdWgtReject (Error). Boundary repeats: D and I.
- Edges: source 10, views 10 distinct. View 1: A to B, B to C, C to D Yes, C to I No, D to I No. Part 2: D to E Yes, D to I No (repeated with its boundary nodes), E to F, F to G, G to H Yes, G to I No.
- Branch ids (6 of 6) as branchId on the elements they label: BR-PROPOSE on A, BR-AOT on C, BR-HUMAN on D, BR-SNAPSHOT on E, BR-GATES on G, BR-QUARANTINE on H.
- D3: nodes as-built 9 of 9 and edges as-built 10 of 10 (QuadBrainOrchestrationService.cs:133-197, 288-373; QuadBrainWeightUpdateRequest at BrainSlotContracts.cs:634). H keeps a deferred property for quarantine workflows and automated fine-tuning. As-built, the gate checks run before the snapshot and update (noted in the view description).

## Fence 6: ARCH-QUAD-001 High-Level System Architecture (line 209, flowchart TB)

Views: ARCH-QUAD-001, ARCH-QUAD-001-part2 to ARCH-QUAD-001-part5 (component views of qbrain.server, filtered with exclude relationship.tag!=QMD so only the source edges appear).

- Nodes: source 9 plus 2 subgraphs, views 9. User is developer (reused). AM is qbrain.server.qmdArbiterRole, CR qmdCreativityRole, LG qmdLogicRole, CM qmdCuriosityRole: four new as-built components, one per BrainSlotRoles value (BrainSlotContracts.cs:12-21), linked from qbrain.server.brain (Brain Slots and Quad Brain, which covers BrainSlotContracts and QuadBrainOrchestrationService). Cache is qmdSharedCache, a new as-built component for BrainSlotTurnContext (BrainSlotTurnContext.cs:7-37). PubSub is qbrain.libs.transactionSecurity (reused; its description names the pub/sub broker). KeyServer is qbrain.keyserver and Sub is qbrain.subscriber (reused, as the COMP-KEYSERVER and COMP-SUBSCRIBER annotations name the separate hosts).
- Subgraphs: QBrain.AI is drawn as the MCP Context Server container boundary around the role and cache components. External is ARCH-QUAD-001-part5 (titled External group), whose elements sit outside the server boundary; as-built they are other containers of the QBrain.AI system.
- Edges: source 10 (AM --> CR & LG & CM and CR & LG & CM --> Cache expand to 3 edges each), views 10. View 1: User to AM. Part 2: AM to CR, AM to LG, AM to CM. Part 3: CR, LG and CM to Cache. Part 4: Cache to PubSub. Part 5: KeyServer to PubSub, Sub to PubSub. Boundary repeats: AM, the three roles, Cache and PubSub. The source edges are unlabelled; descriptions come from the document's imported prose (Imported Architecture Overview and Imported Transactional Model).
- Branch ids (6 of 6) as branchId properties on existing elements: COMP-MCP on qbrain.server, COMP-CLIENT on qbrain.libs.client, COMP-KEYSERVER on qbrain.keyserver, COMP-SUBSCRIBER on qbrain.subscriber, COMP-PUBSUB on qbrain.libs.transactionSecurity, COMP-QUAD on qbrain.server.brain.
- D3: nodes as-built 9 of 9. Edges as-built 3 of 10 (CR, LG and CM to Cache: SetCommittedEvidence at QuadBrainOrchestrationService.cs:121, 129, 131, 176, 178, 237). Planned 7 of 10: User to AM and AM to the three roles (the orchestrator, not the AoT, receives the turn and invokes the roles), Cache to PubSub (the turn context does not publish), KeyServer to PubSub and Sub to PubSub (as-built the coordinator calls the key server and delivers commits to the subscriber).

## Totals

- Fences converted: 6 of 6 (flowchart 5, sequence 1). Views: 23 (custom 12, dynamic 6, component 5).
- Elements: 56 new (7 components of qbrain.server, 49 custom activity elements); reused: developer, qbrain.server.txsec, qbrain.server.brain, qbrain.libs.transactionSecurity, qbrain.keyserver, qbrain.subscriber; branchId properties added to 6 existing elements.
- Relationships: 77 new (activity 51, sequence messages 8, structure edges 10, as-built links 8).
- Repo-annotation branch ids preserved: 31 of 31.
- Imported diagram ids as view keys: 6 of 6, each linked exactly once by ../architecture/c4/rendered/<id>.svg with a View line; headings, Imported section lines, repo annotations and prose unchanged.

## Render inspection (images)

Inspected as images: all 23 views. The 17 activity and structure views come from the run 4 render (identical to run 3), and the 6 SD-DIFFGRAM-001 views from the run 9 render. Checked: every node and edge label against the source, Yes, No and next on the right edges, the loop back K to H in AD-TXN-001-BR-AOT-REJECTED, Planned elements drawn with dashed borders, decision diamonds, Start and End ellipses, Error boxes, numbered dynamic steps, legibility at the page size, and no overlapping labels.

Findings:
- Fixed: SD-DIFFGRAM-001-part2 and SD-DIFFGRAM-001-BR-VALID first held two self steps each, and the renderer drew both labels on top of each other; each is now split into one self step per view.
- Kept: in AD-WEIGHT-001 the No edge from AoT Approves? to Reject runs past the right tip of the Human/Admin Approval? diamond and its label sits at that tip; it stays readable.
- Pre-existing, not from QMD sources: the descriptions of Key Server, Transaction Subscriber and QBrainAi.TransactionSecurity overflow their boxes, and the tops of the two container names are clipped by the crop, in ARCH-QUAD-001-part4 and part5 and equally in the as-built CTR-KEYSERVER-01.
- Not visible: every relationship renders dashed by default and the styles have no Planned relationship style, so Planned edges (for example J to L, message 3) look like as-built edges; the tag and properties carry the distinction.

## Gate receipts (logs under docs/receipts/c4model in the worktree)

- Run 1, 20261019T160603-p4-qmd-render-r1.log: PageOverflow 6 in QMD views, all other gate codes 0.
- Run 2, 20261019T161036-p4-qmd-render-r2.log: PageOverflow 1 (AD-WEIGHT-001, 700 x 1664, with Start and End circles of width 220).
- Run 3, 20261019T161238-p4-qmd-render-r3.log, and run 4, 20261019T161426-p4-qmd-render-r4.log: ValidationFailed, InspectionErrors, RenderFailed, DanglingViewLink, MissingRender, OrphanRender, TextSizeSpread, PageOverflow and InvalidSvg all 0; 234 of 234 renders byte-identical.
- Runs 5 to 9 on the final sources (after the self-step split), 20261019T162155-p4-qmd-render-r5.log through 20261019T162936-p4-qmd-render-r9.log: every gate code above 0 in each run. MermaidFence 429 and MermaidFile 4, all in other documents: 213 SessionTurn architecture, 107 Amendment 1, 109 Stateless, 4 Marketing .mmd, 0 naming this document.
- Byte identity on the final sources: run 8 and run 9 each match the run 6 manifest 236 of 236, so runs 8 and 9 are a byte-identical consecutive pair. Runs 5, 6 and 7 differ from one another in 235 of 236 renders. The differences are only renderer id numbers (j_ and v- counters). The first differing render in run order is position 1 (CMP-SERVER-API-02, an as-built P3 view), directly after the first render, whose counter starts at j_38 in runs 6 and 8 and at j_25 in run 7. Inference: the page-wide id counter is consumed at a timing-dependent point before any QMD view renders, so render byte identity is intermittent in the target itself.

## Unit suite (TEST-MCP-165 swap)

- tests/QBrainAi.Support.Mcp.Tests/Documentation/TurnTransactionPlanArtifactTests.cs: ImportedPlan_PreservesAllSixMermaidDiagramIds is replaced by ImportedPlan_DiagramsAreWorkspaceViews, which calls the unchanged AssertImportedDiagramViews with the real plan text, the concatenated docs/architecture/c4/workspace.dsl plus every .dsl under model and views, and File.Exists on docs/architecture/c4/rendered/<id>.svg. The two mock gate tests and the helper are unchanged.
- ./build.ps1 Test on the final sources, run id c4model-qmd-20261019T1630 (log 20261019T163047-p4-qmd-unit-final.log, TRX under TestResults/c4model-qmd-20261019T1630): 7 TRX files, 4670 total, 4650 passed, 4 failed, 16 skipped (QBrainAi.Support.Mcp.Tests 3282 total, 3262 passed; the 16 skipped are Windows-host-only path tests). TurnTransactionPlanArtifactTests 6 of 6 passed, including ImportedPlan_DiagramsAreWorkspaceViews, ImportedDiagramViewContract_AcceptsConvertedFixture and ImportedDiagramViewContract_RejectsDeviations. The 4 failures are the tests PR 82 and PR 83 change: UseCaseGetWorkspaceResolutionContractTests.ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace, RemoveTodoRecordsMigrationTests.Migrate_RemoveTodoRecords_BackfillsAnchorOnlyTriageTodos, RemoveTodoRecordsMigrationTests.Migrate_RepairTriageCreatedTodoWorkspace_CopiesWrongWorkspaceTodoToTriageWorkspace, WorkspacePathNormalizerRealTests.ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted. The suite is therefore not green under BDPv4 (0 failed, 0 skipped).
- An earlier run on the pre-split sources (run id c4model-qmd-20261019T1615) gave the same counts.
- bench-json-guard restored docs/benchmarks/results/memory-bench-20260919T091800Z.json and memory-bench-multiturn-20260919T091800Z.json after each run; the only change in each was the bench_workspace_id line.

## Open questions

- Document line 56 still says the Mermaid source in this section is preserved from the imported document; D7 keeps prose unchanged, so the sentence now describes content that is gone.
- Combined Argument to AoT is one element for AD-TXN-001 K and AD-AOT-001 F (same step, same name); All 3 Models Agree? (AD-TXN-001 J) and All Three Agree? (AD-AOT-001 E) stay two elements because their source texts differ.
- Generic custom element names may collide with other documents at merge: Reject, Discard, Approved?, High Value?, Response Returned, Transaction Committed, Create Snapshot, Local Research, Value Ranking, Weight Update?.
