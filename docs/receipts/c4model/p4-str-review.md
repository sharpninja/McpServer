# P4 STR review record: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md

PLAN-C4MODEL-001 phase P4, document code STR. Source fences are those of docs/architecture/SessionTurn-Review-AgentContext-Architecture.md at commit 92e2c6c40ad61e80ef7ea486f189b0449acfe587; every line number below is a line of that commit. Model fragment docs/architecture/c4/model/str.dsl, views fragment docs/architecture/c4/views/str.dsl. Generated 2026-10-19T19:46:41 (local) by SCRATCH/p4-str/gen-review.ps1 from the converter coverage report SCRATCH/p4-str/convert-out/coverage.json and the notes in SCRATCH/p4-str/review-notes.psd1.

## Summary

* Fences: 213 (activity flowchart 117, structure flowchart 1, bounded-context graph 1, use-case flowchart 16, sequence diagram 58, ER diagram 16, state diagram 4).
* Views: 1418, one image and view-key pair each in the converted document.
* Activity flowcharts: 1162 of 1162 nodes and 1181 of 1181 edges shown.
* Sequence diagrams: 176 messages, 298 replies, 34 self messages, 217 notes, 115 blocks with 345 branches; 1311 view steps of 1311 expected (including 126 continuation steps of long self steps).
* ER diagrams: 83 of 83 entity occurrences and 73 of 73 relationship occurrences shown.
* State diagrams: 31 of 31 state elements and 60 of 60 transitions and substate links shown (56 source transitions).
* Structure, use-case and bounded-context flowcharts: 199 of 199 element edges shown.
* Coverage gaps: none (every fence below shows all of its source material)

## How the views are keyed and split

* A fence whose heading names a unit of work or diagram identifier keeps it as the view key (D6: for example Resolve-MintOutcome, RequestPipeline-Sequence, UC-1); every other fence is STR-<section>-<NN>, numbered in fence order within its section. Section 9 of the document (the diagram index) now names the key of every unnamed diagram.
* Page parts (AC-5): part 1 keeps the key, later parts are <key>-part2, <key>-part3 and so on. An activity part draws the nodes it owns with all their successors, so the node at a part boundary appears in both parts; a node without successors that an earlier part already drew is not drawn again alone.
* Sequence blocks (D5): each alt or par branch is the dynamic view <key>-alt<k>-<i> (or -par<k>-<i>), each opt, loop and break body the view <key>-opt<k>, <key>-loop<k> or <key>-break<k>, numbered per fence and linked after the parent views. The parent view carries one marker step per block and each branch view starts with its condition step, so a render states its branch.
* Sequence parts hold at most 2 participants, 5 steps and one self step per participant (two self steps on one participant draw the same loop and hide each other), laid out lr 300 100. A reply runs over the caller-to-callee relationship (the CLI accepts a step against the relationship direction), so each participant pair has one relationship.
* A self step (a note, a self message, a block marker or a branch condition) is labelled on a loop above its element; a label of more than 6 lines (15 characters a line) covered the element name, so a longer text is drawn as consecutive self steps "(1 of n) ...", "(2 of n) ...", each in its own part. No word of the source text is dropped; the review counts below include these continuation steps.
* Layout is chosen per part from a longest-path layering (depth and width): activity parts lr 250 150 up to depth 2 by width 3, lr 200 50 at width 4, tb 100 200 up to depth 5 by width 1, tb 100 100 up to depth 3 by width 2, at most 7 nodes; state parts lr 300 300 or tb 200 300, at most 3 states, and each state self transition in a part of its own (excluded from the other parts by an exclude expression); entity parts lr 300 200 (at most 3 entities) or tb 200 300; use-case static parts tb 400 250 (two ranks) or tb 200 100; the bounded-context graph custom parts as activity parts; the system context F1 at most two ranks of tb 250 100 because its three nested boundaries add height. The separations keep a relationship that skips a rank (it runs beside the node in between) or converges with another from covering that node or label.
* Self-step labels on McpDbContext are limited to 3 lines, because the as-built McpDbContext description is longer than its box and pushes its name above the box, where a taller label covered it.
* Activity element names come from a text-fit model calibrated on the renders (wrap-calibrate.ps1: names wrap at 20 characters, descriptions at 29): the whole node text is the name when its text block fits the 220-high shape; otherwise the text is split at a clause break, a comma, a parenthesis, a preposition or a word, the rest becoming the description.
* Static views of the system context and the use cases show only relationships carrying their context tag (exclude relationship.tag!=SystemContext or UseCase), so sequence-only relationships between the same elements stay out. Entity views made only of planned relationships exclude relationship.tag!=Planned.

## Fences

* F1, line 26, structure flowchart (section 2. System context)
  * Views (11): STR-02-01, STR-02-01-part2, STR-02-01-part3, STR-02-01-part4, STR-02-01-part5, STR-02-01-part6, STR-02-01-part7, STR-02-01-part8, STR-02-01-part9, STR-02-01-part10, STR-02-01-part11
  * Source 18 nodes and 21 edges; drawn on 15 model elements (a node that names an existing element or a use case drawn twice is one element) with 21 element edges; views show 15 of 15 elements and 21 of 21 element edges.
* F2, line 247, activity flowchart (section 3.2 Host binding table and TypeScript-host adapter contract)
  * Views (2): STR-03-01, STR-03-01-part2
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 1, End 2) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F3, line 330, activity flowchart (section 3.5.1 Request pipeline (CQRS behaviors))
  * Views (2): RequestPipeline, RequestPipeline-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F4, line 346, activity flowchart (section 3.5.2 Storage and errors)
  * Views (3): SerializableWrite, SerializableWrite-part2, SerializableWrite-part3
  * Source 9 nodes and 10 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 2, End 1) and 10 relationships; views show 9 of 9 nodes and 10 of 10 edges.
* F5, line 362, activity flowchart (section 3.5.2 Storage and errors)
  * Views (5): IdempotentCreate, IdempotentCreate-part2, IdempotentCreate-part3, IdempotentCreate-part4, IdempotentCreate-part5
  * Source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 2, Decision 3, Error 1, End 4) and 12 relationships; views show 11 of 11 nodes and 12 of 12 edges.
* F6, line 381, activity flowchart (section 3.5.2 Storage and errors)
  * Views (2): PagedQuery, PagedQuery-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 1, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F7, line 401, activity flowchart (section 3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`))
  * Views (3): TurnLocator, TurnLocator-part2, TurnLocator-part3
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 0, Decision 4, Error 3, End 2) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F8, line 416, activity flowchart (section 3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`))
  * Views (6): TurnWritePipeline, TurnWritePipeline-part2, TurnWritePipeline-part3, TurnWritePipeline-part4, TurnWritePipeline-part5, TurnWritePipeline-part6
  * Source 20 nodes and 21 edges; model 20 Activity elements (Start 1, Action 7, Decision 3, Error 7, End 2) and 21 relationships; views show 20 of 20 nodes and 21 of 21 edges.
* F9, line 455, activity flowchart (section 3.5.5 Agent context)
  * Views (3): ResponseStatusTransition, ResponseStatusTransition-part2, ResponseStatusTransition-part3
  * Source 10 nodes and 10 edges; model 10 Activity elements (Start 1, Action 3, Decision 3, Error 1, End 2) and 10 relationships; views show 10 of 10 nodes and 10 of 10 edges.
* F10, line 471, activity flowchart (section 3.5.5 Agent context)
  * Views (3): IResponseStalenessRule, IResponseStalenessRule-part2, IResponseStalenessRule-part3
  * Source 8 nodes and 8 edges; model 8 Activity elements (Start 1, Action 1, Decision 3, Error 0, End 3) and 8 relationships; views show 8 of 8 nodes and 8 of 8 edges.
* F11, line 486, activity flowchart (section 3.5.5 Agent context)
  * Views (2): RelationshipWriter, RelationshipWriter-part2
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 2) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F12, line 503, activity flowchart (section 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Views (4): Resolve-MintOutcome, Resolve-MintOutcome-part2, Resolve-MintOutcome-part3, Resolve-MintOutcome-part4
  * Source 10 nodes and 11 edges; model 10 Activity elements (Start 1, Action 0, Decision 3, Error 3, End 3) and 11 relationships; views show 10 of 10 nodes and 11 of 11 edges.
* F13, line 520, activity flowchart (section 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Views (2): Invoke-SessionOpen, Invoke-SessionOpen-part2
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 0, Decision 1, Error 2, End 1) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F14, line 530, activity flowchart (section 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Views (2): Set-SessionUnsupported, Set-SessionUnsupported-part2
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 2, End 0) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F15, line 540, activity flowchart (section 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Views (3): Update-TurnState, Update-TurnState-part2, Update-TurnState-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 2, End 1) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F16, line 562, activity flowchart (section 3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Views (3): Add-McpAlias, Add-McpAlias-part2, Add-McpAlias-part3
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 2) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F17, line 597, state diagram (section 4.1 Components and data)
  * Views (6): STR-04-01, STR-04-01-part2, STR-04-01-part3, STR-04-01-part4, STR-04-01-part5, STR-04-01-part6
  * Source 6 states, 2 start or end pseudo states and 13 transitions; model 8 State elements and 13 relationships (0 substate links); views show 8 of 8 elements and 13 of 13 transitions and links.
* F18, line 640, activity flowchart (section MintSessionTurnCommand: mint a turn at ingress [FR-MCP-SESSIONTURN-001])
  * Views (4): MintSessionTurnCommand, MintSessionTurnCommand-part2, MintSessionTurnCommand-part3, MintSessionTurnCommand-part4
  * Source 12 nodes and 12 edges; model 12 Activity elements (Start 1, Action 2, Decision 2, Error 5, End 2) and 12 relationships; views show 12 of 12 nodes and 12 of 12 edges.
* F19, line 658, activity flowchart (section Nested activity: InsertMintedTurnWithRetry (insert step of MintSessionTurnCommand, rerun by SerializableWrite))
  * Views (3): InsertMintedTurnWithRetry, InsertMintedTurnWithRetry-part2, InsertMintedTurnWithRetry-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 1, Error 1, End 3) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F20, line 679, activity flowchart (section SubmitSessionLogCommand: submit a session log with per-turn outcomes [FR-MCP-SESSIONTURN-004])
  * Views (5): SubmitSessionLogCommand, SubmitSessionLogCommand-part2, SubmitSessionLogCommand-part3, SubmitSessionLogCommand-part4, SubmitSessionLogCommand-part5
  * Source 14 nodes and 14 edges; model 14 Activity elements (Start 1, Action 5, Decision 2, Error 5, End 1) and 14 relationships; views show 14 of 14 nodes and 14 of 14 edges.
* F21, line 699, activity flowchart (section Nested activity: EvaluateSubmittedTurn (loop body of SubmitSessionLogCommand))
  * Views (4): EvaluateSubmittedTurn, EvaluateSubmittedTurn-part2, EvaluateSubmittedTurn-part3, EvaluateSubmittedTurn-part4
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 0, Error 3, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F22, line 727, activity flowchart (section UpsertSessionTurnCommand: append or merge one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-006, FR-MCP-SESSIONTURN-007])
  * Views (5): UpsertSessionTurnCommand, UpsertSessionTurnCommand-part2, UpsertSessionTurnCommand-part3, UpsertSessionTurnCommand-part4, UpsertSessionTurnCommand-part5
  * Source 15 nodes and 14 edges; model 15 Activity elements (Start 1, Action 1, Decision 4, Error 8, End 1) and 14 relationships; views show 15 of 15 nodes and 14 of 14 edges.
* F23, line 752, activity flowchart (section PatchSessionTurnCommand: change named fields of one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007])
  * Views (5): PatchSessionTurnCommand, PatchSessionTurnCommand-part2, PatchSessionTurnCommand-part3, PatchSessionTurnCommand-part4, PatchSessionTurnCommand-part5
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 2, Decision 3, Error 7, End 1) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F24, line 777, activity flowchart (section ReplaceSessionTurnCommand: replace one turn [FR-MCP-SESSIONTURN-004, FR-SUPPORT-016])
  * Views (5): ReplaceSessionTurnCommand, ReplaceSessionTurnCommand-part2, ReplaceSessionTurnCommand-part3, ReplaceSessionTurnCommand-part4, ReplaceSessionTurnCommand-part5
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 2, Decision 3, Error 7, End 1) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F25, line 796, activity flowchart (section Nested activity: PrepareReplacementTurn (body step of ReplaceSessionTurnCommand, before TurnWritePipeline))
  * Views (1): PrepareReplacementTurn
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 2, Decision 1, Error 0, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F26, line 812, activity flowchart (section CompleteSessionTurnCommand: complete one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002])
  * Views (4): CompleteSessionTurnCommand, CompleteSessionTurnCommand-part2, CompleteSessionTurnCommand-part3, CompleteSessionTurnCommand-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 2, Decision 2, Error 6, End 1) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F27, line 834, activity flowchart (section FailSessionTurnCommand: fail one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONCLASS-002])
  * Views (4): FailSessionTurnCommand, FailSessionTurnCommand-part2, FailSessionTurnCommand-part3, FailSessionTurnCommand-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 2, Decision 2, Error 6, End 1) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F28, line 856, activity flowchart (section OpenSessionLogCommand: open (ensure) a session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007])
  * Views (3): OpenSessionLogCommand, OpenSessionLogCommand-part2, OpenSessionLogCommand-part3
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 1, End 2) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F29, line 872, activity flowchart (section AppendTurnDialogCommand: append processing dialog to one turn [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-007])
  * Views (3): AppendTurnDialogCommand, AppendTurnDialogCommand-part2, AppendTurnDialogCommand-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 1, Error 3, End 1) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F30, line 890, activity flowchart (section SetTurnTitleCommand: retitle one turn [FR-MCP-SESSIONTURN-003, FR-MCP-SESSIONTURN-007])
  * Views (3): SetTurnTitleCommand, SetTurnTitleCommand-part2, SetTurnTitleCommand-part3
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 2, End 1) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F31, line 906, activity flowchart (section SetSessionTitleCommand: retitle a session [FR-MCP-SESSIONTURN-007])
  * Views (3): SetSessionTitleCommand, SetSessionTitleCommand-part2, SetSessionTitleCommand-part3
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 2, End 1) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F32, line 924, activity flowchart (section Policy TerminalTurnOutcomeGuard: terminal-turn outcome guard (`ITerminalTurnOutcomeGuard`) [FR-MCP-SESSIONTURN-004])
  * Views (2): TerminalTurnOutcomeGuard, TerminalTurnOutcomeGuard-part2
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 0, Decision 3, Error 1, End 3) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F33, line 947, activity flowchart (section Process ApplyArrivalBridgeRecord: process a transcript record in the arrival bridge [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002])
  * Views (7): ApplyArrivalBridgeRecord, ApplyArrivalBridgeRecord-part2, ApplyArrivalBridgeRecord-part3, ApplyArrivalBridgeRecord-part4, ApplyArrivalBridgeRecord-part5, ApplyArrivalBridgeRecord-part6, ApplyArrivalBridgeRecord-part7
  * Source 16 nodes and 22 edges; model 16 Activity elements (Start 1, Action 7, Decision 5, Error 1, End 2) and 22 relationships; views show 16 of 16 nodes and 22 of 22 edges.
* F34, line 977, activity flowchart (section Nested activity: ApplyEnqueueRecord (enqueue step of ApplyArrivalBridgeRecord))
  * Views (3): ApplyEnqueueRecord, ApplyEnqueueRecord-part2, ApplyEnqueueRecord-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 2, Decision 1, Error 1, End 4) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F35, line 993, activity flowchart (section Nested activity: ApplyDequeueRecord (dequeue step of ApplyArrivalBridgeRecord))
  * Views (2): ApplyDequeueRecord, ApplyDequeueRecord-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 0, Decision 2, Error 1, End 2) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F36, line 1006, activity flowchart (section Nested activity: ApplyRemoveRecord (remove step of ApplyArrivalBridgeRecord))
  * Views (2): ApplyRemoveRecord, ApplyRemoveRecord-part2
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 0, Decision 3, Error 1, End 3) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F37, line 1029, activity flowchart (section Process OpenTurnFromPromptHook: open a turn from the prompt hook (hosts without a bridge) [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-002, FR-MCP-SESSIONTURN-003])
  * Views (4): OpenTurnFromPromptHook, OpenTurnFromPromptHook-part2, OpenTurnFromPromptHook-part3, OpenTurnFromPromptHook-part4
  * Source 13 nodes and 12 edges; model 13 Activity elements (Start 1, Action 4, Decision 3, Error 3, End 2) and 12 relationships; views show 13 of 13 nodes and 12 of 12 edges.
* F38, line 1049, activity flowchart (section Nested activity: OpenTurnUnderLock (the open of OpenTurnFromPromptHook, inside one Update-TurnState change))
  * Views (6): OpenTurnUnderLock, OpenTurnUnderLock-part2, OpenTurnUnderLock-part3, OpenTurnUnderLock-part4, OpenTurnUnderLock-part5, OpenTurnUnderLock-part6
  * Source 19 nodes and 21 edges; model 19 Activity elements (Start 1, Action 6, Decision 4, Error 2, End 6) and 21 relationships; views show 19 of 19 nodes and 21 of 21 edges.
* F39, line 1086, activity flowchart (section Process BindVerbToTurn: bind a session-log verb to its turn [FR-MCP-SESSIONTURN-007])
  * Views (5): BindVerbToTurn, BindVerbToTurn-part2, BindVerbToTurn-part3, BindVerbToTurn-part4, BindVerbToTurn-part5
  * Source 14 nodes and 14 edges; model 14 Activity elements (Start 1, Action 3, Decision 3, Error 5, End 2) and 14 relationships; views show 14 of 14 nodes and 14 of 14 edges.
* F40, line 1108, activity flowchart (section Nested activity: ResolveVerbHostSession (host session step of BindVerbToTurn))
  * Views (3): ResolveVerbHostSession, ResolveVerbHostSession-part2, ResolveVerbHostSession-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 0, Decision 3, Error 2, End 3) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F41, line 1124, activity flowchart (section Nested activity: ResolveVerbTarget (target step of BindVerbToTurn))
  * Views (5): ResolveVerbTarget, ResolveVerbTarget-part2, ResolveVerbTarget-part3, ResolveVerbTarget-part4, ResolveVerbTarget-part5
  * Source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 1, Decision 4, Error 4, End 1) and 12 relationships; views show 11 of 11 nodes and 12 of 12 edges.
* F42, line 1150, activity flowchart (section Process PersistTurnWriteWithReceipt: persist a turn write with a truthful receipt (dispatches SubmitSessionLogCommand with the one bound turn, AppendTurnDialogCommand, or SetTurnTitleCommand through the REPL) [FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005])
  * Views (8): PersistTurnWriteWithReceipt, PersistTurnWriteWithReceipt-part2, PersistTurnWriteWithReceipt-part3, PersistTurnWriteWithReceipt-part4, PersistTurnWriteWithReceipt-part5, PersistTurnWriteWithReceipt-part6, PersistTurnWriteWithReceipt-part7, PersistTurnWriteWithReceipt-part8
  * Source 15 nodes and 19 edges; model 15 Activity elements (Start 1, Action 6, Decision 4, Error 2, End 2) and 19 relationships; views show 15 of 15 nodes and 19 of 19 edges.
* F43, line 1177, activity flowchart (section Nested activity: SendBoundWrite (send step of PersistTurnWriteWithReceipt, for the evidence write and the bound write, and of the drain's ReplayBoundRecord))
  * Views (5): SendBoundWrite, SendBoundWrite-part2, SendBoundWrite-part3, SendBoundWrite-part4, SendBoundWrite-part5
  * Source 16 nodes and 15 edges; model 16 Activity elements (Start 1, Action 3, Decision 5, Error 4, End 3) and 15 relationships; views show 16 of 16 nodes and 15 of 15 edges.
* F44, line 1200, activity flowchart (section Nested activity: MapSubmitOutcome (submit answer step of SendBoundWrite))
  * Views (3): MapSubmitOutcome, MapSubmitOutcome-part2, MapSubmitOutcome-part3
  * Source 10 nodes and 10 edges; model 10 Activity elements (Start 1, Action 2, Decision 3, Error 2, End 2) and 10 relationships; views show 10 of 10 nodes and 10 of 10 edges.
* F45, line 1230, activity flowchart (section Process DrainFailsafe: drain the failsafe [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-004, FR-MCP-SESSIONTURN-005])
  * Views (1): DrainFailsafe
  * Source 4 nodes and 3 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 0, End 2) and 3 relationships; views show 4 of 4 nodes and 3 of 3 edges.
* F46, line 1241, activity flowchart (section Nested activity: DrainOneFailsafeRecord (loop body of DrainFailsafe))
  * Views (5): DrainOneFailsafeRecord, DrainOneFailsafeRecord-part2, DrainOneFailsafeRecord-part3, DrainOneFailsafeRecord-part4, DrainOneFailsafeRecord-part5
  * Source 19 nodes and 19 edges; model 19 Activity elements (Start 1, Action 2, Decision 7, Error 5, End 4) and 19 relationships; views show 19 of 19 nodes and 19 of 19 edges.
* F47, line 1268, activity flowchart (section Nested activity: ReplayBoundRecord (bound record step of DrainOneFailsafeRecord))
  * Views (7): ReplayBoundRecord, ReplayBoundRecord-part2, ReplayBoundRecord-part3, ReplayBoundRecord-part4, ReplayBoundRecord-part5, ReplayBoundRecord-part6, ReplayBoundRecord-part7
  * Source 14 nodes and 19 edges; model 14 Activity elements (Start 1, Action 5, Decision 3, Error 3, End 2) and 19 relationships; views show 14 of 14 nodes and 19 of 19 edges.
* F48, line 1299, activity flowchart (section Process EvaluateStopGate: evaluate the Stop gate [FR-MCP-SESSIONTURN-005])
  * Views (4): EvaluateStopGate, EvaluateStopGate-part2, EvaluateStopGate-part3, EvaluateStopGate-part4
  * Source 13 nodes and 12 edges; model 13 Activity elements (Start 1, Action 7, Decision 2, Error 1, End 2) and 12 relationships; views show 13 of 13 nodes and 12 of 12 edges.
* F49, line 1319, activity flowchart (section Nested activity: EvaluateTurnAtStop (loop body of EvaluateStopGate))
  * Views (3): EvaluateTurnAtStop, EvaluateTurnAtStop-part2, EvaluateTurnAtStop-part3
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 1, Decision 4, Error 1, End 4) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F50, line 1337, activity flowchart (section Nested activity: EvaluateOpenTurnAtStop (open turn step of EvaluateTurnAtStop))
  * Views (4): EvaluateOpenTurnAtStop, EvaluateOpenTurnAtStop-part2, EvaluateOpenTurnAtStop-part3, EvaluateOpenTurnAtStop-part4
  * Source 14 nodes and 13 edges; model 14 Activity elements (Start 1, Action 0, Decision 6, Error 3, End 4) and 13 relationships; views show 14 of 14 nodes and 13 of 13 edges.
* F51, line 1359, activity flowchart (section Process ValidateHookRegistration: validate and repair hook registration [FR-MCP-SESSIONTURN-001])
  * Views (3): ValidateHookRegistration, ValidateHookRegistration-part2, ValidateHookRegistration-part3
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 3, Decision 3, Error 1, End 3) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F52, line 1382, activity flowchart (section Process StartHostSession: start a host session [FR-MCP-SESSIONTURN-001, FR-MCP-SESSIONTURN-007])
  * Views (7): StartHostSession, StartHostSession-part2, StartHostSession-part3, StartHostSession-part4, StartHostSession-part5, StartHostSession-part6, StartHostSession-part7
  * Source 19 nodes and 21 edges; model 19 Activity elements (Start 1, Action 9, Decision 5, Error 3, End 1) and 21 relationships; views show 19 of 19 nodes and 21 of 21 edges.
* F53, line 1409, activity flowchart (section Process EndHostSession: end a host session [FR-MCP-SESSIONTURN-007])
  * Views (1): EndHostSession
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 2, Decision 1, Error 0, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F54, line 1425, activity flowchart (section Process SyncAgentPluginsAtVersion: sync the agent plugins at a fixed version [TR-MCP-NUKE-003 under FR-MCP-BUILD-001])
  * Views (3): SyncAgentPluginsAtVersion, SyncAgentPluginsAtVersion-part2, SyncAgentPluginsAtVersion-part3
  * Source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 3, Decision 2, Error 3, End 1) and 9 relationships; views show 10 of 10 nodes and 9 of 9 edges.
* F55, line 1440, activity flowchart (section Nested activity: SyncOneAgentPlugin (loop body of SyncAgentPluginsAtVersion))
  * Views (4): SyncOneAgentPlugin, SyncOneAgentPlugin-part2, SyncOneAgentPlugin-part3, SyncOneAgentPlugin-part4
  * Source 10 nodes and 11 edges; model 10 Activity elements (Start 1, Action 4, Decision 3, Error 1, End 1) and 11 relationships; views show 10 of 10 nodes and 11 of 11 edges.
* F56, line 1460, activity flowchart (section Process RunArrivalBridge: arrival bridge lifecycle (claude-code, claude-cowork) [FR-MCP-SESSIONTURN-001])
  * Views (3): RunArrivalBridge, RunArrivalBridge-part2, RunArrivalBridge-part3
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 3, Decision 1, Error 1, End 2) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F57, line 1473, activity flowchart (section Nested activity: TailTranscriptRecords (loop of RunArrivalBridge))
  * Views (4): TailTranscriptRecords, TailTranscriptRecords-part2, TailTranscriptRecords-part3, TailTranscriptRecords-part4
  * Source 8 nodes and 10 edges; model 8 Activity elements (Start 1, Action 2, Decision 3, Error 1, End 1) and 10 relationships; views show 8 of 8 nodes and 10 of 10 edges.
* F58, line 1508, sequence diagram (section Sequence: submit MintSessionTurnCommand and process each response)
  * Views (13): STR-04-02, STR-04-02-part2, STR-04-02-part3, STR-04-02-part4, STR-04-02-part5, STR-04-02-part6, STR-04-02-part7, STR-04-02-alt1-1, STR-04-02-alt1-2, STR-04-02-alt1-3, STR-04-02-alt1-4, STR-04-02-alt1-5, STR-04-02-alt1-6
  * Participants 5: P=Plugin -> qbrain.plugins.pluginPs; R=REPL -> qbrain.repl; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 9 replies, 1 self messages, 3 notes, 1 blocks with 6 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 6 branch views for 6 branches.
* F59, line 1548, sequence diagram (section Nested sequence: PrepareHookMint (prompt hook steps before the mint call))
  * Views (15): PrepareHookMint, PrepareHookMint-part2, PrepareHookMint-alt1-1, PrepareHookMint-alt1-1-part2, PrepareHookMint-alt1-2, PrepareHookMint-alt1-2-part2, PrepareHookMint-alt1-3, PrepareHookMint-alt1-3-part2, PrepareHookMint-alt1-3-part3, PrepareHookMint-alt1-4, PrepareHookMint-alt1-4-part2, PrepareHookMint-alt1-4-part3, PrepareHookMint-alt1-4-part4, PrepareHookMint-alt1-4-part5, PrepareHookMint-opt1
  * Participants 2: P=Plugin -> qbrain.plugins.pluginPs; R=REPL -> qbrain.repl.
  * Source 1 messages, 1 replies, 3 self messages, 4 notes, 2 blocks with 5 branches; views carry 17 steps of 17 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 5 branch views for 5 branches.
* F60, line 1575, sequence diagram (section Nested sequence: ApplyMintOutcome (plugin step after Resolve-MintOutcome))
  * Views (14): ApplyMintOutcome, ApplyMintOutcome-alt1-1, ApplyMintOutcome-alt1-1-part2, ApplyMintOutcome-alt1-2, ApplyMintOutcome-alt1-2-part2, ApplyMintOutcome-alt1-3, ApplyMintOutcome-alt1-3-part2, ApplyMintOutcome-alt1-3-part3, ApplyMintOutcome-alt1-4, ApplyMintOutcome-alt1-4-part2, ApplyMintOutcome-alt1-5, ApplyMintOutcome-alt1-5-part2, ApplyMintOutcome-alt1-6, ApplyMintOutcome-alt1-6-part2
  * Participants 1: P=Plugin -> qbrain.plugins.pluginPs.
  * Source 0 messages, 0 replies, 1 self messages, 5 notes, 1 blocks with 6 branches; views carry 14 steps of 14 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 6 branch views for 6 branches.
* F61, line 1598, sequence diagram (section Sequence: submit SubmitSessionLogCommand and process the per-turn outcomes)
  * Views (12): STR-04-03, STR-04-03-part2, STR-04-03-part3, STR-04-03-part4, STR-04-03-alt1-1, STR-04-03-alt1-1-part2, STR-04-03-alt1-1-part3, STR-04-03-alt1-2, STR-04-03-alt1-2-part2, STR-04-03-alt1-3, STR-04-03-alt1-4, STR-04-03-alt1-5
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 6 replies, 0 self messages, 6 notes, 1 blocks with 5 branches; views carry 24 steps of 24 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 5 branch views for 5 branches.
* F62, line 1632, sequence diagram (section Sequence: submit SubmitSessionLogCommand from a plugin verb and process each response)
  * Views (17): STR-04-04, STR-04-04-part2, STR-04-04-alt1-1, STR-04-04-alt1-2, STR-04-04-alt1-2-part2, STR-04-04-alt1-3, STR-04-04-alt1-3-part2, STR-04-04-alt1-3-part3, STR-04-04-alt1-3-part4, STR-04-04-alt1-3-part5, STR-04-04-alt2-1, STR-04-04-alt2-2, STR-04-04-alt2-3, STR-04-04-alt2-4, STR-04-04-alt2-4-part2, STR-04-04-alt2-5, STR-04-04-alt2-5-part2
  * Participants 3: M=Model -> strHostModel; P=Plugin -> qbrain.plugins.pluginPs; C=Controller -> qbrain.server.api.
  * Source 1 messages, 7 replies, 0 self messages, 2 notes, 2 blocks with 8 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 5 continuation steps of long self steps); 8 branch views for 8 branches.
* F63, line 1664, sequence diagram (section Sequence: submit UpsertSessionTurnCommand and process each response)
  * Views (14): STR-04-05, STR-04-05-part2, STR-04-05-alt1-1, STR-04-05-alt1-2, STR-04-05-alt1-2-part2, STR-04-05-alt2-1, STR-04-05-alt2-1-part2, STR-04-05-alt2-1-part3, STR-04-05-alt2-2, STR-04-05-alt2-2-part2, STR-04-05-alt2-3, STR-04-05-alt2-4, STR-04-05-alt2-5, STR-04-05-alt2-6
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 8 replies, 0 self messages, 7 notes, 2 blocks with 8 branches; views carry 30 steps of 30 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 8 branch views for 8 branches.
* F64, line 1705, sequence diagram (section Sequence: submit PatchSessionTurnCommand and process each response)
  * Views (12): STR-04-06, STR-04-06-part2, STR-04-06-part3, STR-04-06-alt1-1, STR-04-06-alt1-1-part2, STR-04-06-alt1-1-part3, STR-04-06-alt1-2, STR-04-06-alt1-2-part2, STR-04-06-alt1-3, STR-04-06-alt1-4, STR-04-06-alt1-5, STR-04-06-alt1-6
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 7 replies, 0 self messages, 6 notes, 1 blocks with 6 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 6 branch views for 6 branches.
* F65, line 1741, sequence diagram (section Sequence: submit ReplaceSessionTurnCommand and process each response)
  * Views (13): STR-04-07, STR-04-07-part2, STR-04-07-part3, STR-04-07-alt1-1, STR-04-07-alt1-1-part2, STR-04-07-alt1-2, STR-04-07-alt1-2-part2, STR-04-07-alt1-3, STR-04-07-alt1-4, STR-04-07-alt1-5, STR-04-07-alt1-5-part2, STR-04-07-alt1-5-part3, STR-04-07-alt1-6
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 7 replies, 0 self messages, 6 notes, 1 blocks with 6 branches; views carry 26 steps of 26 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 6 branch views for 6 branches.
* F66, line 1777, sequence diagram (section Sequence: close a turn from a plugin verb (completeTurn or failTurn through SubmitSessionLogCommand) and process each response)
  * Views (21): STR-04-08, STR-04-08-part2, STR-04-08-alt1-1, STR-04-08-alt1-2, STR-04-08-alt1-3, STR-04-08-alt1-3-part2, STR-04-08-alt1-3-part3, STR-04-08-alt2-1, STR-04-08-alt2-1-part2, STR-04-08-alt2-2, STR-04-08-alt2-2-part2, STR-04-08-alt2-2-part3, STR-04-08-alt2-2-part4, STR-04-08-alt2-2-part5, STR-04-08-alt2-2-part6, STR-04-08-opt1, STR-04-08-opt2, STR-04-08-opt2-part2, STR-04-08-opt2-part3, STR-04-08-break1, STR-04-08-break2
  * Participants 3: M=Model -> strHostModel; P=Plugin -> qbrain.plugins.pluginPs; C=Controller -> qbrain.server.api.
  * Source 2 messages, 6 replies, 2 self messages, 3 notes, 6 blocks with 9 branches; views carry 30 steps of 30 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 9 branch views for 9 branches.
* F67, line 1817, sequence diagram (section Nested sequence: ReceiveCloseAnswer (close answer step of the close sequence))
  * Views (9): ReceiveCloseAnswer, ReceiveCloseAnswer-alt1-1, ReceiveCloseAnswer-alt1-2, ReceiveCloseAnswer-alt1-3, ReceiveCloseAnswer-alt1-3-part2, ReceiveCloseAnswer-alt1-4, ReceiveCloseAnswer-alt1-5, ReceiveCloseAnswer-alt1-6, ReceiveCloseAnswer-alt1-6-part2
  * Participants 2: M=Model -> strHostModel; P=Plugin -> qbrain.plugins.pluginPs.
  * Source 0 messages, 6 replies, 0 self messages, 0 notes, 1 blocks with 6 branches; views carry 15 steps of 15 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 6 branch views for 6 branches.
* F68, line 1841, sequence diagram (section Sequence: submit CompleteSessionTurnCommand and process each response)
  * Views (11): STR-04-09, STR-04-09-part2, STR-04-09-part3, STR-04-09-alt1-1, STR-04-09-alt1-1-part2, STR-04-09-alt1-2, STR-04-09-alt1-3, STR-04-09-alt1-3-part2, STR-04-09-alt1-4, STR-04-09-alt1-5, STR-04-09-alt1-6
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 7 replies, 0 self messages, 3 notes, 1 blocks with 6 branches; views carry 21 steps of 21 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 6 branch views for 6 branches.
* F69, line 1874, sequence diagram (section Sequence: submit FailSessionTurnCommand and process each response)
  * Views (12): STR-04-10, STR-04-10-part2, STR-04-10-part3, STR-04-10-alt1-1, STR-04-10-alt1-1-part2, STR-04-10-alt1-2, STR-04-10-alt1-2-part2, STR-04-10-alt1-3, STR-04-10-alt1-3-part2, STR-04-10-alt1-4, STR-04-10-alt1-5, STR-04-10-alt1-6
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 7 replies, 0 self messages, 3 notes, 1 blocks with 6 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 6 branch views for 6 branches.
* F70, line 1907, sequence diagram (section Sequence: submit OpenSessionLogCommand and process each response)
  * Views (10): STR-04-11, STR-04-11-part2, STR-04-11-part3, STR-04-11-part4, STR-04-11-alt1-1, STR-04-11-alt1-2, STR-04-11-alt1-2-part2, STR-04-11-alt1-3, STR-04-11-alt1-4, STR-04-11-alt1-4-part2
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 3 replies, 0 self messages, 5 notes, 1 blocks with 4 branches; views carry 17 steps of 17 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 4 branch views for 4 branches.
* F71, line 1937, sequence diagram (section Sequence: submit AppendTurnDialogCommand from a plugin verb and process each response)
  * Views (20): STR-04-12, STR-04-12-part2, STR-04-12-alt1-1, STR-04-12-alt1-2, STR-04-12-alt1-2-part2, STR-04-12-alt1-3, STR-04-12-alt1-3-part2, STR-04-12-alt1-3-part3, STR-04-12-alt1-3-part4, STR-04-12-alt1-3-part5, STR-04-12-alt1-3-part6, STR-04-12-alt1-3-part7, STR-04-12-alt1-3-part8, STR-04-12-alt2-1, STR-04-12-alt2-1-part2, STR-04-12-alt2-2, STR-04-12-alt2-2-part2, STR-04-12-alt2-3, STR-04-12-alt2-3-part2, STR-04-12-alt2-4
  * Participants 5: M=Model -> strHostModel; P=Plugin -> qbrain.plugins.pluginPs; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 9 replies, 0 self messages, 3 notes, 2 blocks with 7 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 4 continuation steps of long self steps); 7 branch views for 7 branches.
* F72, line 1975, sequence diagram (section Sequence: submit SetTurnTitleCommand from a plugin verb and process each response)
  * Views (20): STR-04-13, STR-04-13-part2, STR-04-13-alt1-1, STR-04-13-alt1-2, STR-04-13-alt1-2-part2, STR-04-13-alt1-3, STR-04-13-alt1-3-part2, STR-04-13-alt1-3-part3, STR-04-13-alt1-3-part4, STR-04-13-alt1-3-part5, STR-04-13-alt1-3-part6, STR-04-13-alt1-3-part7, STR-04-13-alt1-3-part8, STR-04-13-alt2-1, STR-04-13-alt2-1-part2, STR-04-13-alt2-2, STR-04-13-alt2-2-part2, STR-04-13-alt2-3, STR-04-13-alt2-3-part2, STR-04-13-alt2-4
  * Participants 5: M=Model -> strHostModel; P=Plugin -> qbrain.plugins.pluginPs; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 9 replies, 0 self messages, 3 notes, 2 blocks with 7 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 4 continuation steps of long self steps); 7 branch views for 7 branches.
* F73, line 2013, sequence diagram (section Sequence: submit SetSessionTitleCommand and process each response)
  * Views (7): STR-04-14, STR-04-14-part2, STR-04-14-part3, STR-04-14-part4, STR-04-14-alt1-1, STR-04-14-alt1-2, STR-04-14-alt1-3
  * Participants 4: W=Writer -> qbrain.libs.client; C=Controller -> qbrain.server.api; H=Handler -> qbrain.server.strSessionLogHandlers; D=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 3 branches; views carry 12 steps of 12 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 3 branch views for 3 branches.
* F74, line 2038, sequence diagram (section Sequence: replay the failsafe (process DrainFailsafe) and process each response)
  * Views (4): STR-04-15, STR-04-15-part2, STR-04-15-loop1, STR-04-15-loop1-part2
  * Participants 2: T=Trigger -> qbrain.plugins.pluginHosts; P=Plugin -> qbrain.plugins.pluginPs.
  * Source 1 messages, 1 replies, 0 self messages, 1 notes, 1 blocks with 1 branches; views carry 6 steps of 6 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 1 branch views for 1 branches.
* F75, line 2054, sequence diagram (section Nested sequence: ReplayOneFailsafeRecord (loop body of the DrainFailsafe sequence))
  * Views (22): ReplayOneFailsafeRecord, ReplayOneFailsafeRecord-alt1-1, ReplayOneFailsafeRecord-alt1-1-part2, ReplayOneFailsafeRecord-alt1-2, ReplayOneFailsafeRecord-alt1-2-part2, ReplayOneFailsafeRecord-alt1-3, ReplayOneFailsafeRecord-alt1-3-part2, ReplayOneFailsafeRecord-alt1-4, ReplayOneFailsafeRecord-alt1-4-part2, ReplayOneFailsafeRecord-alt1-5, ReplayOneFailsafeRecord-alt1-5-part2, ReplayOneFailsafeRecord-alt1-6, ReplayOneFailsafeRecord-alt1-6-part2, ReplayOneFailsafeRecord-alt1-6-part3, ReplayOneFailsafeRecord-alt2-1, ReplayOneFailsafeRecord-alt2-1-part2, ReplayOneFailsafeRecord-alt2-2, ReplayOneFailsafeRecord-alt2-2-part2, ReplayOneFailsafeRecord-alt2-3, ReplayOneFailsafeRecord-alt2-3-part2, ReplayOneFailsafeRecord-alt2-4, ReplayOneFailsafeRecord-alt2-4-part2
  * Participants 1: P=Plugin -> qbrain.plugins.pluginPs.
  * Source 0 messages, 0 replies, 0 self messages, 10 notes, 2 blocks with 10 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 10 branch views for 10 branches.
* F76, line 2086, sequence diagram (section Nested sequence: SendDrainedRecord (send step of ReplayOneFailsafeRecord))
  * Views (23): SendDrainedRecord, SendDrainedRecord-alt1-1, SendDrainedRecord-alt1-1-part2, SendDrainedRecord-alt1-1-part3, SendDrainedRecord-alt1-2, SendDrainedRecord-alt1-2-part2, SendDrainedRecord-alt1-3, SendDrainedRecord-alt1-3-part2, SendDrainedRecord-alt1-3-part3, SendDrainedRecord-alt2-1, SendDrainedRecord-alt2-1-part2, SendDrainedRecord-alt2-2, SendDrainedRecord-alt2-2-part2, SendDrainedRecord-alt2-2-part3, SendDrainedRecord-alt2-2-part4, SendDrainedRecord-opt1, SendDrainedRecord-opt1-part2, SendDrainedRecord-alt2-3, SendDrainedRecord-alt2-3-part2, SendDrainedRecord-alt2-4, SendDrainedRecord-alt2-4-part2, SendDrainedRecord-alt2-4-part3, SendDrainedRecord-alt2-4-part4
  * Participants 3: P=Plugin -> qbrain.plugins.pluginPs; R=REPL -> qbrain.repl; C=Controller -> qbrain.server.api.
  * Source 4 messages, 4 replies, 3 self messages, 4 notes, 3 blocks with 8 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 8 branch views for 8 branches.
* F77, line 2134, state diagram (section 5.1 Data model)
  * Views (5): STR-05-01, STR-05-01-part2, STR-05-01-part3, STR-05-01-part4, STR-05-01-part5
  * Source 7 states, 1 start or end pseudo states and 9 transitions; model 8 State elements and 9 relationships (4 substate links); views show 8 of 8 elements and 13 of 13 transitions and links.
* F78, line 2168, activity flowchart (section RecordSessionTurnReviewCommand: record a review [FR-MCP-SESSIONREVIEW-001, -002, -003, -004, -005, -006, -007, FR-SUPPORT-016])
  * Views (8): RecordSessionTurnReviewCommand, RecordSessionTurnReviewCommand-part2, RecordSessionTurnReviewCommand-part3, RecordSessionTurnReviewCommand-part4, RecordSessionTurnReviewCommand-part5, RecordSessionTurnReviewCommand-part6, RecordSessionTurnReviewCommand-part7, RecordSessionTurnReviewCommand-part8
  * Source 19 nodes and 20 edges; model 19 Activity elements (Start 1, Action 7, Decision 2, Error 8, End 1) and 20 relationships; views show 19 of 19 nodes and 20 of 20 edges.
* F79, line 2199, activity flowchart (section ResetSessionTurnReviewCommand: reset a review section [FR-SUPPORT-016, FR-MCP-SESSIONREVIEW-001])
  * Views (5): ResetSessionTurnReviewCommand, ResetSessionTurnReviewCommand-part2, ResetSessionTurnReviewCommand-part3, ResetSessionTurnReviewCommand-part4, ResetSessionTurnReviewCommand-part5
  * Source 13 nodes and 13 edges; model 13 Activity elements (Start 1, Action 5, Decision 2, Error 4, End 1) and 13 relationships; views show 13 of 13 nodes and 13 of 13 edges.
* F80, line 2223, activity flowchart (section GetSessionLogQuery: read one session with its turns and reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004])
  * Views (3): GetSessionLogQuery, GetSessionLogQuery-part2, GetSessionLogQuery-part3
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 2, End 1) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F81, line 2242, activity flowchart (section QuerySessionLogsQuery: query sessions and turns with their reviews [FR-MCP-SESSIONREVIEW-001, FR-MCP-SESSIONREVIEW-004])
  * Views (2): QuerySessionLogsQuery, QuerySessionLogsQuery-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 1, Error 1, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F82, line 2262, activity flowchart (section Policy RequestClassification: request classification and the Chore flip [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006])
  * Views (3): RequestClassification, RequestClassification-part2, RequestClassification-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 1, End 2) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F83, line 2281, activity flowchart (section Policy ReviewValidation: validate a review record and derive its status [FR-MCP-SESSIONREVIEW-003, -006, -007, FR-MCP-SESSIONCLASS-003])
  * Views (5): ReviewValidation, ReviewValidation-part2, ReviewValidation-part3, ReviewValidation-part4, ReviewValidation-part5
  * Source 12 nodes and 13 edges; model 12 Activity elements (Start 1, Action 1, Decision 4, Error 5, End 1) and 13 relationships; views show 12 of 12 nodes and 13 of 13 edges.
* F84, line 2306, activity flowchart (section Policy HvVerdictMapping: HV verdict mapping (server) [FR-MCP-SESSIONREVIEW-005])
  * Views (3): HvVerdictMapping, HvVerdictMapping-part2, HvVerdictMapping-part3
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 1, Decision 4, Error 2, End 3) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F85, line 2325, activity flowchart (section Policy CompletionPolicy: the completion rule [FR-MCP-SESSIONCLASS-002, FR-MCP-SESSIONCLASS-004])
  * Views (3): CompletionPolicy, CompletionPolicy-part2, CompletionPolicy-part3
  * Source 10 nodes and 14 edges; model 10 Activity elements (Start 1, Action 3, Decision 5, Error 0, End 1) and 14 relationships; views show 10 of 10 nodes and 14 of 14 edges.
* F86, line 2353, activity flowchart (section Policy CompletionGate: server enforcement of the completion rule [FR-MCP-SESSIONCLASS-002, -003, -004])
  * Views (2): CompletionGate, CompletionGate-part2
  * Source 6 nodes and 6 edges; model 6 Activity elements (Start 1, Action 1, Decision 2, Error 0, End 2) and 6 relationships; views show 6 of 6 nodes and 6 of 6 edges.
* F87, line 2373, activity flowchart (section Process ConvertHvReceiptToReview: convert a hostile-validation receipt into a review write (plugin), then policy HvVerdictMapping [FR-MCP-SESSIONREVIEW-005])
  * Views (2): ConvertHvReceiptToReview, ConvertHvReceiptToReview-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 1, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F88, line 2391, activity flowchart (section Process ClearSession: clear a session (clear-session coordinator) [TR-MCP-CLEARSESSION-001 under FR-MCP-CLEARSESSION-001])
  * Views (1): ClearSession
  * Source 4 nodes and 3 edges; model 4 Activity elements (Start 1, Action 2, Decision 0, Error 0, End 1) and 3 relationships; views show 4 of 4 nodes and 3 of 3 edges.
* F89, line 2402, activity flowchart (section Nested activity: ClearOneOpenTurn (loop body of ClearSession))
  * Views (4): ClearOneOpenTurn, ClearOneOpenTurn-part2, ClearOneOpenTurn-part3, ClearOneOpenTurn-part4
  * Source 10 nodes and 13 edges; model 10 Activity elements (Start 1, Action 5, Decision 3, Error 0, End 1) and 13 relationships; views show 10 of 10 nodes and 13 of 13 edges.
* F90, line 2423, activity flowchart (section Nested activity: ReportCloseReceipt (receipt step of ClearOneOpenTurn))
  * Views (3): ReportCloseReceipt, ReportCloseReceipt-part2, ReportCloseReceipt-part3
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 0, Decision 1, Error 2, End 4) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F91, line 2439, activity flowchart (section Process AdvisoryCompletionPrecheck: advisory completion pre-check (plugins, REPL, McpAgent) [FR-MCP-SESSIONCLASS-004])
  * Views (1): AdvisoryCompletionPrecheck
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 3, Decision 0, Error 0, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F92, line 2453, activity flowchart (section Process PreserveReviewScoreLexemes: lossless score handling in the REPL [FR-MCP-SESSIONREVIEW-004])
  * Views (2): PreserveReviewScoreLexemes, PreserveReviewScoreLexemes-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 2) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F93, line 2468, sequence diagram (section Sequence: submit RecordSessionTurnReviewCommand and process each response)
  * Views (13): STR-05-02, STR-05-02-part2, STR-05-02-alt1-1, STR-05-02-alt1-2, STR-05-02-alt1-3, STR-05-02-alt1-4, STR-05-02-alt1-5, STR-05-02-alt1-5-part2, STR-05-02-alt2-1, STR-05-02-alt2-1-part2, STR-05-02-alt2-2, STR-05-02-alt2-2-part2, STR-05-02-alt2-2-part3
  * Participants 4: A=Model or coordinator -> strHostModel; P=Plugin recordReview -> qbrain.plugins.pluginPs; R=qbrain-ai-repl -> qbrain.repl; S=Controller or MCP tool -> qbrain.server.api.
  * Source 3 messages, 6 replies, 0 self messages, 2 notes, 2 blocks with 7 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 7 branch views for 7 branches.
* F94, line 2501, sequence diagram (section Nested sequence: ServeReviewWrite (server step of the recordReview sequence))
  * Views (19): ServeReviewWrite, ServeReviewWrite-alt1-1, ServeReviewWrite-alt1-1-part2, ServeReviewWrite-alt1-1-part3, ServeReviewWrite-alt1-2, ServeReviewWrite-alt1-2-part2, ServeReviewWrite-alt2-1, ServeReviewWrite-alt2-1-part2, ServeReviewWrite-alt2-2, ServeReviewWrite-alt2-2-part2, ServeReviewWrite-alt2-2-part3, ServeReviewWrite-alt2-3, ServeReviewWrite-alt2-3-part2, ServeReviewWrite-alt2-4, ServeReviewWrite-alt2-4-part2, ServeReviewWrite-alt2-5, ServeReviewWrite-alt2-5-part2, ServeReviewWrite-alt2-6, ServeReviewWrite-alt2-6-part2
  * Participants 4: A=Model or coordinator -> strHostModel; P=Plugin through qbrain-ai-repl -> qbrain.plugins.pluginPs; S=Controller or MCP tool -> qbrain.server.api; L=IDispatcher and handler -> qbrain.server.strSessionLogHandlers.
  * Source 2 messages, 15 replies, 0 self messages, 0 notes, 2 blocks with 8 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 8 branch views for 8 branches.
* F95, line 2539, sequence diagram (section Sequence: submit ResetSessionTurnReviewCommand and process each response)
  * Views (17): STR-05-03, STR-05-03-part2, STR-05-03-part3, STR-05-03-part4, STR-05-03-part5, STR-05-03-part6, STR-05-03-alt1-1, STR-05-03-alt1-2, STR-05-03-alt1-2-part2, STR-05-03-alt2-1, STR-05-03-alt2-2, STR-05-03-alt2-3, STR-05-03-alt2-4, STR-05-03-alt2-5, STR-05-03-alt2-5-part2, STR-05-03-alt2-5-part3, STR-05-03-alt2-6
  * Participants 3: C=Client -> qbrain.libs.client; S=SessionLogController -> qbrain.server.api; X=IDispatcher and ResetSessionTurnReviewCommandHandler -> qbrain.server.strSessionLogHandlers.
  * Source 2 messages, 8 replies, 1 self messages, 1 notes, 2 blocks with 8 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 6 continuation steps of long self steps); 8 branch views for 8 branches.
* F96, line 2573, sequence diagram (section Sequence: submit a classification through SubmitSessionLogCommand from the plugin updateTurn verb (policy RequestClassification) and process each response)
  * Views (22): STR-05-04, STR-05-04-part2, STR-05-04-part3, STR-05-04-part4, STR-05-04-alt1-1, STR-05-04-alt1-2, STR-05-04-alt1-2-part2, STR-05-04-alt1-2-part3, STR-05-04-alt1-2-part4, STR-05-04-alt2-1, STR-05-04-alt2-1-part2, STR-05-04-alt2-2, STR-05-04-alt2-2-part2, STR-05-04-alt2-2-part3, STR-05-04-alt3-1, STR-05-04-alt3-1-part2, STR-05-04-alt3-1-part3, STR-05-04-alt3-1-part4, STR-05-04-alt3-2, STR-05-04-alt3-2-part2, STR-05-04-alt3-3, STR-05-04-alt3-3-part2
  * Participants 5: M=Model -> strHostModel; P=Plugin updateTurn -> qbrain.plugins.pluginPs; R=qbrain-ai-repl -> qbrain.repl; S=SessionLogController -> qbrain.server.api; X=IDispatcher and SubmitSessionLogCommandHandler -> qbrain.server.strSessionLogHandlers.
  * Source 4 messages, 11 replies, 0 self messages, 3 notes, 3 blocks with 7 branches; views carry 32 steps of 32 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 4 continuation steps of long self steps); 7 branch views for 7 branches.
* F97, line 2615, sequence diagram (section Sequence: submit GetSessionLogQuery and process the response)
  * Views (8): STR-05-05, STR-05-05-part2, STR-05-05-alt1-1, STR-05-05-alt1-2, STR-05-05-alt1-3, STR-05-05-alt1-4, STR-05-05-alt1-4-part2, STR-05-05-alt1-4-part3
  * Participants 3: C=REST caller (the typed client, REPL, MCP, and plugins read through QuerySessionLogsQuery) -> qbrain.libs.client; S=SessionLogController -> qbrain.server.api; X=IDispatcher and GetSessionLogQueryHandler -> qbrain.server.strSessionLogHandlers.
  * Source 2 messages, 5 replies, 2 self messages, 0 notes, 1 blocks with 4 branches; views carry 15 steps of 15 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 4 branch views for 4 branches.
* F98, line 2639, sequence diagram (section Sequence: submit QuerySessionLogsQuery and process the response)
  * Views (9): STR-05-06, STR-05-06-part2, STR-05-06-alt1-1, STR-05-06-alt1-2, STR-05-06-alt1-3, STR-05-06-alt1-3-part2, STR-05-06-alt1-3-part3, STR-05-06-alt1-3-part4, STR-05-06-alt1-3-part5
  * Participants 3: C=Client, REPL, MCP sessionlog_query, or plugin -> qbrain.libs.client; S=SessionLogController or MCP tool -> qbrain.server.api; X=IDispatcher and QuerySessionLogsQueryHandler -> qbrain.server.strSessionLogHandlers.
  * Source 2 messages, 4 replies, 2 self messages, 0 notes, 1 blocks with 3 branches; views carry 15 steps of 15 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 3 branch views for 3 branches.
* F99, line 2860, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (3): STR-06-01, STR-06-01-part2, STR-06-01-part3
  * Source 7 entities and 6 relationships (0 reuse an as-built EF relationship); views show 7 of 7 entities and 6 of 6 relationships.
  * Entities: SessionLogTurn -> entSessionLogTurn; AgentContextPackage -> strEntAgentContextPackage; AgentContextPackageItem -> strEntAgentContextPackageItem; AgentContextPackageFocusRef -> strEntAgentContextPackageFocusRef; AgentContextPackageInput -> strEntAgentContextPackageInput; AgentContextPackageInputLink -> strEntAgentContextPackageInputLink; AgentContextRelationship -> strEntAgentContextRelationship.
* F100, line 2872, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (2): STR-06-02, STR-06-02-part2
  * Source 4 entities and 3 relationships (0 reuse an as-built EF relationship); views show 4 of 4 entities and 3 of 3 relationships.
  * Entities: AgentContextPackage -> strEntAgentContextPackage; AgentContextOmission -> strEntAgentContextOmission; AgentContextFinding -> strEntAgentContextFinding; AgentContextRelationship -> strEntAgentContextRelationship.
* F101, line 2881, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (2): STR-06-03, STR-06-03-part2
  * Source 5 entities and 4 relationships (0 reuse an as-built EF relationship); views show 5 of 5 entities and 4 of 4 relationships.
  * Entities: SessionLogTurn -> entSessionLogTurn; AgentResponse -> strEntAgentResponse; AgentContextPackage -> strEntAgentContextPackage; AgentResponseClaim -> strEntAgentResponseClaim; AgentResponseEvidence -> strEntAgentResponseEvidence.
* F102, line 2891, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (2): STR-06-04, STR-06-04-part2
  * Source 5 entities and 4 relationships (0 reuse an as-built EF relationship); views show 5 of 5 entities and 4 of 4 relationships.
  * Entities: AgentResponse -> strEntAgentResponse; AgentResponseGrade -> strEntAgentResponseGrade; AgentContextHvRequest -> strEntAgentContextHvRequest; SessionLogTurn -> entSessionLogTurn; AgentResponseGradeFinding -> strEntAgentResponseGradeFinding.
* F103, line 2901, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (2): STR-06-05, STR-06-05-part2
  * Source 4 entities and 3 relationships (0 reuse an as-built EF relationship); views show 4 of 4 entities and 3 of 3 relationships.
  * Entities: SessionLog -> entSessionLog; AgentContextHvRequest -> strEntAgentContextHvRequest; AgentContextHvRequestItem -> strEntAgentContextHvRequestItem; AgentResponse -> strEntAgentResponse.
* F104, line 2910, ER diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (2): STR-06-06, STR-06-06-part2
  * Source 4 entities and 3 relationships (0 reuse an as-built EF relationship); views show 4 of 4 entities and 3 of 3 relationships.
  * Entities: AgentContextRelationship -> strEntAgentContextRelationship; AgentContextPackage -> strEntAgentContextPackage; AgentResponse -> strEntAgentResponse; AgentContextProjection -> strEntAgentContextProjection.
* F105, line 2919, state diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (9): STR-06-07, STR-06-07-part2, STR-06-07-part3, STR-06-07-part4, STR-06-07-part5, STR-06-07-part6, STR-06-07-part7, STR-06-07-part8, STR-06-07-part9
  * Source 6 states, 2 start or end pseudo states and 19 transitions; model 8 State elements and 19 relationships (0 substate links); views show 8 of 8 elements and 19 of 19 transitions and links.
* F106, line 2946, state diagram (section 6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Views (6): STR-06-08, STR-06-08-part2, STR-06-08-part3, STR-06-08-part4, STR-06-08-part5, STR-06-08-part6
  * Source 6 states, 1 start or end pseudo states and 15 transitions; model 7 State elements and 15 relationships (0 substate links); views show 7 of 7 elements and 15 of 15 transitions and links.
* F107, line 3060, activity flowchart (section GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009])
  * Views (2): GetAgentContextStatusQuery, GetAgentContextStatusQuery-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 4, Decision 0, Error 0, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F108, line 3083, activity flowchart (section BuildAgentContextPackageCommand: build a context package [FR-MCP-AGENTCONTEXT-001, -002])
  * Views (6): BuildAgentContextPackageCommand, BuildAgentContextPackageCommand-part2, BuildAgentContextPackageCommand-part3, BuildAgentContextPackageCommand-part4, BuildAgentContextPackageCommand-part5, BuildAgentContextPackageCommand-part6
  * Source 13 nodes and 16 edges; model 13 Activity elements (Start 1, Action 4, Decision 2, Error 4, End 2) and 16 relationships; views show 13 of 13 nodes and 16 of 16 edges.
* F109, line 3107, activity flowchart (section Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite))
  * Views (4): ReadBuildInputs, ReadBuildInputs-part2, ReadBuildInputs-part3, ReadBuildInputs-part4
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 5, Decision 1, Error 4, End 1) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F110, line 3126, activity flowchart (section Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler))
  * Views (5): SavePackage, SavePackage-part2, SavePackage-part3, SavePackage-part4, SavePackage-part5
  * Source 13 nodes and 14 edges; model 13 Activity elements (Start 1, Action 5, Decision 1, Error 3, End 3) and 14 relationships; views show 13 of 13 nodes and 14 of 14 edges.
* F111, line 3155, activity flowchart (section Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery))
  * Views (3): FillBudgetWithCandidates, FillBudgetWithCandidates-part2, FillBudgetWithCandidates-part3
  * Source 9 nodes and 11 edges; model 9 Activity elements (Start 1, Action 4, Decision 3, Error 0, End 1) and 11 relationships; views show 9 of 9 nodes and 11 of 11 edges.
* F112, line 3176, activity flowchart (section GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001])
  * Views (2): GetAgentContextPackageQuery, GetAgentContextPackageQuery-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 1, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F113, line 3194, activity flowchart (section QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001])
  * Views (2): QueryAgentContextPackagesQuery, QueryAgentContextPackagesQuery-part2
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F114, line 3210, activity flowchart (section ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001])
  * Views (5): ReplayAgentContextPackageQuery, ReplayAgentContextPackageQuery-part2, ReplayAgentContextPackageQuery-part3, ReplayAgentContextPackageQuery-part4, ReplayAgentContextPackageQuery-part5
  * Source 13 nodes and 12 edges; model 13 Activity elements (Start 1, Action 3, Decision 3, Error 4, End 2) and 12 relationships; views show 13 of 13 nodes and 12 of 12 edges.
* F115, line 3238, activity flowchart (section SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003])
  * Views (6): SubmitAgentResponseCommand, SubmitAgentResponseCommand-part2, SubmitAgentResponseCommand-part3, SubmitAgentResponseCommand-part4, SubmitAgentResponseCommand-part5, SubmitAgentResponseCommand-part6
  * Source 15 nodes and 17 edges; model 15 Activity elements (Start 1, Action 7, Decision 1, Error 4, End 2) and 17 relationships; views show 15 of 15 nodes and 17 of 17 edges.
* F116, line 3268, activity flowchart (section Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite))
  * Views (4): ReadSubmissionInputs, ReadSubmissionInputs-part2, ReadSubmissionInputs-part3, ReadSubmissionInputs-part4
  * Source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 2, Decision 1, Error 4, End 1) and 9 relationships; views show 9 of 9 nodes and 9 of 9 edges.
* F117, line 3288, activity flowchart (section GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003])
  * Views (2): GetAgentResponseQuery, GetAgentResponseQuery-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 1, Error 1, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F118, line 3305, activity flowchart (section QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004])
  * Views (3): QueryAgentResponsesQuery, QueryAgentResponsesQuery-part2, QueryAgentResponsesQuery-part3
  * Source 9 nodes and 10 edges; model 9 Activity elements (Start 1, Action 4, Decision 2, Error 1, End 1) and 10 relationships; views show 9 of 9 nodes and 10 of 10 edges.
* F119, line 3331, activity flowchart (section RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004])
  * Views (6): RunDeterministicGradeCommand, RunDeterministicGradeCommand-part2, RunDeterministicGradeCommand-part3, RunDeterministicGradeCommand-part4, RunDeterministicGradeCommand-part5, RunDeterministicGradeCommand-part6
  * Source 15 nodes and 17 edges; model 15 Activity elements (Start 1, Action 8, Decision 2, Error 3, End 1) and 17 relationships; views show 15 of 15 nodes and 17 of 17 edges.
* F120, line 3359, activity flowchart (section Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand))
  * Views (3): CheckOneEvidenceItem, CheckOneEvidenceItem-part2, CheckOneEvidenceItem-part3
  * Source 9 nodes and 13 edges; model 9 Activity elements (Start 1, Action 4, Decision 3, Error 0, End 1) and 13 relationships; views show 9 of 9 nodes and 13 of 13 edges.
* F121, line 3388, activity flowchart (section RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004])
  * Views (5): RecordModelGradeCommand, RecordModelGradeCommand-part2, RecordModelGradeCommand-part3, RecordModelGradeCommand-part4, RecordModelGradeCommand-part5
  * Source 14 nodes and 16 edges; model 14 Activity elements (Start 1, Action 4, Decision 1, Error 6, End 2) and 16 relationships; views show 14 of 14 nodes and 16 of 16 edges.
* F122, line 3412, activity flowchart (section Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite))
  * Views (3): ReadModelGradeInputs, ReadModelGradeInputs-part2, ReadModelGradeInputs-part3
  * Source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 1, Error 3, End 1) and 8 relationships; views show 9 of 9 nodes and 8 of 8 edges.
* F123, line 3428, activity flowchart (section Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs))
  * Views (3): CheckGradeTarget, CheckGradeTarget-part2, CheckGradeTarget-part3
  * Source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 0, Decision 4, Error 6, End 1) and 11 relationships; views show 12 of 12 nodes and 11 of 11 edges.
* F124, line 3447, activity flowchart (section Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs))
  * Views (4): SaveModelGrade, SaveModelGrade-part2, SaveModelGrade-part3, SaveModelGrade-part4
  * Source 11 nodes and 11 edges; model 11 Activity elements (Start 1, Action 4, Decision 1, Error 2, End 3) and 11 relationships; views show 11 of 11 nodes and 11 of 11 edges.
* F125, line 3474, activity flowchart (section DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004])
  * Views (2): DecideAgentResponseTrustCommand, DecideAgentResponseTrustCommand-part2
  * Source 8 nodes and 8 edges; model 8 Activity elements (Start 1, Action 3, Decision 2, Error 0, End 2) and 8 relationships; views show 8 of 8 nodes and 8 of 8 edges.
* F126, line 3493, activity flowchart (section RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005])
  * Views (3): RejectAgentResponseCommand, RejectAgentResponseCommand-part2, RejectAgentResponseCommand-part3
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 2, Decision 2, Error 4, End 2) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F127, line 3520, activity flowchart (section SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005])
  * Views (5): SupersedeAgentResponseCommand, SupersedeAgentResponseCommand-part2, SupersedeAgentResponseCommand-part3, SupersedeAgentResponseCommand-part4, SupersedeAgentResponseCommand-part5
  * Source 13 nodes and 13 edges; model 13 Activity elements (Start 1, Action 3, Decision 2, Error 5, End 2) and 13 relationships; views show 13 of 13 nodes and 13 of 13 edges.
* F128, line 3551, activity flowchart (section EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005])
  * Views (2): EvaluateStalenessCommand, EvaluateStalenessCommand-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 2, End 1) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F129, line 3564, activity flowchart (section Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand))
  * Views (3): EvaluateOneResponseStaleness, EvaluateOneResponseStaleness-part2, EvaluateOneResponseStaleness-part3
  * Source 7 nodes and 8 edges; model 7 Activity elements (Start 1, Action 3, Decision 1, Error 1, End 1) and 8 relationships; views show 7 of 7 nodes and 8 of 8 edges.
* F130, line 3587, activity flowchart (section CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006])
  * Views (3): CreateAgentContextRelationshipCommand, CreateAgentContextRelationshipCommand-part2, CreateAgentContextRelationshipCommand-part3
  * Source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 3, Decision 1, Error 4, End 2) and 10 relationships; views show 11 of 11 nodes and 10 of 10 edges.
* F131, line 3611, activity flowchart (section DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006])
  * Views (2): DeleteAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand-part2
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 2, Decision 1, Error 2, End 1) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F132, line 3628, activity flowchart (section QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006])
  * Views (4): QueryAgentContextRelationshipsQuery, QueryAgentContextRelationshipsQuery-part2, QueryAgentContextRelationshipsQuery-part3, QueryAgentContextRelationshipsQuery-part4
  * Source 9 nodes and 11 edges; model 9 Activity elements (Start 1, Action 4, Decision 2, Error 1, End 1) and 11 relationships; views show 9 of 9 nodes and 11 of 11 edges.
* F133, line 3651, activity flowchart (section RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007])
  * Views (10): RetrieveAgentContextCandidatesQuery, RetrieveAgentContextCandidatesQuery-part2, RetrieveAgentContextCandidatesQuery-part3, RetrieveAgentContextCandidatesQuery-part4, RetrieveAgentContextCandidatesQuery-part5, RetrieveAgentContextCandidatesQuery-part6, RetrieveAgentContextCandidatesQuery-part7, RetrieveAgentContextCandidatesQuery-part8, RetrieveAgentContextCandidatesQuery-part9, RetrieveAgentContextCandidatesQuery-part10
  * Source 23 nodes and 29 edges; model 23 Activity elements (Start 1, Action 12, Decision 7, Error 2, End 1) and 29 relationships; views show 23 of 23 nodes and 29 of 29 edges.
* F134, line 3693, activity flowchart (section ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007])
  * Views (2): ProjectAgentResponsesCommand, ProjectAgentResponsesCommand-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 2, End 1) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F135, line 3706, activity flowchart (section Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand))
  * Views (5): ProjectOneResponse, ProjectOneResponse-part2, ProjectOneResponse-part3, ProjectOneResponse-part4, ProjectOneResponse-part5
  * Source 12 nodes and 17 edges; model 12 Activity elements (Start 1, Action 4, Decision 5, Error 1, End 1) and 17 relationships; views show 12 of 12 nodes and 17 of 17 edges.
* F136, line 3729, activity flowchart (section Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand))
  * Views (3): ReconcileProjectedDocuments, ReconcileProjectedDocuments-part2, ReconcileProjectedDocuments-part3
  * Source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 4, Decision 3, Error 1, End 2) and 12 relationships; views show 11 of 11 nodes and 12 of 12 edges.
* F137, line 3752, activity flowchart (section GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009])
  * Views (2): GetAgentContextPilotReportQuery, GetAgentContextPilotReportQuery-part2
  * Source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 4, Decision 1, Error 1, End 1) and 7 relationships; views show 8 of 8 nodes and 7 of 7 edges.
* F138, line 3776, activity flowchart (section CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004])
  * Views (4): CreateAgentContextHvRequestCommand, CreateAgentContextHvRequestCommand-part2, CreateAgentContextHvRequestCommand-part3, CreateAgentContextHvRequestCommand-part4
  * Source 12 nodes and 12 edges; model 12 Activity elements (Start 1, Action 4, Decision 1, Error 4, End 2) and 12 relationships; views show 12 of 12 nodes and 12 of 12 edges.
* F139, line 3801, activity flowchart (section Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand))
  * Views (3): SelectHvRequestItems, SelectHvRequestItems-part2, SelectHvRequestItems-part3
  * Source 8 nodes and 10 edges; model 8 Activity elements (Start 1, Action 2, Decision 2, Error 2, End 1) and 10 relationships; views show 8 of 8 nodes and 10 of 10 edges.
* F140, line 3821, activity flowchart (section GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004])
  * Views (2): GetAgentContextHvRequestQuery, GetAgentContextHvRequestQuery-part2
  * Source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1) and 4 relationships; views show 5 of 5 nodes and 4 of 4 edges.
* F141, line 3836, activity flowchart (section QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004])
  * Views (2): QueryAgentContextHvRequestsQuery, QueryAgentContextHvRequestsQuery-part2
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F142, line 3863, activity flowchart (section Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009])
  * Views (6): RunStatelessInvocation, RunStatelessInvocation-part2, RunStatelessInvocation-part3, RunStatelessInvocation-part4, RunStatelessInvocation-part5, RunStatelessInvocation-part6
  * Source 12 nodes and 14 edges; model 12 Activity elements (Start 1, Action 5, Decision 1, Error 2, End 3) and 14 relationships; views show 12 of 12 nodes and 14 of 14 edges.
* F143, line 3885, activity flowchart (section Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build))
  * Views (5): ResolveInvocationTurn, ResolveInvocationTurn-part2, ResolveInvocationTurn-part3, ResolveInvocationTurn-part4, ResolveInvocationTurn-part5
  * Source 15 nodes and 14 edges; model 15 Activity elements (Start 1, Action 2, Decision 3, Error 3, End 6) and 14 relationships; views show 15 of 15 nodes and 14 of 14 edges.
* F144, line 3907, activity flowchart (section Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation))
  * Views (2): FailInvocation, FailInvocation-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 3, End 0) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F145, line 3920, activity flowchart (section Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation))
  * Views (5): AnswerAndSubmit, AnswerAndSubmit-part2, AnswerAndSubmit-part3, AnswerAndSubmit-part4, AnswerAndSubmit-part5
  * Source 13 nodes and 13 edges; model 13 Activity elements (Start 1, Action 5, Decision 3, Error 2, End 2) and 13 relationships; views show 13 of 13 nodes and 13 of 13 edges.
* F146, line 3957, sequence diagram (section Sequence: RequestPipeline (every Phase C dispatch))
  * Views (13): RequestPipeline-Sequence, RequestPipeline-Sequence-part2, RequestPipeline-Sequence-part3, RequestPipeline-Sequence-part4, RequestPipeline-Sequence-alt1-1, RequestPipeline-Sequence-alt1-1-part2, RequestPipeline-Sequence-alt1-2, RequestPipeline-Sequence-alt2-1, RequestPipeline-Sequence-alt2-1-part2, RequestPipeline-Sequence-alt2-1-part3, RequestPipeline-Sequence-alt2-2, RequestPipeline-Sequence-alt2-2-part2, RequestPipeline-Sequence-alt2-2-part3
  * Participants 5: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Workspace=WorkspaceResolutionBehavior -> qbrain.server.strWorkspaceResolutionBehavior; ModeGate=ModeGateBehavior -> qbrain.server.strModeGateBehavior; Handler=Handler -> qbrain.server.strAgentContextHandlers.
  * Source 4 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 4 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 4 branch views for 4 branches.
* F147, line 3991, sequence diagram (section Sequence: submit GetAgentContextStatusQuery)
  * Views (10): STR-06-09, STR-06-09-alt1-1, STR-06-09-alt1-1-part2, STR-06-09-alt1-1-part3, STR-06-09-alt1-1-part4, STR-06-09-alt1-1-part5, STR-06-09-alt1-1-part6, STR-06-09-alt1-1-part7, STR-06-09-alt1-2, STR-06-09-alt1-2-part2
  * Participants 5: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; GraphRAG=GraphRAG -> qbrain.server.graphrag; Store=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 3 replies, 1 self messages, 4 notes, 1 blocks with 2 branches; views carry 16 steps of 16 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F148, line 4020, sequence diagram (section Sequence: submit BuildAgentContextPackageCommand)
  * Views (14): STR-06-10, STR-06-10-part2, STR-06-10-part3, STR-06-10-alt1-1, STR-06-10-alt1-2, STR-06-10-alt1-2-part2, STR-06-10-alt2-1, STR-06-10-alt2-2, STR-06-10-alt2-3, STR-06-10-alt2-4, STR-06-10-alt2-4-part2, STR-06-10-alt2-4-part3, STR-06-10-alt2-4-part4, STR-06-10-alt2-4-part5
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 4 replies, 1 self messages, 7 notes, 2 blocks with 6 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 6 branch views for 6 branches.
* F149, line 4056, sequence diagram (section Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction))
  * Views (12): ReadBuildInputs-Sequence, ReadBuildInputs-Sequence-part2, ReadBuildInputs-Sequence-alt1-1, ReadBuildInputs-Sequence-alt1-2, ReadBuildInputs-Sequence-alt1-3, ReadBuildInputs-Sequence-alt1-3-part2, ReadBuildInputs-Sequence-alt2-1, ReadBuildInputs-Sequence-alt2-2, ReadBuildInputs-Sequence-alt2-3, ReadBuildInputs-Sequence-alt2-3-part2, ReadBuildInputs-Sequence-alt2-3-part3, ReadBuildInputs-Sequence-alt2-3-part4
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 1 messages, 4 replies, 3 self messages, 4 notes, 2 blocks with 6 branches; views carry 20 steps of 20 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 6 branch views for 6 branches.
* F150, line 4089, sequence diagram (section Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence))
  * Views (15): SaveBuiltPackage, SaveBuiltPackage-part2, SaveBuiltPackage-alt1-1, SaveBuiltPackage-alt1-2, SaveBuiltPackage-alt1-3, SaveBuiltPackage-alt1-4, SaveBuiltPackage-alt1-4-part2, SaveBuiltPackage-alt1-4-part3, SaveBuiltPackage-alt1-5, SaveBuiltPackage-alt2-1, SaveBuiltPackage-alt2-1-part2, SaveBuiltPackage-alt2-2, SaveBuiltPackage-alt2-2-part2, SaveBuiltPackage-alt1-6, SaveBuiltPackage-alt1-6-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 8 branches; views carry 26 steps of 26 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 8 branch views for 8 branches.
* F151, line 4124, sequence diagram (section Sequence: submit GetAgentContextPackageQuery)
  * Views (9): STR-06-11, STR-06-11-part2, STR-06-11-part3, STR-06-11-part4, STR-06-11-part5, STR-06-11-alt1-1, STR-06-11-alt1-2, STR-06-11-alt1-2-part2, STR-06-11-alt1-2-part3
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 3 notes, 1 blocks with 2 branches; views carry 14 steps of 14 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 2 branch views for 2 branches.
* F152, line 4147, sequence diagram (section Sequence: submit QueryAgentContextPackagesQuery)
  * Views (7): STR-06-12, STR-06-12-part2, STR-06-12-part3, STR-06-12-part4, STR-06-12-alt1-1, STR-06-12-alt1-2, STR-06-12-alt1-2-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; views carry 12 steps of 12 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F153, line 4171, sequence diagram (section Sequence: submit ReplayAgentContextPackageQuery)
  * Views (16): STR-06-13, STR-06-13-part2, STR-06-13-part3, STR-06-13-part4, STR-06-13-part5, STR-06-13-alt1-1, STR-06-13-alt1-2, STR-06-13-alt1-3, STR-06-13-alt1-4, STR-06-13-alt1-5, STR-06-13-alt1-5-part2, STR-06-13-alt1-5-part3, STR-06-13-alt2-1, STR-06-13-alt2-1-part2, STR-06-13-alt2-2, STR-06-13-alt2-2-part2
  * Participants 5: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext; Assembler=PackageAssembler -> qbrain.server.strPackageAssembler.
  * Source 4 messages, 6 replies, 0 self messages, 5 notes, 2 blocks with 7 branches; views carry 25 steps of 25 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 7 branch views for 7 branches.
* F154, line 4209, sequence diagram (section Sequence: submit SubmitAgentResponseCommand)
  * Views (20): STR-06-14, STR-06-14-part2, STR-06-14-part3, STR-06-14-alt1-1, STR-06-14-alt1-1-part2, STR-06-14-alt1-2, STR-06-14-alt1-2-part2, STR-06-14-alt2-1, STR-06-14-alt2-2, STR-06-14-alt2-3, STR-06-14-alt2-3-part2, STR-06-14-alt2-3-part3, STR-06-14-alt2-3-part4, STR-06-14-alt2-3-part5, STR-06-14-alt3-1, STR-06-14-alt3-2, STR-06-14-alt3-3, STR-06-14-alt3-3-part2, STR-06-14-alt3-4, STR-06-14-alt3-4-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 5 messages, 7 replies, 1 self messages, 7 notes, 3 blocks with 9 branches; views carry 35 steps of 35 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 9 branch views for 9 branches.
* F155, line 4254, sequence diagram (section Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction))
  * Views (12): ReadSubmissionInputs-Sequence, ReadSubmissionInputs-Sequence-part2, ReadSubmissionInputs-Sequence-alt1-1, ReadSubmissionInputs-Sequence-alt1-2, ReadSubmissionInputs-Sequence-alt1-3, ReadSubmissionInputs-Sequence-alt1-3-part2, ReadSubmissionInputs-Sequence-alt1-4, ReadSubmissionInputs-Sequence-alt1-4-part2, ReadSubmissionInputs-Sequence-alt2-1, ReadSubmissionInputs-Sequence-alt2-2, ReadSubmissionInputs-Sequence-alt2-3, ReadSubmissionInputs-Sequence-alt2-3-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 1 messages, 5 replies, 1 self messages, 2 notes, 2 blocks with 7 branches; views carry 19 steps of 19 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 7 branch views for 7 branches.
* F156, line 4285, sequence diagram (section Sequence: submit GetAgentResponseQuery)
  * Views (10): STR-06-15, STR-06-15-part2, STR-06-15-part3, STR-06-15-part4, STR-06-15-part5, STR-06-15-alt1-1, STR-06-15-alt1-2, STR-06-15-alt1-2-part2, STR-06-15-alt1-2-part3, STR-06-15-alt1-2-part4
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 2 replies, 1 self messages, 3 notes, 1 blocks with 2 branches; views carry 13 steps of 13 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F157, line 4310, sequence diagram (section Sequence: submit QueryAgentResponsesQuery)
  * Views (9): STR-06-16, STR-06-16-part2, STR-06-16-part3, STR-06-16-part4, STR-06-16-alt1-1, STR-06-16-alt1-2, STR-06-16-alt1-2-part2, STR-06-16-alt1-2-part3, STR-06-16-alt1-2-part4
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 1 self messages, 3 notes, 1 blocks with 2 branches; views carry 14 steps of 14 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F158, line 4336, sequence diagram (section Sequence: submit RunDeterministicGradeCommand)
  * Views (23): STR-06-17, STR-06-17-part2, STR-06-17-part3, STR-06-17-part4, STR-06-17-alt1-1, STR-06-17-alt1-2, STR-06-17-alt1-3, STR-06-17-alt1-3-part2, STR-06-17-alt1-3-part3, STR-06-17-alt1-3-part4, STR-06-17-alt1-3-part5, STR-06-17-alt1-3-part6, STR-06-17-alt1-3-part7, STR-06-17-alt1-3-part8, STR-06-17-alt1-3-part9, STR-06-17-alt2-1, STR-06-17-alt2-1-part2, STR-06-17-alt2-2, STR-06-17-alt2-2-part2, STR-06-17-alt2-3, STR-06-17-alt2-3-part2, STR-06-17-alt1-4, STR-06-17-alt1-4-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 4 replies, 3 self messages, 8 notes, 2 blocks with 7 branches; views carry 30 steps of 30 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 7 branch views for 7 branches.
* F159, line 4377, sequence diagram (section Sequence: submit RecordModelGradeCommand from an HV review)
  * Views (10): STR-06-18, STR-06-18-part2, STR-06-18-part3, STR-06-18-part4, STR-06-18-part5, STR-06-18-part6, STR-06-18-part7, STR-06-18-part8, STR-06-18-loop1, STR-06-18-loop1-part2
  * Participants 4: Agent=Agent -> agent; Runner=Runner -> qbrain.devTools.evidenceScripts; Reviewer=Reviewer -> llm; Controller=Controller -> qbrain.server.api.
  * Source 4 messages, 3 replies, 0 self messages, 5 notes, 1 blocks with 1 branches; views carry 17 steps of 17 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 1 branch views for 1 branches.
* F160, line 4404, sequence diagram (section Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence))
  * Views (27): SubmitOneModelGrade, SubmitOneModelGrade-alt1-1, SubmitOneModelGrade-alt1-2, SubmitOneModelGrade-alt1-2-part2, SubmitOneModelGrade-alt1-2-part3, SubmitOneModelGrade-alt2-1, SubmitOneModelGrade-alt2-1-part2, SubmitOneModelGrade-alt2-2, SubmitOneModelGrade-alt2-3, SubmitOneModelGrade-alt2-4, SubmitOneModelGrade-alt2-4-part2, SubmitOneModelGrade-alt2-4-part3, SubmitOneModelGrade-alt2-4-part4, SubmitOneModelGrade-alt2-4-part5, SubmitOneModelGrade-alt3-1, SubmitOneModelGrade-alt3-2, SubmitOneModelGrade-alt3-3, SubmitOneModelGrade-alt3-3-part2, SubmitOneModelGrade-alt3-3-part3, SubmitOneModelGrade-alt4-1, SubmitOneModelGrade-alt4-1-part2, SubmitOneModelGrade-alt4-1-part3, SubmitOneModelGrade-alt4-2, SubmitOneModelGrade-alt4-2-part2, SubmitOneModelGrade-alt4-3, SubmitOneModelGrade-alt4-3-part2, SubmitOneModelGrade-alt3-4
  * Participants 4: Reviewer=Reviewer -> llm; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 8 replies, 1 self messages, 7 notes, 4 blocks with 13 branches; views carry 41 steps of 41 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 4 continuation steps of long self steps); 13 branch views for 13 branches.
* F161, line 4454, sequence diagram (section Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction))
  * Views (20): ReadModelGradeInputs-Sequence, ReadModelGradeInputs-Sequence-part2, ReadModelGradeInputs-Sequence-alt1-1, ReadModelGradeInputs-Sequence-alt1-2, ReadModelGradeInputs-Sequence-alt1-3, ReadModelGradeInputs-Sequence-alt1-3-part2, ReadModelGradeInputs-Sequence-alt2-1, ReadModelGradeInputs-Sequence-alt2-2, ReadModelGradeInputs-Sequence-alt2-3, ReadModelGradeInputs-Sequence-alt2-4, ReadModelGradeInputs-Sequence-alt2-5, ReadModelGradeInputs-Sequence-alt2-6, ReadModelGradeInputs-Sequence-alt2-7, ReadModelGradeInputs-Sequence-alt2-7-part2, ReadModelGradeInputs-Sequence-alt3-1, ReadModelGradeInputs-Sequence-alt3-2, ReadModelGradeInputs-Sequence-alt3-2-part2, ReadModelGradeInputs-Sequence-alt4-1, ReadModelGradeInputs-Sequence-alt4-2, ReadModelGradeInputs-Sequence-alt4-2-part2
  * Participants 4: Reviewer=Reviewer -> llm; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 2 messages, 10 replies, 2 self messages, 5 notes, 4 blocks with 14 branches; views carry 37 steps of 37 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 14 branch views for 14 branches.
* F162, line 4504, sequence diagram (section Sequence: submit RejectAgentResponseCommand)
  * Views (12): STR-06-19, STR-06-19-part2, STR-06-19-part3, STR-06-19-alt1-1, STR-06-19-alt1-2, STR-06-19-alt1-2-part2, STR-06-19-alt1-2-part3, STR-06-19-alt2-1, STR-06-19-alt2-2, STR-06-19-alt2-3, STR-06-19-alt2-4, STR-06-19-alt2-5
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 7 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 7 branch views for 7 branches.
* F163, line 4539, sequence diagram (section Sequence: submit SupersedeAgentResponseCommand)
  * Views (20): STR-06-20, STR-06-20-part2, STR-06-20-part3, STR-06-20-alt1-1, STR-06-20-alt1-2, STR-06-20-alt1-2-part2, STR-06-20-alt1-2-part3, STR-06-20-alt1-2-part4, STR-06-20-alt2-1, STR-06-20-alt2-2, STR-06-20-alt2-3, STR-06-20-alt2-3-part2, STR-06-20-alt3-1, STR-06-20-alt3-2, STR-06-20-alt3-2-part2, STR-06-20-alt3-3, STR-06-20-alt3-3-part2, STR-06-20-alt4-1, STR-06-20-alt4-2, STR-06-20-alt2-4
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 5 messages, 8 replies, 0 self messages, 6 notes, 4 blocks with 11 branches; views carry 36 steps of 36 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 11 branch views for 11 branches.
* F164, line 4586, sequence diagram (section Sequence: submit EvaluateStalenessCommand)
  * Views (16): STR-06-21, STR-06-21-part2, STR-06-21-part3, STR-06-21-part4, STR-06-21-alt1-1, STR-06-21-alt1-2, STR-06-21-alt1-2-part2, STR-06-21-alt1-2-part3, STR-06-21-loop1, STR-06-21-loop1-part2, STR-06-21-loop1-part3, STR-06-21-alt2-1, STR-06-21-alt2-1-part2, STR-06-21-alt2-1-part3, STR-06-21-alt2-1-part4, STR-06-21-alt2-2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 5 notes, 3 blocks with 5 branches; views carry 22 steps of 22 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 5 branch views for 5 branches.
* F165, line 4618, sequence diagram (section Sequence: submit CreateAgentContextRelationshipCommand)
  * Views (14): STR-06-22, STR-06-22-part2, STR-06-22-part3, STR-06-22-alt1-1, STR-06-22-alt1-2, STR-06-22-alt1-2-part2, STR-06-22-alt2-1, STR-06-22-alt2-2, STR-06-22-alt2-2-part2, STR-06-22-alt2-2-part3, STR-06-22-alt3-1, STR-06-22-alt3-2, STR-06-22-alt3-3, STR-06-22-alt3-4
  * Participants 5: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext; Resolver=SourceResolver -> qbrain.server.strSharedComponents.
  * Source 4 messages, 6 replies, 0 self messages, 3 notes, 3 blocks with 8 branches; views carry 24 steps of 24 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 8 branch views for 8 branches.
* F166, line 4656, sequence diagram (section Sequence: submit DeleteAgentContextRelationshipCommand)
  * Views (8): STR-06-23, STR-06-23-part2, STR-06-23-part3, STR-06-23-part4, STR-06-23-alt1-1, STR-06-23-alt1-2, STR-06-23-alt1-3, STR-06-23-alt1-3-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 4 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 3 branches; views carry 13 steps of 13 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 3 branch views for 3 branches.
* F167, line 4680, sequence diagram (section Sequence: submit QueryAgentContextRelationshipsQuery)
  * Views (8): STR-06-24, STR-06-24-part2, STR-06-24-part3, STR-06-24-part4, STR-06-24-alt1-1, STR-06-24-alt1-1-part2, STR-06-24-alt1-2, STR-06-24-alt1-2-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; views carry 13 steps of 13 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 2 branch views for 2 branches.
* F168, line 4704, sequence diagram (section Sequence: submit RetrieveAgentContextCandidatesQuery)
  * Views (17): STR-06-25, STR-06-25-part2, STR-06-25-part3, STR-06-25-part4, STR-06-25-alt1-1, STR-06-25-alt1-1-part2, STR-06-25-alt1-2, STR-06-25-alt1-2-part2, STR-06-25-opt1, STR-06-25-alt2-1, STR-06-25-alt2-2, STR-06-25-alt2-3, STR-06-25-alt2-3-part2, STR-06-25-alt2-3-part3, STR-06-25-alt2-3-part4, STR-06-25-alt2-3-part5, STR-06-25-alt2-3-part6
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 4 replies, 2 self messages, 3 notes, 3 blocks with 6 branches; views carry 24 steps of 24 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 6 branch views for 6 branches.
* F169, line 4738, sequence diagram (section Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence))
  * Views (10): ChooseRetrievalPath, ChooseRetrievalPath-part2, ChooseRetrievalPath-part3, ChooseRetrievalPath-opt1, ChooseRetrievalPath-alt1-1, ChooseRetrievalPath-alt2-1, ChooseRetrievalPath-alt2-2, ChooseRetrievalPath-alt2-2-part2, ChooseRetrievalPath-alt1-2, ChooseRetrievalPath-alt1-2-part2
  * Participants 3: Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext; GraphRAG=GraphRAG -> qbrain.server.graphrag.
  * Source 4 messages, 5 replies, 0 self messages, 1 notes, 3 blocks with 5 branches; views carry 20 steps of 20 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 5 branch views for 5 branches.
* F170, line 4768, sequence diagram (section Sequence: submit ProjectAgentResponsesCommand)
  * Views (17): STR-06-26, STR-06-26-part2, STR-06-26-part3, STR-06-26-part4, STR-06-26-alt1-1, STR-06-26-alt1-2, STR-06-26-alt1-2-part2, STR-06-26-alt1-2-part3, STR-06-26-alt1-2-part4, STR-06-26-alt1-2-part5, STR-06-26-alt1-2-part6, STR-06-26-alt1-2-part7, STR-06-26-alt1-2-part8, STR-06-26-loop1, STR-06-26-loop1-part2, STR-06-26-alt2-1, STR-06-26-alt2-2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 5 notes, 3 blocks with 5 branches; views carry 24 steps of 24 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 5 continuation steps of long self steps); 5 branch views for 5 branches.
* F171, line 4800, sequence diagram (section Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence))
  * Views (36): ProjectOneResponse-Sequence, ProjectOneResponse-Sequence-part2, ProjectOneResponse-Sequence-part3, ProjectOneResponse-Sequence-alt1-1, ProjectOneResponse-Sequence-alt1-1-part2, ProjectOneResponse-Sequence-alt2-1, ProjectOneResponse-Sequence-alt2-1-part2, ProjectOneResponse-Sequence-alt2-1-part3, ProjectOneResponse-Sequence-alt3-1, ProjectOneResponse-Sequence-alt3-1-part2, ProjectOneResponse-Sequence-alt3-1-part3, ProjectOneResponse-Sequence-alt3-1-part4, ProjectOneResponse-Sequence-alt3-2, ProjectOneResponse-Sequence-alt3-2-part2, ProjectOneResponse-Sequence-alt3-2-part3, ProjectOneResponse-Sequence-alt3-2-part4, ProjectOneResponse-Sequence-alt3-2-part5, ProjectOneResponse-Sequence-alt3-2-part6, ProjectOneResponse-Sequence-alt3-3, ProjectOneResponse-Sequence-alt3-3-part2, ProjectOneResponse-Sequence-alt3-3-part3, ProjectOneResponse-Sequence-alt3-3-part4, ProjectOneResponse-Sequence-alt3-4, ProjectOneResponse-Sequence-alt3-4-part2, ProjectOneResponse-Sequence-alt2-2, ProjectOneResponse-Sequence-alt1-2, ProjectOneResponse-Sequence-alt1-2-part2, ProjectOneResponse-Sequence-alt4-1, ProjectOneResponse-Sequence-alt4-1-part2, ProjectOneResponse-Sequence-alt4-2, ProjectOneResponse-Sequence-alt4-2-part2, ProjectOneResponse-Sequence-alt4-2-part3, ProjectOneResponse-Sequence-alt4-2-part4, ProjectOneResponse-Sequence-alt1-3, ProjectOneResponse-Sequence-alt1-4, ProjectOneResponse-Sequence-alt1-4-part2
  * Participants 3: Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext; GraphRAG=GraphRAG -> qbrain.server.graphrag.
  * Source 8 messages, 2 replies, 1 self messages, 7 notes, 4 blocks with 12 branches; views carry 44 steps of 44 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 10 continuation steps of long self steps); 12 branch views for 12 branches.
* F172, line 4855, sequence diagram (section Sequence: stateless invocation end to end (process RunStatelessInvocation))
  * Views (27): STR-06-27, STR-06-27-part2, STR-06-27-part3, STR-06-27-part4, STR-06-27-alt1-1, STR-06-27-alt1-1-part2, STR-06-27-alt1-1-part3, STR-06-27-alt1-2, STR-06-27-alt1-2-part2, STR-06-27-alt1-2-part3, STR-06-27-alt2-1, STR-06-27-alt2-1-part2, STR-06-27-alt2-2, STR-06-27-alt2-2-part2, STR-06-27-alt2-2-part3, STR-06-27-alt2-2-part4, STR-06-27-alt2-3, STR-06-27-alt2-3-part2, STR-06-27-alt2-4, STR-06-27-alt2-4-part2, STR-06-27-alt2-4-part3, STR-06-27-alt2-4-part4, STR-06-27-alt2-5, STR-06-27-alt2-5-part2, STR-06-27-alt2-5-part3, STR-06-27-alt2-5-part4, STR-06-27-alt2-5-part5
  * Participants 5: Operator=Operator -> developer; Host=Host -> agent; Plugin=Plugin -> qbrain.plugins.pluginPs; Server=Server -> qbrain.server; Model=Model -> strHostModel.
  * Source 3 messages, 6 replies, 0 self messages, 7 notes, 2 blocks with 7 branches; views carry 34 steps of 34 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 9 continuation steps of long self steps); 7 branch views for 7 branches.
* F173, line 4894, sequence diagram (section Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence))
  * Views (24): BuildAndDeliverPackage, BuildAndDeliverPackage-alt1-1, BuildAndDeliverPackage-alt1-1-part2, BuildAndDeliverPackage-alt1-1-part3, BuildAndDeliverPackage-alt2-1, BuildAndDeliverPackage-alt2-1-part2, BuildAndDeliverPackage-alt2-1-part3, BuildAndDeliverPackage-alt2-2, BuildAndDeliverPackage-alt2-2-part2, BuildAndDeliverPackage-alt1-2, BuildAndDeliverPackage-alt1-2-part2, BuildAndDeliverPackage-alt1-2-part3, BuildAndDeliverPackage-alt1-3, BuildAndDeliverPackage-alt1-3-part2, BuildAndDeliverPackage-alt1-3-part3, BuildAndDeliverPackage-alt3-1, BuildAndDeliverPackage-alt3-1-part2, BuildAndDeliverPackage-alt3-2, BuildAndDeliverPackage-alt3-2-part2, BuildAndDeliverPackage-alt3-2-part3, BuildAndDeliverPackage-alt3-2-part4, BuildAndDeliverPackage-alt3-2-part5, BuildAndDeliverPackage-opt1, BuildAndDeliverPackage-opt1-part2
  * Participants 5: Operator=Operator -> developer; Host=Host -> agent; Plugin=Plugin -> qbrain.plugins.pluginPs; Server=Server -> qbrain.server; Model=Model -> strHostModel.
  * Source 4 messages, 3 replies, 1 self messages, 4 notes, 4 blocks with 8 branches; views carry 28 steps of 28 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 4 continuation steps of long self steps); 8 branch views for 8 branches.
* F174, line 4932, sequence diagram (section Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence))
  * Views (9): SubmitAndGradeResponse, SubmitAndGradeResponse-part2, SubmitAndGradeResponse-alt1-1, SubmitAndGradeResponse-alt2-1, SubmitAndGradeResponse-alt2-1-part2, SubmitAndGradeResponse-alt2-2, SubmitAndGradeResponse-alt1-2, SubmitAndGradeResponse-alt1-2-part2, SubmitAndGradeResponse-alt1-3
  * Participants 3: Model=Model -> strHostModel; Plugin=Plugin -> qbrain.plugins.pluginPs; Server=Server -> qbrain.server.
  * Source 3 messages, 5 replies, 0 self messages, 2 notes, 2 blocks with 5 branches; views carry 17 steps of 17 expected (one per message, reply, self message and note, one marker per block, one condition per branch); 5 branch views for 5 branches.
* F175, line 4961, sequence diagram (section Sequence: submit GetAgentContextPilotReportQuery)
  * Views (8): STR-06-28, STR-06-28-part2, STR-06-28-part3, STR-06-28-part4, STR-06-28-alt1-1, STR-06-28-alt1-1-part2, STR-06-28-alt1-2, STR-06-28-alt1-2-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; views carry 13 steps of 13 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 2 continuation steps of long self steps); 2 branch views for 2 branches.
* F176, line 4985, sequence diagram (section Sequence: submit CreateAgentContextHvRequestCommand)
  * Views (19): STR-06-29, STR-06-29-part2, STR-06-29-part3, STR-06-29-alt1-1, STR-06-29-alt1-2, STR-06-29-alt1-2-part2, STR-06-29-alt1-2-part3, STR-06-29-alt1-2-part4, STR-06-29-alt2-1, STR-06-29-alt2-2, STR-06-29-alt2-3, STR-06-29-alt2-3-part2, STR-06-29-alt3-1, STR-06-29-alt3-2, STR-06-29-alt3-2-part2, STR-06-29-alt3-3, STR-06-29-alt3-3-part2, STR-06-29-alt2-4, STR-06-29-alt2-4-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 6 messages, 7 replies, 0 self messages, 5 notes, 3 blocks with 9 branches; views carry 33 steps of 33 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 3 continuation steps of long self steps); 9 branch views for 9 branches.
* F177, line 5026, sequence diagram (section Sequence: submit GetAgentContextHvRequestQuery)
  * Views (7): STR-06-30, STR-06-30-part2, STR-06-30-part3, STR-06-30-part4, STR-06-30-part5, STR-06-30-alt1-1, STR-06-30-alt1-2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 2 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; views carry 11 steps of 11 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F178, line 5047, sequence diagram (section Sequence: submit QueryAgentContextHvRequestsQuery)
  * Views (7): STR-06-31, STR-06-31-part2, STR-06-31-part3, STR-06-31-part4, STR-06-31-alt1-1, STR-06-31-alt1-2, STR-06-31-alt1-2-part2
  * Participants 4: Caller=Caller -> qbrain.libs.client; Controller=Controller -> qbrain.server.api; Handler=Handler -> qbrain.server.strAgentContextHandlers; Store=Store -> qbrain.server.mcpDbContext.
  * Source 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; views carry 12 steps of 12 expected (one per message, reply, self message and note, one marker per block, one condition per branch, plus 1 continuation steps of long self steps); 2 branch views for 2 branches.
* F179, line 5073, use-case flowchart (section 7.1 Overview)
  * Views (12): STR-07-01, STR-07-01-part2, STR-07-01-part3, STR-07-01-part4, STR-07-01-part5, STR-07-01-part6, STR-07-01-part7, STR-07-01-part8, STR-07-01-part9, STR-07-01-part10, STR-07-01-part11, STR-07-01-part12
  * Source 21 nodes and 21 edges; drawn on 22 model elements (a node that names an existing element or a use case drawn twice is one element) with 25 element edges; views show 22 of 22 elements and 25 of 25 element edges.
* F180, line 5125, use-case flowchart (section 7.2 Use case: Log each user request as exactly one turn (plan UC-1) [FR-MCP-SESSIONTURN-001, -002, -003, -007, -008])
  * Views (8): UC-1, UC-1-part2, UC-1-part3, UC-1-part4, UC-1-part5, UC-1-part6, UC-1-part7, UC-1-part8
  * Source 16 nodes and 15 edges; drawn on 17 model elements (a node that names an existing element or a use case drawn twice is one element) with 16 element edges; views show 17 of 17 elements and 16 of 16 element edges.
* F181, line 5148, use-case flowchart (section 7.3 Use case: Persist session-log writes with truthful receipts (plan UC-2) [FR-MCP-SESSIONTURN-004])
  * Views (4): UC-2, UC-2-part2, UC-2-part3, UC-2-part4
  * Source 9 nodes and 8 edges; drawn on 9 model elements (a node that names an existing element or a use case drawn twice is one element) with 8 element edges; views show 9 of 9 elements and 8 of 8 element edges.
* F182, line 5164, use-case flowchart (section 7.4 Use case: Close every request turn before stopping (plan UC-3) [FR-MCP-SESSIONTURN-005, -006])
  * Views (5): UC-3, UC-3-part2, UC-3-part3, UC-3-part4, UC-3-part5
  * Source 11 nodes and 10 edges; drawn on 11 model elements (a node that names an existing element or a use case drawn twice is one element) with 10 element edges; views show 11 of 11 elements and 10 of 10 element edges.
* F183, line 5182, use-case flowchart (section 7.5 Use case: Classify a request (plan UC-4) [FR-MCP-SESSIONCLASS-001, FR-MCP-SESSIONREVIEW-006])
  * Views (3): UC-4, UC-4-part2, UC-4-part3
  * Source 7 nodes and 6 edges; drawn on 7 model elements (a node that names an existing element or a use case drawn twice is one element) with 6 element edges; views show 7 of 7 elements and 6 of 6 element edges.
* F184, line 5196, use-case flowchart (section 7.6 Use case: Complete or fail a turn under the completion gate (plan UC-5) [FR-MCP-SESSIONCLASS-002, -003, -004])
  * Views (8): UC-5, UC-5-part2, UC-5-part3, UC-5-part4, UC-5-part5, UC-5-part6, UC-5-part7, UC-5-part8
  * Source 12 nodes and 11 edges; drawn on 16 model elements (a node that names an existing element or a use case drawn twice is one element) with 15 element edges; views show 16 of 16 elements and 15 of 15 element edges.
* F185, line 5215, use-case flowchart (section 7.7 Use case: Record an agent self-review (plan UC-6) [FR-MCP-SESSIONREVIEW-001, -002, -003, -006, -008, FR-SUPPORT-016])
  * Views (5): UC-6, UC-6-part2, UC-6-part3, UC-6-part4, UC-6-part5
  * Source 10 nodes and 9 edges; drawn on 10 model elements (a node that names an existing element or a use case drawn twice is one element) with 9 element edges; views show 10 of 10 elements and 9 of 9 element edges.
* F186, line 5232, use-case flowchart (section 7.8 Use case: Record a hostile validation review with receipts (plan UC-7) [FR-MCP-SESSIONREVIEW-002, -005])
  * Views (5): UC-7, UC-7-part2, UC-7-part3, UC-7-part4, UC-7-part5
  * Source 10 nodes and 9 edges; drawn on 10 model elements (a node that names an existing element or a use case drawn twice is one element) with 9 element edges; views show 10 of 10 elements and 9 of 9 element edges.
* F187, line 5249, use-case flowchart (section 7.9 Use case: Configure review pass thresholds (plan UC-8) [FR-MCP-SESSIONREVIEW-007])
  * Views (2): UC-8, UC-8-part2
  * Source 5 nodes and 4 edges; drawn on 5 model elements (a node that names an existing element or a use case drawn twice is one element) with 4 element edges; views show 5 of 5 elements and 4 of 4 element edges.
* F188, line 5261, use-case flowchart (section 7.10 Use case: Read review scores losslessly (plan UC-9) [FR-MCP-SESSIONREVIEW-004])
  * Views (2): UC-9, UC-9-part2
  * Source 4 nodes and 3 edges; drawn on 4 model elements (a node that names an existing element or a use case drawn twice is one element) with 3 element edges; views show 4 of 4 elements and 3 of 3 element edges.
* F189, line 5272, use-case flowchart (section 7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002])
  * Views (5): STR-07-02, STR-07-02-part2, STR-07-02-part3, STR-07-02-part4, STR-07-02-part5
  * Source 10 nodes and 9 edges; drawn on 11 model elements (a node that names an existing element or a use case drawn twice is one element) with 10 element edges; views show 11 of 11 elements and 10 of 10 element edges.
* F190, line 5291, use-case flowchart (section 7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004])
  * Views (7): STR-07-03, STR-07-03-part2, STR-07-03-part3, STR-07-03-part4, STR-07-03-part5, STR-07-03-part6, STR-07-03-part7
  * Source 15 nodes and 14 edges; drawn on 15 model elements (a node that names an existing element or a use case drawn twice is one element) with 14 element edges; views show 15 of 15 elements and 14 of 14 element edges.
* F191, line 5313, use-case flowchart (section 7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007])
  * Views (3): STR-07-04, STR-07-04-part2, STR-07-04-part3
  * Source 6 nodes and 5 edges; drawn on 6 model elements (a node that names an existing element or a use case drawn twice is one element) with 5 element edges; views show 6 of 6 elements and 5 of 5 element edges.
* F192, line 5326, use-case flowchart (section 7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006])
  * Views (4): STR-07-05, STR-07-05-part2, STR-07-05-part3, STR-07-05-part4
  * Source 9 nodes and 8 edges; drawn on 9 model elements (a node that names an existing element or a use case drawn twice is one element) with 8 element edges; views show 9 of 9 elements and 8 of 8 element edges.
* F193, line 5344, use-case flowchart (section 7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-008, -009])
  * Views (7): STR-07-06, STR-07-06-part2, STR-07-06-part3, STR-07-06-part4, STR-07-06-part5, STR-07-06-part6, STR-07-06-part7
  * Source 13 nodes and 12 edges; drawn on 14 model elements (a node that names an existing element or a use case drawn twice is one element) with 13 element edges; views show 14 of 14 elements and 13 of 13 element edges.
* F194, line 5364, use-case flowchart (section 7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007])
  * Views (4): STR-07-07, STR-07-07-part2, STR-07-07-part3, STR-07-07-part4
  * Source 8 nodes and 7 edges; drawn on 8 model elements (a node that names an existing element or a use case drawn twice is one element) with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges.
* F195, line 5381, activity flowchart (section 8.1 Phase order)
  * Views (2): STR-08-01, STR-08-01-part2
  * Source 7 nodes and 6 edges; model 7 Activity elements (Start 0, Action 7, Decision 0, Error 0, End 0) and 6 relationships; views show 7 of 7 nodes and 6 of 6 edges.
* F196, line 5412, bounded-context graph (section 8.3 Bounded contexts (Phase 4 work packages))
  * Views (5): STR-08-02, STR-08-02-part2, STR-08-02-part3, STR-08-02-part4, STR-08-02-part5
  * Source 12 nodes in 2 subgraphs and 16 edges; model 16 relationships; views show 12 of 12 nodes and 16 of 16 edges.
* F197, line 5462, activity flowchart (section Process RunPlanPhases: run the four phases)
  * Views (3): RunPlanPhases, RunPlanPhases-part2, RunPlanPhases-part3
  * Source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 6, Decision 0, Error 0, End 2) and 9 relationships; views show 9 of 9 nodes and 9 of 9 edges.
* F198, line 5477, activity flowchart (section Nested activity: RunOnePlanPhase (loop body of RunPlanPhases))
  * Views (2): RunOnePlanPhase, RunOnePlanPhase-part2
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 3, Decision 1, Error 0, End 2) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F199, line 5490, activity flowchart (section Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases))
  * Views (3): ValidateAndApprove, ValidateAndApprove-part2, ValidateAndApprove-part3
  * Source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 0, End 2) and 7 relationships; views show 7 of 7 nodes and 7 of 7 edges.
* F200, line 5505, activity flowchart (section Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases))
  * Views (1): RunPhase4WorkPackages
  * Source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 2, Decision 1, Error 0, End 1) and 5 relationships; views show 5 of 5 nodes and 5 of 5 edges.
* F201, line 5518, activity flowchart (section Process RunDomainCodingAgent: one Phase 4 work package)
  * Views (2): RunDomainCodingAgent, RunDomainCodingAgent-part2
  * Source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 3, Decision 0, Error 0, End 2) and 5 relationships; views show 6 of 6 nodes and 5 of 5 edges.
* F202, line 5529, activity flowchart (section Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent))
  * Views (1): ImplementBoundedContext
  * Source 4 nodes and 4 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 0, End 2) and 4 relationships; views show 4 of 4 nodes and 4 of 4 edges.
* F203, line 5539, activity flowchart (section Nested activity: ImplementationPass (loop body of ImplementBoundedContext))
  * Views (4): ImplementationPass, ImplementationPass-part2, ImplementationPass-part3, ImplementationPass-part4
  * Source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 3, Decision 4, Error 0, End 3) and 12 relationships; views show 11 of 11 nodes and 12 of 12 edges.
* F204, line 5645, ER diagram (section A.1 References between the groups)
  * Views (9): STR-A-01, STR-A-01-part2, STR-A-01-part3, STR-A-01-part4, STR-A-01-part5, STR-A-01-part6, STR-A-01-part7, STR-A-01-part8, STR-A-01-part9
  * Source 12 entities and 16 relationships (0 reuse an as-built EF relationship); views show 12 of 12 entities and 16 of 16 relationships.
  * Entities: SessionLog -> entSessionLog; AgentContextHvRequest -> strEntAgentContextHvRequest; SessionLogTurn -> entSessionLogTurn; AgentContextPackage -> strEntAgentContextPackage; AgentResponse -> strEntAgentResponse; AgentResponseGrade -> strEntAgentResponseGrade; TodoItem -> entTodoItem; AgentContextPackageItem -> strEntAgentContextPackageItem; Requirement -> entRequirement; AgentContextFinding -> strEntAgentContextFinding; AgentContextRelationship -> strEntAgentContextRelationship; AgentContextPackageInputLink -> strEntAgentContextPackageInputLink.
* F205, line 5676, ER diagram (section A.2 Sessions and turns (Phases A and B))
  * Views (1): STR-A-02
  * Source 3 entities and 2 relationships (1 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: SessionLog -> entSessionLog; SessionLogTurn -> entSessionLogTurn; SessionLogTurnReviewReason -> strEntSessionLogTurnReviewReason.
* F206, line 5713, ER diagram (section A.3 Turn records)
  * Views (3): STR-A-03, STR-A-03-part2, STR-A-03-part3
  * Source 6 entities and 5 relationships (5 reuse an as-built EF relationship); views show 6 of 6 entities and 5 of 5 relationships.
  * Entities: SessionLogAction -> entSessionLogAction; SessionLogProcessingDialog -> entSessionLogProcessingDialog; SessionLogTurnStringList -> entSessionLogTurnStringList; SessionLogCommit -> entSessionLogCommit; SessionLogTurnContext -> entSessionLogTurnContext; SessionLogTurn -> entSessionLogTurn.
* F207, line 5747, ER diagram (section A.4 Requirements, TODOs, and use cases (existing entities the plan references))
  * Views (3): STR-A-04, STR-A-04-part2, STR-A-04-part3
  * Source 7 entities and 7 relationships (7 reuse an as-built EF relationship); views show 7 of 7 entities and 7 of 7 relationships.
  * Entities: TodoItem -> entTodoItem; TodoRequirementLink -> entTodoRequirementLink; Requirement -> entRequirement; RequirementAcceptanceCriterion -> entRequirementAcceptanceCriterion; RequirementTraceabilityLink -> entRequirementTraceabilityLink; UseCase -> entUseCase; UseCaseFrLink -> entUseCaseFrLink.
* F208, line 5809, ER diagram (section A.5 Packages and their build inputs (Phase C))
  * Views (2): STR-A-05, STR-A-05-part2
  * Source 5 entities and 4 relationships (0 reuse an as-built EF relationship); views show 5 of 5 entities and 4 of 4 relationships.
  * Entities: AgentContextPackage -> strEntAgentContextPackage; AgentContextPackageItem -> strEntAgentContextPackageItem; AgentContextPackageFocusRef -> strEntAgentContextPackageFocusRef; AgentContextPackageInput -> strEntAgentContextPackageInput; AgentContextPackageInputLink -> strEntAgentContextPackageInputLink.
* F209, line 5859, ER diagram (section A.6 Package omissions and findings (Phase C))
  * Views (1): STR-A-06
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: AgentContextOmission -> strEntAgentContextOmission; AgentContextFinding -> strEntAgentContextFinding; AgentContextPackage -> strEntAgentContextPackage.
* F210, line 5887, ER diagram (section A.7 Responses and evidence (Phase C))
  * Views (2): STR-A-07, STR-A-07-part2
  * Source 4 entities and 4 relationships (0 reuse an as-built EF relationship); views show 4 of 4 entities and 4 of 4 relationships.
  * Entities: AgentResponse -> strEntAgentResponse; AgentResponseClaim -> strEntAgentResponseClaim; AgentResponseEvidence -> strEntAgentResponseEvidence; AgentContextPackage -> strEntAgentContextPackage.
* F211, line 5926, ER diagram (section A.8 Grades (Phase C))
  * Views (2): STR-A-08, STR-A-08-part2
  * Source 4 entities and 3 relationships (0 reuse an as-built EF relationship); views show 4 of 4 entities and 3 of 3 relationships.
  * Entities: AgentResponseGrade -> strEntAgentResponseGrade; AgentResponseGradeFinding -> strEntAgentResponseGradeFinding; AgentResponse -> strEntAgentResponse; AgentContextHvRequest -> strEntAgentContextHvRequest.
* F212, line 5956, ER diagram (section A.9 HV requests (Phase C))
  * Views (1): STR-A-09
  * Source 3 entities and 2 relationships (0 reuse an as-built EF relationship); views show 3 of 3 entities and 2 of 2 relationships.
  * Entities: AgentContextHvRequest -> strEntAgentContextHvRequest; AgentContextHvRequestItem -> strEntAgentContextHvRequestItem; AgentResponse -> strEntAgentResponse.
* F213, line 5985, ER diagram (section A.10 Relationships, the GraphRAG projection, and retrieval stores (Phase C))
  * Views (3): STR-A-10, STR-A-10-part2, STR-A-10-part3
  * Source 7 entities and 5 relationships (3 reuse an as-built EF relationship); views show 7 of 7 entities and 5 of 5 relationships.
  * Entities: AgentContextRelationship -> strEntAgentContextRelationship; AgentContextProjection -> strEntAgentContextProjection; ContextDocument -> entContextDocument; ContextChunk -> entContextChunk; GraphEntity -> entGraphEntity; GraphRelationship -> entGraphRelationship; AgentResponse -> strEntAgentResponse.

## Render inspection

* Method: every batch render was laid out on contact sheets (SCRATCH/p4-str/all-sheets.ps1, nine renders per sheet with the view key under each, index in SCRATCH/p4-str/sheets/index.txt) and read as images; three read-only geometry checks ran over every STR render of each batch (1418 on the final gate render): svg-text-fit.ps1 (element text against its shape), svg-label-overlap.ps1 (relationship description and technology labels against element text and against each other) and wrap-calibrate.ps1 (calibration of the converter's text-fit model on 1240 name blocks and 157 description blocks).
* Checked on each image: every node, state, entity, use case and participant of the part is drawn with its full name; every edge and step label is present and legible; branch and part titles match the view key; split parts repeat the boundary node; terminals render as ellipses, decisions as diamonds, errors in the error style; nothing crosses the page edge.
* Sheet numbers below refer to the contact-sheet index of the render named (batch 20 and the first gate pair: 1408 views on 157 sheets; final gate pair: 1418 views on 158 sheets).
* Final sources: the last two layout changes (static two-rank parts at tb 400 250, self-step labels on McpDbContext at most 3 lines) were made after reading the first gate renders. affected-views.ps1 lists the 96 views they touch (73 use-case and overview views, 23 dynamic views with a McpDbContext self step, 10 of them new parts); all were read again as images on the final gate render (sheets 97, 129 to 132 and 143 to 152 of 158, and SCRATCH/p4-str/sheets/check-d.png): the association labels now sit below the QBrain.AI boundary name, the McpDbContext name is clear of the step labels, and the split notes read "(1 of 4)" to "(4 of 4)". Every other view keeps the source of the first gate pair.
* Read as images on the first gate renders (runs p4-str-g4-r1 and r2, byte-identical): sheets 3 to 15 (activity views of sections 3.5 and 4.2, from IdempotentCreate to OpenTurnUnderLock, including every part of SerializableWrite, IdempotentCreate, PagedQuery, TurnLocator, TurnWritePipeline, ResponseStatusTransition, IResponseStalenessRule, RelationshipWriter, Resolve-MintOutcome, Invoke-SessionOpen, Set-SessionUnsupported, Update-TurnState, Add-McpAlias, MintSessionTurnCommand, InsertMintedTurnWithRetry, SubmitSessionLogCommand, EvaluateSubmittedTurn, UpsertSessionTurnCommand, PatchSessionTurnCommand, ReplaceSessionTurnCommand, PrepareReplacementTurn, CompleteSessionTurnCommand, FailSessionTurnCommand, OpenSessionLogCommand, AppendTurnDialogCommand, SetTurnTitleCommand, SetSessionTitleCommand, TerminalTurnOutcomeGuard, ApplyArrivalBridgeRecord, ApplyEnqueueRecord, ApplyDequeueRecord, ApplyRemoveRecord, OpenTurnFromPromptHook), sheet 7 (state STR-04-01, all 6 parts), sheet 55 (state STR-05-01, all 5 parts), sheet 72 (state STR-06-07, all 8 parts, and entity STR-06-06-part2).
* Read as images on batch 20 (docs/receipts/c4model/20261019T185909-p4-str-batch20.log; the same sources as the gate except the layout rule of two-rank state parts, so every non-state view has the gate layout): sheets 1 to 102 except 33, and sheets 104, 107, 110, 113, 115, 118, 122, 146, 152, 154 and 156 of 157. They cover the system context STR-02-01 (all 11 parts), every activity view of sections 3 to 6, the sequence views of sections 4.5, 5 and 6.7 up to STR-06-24 with their alt, opt and loop branch views and parts, use cases UC-1 and UC-5 (all parts), the bounded contexts STR-08-02 (all 5 parts), the plan activities of section 8 and the entities of Addendum A (STR-A-01, STR-A-10).
* Read as images on earlier batches and fixed before the gate (batch logs docs/receipts/c4model/20261019T*-p4-str-batch*.log): batch 5 sheets 1 to 8, 26, 60, 114, 123 and 125 (repeated isolated end nodes, decision text covered by edge labels, stacked static labels); batch 10 sheets 1 to 27, 58 to 60, 113, 115 and 117; batch 12 sheets 1 to 6 and 27; batch 13 sheets 1, 3, 5, 7 to 9 and 28 to 30; batch 16 sheets 2, 4, 6, 10, 12, 16, 27, 33, 40, 50, 60 and 66; batch 17 sheets 33, 71, 72, 127, 144 and 151 (F1 label crowding, ellipse first lines outside the shape, split names, self-step loops hiding each other, state self loops, crossing entity labels). Each finding became a converter or layout change listed in the judgment calls and the session log.
* Read as images on the first gate render as well: sheets 33, 103, 105, 106, 108, 109, 111, 112, 114, 116, 117, 119 to 121, 123 to 143, 145, 147 to 150, 153 and 155 (the sequence views of sections 4.5 and 6.7 from STR-04-05 and STR-06-15 to STR-06-31 with every branch and part, the use-case overview STR-07-01 and UC-2 to UC-9, STR-07-02 to STR-07-06, the plan activities of section 8 and the Addendum A entities STR-A-01-part6 to STR-A-04). Together with the batch 20 sheets, every one of the 157 contact sheets was read as an image on batch 20 or the gate renders; sheets 151, 152, 154, 156 and 157 were read on batch 20 only.
* Geometry check results on the final gate renders (p4-str-g4b): svg-label-overlap.ps1 found 13 label boxes covering element text or another label among 5776 labels in 11 of 1418 views (largest: STR-06-08-part2 62.7 units, two transitions between the same two projection-row states; STR-A-04-part2 57.8, two foreign keys between the same two as-built entities; CheckOneEvidenceItem-part2 32.2; STR-07-01-part7 32.0, part3 31.3 and part4 7.1, participates in labels meeting at one use case; RunStatelessInvocation-part4 27.5; SelectHvRequestItems-part2 19.7 and 5.7; ProjectOneResponse-part5 19.6; RecordModelGradeCommand-part5 9.2; STR-04-01-part5 7.5 and 4.2). On every image read the covered text stayed legible; these are parallel or converging edges that the layout engine draws through one corridor.
* svg-text-fit.ps1 on the final gate renders: 125 of 5205 element instances run past their shape; 110 of them are as-built elements whose descriptions are longer than their boxes (McpDbContext in 79 views by 76 units, REPL Host in 29 views by 20, MCP Database in 2 views by 11; workspace.dsl beyond the two include lines is not this agent's file); 15 are converted activity elements whose node text fits no split (GetAgentContextPilotReportQuery-part2, ProjectOneResponse-part2 and two more parts, EvaluateSubmittedTurn and two more parts by 32 units, about one line; ValidateHookRegistration by 16 because "Get-DuplicateHookCommands:" is one word; SubmitSessionLogCommand-part2, ConvertHvReceiptToReview, EvaluateStopGate-part3 by 4) plus the as-built entity name RequirementTraceabilityLink by 4. The 28 node texts with no fitting split are listed in SCRATCH/p4-str/convert-out/nofit.txt; all of their text is drawn and legible.

## Judgment calls

* Sequence participants map to the qualifiers the document gives in sections 4.5 and 6.7: Plugin to lib-ps (qbrain.plugins.pluginPs), REPL to qbrain.repl, Controller and SessionLogController to REST Controllers (qbrain.server.api), Store to McpDbContext (qbrain.server.mcpDbContext), Writer, Client and Caller to QBrainAi.Client (qbrain.libs.client), Trigger to Host adapters (qbrain.plugins.pluginHosts), Runner to the evidence scripts (qbrain.devTools.evidenceScripts), Reviewer to LLM Providers and Agent CLIs (llm), Operator to the developer person, Host and Agent to the AI Coding Agent person, Model to the new external system strHostModel, Handler to strSessionLogHandlers before fence F146 and strAgentContextHandlers from F146 (Phase C). No participant creates a second element for an existing thing (D4).
* Planned components are kept at the granularity of the as-built model: one Shared Components element (strSharedComponents) for the eight shared types of sections 3.5.2 to 3.5.5, separate elements only for WorkspaceResolutionBehavior, ModeGateBehavior, PackageAssembler and the Completion Gate, which the diagrams draw as participants or nodes, and one handler component per bounded context (strSessionLogHandlers, strAgentContextHandlers). SourceResolver maps to Shared Components with its name kept in each step text.
* D3 code search (code-presence.ps1, 17:24:03 local, git grep in src, tests and plugins): 0 hits for WorkspaceResolutionBehavior, ModeGateBehavior, PackageAssembler, CompletionGate, SerializableWrite, IdempotentCreate, ReadableKeyAllocator, PagedQuery, SecretRedactor, TurnLocator, ScoreVerdict, SourceResolver, MintSessionTurnCommand, RecordSessionTurnReviewCommand, AgentContextController, every planned entity name, and the unit names Resolve-MintOutcome, Invoke-SessionOpen, Set-SessionUnsupported, Update-TurnState, Add-McpAlias, IResponseStalenessRule, RelationshipWriter, TurnWritePipeline and ResponseStatusTransition, so all are Planned. SessionLogService and ISessionLogService exist (114 and 104 hits) and are only named in descriptions, inside the as-built Session Log component. The single AgentResponse hit is the Microsoft Agent Framework type in src/QBrainAi.McpAgent.SampleHost/SampleHostPreviewFactory.cs line 588, not the planned EF entity.
* Every element and relationship this document adds carries the tag Planned; relationships that already exist in the as-built model between the same elements are reused instead (the entity foreign keys of model/entities.dsl among them), so the as-built views do not change content.
* Use-case diagrams become container views of QBrain.AI, because custom views refuse people and containers while static views accept custom elements: actors stay the existing people, containers and components, and each use case is one element shared by the overview F179 and the per-use-case diagrams; a use case drawn with another label keeps that label in its description ("Drawn as").
* Existing entities (18, reuse-summary.ps1: entContextChunk, entContextDocument, entGraphEntity, entGraphRelationship, entRequirement, entRequirementAcceptanceCriterion, entRequirementTraceabilityLink, entSessionLog, entSessionLogAction, entSessionLogCommit, entSessionLogProcessingDialog, entSessionLogTurn, entSessionLogTurnContext, entSessionLogTurnStringList, entTodoItem, entTodoRequirementLink, entUseCase, entUseCaseFrLink) and 16 of their as-built foreign-key relationships are reused; the columns a diagram draws for them go into that view's properties, because !element with a properties block breaks the pinned CLI parser (probe 15:56:47 local). Planned entities carry their columns, keys, table and Addendum A group as element properties; their visible description lists the keys and the table and drops the unique keys, then the table name, until it fits the box.
* State diagrams: the start and end pseudo states become State elements tagged Start and End, a choice becomes a State tagged Decision, and a composite state links to its substates with a substate relationship (State hierarchy).
* Activity node kinds: stadium nodes without incoming edges are Start, stadium nodes without outgoing edges are End or Error by rule (HTTP 4xx and 5xx, typed failures, refusals), with 54 per-node overrides in str-config.psd1 reviewed against the terminal report; decision nodes are Decision; subroutine and cylinder nodes are Action with the type line "call" or "store".
* Activity names follow a text-fit model measured from the renders (name lines 40.32 high, description lines 28.8, shapes 220 high; wrap at 20 and 29 characters): the whole node text when it fits, else the clause, comma, parenthesis, preposition or word break that fits with the longest name, else the lowest block that keeps a clause break; unqualified names come first, and a name already used in the workspace carries the view key as qualifier. No node text is dropped: the rest of the text is the element description. 28 node texts fit no split (listed in SCRATCH/p4-str/convert-out/nofit.txt); their text runs past the shape border by a few lines, all of it still readable.
* Section 9 of the document is the diagram index: its Mermaid rendering sentences are replaced by the C4 view rule and key scheme, the 2026-10-12 operator rendering quote is kept with the receipt of the earlier Mermaid renders, and each unnamed diagram gets its view key. The "How to read the diagrams" bullets and the participant notes of sections 4.5 and 6.7 still describe Mermaid and are unchanged operator-approved prose (open question for the implementer).

## Style edit and calibration receipts

* docs/architecture/c4/workspace.dsl diff against 92e2c6c4 (show-diff.ps1, 17:43:38 local): "!include model/str.dsl" added after line 487 ("!include model/entities.dsl"), "!include views/str.dsl" added after line 569 ("!include views/c4.dsl"), and "shape circle" changed to "shape ellipse" at lines 657 and 672 (element styles Start and End). Nothing else changed.
* The terminal style edit was approved by the implementer (QMD and MKT make the same edit): calibration r1 20261019T155739-p4-str-calibration.log showed Start and End circles at rx=225 ry=225 overlapping the next rank; the interim width 220 edit (r2 20261019T160942-p4-str-calibration-r2.log) wrapped terminal text to 8 lines (1284 x 1672); with ellipses (r3 20261019T161248-p4-str-calibration-r3.log) terminals render 450 x 220 like the boxes, and a 7-node activity rendered 1396 x 1604, which set the activity part limits.

## Gate G4 receipts

* Final run 1: docs/receipts/c4model/20261019T193127-p4-str-g4b-r1.log (19:31:27 to 19:34:45 local). diagram-findings.ps1: 0 ValidationFailed, 0 InspectionErrors, 0 RenderFailed, 0 DanglingViewLink, 0 MissingRender, 0 OrphanRender, 0 TextSizeSpread, 0 PageOverflow, 0 InvalidSvg; MermaidFence 222 and MermaidFile 4, none of them in docs/architecture/SessionTurn-Review-AgentContext-Architecture.md or the STR fragments (log-grep for the document and str.dsl: 0 matches); 1631 renders, 1418 of them the STR views. The build exits 255 only on the other documents' Mermaid findings (the MermaidFile findings are docs/Marketing/diagrams/*.mmd).
* Final run 2: docs/receipts/c4model/20261019T193503-p4-str-g4b-r2.log (19:35:03 to 19:38:44 local): the same counts. hash-dir.ps1 manifest of run 1 (SCRATCH/p4-str/manifest-g4b-r1.txt, 19:35:01 local) compared after run 2 (19:38:56 local): Identical 1631 of 1631.
* An earlier gate pair on the sources before the last two layout changes also held: 20261019T191341-p4-str-g4-r1.log and 20261019T191728-p4-str-g4-r2.log, the same zero counts, 1621 renders, Identical 1621 of 1621 (19:21:29 local).
* The .log receipts are ignored by .gitignore line 108 (*.log; check-ignore.ps1, 19:39:45 local) while 37 earlier .log receipts in the folder are tracked, so committing these needs git add -f.
