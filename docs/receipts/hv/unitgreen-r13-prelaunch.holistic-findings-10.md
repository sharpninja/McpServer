# Tenth holistic read before hostile validation round 13

The read-only subagent report of session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T211300Z-holistic-r13j (parent turn req-20261104T231251Z-continue-pr87-completion-2), as the session transcript holds it. Exported 2026-11-05 by Export-SubagentReport.ps1, because the harness refused the subagent's own findings file. Report text unchanged; the harness indentation removed.

2026-11-05T15:45:51 (measured local stamp)

This is the tenth holistic read before hostile validation round 13. It found 15 problems, and four of them are material: b1, c5, g1 and a4. I changed nothing tracked.

**Counts:** (a) 5, (b) 1, (c) 6, (d) 1, (e) 1, (f) 0, (g) 1. Total 15.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T211300Z-holistic-r13j`.
- Opened with Begin-Turn.ps1, updated twice with Update-Turn.ps1, completed with Complete-Turn.ps1.
- Readback: completed, 8 actions, 4 decisions, response file `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13j/turn-response-r13j.txt`.
- The harness refused `findings-r13j.md` ("Subagents should return findings as text"), so the full findings are below.

**What checked clean**
- **Git:** HEAD and both origin refs are `1d77a79c`, with no tracked change, and `1d77a79c..HEAD` is empty.
  - `88c5abcc..1d77a79c` changes 18 files: 94 lines added, 0 removed, every added line a `///` comment or blank (75 and 19).
  - `52e6d627..1d77a79c` lists eight src files and no build file.
  - The commit time is 14:21:40. It carries the marker's signing line and no cryptographic signature.
- **Citations:** every dated entry slice Z7 added cites only ids that its constructor, method or class entry already cites.
- **HostPrefixes** (`TemporaryInstanceChecks.cs:585-592`): its summary matches `ConfigurationKeysSetBy` (`:572-583`) and `IsProductVariable` (`:529-540`).
- **Member completeness:** I ran an independent Roslyn syntax parse (the parser pwsh ships; parsing only, no build) over the 370 PR files. No undocumented member added since `679eb788` remains, so the 34 members are the whole member set.
- **Receipts:**
  - Manifest `154CA6EC`: 34 files, every head `1d77a79c`, every hash equal, 0 failed, 0 not executed.
  - Gate receipts `CE8542DC`, `E8738094` and `E396D96D` show the stated counts with no tracked dirty path.
  - The Windows unit, lane and Build.Tests results show the stated counts and times. The lane's proof head is `1d77a79c` and its task was removed.
  - Every by-name compare shows 0 names only before and 0 only after.
  - Timing sweep: 211 hits, 0 unclassified, ORDER=5. Durations: 314 hits, 255 listed. Citation sweep: 96 ids, 39 absent.
  - Store readback `713EA861` (52 tasks) and compare `AC85C705` match.
  - Binding-block compare: 38 lines, SAME. Host-prefix literal sweep: 48 matches. Ninth-read export `58885103` matches.
- **PR 87:** draft at `1d77a79c`; ready_for_review at 09:59:32 and convert_to_draft at 05:08:10 local.
- **d3 numbers against `ug-r13z6-win-qbrainai-event-sources-1.txt`:** every count matches, and all 769 events in `ug-r13z5-win-lane-host-diagnostics-1.txt` are QBrainAi. The one exception is b1.
- **Ninth-read findings:** every one is fixed where 27.33 says. e3 is fixed too, but 27.33 does not list it (c6).

**(a) Superseded statements with no forward note**
- **a1.** The timing-sweep history chains stop at slice Z6, but the current receipt is `ug-r13z7-timing-sweep.txt` (27.33).
  - Plan 27.24 (line 1734): "and since slice Z6: `ug-r13z6-timing-sweep.txt`, 27.32".
  - Plan 27.25 (line 1784): "`ug-r13z5-timing-sweep.txt`, 27.31, and `ug-r13z6-timing-sweep.txt`, 27.32 (eighth holistic read finding a3)".
  - Fix: append "and since slice Z7: `ug-r13z7-timing-sweep.txt`, 27.33" to both.
- **a2.** Plan 27.25 (line 1782): the store-readback chain ends "and slice Z6 (`ug-r13z6-store-readback-1.json`, 27.32)."
  - Fix: add "and slice Z7 (`ug-r13z7-store-readback-1.json`, 27.33)".
- **a3.** Two pointers still name a superseded section as current.
  - Plan 27.31 (line 2101): "the current line is 27.32's "Docs only after `88c5abcc`"". 27.32's own line (2145) says it is superseded.
  - Plan 27.30 (line 2054): "History since 27.31, which names the TRX set of record". 27.31's set has been history since 27.32.
  - Line 2056 is the same chain.
  - Fix: point each to 27.33.
- **a4 (material).** Several statements date full MEMORY-FACT-008 coverage before slice Z7, with no forward note.
  - Slice Z7 documented fields from earlier slices, which the v1 finder could not see because it matches no field declaration:
    - Slice P: `RequirementTimestampJsonBindingTests.s_clientOptions`.
    - Slice R: `OwnedInstanceProviderSettingContractTests.Root` and `Cases`.
    - Slices U and V: the clocks of FakeSessionLogState, StubAuthRotationHandler, ScriptedClock, ScriptedTimeProvider, GraphRagService and MemoryService.
    - Slice Z4: four src clocks, twelve contract constants, the FederationEnvelopeSignerTests helpers, `SettableTimeProvider._utcNow` and `SettableTimeProviderTests.Early`.
  - The quotes:
    - 27.20 line 1457: "Retrofitted in `62c5882d` to every member and test class slices Q, R and P changed".
    - 27.22 line 1537: "R9-F8's 38 members ... all now documented (the sweep reads 0)".
    - 27.31 line 2083: "which was false until slice Z4 part 4".
    - Brief UG47: "every changed member and test class carries the dated change log MEMORY-FACT-008 requires since slice Z4 part 4".
    - Self-review line 64: "eight did not until slice Z4 part 4".
  - Fix: add a forward note to each (fields excepted until slice Z7, 27.33), and say "since slice Z7" in UG47.
- **a5.** The PR body labels history TRX sets "HV evidence:" under its history headings. Example, line 192: "HV evidence: `docs/receipts/hv/unitgreen-r13-trx-1bb0d61f/manifest.json`". The same label is on lines 206, 216, 226, 236, 248 and 258.
  - The `88c5abcc` and `a3caa21d` blocks already say "Their TRX set: ... (history)".
  - Fix: use that wording on each.

**(b) Contradictions**
- **b1 (material).** Plan 27.32 (line 2131) and projected problems item 139 contradict the receipt they cite.
  - The plan says: "The window before the rerun is held only from 10:02:01, so its 1,333 service events span about 27 minutes, about 49 a minute", and "the service's own rate varies from about 4 to about 49 a minute".
  - Item 139 repeats "about 49 in the 27 minutes".
  - Both cite `ug-r13z7-win-qbrainai-event-windows-1.txt`, whose line 7 says: "WINDOW gapA minutes=83 events=1333 ... servicePerMinute=16.1". That is the understated basis the ninth read's c2 found.
  - No receipt holds 27 minutes or 49 a minute. They are hand arithmetic from the z6 receipt's oldest event (10:02:01) to the end of gapA (10:29:00).
  - The operator's "Declare, out of scope" requires these numbers to be accurate against this receipt.
  - Fix: rerun Measure-EventSourceWindows with gapA starting at the oldest event, or say where 27 and 49 come from and that the z7 receipt's gapA uses the full 83 minutes.

**(c) Statements a reviewer can show false**
- **c1.** Plan 27.33 (line 2159) and self-review line 16 both say "the eleven constants and fields of OwnedInstanceKeyIdentityContractTests". Slice Z7 documents twelve there: six constants and six fields (sweep-2 lines 15 to 26). Fix: "twelve".
- **c2.** "2 local functions" undercounts.
  - It appears in plan 2158, the `1d77a79c` commit message, PR body 147, projected problems item 143, brief UG50 (which also says "is complete for its class") and self-review line 8.
  - The v2 finder sees only local functions that carry a modifier. The syntax parse finds six added since the rule. Besides the two listed:
    - `ServiceStartupHealthContractTests.cs:98` State and `:99` Probe.
    - `ServiceUpdateTests.cs:681` Local.
    - `TemporaryInstanceChecksTests.cs:363` Inside.
  - The documentation outcome still holds.
  - Fix: say 6 local functions (40 items), or narrow the claim to members.
- **c3.** The sweep counts are misdescribed. The quotes are "511 undocumented private members and fields in 370 files" and "36 in 94 files" (plan 2158), "the 475 written before MEMORY-FACT-008" (PR body 302) and projected problems item 143.
  - 370 and 94 are the files scanned. The 36 items lie in 20 files.
  - The hits are not all private: sweep-1 line 58 `public ... Provision`, line 61 `public const string Postgres`, line 195 `internal static readonly ImportScope Empty`.
  - About a hundred hits are text inside C# source string literals of the scanner fixture tests, not members (sweep-1 lines 196 to 301).
  - A syntax parse finds 366 undocumented member declarations written before the rule, not 475.
  - One of them, `TemporaryInstanceChecksTests.WrongStartCheck` (`:304`), was changed after the rule (`:327-330`).
  - Fix: call them heuristic regex hits and give the file counts as files scanned. Then state the parse count or declare the fixture-text hits.
- **c4.** Plan 2158: "Local functions take no XML documentation, so they are covered by the entries of the methods that hold them, as declared since the seventh read's c5."
  - No earlier record declares anything about local functions. The c5 declaration (27.31, line 2084) is about test files logging changed methods at class level.
  - Fix: drop the clause, or make the declaration now.
- **c5 (material).** Brief UG30 claims "the slice N scrub and citation pass leave every lab identifier on a changed line either replaced or one of the four flagged lab-bound exceptions".
  - Slice Z4 (`4398cffc`) added five lines to `tests/QBrainAi.Support.Mcp.Tests/Services/FederationEnvelopeSignerTests.cs` that name lab machines:
    - `:154` and `:176`: `sut.Sign(Operation(), "PAYTON-LEGION2", "PAYTON-DESKTOP")`.
    - `:157` and `:179`: `sut.Verify(envelope, "PAYTON-DESKTOP")`.
    - `:191`: `ProxyId = "PAYTON-LEGION2"`.
  - A search of every line `9b099716..1d77a79c` adds under src, tests and build finds exactly these five.
  - None is one of the four exceptions, and no scrub after slice Z3 covers them. This breaks "Scrub everything in PR 87" (27.7) and the standing rule that no test names the lab.
  - Fix: generic ids in the two D4 bindings and `Operation()`. That is test-only, but it moves the code head, so the gates rerun. Or put the five lines to the operator as exceptions. Either way, correct UG30.
- **c6.** Self-review line 8 says "the other 27 fixed in the records (plan 27.33 lists where)". The 27.33 record list (lines 2163 to 2167) omits e3.
  - e3 itself is fixed: brief line 50 now says "27.32 holds them since the ninth holistic read, finding d1".
  - Fix: list e3 in 27.33.

**(d) Missing disclosures**
- **d1.** Nineteen test members with no XML documentation had their bodies changed after the rule, and the records do not say so.
  - One is from this PR: `TemporaryInstanceChecksTests.WrongStartCheck`.
  - Eighteen predate the PR:
    - FakeSessionLogState.OpenSession, BeginTurn, UpdateTurn, CompleteTurn and FailTurn (`SessionLogTestHelpers.cs:59-123`).
    - StubAuthRotationHandler's four methods and `StubAuthRotationHandlerTests.CreateMarkerData`.
    - CreateMockSelectionState, in RequirementsWorkflowTests and in TodoWorkflowTests.
    - `TransactionSecurityControllerTests.CreateServices` and `MemoryS1Harness.CreateProductionDispatcher`.
    - `GraphRagServiceTests.CreateSut` and `MemoryFederationStateAdapterTests.CreateProvider`.
    - CreateService, in HandoffAuditDefectTests and in WorkspaceHygieneG5OverlayTests.
  - Their classes carry dated logs under the declared class-level test convention.
  - But slice Z7 gave fields of the same classes their own summaries, and 27.33 grounds the 475 declaration on "the rule applies to changes after it".
  - Fix: declare this class beside the 475, or document these members.

**(e) Brief claims the evidence cannot support**
- **e1.** Brief UG50 says compile `ug-r13z7-compile-1` and binding `ug-r13z7-binding-1` (160/0/0) "ran on the final tree".
  - The binding receipt records `head=88c5abcc` with the 18 slice Z7 files modified and no source hashes. The compile receipt records `runHead=88c5abcc`.
  - Nothing receipted ties that tree to the `1d77a79c` blobs, and 27.33 does not declare it (27.28 did declare the same shape for slice Z2, fourth read finding d3).
  - The outcome holds, because the gates at `1d77a79c` compiled the committed tree.
  - Fix: declare it and hash the 18 files against `1d77a79c`, or reword the claim.

**(f) Ninth-read findings that 27.32 or 27.33 says are fixed but are not**
- None. e3 is fixed but unlisted (c6).

**(g) Slice Z7 code or comments against the stored criteria or MEMORY-FACT-008**
- **g1 (material).** The new summary of `FederationEnvelopeSignerTests.Operation()` (`:185-188`) is false: "FR-MCP-103: the fixed operation request every case signs."
  - Only the two D4 bindings call it (`:154`, `:176`). The other five signing cases build their own request (`:28`, `:49`, `:69`, `:89`, `:107`).
  - Its returns line, "A new request with fixed ids and an empty body.", is also wrong. BodyBase64 is the base64 of "{}" (`:193`).
  - This breaks UG50's "each description says no more than its member does" and the self-review's what-if. The usage check 27.33 describes covered only the OwnedInstanceKeyIdentityContractTests members.
  - Fix: "the fixed operation request the two D4 age-boundary bindings sign" and "a JSON body of {}". It is comment text, but D-A makes it a new code head, so the gates rerun. Or declare it with the reason.

**Scripts and outputs.** All scripts are read only and were analyzer clean before they ran. Everything is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13j/`:
- **Copies of the r13i scripts with the output folder changed:** Get-GitFactsJ.ps1, Test-ManifestJ.ps1 and Show-ReceiptSummaryJ.ps1.
- **New scripts:** Find-RoslynAssemblies.ps1, Find-UndocumentedMembersRoslyn.ps1 and Find-AddedLinesMatching.ps1.
- **Outputs:** git-facts.txt, diff-z7.txt, roslyn-undocumented.txt, manifest-check.txt and receipts-summary.txt.
- **Pattern files:** pattern-lab-names.txt, pattern-stale.txt, pattern-line-cites.txt and four smaller ones.
- **Turn files:** turn-update-1.psd1, turn-update-2.psd1 and turn-response-r13j.txt.

Bash only launched `pwsh -NoProfile -NonInteractive -File` with literal arguments. One launch failed in the shell on an unbalanced quote before pwsh started; nothing ran, and I reran the search through a pattern file. I made no contact with PAYTON-LEGION2 and printed no secret.
