# Ninth holistic read before hostile validation round 13

The read-only subagent report of session ClaudeCode-20261102T182906Z-plugin-session, request req-20261105T192530Z-holistic-r13i (parent turn req-20261104T231251Z-continue-pr87-completion-2), as the session transcript holds it. Exported 2026-11-05 by Export-SubagentReport.ps1, because the harness refused the subagent's own findings file. Report text unchanged; the harness indentation removed.

2026-11-05T13:56:00 (measured local stamp)

Ninth holistic read before hostile validation round 13. I found 28 problems. Four are material: b1, c1, c2 and c3. I changed nothing tracked.

**Counts:** (a) 12, (b) 2, (c) 6, (d) 3, (e) 4, (f) 1, (g) 0. Total 28.

**Session log:** session `ClaudeCode-20261102T182906Z-plugin-session`, request `req-20261105T192530Z-holistic-r13i`. I opened the turn with Begin-Turn.ps1, updated it twice with Update-Turn.ps1 and completed it with Complete-Turn.ps1. The readback shows it completed, with 8 actions, 3 decisions and the response file `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13i/turn-response-r13i.txt`. The harness refused `findings-r13i.md` ("Subagents should return findings as text"), so the full findings are below.

**What checked clean**
- **Git:**
  - HEAD and both origin refs (the PR branch and the Windows child branch) are `88c5abcc`, with no tracked change.
  - `a3caa21d..88c5abcc` changes exactly the 6 test files plan 27.32 names.
  - `52e6d627..88c5abcc -- src build .nuke` lists the 7 src files and no build file. The commit time is 12:08:03 and the commit message matches 27.32.
- **Contract first:**
  - The mock gate receipt (E98DC314, 35/0/0, head a3caa21d, only the contract file dirty) pins `TemporaryInstanceChecks.cs` 2C3837D1 and `OwnedReplStart.cs` 1061A534. Both equal the a3caa21d blobs.
  - The binding (CDE25F27, 128/0/0, 98 plus 30) and the compile (A4807869) pin hashes equal to the 88c5abcc blobs.
  - 35 is the 32 cases at a3caa21d, plus 6 new rows, minus 3 held-out bindings.
- **Unit names:** each host has exactly 6 new names, the 6 new wrong-implementation InlineData rows. Both hosts hold the same 4838 names.
- **Gates:** every count, time and receipt hash is as stated. Linux Test 2890BB37; Windows Test 4838 with ValidateTraceability passed; lanes D25D032E 741 and Windows 718 (proof head 88c5abcc, task removed); Build.Tests 13A0BE15 563 and 467; Linux ValidateTraceability 13:11:13 to 13:11:18.
- **Manifest:** B70AEB53, 34 files, every head 88c5abcc, 0 problems. The timing sweep (659B859F), store readback (B294B89A) and eighth-findings export (0BFFF36B) hashes match.
- **Store:** ac-4 read through the plugin getter matches the quoted text. TEST-MCP-TRIAGESTORE-007 is as quoted.
- **PR 87:** draft at 88c5abcc. The timeline shows ready_for_review 09:59:32 local and convert_to_draft 05:08:10 local.
- **Eighth-read findings:** every one is fixed as 27.32 says except f4 (my f1 below).

**Slice Z6 code against ac-4 as stored (none of these is a material gap)**
- **Unprefixed spelling of an owned host-prefixed setting** (NOLOGO beside the owned DOTNET_NOLOGO): removed by SetsOwnedKey and ConfigurationKeysSetBy (`TemporaryInstanceChecks.cs:210, 317-344`). A row covers it.
- **Host-prefixed spelling of an owned unprefixed setting** (DOTNET_Logging__LogLevel__Default, DOTNET_port beside PORT): removed. A row covers it.
- **ASPNETCORE_ spelling of an owned DOTNET_ setting:** removed, and refused if present, because IsProductVariable treats every ASPNETCORE_ name as a product variable (`:531`). No slice Z6 row, but the earlier isolation rows cover ASPNETCORE_ names.
- **The instance's own DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT pair:** accepted, because names are grouped by ConfigurationKeyIdentity. A row covers it, and the wrong check that strips host prefixes is rejected on it.
- **Two cases of an unprefixed spelling** (NOLOGO and nologo): refused. A row covers it.
- **The REPL start path:** ApplyOwnedStartEnvironment goes through OwnedInstanceProcess.ReplaceEnvironment, which builds and checks with the shared rule (`OwnedReplStart.cs:88`, `OwnedInstanceProcess.cs:45-46`). It meets ac-4.
- **Observation:** the start check accepts DOTNET_NOLOGO=1 beside a surviving NOLOGO=0, because the two names differ by a prefix, not only in separators or case. That matches the stored refusal clause, and every start path builds first. If a reviewer wants the check as a backstop, the strengthening option is an ac-4 append: refuse an owned setting beside another name that sets the same key through a host prefix, unless both names are the instance's own. Add a check row and a wrong check.

**(a) Superseded statements with no forward note**
- **a1.** Plan section 13 D-A (line 239) ends: "Superseded again in 27.31: ... 27.31 names the last code head `a3caa21d`, records every gate there, and states the D-A range (its "Docs only after `a3caa21d`" line; it named `ff7dcf51` until slice Z5)."
  - It has no 27.32 note. The eighth read's a1 and a2 fixes reached only 27.20 and 27.21.
  - Fix: append "Superseded again in 27.32: slice Z6 (`88c5abcc`) changed test support and tests after it, and 27.32 names the last code head `88c5abcc`, records every gate there, and states the D-A range (its "Docs only after `88c5abcc`" line)."
- **a2.** Plan section 13 D-B (line 240): "Superseded again in 27.30 and 27.31: as amended by 27.22 to 27.31, and every gate it names is the gate at the last code head `a3caa21d` that 27.31 records".
  - Fix: add "Superseded again in 27.32: as amended by 27.22 to 27.32, ... the gate at the last code head `88c5abcc` that 27.32 records".
- **a3.** Two pointers to the gates of record still end at a3caa21d:
  - Plan 25.7, line 1105: "since 27.31, at `a3caa21d`".
  - Plan 27.22, line 1544: "since 27.31 they are at `a3caa21d`".
  - Fix: append "; since 27.32, at `88c5abcc`" to both.
- **a4.** Two pointers to the last code head still name a3caa21d:
  - Plan 27.22, line 1591: "27.31 ..., which names the current last code head `a3caa21d`".
  - Plan 27.24, line 1750: "the ready step after the gates at the last code head (`a3caa21d`, 27.31) is a real step".
  - Fix: add 27.32 and 88c5abcc to both.
- **a5.** Every task-count chain stops at 50 and 27.31:
  - Plan lines 457 ("since 27.31, all 50"), 1371, 1505, 1543, 1575, 1682 and 1752.
  - Plan lines 1802, 1848 and 1898 ("50 since slice Z4 part 3 and slice Z5, 27.31").
  - Plan lines 1949 ("superseded again by the round 13 D-B of 27.31, all 50 tasks") and 2003.
  - Only line 2102 notes 51. The live TODO has 51 tasks (`ug-r13z6-store-compare-z5-z6.txt`).
  - Fix: append "51 since slice Z6, 27.32" to each.
- **a6.** Brief line 178 is behind slice Z6 in three places:
  - The slice-evidence list ends at 27.31. It has no `ug-r13z6-mockgate-1`, `-compile-1` or `-binding-1`.
  - The list of history gates omits a3caa21d (`ug-r13z5-final-*`).
  - "no deploy ran from ... ff7dcf51 or a3caa21d" omits 88c5abcc.
  - Fix: add all three.
- **a7.** Brief UG26 (line 211) lists the committed-tree citation sweeps only "at b79fd51f and ... ug-r13z5-citation-sweep-all-1.txt at a3caa21d". It omits `ug-r13z6-citation-sweep-all-1.txt` at 88c5abcc, which UG7 names. Fix: add it.
- **a8.** Brief UG27 (line 212) "holds in the code at a3caa21d", and UG37, UG38 and UG39 (lines 222 to 224) "at a3caa21d ...".
  - The head under review is 88c5abcc, and `TemporaryInstanceChecks.cs`, which UG38 names, changed in slice Z6.
  - UG47 (line 232) says "(history; UG48 gives a3caa21d)", but UG48 gives no by-name statement; UG49 does, at 88c5abcc.
  - Fix: "at 88c5abcc" in UG27 and UG37 to UG39, and "UG49 gives 88c5abcc" in UG47.
- **a9.** Self-review line 19: "D-B is stated once, in 27.21 as amended by 27.22 to 27.31". Fix: "27.22 to 27.32".
- **a10.** Projected problems item 5 (line 43) and PR body line 328: "MEMORY-FACT-008's dated change logs cover ... Z4 (...) and Z5". Slice Z6 is missing. Fix: add Z6, with the e1 exception.
- **a11.** Projected problems "47 to 62" (line 58): "5 ORDER unit hits in ug-r13z5-timing-sweep.txt, as in ug-r13z4-timing-sweep.txt". The current receipt is `ug-r13z6-timing-sweep.txt` (still ORDER=5). Fix: cite it.
- **a12.** PR body heading "### Operator rulings (plan 27.19 to 27.31)" (line 243) and its 2026-11-05 bullet (line 253) leave out the 27.32 ruling ("Declare, out of scope", 12:55:43 to 13:10:46). Fix: "27.19 to 27.32" and add that answer.

**(b) Contradictions**
- **b1 (material).** Plan section 6 (line 150) says "the rule for two names is unchanged, so the instance's own DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT pair is accepted." PR body line 140 says the same.
  - The new change log on CheckOwnedInstanceEnvironment (`TemporaryInstanceChecks.cs:241`) says "the names the rule for two names covers now include the unprefixed spellings of a key the instance sets under a host prefix".
  - The new row "two cases of an unprefixed spelling of an owned host-prefixed setting" is refused.
  - The wrong check "two-name rule on owned keys without host prefixes (the check before the eighth holistic read)" is rejected on it.
  - Only the grouping is unchanged, which 27.32, the commit message and UG49 state correctly. Projected problems 141's "Behavior change" sentence names only the builder change.
  - Fix in all three: "the rule for two names now also covers the unprefixed spellings of a key the instance sets under a host prefix (NOLOGO beside nologo is refused) and still groups names by separators and case, so the pair is accepted".
- **b2.** Brief UG12 (line 197) says "the TODO tasks to be checked (all 50)". The brief's own line 59, plan 2146, the PR body and the handoff all say 51. Fix: "(all 51)".

**(c) Statements a reviewer can show false**
- **c1 (material).** The d3 event counts are put on the lane, but the receipt shows the McpServer service wrote part of them.
  - Projected problems 139: "about 150 short-lived processes and one in-process test host wrote 2,777 and 3,506 QBrainAi events (about 210 errors and 510 to 550 warnings each)".
  - PR body line 288: "(about 2,800 to 3,500 QBrainAi events per lane".
  - `ug-r13z6-win-qbrainai-event-sources-1.txt` shows the McpServer service (PID 36252) wrote 808 of the 3,506 (line 24) and 124 of the 2,777 (line 188). The lane's processes wrote about 2,698 and 2,653.
  - The receipt gives levels per window, not per process, so the error and warning counts cannot be put on the lane. Plan 27.32 states the split correctly.
  - The operator's d3 question at 12:55:43 carried the same totals (transcript line 130277). The ruling stands.
  - Fix: "the lane's processes wrote about 2,650 to 2,700 per lane; the service wrote the other 124 and 808 of the window totals of 2,777 and 3,506 (levels not split by process)". Note in 27.32 that the question stated the window totals.
- **c2 (material).** The outside-lane rate is understated.
  - Plan 27.32 (line 2131): "Outside lanes (09:06 to 10:29 and 11:04 to 12:09) ... 16 to 18 a minute". Projected problems 139 repeats it.
  - The receipt's oldest event is 10:02:01 (line 6), and PID 36252's first event in that window is 10:02:01 (line 12). The 16.06 a minute for the window before the rerun divides 1,333 events by 83 minutes, but the log holds only about 27 of them, so the true rate is about 49 a minute.
  - Only the 11:04 to 12:09 window (18.45 a minute) is a measured outside rate. Inside the two lanes the service wrote about 27 and about 4 a minute. So "rises to 116 and 91 a minute" is measured against an understated baseline.
  - Fix: restate that window from 10:02:01, give the service's own range, and compare the lane processes' own counts.
- **c3 (material).** The brief's starting point (line 23) says: "slice Z5 (commit a3caa21d, the last code head ...), ..., the eighth holistic read and its fixes, and the docs-only commits after a3caa21d (`git log --first-parent a3caa21d..{HEAD}`)".
  - Git shows a3caa21d..HEAD holds 88c5abcc, a test and test-support commit.
  - Section 27.32, slice Z6, task 51 and the gates at 88c5abcc are missing, and the eighth read sits under 27.31.
  - Fix: add the 27.32 paragraph (the eighth read and its fixes, slice Z6 at 88c5abcc as the last code head, task 51, the d3 ruling, the gates at 88c5abcc, the ninth read) and use `88c5abcc..{HEAD}`.
- **c4.** Projected problems carried line 36: "on the committed tree, ug-r13z4c-citation-sweep-all-1.txt at a3caa21d ug-r13z5-citation-sweep-all-1.txt". The ug-r13z4c sweep is at b79fd51f (plan 2079). Fix: "ug-r13z4c-... at b79fd51f, ug-r13z5-... at a3caa21d".
- **c5.** Self-review line 7: "DOTNET_NOLOGO, DOTNET_CLI_TELEMETRY_OPTOUT and the instance's ... pair carry one, and the contract rows cover the first two". The slice Z6 rows hold NOLOGO only; no row holds CLI_TELEMETRY_OPTOUT. Fix: "the rows cover DOTNET_NOLOGO; DOTNET_CLI_TELEMETRY_OPTOUT is covered by the same rule with no row of its own".
- **c6.** Slice Z6's scope is stated as host settings only:
  - Plan 27.31, line 2076: "host-prefixed and unprefixed forms of an owned host setting only since slice Z6".
  - Brief line 17: "host-prefixed and unprefixed spellings of the instance's own host settings".
  - Handoff line 9: the same.
  - Slice Z6 also removes host-prefixed spellings of owned unprefixed settings (DOTNET_port beside PORT, DOTNET_Logging__LogLevel__Default), as section 6 line 150 and the code change log say.
  - Fix: "an unprefixed or host-prefixed spelling of a key the instance sets under the other form".

**(d) Missing disclosures**
- **d1.** Plan 27.32 records the d3 question only as "with "Strengthen + fix" recommended; the operator chose "Declare, out of scope"". It gives no option texts.
  - The transcript (line 130277) holds the chosen text: "Record the classification and receipts only; no criterion, code or TODO change." The other options were "Strengthen + fix (Recommended)" and "Declare + follow-up TODO".
  - No triage is cited. The d4 health-window case of 27.29 was triaged, though this option text does not ask for one.
  - Fix: quote the three option texts and state whether a triage was filed.
- **d2.** Plan 27.32 says "about 150 short-lived processes, none still running". The receipt shows PIDs 8048, 19000, 41952 and 44824 running as conhost, and 12136 as llama-server (lines 364, 387, 410, 552, 607).
  - PID 19000, which wrote 2,003 events from 10:58:36, is now a conhost started 11:35:20. These PIDs were reused by later processes, which the records do not say.
  - The short-lived counts are 141 and 139.
  - Fix: state the PID reuse and the counts.
- **d3.** Plan 27.32 ("6 paths, signed"), the PR body and UG10 call the commits signed, but the commits carry no cryptographic signature.
  - `git cat-file` shows no gpgsig header on 88c5abcc, a3caa21d or 52e6d627, and %G? reads N. "Signed" here is the marker-rule "Signed: claude-opus-5-5-xhigh@ClaudeCode | Confidence" line in the message.
  - Fix: define it once in the plan and UG10.

**(e) Brief claims the evidence cannot support**
- **e1.** UG49: "every changed member carries the dated change log ... (Find-ChangedFilesWithoutDatedLog.ps1 over a3caa21d..88c5abcc: 6 files, none without)".
  - The check is per file, and no receipt of it exists in the receipts folder.
  - The new member `private static readonly string[] HostPrefixes = ["DOTNET_", "ASPNETCORE_"];` (`TemporaryInstanceChecks.cs:346`) has no XML documentation and no dated entry. That is the class of the two slice Z3 constants the records declare.
  - The class now states the host prefixes twice: HostPrefixes, and the "ASPNETCORE_" and "DOTNET_" literals in IsProductVariable (`:531-533`). That is the DRY class of the second holistic read's C5.
  - Fix: either document HostPrefixes and use it in IsProductVariable (this moves the head), or declare both and narrow the claim to "every changed method and class". Save the file check as a receipt.
- **e2.** UG49 and 27.32: "the three binding facts held out and re-appended byte-identical (Compare-TextBlock.ps1 ..., 38 lines, 86DD9BF8...)". No receipt of that comparison exists. Fix: save it, or say the claim rests on the transcript.
- **e3.** Brief line 50: "option texts in 27.32 and the transcript". Plan 27.32 holds none (d1). Fix: d1, or say "in the transcript".
- **e4.** UG49: "the sibling sweep ug-r13z6-g1-sibling-sweep-1.txt finds every host-prefixed setting the test support sets".
  - Its pattern, `\["(DOTNET|ASPNETCORE)_...\"\]\s*=`, matches only dictionary indexer assignments.
  - My wider read-only search of every host-prefixed string literal under tests (35 matches) found no further start path, so the outcome holds. The receipt alone does not show "every", and it cannot see start paths that set an owned unprefixed key without the builder.
  - Fix: save the wider sweep as the receipt, or narrow the claim.

**(f) Eighth-read findings that 27.32 says are fixed but are not**
- **f1 (eighth f4).** 27.32 says the self-review's forward-note line "now claims no more than the ninth holistic read confirms".
  - Self-review line 18 still lists, as done for the statements 88c5abcc supersedes, "plan section 13's D-A and D-B history", "every task-count chain (50 since 27.31)" and "every pointer to the last code head and to the gates of record".
  - a1 to a5 show none of these moved to 88c5abcc or 51.
  - Fix: apply a1 to a5, then reword the line.

**(g) Slice Z6 code or tests against the stored criteria**
- None material. The six what-ifs meet ac-4 as stored. The start-check observation is listed with them, with the strengthening option.

**Scripts and outputs.** All scripts are read only and were analyzer clean before they ran. Everything is in `/home/sharpninja/.claude/scratch/pr82-review-20261019/holistic-r13i/`:
- **Copies of the r13h scripts with the output folder changed:** Get-GitFactsI.ps1, Get-BlobHashesI.ps1, Show-ReceiptSummaryI.ps1, Test-ManifestI.ps1 and Save-RequirementsI.ps1.
- **New scripts:** Show-Folder.ps1, Find-TextInFiles.ps1, Test-CommitSignature.ps1, Get-PrState.ps1 and Find-TranscriptSnippet.ps1.
- **Pattern files:** pattern-host-prefix-literals.txt, pattern-fifty.txt, pattern-a3-current.txt and pattern-to-2731.txt.
- **Outputs:** git-facts.txt, diff-z6.txt, blob-hashes.txt, file-hashes.txt, receipts-summary.txt, manifest-check.txt and store-reqs.txt.
- **Turn files:** turn-update-1.psd1, turn-update-2.psd1 and turn-response-r13i.txt.

Bash only launched `pwsh -NoProfile -NonInteractive -File` with literal arguments. I made no contact with PAYTON-LEGION2 and printed no secret.
