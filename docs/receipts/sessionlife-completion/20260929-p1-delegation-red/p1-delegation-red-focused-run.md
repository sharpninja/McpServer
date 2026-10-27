# P1 Delegation Red Focused Run

- Branch: `cursor/sessionlife-p1-delegation-red-edc4`, based on `3b8945b5`.
- SDK: `10.0.401`.
- Command: `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-delegation-red.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-delegation-red/test-results"`.
- Exit code: 1.
- Restore and compile of `_build` and `Build.Tests` succeeded. No CS or NU errors.
- TRX counters: total 95, executed 95, passed 2, failed 93, skipped 0, notExecuted 0.
- TRX SHA-256 `7E673FC33BCD2C905BD4511EB7756E07D0CFC0F14E8448C344EFDBCC68FB3F59`, length 409322. The file is gitignored by `test-results/`.
- Green (2), mock-path delegation only:
  - `ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce`
  - `ValidateSessionLifeUnitGate_RejectsMissingRepeatedCopiedOrWrongTargetCall`
- Red (93), behavior oracles against `SessionLifeUnitGateValidator.Validate` throwing `NotImplementedException`:
  - 88 negative cases expected `InvalidDataException` with the case `reason=` and `affected=` tokens.
  - 5 positive controls expected no exception.
- Fixture setup failures: 0. `InvalidOperationException`: 0.
- `history-r1` still matches `history-manifest.json` SHA-256 `B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4` and all five archived file hashes.
- This run proves exactly-once same-request delegation. It is not validator Green, producer work, orchestration, P1 exit, or TODO closure.
