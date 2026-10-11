<!-- SPDX-License-Identifier: Apache-2.0 -->
# PLAN-LINUXEXPORTCREATE-001: Linux contained export creates its folders (BUG-BASELINE-001)

Status: draft for hostile validation, then operator approval. Owner TODO: BUG-BASELINE-001 (PLAN-SESSIONTURNREVIEW-001 P0.1). Worktree `.claude/worktrees/session-20261009`, branch `worktree-session-20261009`.

## 1. Decision being implemented

Operator, 2026-10-11T00:02:53Z, verbatim choice: "Create folders on Linux (Recommended)": parity with Windows; create the export root before capture, nested folders with mkdirat on a pinned parent plus identity verification, stale cleanup with unlinkat on a pinned parent; planned and HV'd as a product change.

## 2. Current behavior (receipts)

- `src/QBrainAi.Storage/WorkspaceContainedFileSystem.ExportRoot.cs:64-72`: Windows `OpenWindowsExportRoot` calls `Directory.CreateDirectory(rootPath)` before pinning.
- Same file `:75-98`: Linux `OpenLinuxExportRoot` pins with `LinuxPhysicalPathResolver.OpenDirectoryNoCrossDevice`; a missing root throws.
- `src/QBrainAi.Storage/WorkspaceContainedFileSystem.BoundedTraversal.cs:263-293`: `EnsureLinuxDirectoryRooted` only opens existing segments (openat2 `RESOLVE_IN_ROOT | RESOLVE_NO_SYMLINKS | RESOLVE_NO_XDEV | RESOLVE_NO_MAGICLINKS`, `:864-908`); its comment records why: "A validate-then-mkdirat sequence lets an attacker move a pinned parent outside the root."
- `WorkspaceContainedFileSystem.ExportRoot.cs:593-602`: Linux `DeleteContainedEntry` refuses any nested path ("Linux requirements export cleanup rejects nested mutation without a kernel-atomic root-relative delete"); leaves directly beneath the root use `unlinkat(rootfd, ...)`.
- Effect (P0.1 baseline, re-measured 2026-10-11 with no class excluded: Failed 23, all this TODO): 18 tests fail because the export root does not exist, 2 because a nested folder does not exist, 1 because nested stale cleanup is refused, 2 `RequirementsTwelfthAdversarialReviewTests` alias-swap races surface `EEXIST`/`DirectoryNotFoundException`, which their accepted-failure list omits.

## 3. Design

Windows code paths are unchanged. Every Linux mutation below is descriptor-relative; no pathname-based policy decision is reused for a later mutation.

### D1. Pinned child-directory creation (one primitive, used by D2 and D3)

`CreateLinuxChildDirectoryPinned(SafeFileHandle anchor, UnixStatx anchorIdentity, SafeFileHandle parent, string parentRelative, string name, string displayPath)` in `WorkspaceContainedFileSystem.BoundedTraversal.cs`:

1. `name` is one validated segment (no `/`, not `.` or `..`; `SplitContainedSegments` rules).
2. `mkdirat(parent, name, 0777)` (the umask applies, as `Directory.CreateDirectory` does on Unix). Result `0` means "created by this call"; `EEXIST` means "already there" (a concurrent creator or an earlier run); any other errno throws `IOException` with the errno.
3. Identity of the entry in the pinned parent: `statx(parent, name, AT_SYMLINK_NOFOLLOW)`; it must be a directory, else throw `IOException` (an `EEXIST` non-directory is never used).
4. Identity through the anchor: open `parentRelative/name` beneath `anchor` with the existing `OpenLinuxPathBeneathRoot` flags (`O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC`, resolve `IN_ROOT | NO_SYMLINKS | NO_XDEV | NO_MAGICLINKS`), `fstat` it, require a directory on the anchor's mount (`EnsureLinuxDescriptorOnRootMount`), and require (dev, ino) equal to step 3.
5. On any failure in steps 3-4 after step 2 returned `0`: roll back with `unlinkat(parent, name, AT_REMOVEDIR)` only when `statx(parent, name, AT_SYMLINK_NOFOLLOW)` still names the inode step 3 observed (it cannot remove a different object; `ENOTEMPTY` leaves it and is reported), then throw `IOException("Requirements export directory creation rejected a parent that left the pinned root: ...")`. An `EEXIST` entry is never removed.
6. Returns the verified child descriptor from step 4 (the caller uses it as the next parent).

Residual risk, accepted by the operator decision: if a writer who controls a directory inside the root moves it outside between the parent open and `mkdirat`, an empty directory owned by the service is created inside that moved directory and then removed by step 5. No pre-existing object outside the root is modified.

### D2. Nested folders (`EnsureLinuxDirectoryRooted`)

Walk segments as today. For each segment, open it beneath the root; on `DirectoryNotFoundException` call D1 with `anchor = parent = rootHandle` for the first level, otherwise `parent` = the verified descriptor of the previous segment and `parentRelative` = the prefix. Existing segments keep today's checks (directory, root mount). The comment at `:271-275` is replaced by a description of D1.

### D3. Export root creation (`OpenLinuxExportRoot`)

Before the current capture:
1. `BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(rootPath, ...)` (unchanged policy; rejects unbounded mounts lexically, before any creation).
2. Find the deepest existing ancestor `A` of `rootPath` by `statx(AT_SYMLINK_NOFOLLOW)` probes (no follow; after BUG-BASELINE-009 C1 the resolver never stats through links), pin it with `LinuxPhysicalPathResolver.OpenDirectoryNoCrossDevice(A)` (mount-table classification plus anchored `RESOLVE_NO_XDEV`), and create each missing component with D1 using `anchor = A` and the accumulated relative path.
3. Then capture as today (`OpenDirectoryNoCrossDevice(rootPath)` and the seam `s_afterLinuxExportRootPolicyClassification`), and require the captured root's (dev, ino) to equal the leaf D1 returned when step 2 created it; a mismatch disposes the capture and throws `IOException` (the path was swapped between creation and capture).
If `rootPath` exists, step 2 is skipped and behavior equals today's.

### D4. Nested stale-file and empty-directory deletion (`DeleteContainedEntry`)

Replace the nested refusal:
1. Open the parent beneath the root (same flags as D2) to a descriptor `P`; verify directory and root mount.
2. Open the target beneath the root with `O_PATH | O_NOFOLLOW` (resolve flags as D2) and `fstat` it; the target identity must equal `statx(P, leaf, AT_SYMLINK_NOFOLLOW)` and the kind must match the call (regular file for `DeleteFileIfExists`, directory for `DeleteEmptyDirectory`).
3. Immediately before mutation, re-open the parent beneath the root and require its (dev, ino) to equal `P`'s; otherwise throw `IOException` without deleting.
4. `unlinkat(P, leaf, directory ? AT_REMOVEDIR : 0)`; `ENOENT` returns `false`, `ENOTEMPTY` (directory) returns `false`, anything else throws.
Residual risk: a parent moved after step 3 makes `unlinkat` remove the already-verified inode from the moved directory; a different object is removed only if the mover also replaced the entry, which requires write access to that directory.

## 4. Requirements (reconcile, then strengthen)

TR-MCP-USECASE-017 already requires "pinned-directory-relative Windows NT and POSIX operations for contained creation, open, replacement, rollback deletion, and empty-directory removal". It gains ACs (none exist today; checked by readback before the write):
- AC-001 Linux export root creation (D3), including rejection under an unbounded mount before any creation.
- AC-002 Linux nested folder creation through a pinned parent with identity verification and rollback (D1, D2).
- AC-003 Linux nested stale-file and empty-directory deletion through a pinned, re-verified parent (D4).
No new requirement; no FR or TEST record changes.

## 5. Tests (Byrd v4: RED before code)

RED baseline: the 23 failing tests listed in BUG-BASELINE-001 (log `supportmcp-full-g2-after-009.log`).
New tests in `tests/QBrainAi.Support.Mcp.Tests/Storage/BugTriage139EighteenthLinuxTests.cs` (Linux native class; harness inventories updated with each addition):
- `OpenExportRoot_MissingRootAndAncestors_CreatesThenPins` (D3).
- `OpenExportRoot_MissingRootUnderDelayedFuseMount_IsRejectedBeforeCreation` (D3 step 1; uses `LinuxDelayedFuseFixture`, `[Fact(Timeout = 60_000)]`).
- `EnsureDirectory_ParentMovedOutsideBeforeCreate_RollsBackWithoutExternalArtifact` (D1 step 5; a new test seam `s_beforeLinuxPinnedDirectoryCreate` moves the pinned parent outside the root).
- `EnsureDirectory_ConcurrentCreate_IsAcceptedWhenVerified` (D1 `EEXIST`; the seam creates the directory first).
- `DeleteFileIfExists_NestedStaleFile_DeletesThroughPinnedParent` (D4).
- `DeleteFileIfExists_NestedParentMovedBeforeUnlink_FailsClosedWithoutExternalDelete` (D4 step 3; seam `s_beforeLinuxNestedUnlink` moves the parent).
The two `RequirementsTwelfthAdversarialReviewTests` races are re-run after D1-D2; their accepted-failure lists change only if the new outcomes are fail-closed `IOException` subtypes raised by D1 or D4 with no external object written or read, recorded with evidence in the RED/GREEN receipts.

## 6. Gates

- G1: the new tests, `BugTriage139EighteenthLinuxTests`, the 23 RED tests, the two harness inventory classes.
- G2: `QBrainAi.Support.Mcp.Tests` (Category!=AiReview&!=Integration, no class excluded, container endpoints loaded): Failed 0, Skipped 0; the other five unit projects green.
- Build.Tests infrastructure suite unaffected (no build change).
- Windows: code paths unchanged; evidence waits on the GitHub Actions billing lock (PR #78).

## 7. Out of scope

Windows behavior; the requirements export renderer's logic (only its Linux refusals disappear); any new requirement, configuration, or mechanism beyond D1-D4.
