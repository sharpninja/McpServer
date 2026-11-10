@{
    OverallVerdict = 'DISAGREE'
    Accuracy = 96
    Completeness = 96
    Confidence = 99
    PolicyAffectedTasks = @(8,10,14,15,21,22,23,24,25,29,30,40)
    OutOfScopeTasks = @(12,13,35)
    AdditionalTaskRequirementIds = @{
        3=@('FR-MCP-PATHNORM-001','TEST-MCP-PATHNORM-001','TEST-HANDOFF-006','TEST-MCP-083','TEST-MCP-TRIAGE-004','TR-MCP-MT-002')
        7=@('FR-MCP-FED-PATH-001','TR-MCP-FED-PATH-001','TEST-MCP-PATHNORM-001','TR-MCP-DB-001','TR-MCP-GH-003','TR-MCP-GH-004')
        10=@('TR-MCP-NUKE-019','FR-MCP-FED-PATH-001','FR-MCP-PATHNORM-001','TEST-MCP-FED-PATH-001','TEST-MCP-TRIAGE-003','TEST-MCP-USECASE-019','TEST-MCP-USECASE-020','TR-MCP-FED-PATH-001','TR-MCP-QBRAIN-005')
        14=@('FR-MCP-QBSKILLS-003','TR-MCP-QUALITY-001')
        21=@('FR-MCP-077','TR-MCP-CFG-007')
        22=@('TEST-MCP-177','TR-MCP-NUKE-019','TR-MCP-QBRAIN-004','TR-MCP-QBRAIN-005')
        23=@('TEST-MCP-TRIAGE-003','TR-MCP-NUKE-019','TR-MCP-WS-005')
        24=@('TR-MCP-NUKE-019')
        25=@('FR-MCP-REPL-005','TEST-MCP-QBOLLAMA-001','TR-MCP-USECASE-018')
        27=@('TEST-MCP-QBOLLAMA-001')
        29=@('TEST-MCP-TRIAGE-003','TEST-MCP-TRANSCRIPT-008','TEST-MCP-QBOLLAMA-001','FR-MCP-REPL-008')
        30=@('FR-MCP-083','FR-MCP-103','TEST-MCP-PATHNORM-001','TEST-MCP-QBOPENAI-001','TEST-MCP-WIKIEXPORT-001','TR-MCP-GH-003','TR-MCP-GH-004','TR-MCP-PATHNORM-001')
        31=@('TR-MCP-NUKE-019')
        32=@('TR-MCP-NUKE-019','FR-MCP-FED-PATH-001')
        36=@('FR-MCP-BUILD-001','TEST-MCP-QBOLLAMA-001','TR-MCP-NUKE-019')
        37=@('FR-MCP-BUILD-001','FR-MCP-FED-PATH-001','TR-MCP-FED-PATH-001','TR-MCP-NUKE-019','TR-MCP-NUKE-020')
        39=@('FR-MCP-TESTRESOURCE-001')
        42=@('TEST-MCP-089')
        45=@('TEST-MCP-089')
        47=@('FR-MCP-103','TR-MCP-NUKE-019','TR-MCP-USECASE-019')
        49=@('FR-MCP-103')
        50=@('TR-MCP-USECASE-018')
        51=@('TEST-MCP-TRIAGESTORE-007','TR-MCP-USECASE-018')
        52=@('FR-MCP-079','FR-MCP-103','FR-MCP-MEMORY-001')
        53=@('FR-MCP-103','FR-MCP-TESTRESOURCE-001')
        54=@('FR-MCP-077','FR-MCP-103')
        57=@('TR-MCP-USECASE-019')
        58=@('TR-MCP-USECASE-019')
        59=@('TR-MCP-USECASE-019')
        60=@('TR-MCP-USECASE-019')
        61=@('TR-MCP-USECASE-019')
    }
    MatrixInterpretation = 'Mappings combine exact task text, the named plan section, live requirement records and changed-source attribution. AdditionalTaskRequirementIds comes from the independent per-slice added-line citation audit; fixture records FR-HANDOFF-001 and TR-HANDOFF-CONTRACT-001 are deliberately excluded from task 53. A mapping identifies the portion changed or proved by that task, not acceptance of every criterion of the record. Retired IDs appear only where the reviewed change deletes/repoints them. The operational tasks 12, 13 and 35 have no product requirement in flux; no outcome is adjudicated for them. Shared commits 765490a9 and 611a5dc5 are partitioned by the plan slices, not by copying every commit citation to each task.'
    Findings = @(
        @{
            id='R14-F1'; classification='implementer edit'; title='Z15 binding chronology is understated'; claims=@('UG8','UG11','UG59'); tasks=@(60)
            location='docs/plans/2026-10-19-unit-suite-green.md:2425,2428,2444; docs/receipts/hv/unitgreen-r14-prelaunch.pr-description.md:228; brief UG59; relaunch self-review PHASE-BOUNDARIES'
            defect='The slip names only AnyRemovalCanAwait as binding code written before the mock gates, and the later binding step places the counting decorator after production. The sessionCounter parameter, resolver registration, pool wiring and all three counting types were already written before the red gate.'
            evidence='Original implementer transcript lines 164203-164206: 2026-11-06 10:15:19-10:15:37 local. Red gate began 10:16:30; green gate began 10:20:24. Both receipts pin VoiceConversationServiceTests.cs at E7CEE77D. The service file remained at its Z14 hash. Independently read in this reviewer turn; first-launch candidates were not adopted as verdict evidence.'
            changeToPass='Correct every current chronology and slip disclosure to name the complete early binding block and its order. Preserve immutable receipts and commit messages, adding a dated correction where a current record relies on them.'
            requirementIds=@('TR-MCP-USECASE-018'); proposedAcceptanceCriteria=@(); proposedTests=@(); options=@()
        },
        @{
            id='R14-F2'; classification='implementer edit'; title='The criterion is credited with a source-code fact it does not state'; claims=@('UG8','UG11','UG59'); tasks=@(60)
            location='docs/plans/2026-10-19-unit-suite-green.md:2423; docs/receipts/hv/unitgreen-r14-prelaunch.pr-description.md:488; docs/plans/2026-10-30-unit-suite-green-handoff.md:20; relaunch self-review:29; brief UG59'
            defect='These records say AC-006 states that the resolver is the only source of an interactive session. Its example states only the generic condition that no dependency handed the code a resource whose release awaits.'
            evidence='Live TR-MCP-USECASE-018 AC-006, 10818 characters, and the retained Z15 readback. The specific resolver fact appears in the source/test documentation, not in the criterion. Z16 separately supplies direct completion evidence.'
            changeToPass='Attribute the resolver fact to the inspected code and test documentation, and attribute the generic resource check to AC-006. Do not rewrite the criterion merely to make this historical attribution true.'
            requirementIds=@('TR-MCP-USECASE-018'); proposedAcceptanceCriteria=@(); proposedTests=@(); options=@()
        },
        @{
            id='R14-F3'; classification='implementer edit'; title='A claimed document-validation receipt does not exist'; claims=@('UG1','UG8','UG11','UG59'); tasks=@(60)
            location='Brief UG1 and UG59; docs/receipts/hv/unitgreen-r14-prelaunch.pr-description.md; docs/plans/2026-10-19-unit-suite-green.md:2479'
            defect='The current gate-assurance narrative cites ug-r14z15-doc-claim-checks-1.txt as a completed retained check. That file is absent. Declaring outstanding record defects in the addendum does not make the asserted check evidence.'
            evidence='Direct file inspection returned ItemNotFound; repository/file inventory contains no such receipt. Plan 27.43:2479 also explicitly says it does not exist. The current 30 TRX files and their counters/hashes were independently verified and remain valid.'
            changeToPass='Remove the false assertion that the Z15 check is retained or ran as claimed. Either retain a new read-only audit of the final records with its real date/head, or explicitly state that no Z15 document-check receipt is evidence. Never backdate a replacement.'
            requirementIds=@('TR-MCP-USECASE-018'); proposedAcceptanceCriteria=@(); proposedTests=@(); options=@()
        },
        @{
            id='R14-F4'; classification='operator decision'; title='Approved mixed-project layout conflicts with the current project-separation rule'; claims=@('WS1'); tasks=@(8,10,14,15,21,22,23,24,25,29,30,40)
            location='Global MEMORY-BESTPRACTICE-001 v2 rules 4.1 and 4.4; TR-MCP-NUKE-019 AC-2; build/Build.Test.cs; build/Build.TestDependencies.cs; tests/QBrainAi.QBAgent.Tests/QBAgentHeartbeatOverlayTests.cs:43; tests/QBrainAi.Support.Mcp.Tests/TestSupport/CliTempVariablesTests.cs'
            defect='The live rule requires unit and integration tests in separate projects across workspaces. This PR deliberately keeps both categories in projects selected by Test and MigrationIntegrationTests. Category filtering satisfies the approved lane contract but does not satisfy separate project ownership. No explicit exception to the newer rule was supplied or found.'
            evidence='Full live memory text was read through the Codex plugin. QBAgentHeartbeatOverlayTests has unit facts and an Integration-tagged heartbeat case in one project; CliTempVariablesTests has scripted unit cases and environment-writing Integration cases in Support.Mcp.Tests. Project sources and selectors were inspected, and the corresponding unit/lane TRX selections agree with the category filters. This is not evidence that an Integration case ran as a unit case.'
            changeToPass='The operator must resolve the rule conflict. If separation applies to this PR, reconcile the affected requirements and project ownership, implement the structural move under BDPv4 without losing cases/assertions, and supply the replacement full gate evidence. Alternatively, explicitly approve a PR87 workspace exception retaining category separation. No scanner change is requested.'
            requirementIds=@('TR-MCP-NUKE-019','TR-MCP-USECASE-018','TR-MCP-USECASE-019')
            proposedAcceptanceCriteria=@(@{name='test-project-ownership'; statement='If project separation is selected: given a declared project role and a supplied case inventory, unit selection accepts only unit-owned projects containing unit cases, integration selection accepts only integration-owned projects containing integration cases, and valid selections preserve every intended case exactly once.'})
            proposedTests=@(@{name='UnitProject_WithIntegrationCase_IsRejected'; statement='A scripted unit-project inventory containing an Integration case is rejected.'},@{name='IntegrationProject_WithUnitCase_IsRejected'; statement='A scripted integration-project inventory containing a unit case is rejected.'},@{name='SeparatedProjects_PreserveSelectedCases'; statement='Independent scripted valid inventories preserve all selected case identities, and wrong selectors that omit or duplicate cases are rejected.'})
            options=@('Apply separate project ownership in PR87, reconcile requirements, implement and supply new gates.','Explicitly retain PR87 category-filtered mixed projects as a workspace exception. The existing functional evidence can then be assessed without this conflict.')
        },
        @{
            id='R14-F5'; classification='implementer edit'; title='Current prelaunch records misstate checks and historical run/commit attribution'; claims=@('UG8','UG11'); tasks=@()
            location='docs/receipts/hv/20261106T181559-unitgreen-r14.selfreview.md:79; docs/receipts/hv/unitgreen-r14-prelaunch.projected-problems.md:64,71,74'
            defect='The self-review says the launcher checks all three policy 7.9 sentences, but it checks only sentences two and three plus the manifest. Projected problems 130 and 137 attribute the hung-save case Integration move to slice W, but commit 765490a9 made it. Item 140 calls eight comparison lanes passing even though one had a failed hung-save case; their SQL Server cases passed.'
            evidence='Invoke-HvRound.ps1:92 and :104-133, read only. git log -G Category.*Integration over SessionLogTriageStoreTests.cs returns 765490a9035878c7888c911776289880493dacab. ug-r13z5-win-lane-elapsed-bound-durations-1.txt:25 records Failed=1 Passed=8 across nine runs, with ug-r13z4c-final-win-lane-1 the failed case; the corresponding failed lane is retained and declared elsewhere. These independently confirm eighteenth-read C5, C19 and C21.'
            changeToPass='Correct the current assertions to the actual launcher checks, commit and successful-case scope. Propagate those factual corrections to every current copy; retain previous review artifacts as history. This requires record edits, not a product-code change or another test run.'
            requirementIds=@('TR-MCP-USECASE-018','TR-MCP-NUKE-019','TEST-MCP-TRIAGESTORE-007'); proposedAcceptanceCriteria=@(); proposedTests=@(); options=@()
        }
    )
    ClaimRows = @(
        'UG1|FAIL|The current da11faa2 gates, 34 evidence-file hashes, 30 TRX counters/result-definition joins, seven-project selections and docs-only tail pass. Unit 4866/4866, lanes 741/718, Build.Tests 563/467, zero failures/skips. The additional assertion of a retained Z15 document-check receipt fails R14-F3.|R14-F3',
        'UG2|PASS|Reviewed requirements, mock-gate, binding and commit chronology for the implementation slices, including Z16 red control, green mock gate with unchanged production, then service/binding. Retrospective receipts identify their provenance; failed attempts are not used as green gates. Z15 early binding accounting is separately R14-F1; its production still followed the green mock gate.|',
        'UG3|PASS|Inspected unit callers and clock/environment seams, timing and background sweeps and their exclusions. Z16 observes the actual triggered cleanup task before decisions; D8/D9 use scripted boundaries. Unit multisets agree on both hosts. No remaining environment-, platform- or elapsed-time-dependent unit outcome was established. Project ownership is separately WS1.|',
        'UG4|PASS|TR-MCP-NUKE-019 AC-1/2/3/6 match the seven-project selection, two Support integration classes, logged filters and owned-instance checks. All selected lane results pass. The newer separate-project policy conflict is WS1, not a false statement that these ACs or lane counts failed.|',
        'UG5|PASS|Directory.Packages.props pins and retained Compile/Test logs support QUALITY ac-21, including Roslyn 5.9.0 and repaired cryptography versions. Current gate logs report zero NU190x without an audit-disable override; no-op restore limitations remain disclosed.|',
        'UG6|PASS|PATHNORM FR/TR/TEST records and mappings are present. Explicit host inputs, the independent path contract and real binding preserve native/foreign path decisions without choosing expected results from the executing host. Current unit results include the cases.|',
        'UG7|PASS|Live effective requirements, selected dedicated getters, criterion chains and all 61 task text hashes/states match the supplied snapshot and amendments. Retired FR-USECASE-016/017 are absent through both routes. Independent changed-line citation inspection found 101 IDs, 39 absent, all historical labels or three fixture-data IDs. Follow-up TODO states and BUG-139 links agree.|',
        'UG8|FAIL|The full plan and amendments were read. The revised history rule resolves superseded current values and explicit historical corrections. It cannot cure inaccurate Z15 chronology, attribution, a nonexistent check or false current assurance copies. See R14-F1, F2, F3 and F5.|R14-F1,R14-F2,R14-F3,R14-F5',
        'UG9|PASS|Takeover commit 11681680 and its recorded source c2125c42 preserve the 16 transferred files; the separate C4 reversal and ownership ruling are retained. Subsequent modifications belong to this branch and are represented in its history.|',
        'UG10|PASS|131 first-parent commits carry the required author/model signing line; this is not a cryptographic signature claim. Current plan/full PR have no prohibited tables or en/em dashes. Changed blobs contain no current marker key. Historical scripting exceptions remain disclosed; TRX committed bytes match hashes. No reviewer tracked edit or push occurred.|',
        'UG11|FAIL|Self-review, projected problems, PR summary/full description and handoff were read in full. R13 fixes are disclosed, but R14-F1/F2/F3/F5 leave false current factual statements. The launch addendum honestly declares open defects; it does not waive their correction.|R14-F1,R14-F2,R14-F3,R14-F5',
        'UG12|PASS|Latest D-B at plan 27.42-27.43 and the addendum conditions criterion satisfaction on overall AGREE, task completion on each task AGREE, and records both TODO and plan updates. FR-040 waits for post-merge server plus REPL deployment/live read; startup-health-wait remains excluded. No current task/criterion was prematurely set.|',
        'UG13|PASS|Reattacked original findings and repaired siblings against current source, records and supplied evidence. R13-F1 is closed through Z12-Z16; R13-F2 is independently confirmed by 18 matching compile pins and no binding pins. The detailed closure list separates original repairs from new record and policy findings.|',
        'UG14|PASS|DirectoryTreeWalker, AgentPluginVersionSources, marker resolution and injected read errors preserve the owned source seam and fallback behavior. Real-profile/native boundaries are Integration and their selected lane cases pass; scripted unit resolution is independent of ambient plugin caches.|',
        'UG15|PASS|LocalDB default timeout and the four-class nonparallel collection remain. Windows lane selected SQL Server cases are present and passed. Historical command-timeout failures and the operator-selected rerun are not recast as successful first runs.|',
        'UG16|PASS|The pg_ctl quiet start remains. Current Windows lane result identities/multiplicities match the previous complete lane, with all 718 results. No lost result or definition-only substitute is counted.|',
        'UG17|PASS|Operator choices were compared with the plan, code, store and transcript. Declared departures, counts and later rulings are retained. No additional scanner work is required or scored. The newer project-ownership conflict is separately exposed for an operator decision in WS1.|',
        'UG18|OVERRIDDEN|UnitSuitePlatformScanner implementation, tests and coverage descriptions are wholly excluded by the explicit operator override.|',
        'UG19|PASS|Handoff and memory harnesses give concurrent contexts separate connections to owned SQLite files and preserve assertions. Memory binding cites the present mapped FR/TR/TEST-MCP-MEMORY-002. Current unit evidence contains the cases.|',
        'UG20|OUT OF SCOPE|Hostile-environment runs are excluded by the explicit operator rulings. Their absence is neither a failure nor an evidence gap for this review.|',
        'UG21|PASS|DirectoryTreeWalker checks linked scan roots and ancestors before traversal and skips linked descendants. Scripted reachability controls and native integration bindings remain; the supplied lane evidence covers the approved native cases.|',
        'UG22|PASS|CliTempVariables owns all five resolver/fallback names, separates primary from fallback folders and restores exact prior values, including exceptions. Caller inspection and Process environment serialization checks cover the four CLI resolver families. Current unit/lane evidence contains the relevant cases.|',
        'UG23|PASS|ReplCommandTimeout accepts only bounded positive whole seconds and otherwise defaults; both call sites use the injected read seam. Scripted wrong parsers/call sites are rejected and current unit TRX includes their bindings.|',
        'UG24|PASS|WindowsJunction uses native in-process creation; the buffer contract and controls remain. The sibling traversal fixture uses the same helper. Native cases are correctly selected by the Windows lane, with no process-launch timing substituted for the fixture outcome.|',
        'UG25|PASS|Moved marker-walk criteria are present on FR-MCP-REPL-007; TR-MCP-REPL-008 no longer duplicates them. Scripted filesystem walk cases and production binding preserve precedence and refusal behavior. Historical AC IDs are interpreted through the recorded move.|',
        'UG26|PASS|Develop is an ancestor of the reviewed head; legacy read-only merge-tree of develop into HEAD is empty, exit 0. d43c1d12 is docs-only. The manifest inventories 23 projects and supplies every selected unit/lane project; held-out integration scope is explicit. Citation labels were independently checked.|',
        'UG27|PASS|DesktopProcessSpawner compares explicitly supplied maps; equal values are omitted and changed values retained. The ambient overload delegates. The GH_TOKEN integration case owns/restores its baseline and is serialized with environment writers. Scripted positive/negative contracts and both-host results remain.|',
        'UG28|PASS|Inspected TemporaryInstanceChecks, OwnedInstanceProcess, OwnedReplStart, ScratchQBrainAi, the factory and stdio starts. Canonical/legacy sections, configuration-key aliases and host-prefixed owned aliases cannot select an outside database. The factory guard runs before opening the connection. Current contracts and lane cases pass.|',
        'UG29|PASS|Lane selector returns seven projects and only the two approved Support integration class names; filters compose the host predicate and validated class identifiers. The Support integration TRX has exactly those two classes and six cases on each host. The project-layout policy is separate WS1.|',
        'UG30|PASS|Slices I-O and their contract/binding receipts match the final source. Owned starts, retry/port handling, QuadBrain rounds, foreign workspace handling, test changes and scrub are traceable. Lab identifiers in touched files are generic, approved exceptions or published identities; held-out work remains held out. Current policy conflict is WS1.|',
        'UG31|PASS|TestGroups composes named groups from lane pieces, joins executed results to definitions and rejects missing classes, empty/unknown groups, failures and skips. Contracts and retained negative gate discriminate the failures. Build.Tests execution through this target follows the specific approved TRs.|',
        'UG32|PASS|Engine-setting selection, 22-row provider contract, exact path handling and four reviewed GetFullPath uses match the source. Failed binding r-1 is declared and task 32 cites r-2. No absent direct-GetFullPath criterion is claimed satisfied.|',
        'UG33|PASS|Stored timestamp conversion, earliest mapping link and REST/client/REPL adapters match the independent contracts. Exact complete-date parsing preserves UTC/null semantics. Live deployment remains unverified because the plugin REPL predates the fix; task 33 proves implementation, not live deployment or criterion satisfaction.|',
        'UG34|PASS|Restored mapping edges are backed by record references; deleted records are retained in the reconciliation receipt and absent from the effective view. BUG-139 now links the replacement FR/TRs. Projections are a declared historical generated snapshot; current ValidateTraceability passes.|',
        'UG35|PASS|Generated artifacts use owned external output folders. The 39-row contract and wrong checks cover relative/dot/device/cross-syntax/UNC alias cases. Memory benchmark stubs retain their assertions. Benchmark tracked files are unchanged by current lanes; accepted host event-log disclosure is outside this criterion review.|',
        'UG36|PASS|Changed-member documentation and dated-entry retrofits were checked against changed files and canonical citations. Later field/member supplements and type-level logging limits are accurately disclosed. No unrelated retrospective retrofit is required by the live rule.|',
        'UG37|PASS|Slice T retains executed-class identity, exact JSON nonce, syntax-aware paths, complete stored dates and one Ollama model rule. Mock and binding pins/negative controls discriminate the original failures. Strengthened criteria and the sixth approved sibling write are recorded.|',
        'UG38|PASS|Slice U exact date formats, explicit path-root identity and six recorded-time cases on ScriptedClock hold. Mock/binding contract pins and the failed compile history are retained. No missing date component is filled from the read clock.|',
        'UG39|PASS|GraphRagService and MemoryService stamp through supplied clocks; changed tests assert exact scripted instants. Auth/storage fixtures use fixed dates. The older incomplete sweep is explicitly historical; subsequent sweep exclusions were independently examined.|',
        'UG40|PASS|Lease renewal is observed and compared to exact scripted expiry. The heartbeat and watcher cases retain assertions and are Integration-tagged; historical by-name transfers agree. This functional classification repair holds; its project ownership remains subject to R14-F4.|',
        'UG41|PASS|Slice X output guard refuses device paths and syntax aliases; marker callback cancellation is explicit rather than a 100 ms race. Watcher historical citation is labeled. The stopped R11 review has no fabricated verdict, and later ready/draft history is disclosed.|',
        'UG42|PASS|Slice Y applies Windows alias rules to UNC roots and preserves containment behavior. The 39-row contract and rejected checks remain. Its margin review limitation and later Z/Z2/Z3 corrections are declared; constructor/docs fixes are evaluated at the final head.|',
        'UG43|PASS|Slice Z assertions remain, timeout increases and one separator source match the diff. No new criterion was falsely claimed. Later sum-based corrections supersede the original largest-delay measure. The final timing inventory and current TRX support the repaired scope.|',
        'UG44|PASS|Z2 uses the sum of delays; Z3 later covers conditional SQLite retries. The 30 s and 15 s bounds remain above the stated summed paths, with the overcount and cleanup-only swallowed timeout disclosed. Binding and historical by-name results agree.|',
        'UG45|PASS|Repeated unit properties are joined in order and bound to the same contract rows. Health wait uses scripted probes; its known failure to prove a full elapsed ten minutes remains explicitly outside D-B by operator choice. Conditional-wait changes and deployment outage/recovery history are retained.|',
        'UG46|PASS|Failed-update recovery restores then retargets/reloads after replacement; six contract rows and wrong recoveries bind the production path. The operator accepted the declared broader replacement/start-limit gaps in D-B. No build code changed after 52e6d627; later changes are elsewhere.|',
        'UG47|PASS|Z4 key identity and age decisions bind independent contracts; the original raw-prefix and live-clock defects are fixed. D8/D9 and Z16 close the previously missed background sibling. R12-F3 receipt chain and historical-label correction hold; failed lane/compile attempts remain failed history.|',
        'UG48|PASS|Ordinal inherited names, owned-key replacement and read-only REPL start are in place. Four storage-budget Integration cases assert the exact budget and preserve persistence/classification checks; their reduced promptness claim is disclosed and paired with budget timing cases. Incorrect projected-history attribution is separately R14-F5.|',
        'UG49|PASS|Z6 compares both host-prefixed and unprefixed owned settings without collapsing unrelated inherited names or rejecting the intended two host prefixes. Rows and wrong implementations discriminate the sibling defect. Event-log writes are declared under the explicit out-of-scope ruling.|',
        'UG50|PASS|Z7 is documentation-only for 34 members. Independent raw-blob hashing confirms all 18 compile pins match 1d77a79c and the binding has no pins; timestamps prove only post-run pinning, not compile-time bytes. Committed-head gates provide the final compilation evidence.|',
        'UG51|PASS|Z8 generic proxy identifiers preserve relations and assertions, and Z8b documents the two mapping methods. The syntax-aware documentation audit and retained diff/pins support the declared counts and limits. R13-F2 descriptions are corrected with post-run pin limits.|',
        'UG52|PASS|Z9 replaces corresponding lab IDs consistently in four touched federation files. The assertion literal retains the same relationship after renaming. Earlier limited-pattern sweep is not credited with the later classified sweep coverage. Mapping-entry history is preserved after Z11.|',
        'UG53|PASS|Z10 removes the lab-account summary and corrects documentation; the fifth shipped-config exception has the cited operator ruling and QBRAIN-006 backing. The v3 citation audit covers added build files; independent current audit reproduces the 101/39 ID result.|',
        'UG54|PASS|Z11 keeps original dated entries beside explicit corrections and adds the later accurate wording. Store scrub preserves criterion outcomes/flags; the two lab-bound criteria remain unset. Published-identity exceptions and generated-projection limits are disclosed.|',
        'UG55|PASS|Z12 adds the optional production clock, manual test timers and D8/D9/lifecycle contracts before binding. Rechecked sweep exclusions do not establish another uncontrolled lifecycle outcome. Current Z16 assertions strengthen completion observation beyond the original sentinel evidence.|',
        'UG56|PASS|R13-F2 pin correction is independently verified. Task 57 timing and late draft transition remain declared. The 8c944541 gates are historical and their supplied file/hash/count claims are distinct from current da11faa2 evidence. Current task count resolves to 61.|',
        'UG57|PASS|Z13 deferred-pass negative control and sentinel trace match retained red/green evidence. Its XML-only production change and early sentinel binding declaration are accurate. Later Z14-Z16 changes supersede the original completion inference and constructor exception wording.|',
        'UG58|PASS|Z14 changed documentation and two assertion messages only. The NullReferenceException claim is explicitly corrected by Z15 and the no-await condition is ultimately enforced by Z16 direct completion. Historical Z14 is not treated as proof of those later fixes.|',
        'UG59|FAIL|The Z15 counter/controls and constructor correction are present and pass current evidence; Z16 fixes their completion-observation limitation. However this compound claim retains the false early-binding chronology, criterion attribution and nonexistent document-check citation. See R14-F1/F2/F3.|R14-F1,R14-F2,R14-F3',
        'WS1|FAIL|Mandatory workspace-rule surface: the current separate-project rule conflicts with the approved mixed-project selectors and category moves. The operator must choose structural separation or an explicit workspace exception. This is outside the scanner override and does not impugn the passing gate counts.|R14-F4'
    )
    TaskRows = @(
        '1|FR-MCP-PATHNORM-001,TR-MCP-PATHNORM-001,TEST-MCP-PATHNORM-001,TR-MCP-QUALITY-001|7,9,10|Requirement records and unitgreen-native-host are present with the approved narrowing; task text describes the historical write accurately.',
        '2|TR-MCP-QUALITY-001|10 slice A|Central cryptography pins and both retained build receipts agree; current gates have zero NU190x.',
        '3|TR-MCP-USECASE-018,TR-MCP-USECASE-019,TR-MCP-PATHNORM-001|10 B/D/F|Scripted expected results and negative controls precede the explicit-host binding; current cases retain assertions.',
        '4|TR-MCP-USECASE-018,TR-MCP-USECASE-019|10 mock gate|The 4666/0/16 historical result is explicitly not the zero-skip exit gate; current whole unit suites have zero skips.',
        '5|FR-MCP-PATHNORM-001,TR-MCP-PATHNORM-001,TEST-MCP-PATHNORM-001,TR-MCP-USECASE-018|10 slice D|Explicit-host overload and real binding preserve the independent contract; the historical skips are disclosed.',
        '6|TR-MCP-HOSTILEREVIEW-003,TR-MCP-USECASE-018|10 slice F|Startup test removes Event Log provider dependence; supplied unit evidence includes the corrected startup cases.',
        '7|TR-MCP-NUKE-018,TR-MCP-QUALITY-001,TR-MCP-PATHNORM-001|7,8,9|Takeover blobs match their source and later modifications are represented in this branch history.',
        '8|FR-MCP-BUILD-001,TR-MCP-NUKE-019|8,10 slice G|Host-scoped filters and original 21/24 contract/binding counts hold; current project ownership needs R14-F4 resolution.',
        '9|FR-MCP-USECASE-001,TR-MCP-USECASE-018,TR-MCP-USECASE-019,FR-MCP-USECASE-016,FR-MCP-USECASE-017|9,27.21|Retired FRs are absent, replacement mappings exist, and Azure export failure is informational under the explicit override.',
        '10|TR-MCP-USECASE-018,TR-MCP-USECASE-019|9,14|Scanner work is excluded. Category classification holds functionally; mixed-project ownership needs R14-F4 resolution.',
        '11|TR-MCP-QUALITY-001,TR-MCP-NUKE-019,TR-MCP-USECASE-018,TR-MCP-USECASE-019|23,27.42|Current Linux/Windows unit, lane, Build.Tests and traceability gates are complete in the supplied scope with zero failures/skips.',
        '12||operational HV gate|No product requirement in flux is mapped to this review/presentation operation. It remains open without verdict or scores.',
        '13||operational PR/handoff|No product requirement in flux is mapped to this PR/handoff operation. It remains open without verdict or scores.',
        '14|TR-MCP-USECASE-018,TR-MCP-USECASE-019,TR-MCP-NUKE-019|15|Scanner part excluded. Root-locator dependency classification and static runner are supported; current project ownership conflicts with R14-F4.',
        '15|FR-MCP-018,TR-MCP-WS-005,FR-MCP-SESSIONLOGCTX-001,TR-MCP-SESSIONLOG-006|16|Directory traversal/source seams and 12 category moves hold; current mixed-project placement needs R14-F4 resolution.',
        '16|TR-MCP-NUKE-019|16|LocalDB timeout/serialization and quiet pg_ctl preserve intended results; current Windows lane is complete.',
        '17|FR-MCP-018,TR-MCP-WS-005|16|Unreadable version files fall through, unrelated errors propagate, and injected reader controls discriminate.',
        '18|FR-MCP-018,TR-MCP-WS-005,TEST-HANDOFF-004,TEST-HANDOFF-005,TEST-HANDOFF-006,TEST-HANDOFF-007|17|Source seam and owned concurrent-context database repair hold; scanner and hostile-run portions are excluded.',
        '19|FR-MCP-MEMORY-002,TR-MCP-MEMORY-002,TEST-MCP-MEMORY-002,TEST-HANDOFF-004,TEST-HANDOFF-005,TEST-HANDOFF-006,TEST-HANDOFF-007|17|Both sibling harnesses use separate connections to owned SQLite files with assertions intact.',
        '20|FR-MCP-MEMORY-002,TR-MCP-MEMORY-002,TEST-MCP-MEMORY-002|18|Scanner portion excluded; memory binding citations and historical record corrections hold.',
        '21|TR-MCP-USECASE-018,TR-MCP-USECASE-019|18|Hostile runs excluded; environment-case ownership/restoration is correct, but project placement needs R14-F4 resolution.',
        '22|FR-MCP-018,TR-MCP-WS-005,FR-MCP-SESSIONLOGCTX-001,TR-MCP-SESSIONLOG-006,TR-MCP-USECASE-018,TR-MCP-USECASE-019|19|Reachability and environment restoration/serialization hold; category moves retain mixed-project ownership under R14-F4.',
        '23|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018,TR-MCP-USECASE-019|20|All fallback variables are owned/restored and primary wins remains discriminating; category placement needs R14-F4 resolution.',
        '24|FR-MCP-REPL-001,TR-MCP-REPL-001,TR-MCP-USECASE-018,TR-MCP-USECASE-019|21|Bounded timeout parser/call-site contracts and native junction fix hold; native Integration placement remains in mixed projects under R14-F4.',
        '25|TR-MCP-NUKE-019,TR-MCP-NUKE-018,TR-MCP-NUKE-016,FR-MCP-REPL-007,TR-MCP-REPL-008,FR-MCP-TESTRESOURCE-001|22|Owned instances, streaming download, marker walk and approved project selection hold; mixed-project selection needs R14-F4 resolution.',
        '26|TR-MCP-USECASE-018,TR-MCP-USECASE-019,FR-MCP-018,TR-MCP-WS-005|23|Hostile-run clauses are removed under the operator directive; no current gate requires those runs.',
        '27|TR-MCP-NUKE-019,TR-MCP-NUKE-018,TR-MCP-USECASE-019,TR-MCP-QUALITY-001,TEST-MCP-PATHNORM-001|24|Removed environment-specific criteria are absent; kept behavior and historical citations follow the ruling.',
        '28|FR-MCP-018,TR-MCP-WS-005,TR-MCP-NUKE-019|25|Docs-only merge and reviewed range are correct; ClineQb code is absent and current merge is clean.',
        '29|FR-MCP-047,FR-MCP-TESTRESOURCE-001,TR-MCP-NUKE-019,TR-MCP-USECASE-018,TR-MCP-USECASE-019|26|Explicit baseline, owned database and two-class lane repairs hold; overall project-selection ownership needs R14-F4 resolution.',
        '30|FR-MCP-QBOPENAI-001,FR-MCP-FED-PATH-001,TR-MCP-FED-PATH-001,TR-MCP-NUKE-019,FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018,TR-MCP-USECASE-019,FR-MCP-047,FR-MCP-REPL-001,FR-MCP-REPL-007,FR-MCP-REPL-008,FR-MCP-018,FR-MCP-SESSIONLOGCTX-001,TR-MCP-WS-005,TR-MCP-SESSIONLOG-006|27.1-27.18|I-O implementation and placement amendments hold; category/project ownership remains unresolved under R14-F4.',
        '31|FR-MCP-BUILD-001,TR-MCP-NUKE-020|27.20|Named group selector, result-class joins and reject conditions hold with independent contracts and current Build.Tests evidence.',
        '32|FR-MCP-TESTRESOURCE-001,TR-MCP-FED-PATH-001|27.20|Provider-only keys and retained GetFullPath allowlist match; r-1 failure is disclosed and r-2 is cited.',
        '33|FR-MCP-040|27.20|Stored timestamps are implemented through server/client/REPL contracts. This task does not include post-merge deployment; criterion stays unset pending that separate live read.',
        '34|FR-MCP-074,TR-MCP-CI-001,FR-MCP-PLUGINCORE-002,TR-MCP-PLUGINCORE-002,TEST-MCP-PLUGINCORE-002,FR-MCP-SERVICEUPDATE-001,TR-MCP-SERVICEUPDATE-001,TEST-MCP-SERVICEUPDATE-001,FR-MCP-USECASE-018,TR-MCP-USECASE-020,TEST-MCP-USECASE-021,FR-TIMER-001,TR-TIMER-LIFECYCLE-001,TEST-TIMER-001,FR-MCP-USECASE-016,FR-MCP-USECASE-017,FR-MCP-FED-BACKFILL-001,TR-MCP-FED-BACKFILL-001,FR-MCP-FED-ROUTE-001,TR-MCP-FED-ROUTE-001,FR-MCP-FED-WSSYNC-001,TR-MCP-FED-WSSYNC-001,FR-WF-005|27.19-27.21|Reconciliation edges/deletions have retained getter records; replacement links, projections and current traceability evidence hold.',
        '35||operational child branch|No product requirement in flux is mapped to the branch/SSH routing operation. It remains open without verdict or scores.',
        '36|TR-MCP-NUKE-020,FR-MCP-TESTRESOURCE-001,FR-MCP-040,TR-MCP-NUKE-016|27.22|Six strengthened criteria, five functional repairs, XML retrofit and original record fixes hold at the final head.',
        '37|FR-MCP-TESTRESOURCE-001,FR-MCP-040,FR-MCP-QBOPENAI-001,TR-MCP-USECASE-018,TR-MCP-USECASE-019|27.20-27.21|Artifact ownership, timestamp UTC, ac-5/ac-6 port, retired-reference remedy and retrospective evidence are supported.',
        '38|FR-MCP-040,FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018|27.23 U|Exact complete dates, path-root identity and scripted recorded clocks discriminate the three original failures.',
        '39|TR-MCP-USECASE-018,FR-MCP-079,FR-MCP-MEMORY-001,FR-MCP-MEMORY-002|27.23 V|Service stamps use supplied clocks and exact assertions; documentation and sibling fixes hold.',
        '40|TR-MCP-USECASE-018|27.23 W|Scripted lease renewal holds; heartbeat/watcher category moves retain mixed-project ownership under R14-F4.',
        '41|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018|27.24|Device-path refusal, explicit callback cancellation and historical citation correction hold.',
        '42|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018|27.25|UNC root-alias refusal and stated margin amendments hold with later corrected sum/conditional-delay measure.',
        '43|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018|27.26|Six guard changes and shared separators are preserved; later notices correctly supersede the original margin interpretation.',
        '44|TR-MCP-USECASE-018|27.27|Summed-delay bounds and recorded overcounts/cleanup-only bounds are accurately delimited.',
        '45|FR-MCP-SERVICEUPDATE-001,TR-MCP-SERVICEUPDATE-001,TEST-MCP-SERVICEUPDATE-001,TR-MCP-USECASE-018|27.28|Repeated-property reader and conditional waits hold. Startup health remains as built under the operator exception and outside D-B.',
        '46|FR-MCP-SERVICEUPDATE-001,TR-MCP-SERVICEUPDATE-001,TEST-MCP-SERVICEUPDATE-001|27.29|Six-row failed-update recovery binds production; accepted broader replacement/start-limit limits are not concealed.',
        '47|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018,TR-MCP-SERVICEUPDATE-001|27.30|Key identity and scripted age decisions hold with Z6/Z12-Z16 supplements; R12-F3 receipt chain is correct.',
        '48|TR-MCP-USECASE-018|27.30 Z4 part 2|Absent MEMORY-JOBS criterion citations are explicitly historical; documentation-only change is compiled in current gates.',
        '49|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018|27.31 Z4 parts 3/4|Ordinal unrelated names, owned-key replacement, REPL read-only view and member documentation hold.',
        '50|TEST-MCP-TRIAGESTORE-007,TEST-MCP-TRIAGE-003,TEST-MCP-TRIAGE-005|27.31 Z5|Four lane assertions identify the exact budget while preserving persistence checks; promptness relies on the separately supplied budget cases as disclosed.',
        '51|FR-MCP-TESTRESOURCE-001|27.32|Owned aliases across host prefixes are replaced without collapsing unrelated names; controls and bindings hold.',
        '52|FR-MCP-TESTRESOURCE-001,TR-MCP-USECASE-018,FR-MCP-040|27.33|34-member documentation supplement and 18 compile pins are verified; post-run pin limitation is preserved.',
        '53|TR-MCP-USECASE-018,FR-MCP-040|27.34|Proxy IDs preserve relations; nineteen test members and two mapping methods have dated entries, with later corrections retained.',
        '54|FR-MCP-MEMORY-008,TR-MCP-FED-MEMORY-001,TEST-MCP-MEMORY-FED-001,FR-MCP-040|27.35|Four federation files use corresponding generic IDs; literal assertion relation is unchanged and mapping history is corrected.',
        '55|TR-MCP-HOSTILEREVIEW-003,FR-MCP-040|27.36|Account-name summary is generic; documentation corrections and wider citation/lab-name checks hold.',
        '56|FR-MCP-040,TR-MCP-HOSTILEREVIEW-003,TR-MCP-NUKE-016,TEST-MCP-QBOLLAMA-001|27.37|Original dated entries plus later corrections are preserved; generic store text keeps outcomes/flags and disclosed exceptions.',
        '57|TR-MCP-USECASE-018|27.38|Optional clock, triggered timers, D8/D9 and lifecycle binding repair R13-F1; R13-F2 pins and notices are corrected.',
        '58|TR-MCP-USECASE-018|27.39|Deferred-pass control and sentinel trace are retained; later direct completion supersedes the original inference.',
        '59|TR-MCP-USECASE-018|27.40|Documentation-only scope is accurate; subsequent notices and Z15/Z16 correct the constructor entry and no-await proof.',
        '60|TR-MCP-USECASE-018|27.41|Counter, controls and constructor correction work, but R14-F1/F2/F3 leave its chronology/criterion/evidence record inaccurate.',
        '61|TR-MCP-USECASE-018|27.42|Direct completion after every trigger precedes decisions; unreported-await controls reject an unfinished pass. Red/green mock pins, production binding and final 4866-unit gates support the complete slice.'
    )
    ChangeGroups = @(
        @{change='Initial A-F: cbc74e82,28bf0b22,de581353,b2319488 and requirement writes';tasks=@(1,2,3,4,5,6);source='Plan 7-10; commit diffs and original receipts'},
        @{change='Takeover and G: 11681680,8c2b83ab';tasks=@(7,8);source='Plan 8-10; 16-file blob comparison and filter contracts'},
        @{change='Classification and rounds 1-2: b2bd50b3,49cfec92,b4ad42aa,27c43bcc; scanner parts overridden';tasks=@(9,10,14);source='Plan 9,14-15; changed tests and retirement records'},
        @{change='Round 3 scans/version and lane repairs: 538d0ccf,6042dbb3,0210244d,68fa7b39,6ea425ab,e0b078c8';tasks=@(15,16,17);source='Plan 16 and associated contracts/receipts'},
        @{change='Round 4 source seam and concurrency harnesses: 801e6862,26fc8348,97b78114';tasks=@(18,19);source='Plan 17; source seams and database harnesses'},
        @{change='Round 5 citations/classification: 8c254695,810ee8aa; scanner documentation overridden';tasks=@(20,21);source='Plan 18; retained original corrections'},
        @{change='Round 6 reachability/environment ownership: 9933796c,c8418301';tasks=@(22);source='Plan 19 and amended requirement records'},
        @{change='Round 7 CLI fallback ownership: 6a87791d,04841148';tasks=@(23);source='Plan 20; CliTempVariables and callers'},
        @{change='Round 8 bounded timeout and junction: f5cda3c1,7ab3c1c4,845f323b,ccd9e91f';tasks=@(24);source='Plan 21; timeout/junction contracts'},
        @{change='Round 8b owned instances/lane/provisioning/marker: 8c651f13,d5a23364,6667c405,2f679213,4fab4424,3e487b6b';tasks=@(25);source='Plan 22 and retained mock/binding receipts'},
        @{change='Scope/criterion narrowing and citation labels: 433ce191,a78640ba';tasks=@(26,27,28);source='Plan 23-25; explicit operator rulings'},
        @{change='Develop docs-only merge d43c1d12';tasks=@();source='Plan 25; 99 docs-only paths; no imported develop requirement is inferred as implementation flux'},
        @{change='Slices D-G at 765490a9 with pre-commit evidence';tasks=@(29);source='Plan 26; explicit-baseline, owned-start and lane-class contracts'},
        @{change='Slices I-O at 765490a9 and associated criterion placement writes';tasks=@(30);source='Plan 27.1-27.18; records and bindings'},
        @{change='Slice Q at 611a5dc5';tasks=@(31);source='Plan 27.20; TestGroups contracts and negative gate'},
        @{change='Slice R at 611a5dc5';tasks=@(32);source='Plan 27.20; provider rows and foreign-path source'},
        @{change='Slice P at 611a5dc5';tasks=@(33);source='Plan 27.20; timestamp contracts and adapters'},
        @{change='Traceability reconciliation and retired-reference remedy, 611a5dc5 and store writes';tasks=@(34,9);source='Plan 27.19-27.21; traceability-reconcile-20261102T111536.json'},
        @{change='Retrofit, slice S and ac-5/ac-6 port records: 62c5882d,878c366f';tasks=@(37);source='Plan 27.20-27.21; source and retrospective receipts'},
        @{change='Slice T 6082b116';tasks=@(36);source='Plan 27.22; six criterion writes and contract/binding pins'},
        @{change='Slice U 2e121015';tasks=@(38);source='Plan 27.23 U'},
        @{change='Slice V cb174ac8';tasks=@(39);source='Plan 27.23 V'},
        @{change='Slice W 1f51d4b2';tasks=@(40);source='Plan 27.23 W'},
        @{change='Slice X 11406fdf';tasks=@(41);source='Plan 27.24'},
        @{change='Slice Y ec58cf4a';tasks=@(42);source='Plan 27.25'},
        @{change='Slice Z a4e8d45f';tasks=@(43);source='Plan 27.26'},
        @{change='Slice Z2 121992b3';tasks=@(44);source='Plan 27.27'},
        @{change='Slice Z3 b3f26862,4920a8ea';tasks=@(45);source='Plan 27.28'},
        @{change='Slice Z3 part 3 52e6d627';tasks=@(46);source='Plan 27.29'},
        @{change='Slice Z4 4398cffc';tasks=@(47);source='Plan 27.30'},
        @{change='Slice Z4 part 2 1bb0d61f';tasks=@(48);source='Plan 27.30'},
        @{change='Slice Z4 parts 3/4 b79fd51f,ff7dcf51';tasks=@(49);source='Plan 27.31'},
        @{change='Slice Z5 a3caa21d';tasks=@(50);source='Plan 27.31'},
        @{change='Slice Z6 88c5abcc';tasks=@(51);source='Plan 27.32'},
        @{change='Slice Z7 1d77a79c';tasks=@(52);source='Plan 27.33'},
        @{change='Slice Z8 b21c21ec,becb0742';tasks=@(53);source='Plan 27.34'},
        @{change='Slice Z9 94dc5b69';tasks=@(54);source='Plan 27.35'},
        @{change='Slice Z10 baff082f';tasks=@(55);source='Plan 27.36'},
        @{change='Slice Z11 53a210ca and generic store-text writes';tasks=@(56);source='Plan 27.37'},
        @{change='Slice Z12 8c944541';tasks=@(57);source='Plan 27.38'},
        @{change='Slice Z13 f9b6bde8';tasks=@(58);source='Plan 27.39'},
        @{change='Slice Z14 288b03c3';tasks=@(59);source='Plan 27.40'},
        @{change='Slice Z15 548b1a28';tasks=@(60);source='Plan 27.41; original transcript, with R14-F1/F2/F3 corrections required'},
        @{change='Slice Z16 da11faa2';tasks=@(61);source='Plan 27.42; eight-case mock contract, 108-case binding, final gates'},
        @{change='Documentation/receipts between slices, including 9a261993,aedee994,0980a52b';tasks=@(11,60,61);source='First-parent inventory; docs-only tail. These evidence existing flux, not new unrelated implementation requirements.'}
    )
    ClosureRows = @(
        'R13-F1|PASS|The real cleanup timer/clock is replaced in unit fixtures by TriggeredTimerTimeProvider; Z16 observes the actual pass completion and rejects unreported awaits. D8/D9 boundaries and all named siblings are covered.',
        'R13-F2|PASS|Independent raw-blob audit confirms 18 Z7 compile pins match 1d77a79c, written after the compile; binding pins none. Corrected current descriptions preserve that limit.',
        'R12-F1|PASS|Configuration-key aliases, exact unrelated names and host-prefix owned aliases are covered by Z4/Z6 contracts and the actual owned starts.',
        'R12-F2|PASS|Named age decisions and the previously missed voice background sibling now use scripted clocks/triggers and direct completion observation.',
        'R12-F3|PASS|The retained five/six/seven/eight-criterion service-update chain identifies the first receipt correctly.',
        'R11-F1|PASS|Device paths, cross-syntax roots and Windows segment/UNC aliases are refused by the 39-row bound output contract.',
        'R11-F2|PASS|Marker-stub cancellation follows the asserted callback instead of racing it against a 100 ms timer.',
        'R11-F3|PASS|Watcher citation is explicitly historical; current changed-line citation audit found no unclassified absent ID.',
        'R10-F1|PASS|Exact complete-date formats reject year/month-only text and incidental year digits.',
        'R10-F2|PASS|Root kind and identity are retained; drive/UNC compare case-insensitively and POSIX ordinally.',
        'R10-F3|PASS|Original recorded/stamped times and later age/background siblings use independent scripted inputs; no remaining sibling was established.',
        'R9-F1|PASS|A named class counts only through an executed result joined to its definition by test ID.',
        'R9-F2|PASS|Health check compares the entire JSON nonce field and rejects longer/other-field/malformed responses.',
        'R9-F3|PASS|Output identity follows syntax, roots and Windows alias rules, with independent rejected checks.',
        'R9-F4|PASS|Stored dates cannot acquire missing date components from the live read date.',
        'R9-F5|PASS|Provisioning and cases use the same canonical-first Ollama model rule.',
        'R9-F6|PASS|Handoff points to current D-B and task 32 cites r-2 while preserving r-1 failure. New record defects are separately R14-F1/F2/F3/F5.',
        'R9-F7|PASS|Current gate receipts disclose docs-only dirt and its post-run observation limits; TRX and committed code tail are independently checked.',
        'R9-F8|PASS|Required member documentation/dated entries are present with declared later supplements and valid or historical IDs.',
        'R8B-F1|PASS|Explicit-baseline delta and owned/restored GH_TOKEN integration case remain.',
        'R8B-F2|PASS|Exactly the two approved Support integration classes and six cases are supplied on both hosts.',
        'R8B-F3|PASS|Inherited database selection and normalized-key siblings are excluded/refused on the actual start paths.',
        'R7-F1|PASS|Bounded timeout parser and explicit call-site read seam prevent invalid cancellation delays.',
        'R6-F1|PASS|The four CLI resolver families run inside the owned/restored fallback-variable scope.',
        'R6-F2|PASS|Historical absent citations are labeled; current source citations were independently checked.',
        'R5-F1|PASS|Linked scan roots and ancestors are checked before traversal, with scripted and native binding evidence.',
        'R5-F2|PASS|Canonical/legacy environment ownership, restoration and serialization hold for the original cases; project ownership is a distinct current policy finding.',
        'R4-F1|OVERRIDDEN|Scanner alias coverage and its descriptions are excluded by the operator.',
        'R4-F2|PASS|Historical task/head notices remain, and current snapshot/live TODO agree on 61 open tasks.',
        'R4-F3|PASS|Memory binding cites mapped FR/TR/TEST-MCP-MEMORY-002.',
        'R3-F1|PASS|Plugin-version inputs are supplied through the test-owned source seam.',
        'R3-F2|OVERRIDDEN|Scanner-mediated closure is excluded.',
        'R3-F3|PASS|Both retired FRs are absent through dedicated getters and effective view; replacement links and notices hold.',
        'R3-F4|PASS|Original self-review/head correction is retained; new current record defects are separately reported.',
        'R2-F1|PASS|Original native/environment cases retain correct category placement or deterministic seams; new project-ownership rule is separately R14-F4.',
        'R2-F2|OVERRIDDEN|Scanner-mediated closure is excluded.',
        'R2-F3|PASS|Static runner, hash proof and disclosed scripting exceptions hold in current lane evidence.',
        'F1|PASS|Original failed mock gate remains failed history; current non-overridden implementation has green discriminating mock/binding evidence.',
        'F2|PASS|Original native filesystem/SQLite cases retain classifications or deterministic seams and assertions.',
        'F3|OVERRIDDEN|Scanner/skip-syntax coverage and descriptions are excluded.',
        'F4|PASS|Current Windows lane has interactive-session, pinned runner, head, selection and 718 passed-result evidence.',
        'F5|OVERRIDDEN|Scanner project-selection change-detection coverage is excluded.',
        'F6|PASS|Historical Bash exception is disclosed, not relabeled as PowerShell compliance.',
        'F7|PASS|Missing Windows evidence is supplied and unconditional satisfaction language is replaced by the current conditional D-B.'
    )
}
