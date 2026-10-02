# BUG-TRIAGE-139 Native Linux Preflight

- Recorded at: `2026-09-06T18:51:28.8585745Z`
- Delegated Codex task: `01a07806-a26e-7c63-a174-32181657d51e`
- Requested execution model: `gpt-5.6-sol`, effort `high`
- PowerShell.Mcp console: isolated subagent `sa-899f0c27`
- Scope: prerequisite diagnosis only; no product code, service restart, distribution shutdown, installation, VHD mutation, or production mutation was performed.

## Result

No currently usable native Linux executor was verified.

The local WSL installation is present, but its distribution-enumeration control path is unresponsive. `wsl.exe --version` completed in 67 ms and reported WSL `2.7.11.0` with kernel `6.18.33.2-2`. A distinct bounded `wsl.exe --list --verbose` probe produced no stdout or stderr and timed out after 10,059 ms. The probe process created by this preflight was terminated after its deadline, and no `wsl` or `ssh` probe process remained.

`WslService`, `vmcompute`, and `hns` all reported `Running`. `WslService` reported status `OK`, PID `5388`, service exit code `0`, and start time `2026-09-03T12:33:12-05:00`. No `wslhost` or `vmmemWSL` process was present. No matching WSL/Lxss/Host Compute error was found in the prior 24 hours of the System or Application event logs.

The registry contains three WSL2 distributions, all reporting registry `State=1` and `DefaultUid=0`: `docker-desktop` on C:, `Pengwin` on E:, and `Ubuntu-24.04` on C:. Their `ext4.vhdx` files exist. No runtime meaning was inferred from the undocumented `State` value. `Get-VHD` inspection was denied by the host authorization policy, so VHD attachment or structural health was not established.

The only configured SSH Linux target is `PAYTON-DESKTOP` at `192.168.1.77:2222`. A batch-mode connection with a five-second connect deadline failed with `Connection timed out`. GitHub workflows declare only `windows-latest`. Azure Pipelines uses pool `Default` without an OS demand, so repository configuration does not prove that a Linux agent is available there.

## Native Test Requirements

The current BUG-TRIAGE-139 candidate requires more than a generic Linux shell:

- `BugTriage139EighteenthLinuxTests.NativeLinuxGate_EffectiveUserId_IsNonRoot` requires Linux and effective UID other than zero.
- `SeparateLocalMountFixture` requires that non-root process to have a writable mount other than `/` whose type is `ext4`, `xfs`, `btrfs`, `overlay`, or `tmpfs`; the completion requirement narrows this to native ext4.
- `LinuxHangingFuseFixture` requires `/dev/fuse`, `/usr/bin/gcc`, `/usr/include/fuse3/fuse.h`, linkable `libfuse3`, and `/usr/bin/fusermount3`.
- `LinuxDelayedFuseFixture` additionally requires `/usr/bin/mount`, `/usr/bin/umount`, `/proc/self/mountinfo`, and permission to execute dynamic bind mounts.
- FIFO tests require the libc `mkfifo` syscall and native Unix file semantics. Permission rollback tests require working `UnixFileMode` behavior.
- The test assembly targets .NET 10. The historical receipt used SDK `10.0.301`, but the current candidate must be built and tested from its exact commit on the selected executor.

One executor identity cannot satisfy the current source contract as an ordinary unprivileged account unless it also has narrowly delegated mount authority: one test asserts non-root effective UID, while delayed-FUSE replacement tests invoke `mount --bind` directly. The completion gate therefore needs native ext4 plus two explicit execution contexts on the same Linux host: a normal non-root context for non-root/FUSE/Unix-permission coverage, and a controlled mount-capable context for bind-mount race cases. Neither context may use a Windows `/mnt/<drive>` filesystem as the workspace or test temp root.

## Root Symptom

The verified failure boundary is the local WSL distribution-enumeration/runtime control path. Static WSL package metadata is readable, but the first service-backed distribution enumeration does not return. Available evidence does not distinguish a wedged `WslService` runtime state from a distribution/VHD enumeration stall. Claiming a specific damaged VHD would be unsupported.

## Minimal Next Action

Approve one elevated restart of `WslService` on `PAYTON-LEGION2`, then immediately rerun a bounded distribution list:

```powershell
Restart-Service -Name WslService -Force
wsl.exe --list --verbose
```

This is the narrowest reversible recovery action. It does not require restarting `vmcompute`: Windows reports no service dependency in either direction. It affects all WSL2 distributions on this host and can interrupt the Docker Desktop WSL backend. No WSL VM process was active at preflight time, but Docker Desktop should still be treated as an affected workload.

The service restart is the approval boundary. If enumeration recovers, inspect `Pengwin` and `Ubuntu-24.04` without installing anything, select an existing non-root user explicitly, and verify native ext4, `/dev/fuse` access, .NET 10, GCC, FUSE3 headers/library, `fusermount3`, and the required controlled bind-mount authority. Missing packages, changing a distribution default user, granting capabilities, or provisioning a new executor requires a separate approved action.

If the service restart does not restore enumeration, stop and collect an elevated `Get-VHD`/WSL diagnostic trace before any VHD repair, move, unregister, or re-import. The configured `PAYTON-DESKTOP` SSH target is an alternative only after port `2222` is reachable and the same capability probe passes.

## Evidence Commands

All commands ran through PowerShell.Mcp 1.14 `execute_command` with `is_subagent=true` and `agent_id=sa-899f0c27`.

```text
wsl.exe --version
  exit=0 duration=67ms WSL=2.7.11.0 kernel=6.18.33.2-2

wsl.exe --list --verbose
  timeout=10059ms stdout=<empty> stderr=<empty>

Get-Service WslService,vmcompute,hns
  WslService=Running vmcompute=Running hns=Running

ssh -T -o BatchMode=yes -o ConnectTimeout=5 PAYTON-DESKTOP ...
  exit=255 error=connect to host 192.168.1.77 port 2222: Connection timed out
```
