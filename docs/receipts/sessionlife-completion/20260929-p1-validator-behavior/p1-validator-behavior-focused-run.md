# P1 Validator Behavior Focused Run

- Branch: `cursor/sessionlife-p1-validator-behavior-2908`, based on `d60379cba8413eb1307b175c5b728fde37fd18ef`.
- SDK: `10.0.401`.
- Command: `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-validator-behavior.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-validator-behavior/test-results"`.
- Exit code: 0.
- Restore and compile of `_build` and `Build.Tests` succeeded. No CS or NU errors.
- TRX counters: total 95, executed 95, passed 95, failed 0, skipped 0, notExecuted 0.
- TRX SHA-256 `3678F724D7DD5FAD85404296AB579154B843BC91E700771E23268F2654617665`, length 133623. The file is gitignored by `test-results/`.
- Green (95):
  - 2 consumer facts still prove exactly-once same-request delegation.
  - 5 positive controls accept, including the nested `.mcpServer` ancestor.
  - 88 negative controls throw `InvalidDataException` with the case `reason=` and `affected=` tokens.
- Fixture setup failures: 0.
- `history-r1/history-manifest.json` SHA-256 remains `B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4`, and all five archived file hashes still match.
- This run is validator behavior for the accepted fixtures. It is not producer work, orchestration, P1 exit, TODO closure, or P2.
