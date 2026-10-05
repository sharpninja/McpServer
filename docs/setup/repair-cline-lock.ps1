<# .SYNOPSIS Reconciles one stale local lock checksum to its Git-verified pinned archive and validates the plugin. #>
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$true
$repo='/home/sharpninja/github/mcpserver-cline-plugin'
Set-Location $repo
if ((& git rev-parse HEAD) -ne '4ded634073f9108f8a0c442801f21d9d4532a3eb') { throw 'Unexpected source pin.' }
if (& git status --porcelain -- package-lock.json vendor/sharpninja-mcpserver-plugin-core.tgz) { throw 'Dependency inputs have local changes; refusing to overwrite.' }
$expectedBlob=(& git rev-parse HEAD:vendor/sharpninja-mcpserver-plugin-core.tgz).Trim()
$actualBlob=(& git hash-object vendor/sharpninja-mcpserver-plugin-core.tgz).Trim()
if ($expectedBlob -ne $actualBlob) { throw 'Archive differs from approved Git commit.' }
$path=Join-Path $repo 'package-lock.json'
$raw=Get-Content $path -Raw
$lock=$raw | ConvertFrom-Json -AsHashtable
$entry=$lock.packages['node_modules/@qbrainai/qbrain-ai-plugin-core']
$old='sha512-bW5Zurhsm+MxFWw8vA5GCVLcNMGCIPC91aV0+kG3E6I77kSIPe4tyGVeGo9u8oi7lvQyY2JjjWJCAfP7NV6VRw=='
if ($entry.integrity -cne $old -or $entry.resolved -ne 'file:vendor/sharpninja-mcpserver-plugin-core.tgz') { throw 'Unexpected lock entry.' }
$bytes=[IO.File]::ReadAllBytes((Join-Path $repo 'vendor/sharpninja-mcpserver-plugin-core.tgz'))
$new='sha512-'+[Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData($bytes))
$encodedOld=[Text.Json.JsonSerializer]::Serialize[string]($old,[Text.Json.JsonSerializerOptions]::Default)
$encodedNew=[Text.Json.JsonSerializer]::Serialize[string]($new,[Text.Json.JsonSerializerOptions]::Default)
if ([regex]::Matches($raw,[regex]::Escape($encodedOld)).Count -ne 1) { throw 'Expected exactly one stale checksum.' }
Copy-Item $path /tmp/mcpserver-setup/cline-package-lock.before.json
# This JSON edit changes exactly the verified integrity field; dependency versions stay fixed.
$updated=$raw.Replace($encodedOld,$encodedNew)
[IO.File]::WriteAllText($path,$updated,[Text.UTF8Encoding]::new($false))
& git diff -- package-lock.json
& npm ci
& npm run build
& npm test -- --runInBand --json --outputFile=/tmp/mcpserver-setup/cline-test-results.json
$results=Get-Content /tmp/mcpserver-setup/cline-test-results.json -Raw | ConvertFrom-Json
if (-not $results.success -or $results.numFailedTests -ne 0 -or $results.numPendingTests -ne 0 -or $results.numTodoTests -ne 0) { throw 'Plugin test gate requires zero failures and zero skips/todos.' }
[pscustomobject]@{GitBlob=$expectedBlob;NewIntegrity=$new;Tests=$results.numPassedTests;Failed=$results.numFailedTests;Skipped=$results.numPendingTests} | ConvertTo-Json -Compress
