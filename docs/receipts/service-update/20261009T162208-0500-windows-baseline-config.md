# Windows UpdateService baseline configuration gate

Timestamp: 2026-10-09T16:22:08-05:00
Requirements: FR-MCP-SERVICEUPDATE-001, TR-MCP-SERVICEUPDATE-001, TEST-MCP-SERVICEUPDATE-001
Base commit: d9efa0460729bc02f822d69e942f28226aa9ec55

## Observed failure

Nuke UpdateService published and copied version 1.4.40, then the Windows service stopped during startup. Windows Application event 1026 reported that the deployment was missing appsettings.yaml. The install root and retained backup both lacked that file.

## BDPv4 evidence

- Mocks-first contract: 1 passed, 0 failed, 0 skipped.
- Production-bound focused gate: 4 passed, 0 failed, 0 skipped.
- Affected non-Linux service and target gate: 26 passed, 0 failed, 0 skipped.
- Diagnostic Build.Tests run: 339 passed, 13 failed, 0 skipped. All failures are outside this Windows configuration slice. Eight invoke Unix-only primitives or metadata on Windows. Five cover unrelated REPL version parsing, plugin checksums, shipped config validation, and trim-warning inventory.
- git diff --check: clean.

## Implemented behavior

- The Windows publish stage now contains appsettings.yaml before the live service is stopped.
- An explicitly supplied publish-stage appsettings.yaml is preserved.
- A missing stage config is seeded from src/QBrainAi.Support.Mcp/appsettings.yaml.
- A missing repository baseline fails before StopService.
- Existing live configuration remains backed up and restored after installation.

## Source hashes before commit

- build/Build.UpdateService.cs: 046E317E2D4A144540A8E542A79B2F48AEC3CD99EA31BF27D152F0AB0B9242E5
- build/WindowsServiceHelper.cs: DC6E0B58B63E841FD4B51E6FE5BDCEF7B8EDC1DE1111D1E8F450483E765DAAAC
- tests/Build.Tests/BuildTargetTests.cs: C75C6BD861E1326C39E4391881816651636BA0D8A5F003C53F16C2A126D61353
- tests/Build.Tests/ServiceUpdateTests.cs: 56F5534A644BD1C43908E76FED1478C590661772381E8CC6E7E16348BB2608ED

Runtime deployment proof remains pending.
