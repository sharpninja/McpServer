<!-- SPDX-License-Identifier: Apache-2.0 -->
# BUG-BUILD-007 root cause: evidence and experiments

This note answers HV round 1 finding F-B7. The reviewer judged the live evidence log insufficient to show that the worker node holding
the test's pipes was spawned by the test's child build, or to explain why the hang was intermittent. Three bounded experiments below
settle both questions. They also refine the root cause.

## The live hang (`bug-build-007-evidence.log.txt`)

- The Build.Tests process (PID 2575389) used 0 s of CPU and had no children.
- It held the read ends of two pipes, `pipe:[16191191]` and `pipe:[16191192]`.
- The write ends were fd 1 and fd 2 of MSBuild worker node 2575659 (`/nodemode:1 /nodeReuse:true`). That node's parent was PID
  1082, the user `systemd --user`. The node's fd 0 was the test process's own stdin.
- So all three of the node's standard streams came from the test unredirected.

**The timing values in that log cannot be compared with each other.** The script read the test-process line first. It then scanned
every `/proc/<pid>/fd` on the host for about five minutes, and only then read the node line. That gap is why the node shows 729 s
against the test's 425 s; the round-1 reading that the node predates the test came from it. Two observations contradict that reading:

- **PID order.** The node's PID (2575659) is above the test's PID (2575389). `pid_max` on this host is 4194304, and the two PIDs are
  270 apart.
- **Same-instant snapshot.** One `ps` run earlier in the session read the node at 225 s and the test host at 233 s.

The experiments below prove provenance without relying on either of these.

## Experiment 1: plain shell (`bug-build-007-experiment1.log.txt`)

Three builds of `src/QBrainAi.McpAgent` into fresh artifacts paths, launched from a plain PowerShell process. Each build ran with
redirected stdout and stderr, as the pre-fix helpers did, and with a unique `MSBUILDNODEHANDSHAKESALT` so that only this run's nodes
were counted.

- **spawn.** A node was spawned by the build child: its parent PID equals the child's PID, read while the child was running. Its
  fd 1 and fd 2 were its own pipes, not the shell's, and the output closed at once.
- **reuse.** The idle node from the spawn case was reused and no new node started. The output closed at once.
- **control (`--disable-build-servers`).** A node was spawned and exited with the build. The output closed at once.

The script's summary line reports "salt-B nodes left after exit: 1". That line is wrong: the function's progress lines leaked into
its return value. The detail lines show `after exit: []` and `now alive False`, and cleanup stopped only the salt-A node.

Conclusion: launched from a plain shell, MSBuild redirects a worker node's standard streams. Pipe inheritance does not happen.

## Experiment 2: the `dotnet test` environment (`bug-build-007-experiment2.log.txt`)

**probe.** During a short `dotnet test --no-build` run of Build.Tests, the script read `MSBUILDENSURESTDOUTFORTASKPROCESSES` from each
process in the test tree:

- `dotnet test` itself: unset.
- `vstest.console`, the test host, and the xUnit v3 test executable: `=1`.

Every `dotnet build` or `dotnet publish` that a Build.Tests case starts inherits that value. One unrelated `dotnet test` process
elsewhere on the host also matched the probe; its command line is omitted from the log.

**ensure** (`MSBUILDENSURESTDOUTFORTASKPROCESSES=1`). A node was spawned by the build child, and its fd 1 and fd 2 were the shell's
own pipes. The output was still open 30 s after the build exited. This reproduces the hang.

**plain** (variable unset). The spawned node had its own redirected streams, and the output closed at once.

## Experiment 3: the shipped fix (`bug-build-007-experiment3.log.txt`)

**ensure-fixed** (`MSBUILDENSURESTDOUTFORTASKPROCESSES=1` plus `--disable-build-servers`). The node spawned by the child held the
shell's pipes while the build ran, then exited with the build. The output closed at once and no node was left running.

## Root cause, refined

1. `dotnet test` sets `MSBUILDENSURESTDOUTFORTASKPROCESSES=1` for the test process tree. Every build a Build.Tests case starts
   inherits it.
2. With that variable set, MSBuild starts worker nodes without redirecting their standard streams. A node spawned by such a build
   therefore inherits the build's redirected stdout and stderr (experiment 2).
3. Node reuse keeps that node alive after the build ends, so the test's wait for end of stream never finishes.
4. When an idle node with a matching handshake already exists, the build reuses it and spawns nothing (experiment 1). That is why the
   hang was intermittent.

The fix, `--disable-build-servers` on every dotnet child, turns node reuse off. The node then exits with the build and the pipes close
(experiment 3); the full Build.Tests runs on 2026-10-10 completed in the default environment. BoundedProcess additionally bounds the
wait if anything else holds the pipes.
