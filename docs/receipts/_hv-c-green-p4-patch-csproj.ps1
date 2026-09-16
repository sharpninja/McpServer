#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p4-tests.ps1'
$t = Get-Content -LiteralPath $p -Raw
$old = '<IsPackable>false</IsPackable>'
$new = @"
<IsPackable>false</IsPackable>
    <IsTestProject>false</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>`$(NoWarn);1591;CS1591</NoWarn>
"@
if (-not $t.Contains($old)) { throw 'NO_MATCH' }
# Only the first occurrence is inside the generated csproj here-string.
$idx = $t.IndexOf($old)
$t2 = $t.Substring(0, $idx) + $new + $t.Substring($idx + $old.Length)
Set-Content -LiteralPath $p -Value $t2 -Encoding utf8
Write-Output 'PATCHED'
