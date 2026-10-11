<!-- SPDX-License-Identifier: Apache-2.0 -->
# Hostile validator: Linux export creation plan, round 1

TimestampUtc: 2026-10-11T12:07:44.6216064Z
Validator: Codex CLI 0.160.1, gpt-6-sol, xhigh. Live session proof: /home/sharpninja/.codex/sessions/2026/10/11/rollout-2026-10-11T06-57-40-01a12ad3-6574-7932-995d-c1bae28ba32f.jsonl, session_meta ordinal 0 and turn_context ordinal 7; CODEX_SESSION_ID and runner thread.started match 01a12ad3-6574-7932-995d-c1bae28ba32f.
First actions: add-profile skill and 19 non-skill profile Markdown files read in full; main workspace marker read in full; workflow.memory.list returned 13 memories and every text was read.
HEAD: 93746b9b1feea22d348cf06b687c2a49e030c2df. Own MCP turn: Codex-20261010T004320Z-plugin-session / req-20261011T120009Z-prompt-59c0.

OverallVerdict: **DISAGREE**. Accuracy 84%; completeness 72%; confidence 97%. Claims: 1 PASS, 7 FAIL, 1 UNKNOWN. The scores reflect false absolute containment assertions, an unbounded prefix-probe path, insufficient tests, incomplete error contracts and an inaccurate unit gate.

## Claims

### P1 UNKNOWN

- HEAD 93746b9b commits only the 85-line plan. ExportRoot.cs:64-72,75-98,593-615 and BoundedTraversal.cs:263-293,864-908 support the quoted current-behavior statements.
- Live workflow.todo.get BUG-BASELINE-001 names exactly 23 target failures, allocated 18 missing roots, 2 missing nested folders, 1 nested cleanup refusal, and 2 alias-swap races.
- Independent filtered test run at HEAD failed 28/88, with all 23 named targets failing plus five Twelfth traversal failures. Runner /proc/self/mountinfo has two /tmp mount entries, IDs 2274 and 2327, causing OpenApprovedMountAnchor to reject the approved mount. The historical supportmcp-full-g2-after-009.log is absent from this repo, so the exact historical 23-only remeasurement is unverified.

### P2 FAIL

- Plan:27-33 promises rollback on every post-mkdirat failure and says an inode check means it cannot remove another object.
- If the first statx fails after successful mkdirat, step 3 observed no inode, so step 5 cannot identify its own child. A leaf can also be replaced between the second statx and unlinkat; unlinkat names a directory entry, not the checked inode. A moved pinned parent makes both operations external. The stated absolute no-pre-existing-external-object guarantee and operator acceptance of that precise risk are unsupported.

### P3 PASS

- Plan:35-37 uses root as anchor and first parent, then each prior verified child descriptor as parent with the accumulated prefix; successful existing-segment opens retain directory and root-mount checks from BoundedTraversal.cs:277-291. This claim is assessed subject to the D1 defects in P2.

### P4 FAIL

- Plan:42-45 says full-path statx AT_SYMLINK_NOFOLLOW probes never follow links. LinuxPhysicalPathResolver.cs:438-446 passes an absolute path to statx with that flag; the flag applies to the terminal component, while intermediate links are resolved. A missing root beneath an intermediate symlink to FUSE can therefore probe the unapproved target before the anchored capture. Plan:69 tests only a lexical FUSE mount, not this case or a swap after creation before capture.

### P5 FAIL

- Plan:49-54 handles ENOENT only from unlinkat, but OpenLinuxPathBeneathRoot maps a missing target or parent to DirectoryNotFoundException at BoundedTraversal.cs:890-895. Existing direct-leaf DeleteContainedEntry returns false on ENOENT at ExportRoot.cs:604-615, so nested missing semantics are unstated.
- An attacker can replace the leaf after the target/statx identity comparison and before unlinkat, even without moving the parent. The residual-risk sentence says replacement only occurs if the mover also replaces the entry. It understates the name-based final race; with a moved parent the replacement may be outside the export root.

### P6 FAIL

- The needed libc P/Invokes already exist at WorkspaceContainedFileSystem.cs:959-977; O_PATH and openat2 constants exist at BoundedTraversal.cs:36-49. Linux man-pages confirm mkdirat dirfd semantics, umask, O_PATH directory-fd use, statx no-follow terminal behavior, openat2 resolution flags, and unlinkat AT_REMOVEDIR/ENOTEMPTY.
- Plan:43 applies AT_SYMLINK_NOFOLLOW to an entire absolute path, which is incorrect. Plan:27 spells the mode as 0777 without a C# numeric value; a literal 0777 in C# is decimal 777, not POSIX octal 0777. The handoff must say 0x1FFu and explicitly reuse the existing P/Invokes.

### P7 FAIL

- Live main-workspace getTr returns TR-MCP-USECASE-017 with acceptanceCriteria: []; getFr FR-MCP-USECASE-015 and getTest TEST-MCP-USECASE-018 cover contained export and requirements/wiki behavior; listMappings returns both FR-to-TR and FR-to-TEST links. No new FR, TEST, or mapping is needed.
- Plan:58-61 AC labels omit observable EEXIST verification, root post-create swap rejection, target identity/kind, and missing-path behavior. Live BUG-BASELINE-001 technicalRequirements is [], and plan:56-62 does not schedule linking the TODO to TR-MCP-USECASE-017.

### P8 FAIL

- Plan:67-74 names six new tests but none for root swap between creation and capture, rollback when statx fails or child becomes nonempty, EEXIST non-directory, nested empty-directory deletion, or missing nested target.
- Existing BugTriage139EighteenthLinuxTests.cs:68-84 expects missing nested directory to throw, and :534-565 expects nested stale cleanup to be refused; both contradict D2/D4 and are not named for update. The read alias-swap race in RequirementsTwelfthAdversarialReviewTests.cs:112-167 does not use D1/D2, because it opens an existing root and file. Plan:74 only permits accepted-failure-list changes for new D1/D4 outcomes, so it cannot resolve both existing race failures.

### P9 FAIL

- Plan:78-79 says the other five unit projects; approved-plan P0.1 receipt p01-baselines-20261010T215349Z.md:25 lists seven unit projects total, hence six besides Support.Mcp. build/Build.Test.cs enumerates all eligible projects and does not limit this to five.
- Plan:27-30,43-44,49-54 leaves errno-to-failure behavior unresolved for failed ancestor statx, failed first child statx after creation, missing nested delete pre-open, and failed rollback. G1 omits the two existing contradictory tests. Windows code paths are unchanged and Build.Tests is infrastructure, but the gate and failure contract are not decision-complete.

## Material findings and required changes

### F1 (P2)

- Location: Plan:27-33
- Defect: Rollback cannot prove ownership when first statx fails, and statx-then-unlinkat can remove a replacement after the check. The sentence promising no pre-existing external object mutation is false under a moved parent and adversarial leaf replacement.
- Evidence: Plan D1 ordering; WorkspaceContainedFileSystem.cs:959-977; Linux unlinkat and statx manuals.
- To pass: Specify the no-identity and changed-identity outcomes without claiming safe rollback; add a deterministic replacement-before-unlink adversarial case. Resolve whether the accepted product guarantee permits external transient creation or name-race deletion, or require a design that actually enforces the strict guarantee.
- Classification: Operator decision: accept explicit residual external mutation under this chosen Linux-create direction, or require a stronger strict-containment contract. Implementer must correct the plan either way.

### F2 (P4, P6)

- Location: Plan:42-44; LinuxPhysicalPathResolver.cs:438-446
- Defect: Absolute-path statx with AT_SYMLINK_NOFOLLOW can traverse an intermediate symlink before the approved-mount pin.
- Evidence: Current TryStatxPath uses AT_FDCWD plus full path; Linux statx/path-resolution semantics limit no-follow to the terminal component.
- To pass: Walk candidate ancestors component by component from a pinned approved mount with no-symlink and no-cross-device resolution, distinguish ENOENT from other errors, and add a delayed-FUSE symlink-prefix test.
- Classification: Implementer edit.

### F3 (P5)

- Location: Plan:49-54
- Defect: Missing nested entries can throw during parent or target open before the planned ENOENT handler, and the residual statement omits leaf replacement while the parent remains in place.
- Evidence: BoundedTraversal.cs:890-895 maps ENOENT to DirectoryNotFoundException; ExportRoot.cs:604-615 returns false for direct-leaf ENOENT; unlinkat acts on a name after the identity check.
- To pass: Define false versus failure for each pre-open/statx missing case, document the leaf replacement race accurately, and test both nested file and empty-directory deletion including moved-parent and replacement cases.
- Classification: Implementer edit, subject to the F1 operator risk decision.

### F4 (P6)

- Location: Plan:27
- Defect: POSIX 0777 is ambiguous in a C# handoff and would be decimal if pasted as a C# literal.
- Evidence: WorkspaceContainedFileSystem.cs:123-124 uses hexadecimal permission constants; C# has no leading-zero octal literal.
- To pass: State mode 0x1FFu (POSIX octal 0777), umask application, and that MakeDirectoryAtUnix, UnlinkAtUnix, StatxUnix and OpenAt2Traversal are existing declarations, with no new P/Invoke required.
- Classification: Implementer edit.

### F5 (P7)

- Location: Plan:56-62; live BUG-BASELINE-001
- Defect: The planned ACs do not test key observable D1-D4 outcomes and the owning TODO has no technicalRequirements link.
- Evidence: Live getTr shows zero ACs; live getFr/getTest and listMappings show existing accountability; live todo.get shows technicalRequirements: [].
- To pass: Strengthen the three AC texts with EEXIST/rollback, root swap/mount rejection, and nested deletion identity/kind/missing outcomes; explicitly link BUG-BASELINE-001 to TR-MCP-USECASE-017 before implementation.
- Classification: Implementer edit.

### F6 (P8)

- Location: Plan:67-74; BugTriage139EighteenthLinuxTests.cs:68-84,534-565; RequirementsTwelfthAdversarialReviewTests.cs:112-229
- Defect: Named tests omit material branches, two existing Linux tests assert obsolete behavior, and the read alias race cannot be fixed by D1/D2.
- Evidence: The existing tests and call paths at WorkspaceContainedFileSystem.cs:218-233 and BoundedTraversal.cs:211-245 show the conflict and separate read/write paths.
- To pass: Name tests for root create-to-capture swap, rollback and EEXIST failure paths, nested empty-directory/missing deletion, and update the two obsolete Linux assertions. Specify evidence-based accepted fail-closed exceptions for both Twelfth races, including the existing read path.
- Classification: Implementer edit.

### F7 (P9)

- Location: Plan:27-30,43-44,49-54,78-79
- Defect: G2 omits one of the six other unit projects and the plan leaves several error mappings and rollback failure results to the implementer.
- Evidence: p01-baselines-20261010T215349Z.md:25 has seven unit projects; build/Build.Test.cs executes the eligible project inventory; source maps missing openat2 paths to DirectoryNotFoundException.
- To pass: Name the full G2 ./build.ps1 Test gate or all seven unit projects, add the obsolete-test updates to G1, and specify error types/messages for every D1-D4 failure including statx errors, post-create capture mismatch, and missing nested deletion.
- Classification: Implementer edit.

## Commands and exit codes

- Exit 0: git rev-parse HEAD; git status --short; git show --stat HEAD. HEAD 93746b9b; only plan committed at HEAD; unrelated benchmark files pre-existed dirty.
- Exit 0: Invoke-CodexMcpPlugin.ps1 -Command Status (worktree). available; signed-marker/nonce plugin status; own turn opened and updated.
- Exit 0: workflow.memory.list through Codex wrapper. 13 records, every text read; coordination record length 120696 characters.
- Exit 1: workflow.todo.get BUG-BASELINE-001 with MCP_WORKSPACE_PATH main, default cache. Plugin command failed with exit code 1. False; retried with outside-worktree CacheRoot.
- Exit 0: workflow.todo.get; getTr; getFr; getTest; listMappings with MCP_WORKSPACE_PATH main and CacheRoot /tmp/codex-hv-linuxexportcreate-20261011T115738Z-cache. TODO and FR/TR/TEST/mapping readbacks returned successfully; TR AC array empty; TODO technicalRequirements empty.
- Exit 1: dotnet test tests/QBrainAi.Support.Mcp.Tests -c Debug --filter named eight-class substring filter --logger console;verbosity=normal. 28 failed, 69 passed, 0 skipped, 97 total; substring also included TransactionGated class.
- Exit 1: dotnet test tests/QBrainAi.Support.Mcp.Tests -c Debug --no-build --filter eight-class filter excluding TransactionGatedRequirementsDocumentServiceTests --logger console;verbosity=quiet. 28 failed, 60 passed, 0 skipped, 88 total; 23 named TODO failures plus five /tmp mount-ID traversal failures.
- Exit 0: rg /tmp /proc/self/mountinfo. Two /tmp entries: mount IDs 2274 and 2327, explaining runner-specific approved-mount identity rejection.
- Exit 0: Read plan, source, tests, Build.Test.cs, approved plan, receipts, session metadata at HEAD. Anchors cited claim by claim and finding by finding.

## Primary Linux syscall references

- https://man7.org/linux/man-pages/man2/openat2.2.html
- https://man7.org/linux/man-pages/man2/statx.2.html
- https://man7.org/linux/man-pages/man2/mkdir.2.html
- https://man7.org/linux/man-pages/man2/unlink.2.html
- https://man7.org/linux/man-pages/man2/open.2.html

## Receipt and session-log paths

- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.md
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.json
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261011T115738Z-plan-readiness-linuxexportcreate-r1.request.jsonl
- /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261011T115738Z-plan-readiness-linuxexportcreate-r1.response.jsonl
- Reviewer MCP session: Codex-20261010T004320Z-plugin-session / req-20261011T120009Z-prompt-59c0

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":84,"completeness":72,"confidence":97,"passCount":1,"failCount":7,"unknownCount":1,"failList":["P2","P4","P5","P6","P7","P8","P9"],"unknownList":["P1"],"implementerFixes":["F2 component-wise anchored ancestor discovery and test","F3 nested deletion missing/error/race contract and tests","F4 explicit C# mode and existing P/Invoke list","F5 stronger ACs and TODO TR link","F6 complete named tests and update obsolete assertions","F7 full G2 inventory and error contracts"],"operatorDecisionFails":["F1 choose whether strict no-external-object mutation remains mandatory under the selected mkdirat/unlinkat direction or explicitly accept its identified residual races"],"headSha":"93746b9b1feea22d348cf06b687c2a49e030c2df","receiptPaths":["/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.md","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.json","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261011T115738Z-plan-readiness-linuxexportcreate-r1.request.jsonl","/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261011T115738Z-plan-readiness-linuxexportcreate-r1.response.jsonl"]}
