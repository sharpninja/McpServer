<!-- SPDX-License-Identifier: Apache-2.0 -->
# PLAN-LINUXEXPORTCREATE-001: Linux contained export creates its folders (BUG-BASELINE-001)

Status: revision 3, implemented test-first (operator: "Implement landlock with failback."); reviewed with the implementation in the batched hostile validation. Owner TODO: BUG-BASELINE-001 (PLAN-SESSIONTURNREVIEW-001 P0.1). Branch `worktree-session-20261009`.

## 1. Decisions

1. 2026-10-11T00:02:53Z, verbatim: "Create folders on Linux (Recommended)": parity with Windows for export root and nested folder creation and nested stale cleanup.
2. After plan rounds 1 and 2 (docs/receipts/hv/hostile-validator-linuxexportcreate-r1-20261011T115738Z.md, DISAGREE 84/72; r2-20261011T123336Z, DISAGREE 87/82) showed that Linux has no kernel-atomic create or delete beneath a pinned root, so any pinned-parent mkdirat or unlinkat can act outside the root when a parent is moved, the operator chose, verbatim: "Implement landlock with failback." This supersedes the earlier "Accept stated residuals" contract (revision 2): no residual external effect is accepted.

## 2. Contract

- Windows: unchanged.
- Linux with Landlock (the Landlock LSM is enabled and reports ABI 1 or later): every directory creation and every nested deletion of a contained export runs on a dedicated thread that confines itself with Landlock to the pinned root, or, when the root itself is missing, to its pinned deepest existing ancestor. The kernel denies `mkdirat`, `unlinkat`, renames, writes, and truncation outside that hierarchy at the moment of the call, so a parent moved outside is refused (`EACCES`, surfaced as `IOException`) and no object outside is created, modified, or deleted.
- Linux without Landlock: the behavior before this change, fail closed: missing roots and nested folders throw `DirectoryNotFoundException`, nested deletion throws `NotSupportedException`, and stale cleanup refuses nested stale paths before mutating anything.

## 3. Design (as implemented)

- `src/QBrainAi.Storage/WorkspaceContainedFileSystem.Landlock.cs` (new): `GetLandlockAbiVersion()` (syscall 444 with `LANDLOCK_CREATE_RULESET_VERSION`; 0 when unavailable; test seam `s_landlockAbiVersionOverride`); `RunLandlockConfined<T>(hierarchy, operation)`: a new thread builds a ruleset handling write, remove (file and directory), every make-node right, and, by ABI, refer (2+) and truncate (3+); adds one path-beneath rule on the pinned hierarchy descriptor allowing exactly those rights; sets `PR_SET_NO_NEW_PRIVS`; calls `landlock_restrict_self`; runs the operation; exits. Reads are not handled, so confined code still resolves paths. The hierarchy handle is reference-counted for the thread's life. Any confinement step failing throws `IOException` before the operation runs. Test seam `s_beforeLinuxConfinedMutation` runs on the confined thread before each mutating call.
- Nested folders (`EnsureLinuxDirectoryRooted`): existing segments are opened as before; at the first missing segment, `CreateLinuxDirectoriesConfined` creates the rest of the chain under confinement, each with `mkdirat(pinned parent, name, 0x1FFu)` (`EEXIST` accepted), then re-opens the child beneath the anchor with the existing contained openat2 flags and requires a directory on the anchor's mount before it becomes the next parent.
- Export root (`OpenLinuxExportRoot`): if capture finds the root missing and Landlock is available, `LinuxPhysicalPathResolver.OpenDeepestExistingDirectoryNoCrossDevice` classifies and pins the approved mount and walks the mount-relative components with openat2 (`RESOLVE_NO_SYMLINKS | RESOLVE_NO_XDEV | RESOLVE_NO_MAGICLINKS`), stopping at the first `ENOENT`; the missing tail is created under confinement to that ancestor; then the root is captured exactly as an existing root.
- Nested deletion (`DeleteContainedEntry`): under confinement to the root, the parent and the target are opened beneath the root (missing either returns `false`), the target's kind must match the call and its identity must equal `statx(parent, leaf, AT_SYMLINK_NOFOLLOW)`, then `unlinkat(parent, leaf, ...)`; `ENOENT` and (for directories) `ENOTEMPTY` return `false`.
- `WorkspaceExportRoot.SupportsNestedDeletion` (public): true on Windows, and on Linux when Landlock is available; the requirements export writer's stale cleanup refuses nested stale paths only when it is false.
- Linux traversal parity: an export directory that does not exist yet enumerates as empty (as the Windows traversal does for a missing path); a path that disappears mid-traversal keeps failing closed.
- Found while testing: `docs/wiki.yaml` still named `docs/McpServer-UseCase-Extension-Design-v3.0.md`, which 6de989f3 renamed to `docs/QBrainAi-UseCase-Extension-Design-v3.0.md`; the source was updated through the YAML object-mutation helper (CRLF preserved).

## 4. Tests

In `BugTriage139EighteenthLinuxTests` (harness inventories updated: 23 in that class, 35 Linux native in total): `EnsureDirectory_MissingNestedDirectory_IsCreatedUnderLandlock`, `EnsureDirectory_ParentMovedOutsideBeforeCreate_IsDeniedByLandlock`, `DeleteFileIfExists_NestedStaleFile_DeletesUnderLandlock`, `DeleteFileIfExists_NestedParentMovedOutsideBeforeUnlink_IsDeniedByLandlock`, `OpenExportRoot_MissingRootAndAncestors_IsCreatedUnderLandlock`, `OpenExportRoot_MissingRoot_FailsClosedWithoutLandlock`, `LandlockConfinement_DeniesDirectoryCreationOutsideItsHierarchy` (the kernel, not path checks, refuses an absolute-path create outside), `RequirementsExport_NestedStaleCleanup_DeletesStaleUnderLandlock`; the two former refusal tests keep their names and now assert the fallback with Landlock reported unavailable. Moved-parent seams move the parent on another thread, because the confined thread itself may not mutate outside the root.

Corrected RED: the two `RequirementsTwelfthAdversarialReviewTests` alias-swap races fail in existing open paths with fail-closed outcomes their Linux list omitted (`DirectoryNotFoundException`, low word 3, while the fixture has moved the directory; `EEXIST` 17 from the `O_CREAT | O_EXCL` retry after a swap back); the list now accepts both, and both tests still assert the external file is untouched.

## 5. Requirements

TR-MCP-USECASE-017 (FR-MCP-USECASE-015; TEST-MCP-USECASE-018), which already requires pinned-directory-relative POSIX creation and deletion and had no ACs, gains AC-001 (Landlock-confined creation of missing root and nested folders), AC-002 (Landlock-confined nested deletion with identity and kind checks and false for missing entries), and AC-003 (fail-closed fallback without Landlock). BUG-BASELINE-001 links TR-MCP-USECASE-017 and FR-MCP-USECASE-015.

## 6. Gates

G1: the BUG-BASELINE-001 classes, the alias-race class, both harness inventories (126 tests). G2: the seven unit projects of `./build.ps1 Test` with the container endpoints loaded. Windows evidence waits on the GitHub Actions billing lock (PR #78). Hosts without Landlock run the fallback path; the native Linux gate requires Landlock.
