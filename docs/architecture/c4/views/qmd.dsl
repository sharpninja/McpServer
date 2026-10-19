# PLAN-C4MODEL-001 P4: views of docs/Project/Quad-Model-Transactional-Diffgram-Plan.md (DocCode QMD). Keys are the six
# imported diagram ids (D6, TR-MCP-TXNDIAGRAMS-001); page parts are <id>-part<N> and branch views use the repo-annotation
# branch ids. Each part repeats the node at its boundary (AC-5). These views include Planned material on purpose.

# AD-TXN-001 Normal Turn Transaction (imported section 3.1): 16 nodes, 16 edges in four views.
custom "AD-TXN-001" "AD-TXN-001 Normal Turn Transaction (1 of 4: subscriber check and manifest signing)" "Imported section 3.1, nodes A to E: AD-TXN-001-BR-NO-SUBSCRIBER and AD-TXN-001-BR-START. As-built, each role invocation runs its own turn transaction after the model call." {
    include qmdTxnTurnStarts qmdTxnSubscriberPresent qmdTxnDegraded qmdTxnStartTransaction qmdTxnSignManifest
    autolayout tb 100 50
}
custom "AD-TXN-001-part2" "AD-TXN-001 Normal Turn Transaction (2 of 4: role processing and AoT decision)" "Imported section 3.1, nodes E to H." {
    include qmdTxnSignManifest qmdTxnModelsProcess qmdTxnAotArbitrates qmdTxnAotApproves
    autolayout tb 100 50
}
custom "AD-TXN-001-BR-AOT-APPROVED" "AD-TXN-001 Normal Turn Transaction (3 of 4: AD-TXN-001-BR-AOT-APPROVED)" "Imported section 3.1, nodes H, M, N, O, P: the AoT approves and the final diffgram commits." {
    include qmdTxnAotApproves qmdTxnDiffgramPublished qmdTxnSubscriberCommits qmdTxnCommitted qmdTxnResponseReturned
    autolayout tb 100 50
}
custom "AD-TXN-001-BR-AOT-REJECTED" "AD-TXN-001 Normal Turn Transaction (4 of 4: AD-TXN-001-BR-AOT-REJECTED and AD-TXN-001-BR-ROLLBACK)" "Imported section 3.1, nodes H, I, J, K, L: reconciliation after an AoT rejection; the J to L edge is Planned." {
    include qmdTxnAotApproves qmdTxnReconciliation qmdTxnModelsAgree qmdCombinedArgument qmdTxnAbortRollback
    autolayout tb 100 50
}

# AD-CURIOSITY-001 Curiosity Engine Flow (imported section 3.2): 16 nodes, 16 edges in four views.
custom "AD-CURIOSITY-001" "AD-CURIOSITY-001 Curiosity Engine Flow (1 of 4: monitoring and frustration)" "Imported section 3.2, nodes A to E. Planned elements have no code; the autonomous flow stays disabled." {
    include qmdCurMonitor qmdCurFrustration qmdCurTargetedResearch qmdCurStruggleNovel qmdCurValueRanking
    autolayout tb 100 50
}
custom "AD-CURIOSITY-001-part2" "AD-CURIOSITY-001 Curiosity Engine Flow (2 of 4: value ranking and research)" "Imported section 3.2, nodes E to H." {
    include qmdCurValueRanking qmdCurHighValue qmdCurResearchExperiments qmdCurExternalNeeded
    autolayout tb 100 50
}
custom "AD-CURIOSITY-001-part3" "AD-CURIOSITY-001 Curiosity Engine Flow (3 of 4: external or local research and AoT review)" "Imported section 3.2, nodes H to L." {
    include qmdCurExternalNeeded qmdCurCallExternal qmdCurLocalResearch qmdCurAotReview qmdCurApproved
    autolayout tb 100 50
}
custom "AD-CURIOSITY-001-part4" "AD-CURIOSITY-001 Curiosity Engine Flow (4 of 4: admission and weight update)" "Imported section 3.2, nodes L to P." {
    include qmdCurApproved qmdCurInject qmdCurDiscard qmdCurWeightUpdate qmdCurDualControlUpdate
    autolayout tb 100 50
}

# SD-DIFFGRAM-001 Three-Party Diffgram Exchange (imported section 3.3): 8 messages, alt with 2 branches. A part holds at
# most two participants (page width) and at most one self step per participant (the renderer draws self steps of one
# element on top of each other); step numbers restart in each view, so the view titles name the source message numbers.
dynamic qbrain.server "SD-DIFFGRAM-001" "Imported section 3.3, messages 1 and 2 (publisher and 3PKS), drawn with the default in-process parties of the MCP Context Server (Program.cs AddInProcessTransactionSecurity); the standalone Key Server and Transaction Subscriber hosts run the same services." {
    title "SD-DIFFGRAM-001 Three-Party Diffgram Exchange (1 of 3: messages 1 and 2)"
    qbrain.server.txsec -> qbrain.server.qmdKeyServer "Public Keys + Diffgram SHA"
    qbrain.server.qmdKeyServer -> qbrain.server.qmdKeyServer "Sign Transaction Manifest"
    autolayout lr 100 50
}
dynamic qbrain.server "SD-DIFFGRAM-001-part2" "Imported section 3.3, messages 3 and 4 (3PKS and subscriber)." {
    title "SD-DIFFGRAM-001 Three-Party Diffgram Exchange (2 of 3: messages 3 and 4)"
    qbrain.server.qmdKeyServer -> qbrain.server.qmdSubscriber "Encrypted SHA + Encrypted Diffgram"
    qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Decrypt SHA (3PKS Pub + Own Priv)"
    autolayout lr 100 50
}
dynamic qbrain.server "SD-DIFFGRAM-001-part3" "Imported section 3.3, message 5 (subscriber), the last step before the alt block." {
    title "SD-DIFFGRAM-001 Three-Party Diffgram Exchange (3 of 3: message 5)"
    qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Verify SHA"
    autolayout lr 100 50
}
dynamic qbrain.server "SD-DIFFGRAM-001-BR-INVALID" "Imported section 3.3, alt Invalid (SD-DIFFGRAM-001-BR-INVALID): an invalid hash, signature or decrypt aborts and audits." {
    title "SD-DIFFGRAM-001 alt Invalid: message 6"
    qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Abort Transaction"
    autolayout lr 100 50
}
dynamic qbrain.server "SD-DIFFGRAM-001-BR-VALID" "Imported section 3.3, else Valid (SD-DIFFGRAM-001-BR-VALID), first message: a valid manifest and payload commits durably." {
    title "SD-DIFFGRAM-001 else Valid (1 of 2: message 7)"
    qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Decrypt Diffgram (Publisher Pub + Own Priv)"
    autolayout lr 100 50
}
dynamic qbrain.server "SD-DIFFGRAM-001-BR-VALID-part2" "Imported section 3.3, else Valid (SD-DIFFGRAM-001-BR-VALID), second message." {
    title "SD-DIFFGRAM-001 else Valid (2 of 2: message 8)"
    qbrain.server.qmdSubscriber -> qbrain.server.qmdSubscriber "Commit"
    autolayout lr 100 50
}

# AD-AOT-001 AoT Reconciliation Process (imported section 3.4): 9 nodes, 9 edges in two views.
custom "AD-AOT-001" "AD-AOT-001 AoT Reconciliation Process (1 of 2: rejection to the agreement check)" "Imported section 3.4, nodes A to E: the voting round of QuadBrainOrchestrationService." {
    include qmdAotRejects qmdAotOpenReconciliation qmdAotModelRefines qmdAotOthersWeighIn qmdAotAllAgree
    autolayout tb 100 50
}
custom "AD-AOT-001-part2" "AD-AOT-001 AoT Reconciliation Process (2 of 2: combined argument and AoT decision)" "Imported section 3.4, nodes E to I." {
    include qmdAotAllAgree qmdCombinedArgument qmdAotRejectionStands qmdAotAccepts qmdAotApproveRevised
    autolayout tb 100 50
}

# AD-WEIGHT-001 Weight Redistribution Safety (imported section 3.5): 9 nodes, 10 edges in two views.
custom "AD-WEIGHT-001" "AD-WEIGHT-001 Weight Redistribution Safety (1 of 2: proposal and approvals)" "Imported section 3.5, nodes A to D and I: ExecuteWeightUpdateAsync." {
    include qmdWgtProposes qmdWgtAotReview qmdWgtAotApproves qmdWgtAdminApproval qmdWgtReject
    autolayout tb 100 50
}
custom "AD-WEIGHT-001-part2" "AD-WEIGHT-001 Weight Redistribution Safety (2 of 2: snapshot, gates and update)" "Imported section 3.5, nodes D to I. As-built, the gate checks run before the snapshot and update." {
    include qmdWgtAdminApproval qmdWgtSnapshot qmdWgtRunGates qmdWgtGatesPassed qmdWgtApply qmdWgtReject
    autolayout tb 100 50
}

# ARCH-QUAD-001 High-Level System Architecture (imported section 3.6): 9 nodes, 10 edges in five views, filtered to the
# relationships this document draws (tag QMD).
component qbrain.server "ARCH-QUAD-001" "Imported section 3.6 (1 of 5): the user sends each turn to the Arbiter of Truth." {
    title "ARCH-QUAD-001 High-Level System Architecture (1 of 5: user)"
    include developer qbrain.server.qmdArbiterRole
    exclude "relationship.tag!=QMD"
    autolayout tb 100 50
}
component qbrain.server "ARCH-QUAD-001-part2" "Imported section 3.6 (2 of 5): the Arbiter of Truth routes to the three other roles." {
    title "ARCH-QUAD-001 High-Level System Architecture (2 of 5: routing)"
    include qbrain.server.qmdArbiterRole qbrain.server.qmdCreativityRole qbrain.server.qmdLogicRole qbrain.server.qmdCuriosityRole
    exclude "relationship.tag!=QMD"
    autolayout tb 100 50
}
component qbrain.server "ARCH-QUAD-001-part3" "Imported section 3.6 (3 of 5): the three roles share the cache." {
    title "ARCH-QUAD-001 High-Level System Architecture (3 of 5: shared cache)"
    include qbrain.server.qmdCreativityRole qbrain.server.qmdLogicRole qbrain.server.qmdCuriosityRole qbrain.server.qmdSharedCache
    exclude "relationship.tag!=QMD"
    autolayout tb 100 50
}
component qbrain.server "ARCH-QUAD-001-part4" "Imported section 3.6 (4 of 5): the cache feeds the ACID pub-sub." {
    title "ARCH-QUAD-001 High-Level System Architecture (4 of 5: cache to pub-sub)"
    include qbrain.server.qmdSharedCache qbrain.libs.transactionSecurity
    exclude "relationship.tag!=QMD"
    autolayout tb 100 50
}
component qbrain.server "ARCH-QUAD-001-part5" "Imported section 3.6 (5 of 5): the External group, with 3PKS and the subscriber feeding the ACID pub-sub." {
    title "ARCH-QUAD-001 High-Level System Architecture (5 of 5: external parties)"
    include qbrain.libs.transactionSecurity qbrain.keyserver qbrain.subscriber
    exclude "relationship.tag!=QMD"
    autolayout tb 100 50
}
