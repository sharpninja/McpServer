#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.md'
$text = Get-Content -LiteralPath $mdPath -Raw

$text = $text.Replace(
    'Prior C-green-P14: docs/receipts/hostile-validator-20260822T044818Z.md and docs/receipts/hostile-validator-20260822T050748Z.md',
    "Prior C-green-P14: docs/receipts/hostile-validator-20260822T044818Z.md and docs/receipts/hostile-validator-20260822T050748Z.md`r`nPeer C-red-P15 AGREE (parallel spawn, not this session): docs/receipts/hostile-validator-20260822T053539Z.md (OverallVerdict AGREE, independent filter Failed 24 Passed 0 Skipped 0, session GrokSubagentHostile-20260822T052803Z-c-red-p15, completed 2026-08-22T05:38:25Z). Green adapter rewrite LastWriteTimeUtc 2026-08-22T05:40:18Z is after that peer AGREE."
)

$text = $text.Replace(
    'This is green-before-red. C-red-P15 AGREE is not earned. This receipt does not authorize P15 green (green already started without the gate) and does not authorize P16. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. This review wrote no todo_update.',
    'This duplicate C-red review DISAGREE because the current independent filter is Passed 24, not Failed 24. A peer hostile receipt 053539Z already AGREEd C-red with Failed 24 at 05:35:39, before the 05:40:18 green rewrite. This session does not re-AGREE red, does not close PLAN or MCP-PLUGININT-001, and does not authorize P16. This review wrote no todo_update.'
)

$text = $text.Replace(
    'Verdict: FAIL (green-before-red)',
    'Verdict: FAIL (current independent filter is green, not 24-fail red)'
)

$text = $text.Replace(
    'Evidence: OverallVerdict DISAGREE. P15 green already started at 05:40:18 without C-red-P15 AGREE. This receipt does not authorize P15 green or P16 and does not close PLAN or MCP-PLUGININT-001.',
    'Evidence: OverallVerdict DISAGREE for this session. Current independent filter is Passed 24. Peer 053539Z already AGREEd C-red at 05:35:39. This duplicate review does not re-AGREE red and does not authorize P16 or close PLAN/PLUGININT.'
)

$text = $text.Replace(
    @'
#### B1. Honesty / no fabricated results.

Verdict: FAIL

Evidence: Parent dispatched C-red-P15 claiming the filter is currently red (Passed 0). Independent first run against 05:15:12 sources was red-ish (22 fail then crash). During this review the implementer rewrote adapter/tests at 05:40:18 and the current independent filter is Passed 24. Result.cs still comments C-red-P15 stays false while the adapter now sets FailsafePathVerified true. That is a green slice mixed into a red gate.

#### B2. Byrd v4 phase-order at this inter-phase gate.

Verdict: FAIL

Evidence: Plan order is C-red-P15 AGREE, then P15 green, then C-green-P15 AGREE. Green adapter implementation landed 05:40:18 while this C-red review was open and before any C-red-P15 AGREE. This is not a post-hoc FR createdAt vs mtime FAIL. It is the inter-phase order defect this gate exists to catch.
'@,
    @'
#### B1. Honesty / no fabricated results.

Verdict: PASS

Evidence: At dispatch (~05:29) and first read (05:15:12 sources) the named theories failed closed. Peer 053539Z independently recorded Failed 24 Passed 0 (WMI PID 71784, TRX 05:33:04Z) and AGREEd C-red. Green rewrite 05:40:18 is after that peer AGREE. Result.cs comment still says C-red-P15 stays false; that is stale documentation, recorded as residual, not a fabricated test count. This validator does not treat the 053539Z AGREE as this session's verdict.

#### B2. Byrd v4 phase-order at this inter-phase gate.

Verdict: PASS

Evidence: Plan order is C-red-P15 AGREE, then P15 green. Peer receipt docs/receipts/hostile-validator-20260822T053539Z.md OverallVerdict AGREE at 05:35:39Z with Failed 24. Adapter/tests LastWriteTimeUtc 05:40:18Z is after that AGREE. This review does not FAIL B2 from FR createdAt versus file mtimes. This duplicate C-red spawn cannot re-score the already-passed red gate as still red.
'@
)

$text = $text.Replace(
    @'
#### D3. Green implementation without C-red-P15 AGREE.

Verdict: FAIL

Evidence: Adapter/tests LastWriteTimeUtc 2026-08-22T05:40:18Z during this red review. Independent filter now Passed 24. Plan forbids mixing red and green into one hostile gate.
'@,
    @'
#### D3. Green implementation without C-red-P15 AGREE.

Verdict: PASS

Evidence: Peer C-red-P15 AGREE docs/receipts/hostile-validator-20260822T053539Z.md exists at 05:35:39Z with Failed 24. Green rewrite 05:40:18Z follows that AGREE. This duplicate review still DISAGREE on the assigned current-red claim because run3 is Passed 24. Combined PLAN task stays done false; next required gate is C-green-P15, not another C-red AGREE.
'@
)

$text = $text.Replace(
    @'
## Explicit FAIL list

- A3: current adapter no longer throws not-implemented; FailsafePathVerified is set true.
- A4: independent P15 filter is Passed 24 Failed 0 Skipped 0 (green-before-red). First isolated run was 22 fail then crash, not a complete 24-fail red TRX.
- A8: FailedSubmit still does not assert root-id filename; failsafe AC was greened during the red gate.
- A9: C-red-P15 AGREE not earned; does not authorize P15 green or P16.
- B1: honesty: green rewrite during a claimed-red hostile gate; Result.cs comment stale.
- B2: Byrd phase-order: P15 green before C-red-P15 AGREE.
- C3: red-gate AC coverage broken by green mix; root-id filename still unasserted.
- D3: plan order C-red AGREE then green was violated.

## Explicit FAIL count

FAIL 8. UNKNOWN 0.
'@,
    @'
## Explicit FAIL list

- A3: current adapter no longer throws not-implemented; FailsafePathVerified is set true.
- A4: independent P15 filter for this session is Passed 24 Failed 0 Skipped 0. First isolated run was 22 fail then crash, not a complete 24-fail red TRX.
- A8: FailedSubmit still does not assert a root-id filename after the green rewrite.
- A9: this session does not AGREE C-red (current filter is green). Does not authorize P16.
- C3: green tests still omit root-id filename on FailedSubmit.

## Explicit FAIL count

FAIL 5. UNKNOWN 0.
'@
)

$text = $text.Replace(
    'Accuracy: 96. Green-before-red is pinned to LastWriteTimeUtc 05:40:18, first-run 22-fail TRX, and run3 Passed 24 TRX.',
    'Accuracy: 96. Current filter Passed 24 is pinned to run3 TRX. Peer 053539Z AGREE plus 05:40:18 green rewrite are timestamped. First isolated run 22-fail crash is recorded.'
)

if ($text.Contains([char]0x2014) -or $text.Contains([char]0x2013)) { throw 'dash found' }
Set-Content -LiteralPath $mdPath -Value $text -Encoding utf8

# Patch JSON via deserialize/mutate/serialize
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.json'
$j = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$j.FailCount = 5
$j.PassCount = 20
$j.ExplicitFailList = @(
    'A3: current adapter no longer throws not-implemented; FailsafePathVerified is set true'
    'A4: independent P15 filter Passed 24 Failed 0 Skipped 0; first isolated run 22 fail then crash'
    'A8: FailedSubmit still does not assert root-id filename after the green rewrite'
    'A9: this session does not AGREE C-red (current filter is green); does not authorize P16'
    'C3: green tests still omit root-id filename on FailedSubmit'
)
if ($j.PSObject.Properties.Name -notcontains 'PeerCRedAgree') {
    $j | Add-Member -NotePropertyName PeerCRedAgree -NotePropertyValue 'docs/receipts/hostile-validator-20260822T053539Z.md'
}
foreach ($c in $j.Claims) {
    if ($c.Id -eq 'A4') { $c.Text = 'Independent filter currently Passed 24 Failed 0; not 24-fail red for this session' }
    if ($c.Id -eq 'A9') { $c.Text = 'This session DISAGREE; current filter is green; does not authorize P16'; $c.Verdict = 'FAIL' }
    if ($c.Id -eq 'B1') { $c.Verdict = 'PASS'; $c.Text = 'No fabricated counts; peer 053539Z AGREEd red then green rewrite 05:40:18' }
    if ($c.Id -eq 'B2') { $c.Verdict = 'PASS'; $c.Text = 'Byrd phase-order: peer C-red AGREE 053539Z at 05:35:39 precedes green 05:40:18' }
    if ($c.Id -eq 'D3') { $c.Verdict = 'PASS'; $c.Text = 'Green after peer C-red AGREE 053539Z; this duplicate C-red still DISAGREE on current-red' }
}
$j | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'CORRECTED'
Write-Output ('MD_AGREE=' + [bool]((Get-Content $mdPath -Raw) -match '(?m)^OverallVerdict:\s*AGREE\s*$'))
Write-Output ('MD_DISAGREE=' + [bool]((Get-Content $mdPath -Raw) -match '(?m)^OverallVerdict:\s*DISAGREE\s*$'))
Write-Output ('JSON_FAIL=' + ((Get-Content $jsonPath -Raw | ConvertFrom-Json).FailCount))
