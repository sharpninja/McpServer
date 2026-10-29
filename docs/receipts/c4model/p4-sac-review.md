# P4 SAC review record: docs/Stateless-Agent-Context-Management.md

PLAN-C4MODEL-001 phase P4 stage 2, document code SAC (identifier prefix sac). Source fences are those of docs/Stateless-Agent-Context-Management.md at commit b457ac22e4588912e38fd58ffda14a47cc967361; every line number below is a line of that commit unless it says otherwise. The SessionTurn (STR) fences it is compared with are those of docs/architecture/SessionTurn-Review-AgentContext-Architecture.md at b457ac22^1 (the Mermaid text, byte-identical to the STR conversion source at 92e2c6c4, SHA-256 6E5D4F41...), and the STR views are views/str.dsl and model/str.dsl as merged at b457ac22. Model fragment docs/architecture/c4/model/sac.dsl and views fragment docs/architecture/c4/views/sac.dsl hold only a comment: SAC defines no element, relationship or view of its own. Generated 2026-10-19T20:32:07 (local) by SCRATCH/p4-sac/gen-review.ps1 from SCRATCH/p4-sac/compare.json, convert-out/views-by-fence.json, convert-out/coverage-sac.json, convert-out/reuse.json and review-notes.psd1.

## Summary

* Fences: 109 (activity flowchart 61, use-case flowchart 6, bounded-context graph 1, sequence diagram 33, ER diagram 6, state diagram 2).
* D1 for views: 109 of 109 fences draw the same diagram as one STR fence (identical text after trimming each line and dropping blank lines; 109 of 109 are byte-identical line for line, and 109 of 109 sit under the same heading text). Each fence therefore links the existing STR views of that fence, in the same order and with the same titles: 753 image and View pairs, 753 distinct view keys. New views 0, new elements 0, new relationships 0.
* Linked views by kind: custom (activity) 204, custom (entity) 13, custom (state) 15, dynamic (sequence) 486, component (use cases) 4, container (use cases) 26, custom (bounded contexts) 5.
* Activity flowcharts (recount): 579 of 579 nodes and 598 of 598 edges shown; 579 of 579 Activity elements carry the node text; 598 of 598 edges are relationships with the edge label.
* Sequence diagrams (recount): 117 messages, 149 replies, 19 self messages, 135 notes, 72 blocks with 191 branches; 756 of 756 expected view steps match in order, participant for participant (including 73 continuation steps of long self steps); 191 of 191 branch views open with their condition.
* ER diagrams (recount): 29 of 29 entity occurrences and 23 of 23 relationships shown; 23 of 23 are model relationships with their cardinality.
* State diagrams (recount): 15 of 15 state and pseudo-state elements and 34 of 34 transitions shown (34 named by their trigger in the model).
* Use-case and bounded-context flowcharts (recount): use cases 63 of 63 elements and 57 of 57 element edges; bounded contexts 12 of 12 contexts and 16 of 16 edges.
* Coverage gaps: none (0 problems over 109 fences).
* Fidelity differences of the reused views: 10 relationships shown beyond the source fence and 13 source labels drawn merged with another diagram's label, all in the six section 6.3 ER fences (STR-06-01 to STR-06-06); 0 in every other fence. Listed under each ER fence and discussed in the judgment calls.

## How the comparison and the counts were made

* compare-fences.ps1 extracted the 109 SAC fences and the 213 STR fences (extract-fences.ps1, heading chain and start line per fence), mapped every STR fence to the view keys that replaced it by walking the Mermaid STR document and the converted STR document side by side (213 of 213 mapped; the two differ outside fences only in the STR diagram index, converted lines 7320 to 7345), and compared each SAC fence with every STR fence after trimming each line and dropping blank lines. A match means the same nodes, edges, messages, notes, blocks and labels in the same order (D1 for views).
* convert-sac.ps1 took each matched fence's view keys and titles from the STR converter report (coverage.json of 19:31:14, whose views/str.dsl, model/str.dsl and converted document are byte-identical to the merged files, SHA-256 D64DF622..., 99E1B50C..., 0B061137...), checked them against the parallel walk, against the image lines of the converted STR document and against the 1677 view keys of the workspace, and replaced each fence in place (D7) with the path architecture/c4/rendered/<key>.svg. verify-doc.ps1: 109 of 109 fences replaced, 753 image and View pairs, 0 differences outside the fences, 0 Mermaid fences left, LF line ends and final newline as in the original.
* verify-coverage.ps1 recounted every fence independently of the STR report: it re-parsed the source fence (parsing rules copied from convert-str.ps1) and compared it with the merged DSL text: activity nodes against the elements of the fence's block in model/str.dsl (name plus description equal to the node text) and edges against relationships with the edge label; sequence steps view by view, in order, from and to element and text; ER entities and relationships with their cardinality; state elements and transitions named by trigger, self transitions only in the views that do not exclude them; use cases and contexts against their elements and relationships. It also lists every model relationship a static or custom view draws between its included elements that the source fence does not draw.

## Fences

* F1, line 47, activity flowchart (3.5.1 Request pipeline (CQRS behaviors))
  * Same diagram as STR F3, line 330 (byte-identical; STR heading "3.5.1 Request pipeline (CQRS behaviors)").
  * Views (2): RequestPipeline, RequestPipeline-part2
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 1), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F2, line 63, activity flowchart (3.5.2 Storage and errors)
  * Same diagram as STR F4, line 346 (byte-identical; STR heading "3.5.2 Storage and errors").
  * Views (3): SerializableWrite, SerializableWrite-part2, SerializableWrite-part3
  * Coverage: source 9 nodes and 10 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 2, End 1), 9 of 9 with the node text, 10 of 10 edges as relationships with the edge label; views show 9 of 9 nodes and 10 of 10 edges.
* F3, line 79, activity flowchart (3.5.2 Storage and errors)
  * Same diagram as STR F5, line 362 (byte-identical; STR heading "3.5.2 Storage and errors").
  * Views (5): IdempotentCreate, IdempotentCreate-part2, IdempotentCreate-part3, IdempotentCreate-part4, IdempotentCreate-part5
  * Coverage: source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 2, Decision 3, Error 1, End 4), 11 of 11 with the node text, 12 of 12 edges as relationships with the edge label; views show 11 of 11 nodes and 12 of 12 edges.
* F4, line 98, activity flowchart (3.5.2 Storage and errors)
  * Same diagram as STR F6, line 381 (byte-identical; STR heading "3.5.2 Storage and errors").
  * Views (2): PagedQuery, PagedQuery-part2
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 1, End 1), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F5, line 118, activity flowchart (3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`))
  * Same diagram as STR F7, line 401 (byte-identical; STR heading "3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`)").
  * Views (3): TurnLocator, TurnLocator-part2, TurnLocator-part3
  * Coverage: source 10 nodes and 9 edges; model 10 Activity elements (Start 1, Action 0, Decision 4, Error 3, End 2), 10 of 10 with the node text, 9 of 9 edges as relationships with the edge label; views show 10 of 10 nodes and 9 of 9 edges.
* F6, line 133, activity flowchart (3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`))
  * Same diagram as STR F8, line 416 (byte-identical; STR heading "3.5.3 Session log (namespace `QBrainAi.Support.Mcp.SessionLog`)").
  * Views (6): TurnWritePipeline, TurnWritePipeline-part2, TurnWritePipeline-part3, TurnWritePipeline-part4, TurnWritePipeline-part5, TurnWritePipeline-part6
  * Coverage: source 20 nodes and 21 edges; model 20 Activity elements (Start 1, Action 7, Decision 3, Error 7, End 2), 20 of 20 with the node text, 21 of 21 edges as relationships with the edge label; views show 20 of 20 nodes and 21 of 21 edges.
* F7, line 172, activity flowchart (3.5.5 Agent context)
  * Same diagram as STR F9, line 455 (byte-identical; STR heading "3.5.5 Agent context").
  * Views (3): ResponseStatusTransition, ResponseStatusTransition-part2, ResponseStatusTransition-part3
  * Coverage: source 10 nodes and 10 edges; model 10 Activity elements (Start 1, Action 3, Decision 3, Error 1, End 2), 10 of 10 with the node text, 10 of 10 edges as relationships with the edge label; views show 10 of 10 nodes and 10 of 10 edges.
* F8, line 188, activity flowchart (3.5.5 Agent context)
  * Same diagram as STR F10, line 471 (byte-identical; STR heading "3.5.5 Agent context").
  * Views (3): IResponseStalenessRule, IResponseStalenessRule-part2, IResponseStalenessRule-part3
  * Coverage: source 8 nodes and 8 edges; model 8 Activity elements (Start 1, Action 1, Decision 3, Error 0, End 3), 8 of 8 with the node text, 8 of 8 edges as relationships with the edge label; views show 8 of 8 nodes and 8 of 8 edges.
* F9, line 203, activity flowchart (3.5.5 Agent context)
  * Same diagram as STR F11, line 486 (byte-identical; STR heading "3.5.5 Agent context").
  * Views (2): RelationshipWriter, RelationshipWriter-part2
  * Coverage: source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 2), 8 of 8 with the node text, 7 of 7 edges as relationships with the edge label; views show 8 of 8 nodes and 7 of 7 edges.
* F10, line 220, activity flowchart (3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Same diagram as STR F12, line 503 (byte-identical; STR heading "3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)").
  * Views (4): Resolve-MintOutcome, Resolve-MintOutcome-part2, Resolve-MintOutcome-part3, Resolve-MintOutcome-part4
  * Coverage: source 10 nodes and 11 edges; model 10 Activity elements (Start 1, Action 0, Decision 3, Error 3, End 3), 10 of 10 with the node text, 11 of 11 edges as relationships with the edge label; views show 10 of 10 nodes and 11 of 11 edges.
* F11, line 237, activity flowchart (3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Same diagram as STR F13, line 520 (byte-identical; STR heading "3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)").
  * Views (2): Invoke-SessionOpen, Invoke-SessionOpen-part2
  * Coverage: source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 0, Decision 1, Error 2, End 1), 5 of 5 with the node text, 4 of 4 edges as relationships with the edge label; views show 5 of 5 nodes and 4 of 4 edges.
* F12, line 247, activity flowchart (3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Same diagram as STR F14, line 530 (byte-identical; STR heading "3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)").
  * Views (2): Set-SessionUnsupported, Set-SessionUnsupported-part2
  * Coverage: source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 2, End 0), 5 of 5 with the node text, 4 of 4 edges as relationships with the edge label; views show 5 of 5 nodes and 4 of 4 edges.
* F13, line 257, activity flowchart (3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Same diagram as STR F15, line 540 (byte-identical; STR heading "3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)").
  * Views (3): Update-TurnState, Update-TurnState-part2, Update-TurnState-part3
  * Coverage: source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 2, Error 2, End 1), 9 of 9 with the node text, 8 of 8 edges as relationships with the edge label; views show 9 of 9 nodes and 8 of 8 edges.
* F14, line 279, activity flowchart (3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell))
  * Same diagram as STR F16, line 562 (byte-identical; STR heading "3.5.6 Plugin core functions (PowerShell, `plugins/core/lib-ps`, owner PluginCorePowerShell)").
  * Views (3): Add-McpAlias, Add-McpAlias-part2, Add-McpAlias-part3
  * Coverage: source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 1, Decision 2, Error 2, End 2), 8 of 8 with the node text, 7 of 7 edges as relationships with the edge label; views show 8 of 8 nodes and 7 of 7 edges.
* F15, line 491, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F99, line 2860 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (3): STR-06-01, STR-06-01-part2, STR-06-01-part3
  * Coverage: source 7 entities and 6 relationships (entSessionLogTurn, strEntAgentContextPackage, strEntAgentContextPackageItem, strEntAgentContextPackageFocusRef, strEntAgentContextPackageInput, strEntAgentContextPackageInputLink, strEntAgentContextRelationship); 6 of 6 relationships in the model with their cardinality; views show 7 of 7 entities and 6 of 6 relationships.
  * Drawn beyond the source (3): STR-06-01: strEntAgentContextPackageItem -> entSessionLogTurn "0..n to 1: SourceRef, kind SessionLogTurn"; STR-06-01-part3: strEntAgentContextRelationship -> strEntAgentContextPackage "0..n to 0..n: From or To kind Package"; STR-06-01-part3: strEntAgentContextPackageInputLink -> strEntAgentContextRelationship "0..n to 1: RelationshipId, read at build".
  * Labels merged (4): AgentContextPackage -> AgentContextPackageItem "1 to 0..n: contains" drawn as "1 to 0..n: contains / items"; AgentContextPackage -> AgentContextPackageFocusRef "1 to 0..n: focuses" drawn as "1 to 0..n: focuses / focus refs"; AgentContextPackage -> AgentContextPackageInput "1 to 0..n: snapshots" drawn as "1 to 0..n: snapshots / inputs"; AgentContextPackage -> AgentContextPackageInputLink "1 to 0..n: snapshots links" drawn as "1 to 0..n: snapshots links / input links".
* F16, line 503, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F100, line 2872 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (2): STR-06-02, STR-06-02-part2
  * Coverage: source 4 entities and 3 relationships (strEntAgentContextPackage, strEntAgentContextOmission, strEntAgentContextFinding, strEntAgentContextRelationship); 3 of 3 relationships in the model with their cardinality; views show 4 of 4 entities and 3 of 3 relationships.
  * Drawn beyond the source (1): STR-06-02-part2: strEntAgentContextFinding -> strEntAgentContextRelationship "0..n to 1: RelationshipId".
  * Labels merged (2): AgentContextPackage -> AgentContextOmission "1 to 0..n: records" drawn as "1 to 0..n: records / omissions"; AgentContextPackage -> AgentContextFinding "1 to 0..n: records" drawn as "1 to 0..n: records / findings".
* F17, line 512, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F101, line 2881 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (2): STR-06-03, STR-06-03-part2
  * Coverage: source 5 entities and 4 relationships (entSessionLogTurn, strEntAgentResponse, strEntAgentContextPackage, strEntAgentResponseClaim, strEntAgentResponseEvidence); 4 of 4 relationships in the model with their cardinality; views show 5 of 5 entities and 4 of 4 relationships.
  * Drawn beyond the source (2): STR-06-03: entSessionLogTurn -> strEntAgentContextPackage "1 to 0..n: built for, by turn key"; STR-06-03: strEntAgentResponse -> strEntAgentResponse "0..1 to 0..1: SupersededByResponseId".
  * Labels merged (2): AgentResponse -> AgentResponseClaim "1 to 0..n: asserts" drawn as "1 to 0..n: asserts / claims"; AgentResponse -> AgentResponseEvidence "1 to 0..n: cites" drawn as "1 to 0..n: cites / evidence".
* F18, line 522, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F102, line 2891 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (2): STR-06-04, STR-06-04-part2
  * Coverage: source 5 entities and 4 relationships (strEntAgentResponse, strEntAgentResponseGrade, strEntAgentContextHvRequest, entSessionLogTurn, strEntAgentResponseGradeFinding); 4 of 4 relationships in the model with their cardinality; views show 5 of 5 entities and 4 of 4 relationships.
  * Drawn beyond the source (1): STR-06-04: strEntAgentResponse -> strEntAgentResponse "0..1 to 0..1: SupersededByResponseId".
  * Labels merged (3): AgentResponse -> AgentResponseGrade "1 to 0..n: graded by" drawn as "1 to 0..n: graded by / grades"; SessionLogTurn -> AgentResponseGrade "1 to 0..n: reviewer turn" drawn as "1 to 0..n: reviewer turn / reviewer turn key"; AgentResponseGrade -> AgentResponseGradeFinding "1 to 0..n: lists" drawn as "1 to 0..n: lists / findings".
* F19, line 532, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F103, line 2901 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (2): STR-06-05, STR-06-05-part2
  * Coverage: source 4 entities and 3 relationships (entSessionLog, strEntAgentContextHvRequest, strEntAgentContextHvRequestItem, strEntAgentResponse); 3 of 3 relationships in the model with their cardinality; views show 4 of 4 entities and 3 of 3 relationships.
  * Drawn beyond the source (1): STR-06-05-part2: strEntAgentResponse -> strEntAgentResponse "0..1 to 0..1: SupersededByResponseId".
  * Labels merged (1): AgentContextHvRequest -> AgentContextHvRequestItem "1 to 0..n: lists" drawn as "1 to 0..n: lists / items".
* F20, line 541, ER diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F104, line 2910 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (2): STR-06-06, STR-06-06-part2
  * Coverage: source 4 entities and 3 relationships (strEntAgentContextRelationship, strEntAgentContextPackage, strEntAgentResponse, strEntAgentContextProjection); 3 of 3 relationships in the model with their cardinality; views show 4 of 4 entities and 3 of 3 relationships.
  * Drawn beyond the source (2): STR-06-06: strEntAgentContextPackage -> strEntAgentResponse "1 to 0..n: answered by"; STR-06-06: strEntAgentResponse -> strEntAgentResponse "0..1 to 0..1: SupersededByResponseId".
  * Labels merged (1): AgentResponse -> AgentContextProjection "1 to 0..1: projected as" drawn as "1 to 0..1: projected as / projection".
* F21, line 550, state diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F105, line 2919 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (9): STR-06-07, STR-06-07-part2, STR-06-07-part3, STR-06-07-part4, STR-06-07-part5, STR-06-07-part6, STR-06-07-part7, STR-06-07-part8, STR-06-07-part9
  * Coverage: source 6 states, 2 start or end pseudo states, 19 transitions and 0 substate links; 8 of 8 State elements named by the state, 19 of 19 transitions as relationships named by their trigger; views show 8 of 8 elements and 19 of 19 transitions.
* F22, line 577, state diagram (6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`))
  * Same diagram as STR F106, line 2946 (byte-identical; STR heading "6.3 Entities (namespace `QBrainAi.Support.Mcp.Storage.Entities.AgentContext`)").
  * Views (6): STR-06-08, STR-06-08-part2, STR-06-08-part3, STR-06-08-part4, STR-06-08-part5, STR-06-08-part6
  * Coverage: source 6 states, 1 start or end pseudo states, 15 transitions and 0 substate links; 7 of 7 State elements named by the state, 15 of 15 transitions as relationships named by their trigger; views show 7 of 7 elements and 15 of 15 transitions.
* F23, line 691, activity flowchart (GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009])
  * Same diagram as STR F107, line 3060 (byte-identical; STR heading "GetAgentContextStatusQuery: get status [FR-MCP-AGENTCONTEXT-008, -009]").
  * Views (2): GetAgentContextStatusQuery, GetAgentContextStatusQuery-part2
  * Coverage: source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 4, Decision 0, Error 0, End 1), 6 of 6 with the node text, 5 of 5 edges as relationships with the edge label; views show 6 of 6 nodes and 5 of 5 edges.
* F24, line 714, activity flowchart (BuildAgentContextPackageCommand: build a context package [FR-MCP-AGENTCONTEXT-001, -002])
  * Same diagram as STR F108, line 3083 (byte-identical; STR heading "BuildAgentContextPackageCommand: build a context package [FR-MCP-AGENTCONTEXT-001, -002]").
  * Views (6): BuildAgentContextPackageCommand, BuildAgentContextPackageCommand-part2, BuildAgentContextPackageCommand-part3, BuildAgentContextPackageCommand-part4, BuildAgentContextPackageCommand-part5, BuildAgentContextPackageCommand-part6
  * Coverage: source 13 nodes and 16 edges; model 13 Activity elements (Start 1, Action 4, Decision 2, Error 4, End 2), 13 of 13 with the node text, 16 of 16 edges as relationships with the edge label; views show 13 of 13 nodes and 16 of 16 edges.
* F25, line 738, activity flowchart (Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite))
  * Same diagram as STR F109, line 3107 (byte-identical; STR heading "Nested activity: ReadBuildInputs (reads of BuildAgentContextPackageCommand, before its SerializableWrite)").
  * Views (4): ReadBuildInputs, ReadBuildInputs-part2, ReadBuildInputs-part3, ReadBuildInputs-part4
  * Coverage: source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 5, Decision 1, Error 4, End 1), 12 of 12 with the node text, 11 of 11 edges as relationships with the edge label; views show 12 of 12 nodes and 11 of 11 edges.
* F26, line 757, activity flowchart (Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler))
  * Same diagram as STR F110, line 3126 (byte-identical; STR heading "Nested activity: SavePackage (SerializableWrite of BuildAgentContextPackageCommand, after ReadBuildInputs and PackageAssembler)").
  * Views (5): SavePackage, SavePackage-part2, SavePackage-part3, SavePackage-part4, SavePackage-part5
  * Coverage: source 13 nodes and 14 edges; model 13 Activity elements (Start 1, Action 5, Decision 1, Error 3, End 3), 13 of 13 with the node text, 14 of 14 edges as relationships with the edge label; views show 13 of 13 nodes and 14 of 14 edges.
* F27, line 786, activity flowchart (Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery))
  * Same diagram as STR F111, line 3155 (byte-identical; STR heading "Nested activity: FillBudgetWithCandidates (loop of PackageAssembler, for BuildAgentContextPackageCommand and ReplayAgentContextPackageQuery)").
  * Views (3): FillBudgetWithCandidates, FillBudgetWithCandidates-part2, FillBudgetWithCandidates-part3
  * Coverage: source 9 nodes and 11 edges; model 9 Activity elements (Start 1, Action 4, Decision 3, Error 0, End 1), 9 of 9 with the node text, 11 of 11 edges as relationships with the edge label; views show 9 of 9 nodes and 11 of 11 edges.
* F28, line 807, activity flowchart (GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001])
  * Same diagram as STR F112, line 3176 (byte-identical; STR heading "GetAgentContextPackageQuery: get a package [FR-MCP-AGENTCONTEXT-001]").
  * Views (2): GetAgentContextPackageQuery, GetAgentContextPackageQuery-part2
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 1, End 1), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F29, line 825, activity flowchart (QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001])
  * Same diagram as STR F113, line 3194 (byte-identical; STR heading "QueryAgentContextPackagesQuery: query packages [FR-MCP-AGENTCONTEXT-001]").
  * Views (2): QueryAgentContextPackagesQuery, QueryAgentContextPackagesQuery-part2
  * Coverage: source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1), 5 of 5 with the node text, 5 of 5 edges as relationships with the edge label; views show 5 of 5 nodes and 5 of 5 edges.
* F30, line 841, activity flowchart (ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001])
  * Same diagram as STR F114, line 3210 (byte-identical; STR heading "ReplayAgentContextPackageQuery: replay a package [FR-MCP-AGENTCONTEXT-001]").
  * Views (5): ReplayAgentContextPackageQuery, ReplayAgentContextPackageQuery-part2, ReplayAgentContextPackageQuery-part3, ReplayAgentContextPackageQuery-part4, ReplayAgentContextPackageQuery-part5
  * Coverage: source 13 nodes and 12 edges; model 13 Activity elements (Start 1, Action 3, Decision 3, Error 4, End 2), 13 of 13 with the node text, 12 of 12 edges as relationships with the edge label; views show 13 of 13 nodes and 12 of 12 edges.
* F31, line 869, activity flowchart (SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003])
  * Same diagram as STR F115, line 3238 (byte-identical; STR heading "SubmitAgentResponseCommand: submit a response [FR-MCP-AGENTCONTEXT-003]").
  * Views (6): SubmitAgentResponseCommand, SubmitAgentResponseCommand-part2, SubmitAgentResponseCommand-part3, SubmitAgentResponseCommand-part4, SubmitAgentResponseCommand-part5, SubmitAgentResponseCommand-part6
  * Coverage: source 15 nodes and 17 edges; model 15 Activity elements (Start 1, Action 7, Decision 1, Error 4, End 2), 15 of 15 with the node text, 17 of 17 edges as relationships with the edge label; views show 15 of 15 nodes and 17 of 17 edges.
* F32, line 899, activity flowchart (Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite))
  * Same diagram as STR F116, line 3268 (byte-identical; STR heading "Nested activity: ReadSubmissionInputs (reads of SubmitAgentResponseCommand, before its SerializableWrite)").
  * Views (4): ReadSubmissionInputs, ReadSubmissionInputs-part2, ReadSubmissionInputs-part3, ReadSubmissionInputs-part4
  * Coverage: source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 2, Decision 1, Error 4, End 1), 9 of 9 with the node text, 9 of 9 edges as relationships with the edge label; views show 9 of 9 nodes and 9 of 9 edges.
* F33, line 919, activity flowchart (GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003])
  * Same diagram as STR F117, line 3288 (byte-identical; STR heading "GetAgentResponseQuery: get a response [FR-MCP-AGENTCONTEXT-003]").
  * Views (2): GetAgentResponseQuery, GetAgentResponseQuery-part2
  * Coverage: source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 1, Error 1, End 1), 6 of 6 with the node text, 5 of 5 edges as relationships with the edge label; views show 6 of 6 nodes and 5 of 5 edges.
* F34, line 936, activity flowchart (QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004])
  * Same diagram as STR F118, line 3305 (byte-identical; STR heading "QueryAgentResponsesQuery: query responses [FR-MCP-AGENTCONTEXT-003, -004]").
  * Views (3): QueryAgentResponsesQuery, QueryAgentResponsesQuery-part2, QueryAgentResponsesQuery-part3
  * Coverage: source 9 nodes and 10 edges; model 9 Activity elements (Start 1, Action 4, Decision 2, Error 1, End 1), 9 of 9 with the node text, 10 of 10 edges as relationships with the edge label; views show 9 of 9 nodes and 10 of 10 edges.
* F35, line 962, activity flowchart (RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F119, line 3331 (byte-identical; STR heading "RunDeterministicGradeCommand: run the deterministic grade [FR-MCP-AGENTCONTEXT-004]").
  * Views (6): RunDeterministicGradeCommand, RunDeterministicGradeCommand-part2, RunDeterministicGradeCommand-part3, RunDeterministicGradeCommand-part4, RunDeterministicGradeCommand-part5, RunDeterministicGradeCommand-part6
  * Coverage: source 15 nodes and 17 edges; model 15 Activity elements (Start 1, Action 8, Decision 2, Error 3, End 1), 15 of 15 with the node text, 17 of 17 edges as relationships with the edge label; views show 15 of 15 nodes and 17 of 17 edges.
* F36, line 990, activity flowchart (Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand))
  * Same diagram as STR F120, line 3359 (byte-identical; STR heading "Nested activity: CheckOneEvidenceItem (loop body of RunDeterministicGradeCommand)").
  * Views (3): CheckOneEvidenceItem, CheckOneEvidenceItem-part2, CheckOneEvidenceItem-part3
  * Coverage: source 9 nodes and 13 edges; model 9 Activity elements (Start 1, Action 4, Decision 3, Error 0, End 1), 9 of 9 with the node text, 13 of 13 edges as relationships with the edge label; views show 9 of 9 nodes and 13 of 13 edges.
* F37, line 1019, activity flowchart (RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F121, line 3388 (byte-identical; STR heading "RecordModelGradeCommand: record a model grade from an HV review [FR-MCP-AGENTCONTEXT-004]").
  * Views (5): RecordModelGradeCommand, RecordModelGradeCommand-part2, RecordModelGradeCommand-part3, RecordModelGradeCommand-part4, RecordModelGradeCommand-part5
  * Coverage: source 14 nodes and 16 edges; model 14 Activity elements (Start 1, Action 4, Decision 1, Error 6, End 2), 14 of 14 with the node text, 16 of 16 edges as relationships with the edge label; views show 14 of 14 nodes and 16 of 16 edges.
* F38, line 1043, activity flowchart (Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite))
  * Same diagram as STR F122, line 3412 (byte-identical; STR heading "Nested activity: ReadModelGradeInputs (reads of RecordModelGradeCommand, before its SerializableWrite)").
  * Views (3): ReadModelGradeInputs, ReadModelGradeInputs-part2, ReadModelGradeInputs-part3
  * Coverage: source 9 nodes and 8 edges; model 9 Activity elements (Start 1, Action 3, Decision 1, Error 3, End 1), 9 of 9 with the node text, 8 of 8 edges as relationships with the edge label; views show 9 of 9 nodes and 8 of 8 edges.
* F39, line 1059, activity flowchart (Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs))
  * Same diagram as STR F123, line 3428 (byte-identical; STR heading "Nested activity: CheckGradeTarget (response, claim, HV request, and item checks of ReadModelGradeInputs)").
  * Views (3): CheckGradeTarget, CheckGradeTarget-part2, CheckGradeTarget-part3
  * Coverage: source 12 nodes and 11 edges; model 12 Activity elements (Start 1, Action 0, Decision 4, Error 6, End 1), 12 of 12 with the node text, 11 of 11 edges as relationships with the edge label; views show 12 of 12 nodes and 11 of 11 edges.
* F40, line 1078, activity flowchart (Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs))
  * Same diagram as STR F124, line 3447 (byte-identical; STR heading "Nested activity: SaveModelGrade (SerializableWrite of RecordModelGradeCommand, after ReadModelGradeInputs)").
  * Views (4): SaveModelGrade, SaveModelGrade-part2, SaveModelGrade-part3, SaveModelGrade-part4
  * Coverage: source 11 nodes and 11 edges; model 11 Activity elements (Start 1, Action 4, Decision 1, Error 2, End 3), 11 of 11 with the node text, 11 of 11 edges as relationships with the edge label; views show 11 of 11 nodes and 11 of 11 edges.
* F41, line 1105, activity flowchart (DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F125, line 3474 (byte-identical; STR heading "DecideAgentResponseTrustCommand: decide trust (internal, after every grade) [FR-MCP-AGENTCONTEXT-004]").
  * Views (2): DecideAgentResponseTrustCommand, DecideAgentResponseTrustCommand-part2
  * Coverage: source 8 nodes and 8 edges; model 8 Activity elements (Start 1, Action 3, Decision 2, Error 0, End 2), 8 of 8 with the node text, 8 of 8 edges as relationships with the edge label; views show 8 of 8 nodes and 8 of 8 edges.
* F42, line 1124, activity flowchart (RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005])
  * Same diagram as STR F126, line 3493 (byte-identical; STR heading "RejectAgentResponseCommand: reject a response [FR-MCP-AGENTCONTEXT-005]").
  * Views (3): RejectAgentResponseCommand, RejectAgentResponseCommand-part2, RejectAgentResponseCommand-part3
  * Coverage: source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 2, Decision 2, Error 4, End 2), 11 of 11 with the node text, 10 of 10 edges as relationships with the edge label; views show 11 of 11 nodes and 10 of 10 edges.
* F43, line 1151, activity flowchart (SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005])
  * Same diagram as STR F127, line 3520 (byte-identical; STR heading "SupersedeAgentResponseCommand: supersede a response [FR-MCP-AGENTCONTEXT-005]").
  * Views (5): SupersedeAgentResponseCommand, SupersedeAgentResponseCommand-part2, SupersedeAgentResponseCommand-part3, SupersedeAgentResponseCommand-part4, SupersedeAgentResponseCommand-part5
  * Coverage: source 13 nodes and 13 edges; model 13 Activity elements (Start 1, Action 3, Decision 2, Error 5, End 2), 13 of 13 with the node text, 13 of 13 edges as relationships with the edge label; views show 13 of 13 nodes and 13 of 13 edges.
* F44, line 1182, activity flowchart (EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005])
  * Same diagram as STR F128, line 3551 (byte-identical; STR heading "EvaluateStalenessCommand: evaluate staleness [FR-MCP-AGENTCONTEXT-005]").
  * Views (2): EvaluateStalenessCommand, EvaluateStalenessCommand-part2
  * Coverage: source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 2, End 1), 6 of 6 with the node text, 5 of 5 edges as relationships with the edge label; views show 6 of 6 nodes and 5 of 5 edges.
* F45, line 1195, activity flowchart (Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand))
  * Same diagram as STR F129, line 3564 (byte-identical; STR heading "Nested activity: EvaluateOneResponseStaleness (loop body of EvaluateStalenessCommand)").
  * Views (3): EvaluateOneResponseStaleness, EvaluateOneResponseStaleness-part2, EvaluateOneResponseStaleness-part3
  * Coverage: source 7 nodes and 8 edges; model 7 Activity elements (Start 1, Action 3, Decision 1, Error 1, End 1), 7 of 7 with the node text, 8 of 8 edges as relationships with the edge label; views show 7 of 7 nodes and 8 of 8 edges.
* F46, line 1218, activity flowchart (CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006])
  * Same diagram as STR F130, line 3587 (byte-identical; STR heading "CreateAgentContextRelationshipCommand: create a relationship [FR-MCP-AGENTCONTEXT-006]").
  * Views (3): CreateAgentContextRelationshipCommand, CreateAgentContextRelationshipCommand-part2, CreateAgentContextRelationshipCommand-part3
  * Coverage: source 11 nodes and 10 edges; model 11 Activity elements (Start 1, Action 3, Decision 1, Error 4, End 2), 11 of 11 with the node text, 10 of 10 edges as relationships with the edge label; views show 11 of 11 nodes and 10 of 10 edges.
* F47, line 1242, activity flowchart (DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006])
  * Same diagram as STR F131, line 3611 (byte-identical; STR heading "DeleteAgentContextRelationshipCommand: delete a relationship [FR-MCP-AGENTCONTEXT-006]").
  * Views (2): DeleteAgentContextRelationshipCommand, DeleteAgentContextRelationshipCommand-part2
  * Coverage: source 7 nodes and 6 edges; model 7 Activity elements (Start 1, Action 2, Decision 1, Error 2, End 1), 7 of 7 with the node text, 6 of 6 edges as relationships with the edge label; views show 7 of 7 nodes and 6 of 6 edges.
* F48, line 1259, activity flowchart (QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006])
  * Same diagram as STR F132, line 3628 (byte-identical; STR heading "QueryAgentContextRelationshipsQuery: query relationships [FR-MCP-AGENTCONTEXT-006]").
  * Views (4): QueryAgentContextRelationshipsQuery, QueryAgentContextRelationshipsQuery-part2, QueryAgentContextRelationshipsQuery-part3, QueryAgentContextRelationshipsQuery-part4
  * Coverage: source 9 nodes and 11 edges; model 9 Activity elements (Start 1, Action 4, Decision 2, Error 1, End 1), 9 of 9 with the node text, 11 of 11 edges as relationships with the edge label; views show 9 of 9 nodes and 11 of 11 edges.
* F49, line 1282, activity flowchart (RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007])
  * Same diagram as STR F133, line 3651 (byte-identical; STR heading "RetrieveAgentContextCandidatesQuery: retrieve candidate context [FR-MCP-AGENTCONTEXT-007]").
  * Views (10): RetrieveAgentContextCandidatesQuery, RetrieveAgentContextCandidatesQuery-part2, RetrieveAgentContextCandidatesQuery-part3, RetrieveAgentContextCandidatesQuery-part4, RetrieveAgentContextCandidatesQuery-part5, RetrieveAgentContextCandidatesQuery-part6, RetrieveAgentContextCandidatesQuery-part7, RetrieveAgentContextCandidatesQuery-part8, RetrieveAgentContextCandidatesQuery-part9, RetrieveAgentContextCandidatesQuery-part10
  * Coverage: source 23 nodes and 29 edges; model 23 Activity elements (Start 1, Action 12, Decision 7, Error 2, End 1), 23 of 23 with the node text, 29 of 29 edges as relationships with the edge label; views show 23 of 23 nodes and 29 of 29 edges.
* F50, line 1324, activity flowchart (ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007])
  * Same diagram as STR F134, line 3693 (byte-identical; STR heading "ProjectAgentResponsesCommand: project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]").
  * Views (2): ProjectAgentResponsesCommand, ProjectAgentResponsesCommand-part2
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 3, Decision 0, Error 2, End 1), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F51, line 1337, activity flowchart (Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand))
  * Same diagram as STR F135, line 3706 (byte-identical; STR heading "Nested activity: ProjectOneResponse (loop body of ProjectAgentResponsesCommand)").
  * Views (5): ProjectOneResponse, ProjectOneResponse-part2, ProjectOneResponse-part3, ProjectOneResponse-part4, ProjectOneResponse-part5
  * Coverage: source 12 nodes and 17 edges; model 12 Activity elements (Start 1, Action 4, Decision 5, Error 1, End 1), 12 of 12 with the node text, 17 of 17 edges as relationships with the edge label; views show 12 of 12 nodes and 17 of 17 edges.
* F52, line 1360, activity flowchart (Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand))
  * Same diagram as STR F136, line 3729 (byte-identical; STR heading "Nested activity: ReconcileProjectedDocuments (after the loop of ProjectAgentResponsesCommand)").
  * Views (3): ReconcileProjectedDocuments, ReconcileProjectedDocuments-part2, ReconcileProjectedDocuments-part3
  * Coverage: source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 4, Decision 3, Error 1, End 2), 11 of 11 with the node text, 12 of 12 edges as relationships with the edge label; views show 11 of 11 nodes and 12 of 12 edges.
* F53, line 1383, activity flowchart (GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009])
  * Same diagram as STR F137, line 3752 (byte-identical; STR heading "GetAgentContextPilotReportQuery: pilot report [FR-MCP-AGENTCONTEXT-009]").
  * Views (2): GetAgentContextPilotReportQuery, GetAgentContextPilotReportQuery-part2
  * Coverage: source 8 nodes and 7 edges; model 8 Activity elements (Start 1, Action 4, Decision 1, Error 1, End 1), 8 of 8 with the node text, 7 of 7 edges as relationships with the edge label; views show 8 of 8 nodes and 7 of 7 edges.
* F54, line 1407, activity flowchart (CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F138, line 3776 (byte-identical; STR heading "CreateAgentContextHvRequestCommand: create an HV request for a range of turns [FR-MCP-AGENTCONTEXT-004]").
  * Views (4): CreateAgentContextHvRequestCommand, CreateAgentContextHvRequestCommand-part2, CreateAgentContextHvRequestCommand-part3, CreateAgentContextHvRequestCommand-part4
  * Coverage: source 12 nodes and 12 edges; model 12 Activity elements (Start 1, Action 4, Decision 1, Error 4, End 2), 12 of 12 with the node text, 12 of 12 edges as relationships with the edge label; views show 12 of 12 nodes and 12 of 12 edges.
* F55, line 1432, activity flowchart (Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand))
  * Same diagram as STR F139, line 3801 (byte-identical; STR heading "Nested activity: SelectHvRequestItems (turn, range, and item selection inside the SerializableWrite delegate of CreateAgentContextHvRequestCommand)").
  * Views (3): SelectHvRequestItems, SelectHvRequestItems-part2, SelectHvRequestItems-part3
  * Coverage: source 8 nodes and 10 edges; model 8 Activity elements (Start 1, Action 2, Decision 2, Error 2, End 1), 8 of 8 with the node text, 10 of 10 edges as relationships with the edge label; views show 8 of 8 nodes and 10 of 10 edges.
* F56, line 1452, activity flowchart (GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F140, line 3821 (byte-identical; STR heading "GetAgentContextHvRequestQuery: get an HV request [FR-MCP-AGENTCONTEXT-004]").
  * Views (2): GetAgentContextHvRequestQuery, GetAgentContextHvRequestQuery-part2
  * Coverage: source 5 nodes and 4 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1), 5 of 5 with the node text, 4 of 4 edges as relationships with the edge label; views show 5 of 5 nodes and 4 of 4 edges.
* F57, line 1467, activity flowchart (QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004])
  * Same diagram as STR F141, line 3836 (byte-identical; STR heading "QueryAgentContextHvRequestsQuery: query HV requests [FR-MCP-AGENTCONTEXT-004]").
  * Views (2): QueryAgentContextHvRequestsQuery, QueryAgentContextHvRequestsQuery-part2
  * Coverage: source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 1, Decision 1, Error 1, End 1), 5 of 5 with the node text, 5 of 5 edges as relationships with the edge label; views show 5 of 5 nodes and 5 of 5 edges.
* F58, line 1494, activity flowchart (Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009])
  * Same diagram as STR F142, line 3863 (byte-identical; STR heading "Process RunStatelessInvocation: run a stateless invocation (plugin process) [FR-MCP-AGENTCONTEXT-009]").
  * Views (6): RunStatelessInvocation, RunStatelessInvocation-part2, RunStatelessInvocation-part3, RunStatelessInvocation-part4, RunStatelessInvocation-part5, RunStatelessInvocation-part6
  * Coverage: source 12 nodes and 14 edges; model 12 Activity elements (Start 1, Action 5, Decision 1, Error 2, End 3), 12 of 12 with the node text, 14 of 14 edges as relationships with the edge label; views show 12 of 12 nodes and 14 of 14 edges.
* F59, line 1516, activity flowchart (Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build))
  * Same diagram as STR F143, line 3885 (byte-identical; STR heading "Nested activity: ResolveInvocationTurn (Mode, host, trigger, and turn checks of RunStatelessInvocation, before the build)").
  * Views (5): ResolveInvocationTurn, ResolveInvocationTurn-part2, ResolveInvocationTurn-part3, ResolveInvocationTurn-part4, ResolveInvocationTurn-part5
  * Coverage: source 15 nodes and 14 edges; model 15 Activity elements (Start 1, Action 2, Decision 3, Error 3, End 6), 15 of 15 with the node text, 14 of 14 edges as relationships with the edge label; views show 15 of 15 nodes and 14 of 14 edges.
* F60, line 1538, activity flowchart (Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation))
  * Same diagram as STR F144, line 3907 (byte-identical; STR heading "Nested activity: FailInvocation (refusal and canceled close of RunStatelessInvocation)").
  * Views (2): FailInvocation, FailInvocation-part2
  * Coverage: source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 2, Decision 0, Error 3, End 0), 6 of 6 with the node text, 5 of 5 edges as relationships with the edge label; views show 6 of 6 nodes and 5 of 5 edges.
* F61, line 1551, activity flowchart (Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation))
  * Same diagram as STR F145, line 3920 (byte-identical; STR heading "Nested activity: AnswerAndSubmit (model answer, submission, and grade request of RunStatelessInvocation)").
  * Views (5): AnswerAndSubmit, AnswerAndSubmit-part2, AnswerAndSubmit-part3, AnswerAndSubmit-part4, AnswerAndSubmit-part5
  * Coverage: source 13 nodes and 13 edges; model 13 Activity elements (Start 1, Action 5, Decision 3, Error 2, End 2), 13 of 13 with the node text, 13 of 13 edges as relationships with the edge label; views show 13 of 13 nodes and 13 of 13 edges.
* F62, line 1588, sequence diagram (Sequence: RequestPipeline (every Phase C dispatch))
  * Same diagram as STR F146, line 3957 (byte-identical; STR heading "Sequence: RequestPipeline (every Phase C dispatch)").
  * Views (13): RequestPipeline-Sequence, RequestPipeline-Sequence-part2, RequestPipeline-Sequence-part3, RequestPipeline-Sequence-part4, RequestPipeline-Sequence-alt1-1, RequestPipeline-Sequence-alt1-1-part2, RequestPipeline-Sequence-alt1-2, RequestPipeline-Sequence-alt2-1, RequestPipeline-Sequence-alt2-1-part2, RequestPipeline-Sequence-alt2-1-part3, RequestPipeline-Sequence-alt2-2, RequestPipeline-Sequence-alt2-2-part2, RequestPipeline-Sequence-alt2-2-part3
  * Coverage: source 5 participants, 4 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 4 branches; 4 branch view families (4 open with their condition); view steps match 22 of 22 expected in order (including 2 continuation steps).
* F63, line 1622, sequence diagram (Sequence: submit GetAgentContextStatusQuery)
  * Same diagram as STR F147, line 3991 (byte-identical; STR heading "Sequence: submit GetAgentContextStatusQuery").
  * Views (10): STR-06-09, STR-06-09-alt1-1, STR-06-09-alt1-1-part2, STR-06-09-alt1-1-part3, STR-06-09-alt1-1-part4, STR-06-09-alt1-1-part5, STR-06-09-alt1-1-part6, STR-06-09-alt1-1-part7, STR-06-09-alt1-2, STR-06-09-alt1-2-part2
  * Coverage: source 5 participants, 4 messages, 3 replies, 1 self messages, 4 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 16 of 16 expected in order (including 1 continuation steps).
* F64, line 1651, sequence diagram (Sequence: submit BuildAgentContextPackageCommand)
  * Same diagram as STR F148, line 4020 (byte-identical; STR heading "Sequence: submit BuildAgentContextPackageCommand").
  * Views (14): STR-06-10, STR-06-10-part2, STR-06-10-part3, STR-06-10-alt1-1, STR-06-10-alt1-2, STR-06-10-alt1-2-part2, STR-06-10-alt2-1, STR-06-10-alt2-2, STR-06-10-alt2-3, STR-06-10-alt2-4, STR-06-10-alt2-4-part2, STR-06-10-alt2-4-part3, STR-06-10-alt2-4-part4, STR-06-10-alt2-4-part5
  * Coverage: source 4 participants, 3 messages, 4 replies, 1 self messages, 7 notes, 2 blocks with 6 branches; 6 branch view families (6 open with their condition); view steps match 25 of 25 expected in order (including 2 continuation steps).
* F65, line 1687, sequence diagram (Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction))
  * Same diagram as STR F149, line 4056 (byte-identical; STR heading "Nested sequence: ReadBuildInputs (reads of the BuildAgentContextPackageCommand sequence, before any transaction)").
  * Views (12): ReadBuildInputs-Sequence, ReadBuildInputs-Sequence-part2, ReadBuildInputs-Sequence-alt1-1, ReadBuildInputs-Sequence-alt1-2, ReadBuildInputs-Sequence-alt1-3, ReadBuildInputs-Sequence-alt1-3-part2, ReadBuildInputs-Sequence-alt2-1, ReadBuildInputs-Sequence-alt2-2, ReadBuildInputs-Sequence-alt2-3, ReadBuildInputs-Sequence-alt2-3-part2, ReadBuildInputs-Sequence-alt2-3-part3, ReadBuildInputs-Sequence-alt2-3-part4
  * Coverage: source 4 participants, 1 messages, 4 replies, 3 self messages, 4 notes, 2 blocks with 6 branches; 6 branch view families (6 open with their condition); view steps match 20 of 20 expected in order (including 0 continuation steps).
* F66, line 1720, sequence diagram (Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence))
  * Same diagram as STR F150, line 4089 (byte-identical; STR heading "Nested sequence: SaveBuiltPackage (the SerializableWrite of the BuildAgentContextPackageCommand sequence)").
  * Views (15): SaveBuiltPackage, SaveBuiltPackage-part2, SaveBuiltPackage-alt1-1, SaveBuiltPackage-alt1-2, SaveBuiltPackage-alt1-3, SaveBuiltPackage-alt1-4, SaveBuiltPackage-alt1-4-part2, SaveBuiltPackage-alt1-4-part3, SaveBuiltPackage-alt1-5, SaveBuiltPackage-alt2-1, SaveBuiltPackage-alt2-1-part2, SaveBuiltPackage-alt2-2, SaveBuiltPackage-alt2-2-part2, SaveBuiltPackage-alt1-6, SaveBuiltPackage-alt1-6-part2
  * Coverage: source 4 participants, 4 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 8 branches; 8 branch view families (8 open with their condition); view steps match 26 of 26 expected in order (including 2 continuation steps).
* F67, line 1755, sequence diagram (Sequence: submit GetAgentContextPackageQuery)
  * Same diagram as STR F151, line 4124 (byte-identical; STR heading "Sequence: submit GetAgentContextPackageQuery").
  * Views (9): STR-06-11, STR-06-11-part2, STR-06-11-part3, STR-06-11-part4, STR-06-11-part5, STR-06-11-alt1-1, STR-06-11-alt1-2, STR-06-11-alt1-2-part2, STR-06-11-alt1-2-part3
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 3 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 14 of 14 expected in order (including 2 continuation steps).
* F68, line 1778, sequence diagram (Sequence: submit QueryAgentContextPackagesQuery)
  * Same diagram as STR F152, line 4147 (byte-identical; STR heading "Sequence: submit QueryAgentContextPackagesQuery").
  * Views (7): STR-06-12, STR-06-12-part2, STR-06-12-part3, STR-06-12-part4, STR-06-12-alt1-1, STR-06-12-alt1-2, STR-06-12-alt1-2-part2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 12 of 12 expected in order (including 1 continuation steps).
* F69, line 1802, sequence diagram (Sequence: submit ReplayAgentContextPackageQuery)
  * Same diagram as STR F153, line 4171 (byte-identical; STR heading "Sequence: submit ReplayAgentContextPackageQuery").
  * Views (16): STR-06-13, STR-06-13-part2, STR-06-13-part3, STR-06-13-part4, STR-06-13-part5, STR-06-13-alt1-1, STR-06-13-alt1-2, STR-06-13-alt1-3, STR-06-13-alt1-4, STR-06-13-alt1-5, STR-06-13-alt1-5-part2, STR-06-13-alt1-5-part3, STR-06-13-alt2-1, STR-06-13-alt2-1-part2, STR-06-13-alt2-2, STR-06-13-alt2-2-part2
  * Coverage: source 5 participants, 4 messages, 6 replies, 0 self messages, 5 notes, 2 blocks with 7 branches; 7 branch view families (7 open with their condition); view steps match 25 of 25 expected in order (including 1 continuation steps).
* F70, line 1840, sequence diagram (Sequence: submit SubmitAgentResponseCommand)
  * Same diagram as STR F154, line 4209 (byte-identical; STR heading "Sequence: submit SubmitAgentResponseCommand").
  * Views (20): STR-06-14, STR-06-14-part2, STR-06-14-part3, STR-06-14-alt1-1, STR-06-14-alt1-1-part2, STR-06-14-alt1-2, STR-06-14-alt1-2-part2, STR-06-14-alt2-1, STR-06-14-alt2-2, STR-06-14-alt2-3, STR-06-14-alt2-3-part2, STR-06-14-alt2-3-part3, STR-06-14-alt2-3-part4, STR-06-14-alt2-3-part5, STR-06-14-alt3-1, STR-06-14-alt3-2, STR-06-14-alt3-3, STR-06-14-alt3-3-part2, STR-06-14-alt3-4, STR-06-14-alt3-4-part2
  * Coverage: source 4 participants, 5 messages, 7 replies, 1 self messages, 7 notes, 3 blocks with 9 branches; 9 branch view families (9 open with their condition); view steps match 35 of 35 expected in order (including 3 continuation steps).
* F71, line 1885, sequence diagram (Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction))
  * Same diagram as STR F155, line 4254 (byte-identical; STR heading "Nested sequence: ReadSubmissionInputs (reads of the SubmitAgentResponseCommand sequence, before any transaction)").
  * Views (12): ReadSubmissionInputs-Sequence, ReadSubmissionInputs-Sequence-part2, ReadSubmissionInputs-Sequence-alt1-1, ReadSubmissionInputs-Sequence-alt1-2, ReadSubmissionInputs-Sequence-alt1-3, ReadSubmissionInputs-Sequence-alt1-3-part2, ReadSubmissionInputs-Sequence-alt1-4, ReadSubmissionInputs-Sequence-alt1-4-part2, ReadSubmissionInputs-Sequence-alt2-1, ReadSubmissionInputs-Sequence-alt2-2, ReadSubmissionInputs-Sequence-alt2-3, ReadSubmissionInputs-Sequence-alt2-3-part2
  * Coverage: source 4 participants, 1 messages, 5 replies, 1 self messages, 2 notes, 2 blocks with 7 branches; 7 branch view families (7 open with their condition); view steps match 19 of 19 expected in order (including 1 continuation steps).
* F72, line 1916, sequence diagram (Sequence: submit GetAgentResponseQuery)
  * Same diagram as STR F156, line 4285 (byte-identical; STR heading "Sequence: submit GetAgentResponseQuery").
  * Views (10): STR-06-15, STR-06-15-part2, STR-06-15-part3, STR-06-15-part4, STR-06-15-part5, STR-06-15-alt1-1, STR-06-15-alt1-2, STR-06-15-alt1-2-part2, STR-06-15-alt1-2-part3, STR-06-15-alt1-2-part4
  * Coverage: source 4 participants, 3 messages, 2 replies, 1 self messages, 3 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 13 of 13 expected in order (including 1 continuation steps).
* F73, line 1941, sequence diagram (Sequence: submit QueryAgentResponsesQuery)
  * Same diagram as STR F157, line 4310 (byte-identical; STR heading "Sequence: submit QueryAgentResponsesQuery").
  * Views (9): STR-06-16, STR-06-16-part2, STR-06-16-part3, STR-06-16-part4, STR-06-16-alt1-1, STR-06-16-alt1-2, STR-06-16-alt1-2-part2, STR-06-16-alt1-2-part3, STR-06-16-alt1-2-part4
  * Coverage: source 4 participants, 3 messages, 3 replies, 1 self messages, 3 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 14 of 14 expected in order (including 1 continuation steps).
* F74, line 1967, sequence diagram (Sequence: submit RunDeterministicGradeCommand)
  * Same diagram as STR F158, line 4336 (byte-identical; STR heading "Sequence: submit RunDeterministicGradeCommand").
  * Views (23): STR-06-17, STR-06-17-part2, STR-06-17-part3, STR-06-17-part4, STR-06-17-alt1-1, STR-06-17-alt1-2, STR-06-17-alt1-3, STR-06-17-alt1-3-part2, STR-06-17-alt1-3-part3, STR-06-17-alt1-3-part4, STR-06-17-alt1-3-part5, STR-06-17-alt1-3-part6, STR-06-17-alt1-3-part7, STR-06-17-alt1-3-part8, STR-06-17-alt1-3-part9, STR-06-17-alt2-1, STR-06-17-alt2-1-part2, STR-06-17-alt2-2, STR-06-17-alt2-2-part2, STR-06-17-alt2-3, STR-06-17-alt2-3-part2, STR-06-17-alt1-4, STR-06-17-alt1-4-part2
  * Coverage: source 4 participants, 4 messages, 4 replies, 3 self messages, 8 notes, 2 blocks with 7 branches; 7 branch view families (7 open with their condition); view steps match 30 of 30 expected in order (including 2 continuation steps).
* F75, line 2008, sequence diagram (Sequence: submit RecordModelGradeCommand from an HV review)
  * Same diagram as STR F159, line 4377 (byte-identical; STR heading "Sequence: submit RecordModelGradeCommand from an HV review").
  * Views (10): STR-06-18, STR-06-18-part2, STR-06-18-part3, STR-06-18-part4, STR-06-18-part5, STR-06-18-part6, STR-06-18-part7, STR-06-18-part8, STR-06-18-loop1, STR-06-18-loop1-part2
  * Coverage: source 4 participants, 4 messages, 3 replies, 0 self messages, 5 notes, 1 blocks with 1 branches; 1 branch view families (1 open with their condition); view steps match 17 of 17 expected in order (including 3 continuation steps).
* F76, line 2035, sequence diagram (Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence))
  * Same diagram as STR F160, line 4404 (byte-identical; STR heading "Nested sequence: SubmitOneModelGrade (loop body of the RecordModelGradeCommand sequence)").
  * Views (27): SubmitOneModelGrade, SubmitOneModelGrade-alt1-1, SubmitOneModelGrade-alt1-2, SubmitOneModelGrade-alt1-2-part2, SubmitOneModelGrade-alt1-2-part3, SubmitOneModelGrade-alt2-1, SubmitOneModelGrade-alt2-1-part2, SubmitOneModelGrade-alt2-2, SubmitOneModelGrade-alt2-3, SubmitOneModelGrade-alt2-4, SubmitOneModelGrade-alt2-4-part2, SubmitOneModelGrade-alt2-4-part3, SubmitOneModelGrade-alt2-4-part4, SubmitOneModelGrade-alt2-4-part5, SubmitOneModelGrade-alt3-1, SubmitOneModelGrade-alt3-2, SubmitOneModelGrade-alt3-3, SubmitOneModelGrade-alt3-3-part2, SubmitOneModelGrade-alt3-3-part3, SubmitOneModelGrade-alt4-1, SubmitOneModelGrade-alt4-1-part2, SubmitOneModelGrade-alt4-1-part3, SubmitOneModelGrade-alt4-2, SubmitOneModelGrade-alt4-2-part2, SubmitOneModelGrade-alt4-3, SubmitOneModelGrade-alt4-3-part2, SubmitOneModelGrade-alt3-4
  * Coverage: source 4 participants, 4 messages, 8 replies, 1 self messages, 7 notes, 4 blocks with 13 branches; 13 branch view families (13 open with their condition); view steps match 41 of 41 expected in order (including 4 continuation steps).
* F77, line 2085, sequence diagram (Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction))
  * Same diagram as STR F161, line 4454 (byte-identical; STR heading "Nested sequence: ReadModelGradeInputs (reads of the SubmitOneModelGrade sequence, before any transaction)").
  * Views (20): ReadModelGradeInputs-Sequence, ReadModelGradeInputs-Sequence-part2, ReadModelGradeInputs-Sequence-alt1-1, ReadModelGradeInputs-Sequence-alt1-2, ReadModelGradeInputs-Sequence-alt1-3, ReadModelGradeInputs-Sequence-alt1-3-part2, ReadModelGradeInputs-Sequence-alt2-1, ReadModelGradeInputs-Sequence-alt2-2, ReadModelGradeInputs-Sequence-alt2-3, ReadModelGradeInputs-Sequence-alt2-4, ReadModelGradeInputs-Sequence-alt2-5, ReadModelGradeInputs-Sequence-alt2-6, ReadModelGradeInputs-Sequence-alt2-7, ReadModelGradeInputs-Sequence-alt2-7-part2, ReadModelGradeInputs-Sequence-alt3-1, ReadModelGradeInputs-Sequence-alt3-2, ReadModelGradeInputs-Sequence-alt3-2-part2, ReadModelGradeInputs-Sequence-alt4-1, ReadModelGradeInputs-Sequence-alt4-2, ReadModelGradeInputs-Sequence-alt4-2-part2
  * Coverage: source 4 participants, 2 messages, 10 replies, 2 self messages, 5 notes, 4 blocks with 14 branches; 14 branch view families (14 open with their condition); view steps match 37 of 37 expected in order (including 0 continuation steps).
* F78, line 2135, sequence diagram (Sequence: submit RejectAgentResponseCommand)
  * Same diagram as STR F162, line 4504 (byte-identical; STR heading "Sequence: submit RejectAgentResponseCommand").
  * Views (12): STR-06-19, STR-06-19-part2, STR-06-19-part3, STR-06-19-alt1-1, STR-06-19-alt1-2, STR-06-19-alt1-2-part2, STR-06-19-alt1-2-part3, STR-06-19-alt2-1, STR-06-19-alt2-2, STR-06-19-alt2-3, STR-06-19-alt2-4, STR-06-19-alt2-5
  * Coverage: source 4 participants, 3 messages, 6 replies, 0 self messages, 4 notes, 2 blocks with 7 branches; 7 branch view families (7 open with their condition); view steps match 22 of 22 expected in order (including 0 continuation steps).
* F79, line 2170, sequence diagram (Sequence: submit SupersedeAgentResponseCommand)
  * Same diagram as STR F163, line 4539 (byte-identical; STR heading "Sequence: submit SupersedeAgentResponseCommand").
  * Views (20): STR-06-20, STR-06-20-part2, STR-06-20-part3, STR-06-20-alt1-1, STR-06-20-alt1-2, STR-06-20-alt1-2-part2, STR-06-20-alt1-2-part3, STR-06-20-alt1-2-part4, STR-06-20-alt2-1, STR-06-20-alt2-2, STR-06-20-alt2-3, STR-06-20-alt2-3-part2, STR-06-20-alt3-1, STR-06-20-alt3-2, STR-06-20-alt3-2-part2, STR-06-20-alt3-3, STR-06-20-alt3-3-part2, STR-06-20-alt4-1, STR-06-20-alt4-2, STR-06-20-alt2-4
  * Coverage: source 4 participants, 5 messages, 8 replies, 0 self messages, 6 notes, 4 blocks with 11 branches; 11 branch view families (11 open with their condition); view steps match 36 of 36 expected in order (including 2 continuation steps).
* F80, line 2217, sequence diagram (Sequence: submit EvaluateStalenessCommand)
  * Same diagram as STR F164, line 4586 (byte-identical; STR heading "Sequence: submit EvaluateStalenessCommand").
  * Views (16): STR-06-21, STR-06-21-part2, STR-06-21-part3, STR-06-21-part4, STR-06-21-alt1-1, STR-06-21-alt1-2, STR-06-21-alt1-2-part2, STR-06-21-alt1-2-part3, STR-06-21-loop1, STR-06-21-loop1-part2, STR-06-21-loop1-part3, STR-06-21-alt2-1, STR-06-21-alt2-1-part2, STR-06-21-alt2-1-part3, STR-06-21-alt2-1-part4, STR-06-21-alt2-2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 5 notes, 3 blocks with 5 branches; 5 branch view families (5 open with their condition); view steps match 22 of 22 expected in order (including 3 continuation steps).
* F81, line 2249, sequence diagram (Sequence: submit CreateAgentContextRelationshipCommand)
  * Same diagram as STR F165, line 4618 (byte-identical; STR heading "Sequence: submit CreateAgentContextRelationshipCommand").
  * Views (14): STR-06-22, STR-06-22-part2, STR-06-22-part3, STR-06-22-alt1-1, STR-06-22-alt1-2, STR-06-22-alt1-2-part2, STR-06-22-alt2-1, STR-06-22-alt2-2, STR-06-22-alt2-2-part2, STR-06-22-alt2-2-part3, STR-06-22-alt3-1, STR-06-22-alt3-2, STR-06-22-alt3-3, STR-06-22-alt3-4
  * Coverage: source 5 participants, 4 messages, 6 replies, 0 self messages, 3 notes, 3 blocks with 8 branches; 8 branch view families (8 open with their condition); view steps match 24 of 24 expected in order (including 0 continuation steps).
* F82, line 2287, sequence diagram (Sequence: submit DeleteAgentContextRelationshipCommand)
  * Same diagram as STR F166, line 4656 (byte-identical; STR heading "Sequence: submit DeleteAgentContextRelationshipCommand").
  * Views (8): STR-06-23, STR-06-23-part2, STR-06-23-part3, STR-06-23-part4, STR-06-23-alt1-1, STR-06-23-alt1-2, STR-06-23-alt1-3, STR-06-23-alt1-3-part2
  * Coverage: source 4 participants, 4 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 3 branches; 3 branch view families (3 open with their condition); view steps match 13 of 13 expected in order (including 0 continuation steps).
* F83, line 2311, sequence diagram (Sequence: submit QueryAgentContextRelationshipsQuery)
  * Same diagram as STR F167, line 4680 (byte-identical; STR heading "Sequence: submit QueryAgentContextRelationshipsQuery").
  * Views (8): STR-06-24, STR-06-24-part2, STR-06-24-part3, STR-06-24-part4, STR-06-24-alt1-1, STR-06-24-alt1-1-part2, STR-06-24-alt1-2, STR-06-24-alt1-2-part2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 13 of 13 expected in order (including 2 continuation steps).
* F84, line 2335, sequence diagram (Sequence: submit RetrieveAgentContextCandidatesQuery)
  * Same diagram as STR F168, line 4704 (byte-identical; STR heading "Sequence: submit RetrieveAgentContextCandidatesQuery").
  * Views (17): STR-06-25, STR-06-25-part2, STR-06-25-part3, STR-06-25-part4, STR-06-25-alt1-1, STR-06-25-alt1-1-part2, STR-06-25-alt1-2, STR-06-25-alt1-2-part2, STR-06-25-opt1, STR-06-25-alt2-1, STR-06-25-alt2-2, STR-06-25-alt2-3, STR-06-25-alt2-3-part2, STR-06-25-alt2-3-part3, STR-06-25-alt2-3-part4, STR-06-25-alt2-3-part5, STR-06-25-alt2-3-part6
  * Coverage: source 4 participants, 3 messages, 4 replies, 2 self messages, 3 notes, 3 blocks with 6 branches; 6 branch view families (6 open with their condition); view steps match 24 of 24 expected in order (including 3 continuation steps).
* F85, line 2369, sequence diagram (Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence))
  * Same diagram as STR F169, line 4738 (byte-identical; STR heading "Nested sequence: ChooseRetrievalPath (path choice of the RetrieveAgentContextCandidatesQuery sequence)").
  * Views (10): ChooseRetrievalPath, ChooseRetrievalPath-part2, ChooseRetrievalPath-part3, ChooseRetrievalPath-opt1, ChooseRetrievalPath-alt1-1, ChooseRetrievalPath-alt2-1, ChooseRetrievalPath-alt2-2, ChooseRetrievalPath-alt2-2-part2, ChooseRetrievalPath-alt1-2, ChooseRetrievalPath-alt1-2-part2
  * Coverage: source 3 participants, 4 messages, 5 replies, 0 self messages, 1 notes, 3 blocks with 5 branches; 5 branch view families (5 open with their condition); view steps match 20 of 20 expected in order (including 2 continuation steps).
* F86, line 2399, sequence diagram (Sequence: submit ProjectAgentResponsesCommand)
  * Same diagram as STR F170, line 4768 (byte-identical; STR heading "Sequence: submit ProjectAgentResponsesCommand").
  * Views (17): STR-06-26, STR-06-26-part2, STR-06-26-part3, STR-06-26-part4, STR-06-26-alt1-1, STR-06-26-alt1-2, STR-06-26-alt1-2-part2, STR-06-26-alt1-2-part3, STR-06-26-alt1-2-part4, STR-06-26-alt1-2-part5, STR-06-26-alt1-2-part6, STR-06-26-alt1-2-part7, STR-06-26-alt1-2-part8, STR-06-26-loop1, STR-06-26-loop1-part2, STR-06-26-alt2-1, STR-06-26-alt2-2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 5 notes, 3 blocks with 5 branches; 5 branch view families (5 open with their condition); view steps match 24 of 24 expected in order (including 5 continuation steps).
* F87, line 2431, sequence diagram (Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence))
  * Same diagram as STR F171, line 4800 (byte-identical; STR heading "Nested sequence: ProjectOneResponse (loop body of the ProjectAgentResponsesCommand sequence)").
  * Views (36): ProjectOneResponse-Sequence, ProjectOneResponse-Sequence-part2, ProjectOneResponse-Sequence-part3, ProjectOneResponse-Sequence-alt1-1, ProjectOneResponse-Sequence-alt1-1-part2, ProjectOneResponse-Sequence-alt2-1, ProjectOneResponse-Sequence-alt2-1-part2, ProjectOneResponse-Sequence-alt2-1-part3, ProjectOneResponse-Sequence-alt3-1, ProjectOneResponse-Sequence-alt3-1-part2, ProjectOneResponse-Sequence-alt3-1-part3, ProjectOneResponse-Sequence-alt3-1-part4, ProjectOneResponse-Sequence-alt3-2, ProjectOneResponse-Sequence-alt3-2-part2, ProjectOneResponse-Sequence-alt3-2-part3, ProjectOneResponse-Sequence-alt3-2-part4, ProjectOneResponse-Sequence-alt3-2-part5, ProjectOneResponse-Sequence-alt3-2-part6, ProjectOneResponse-Sequence-alt3-3, ProjectOneResponse-Sequence-alt3-3-part2, ProjectOneResponse-Sequence-alt3-3-part3, ProjectOneResponse-Sequence-alt3-3-part4, ProjectOneResponse-Sequence-alt3-4, ProjectOneResponse-Sequence-alt3-4-part2, ProjectOneResponse-Sequence-alt2-2, ProjectOneResponse-Sequence-alt1-2, ProjectOneResponse-Sequence-alt1-2-part2, ProjectOneResponse-Sequence-alt4-1, ProjectOneResponse-Sequence-alt4-1-part2, ProjectOneResponse-Sequence-alt4-2, ProjectOneResponse-Sequence-alt4-2-part2, ProjectOneResponse-Sequence-alt4-2-part3, ProjectOneResponse-Sequence-alt4-2-part4, ProjectOneResponse-Sequence-alt1-3, ProjectOneResponse-Sequence-alt1-4, ProjectOneResponse-Sequence-alt1-4-part2
  * Coverage: source 3 participants, 8 messages, 2 replies, 1 self messages, 7 notes, 4 blocks with 12 branches; 12 branch view families (12 open with their condition); view steps match 44 of 44 expected in order (including 10 continuation steps).
* F88, line 2486, sequence diagram (Sequence: stateless invocation end to end (process RunStatelessInvocation))
  * Same diagram as STR F172, line 4855 (byte-identical; STR heading "Sequence: stateless invocation end to end (process RunStatelessInvocation)").
  * Views (27): STR-06-27, STR-06-27-part2, STR-06-27-part3, STR-06-27-part4, STR-06-27-alt1-1, STR-06-27-alt1-1-part2, STR-06-27-alt1-1-part3, STR-06-27-alt1-2, STR-06-27-alt1-2-part2, STR-06-27-alt1-2-part3, STR-06-27-alt2-1, STR-06-27-alt2-1-part2, STR-06-27-alt2-2, STR-06-27-alt2-2-part2, STR-06-27-alt2-2-part3, STR-06-27-alt2-2-part4, STR-06-27-alt2-3, STR-06-27-alt2-3-part2, STR-06-27-alt2-4, STR-06-27-alt2-4-part2, STR-06-27-alt2-4-part3, STR-06-27-alt2-4-part4, STR-06-27-alt2-5, STR-06-27-alt2-5-part2, STR-06-27-alt2-5-part3, STR-06-27-alt2-5-part4, STR-06-27-alt2-5-part5
  * Coverage: source 5 participants, 3 messages, 6 replies, 0 self messages, 7 notes, 2 blocks with 7 branches; 7 branch view families (7 open with their condition); view steps match 34 of 34 expected in order (including 9 continuation steps).
* F89, line 2525, sequence diagram (Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence))
  * Same diagram as STR F173, line 4894 (byte-identical; STR heading "Nested sequence: BuildAndDeliverPackage (build and delivery of the stateless invocation sequence)").
  * Views (24): BuildAndDeliverPackage, BuildAndDeliverPackage-alt1-1, BuildAndDeliverPackage-alt1-1-part2, BuildAndDeliverPackage-alt1-1-part3, BuildAndDeliverPackage-alt2-1, BuildAndDeliverPackage-alt2-1-part2, BuildAndDeliverPackage-alt2-1-part3, BuildAndDeliverPackage-alt2-2, BuildAndDeliverPackage-alt2-2-part2, BuildAndDeliverPackage-alt1-2, BuildAndDeliverPackage-alt1-2-part2, BuildAndDeliverPackage-alt1-2-part3, BuildAndDeliverPackage-alt1-3, BuildAndDeliverPackage-alt1-3-part2, BuildAndDeliverPackage-alt1-3-part3, BuildAndDeliverPackage-alt3-1, BuildAndDeliverPackage-alt3-1-part2, BuildAndDeliverPackage-alt3-2, BuildAndDeliverPackage-alt3-2-part2, BuildAndDeliverPackage-alt3-2-part3, BuildAndDeliverPackage-alt3-2-part4, BuildAndDeliverPackage-alt3-2-part5, BuildAndDeliverPackage-opt1, BuildAndDeliverPackage-opt1-part2
  * Coverage: source 5 participants, 4 messages, 3 replies, 1 self messages, 4 notes, 4 blocks with 8 branches; 8 branch view families (8 open with their condition); view steps match 28 of 28 expected in order (including 4 continuation steps).
* F90, line 2563, sequence diagram (Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence))
  * Same diagram as STR F174, line 4932 (byte-identical; STR heading "Nested sequence: SubmitAndGradeResponse (response submission of the stateless invocation sequence)").
  * Views (9): SubmitAndGradeResponse, SubmitAndGradeResponse-part2, SubmitAndGradeResponse-alt1-1, SubmitAndGradeResponse-alt2-1, SubmitAndGradeResponse-alt2-1-part2, SubmitAndGradeResponse-alt2-2, SubmitAndGradeResponse-alt1-2, SubmitAndGradeResponse-alt1-2-part2, SubmitAndGradeResponse-alt1-3
  * Coverage: source 3 participants, 3 messages, 5 replies, 0 self messages, 2 notes, 2 blocks with 5 branches; 5 branch view families (5 open with their condition); view steps match 17 of 17 expected in order (including 0 continuation steps).
* F91, line 2592, sequence diagram (Sequence: submit GetAgentContextPilotReportQuery)
  * Same diagram as STR F175, line 4961 (byte-identical; STR heading "Sequence: submit GetAgentContextPilotReportQuery").
  * Views (8): STR-06-28, STR-06-28-part2, STR-06-28-part3, STR-06-28-part4, STR-06-28-alt1-1, STR-06-28-alt1-1-part2, STR-06-28-alt1-2, STR-06-28-alt1-2-part2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 13 of 13 expected in order (including 2 continuation steps).
* F92, line 2616, sequence diagram (Sequence: submit CreateAgentContextHvRequestCommand)
  * Same diagram as STR F176, line 4985 (byte-identical; STR heading "Sequence: submit CreateAgentContextHvRequestCommand").
  * Views (19): STR-06-29, STR-06-29-part2, STR-06-29-part3, STR-06-29-alt1-1, STR-06-29-alt1-2, STR-06-29-alt1-2-part2, STR-06-29-alt1-2-part3, STR-06-29-alt1-2-part4, STR-06-29-alt2-1, STR-06-29-alt2-2, STR-06-29-alt2-3, STR-06-29-alt2-3-part2, STR-06-29-alt3-1, STR-06-29-alt3-2, STR-06-29-alt3-2-part2, STR-06-29-alt3-3, STR-06-29-alt3-3-part2, STR-06-29-alt2-4, STR-06-29-alt2-4-part2
  * Coverage: source 4 participants, 6 messages, 7 replies, 0 self messages, 5 notes, 3 blocks with 9 branches; 9 branch view families (9 open with their condition); view steps match 33 of 33 expected in order (including 3 continuation steps).
* F93, line 2657, sequence diagram (Sequence: submit GetAgentContextHvRequestQuery)
  * Same diagram as STR F177, line 5026 (byte-identical; STR heading "Sequence: submit GetAgentContextHvRequestQuery").
  * Views (7): STR-06-30, STR-06-30-part2, STR-06-30-part3, STR-06-30-part4, STR-06-30-part5, STR-06-30-alt1-1, STR-06-30-alt1-2
  * Coverage: source 4 participants, 3 messages, 2 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 11 of 11 expected in order (including 1 continuation steps).
* F94, line 2678, sequence diagram (Sequence: submit QueryAgentContextHvRequestsQuery)
  * Same diagram as STR F178, line 5047 (byte-identical; STR heading "Sequence: submit QueryAgentContextHvRequestsQuery").
  * Views (7): STR-06-31, STR-06-31-part2, STR-06-31-part3, STR-06-31-part4, STR-06-31-alt1-1, STR-06-31-alt1-2, STR-06-31-alt1-2-part2
  * Coverage: source 4 participants, 3 messages, 3 replies, 0 self messages, 2 notes, 1 blocks with 2 branches; 2 branch view families (2 open with their condition); view steps match 12 of 12 expected in order (including 1 continuation steps).
* F95, line 2704, use-case flowchart (7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002])
  * Same diagram as STR F189, line 5272 (byte-identical; STR heading "7.11 Use case: Build a context package for a request [FR-MCP-AGENTCONTEXT-001, -002]").
  * Views (5): STR-07-02, STR-07-02-part2, STR-07-02-part3, STR-07-02-part4, STR-07-02-part5
  * Coverage: source 10 nodes and 9 edges, drawn on 11 elements with 10 element edges; views show 11 of 11 elements and 10 of 10 element edges (10 of 10 are UseCase relationships in the model).
* F96, line 2723, use-case flowchart (7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004])
  * Same diagram as STR F190, line 5291 (byte-identical; STR heading "7.12 Use case: Submit and grade an agent response [FR-MCP-AGENTCONTEXT-003, -004]").
  * Views (7): STR-07-03, STR-07-03-part2, STR-07-03-part3, STR-07-03-part4, STR-07-03-part5, STR-07-03-part6, STR-07-03-part7
  * Coverage: source 15 nodes and 14 edges, drawn on 15 elements with 14 element edges; views show 15 of 15 elements and 14 of 14 element edges (14 of 14 are UseCase relationships in the model).
* F97, line 2745, use-case flowchart (7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007])
  * Same diagram as STR F191, line 5313 (byte-identical; STR heading "7.13 Use case: Retrieve trusted prior context [FR-MCP-AGENTCONTEXT-007]").
  * Views (3): STR-07-04, STR-07-04-part2, STR-07-04-part3
  * Coverage: source 6 nodes and 5 edges, drawn on 6 elements with 5 element edges; views show 6 of 6 elements and 5 of 5 element edges (5 of 5 are UseCase relationships in the model).
* F98, line 2758, use-case flowchart (7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006])
  * Same diagram as STR F192, line 5326 (byte-identical; STR heading "7.14 Use case: Maintain relationships, staleness, and supersession [FR-MCP-AGENTCONTEXT-005, -006]").
  * Views (4): STR-07-05, STR-07-05-part2, STR-07-05-part3, STR-07-05-part4
  * Coverage: source 9 nodes and 8 edges, drawn on 9 elements with 8 element edges; views show 9 of 9 elements and 8 of 8 element edges (8 of 8 are UseCase relationships in the model).
* F99, line 2776, use-case flowchart (7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-008, -009])
  * Same diagram as STR F193, line 5344 (byte-identical; STR heading "7.15 Use case: Run a stateless or pilot invocation [FR-MCP-AGENTCONTEXT-008, -009]").
  * Views (7): STR-07-06, STR-07-06-part2, STR-07-06-part3, STR-07-06-part4, STR-07-06-part5, STR-07-06-part6, STR-07-06-part7
  * Coverage: source 13 nodes and 12 edges, drawn on 14 elements with 13 element edges; views show 14 of 14 elements and 13 of 13 element edges (13 of 13 are UseCase relationships in the model).
* F100, line 2796, use-case flowchart (7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007])
  * Same diagram as STR F194, line 5364 (byte-identical; STR heading "7.16 Use case: Project responses into GraphRAG [FR-MCP-AGENTCONTEXT-007]").
  * Views (4): STR-07-07, STR-07-07-part2, STR-07-07-part3, STR-07-07-part4
  * Coverage: source 8 nodes and 7 edges, drawn on 8 elements with 7 element edges; views show 8 of 8 elements and 7 of 7 element edges (7 of 7 are UseCase relationships in the model).
* F101, line 2811, activity flowchart (8.1 Phase order)
  * Same diagram as STR F195, line 5381 (byte-identical; STR heading "8.1 Phase order").
  * Views (2): STR-08-01, STR-08-01-part2
  * Coverage: source 7 nodes and 6 edges; model 7 Activity elements (Start 0, Action 7, Decision 0, Error 0, End 0), 7 of 7 with the node text, 6 of 6 edges as relationships with the edge label; views show 7 of 7 nodes and 6 of 6 edges.
* F102, line 2842, bounded-context graph (8.3 Bounded contexts (Phase 4 work packages))
  * Same diagram as STR F196, line 5412 (byte-identical; STR heading "8.3 Bounded contexts (Phase 4 work packages)").
  * Views (5): STR-08-02, STR-08-02-part2, STR-08-02-part3, STR-08-02-part4, STR-08-02-part5
  * Coverage: source 12 contexts, 16 merge-order edges and 2 groups; views show 12 of 12 contexts and 16 of 16 edges (16 of 16 in the model).
* F103, line 2892, activity flowchart (Process RunPlanPhases: run the four phases)
  * Same diagram as STR F197, line 5462 (byte-identical; STR heading "Process RunPlanPhases: run the four phases").
  * Views (3): RunPlanPhases, RunPlanPhases-part2, RunPlanPhases-part3
  * Coverage: source 9 nodes and 9 edges; model 9 Activity elements (Start 1, Action 6, Decision 0, Error 0, End 2), 9 of 9 with the node text, 9 of 9 edges as relationships with the edge label; views show 9 of 9 nodes and 9 of 9 edges.
* F104, line 2907, activity flowchart (Nested activity: RunOnePlanPhase (loop body of RunPlanPhases))
  * Same diagram as STR F198, line 5477 (byte-identical; STR heading "Nested activity: RunOnePlanPhase (loop body of RunPlanPhases)").
  * Views (2): RunOnePlanPhase, RunOnePlanPhase-part2
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 3, Decision 1, Error 0, End 2), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F105, line 2920, activity flowchart (Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases))
  * Same diagram as STR F199, line 5490 (byte-identical; STR heading "Nested activity: ValidateAndApprove (used by RunOnePlanPhase and RunPlanPhases)").
  * Views (3): ValidateAndApprove, ValidateAndApprove-part2, ValidateAndApprove-part3
  * Coverage: source 7 nodes and 7 edges; model 7 Activity elements (Start 1, Action 2, Decision 2, Error 0, End 2), 7 of 7 with the node text, 7 of 7 edges as relationships with the edge label; views show 7 of 7 nodes and 7 of 7 edges.
* F106, line 2935, activity flowchart (Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases))
  * Same diagram as STR F200, line 5505 (byte-identical; STR heading "Nested activity: RunPhase4WorkPackages (loop of RunPlanPhases)").
  * Views (1): RunPhase4WorkPackages
  * Coverage: source 5 nodes and 5 edges; model 5 Activity elements (Start 1, Action 2, Decision 1, Error 0, End 1), 5 of 5 with the node text, 5 of 5 edges as relationships with the edge label; views show 5 of 5 nodes and 5 of 5 edges.
* F107, line 2948, activity flowchart (Process RunDomainCodingAgent: one Phase 4 work package)
  * Same diagram as STR F201, line 5518 (byte-identical; STR heading "Process RunDomainCodingAgent: one Phase 4 work package").
  * Views (2): RunDomainCodingAgent, RunDomainCodingAgent-part2
  * Coverage: source 6 nodes and 5 edges; model 6 Activity elements (Start 1, Action 3, Decision 0, Error 0, End 2), 6 of 6 with the node text, 5 of 5 edges as relationships with the edge label; views show 6 of 6 nodes and 5 of 5 edges.
* F108, line 2959, activity flowchart (Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent))
  * Same diagram as STR F202, line 5529 (byte-identical; STR heading "Nested activity: ImplementBoundedContext (loop of RunDomainCodingAgent)").
  * Views (1): ImplementBoundedContext
  * Coverage: source 4 nodes and 4 edges; model 4 Activity elements (Start 1, Action 1, Decision 0, Error 0, End 2), 4 of 4 with the node text, 4 of 4 edges as relationships with the edge label; views show 4 of 4 nodes and 4 of 4 edges.
* F109, line 2969, activity flowchart (Nested activity: ImplementationPass (loop body of ImplementBoundedContext))
  * Same diagram as STR F203, line 5539 (byte-identical; STR heading "Nested activity: ImplementationPass (loop body of ImplementBoundedContext)").
  * Views (4): ImplementationPass, ImplementationPass-part2, ImplementationPass-part3, ImplementationPass-part4
  * Coverage: source 11 nodes and 12 edges; model 11 Activity elements (Start 1, Action 3, Decision 4, Error 0, End 3), 11 of 11 with the node text, 12 of 12 edges as relationships with the edge label; views show 11 of 11 nodes and 12 of 12 edges.

## Existing views reused (D1)

* Every linked view is an existing STR view; the list per fence is above and the comparison that shows each fence is the same diagram as the STR fence the view was converted from is the text match (109 of 109, 109 byte-identical) and the recount (0 problems). Linked views by kind: custom (activity) 204, custom (entity) 13, custom (state) 15, dynamic (sequence) 486, component (use cases) 4, container (use cases) 26, custom (bounded contexts) 5; 753 in all, none linked twice.
* SAC section numbers equal the architecture document's for every fence (sections 3.5, 6, 7.11 to 7.16 and 8), so view titles that name a section, such as "Packages and their build inputs (section 6.3)", stay true in this document.

## Existing elements reused (D4)

* The linked views draw 683 elements (reuse-summary.ps1 over the view includes and dynamic steps): 77 STR canonical elements, 12 as-built elements, and 579 Activity and 15 State elements that belong to their STR diagrams (per-diagram step elements, never reused elsewhere). Unclassified: 0.
* STR canonical BoundedContext (12): strBcBuildTooling (BuildTooling context, BoundedContext,Planned); strBcContextGraph (ContextGraph context, BoundedContext,Planned); strBcContextPackages (ContextPackages context, BoundedContext,Planned); strBcDocumentation (Documentation context, BoundedContext,Planned); strBcHostPluginAdoption (HostPluginAdoption context, BoundedContext,Planned); strBcPluginCorePowerShell (PluginCorePowerShell context, BoundedContext,Planned); strBcReplAndAgents (ReplAndAgents context, BoundedContext,Planned); strBcResponsesAndTrust (ResponsesAndTrust context, BoundedContext,Planned); strBcSessionTurnLogging (SessionTurnLogging context, BoundedContext,Planned); strBcSharedComponents (SharedComponents context, BoundedContext,Planned); strBcStorageSchema (StorageSchema context, BoundedContext,Planned); strBcTurnReviews (TurnReviews context, BoundedContext,Planned).
* STR canonical Component (5): qbrain.server.strAgentContextHandlers (Agent-Context Handlers, Component,Planned); qbrain.server.strModeGateBehavior (ModeGateBehavior, Component,Planned); qbrain.server.strPackageAssembler (PackageAssembler, Component,Planned); qbrain.server.strSharedComponents (Shared Components, Component,Planned); qbrain.server.strWorkspaceResolutionBehavior (WorkspaceResolutionBehavior, Component,Planned).
* STR canonical Entity (16): strEntAgentContextFinding (AgentContextFinding, Entity,Planned); strEntAgentContextHvRequest (AgentContextHvRequest, Entity,Planned); strEntAgentContextHvRequestItem (AgentContextHvRequestItem, Entity,Planned); strEntAgentContextOmission (AgentContextOmission, Entity,Planned); strEntAgentContextPackage (AgentContextPackage, Entity,Planned); strEntAgentContextPackageFocusRef (AgentContextPackageFocusRef, Entity,Planned); strEntAgentContextPackageInput (AgentContextPackageInput, Entity,Planned); strEntAgentContextPackageInputLink (AgentContextPackageInputLink, Entity,Planned); strEntAgentContextPackageItem (AgentContextPackageItem, Entity,Planned); strEntAgentContextProjection (AgentContextProjection, Entity,Planned); strEntAgentContextRelationship (AgentContextRelationship, Entity,Planned); strEntAgentResponse (AgentResponse, Entity,Planned); strEntAgentResponseClaim (AgentResponseClaim, Entity,Planned); strEntAgentResponseEvidence (AgentResponseEvidence, Entity,Planned); strEntAgentResponseGrade (AgentResponseGrade, Entity,Planned); strEntAgentResponseGradeFinding (AgentResponseGradeFinding, Entity,Planned).
* STR canonical SoftwareSystem (1): strHostModel (Agent Host Model, Software System,External).
* STR canonical UseCase (43): strUcAbsorbedMidTurnRunsAsPilot (Absorbed mid-turn: runs as Pilot, UseCase,Planned); strUcAssembleSecretsOmissionsFindingsBudgetRendering (Assemble: secrets, omissions, findings, budget,, UseCase,Planned); strUcBridgeHostHookSyncsTheTranscript (Bridge host: hook syncs the transcript, UseCase,Planned); strUcBuildAContextPackageForA (Build a context package for a request, UseCase,Planned); strUcBuildFailsOrMintPendingRefused (Build fails or mint pending: refused, canceled, UseCase,Planned); strUcCanceledCloseHeldByAnOutage (Canceled close held by an outage: warn, UseCase,Planned); strUcCommitOrProfileUnavailable409 (Commit or profile unavailable: 409, UseCase,Planned); strUcCreateDeleteQueryLinksCreateAgentContextRelationshipCommandTo (Create, delete, query links, UseCase,Planned); strUcDependencyChangedRemovedNeverProjected (Dependency changed: removed, never projected, UseCase,Planned); strUcDependencyChangedStaleNotTrusted (Dependency changed: Stale, not Trusted, UseCase,Planned); strUcDeterministicGradeRunDeterministicGradeCommand (Deterministic grade (RunDeterministicGradeCommand), UseCase,Planned); strUcEvaluateStalenessAtTheHEADCommit (Evaluate staleness at the HEAD commit, UseCase,Planned); strUcGradeFailsStaysGrading (Grade fails: stays Grading, UseCase,Planned); strUcGraphRAGDisabledIngestSkippedRemovalStays (GraphRAG disabled: ingest skipped, removal stays Pending, UseCase,Planned); strUcGraphRAGUnavailableOrAskedForOther (GraphRAG unavailable or asked for other kinds:, UseCase,Planned); strUcHostCannotRefuseAPromptRuns (Host cannot refuse a prompt: runs as Off, UseCase,Planned); strUcHVRequestRetriedStoredRequest (HV request retried: stored request, UseCase,Planned); strUcIngestOrDeleteErrorFailedRetried (Ingest or delete error: Failed, retried next pass, UseCase,Planned); strUcInvalidLinkCycleClientSupersedesLink (Invalid link, cycle, client supersedes link,, UseCase,Planned); strUcMaintainRelationshipsStalenessAndSupersession (Maintain relationships, staleness, and supersession, UseCase,Planned); strUcMandatoryOverBudget422InvocationFails (Mandatory over budget: 422, invocation fails, UseCase,Planned); strUcModelGradeRecordModelGradeCommand (Model grade (RecordModelGradeCommand), UseCase,Planned); strUcModeOffOr409DisabledStateful (Mode Off or 409 disabled: stateful, UseCase,Planned); strUcModeOffStillAnswersReadOnly (Mode Off: still answers (read-only query), UseCase,Planned); strUcModeSwitchedToOff409Agent (Mode switched to Off:, UseCase,Planned); strUcNoSubmissionNotAStopGate (No submission: not a Stop-gate item, UseCase,Planned); strUcOperatorRejectOfASupersededOne (Operator reject (of a Superseded one: 409), UseCase,Planned); strUcPilotReportReadableInOff (Pilot report, readable in Off, UseCase,Planned); strUcProjectionEnabledFalseIngestSkippedRemovalStill (ProjectionEnabled false: ingest skipped, removal still runs, UseCase,Planned); strUcProjectResponsesIntoGraphRAG (Project responses into GraphRAG, UseCase,Planned); strUcReadOnlyStalenessCheckOfEach (Read-only staleness check of each prior response, UseCase,Planned); strUcRejectDuringAnIngestDocumentRemoved (Reject during an ingest:, UseCase,Planned); strUcRepeatedKeySamePackageIdempotentCreate (Repeated key: same package (IdempotentCreate), UseCase,Planned); strUcReplayProvesDeterminismReplayAgentContextPackageQuery (Replay proves determinism (ReplayAgentContextPackageQuery), UseCase,Planned); strUcRetrieveCandidatesRetrieveAgentContextCandidatesQuery (Retrieve candidates (RetrieveAgentContextCandidatesQuery), UseCase,Planned); strUcRetrieveTrustedPriorContext (Retrieve trusted prior context, UseCase,Planned); strUcRunAStatelessOrPilotInvocation (Run a stateless or pilot invocation, UseCase,Planned); strUcSubmitAndGradeAnAgentResponse (Submit and grade an agent response, UseCase,Planned); strUcSubmitSubmitAgentResponseCommand (Submit (SubmitAgentResponseCommand), UseCase,Planned); strUcSupersedeAResponseSupersedeAgentResponseCommand (Supersede a response (SupersedeAgentResponseCommand), UseCase,Planned); strUcSupersedeOfARejectedOrAlready (Supersede of a Rejected or already superseded response:, UseCase,Planned); strUcTrustDecideAgentResponseTrustCommand (Trust (DecideAgentResponseTrustCommand), UseCase,Planned); strUcUnconfiguredGrader403 (Unconfigured grader: 403, UseCase,Planned).
* As-built (12): agent (AI Coding Agent); developer (Developer / Operator); entSessionLog (SessionLog); entSessionLogTurn (SessionLogTurn); llm (LLM Providers and Agent CLIs); qbrain.devTools.evidenceScripts (Evidence collector scripts); qbrain.libs.client (QBrainAi.Client); qbrain.plugins.pluginPs (lib-ps); qbrain.server (MCP Context Server); qbrain.server.api (REST Controllers); qbrain.server.graphrag (GraphRAG); qbrain.server.mcpDbContext (McpDbContext).
* New canonical elements: none. New relationships: none. D3 needs no code search, because nothing new is tagged.

## Render inspection

* Method: all 753 linked views of the gate render were laid out on 84 contact sheets in document order (SCRATCH/p4-sac/all-sheets.ps1, nine renders per sheet with the view key under each, index in SCRATCH/p4-sac/sheets/index.txt). No view is new, so the brief's "every new view at least once" covers no view; a sample of every diagram kind was read as images: sheets 001, 006, 007, 019, 025, 044, 059, 079, 082, 084 (10 sheets).
  * sheet-001 (F1 F2 F3): RequestPipeline, RequestPipeline-part2, SerializableWrite, SerializableWrite-part2, SerializableWrite-part3, IdempotentCreate, IdempotentCreate-part2, IdempotentCreate-part3, IdempotentCreate-part4
  * sheet-006 (F15 F16 F17 F18 F19): STR-06-01-part3, STR-06-02, STR-06-02-part2, STR-06-03, STR-06-03-part2, STR-06-04, STR-06-04-part2, STR-06-05, STR-06-05-part2
  * sheet-007 (F20 F21): STR-06-06, STR-06-06-part2, STR-06-07, STR-06-07-part2, STR-06-07-part3, STR-06-07-part4, STR-06-07-part5, STR-06-07-part6, STR-06-07-part7
  * sheet-019 (F48 F49): QueryAgentContextRelationshipsQuery-part4, RetrieveAgentContextCandidatesQuery, RetrieveAgentContextCandidatesQuery-part2, RetrieveAgentContextCandidatesQuery-part3, RetrieveAgentContextCandidatesQuery-part4, RetrieveAgentContextCandidatesQuery-part5, RetrieveAgentContextCandidatesQuery-part6, RetrieveAgentContextCandidatesQuery-part7, RetrieveAgentContextCandidatesQuery-part8
  * sheet-025 (F62): RequestPipeline-Sequence-part3, RequestPipeline-Sequence-part4, RequestPipeline-Sequence-alt1-1, RequestPipeline-Sequence-alt1-1-part2, RequestPipeline-Sequence-alt1-2, RequestPipeline-Sequence-alt2-1, RequestPipeline-Sequence-alt2-1-part2, RequestPipeline-Sequence-alt2-1-part3, RequestPipeline-Sequence-alt2-2
  * sheet-044 (F75 F76): STR-06-18-part4, STR-06-18-part5, STR-06-18-part6, STR-06-18-part7, STR-06-18-part8, STR-06-18-loop1, STR-06-18-loop1-part2, SubmitOneModelGrade, SubmitOneModelGrade-alt1-1
  * sheet-059 (F84): STR-06-25-part4, STR-06-25-alt1-1, STR-06-25-alt1-1-part2, STR-06-25-alt1-2, STR-06-25-alt1-2-part2, STR-06-25-opt1, STR-06-25-alt2-1, STR-06-25-alt2-2, STR-06-25-alt2-3
  * sheet-079 (F95 F96): STR-07-02-part3, STR-07-02-part4, STR-07-02-part5, STR-07-03, STR-07-03-part2, STR-07-03-part3, STR-07-03-part4, STR-07-03-part5, STR-07-03-part6
  * sheet-082 (F100 F101 F102 F103): STR-07-07-part4, STR-08-01, STR-08-01-part2, STR-08-02, STR-08-02-part2, STR-08-02-part3, STR-08-02-part4, STR-08-02-part5, RunPlanPhases
  * sheet-084 (F107 F108 F109): RunDomainCodingAgent-part2, ImplementBoundedContext, ImplementationPass, ImplementationPass-part2, ImplementationPass-part3, ImplementationPass-part4
* Checked on each image: every node, state, entity, use case and participant drawn with its name; edge, transition and step labels present and readable at contact-sheet scale; branch, loop and opt views open with their condition step; terminals as ellipses, decisions as diamonds, errors in the error style; no shape cut off at the image edge (the page size itself is the AC-5 check of the gate runs; renders carry no title, so the key under each image comes from the sheet index). Seen and recorded: the ER views of sheets 6 and 7 draw the AgentResponse SupersededByResponseId self reference and merged labels such as "records / omissions" (the ER fidelity judgment call); the as-built McpDbContext description runs past its box in STR-06-25-opt1 (sheet 59), the known as-built case of the STR review; element names qualified with the STR view key (for example "400 validation_error (RetrieveAgentContextCandidatesQuery)", sheet 19) are the STR naming rule for unique names.
* The STR agent read every STR view as an image on its own gate renders (docs/receipts/c4model/p4-str-review.md, Render inspection); the AC-5 page check of both gate runs here passed for all 1677 renders (0 PageOverflow, 0 TextSizeSpread).

## Prose that describes Mermaid (left unchanged for the operator)

* Line 4 (converted document line 4): operator directive quoted verbatim: "a single, complete activity diagram (mermaid) for every unit of work".
* Line 31 (converted document line 31): shared components are drawn as one call node in Mermaid subroutine syntax ([["ComponentName ..."]]).
* Line 487 (converted document line 478): ER line styles: a solid line is a composite foreign key, a dotted line a logical reference; the C4 renders state this in the relationship technology (EF Core foreign key or Logical reference), not by line style, and "Each relationship is drawn once" no longer holds in the reused views (see the ER fidelity judgment call).
* Line 680 (converted document line 693): call nodes [["Component: purpose"]] and SerializableWrite "drawn as a call node followed by a box that holds its delegate" (a Mermaid subgraph); the activity views draw no box.
* Line 1514 (converted document line 1477): "Each branch ends in its own terminal so the diagram stays one page"; the C4 views of that diagram (ResolveInvocationTurn) are split into 5 page parts.
* Line 1570 (converted document line 1537): sequence conventions of the Mermaid renders: the note ref RequestPipeline, labels without br breaks because "the renderer wraps an unbroken label", five participants to fit "a landscape letter page at 8 pt", and nested sequences for diagrams "too long for three pages".
* Borderline, line 1574 (converted document line 1541): the dispatch "and then the note ref RequestPipeline"; the dynamic views draw it as a self step labelled "Note over Controller and Handler: ref RequestPipeline ...".
* Borderline, line 1575 (converted document line 1542): SerializableWrite "draws one note naming the delegate"; drawn as a self step labelled "Note: ...".
* The document has no diagram index (its summary at line 6 lists sections, not diagrams), so no index was added; each view key is named by the View line under its image.

## Judgment calls

* Reuse for every fence: all 109 fences are the same text as STR fences, so the D1 rule for views links the STR views. Consequence: SAC adds no view, element or relationship, its renders are the STR renders, and a later change to one of these diagrams is made once, in model/str.dsl and views/str.dsl.
* ER fidelity: the six section 6.3 ER fences link STR-06-01 to STR-06-06, which draw 10 relationships the fence does not draw and 13 merged labels (listed under F15 to F20). Cause (from the DSL, not inferred from images): the STR model keeps one relationship per foreign key whose description joins the labels of every diagram that draws it (section 6.3 and Addendum A), and a custom view shows every model relationship between its included entities. A SAC view with the same entities would render the same relationships, and a separate relationship per diagram would put two relationships on one foreign key (D4), so a SAC copy would not remove the difference. It also affects the STR document's own section 6.3 diagrams. Open question for the operator: keep the merged form, or change the STR ER views (for example exclude expressions per view), which fixes both documents at once.
* Fragments: the brief names model/sac.dsl and views/sac.dsl as files to create; with nothing to define they hold only a comment that records the reuse, and workspace.dsl has exactly the two include lines. The pinned CLI accepts them (inspect exit 0). If empty fragments are not wanted, both files and both include lines can be dropped without any other change.
* Image alt text is the STR view title, as the STR document shows it ("[" and "]" as parentheses), and the path is architecture/c4/rendered/<key>.svg because the document sits in docs/; the AC-4 link check resolves it to docs/architecture/c4/rendered (0 DanglingViewLink in both runs; verify-doc.ps1 resolved all 753 links to defined keys).

## Gate G4 receipts

* Run 1: docs/receipts/c4model/20261019T201106-p4-sac-g4-r1.log (20:11:06 to 20:15:14 local). diagram-findings.ps1: 0 ValidationFailed, 0 InspectionErrors, 0 RenderFailed, 0 DanglingViewLink, 0 MissingRender, 0 OrphanRender, 0 PageOverflow, 0 TextSizeSpread, 0 InvalidSvg, 0 MermaidFile and 107 MermaidFence; all 214 MermaidFence log lines (107 findings, each repeated in the Nuke summary) name docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md, the other stage 2 document; log-grep for Stateless-Agent-Context and sac.dsl: 0 matches in both logs. 1677 renders (Render lines 1677, SVG files 1677) in both runs; the build exits 255 only on the Amendment 1 fences.
* Run 2: docs/receipts/c4model/20261019T201552-p4-sac-g4-r2.log (20:15:52 to 20:19:43 local): the same counts. hash-dir.ps1 manifest of run 1 (SCRATCH/p4-sac/manifest-g4-r1.txt, 20:15:47 local) compared after run 2 (20:19:57 local): Identical 1677 of 1677.
* Inspection: pinned Structurizr CLI inspect at severity error on the worktree sources with the two fragments included (inspect-workspace.ps1, 20:10:31 local): exit 0, 0 lines.
* Renders: git status of docs/architecture/c4/rendered after the runs (render-status-check.ps1, 20:31:48 local): 1418 untracked renders, exactly the 1418 view keys of views/str.dsl (0 STR views without a render), which b457ac22 does not carry, and 259 modified renders, all of them views outside views/str.dsl that b457ac22 carries; why those 259 differ is not checked render by render (inference from the brief: the page-wide JointJS ids shift with any workspace change, here the STR views merged after those renders were made). None of them is a SAC view, because SAC adds none. The renders are gate evidence in the sac worktree only.
* The .log receipts are ignored by .gitignore (*.log); committing them needs git add -f. The two .pid files beside them are untracked process-id leftovers of start-nuke.ps1.
