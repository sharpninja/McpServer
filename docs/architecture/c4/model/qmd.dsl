# PLAN-C4MODEL-001 P4: model of docs/Project/Quad-Model-Transactional-Diffgram-Plan.md (DocCode QMD, FR-MCP-124,
# TR-MCP-TXNDIAGRAMS-001). The six imported diagram ids are view keys in views/qmd.dsl; repo-annotation branch ids are
# branchId properties. D3: only steps and links with no code carry Planned; as-built elements cite code in asBuilt.
# Relationships tagged QMD are the edges and messages drawn in this document (the static views filter to them).

# Existing elements labelled by ARCH-QUAD-001 repo annotations, and as-built server components the diagrams name.
!element qbrain.server {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-MCP"
    }

    qmdArbiterRole = component "Arbiter of Truth Role" "Quad-Brain role for oversight, final gatekeeping and reconciliation of committed role evidence." "Brain slot role (BrainSlotRoles.ArbiterOfTruth)" {
        properties {
            "diagram" "ARCH-QUAD-001"
            "mermaidNode" "AM"
            "asBuilt" "BrainSlotRoles.ArbiterOfTruth (src/QBrainAi.Support.Mcp/Services/BrainSlotContracts.cs:21); run by QuadBrainOrchestrationService.ExecuteAotReconciliationCoreAsync (QuadBrainOrchestrationService.cs:241-285) after Creativity and Logic; slot assignment in config/brain-slots/quad-brain-slot-assignments.yaml"
        }
    }
    qmdCreativityRole = component "Creativity Role" "Quad-Brain role for holistic, associative and generative synthesis; runs in parallel with Logic." "Brain slot role (BrainSlotRoles.Creativity)" {
        properties {
            "diagram" "ARCH-QUAD-001"
            "mermaidNode" "CR"
            "asBuilt" "BrainSlotRoles.Creativity (BrainSlotContracts.cs:12); invoked by QuadBrainOrchestrationService.ExecuteFullOrchestrationAsync (QuadBrainOrchestrationService.cs:84-98)"
        }
    }
    qmdLogicRole = component "Logic Role" "Quad-Brain role for analytical, sequential and rule-based reasoning; runs in parallel with Creativity." "Brain slot role (BrainSlotRoles.Logic)" {
        properties {
            "diagram" "ARCH-QUAD-001"
            "mermaidNode" "LG"
            "asBuilt" "BrainSlotRoles.Logic (BrainSlotContracts.cs:15); invoked by QuadBrainOrchestrationService.ExecuteFullOrchestrationAsync (QuadBrainOrchestrationService.cs:91-98)"
        }
    }
    qmdCuriosityRole = component "Curiosity Engine Role" "Quad-Brain research role, escalated when neither Creativity nor Logic returns valid output; the only role allowed to request GraphRAG admission." "Brain slot role (BrainSlotRoles.CuriosityEngine)" {
        properties {
            "diagram" "ARCH-QUAD-001"
            "mermaidNode" "CM"
            "asBuilt" "BrainSlotRoles.CuriosityEngine (BrainSlotContracts.cs:18); escalation in QuadBrainOrchestrationService.cs:109-126; GraphRAG admission only for this role (BrainSlotInvocationService.cs:86-87, 184-194)"
        }
    }
    qmdSharedCache = component "Shared Cache" "Per-turn orchestration context shared by reference with every role invocation; holds committed role evidence. The imported cross-turn Shared Corpus Callosum Cache is not built." "BrainSlotTurnContext" {
        properties {
            "diagram" "ARCH-QUAD-001"
            "mermaidNode" "Cache"
            "asBuilt" "src/QBrainAi.Support.Mcp/Services/BrainSlotTurnContext.cs:7-37 (CommittedRoleEvidence, SetCommittedEvidence); written in QuadBrainOrchestrationService.cs:121, 129, 131, 149, 176, 178, 271"
        }
    }
    qmdKeyServer = component "In-Process Key Server" "Key server services the MCP Context Server hosts in process by default: registers parties, signs and verifies transaction manifests (the third party, 3PKS)." "InMemoryKeyServerService (AddTransactionKeyServer)" {
        properties {
            "diagram" "SD-DIFFGRAM-001"
            "mermaidNode" "T (3PKS)"
            "asBuilt" "AddInProcessTransactionSecurity (src/QBrainAi.Support.Mcp/Program.cs:214; TransactionSecurityServiceCollectionExtensions.cs:87-102) registers AddTransactionKeyServer (lines 18-33): InMemoryKeyServerService (TransactionSecurityServices.cs:171) as IKeyServerManifestService; compatibility endpoints in src/QBrainAi.Support.Mcp/Controllers/KeyServerController.cs. The standalone Key Server host (qbrain.keyserver) runs the same service."
        }
    }
    qmdSubscriber = component "In-Process Subscriber" "Subscriber the MCP Context Server hosts in process by default: verifies the manifest and hashes, decrypts protected diffgrams, then commits or rejects with audit." "InMemorySubscriberCommitService (AddInProcessTransactionSubscriber)" {
        properties {
            "diagram" "SD-DIFFGRAM-001"
            "mermaidNode" "S (Subscriber)"
            "asBuilt" "AddInProcessTransactionSecurity registers AddInProcessTransactionSubscriber (TransactionSecurityServiceCollectionExtensions.cs:39-54): InMemorySubscriberCommitService (TransactionSecurityServices.cs:775); compatibility endpoints in src/QBrainAi.Support.Mcp/Controllers/SubscriberController.cs. The standalone Transaction Subscriber host (qbrain.subscriber) runs the same service."
        }
    }
}

!element qbrain.server.brain {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-QUAD"
    }
}

!element qbrain.libs.client {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-CLIENT"
    }
}

!element qbrain.libs.transactionSecurity {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-PUBSUB"
    }
}

!element qbrain.keyserver {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-KEYSERVER"
    }
}

!element qbrain.subscriber {
    properties {
        "branchId" "ARCH-QUAD-001-COMP-SUBSCRIBER"
    }
}

# As-built links of the new server components (model only; not edges of the imported diagrams).
qbrain.server.brain -> qbrain.server.qmdArbiterRole "Invokes on its assigned brain slot" "In-process"
qbrain.server.brain -> qbrain.server.qmdCreativityRole "Invokes on its assigned brain slot" "In-process"
qbrain.server.brain -> qbrain.server.qmdLogicRole "Invokes on its assigned brain slot" "In-process"
qbrain.server.brain -> qbrain.server.qmdCuriosityRole "Invokes on its assigned brain slot" "In-process"
qbrain.server.brain -> qbrain.server.qmdSharedCache "Creates one per turn and passes it to every role" "In-process"
qbrain.server.qmdKeyServer -> qbrain.libs.transactionSecurity "Implemented in" "Assembly"
qbrain.server.qmdSubscriber -> qbrain.libs.transactionSecurity "Implemented in" "Assembly"
qbrain.server.qmdSubscriber -> qbrain.server.qmdKeyServer "Verifies manifests" "In-process"

# ARCH-QUAD-001 High-Level System Architecture (imported section 3.6). Unlabelled source edges carry descriptions taken
# from the document's imported prose (sections Imported Architecture Overview and Imported Transactional Model).
developer -> qbrain.server.qmdArbiterRole "Sends each turn" "HTTPS" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "User --> AM"
        "asBuilt" "Turns enter POST /v1/chat/completions (QuadBrainOpenAiController.cs:14, 29) and QuadBrainOrchestrationService, which runs Creativity and Logic before ArbiterOfTruth reconciles (QuadBrainOrchestrationService.cs:84-140); the AoT does not receive the turn first."
    }
}
qbrain.server.qmdArbiterRole -> qbrain.server.qmdCreativityRole "Routes the turn" "In-process" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "AM --> CR"
        "asBuilt" "QuadBrainOrchestrationService invokes the roles; the AoT does not route them (QuadBrainOrchestrationService.cs:84-98)."
    }
}
qbrain.server.qmdArbiterRole -> qbrain.server.qmdLogicRole "Routes the turn" "In-process" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "AM --> LG"
        "asBuilt" "QuadBrainOrchestrationService invokes the roles; the AoT does not route them (QuadBrainOrchestrationService.cs:84-98)."
    }
}
qbrain.server.qmdArbiterRole -> qbrain.server.qmdCuriosityRole "Routes the turn" "In-process" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "AM --> CM"
        "asBuilt" "QuadBrainOrchestrationService escalates to CuriosityEngine when neither Creativity nor Logic returns valid output (QuadBrainOrchestrationService.cs:109-126)."
    }
}
qbrain.server.qmdCreativityRole -> qbrain.server.qmdSharedCache "Records committed evidence" "In-process" "QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "CR --> Cache"
        "asBuilt" "SetCommittedEvidence(Creativity) at QuadBrainOrchestrationService.cs:129, 176"
    }
}
qbrain.server.qmdLogicRole -> qbrain.server.qmdSharedCache "Records committed evidence" "In-process" "QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "LG --> Cache"
        "asBuilt" "SetCommittedEvidence(Logic) at QuadBrainOrchestrationService.cs:131, 178"
    }
}
qbrain.server.qmdCuriosityRole -> qbrain.server.qmdSharedCache "Records committed evidence" "In-process" "QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "CM --> Cache"
        "asBuilt" "SetCommittedEvidence(CuriosityEngine) at QuadBrainOrchestrationService.cs:121, 237"
    }
}
qbrain.server.qmdSharedCache -> qbrain.libs.transactionSecurity "Publishes activity" "In-process" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "Cache --> PubSub"
        "asBuilt" "The turn context does not publish; each brain-slot invocation commits its own diffgram through TurnTransactionCoordinator and ITransactionPubSub (BrainSlotInvocationService.cs:145-168)."
    }
}
qbrain.keyserver -> qbrain.libs.transactionSecurity "Signs transaction manifests" "HTTP/JSON" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "KeyServer --> PubSub"
        "asBuilt" "The key server does not feed the pub-sub; the coordinator asks it to sign before publishing (TurnTransactionCoordinator.cs:343)."
    }
}
qbrain.subscriber -> qbrain.libs.transactionSecurity "Commits diffgrams before the response" "HTTP/JSON" "Planned,QMD" {
    properties {
        "diagram" "ARCH-QUAD-001"
        "mermaidEdge" "Sub --> PubSub"
        "asBuilt" "Delivery runs the other way: ITransactionPubSub (Direct, Http, FanOut, ExternalBroker, Durable; TransactionPubSubServices.cs) delivers commits to the subscriber, and the coordinator waits for the commit (TurnTransactionCoordinator.cs:394-422)."
    }
}

# SD-DIFFGRAM-001 Three-Party Diffgram Exchange (imported section 3.3): one relationship per message.
qbrain.server.txsec -> qbrain.server.qmdKeyServer "Public Keys + Diffgram SHA" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-MSG-PUBLISHER-KEYS"
        "asBuilt" "TurnTransactionCoordinator.ExecuteAsync builds the diffgram and calls SignManifestAsync with party ids, key ids and hashes (TurnTransactionCoordinator.cs:342-345)."
    }
}
qbrain.server.qmdKeyServer -> qbrain.server.qmdKeyServer "Sign Transaction Manifest" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-MSG-SIGN"
        "asBuilt" "InMemoryKeyServerService.SignManifestAsync (TransactionSecurityServices.cs:284)"
    }
}
qbrain.server.qmdKeyServer -> qbrain.server.qmdSubscriber "Encrypted SHA + Encrypted Diffgram" "In-process" "Planned,QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "mermaidArrow" "-->> (dashed)"
        "asBuilt" "The key server returns the signed manifest to the coordinator; the coordinator, not the key server, sends the manifest, encrypted diffgram and hashes to the subscriber through ITransactionPubSub (TurnTransactionCoordinator.cs:394-404)."
    }
}
qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Decrypt SHA (3PKS Pub + Own Priv)" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "asBuilt" "InMemorySubscriberCommitService verifies the signed manifest through IKeyServerManifestService.VerifyManifestAsync (TransactionSecurityServices.cs:872-887)."
    }
}
qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Verify SHA" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-MSG-VERIFY-HASH"
        "asBuilt" "Encrypted-body SHA-256 checks (TransactionSecurityServices.cs:889-906) and plaintext SHA-256 check after decrypt (933-945)."
    }
}
qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Abort Transaction" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-BR-INVALID"
        "condition" "alt Invalid"
        "asBuilt" "RejectedAsync on an invalid signature, hash mismatch or failed decrypt (TransactionSecurityServices.cs:879-886, 889-906, 913-931, 936-945)."
    }
}
qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Decrypt Diffgram (Publisher Pub + Own Priv)" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-BR-VALID"
        "condition" "else Valid"
        "asBuilt" "TransactionDiffgramProtector.Unprotect of the protected envelope (TransactionSecurityServices.cs:908-912)."
    }
}
qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Commit" "In-process" "QMD" {
    properties {
        "diagram" "SD-DIFFGRAM-001"
        "branchId" "SD-DIFFGRAM-001-BR-VALID"
        "condition" "else Valid"
        "asBuilt" "AddOrCompleteTransactionAsync, sequence update and committed audit (TransactionSecurityServices.cs:970-999)."
    }
}

# AD-TXN-001 Normal Turn Transaction (imported section 3.1). As-built, each role invocation and the AoT reconciliation run
# their own turn transaction after the model call (BrainSlotInvocationService.cs:114-168), so the transaction steps repeat
# per invocation.
qmdTxnTurnStarts = element "User Request / Turn Starts" "Activity: AD-TXN-001" "Turn request reaches orchestration" "Activity,Start" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "A"
        "asBuilt" "POST /v1/chat/completions (QuadBrainOpenAiController.cs:14, 29) into QuadBrainOrchestrationService.ExecuteFullOrchestrationAsync (QuadBrainOrchestrationService.cs:55-211); each mutation enters TurnTransactionCoordinator.ExecuteAsync (TurnTransactionCoordinator.cs:296)"
    }
}
qmdTxnSubscriberPresent = element "Active Subscriber Present?" "Activity: AD-TXN-001" "Degraded-mode policy" "Activity,Decision" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "B"
        "asBuilt" "TransactionDegradedModePolicy enters degraded mode on SubscriberUnavailable, KeyServerUnavailable, CommitTimeout, Unknown or TransactionsDisabled (TurnTransactionCoordinator.cs:117-123); the check reacts to a failure instead of probing first"
    }
}
qmdTxnDegraded = element "Enter Degraded Mode" "Activity: AD-TXN-001" "Coordinator degraded status" "Activity,Error" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "C"
        "asBuilt" "Failure returns status degraded and records transaction_degraded (TurnTransactionCoordinator.cs:459-485)"
    }
}
qmdTxnStartTransaction = element "Start Transaction + Transaction Manifest" "Activity: AD-TXN-001" "Coordinator builds the diffgram" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "D"
        "asBuilt" "TurnTransactionCoordinator.ExecuteAsync resolves parties and keys and builds the diffgram (TurnTransactionCoordinator.cs:330-342)"
    }
}
qmdTxnSignManifest = element "3PKS Signs Manifest" "Activity: AD-TXN-001" "Key server signs the manifest" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "E"
        "asBuilt" "SignManifestAsync before the mutation (TurnTransactionCoordinator.cs:343-347); InMemoryKeyServerService.SignManifestAsync (TransactionSecurityServices.cs:284)"
    }
}
qmdTxnModelsProcess = element "Creativity + Logic + Curiosity Process" "Activity: AD-TXN-001" "Creativity and Logic; Curiosity on escalation" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "F"
        "asBuilt" "Creativity and Logic run in parallel (QuadBrainOrchestrationService.cs:84-98); CuriosityEngine only when neither returns valid output (109-126)"
    }
}
qmdTxnAotArbitrates = element "AoT Arbitrates" "Activity: AD-TXN-001" "ArbiterOfTruth reconciliation" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "G"
        "asBuilt" "ExecuteAotReconciliationCoreAsync (QuadBrainOrchestrationService.cs:133-145, 241-285)"
    }
}
qmdTxnAotApproves = element "AoT Approves Final Diffgram?" "Activity: AD-TXN-001" "Semantic rejection check" "Activity,Decision" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "H"
        "asBuilt" "IsCommitted and IsAotSemanticRejection checks (QuadBrainOrchestrationService.cs:144-146, 188)"
    }
}
qmdTxnReconciliation = element "Reconciliation Process" "Activity: AD-TXN-001" "Voting round" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "I"
        "elaboratedBy" "AD-AOT-001"
        "asBuilt" "Voting round: Creativity and Logic vote on the AoT rejection (QuadBrainOrchestrationService.cs:147-169)"
    }
}
qmdTxnModelsAgree = element "All 3 Models Agree?" "Activity: AD-TXN-001" "Valid votes from both roles" "Activity,Decision" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "J"
        "asBuilt" "Both votes must return valid output, else the orchestration is rejected (QuadBrainOrchestrationService.cs:170-173); CuriosityEngine does not vote"
    }
}
qmdCombinedArgument = element "Combined Argument to AoT" "Activity: AD-TXN-001, AD-AOT-001" "Second AoT reconciliation" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001, AD-AOT-001"
        "mermaidNode" "AD-TXN-001 K; AD-AOT-001 F"
        "asBuilt" "Vote outputs become committed evidence and a second ExecuteAotReconciliationCoreAsync runs (QuadBrainOrchestrationService.cs:175-187)"
    }
}
qmdTxnAbortRollback = element "Abort + Rollback" "Activity: AD-TXN-001" "Abort publish and rollback" "Activity,Error" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "L"
        "asBuilt" "PublishAbortAsync, RollbackMutationAsync and pending-commit cancellation, audit kept (TurnTransactionCoordinator.cs:362-391, 487-507)"
    }
}
qmdTxnDiffgramPublished = element "Final Diffgram Published" "Activity: AD-TXN-001" "Commit through the pub-sub" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "M"
        "asBuilt" "CommitDiffgramAsync through ITransactionPubSub (TurnTransactionCoordinator.cs:394-404)"
    }
}
qmdTxnSubscriberCommits = element "Subscriber Verifies + Commits" "Activity: AD-TXN-001" "Subscriber verifies and commits" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "N"
        "asBuilt" "InMemorySubscriberCommitService verifies manifest and hashes, decrypts and commits (TransactionSecurityServices.cs:872-999)"
    }
}
qmdTxnCommitted = element "Transaction Committed" "Activity: AD-TXN-001" "Committed result and audit" "Activity,Action" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "O"
        "asBuilt" "Status committed and diffgram_committed audit (TurnTransactionCoordinator.cs:406-422)"
    }
}
qmdTxnResponseReturned = element "Response Returned" "Activity: AD-TXN-001" "Committed orchestration response" "Activity,End" {
    properties {
        "diagram" "AD-TXN-001"
        "mermaidNode" "P"
        "asBuilt" "QuadBrainOrchestrationResponse with status committed (QuadBrainOrchestrationService.cs:199-210)"
    }
}

qmdTxnTurnStarts -> qmdTxnSubscriberPresent "next" "Control flow"
qmdTxnSubscriberPresent -> qmdTxnDegraded "No" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-NO-SUBSCRIBER"
    }
}
qmdTxnSubscriberPresent -> qmdTxnStartTransaction "Yes" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-START"
    }
}
qmdTxnStartTransaction -> qmdTxnSignManifest "next" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-START"
    }
}
qmdTxnSignManifest -> qmdTxnModelsProcess "next" "Control flow"
qmdTxnModelsProcess -> qmdTxnAotArbitrates "next" "Control flow"
qmdTxnAotArbitrates -> qmdTxnAotApproves "next" "Control flow"
qmdTxnAotApproves -> qmdTxnReconciliation "No" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-REJECTED"
    }
}
qmdTxnReconciliation -> qmdTxnModelsAgree "next" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-REJECTED"
    }
}
qmdTxnModelsAgree -> qmdCombinedArgument "Yes" "Control flow"
qmdTxnModelsAgree -> qmdTxnAbortRollback "No" "Control flow" "Planned" {
    properties {
        "branchId" "AD-TXN-001-BR-ROLLBACK"
        "deferred" "Model-disagreement policy automation remains deferred (repo annotation); as-built, an invalid vote rejects the orchestration (QuadBrainOrchestrationService.cs:170-173) without a transaction rollback."
    }
}
qmdCombinedArgument -> qmdTxnAotApproves "next" "Control flow"
qmdTxnAotApproves -> qmdTxnDiffgramPublished "Yes" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-APPROVED"
    }
}
qmdTxnDiffgramPublished -> qmdTxnSubscriberCommits "next" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-APPROVED"
    }
}
qmdTxnSubscriberCommits -> qmdTxnCommitted "next" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-APPROVED"
    }
}
qmdTxnCommitted -> qmdTxnResponseReturned "next" "Control flow" {
    properties {
        "branchId" "AD-TXN-001-BR-AOT-APPROVED"
    }
}

# AD-CURIOSITY-001 Curiosity Engine Flow (imported section 3.2). The autonomous flow stays disabled (Implementation
# Ordering item 5); only the external call, AoT review, GraphRAG admission and explicit weight update have code.
qmdCurMonitor = element "Monitor Cache + Logs + Traces" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Start,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "A"
        "branchId" "AD-CURIOSITY-001-BR-MONITOR"
        "deferred" "Documented for future cache, log and trace monitoring (repo annotation)."
    }
}
qmdCurFrustration = element "Frustration Detected?" "Activity: AD-CURIOSITY-001" "Planned: deferred" "Activity,Decision,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "B"
        "branchId" "AD-CURIOSITY-001-BR-FRUSTRATION"
        "deferred" "Deferred and disabled (repo annotation); frustration appears only as an instruction in the CuriosityEngine escalation prompt (QuadBrainOrchestrationService.cs:513)."
    }
}
qmdCurTargetedResearch = element "Targeted Research" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Action,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "C"
    }
}
qmdCurStruggleNovel = element "Struggle or Novel Concept?" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Decision,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "D"
    }
}
qmdCurValueRanking = element "Value Ranking" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Action,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "E"
    }
}
qmdCurHighValue = element "High Value?" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Decision,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "F"
    }
}
qmdCurResearchExperiments = element "Research + Code Experiments" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Action,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "G"
    }
}
qmdCurExternalNeeded = element "External Help Needed?" "Activity: AD-CURIOSITY-001" "Planned: no runtime decision" "Activity,Decision,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "H"
        "deferred" "No runtime decision exists; external invocation is configured per brain slot."
    }
}
qmdCurCallExternal = element "Call External + Inject Context" "Activity: AD-CURIOSITY-001" "Gated external brain-slot call" "Activity,Action" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "I"
        "branchId" "AD-CURIOSITY-001-BR-EXTERNAL"
        "asBuilt" "BrainSlotInvocationService.InvokeAsync runs the configured provider with the shared BrainSlotTurnContext (BrainSlotInvocationService.cs:114-116) inside a turn transaction (145-168); refused when transactions are not required (88-89)"
    }
}
qmdCurLocalResearch = element "Local Research" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,Action,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "J"
    }
}
qmdCurAotReview = element "Judgment + AoT Review" "Activity: AD-CURIOSITY-001" "AoT reconciles Curiosity evidence" "Activity,Action" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "K"
        "asBuilt" "ExecuteAotReconciliationAsync accepts CuriosityEngine output as committed evidence (QuadBrainOrchestrationService.cs:214-239, prompt at 522)"
    }
}
qmdCurApproved = element "Approved?" "Activity: AD-CURIOSITY-001" "Planned: no approval gate" "Activity,Decision,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "L"
        "deferred" "No AoT approval gate before admission; admission needs a committed CuriosityEngine invocation with AdmitToGraphRag (BrainSlotInvocationService.cs:184)."
    }
}
qmdCurInject = element "Inject into Cache + GraphRAG" "Activity: AD-CURIOSITY-001" "GraphRAG admission of committed output" "Activity,Action" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "M"
        "branchId" "AD-CURIOSITY-001-BR-INJECT"
        "asBuilt" "Context admission of committed CuriosityEngine output (BrainSlotInvocationService.cs:184-194); other roles rejected (86-87); no cross-turn cache"
    }
}
qmdCurDiscard = element "Discard" "Activity: AD-CURIOSITY-001" "Planned: no code" "Activity,End,Planned" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "N"
    }
}
qmdCurWeightUpdate = element "Weight Update?" "Activity: AD-CURIOSITY-001" "Explicit weight-update request" "Activity,Decision" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "O"
        "asBuilt" "request.WeightUpdate check (QuadBrainOrchestrationService.cs:192-197)"
    }
}
qmdCurDualControlUpdate = element "Dual-Control Weight Update" "Activity: AD-CURIOSITY-001" "Safety-gated weight update" "Activity,End" {
    properties {
        "diagram" "AD-CURIOSITY-001"
        "mermaidNode" "P"
        "branchId" "AD-CURIOSITY-001-BR-WEIGHT"
        "elaboratedBy" "AD-WEIGHT-001"
        "asBuilt" "ExecuteWeightUpdateAsync requires AotApproved and AdminApproved (QuadBrainOrchestrationService.cs:288-373, check at 294)"
    }
}

qmdCurMonitor -> qmdCurFrustration "next" "Control flow" "Planned"
qmdCurFrustration -> qmdCurTargetedResearch "Yes" "Control flow" "Planned"
qmdCurFrustration -> qmdCurStruggleNovel "No" "Control flow" "Planned"
qmdCurStruggleNovel -> qmdCurValueRanking "Yes" "Control flow" "Planned"
qmdCurValueRanking -> qmdCurHighValue "next" "Control flow" "Planned"
qmdCurHighValue -> qmdCurResearchExperiments "Yes" "Control flow" "Planned"
qmdCurResearchExperiments -> qmdCurExternalNeeded "next" "Control flow" "Planned"
qmdCurExternalNeeded -> qmdCurCallExternal "Yes" "Control flow" "Planned"
qmdCurExternalNeeded -> qmdCurLocalResearch "No" "Control flow" "Planned"
qmdCurCallExternal -> qmdCurAotReview "next" "Control flow" "Planned" {
    properties {
        "deferred" "The escalation path returns without an AoT review (QuadBrainOrchestrationService.cs:109-126); AoT review of Curiosity output is a separate call."
    }
}
qmdCurLocalResearch -> qmdCurAotReview "next" "Control flow" "Planned"
qmdCurAotReview -> qmdCurApproved "next" "Control flow" "Planned"
qmdCurApproved -> qmdCurInject "Yes" "Control flow" "Planned"
qmdCurApproved -> qmdCurDiscard "No" "Control flow" "Planned"
qmdCurInject -> qmdCurWeightUpdate "next" "Control flow" "Planned" {
    properties {
        "deferred" "The Curiosity escalation path returns before the weight-update step (QuadBrainOrchestrationService.cs:122-125, 192-197)."
    }
}
qmdCurWeightUpdate -> qmdCurDualControlUpdate "Yes" "Control flow" {
    properties {
        "asBuilt" "request.WeightUpdate leads to ExecuteWeightUpdateAsync (QuadBrainOrchestrationService.cs:193-197)"
    }
}

# AD-AOT-001 AoT Reconciliation Process (imported section 3.4): the voting round of QuadBrainOrchestrationService.
qmdAotRejects = element "AoT Rejects" "Activity: AD-AOT-001" "Semantic rejection" "Activity,Start" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "A"
        "branchId" "AD-AOT-001-BR-REJECT"
        "asBuilt" "IsAotSemanticRejection on the committed reconciliation (QuadBrainOrchestrationService.cs:146)"
    }
}
qmdAotOpenReconciliation = element "Open Reconciliation" "Activity: AD-AOT-001" "Voting round starts" "Activity,Action" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "B"
        "asBuilt" "AoT rejection stored as committed evidence (QuadBrainOrchestrationService.cs:148-149)"
    }
}
qmdAotModelRefines = element "Model Refines + Additional Reasoning" "Activity: AD-AOT-001" "Voting prompt with the rejection" "Activity,Action" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "C"
        "asBuilt" "BuildVotingPrompt carries both outputs and the AoT rejection (QuadBrainOrchestrationService.cs:150-163)"
    }
}
qmdAotOthersWeighIn = element "Other Two Models Weigh In" "Activity: AD-AOT-001" "Creativity and Logic vote" "Activity,Action" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "D"
        "asBuilt" "Creativity and Logic votes run in parallel (QuadBrainOrchestrationService.cs:150-169); CuriosityEngine does not vote"
    }
}
qmdAotAllAgree = element "All Three Agree?" "Activity: AD-AOT-001" "Valid votes from both roles" "Activity,Decision" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "E"
        "asBuilt" "Both votes need valid output (QuadBrainOrchestrationService.cs:170-173)"
    }
}
qmdAotRejectionStands = element "Rejection Stands" "Activity: AD-AOT-001" "Orchestration rejected" "Activity,Error" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "G"
        "asBuilt" "RejectOrchestration fails closed (QuadBrainOrchestrationService.cs:170-173, 188-189)"
    }
}
qmdAotAccepts = element "AoT Accepts?" "Activity: AD-AOT-001" "Second semantic check" "Activity,Decision" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "H"
        "asBuilt" "IsCommitted and IsAotSemanticRejection on the second reconciliation (QuadBrainOrchestrationService.cs:188)"
    }
}
qmdAotApproveRevised = element "Approve Revised Result" "Activity: AD-AOT-001" "Committed final decision" "Activity,End" {
    properties {
        "diagram" "AD-AOT-001"
        "mermaidNode" "I"
        "asBuilt" "Committed QuadBrainOrchestrationResponse from the revised reconciliation (QuadBrainOrchestrationService.cs:199-210)"
    }
}

qmdAotRejects -> qmdAotOpenReconciliation "next" "Control flow"
qmdAotOpenReconciliation -> qmdAotModelRefines "next" "Control flow"
qmdAotModelRefines -> qmdAotOthersWeighIn "next" "Control flow"
qmdAotOthersWeighIn -> qmdAotAllAgree "next" "Control flow"
qmdAotAllAgree -> qmdCombinedArgument "Yes" "Control flow" {
    properties {
        "branchId" "AD-AOT-001-BR-AGREE"
    }
}
qmdAotAllAgree -> qmdAotRejectionStands "No" "Control flow" {
    properties {
        "branchId" "AD-AOT-001-BR-DISAGREE"
    }
}
qmdCombinedArgument -> qmdAotAccepts "next" "Control flow"
qmdAotAccepts -> qmdAotApproveRevised "Yes" "Control flow" {
    properties {
        "branchId" "AD-AOT-001-BR-ACCEPT"
    }
}
qmdAotAccepts -> qmdAotRejectionStands "No" "Control flow"

# AD-WEIGHT-001 Weight Redistribution Safety (imported section 3.5): ExecuteWeightUpdateAsync. As-built, the gate checks
# run before the snapshot and update, which run inside one turn transaction.
qmdWgtProposes = element "Curiosity Proposes Update" "Activity: AD-WEIGHT-001" "Explicit caller-supplied request" "Activity,Start" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "A"
        "branchId" "AD-WEIGHT-001-BR-PROPOSE"
        "asBuilt" "QuadBrainWeightUpdateRequest with ProposedBy (src/QBrainAi.Support.Mcp/Services/BrainSlotContracts.cs:634); autonomous Curiosity proposal stays disabled"
    }
}
qmdWgtAotReview = element "AoT Review" "Activity: AD-WEIGHT-001" "AoT reconciliation before the update" "Activity,Action" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "B"
        "asBuilt" "Full orchestration runs AoT reconciliation before the weight update (QuadBrainOrchestrationService.cs:133-197)"
    }
}
qmdWgtAotApproves = element "AoT Approves?" "Activity: AD-WEIGHT-001" "aotApproved required" "Activity,Decision" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "C"
        "branchId" "AD-WEIGHT-001-BR-AOT"
        "asBuilt" "AotApproved check (QuadBrainOrchestrationService.cs:294)"
    }
}
qmdWgtAdminApproval = element "Human/Admin Approval?" "Activity: AD-WEIGHT-001" "adminApproved required" "Activity,Decision" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "D"
        "branchId" "AD-WEIGHT-001-BR-HUMAN"
        "asBuilt" "AdminApproved check (QuadBrainOrchestrationService.cs:294)"
    }
}
qmdWgtSnapshot = element "Create Snapshot" "Activity: AD-WEIGHT-001" "Before and after role snapshots" "Activity,Action" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "E"
        "branchId" "AD-WEIGHT-001-BR-SNAPSHOT"
        "asBuilt" "ApplyWeightUpdateAsync returns per-role snapshots and a rollback inside the turn transaction (QuadBrainOrchestrationService.cs:337-358)"
    }
}
qmdWgtRunGates = element "Run Safety Gates" "Activity: AD-WEIGHT-001" "Gate, reason and version checks" "Activity,Action" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "F"
        "asBuilt" "Checks safetyGatesPassed, reason text, required transactions and expected weight versions (QuadBrainOrchestrationService.cs:294-322); the caller runs the gates themselves"
    }
}
qmdWgtGatesPassed = element "Gates Passed?" "Activity: AD-WEIGHT-001" "Safety-gated update check" "Activity,Decision" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "G"
        "branchId" "AD-WEIGHT-001-BR-GATES"
        "asBuilt" "Rejects unless AoT approval, admin approval, safety gates passed, versions and reason text hold (QuadBrainOrchestrationService.cs:294-322)"
    }
}
qmdWgtApply = element "Apply Update + Quarantine" "Activity: AD-WEIGHT-001" "Apply built; quarantine deferred" "Activity,End" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "H"
        "branchId" "AD-WEIGHT-001-BR-QUARANTINE"
        "asBuilt" "ApplyWeightUpdateAsync inside a turn transaction (QuadBrainOrchestrationService.cs:337-372)"
        "deferred" "Quarantine workflows and automated model fine-tuning remain future work (repo annotation)."
    }
}
qmdWgtReject = element "Reject" "Activity: AD-WEIGHT-001" "WeightUpdateRejected" "Activity,Error" {
    properties {
        "diagram" "AD-WEIGHT-001"
        "mermaidNode" "I"
        "asBuilt" "RejectWeight with WeightUpdateRejected or a version, readiness or commit reason (QuadBrainOrchestrationService.cs:294-322, 360-361)"
    }
}

qmdWgtProposes -> qmdWgtAotReview "next" "Control flow"
qmdWgtAotReview -> qmdWgtAotApproves "next" "Control flow"
qmdWgtAotApproves -> qmdWgtAdminApproval "Yes" "Control flow"
qmdWgtAotApproves -> qmdWgtReject "No" "Control flow"
qmdWgtAdminApproval -> qmdWgtSnapshot "Yes" "Control flow"
qmdWgtAdminApproval -> qmdWgtReject "No" "Control flow"
qmdWgtSnapshot -> qmdWgtRunGates "next" "Control flow"
qmdWgtRunGates -> qmdWgtGatesPassed "next" "Control flow"
qmdWgtGatesPassed -> qmdWgtApply "Yes" "Control flow"
qmdWgtGatesPassed -> qmdWgtReject "No" "Control flow"
