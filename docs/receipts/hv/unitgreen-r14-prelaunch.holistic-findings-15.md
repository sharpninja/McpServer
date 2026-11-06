Fifteenth holistic read before HV round 14 (read-only subagent, session ClaudeCode-20261102T182906Z-plugin-session, turn req-20261106T105300Z-holistic-r14a, parent turn req-20261104T231251Z-continue-pr87-completion-2). Hand-back report exported from the session transcript because the harness refused the subagent's findings file.

2026-11-06T05:26:39

The fifteenth holistic read (the first before round 14) found 30 problems: (a) 5, (b) 6, (c) 8, (d) 8, (e) 1, (f) 0, (g) 2. No round 13 finding or FAIL claim is still open. Slice Z12 meets TR-MCP-USECASE-018 AC-006 as appended and the BDPv4 order; its two (g) findings are low-weight documentation gaps.

Session ClaudeCode-20261102T182906Z-plugin-session, request req-20261106T105300Z-holistic-r14a: opened, 4 actions and 3 decisions, completed at 05:28:31 with response file holistic-r14a/response-r14a.txt.

How the read was done: I read the plan master end to end (lines 1 to 2341). That used most of my context, so four read-only helpers did the other end-to-end reads, under the same standing rules and a shared context file I wrote (holistic-r14a/subagent-common.md). Each logged its own child turn, all completed: -brief (the brief), -pp (projected problems, PR description and PR body summary), -hs (handoff, self-review and the round 13 verdict), -code (the 53a210ca..8c944541 diff and commit message, AC-006, the ug-r14z12 receipts, the background sweep and the TRX manifest). Below are their findings, merged and deduplicated; the two (g) items rest on one helper's source read. The harness refused findings-r14a.md in holistic-r14a ("Subagents should return findings as text"), and refused each helper's findings.md the same way, so this hand-back is the only full copy.

Findings that matter most:
- d1: neither the plan nor the handoff says when PR 87 was marked ready for round 13 (23:41:26).
- c1: the Linux unit receipt already lists the background sweep, so the dirty-path sentence is wrong in plan line 2329 and four other records.
- d7: the bindings rely on the fire-and-forget cleanup finishing inside Fire().
- b5: the device lookup is called "enforced", but the sweep lists it as not reached.
- e1 and d4: the sweep's 90% completeness note is missing from the brief, the plan and the handoff.

(a) Superseded statements
- a1. Brief line 254 (UG1) still names "ug-r13z11-doc-claim-checks-1.txt for the gate claims at 53a210ca". Fix: cite ug-r14z12-doc-claim-checks-1.txt once written, and mark the r13z11 file as history since slice Z12.
- a2. Low weight: brief line 24 says "slice Z11 (commit 53a210ca, the last code head: ..." with no "until slice Z12" (line 25 does say "until slice Z3"). Fix: add "until slice Z12".
- a3. Projected problems item 122 (line 29): "A reviewer may find a live-clock decision the sweep did not reach." It has no forward note to R13-F1, although the file header promises such notes. Fix: point it to items 154 to 157.
- a4. PR body summary line 27 cites "the v3 sweep `ug-r13z11-citation-sweep-all-2.txt` reads all 370 changed C# files" with no 8c944541 note. Fix: cite ug-r14z12-citation-sweep-all-1.txt (374 files, 101 ids, 39 absent).
- a5. Handoff line 20 says "under TR-MCP-USECASE-018 AC-006 as appended through 2026-11-04 15:29:24". AC-006 was appended on 2026-11-05 and at 2026-11-06 02:13:52, and D-B (plan line 2337) sets it as appended through slice Z12. Fix: say which appends the hang-guard classes apply and which sweeps cover the later ones.

(b) Contradictions
- b1. Brief line 69 says "(declared; plan 27.38 names the plan edit only)", and projected problems item 164 says "which names the plan edit only". Plan line 2329 now names more than that, and line 2341 lists this wording as a draft slip. Fix: cite 27.38's actual sentence and its reason.
- b2. Projected problems item 158 (line 13) says "plan 27.38 records the return without calling the delay a slip". Plan line 2328 says "(a slip, below)" and line 2341 says "the round 12 slip of 27.30 repeated". Fix: correct item 158.
- b3. PR body summary line 7 says the PR returned to draft "under my HV order (plan 27.26)". Plan lines 2328 and 2341 say the return came late, against 27.26. Fix: "later than plan 27.26 requires (a slip, 27.38)".
- b4. Self-review lines 64 and 70 say the late-draft slip is one "which this record adds" and "27.38's slip list does not name it". Plan line 2341 does name it. Fix: correct both lines.
- b5. Brief line 266 (UG13) says "the device-lookup half ... are enforced", line 300 (UG47) relies on the same, and plan line 2320 says "enforced". The sweep lists FindSessionByDevice as "INERT or not reached" ("no unit caller", report line 118), not among its 15 enforced exclusions. Fix: "reached by no unit case at 4e992d30; bound on the scripted clock by D9 since slice Z12".
- b6. Plan line 2203 (slice Z8) says both receipts pin the hashes, "so the tested tree is tied to the committed blobs". Both receipts were written after their runs (compile 16:05:03, written 16:06:21; binding 16:07:56, written 16:08:41). Lines 2212 and 2326 now say such pins "alone do not prove the bytes the compile read". Fix: give line 2203 the same limit, and sweep the brief and PR description for other "tested tree" claims.

(c) Statements a reviewer could show false
- c1. Plan line 2329, brief line 69, projected problems item 164, PR description line 193 and PR body line 31 say only the lane, Build.Tests and traceability receipts "also" list the round 14 prelaunch records ("the background sweep and the PR description"). In fact ug-r14z12-final-linux-unit-1.json dirtyPaths line 9 already lists unitgreen-r14-prelaunch.background-sweep.md, written at 02:46:41 during that run. Fix: the unit receipt lists the plan edit, the background sweep and new receipts; the later three add the handoff edit and the PR description.
- c2. Brief lines 59, 69 and 254 and handoff line 20 say the Windows results "list no dirty paths" or "list none". The four Windows result files have no dirty-path field at all; they check only the origin ref and HEAD. Fix: use 27.38's wording, "carry no dirty-path list (declared)".
- c3. Brief line 17 ("reading every time from it"), brief line 256 (UG3), handoff line 9, task 57's stored text ("for every time it reads", ug-r14z12-store-readback-1.json:456) and the commit message all say the service reads every time from the clock. VoiceConversationService.cs:251 and :630 still use a system Stopwatch for LatencyMs (read at :307, :663 and :724). No unit assertion reads LatencyMs, so no test depends on it. Fix: "every time it records or compares", and declare the latency. Do not move it to the clock's GetTimestamp, which SettableTimeProvider refuses.
- c4. Plan line 2323 says idle times are "written in nine places". The real count is twelve: nine direct writes (:229, :280, :336, :365, :391, :417, :608, :686, :1324) plus :133 and :187/:2016 (both from the read at :111) and :523 (from :510). The list also omits the create-and-reuse read at :111, which is the read R13-F1 depends on. That makes 17 reads in all, matching the 17 removed UtcNow lines. Fix: restate the count.
- c5. PR description line 172 says "The sibling sweep `ug-r13z11-dated-entry-sweep-1.txt` ... finds no other entry rewritten in place". That receipt ends "REWRITTEN=20"; the result belongs to the hand classification (-classification-1.md). Plan line 2285 and item 151 were corrected for this; the description was not. Fix: credit the hand classification.
- c6. Projected problems line 1 says "last written 2026-11-06T03:49:20". The file was last written at 04:50:51 and holds later text (item 162's receipt is from 04:46:10). Fix: restamp it.
- c7. Low weight: PR description line 405 and plan line 2335 attach "104 and 104" to the v3 sweep. The v3 receipt says 101 ids and 39 absent over 374 files; 104/104 is the compare's count. Fix: state both.
- c8. Low weight: self-review line 11 says the sweep has "two" inference statements. The report labels three (lines 35, 62 and 93). Fix: say three.

(d) Missing disclosures
- d1. The plan has 0 hits for "23:41". Handoff line 14 gives no time for the round 13 ready mark. Brief lines 17 and 65 cite the plan for the ready history. GitHub's ready_for_review event is 2026-11-05 23:41:26: after the records commit 11326d42 (23:40:34) and before the launch (23:45:07). Fix: record the event in 27.38's PR-state bullet (line 2328) and on handoff line 14.
- d2. Brief lines 59 and 69 say no target reads the plan or handoff, so the tested tree is the code head. They do not mention that the untracked docs/receipts files in the dirty lists are not read either. Fix: add 27.38's clause "the docs/receipts readers name other files".
- d3. Brief line 20 and line 256 (UG3) summarize the AC-006 append without its clause "the same scripted trace gives the same result regardless of real elapsed time or wall-clock adjustment". Fix: add it.
- d4. Plan line 2320, handoff line 20 and task 57 state the R13-F1 sweep's coverage without its own note ("Completeness 90%", background-sweep.md:146). That note says the SqliteBoundedOpenFourteenthTests margins at :43 and :269, WorkspaceIdentityPath.cs:446 and MemoryConsolidateTests were not read individually. The helpers checked all three, and none is a sibling: the margins are 113 times; :446 only disposes and decrements a counter; LockTtl applies only with forceOverlap (MemoryConsolidateOperations.Locks.cs:74). Fix: record the note and this check.
- d5. Projected problems item 166 and PR description line 405 omit two slips that plan line 2341 lists: the late return to draft, and the first drafts that said 02:47:09 and "only the plan edit" was dirty. The self-review omits the second slip. Fix: add both slips.
- d6. The R13-F2 limit on after-the-run pins is not applied to slice Z12's own receipts. Item 157, PR description lines 184 and 186 and plan line 2325 say "both pin the seven files" without it. Each Z12 receipt was written 39 to 66 s after its run. The tests-first order still holds on other evidence: the binding pins equal the 8c944541 blobs, the mock gate pins the production file at its 53a210ca blob D6A5B5FA, and the production file was written at 02:27:01, after the mock gate. Fix: add the one-clause limit in those places.
- d7. The idle-cleanup tick is `_ = CleanupIdleSessionsAsync();` (VoiceConversationService.cs:1907). The bindings assert right after Fire() (VoiceConversationServiceTests.cs:274-275; the pool at :435 as IdleSessionLifecycleContractTests.cs:75-76 and :87-88 use it). That works only because DeleteSessionAsync (:456-499) never yields for these sessions. If it ever did, the at-threshold "kept" rows would also pass an inclusive comparison. Fire's doc at TriggeredTimerTimeProvider.cs:110 covers only the callback, not the task it starts. Fix, the recommended option: append to AC-006 that a test sees a triggered callback's work complete before asserting, then have the binding await it. Otherwise, declare it.
- d8. Self-review line 50 says "17 scripts written since the round 13 launch". It omits the six generated runs-r9 wrappers (mockgate-1, compile-1, binding-1, final-linux-unit-1, final-linux-lane-1, final-linux-traceability-1), and line 54 omits records-r14/Get-FileSha256.ps1. Fix: name them and say whether they were analyzed.

(e) Brief claims the evidence cannot support
- e1. Brief line 308 (UG55), which UG3 (line 256) and UG13 (line 266) rely on, says the sweep "covers every src timer, periodic timer, hosted loop, deadline loop and expiring cache and then every unit caller ... and gives the reason for each of the rest". The report rates itself 90% complete and names three items it did not read (see d4). Fix: add the note and the helpers' check, citing item 156.

(f) Closed findings that are not closed
- None. R13-F1:
  - The only unit construction of the service (VoiceConversationServiceTests.cs:379) defaults to TriggeredTimerTimeProvider.
  - All 17 former UtcNow reads go through `_time`, and the timer is armed through `_time.CreateTimer` with the 60 s schedule.
  - The four named tests exist.
  - AC-006 has 7 criteria and 7669 characters (6745 plus the append), not set.
  - The TODO has 57 tasks, none checked.
- R13-F2: 18 of 18 Z7 compile pins match 1d77a79c, and the binding pins none. Its one missed sibling sentence is b6.

(g) Slice Z12 against AC-006, MEMORY-FACT-008 and BDPv4
- What holds:
  - Order: mock gate 02:21:34 to 02:21:54, 71/0/0; production file written at 02:27:01; compile and binding pin 7 of 7, equal to 8c944541.
  - Negative controls: each fails the pool or clock it names.
  - Earlier assertions are unchanged.
  - DI: src registers only TryAddSingleton(TimeProvider.System), and no unit container resolves the real service.
  - No sibling was missed: SessionLogFileWatcher and TranscriptMcpStdioHostTests are Integration; the AgentCliInteractiveSession constructor work completes synchronously; G1's 1 s guard precedes its 2 s cancellation; CqrsRelayCommand has no unit caller; DesktopProcessLauncher's constructor is inert.
  - Every new dated entry cites only TR-MCP-USECASE-018 AC-006, as its class entry does.
- g1. Low weight: the XML at VoiceConversationService.cs:24 ("every time the service reads (...) and the idle-cleanup timer now come from one clock"), :49 and :69 is false because of the Stopwatch (c3). AC-006 is still met. Fix: "every UTC instant the service records or compares"; declare it now and fix the XML at the next code change.
- g2. Low weight: only S1 gained XML and a dated entry (VoiceConversationServiceTests.cs:35-37). S2 (:128), S3 (:155) and S4 (:177) have none, although the commit message counts them among the four fixed siblings and CLAUDE.md requires test-method docs. If their bodies are unchanged, this is a declaration item, not a breach. The rewritten constructor docs (:57-70) also have no exception tags (ArgumentNullException at :83, :84, :86, :90-94; InvalidOperationException through GetRequiredService at :85, :87-89; MEMORY-FACT-008 says "exceptions as applicable"). Fix: add at the next code change, or declare in 27.38.

Checked and consistent (not findings)
- Hashes: all 13 cited hashes match (sweep E2526C0D, manifest C611201A, Z7 pins 6D0013FA and 941737FA, store readback 051B8BBE, compare 8888CE71, and the rest).
- TRX manifest: 34 files, all at head 8c944541.
- Gates: counts and times match. Unit 4861 on both hosts, with the 23 new cases split 4 bindings, 10 age-boundary rows, 5 lifecycle and 4 timer clock. Lanes 741 and 718; Build.Tests 563 and 467.
- Sweeps: timing 211; durations 314 to 330, 255 to 265 and 59 to 65; lab names 385 paths, 163 hits, 0 lab names; NU190x 0.
- GitHub times: 23:41:26, 02:47:11, 02:08:03 and 02:43:41. No "02:47:09" and no current "56 tasks" remain.
- The plan line 2031 forward note is present, and the operator quotes are verbatim.
- PR body summary: 11781 characters, 55 lines, under 65536. Your action 239's 11622 is stale.
- Your two open plan checks: "the five test files" in 27.38 is correct. VoiceConversationServiceTests.cs, which holds the bindings, was unmodified at the mock gate and is first pinned by the compile. The missing 23:41:26 ready mark is confirmed as d1.

Disclosed:
- The hs helper passed a regex alternation bar inside quoted -Pattern arguments in some Bash calls. They were read only, and it switched to pattern files.
- No tracked file was edited.
- Everything I and the helpers wrote is under /home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r14a/: the turn data files and response file, the shared context file, and each helper's scripts and response file in brief/, pp/, hs/ and code/.
