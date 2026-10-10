# P1 Behavior Red R2 Focused Run

- Commit under test: `da24e766294bead164c16b786337227b803f806f` on `cursor/sessionlife-p1-behavior-r2-6fe1`.
- SDK: `10.0.401`.
- Command: `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~SessionLifeUnitGate" --logger "trx;LogFileName=p1-behavior-red-r2.trx" --results-directory "docs/receipts/sessionlife-completion/20260929-p1-behavior-red-r2/test-results"`.
- Exit code: 1.
- Restore and compile of `_build` and `Build.Tests` succeeded. No CS or NU errors.
- TRX counters: total 94, executed 94, passed 0, failed 94, skipped 0, notExecuted 0.
- Classification of the 94 failures:
  - 88 negative cases failed because `Validate` threw `NotImplementedException` instead of `InvalidDataException` carrying the case `reason=` and `affected=` tokens.
  - 5 positive controls failed on `Assert.Null` because `Validate` threw `NotImplementedException`.
  - 1 consumer test failed because `Build.ValidateSessionLifeUnitGate` does not delegate to the NSubstitute mock.
- Fixture setup failures: 0. `InvalidOperationException`: 0.
- The TRX is gitignored by `test-results/`. This note is the committed summary of that run.
- The scaffold remains `NotImplementedException` plus an empty Build method. This run is Red evidence for the corrected matrix. It is not validator Green.
