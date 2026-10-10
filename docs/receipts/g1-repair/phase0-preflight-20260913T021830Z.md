# G1 Phase 0 C10 executor preflight

Recorded at: 2026-09-13T02:18:30Z
Host: PAYTON-LEGION2
Console: PowerShell.Mcp Baltic #69712
Operator execution approval: 2026-09-12 (stop waiting after plan AGREE)

## Windows host probes

- `C:\WINDOWS\system32\wsl.exe --version`: TimedOut=false, ExitCode=0, DurationMs=36
  - WSL version: 2.7.11.0
  - Kernel: 6.18.33.2-2
- `wsl.exe --list --verbose`: TimedOut=false, ExitCode=0, DurationMs=37
  - `wsl -l -q` names: Pengwin, docker-desktop, Ubuntu-24.04
  - Default display name decoded as Pengwin (UTF-16 list previously looked like Penguin)
  - Pengwin `-d Penguin` is WSL_E_DISTRO_NOT_FOUND; do not use that name
  - docker-desktop Running; not a C10 executor
  - Ubuntu-24.04 Stopped then started by probe; used as executor
- Services: WslService=Running/Automatic, vmcompute=Running/Manual, hns=Running/Manual
- Processes present: vmmemWSL pid=68384; wslhost pids 32240, 61812, 67592, 69080
- No WslService restart performed

This is not the 2026-09-06 hang class. List succeeded. Native execution is not fail-closed on WSL enumeration.

## Ubuntu-24.04 executor route

Exact host launch: `wsl.exe -d Ubuntu-24.04 -u bug139 --cd /home/bug139 -- <linux argv>`

Harmless probe (`id`, writable home, `dotnet --info`): ExitCode=0

- Distro: Ubuntu-24.04
- User: bug139 uid=1000 gid=1000 (non-root). Default distro user without `-u` is root uid=0; non-root route must pass `-u bug139`
- Working directory: `/home/bug139` on ext4 `/dev/sdf` (never `/mnt/<drive>`)
- HOME write probe: ok
- git: `/usr/bin/git` version 2.43.0
- dotnet: `/opt/bug139-dotnet10/dotnet` SDK 10.0.301 linux-x64
- gcc: `/usr/bin/gcc`
- fusermount3: `/usr/bin/fusermount3`
- mount/umount: present
- `/dev/fuse`: present
- libfuse3-dev: install ok
- `/usr/include/fuse3`: fuse.h and related headers present
- `/proc/self/mountinfo`: readable
- Existing root-owned trees under `/opt/bug139-fifteenth` and `/opt/bug139-sixteenth` are historical; C10 must use a fresh native-ext4 checkout under `/home/bug139` after G1-Repair freeze

## Original workspace snapshot (not mutated)

- Path: `F:\GitHub\McpServer`
- Branch: develop
- HEAD: `08eaf2a506a0aa2db89766e6a547d9ae1c85f681`
- TREE: `40ae058c5bac675820a6b9dc020c7f7aa06839e9`
- Dirty overlay plus QuadBrain left in place. No reset, clean, stash, or revert.

## Isolated G1-Repair worktree

Command: `git worktree add -b grok/g1-repair-20260911 C:\Users\kingd\AppData\Local\Temp\McpServer-G1-Repair-20260911 808ec049d56daf214c391beccd6b9d69bb9867c6`

- WORKTREE_EXIT=0
- HEAD: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- TREE: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- Four Linux C10 class files present
- ProcessRunner.Windows.cs absent on this candidate (port from develop is Phase 1/Task 6)

## HV jsonl copy

Copied 20260911T17* and 20260911T18* `g1-plan-review-astra*.jsonl` from `%USERPROFILE%\.grok\hv-receipts\McpServer` to `docs/receipts/hv/`.
