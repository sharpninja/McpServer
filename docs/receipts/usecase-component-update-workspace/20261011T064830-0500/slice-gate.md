# UC-125 component-update workspace resolution receipt

Timestamp: 2026-10-11 06:55:00 -05:00

## Mocks-first gate

- Tests: `UseCaseComponentUpdateWorkspaceResolutionContractTests&MockGate_`
- Failed: 0
- Passed: 4
- Skipped: 0
- TRX SHA-256: `F9E5C9B695A28978A2CE72E7922135E7F16830F7ECDB01A1319BA0FB9F44E34E`
- Pre-implementation test source SHA-256: `C3BF0A6F394FE71B53DF3ABC4DB8B1E1CA8941B9F65D9B76F144706472CC4415`

## Production and focused gates

- Production contract: 5 passed, 0 failed, 0 skipped.
- Component, controller, and MCP tool slice: 15 passed, 0 failed, 0 skipped.
- Production change: the actor, flow, and step update handlers now resolve the workspace through the canonical async resolver before querying filtered entities.

## Full affected suite

- Total: 3006
- Passed: 2994
- Failed: 12
- Skipped: 0
- Full-suite TRX SHA-256: `9A85EEAACB3DA9E660CD3DB493B4E934C219E0DD76B00EA450C58320F7B08BBC`
- Eleven failure names exactly match the previously captured untouched `origin/develop` baseline.
- One additional full-run failure was `SqlServerRenameQuadBrainRolesMigrationTests.Migrate_RenameQuadBrainRoles_RenamesLegacyHemisphereRows`.
- The additional failure passed when rerun individually: 1 passed, 0 failed, 0 skipped.

The full suite is not green. Advancing the gate requires explicit operator agreement to treat the eleven proven baseline failures as an exception.


## 2026-10-11 full-suite correction

- Corrected failure set: 26 passed, 1 failed, 0 skipped. The remaining failure identified an omitted immutable evidence file.
- Final evidence test individual v2: 1 passed, 0 failed, 0 skipped. TRX SHA-256: `584975FAD4F3A5960100A66C4D33E5E718CB18C28BD67221678F6EB0C5F78A60`.
- First corrected full run: 3005 passed, 1 failed, 0 skipped. The only failure was a SQL Server migration timeout.
- SQL Server audit-payload migration individual: 1 passed, 0 failed, 0 skipped. TRX SHA-256: `FFF239DC5E6E38AC48B69E6D39E90529D1F449572D96C6BA5EF336568895B92A`.
- Remaining batch excluding that exact test: 3008 passed, 0 failed, 0 skipped. TRX SHA-256: `65F465B7CF74B73C18F6354AF25FA891C308AECF7D5BF972488BEFBA30C70570`.
- The full current test inventory is green across the required individual failure rerun and remaining-suite batch. No failure or skip exception is used.
