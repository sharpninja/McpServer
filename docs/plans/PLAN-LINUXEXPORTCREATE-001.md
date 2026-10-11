<!-- SPDX-License-Identifier: Apache-2.0 -->
# PLAN-LINUXEXPORTCREATE-001: Linux contained export creates its folders (BUG-BASELINE-001)

Status: revision 2, for hostile validation round 2, then implementation. Owner TODO: BUG-BASELINE-001 (PLAN-SESSIONTURNREVIEW-001 P0.1). Worktree `.claude/worktrees/session-20261009`, branch `worktree-session-20261009`. Round 1 (docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.md, DISAGREE 84/72, F1-F7) is answered in section 9.

## 1. Decisions being implemented

- Operator, 2026-10-11T00:02:53Z, verbatim choice: "Create folders on Linux (Recommended)": parity with Windows; create the export root before capture, nested folders with mkdirat on a pinned parent plus identity verification, stale cleanup with unlinkat on a pinned parent; planned and HV'd as a product change.
- Operator, 2026-10-11 (after round 1 showed Linux has no kernel-atomic create or delete beneath a pinned root), verbatim choice: "Accept stated residuals (Recommended)": keep the pinned-parent design, state the two races precisely as accepted residual risk, narrow the deletion window by renaming the entry to a random name inside the same pinned directory, verifying its inode, then unlinking; adversarial tests for both races; no external mutation stays guaranteed for every path without a concurrent malicious writer inside the root.

## 2. Current behavior (receipts at HEAD 93746b9b)

- `src/QBrainAi.Storage/WorkspaceContainedFileSystem.ExportRoot.cs:64-72`: Windows `OpenWindowsExportRoot` calls `Directory.CreateDirectory(rootPath)` before pinning.
- Same file `:75-98`: Linux `OpenLinuxExportRoot` pins with `LinuxPhysicalPathResolver.OpenDirectoryNoCrossDevice` (`src/QBrainAi.Client/LinuxPhysicalPathResolver.cs:223`); a missing root throws.
- `src/QBrainAi.Storage/WorkspaceContainedFileSystem.BoundedTraversal.cs:263-293`: `EnsureLinuxDirectoryRooted` only opens existing segments through `OpenLinuxPathBeneathRoot` (`:864-908`: openat2 with `RESOLVE_NO_XDEV | RESOLVE_NO_MAGICLINKS | RESOLVE_NO_SYMLINKS | RESOLVE_IN_ROOT`; `ENOENT` maps to `DirectoryNotFoundException(displayPath)`, `EXDEV` to `NotSupportedException`, `ELOOP` and `ENOTDIR` to `IOException`, others to `IOException` with `Win32Exception(errno)`). Its comment `:271-275` records why creation was refused.
- `WorkspaceContainedFileSystem.ExportRoot.cs:593-616`: Linux `DeleteContainedEntry` refuses any nested path with `NotSupportedException`; a leaf directly beneath the root uses `unlinkat(rootfd, ...)`, returning `false` on `ENOENT` (and `ENOTEMPTY` for directories).
- Existing native declarations (no change needed): `MakeDirectoryAtUnix` (mkdirat), `UnlinkAtUnix`, `StatxUnix` at `WorkspaceContainedFileSystem.cs:959-977`; `OpenAt2Traversal` and the openat2 constants including `LinuxOpenPath = 0x200000` at `BoundedTraversal.cs:30-52`. New declarations needed: `renameat2` in Storage (D5) with `RENAME_NOREPLACE = 1`, and the constant `ResolveBeneath = 0x08` in `LinuxPhysicalPathResolver` (which today declares `ResolveNoCrossDevice`, `ResolveNoMagicLinks`, `ResolveNoSymbolicLinks` at `:25-27`).
- Effect: the Support.Mcp unit suite at 26c47e15 (Category!=AiReview&!=Integration, no class excluded, container endpoints loaded) fails 23 tests, all owned by this TODO; the receipt is committed with this plan's implementation (it lived in the scratchpad when round 1 ran). 18 fail because the export root does not exist, 2 because a nested folder does not exist, 1 because nested stale cleanup is refused, and the 2 `RequirementsTwelfthAdversarialReviewTests` alias-swap races surface outcomes their accepted list omits (section 5.4).

## 3. Contract

- Windows behavior and code paths are unchanged.
- Linux, without a concurrent malicious writer inside the export root: creation and deletion act only on objects beneath the pinned root, and no object outside it is created, modified, read, or deleted.
- Linux, with a local writer who controls a directory inside the export root and races these operations (accepted residual risk, operator decision above): (R1) creation may leave or briefly create one empty, service-owned directory inside a directory that writer moved outside the root; (R2) deletion may remove an object that writer placed under the random temporary name inside a directory that writer controls. No other external effect is possible: every mutation is relative to a descriptor pinned beneath the root and targets one directory entry in that directory.

## 4. Design

### D1. Pinned child-directory creation

`CreateLinuxChildDirectoryPinned` (new private static in `WorkspaceContainedFileSystem.BoundedTraversal.cs`), inputs: `anchor` (`SafeFileHandle` of the resolution root), `anchorIdentity` (`UnixStatx`), `parent` (pinned `SafeFileHandle` of the directory that receives the child, itself resolved beneath `anchor`), `parentRelative` (anchor-relative path of `parent`, `"."` for the anchor), `name` (one segment validated by `SplitContainedSegments`), `displayPath`. Returns the verified child `SafeFileHandle`.

1. `MakeDirectoryAtUnix(parent, name, 0x1FFu)` (POSIX mode 0777; the process umask applies, as for `Directory.CreateDirectory` on Unix). `0` sets `created = true`; `EEXIST` sets `created = false`; any other errno throws `IOException($"Unable to create contained Linux directory '{displayPath}'.", new Win32Exception(errno))`.
2. `StatxUnix(parent, name, AT_SYMLINK_NOFOLLOW, basic mask)`. On failure: if `created`, no rollback is attempted (ownership cannot be proven) and the method throws `IOException($"Requirements export could not verify the directory it created: '{displayPath}'.", new Win32Exception(errno))` (residual R1 if the parent had left the root); if not `created`, `ENOENT` means a concurrent remover won and the method throws `DirectoryNotFoundException(displayPath)`, other errnos throw `IOException` with the errno. A non-directory kind throws `IOException($"Requirements export path is not a directory: '{displayPath}'.")` (an `EEXIST` file or symlink is never used and never removed).
3. Re-resolve `parentRelative/name` beneath `anchor` with `OpenLinuxPathBeneathRoot` (flags `O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC`), `fstat` it, require a directory (`EnsureLinuxDirectory`) on the anchor's mount (`EnsureLinuxDescriptorOnRootMount`), and require its (dev, ino) to equal step 2's.
4. If step 3 fails or the identities differ: when `created`, roll back by name-narrowed removal (D5 with `directory: true`, expected identity from step 2; `ENOTEMPTY` or a mismatch leaves the entry and is included in the message); then throw `IOException($"Requirements export directory creation rejected a parent that left the pinned root: '{displayPath}'.")`. When not `created`, throw the same message without any removal.
5. Test seams (static `Action?` fields, null in production): `s_beforeLinuxPinnedDirectoryCreate` (after the parent is pinned, before step 1) and `s_beforeLinuxCreatedDirectoryRollback` (before step 4's removal).

### D2. Nested folders

`EnsureLinuxDirectoryRooted` keeps today's open-and-check for each existing segment; on `DirectoryNotFoundException` for a segment it calls D1 with `anchor = rootHandle`, `parent` = the previous verified segment descriptor (the root for the first segment), `parentRelative` = the accumulated prefix. The returned descriptor is the parent of the next segment. The comment at `:271-275` is replaced with a summary of D1 and section 3.

### D3. Export root creation

Before today's capture in `OpenLinuxExportRoot`:
1. `BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(rootPath, "Requirements export root creation")`: unbounded mounts are rejected lexically before anything else (`NotSupportedException`, existing message).
2. New internal `LinuxPhysicalPathResolver.OpenDeepestExistingDirectoryNoCrossDevice(string lexicalPath, string operation, out string missingRelativeTail)` (in `src/QBrainAi.Client/LinuxPhysicalPathResolver.cs`, visible to Storage through the existing InternalsVisibleTo): `ResolveSymlinksOnApprovedMounts(lexicalPath)` (prefix-by-prefix; each link target is classified by the mount table before it is followed, and after BUG-BASELINE-009 C1 no prefix probe follows a link), then the approved mount of the resolved path is pinned (`OpenApprovedMountAnchor`), then the mount-relative components are walked one at a time with openat2 (`O_PATH | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC`, `RESOLVE_NO_SYMLINKS | RESOLVE_NO_XDEV | RESOLVE_NO_MAGICLINKS | RESOLVE_BENEATH`) from the anchor. `ENOENT` ends the walk: it returns the last opened descriptor and the remaining tail. `ELOOP`, `EXDEV`, `ENOTDIR`, and other errnos throw (`IOException`, `NotSupportedException`, `IOException`, `IOException` with the errno). No absolute-path `statx` is used.
3. If the tail is empty, behavior equals today's. Otherwise each tail component is created with D1 (`anchor` = the returned descriptor and its identity, `parent` advancing per component).
4. Then today's capture (`OpenDirectoryNoCrossDevice(rootPath)` with `s_afterLinuxExportRootPolicyClassification`) runs, and the captured root's (dev, ino) must equal the last descriptor D1 returned; otherwise the capture is disposed and `IOException($"Requirements export root changed between creation and capture: '{rootPath}'.")` is thrown. Test seam `s_afterLinuxExportRootCreated` runs between steps 3 and 4.

### D4. Nested deletion (`DeleteFileIfExists`, `DeleteEmptyDirectory` for paths containing `/`)

Direct leaves beneath the root keep today's code. For nested paths:
1. Open the parent beneath the root (`O_PATH | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC`, same resolve flags as D2). `DirectoryNotFoundException` returns `false`; other failures propagate.
2. Open the target beneath the root with `O_PATH | O_NOFOLLOW | O_CLOEXEC`; `DirectoryNotFoundException` returns `false`. `fstat` it: the kind must match the call (regular file or directory, else `IOException($"Requirements export cleanup target is not a {kind}: '{displayPath}'.")`), and its (dev, ino) must equal `StatxUnix(parent, leaf, AT_SYMLINK_NOFOLLOW)` (mismatch or `ENOENT` there: `IOException($"Requirements export cleanup rejected an entry replaced during deletion: '{displayPath}'.")`, or `false` for `ENOENT`).
3. Re-open the parent beneath the root and require the same (dev, ino); otherwise `IOException($"Requirements export cleanup rejected a parent that left the pinned root: '{displayPath}'.")` without mutation.
4. D5 with the verified identity.
Test seams: `s_beforeLinuxNestedDeleteRename` (after step 3) and `s_beforeLinuxNestedDeleteUnlink` (inside D5 after the temporary rename).

### D5. Name-narrowed removal (shared by D1 rollback and D4)

Inputs: pinned `parent`, `leaf`, expected (dev, ino), `directory` flag, `displayPath`.
1. `renameat2(parent, leaf, parent, tmp, RENAME_NOREPLACE)` with `tmp = ".qbrainai-remove-" + Guid.NewGuid().ToString("N")`. `ENOENT` returns `false` (already gone); other errnos throw `IOException` with the errno.
2. `StatxUnix(parent, tmp, AT_SYMLINK_NOFOLLOW)` must equal the expected identity. If not: rename back with `renameat2(parent, tmp, parent, leaf, RENAME_NOREPLACE)`; if that fails, the entry stays under `tmp` and the message names it; throw `IOException("... rejected an entry replaced during deletion ...")`.
3. `UnlinkAtUnix(parent, tmp, directory ? AT_REMOVEDIR : 0)`. `ENOTEMPTY` (directory): rename back as in step 2 and return `false`; `ENOENT`: return `false`; other errnos throw `IOException` with the errno. Success returns `true`.
Residual R2 is exactly the window between step 2 and step 3.

## 5. Tests (Byrd v4: RED before code)

All new Linux tests go in `tests/QBrainAi.Support.Mcp.Tests/Storage/BugTriage139EighteenthLinuxTests.cs`; every addition or rename updates `BugTriage139FourteenthHarnessTests` (per-class count and the platform total) and `BugTriage139FifteenthHarnessTests` (the inventory list).

5.1 D3: `OpenExportRoot_MissingRootAndAncestors_CreatesThenPins`; `OpenExportRoot_MissingRootUnderDelayedFuseMount_IsRejectedBeforeCreation` (FUSE fixture, `[Fact(Timeout = 60_000)]`, asserts nothing created and no request reached the helper within the bound); `OpenExportRoot_MissingRootBeneathSymlinkIntoFuse_IsRejectedWithoutEnteringTheMount` (link below the FUSE root, as in BUG-BASELINE-009 C1); `OpenExportRoot_RootSwappedBetweenCreateAndCapture_FailsClosed` (seam `s_afterLinuxExportRootCreated`).

5.2 D1/D2: `EnsureDirectory_MissingNestedDirectory_IsCreatedAndVerified` (replaces the obsolete `EnsureDirectory_MissingNestedDirectory_FailsClosedBeforeMutation` at `:68-84`, keeping its assertion that nothing appears under the external parent); `EnsureDirectory_ParentMovedOutsideBeforeCreate_RollsBackWithoutExternalArtifact`; `EnsureDirectory_CreatedChildReplacedBeforeRollback_LeavesReplacementIntact` (seam `s_beforeLinuxCreatedDirectoryRollback` swaps the child; the replacement survives and the call throws); `EnsureDirectory_ConcurrentCreate_IsAcceptedWhenVerified` (the create seam makes the directory first); `EnsureDirectory_ExistingNonDirectory_IsRejectedAndKept`.

5.3 D4/D5: `DeleteFileIfExists_NestedStaleFile_DeletesThroughPinnedParent`; `DeleteEmptyDirectory_NestedEmptyDirectory_DeletesThroughPinnedParent`; `DeleteEmptyDirectory_NestedNonEmptyDirectory_ReturnsFalseAndKeepsIt`; `DeleteFileIfExists_NestedMissingEntry_ReturnsFalse` (missing parent and missing target); `DeleteFileIfExists_NestedParentMovedBeforeRename_FailsClosedWithoutExternalDelete`; `DeleteFileIfExists_NestedEntryReplacedBeforeRename_IsRestoredAndRejected` (the replacement survives under its original name); `DeleteFileIfExists_TemporaryNameReplacedBeforeUnlink_DeletesOnlyThePlacedObject` (documents R2: the object the seam placed under the temporary name is the only thing removed; the original stays under the restored name or the call reports it). The obsolete `RequirementsExport_NestedStaleCleanup_IsRejectedBeforeGeneratedWrite` (`:534-565`) becomes `RequirementsExport_NestedStaleCleanup_DeletesStaleAndWritesGenerated`.

5.4 Corrected RED for the two alias-swap races (`RequirementsTwelfthAdversarialReviewTests.cs:112-229`, assertion helper `:257-276`): both use the existing open paths, not D1-D4. The read race throws `DirectoryNotFoundException` (openat2 `ENOENT` while the fixture has moved `wiki`; HResult low word 3, absent from the Linux list) and the write race throws `IOException` with `Win32Exception(17)` (`EEXIST` from the `O_CREAT | O_EXCL` retry in `OpenLinuxContainedFileRooted:234-243` after the name is swapped back). Both are fail-closed outcomes with no external read or write, and the tests already assert the external file is untouched. The helper's Linux branch accepts `DirectoryNotFoundException` and native error 17 in addition to today's list. This is a test correction, recorded as such; no product change.

## 6. Requirements (reconcile, then strengthen)

TR-MCP-USECASE-017 (FR-MCP-USECASE-015; TEST-MCP-USECASE-018) already requires pinned-directory-relative POSIX creation and deletion and has no ACs (live store, 2026-10-11). It gains, by guarded write (refuse if ACs appeared):
- AC-001: On Linux a missing export root and its missing ancestors are created through pinned descriptors after the unbounded-mount policy and without following links into unapproved mounts; a root swapped between creation and capture is rejected; an existing root behaves as before.
- AC-002: On Linux a missing nested export folder is created with mkdirat on a pinned parent and accepted only when re-resolution beneath the root yields the same directory identity; an existing non-directory is rejected and kept; a concurrent creator's directory is accepted only after the same verification; a failed verification removes only the directory the call created, by name-narrowed removal, or reports it.
- AC-003: On Linux nested stale files and empty directories are deleted through a pinned, re-verified parent by renaming the verified entry to a random name in the same directory, re-verifying its identity, and unlinking it; missing entries return false; replaced entries are restored and rejected; a non-empty directory is kept and returns false.
- AC-004: The Linux residual risks R1 and R2 of PLAN-LINUXEXPORTCREATE-001 section 3 are the only external effects possible, and only with a concurrent malicious writer inside the root.
BUG-BASELINE-001 is linked to TR-MCP-USECASE-017 and FR-MCP-USECASE-015 before implementation. No new requirement, mapping, FR, or TEST record.

## 7. Gates

- G1: every test named in section 5, the two harness inventory classes, the 23 RED tests, and `BugTriage139EighteenthLinuxTests` in full.
- G2: all seven unit projects of `./build.ps1 Test` (QBrainAi.Support.Mcp.Tests, Client, Cqrs, Launcher, McpAgent, Repl.Core, QBAgent) with the Test target's filter and the container endpoints loaded: Failed 0, Skipped 0.
- Build.Tests infrastructure suite: unaffected (no build change); not rerun.
- Windows: unchanged code; evidence waits on the GitHub Actions billing lock (PR #78).

## 8. Out of scope

Windows behavior; direct-leaf deletion beneath the root (unchanged); the requirements renderers' logic beyond losing the Linux refusals; any configuration or mechanism beyond D1-D5.

## 9. Round 1 findings and answers

- F1 (operator decision): answered by the decision in section 1; sections 3 and 4 state R1 and R2 exactly, D1 step 2 removes the unprovable rollback, D5 narrows deletion; tests in 5.2 and 5.3.
- F2: D3 step 2 walks component by component from a pinned approved mount with openat2 and no absolute statx; test `OpenExportRoot_MissingRootBeneathSymlinkIntoFuse_IsRejectedWithoutEnteringTheMount`.
- F3: D4 defines `false` versus failure for every missing case and D5 the replacement race.
- F4: mode `0x1FFu`; existing declarations reused; the new declarations are `renameat2` (Storage) and `ResolveBeneath` (Client).
- F5: AC-001..AC-004 with observable outcomes; the TODO link is written before implementation.
- F6: section 5 names a test per branch, replaces both obsolete tests, and settles both races with evidence.
- F7: G2 names all seven unit projects; every failure has a type and message in D1-D5.
- Sandbox note for the reviewer: round 1 saw five extra Twelfth traversal failures caused by two `/tmp` entries in its sandbox's mountinfo; the host has one (`/proc/self/mountinfo`, mount 63, tmpfs), so those are sandbox-only.
