$ErrorActionPreference = 'Stop'
$obj = [ordered]@{
    TimestampUtc = '2026-08-19T23:32:52Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    Worktree = 'F:\GitHub\McpServer\.worktrees\triage-transcript'
    Branch = 'triage/transcript'
    HeadSha = 'dddcab83f13d579ca358316fd2b2d5e7dbda9133'
    HeadShort = 'dddcab83'
    WorkClass = 'class 1'
    AddProfileExecuted = $true
    ProfileFileCount = 18
    SessionId = 'GrokCode-20260819T232048Z-hostile-g9s4'
    RequestId = 'req-20260819T232048Z-001-hostile-validate-bug-triage-122'
    TurnId = 42130
    PlanFile = 'docs/plans/triage-cluster-002.md'
    TodoId = 'BUG-TRIAGE-122'
    OverallVerdict = 'DISAGREE'
    AccuracyRating = 94
    CompletenessRating = 90
    MarkerSignature = $true
    HealthNonceEchoOk = $true
    HealthNonce = '1d59d6b12c2f4363a1e014e84e199ab3'
    HealthStatus = 'Healthy'
    NamedTests = [ordered]@{
        Filter = 'FullyQualifiedName~CodexTranscriptAdapterCoverageTests'
        Failed = 0
        Passed = 12
        Skipped = 0
        ExitCode = 0
        Trx = 'F:\GitHub\McpServer\.worktrees\triage-transcript\docs\receipts\_hv-g9s4-codex-adapter.trx'
    }
    TodoDone = [ordered]@{
        'BUG-TRIAGE-122' = $false
        'PLAN-TRIAGELEFTOVER-001' = $false
    }
    Counts = [ordered]@{
        PASS = 14
        FAIL = 2
        UNKNOWN = 0
    }
    FailList = @(
        'B2: No S4/G9 test-phase (H-red) hostile AGREE receipt exists before this implementation-exit review. Plan protocol step 4 and hostile-phase-gates require that gate. Single commit dddcab83 adds tests and adapter together; tests are already green. Do not merge. Do not mark BUG-TRIAGE-122 done.',
        'D2: Same root cause. Merge and 122 done:true require OverallVerdict AGREE with empty FAIL list after H-red then H-green. This receipt is DISAGREE.'
    )
    Claims = @(
        [ordered]@{ Id = 'A1'; Surface = 'A'; Verdict = 'PASS'; Text = 'HEAD dddcab83 contains TranscriptAdapters.cs and CodexTranscriptAdapterCoverageTests.cs' }
        [ordered]@{ Id = 'A2'; Surface = 'A'; Verdict = 'PASS'; Text = 'inter_agent skip; tool_search paired; Persist=true deletes importRecovery persisted=true degraded=false' }
        [ordered]@{ Id = 'A3'; Surface = 'A'; Verdict = 'PASS'; Text = 'Named filter Failed 0 Passed 12 Skipped 0 EXIT 0 re-run in worktree' }
        [ordered]@{ Id = 'A4'; Surface = 'A'; Verdict = 'PASS'; Text = 'BUG-TRIAGE-122 and PLAN-TRIAGELEFTOVER-001 still Done=false' }
        [ordered]@{ Id = 'B1'; Surface = 'B'; Verdict = 'PASS'; Text = 'Byrd phase-order not scored from FR createdAt vs file mtimes; S0 H0 AGREE exists' }
        [ordered]@{ Id = 'B2'; Surface = 'B'; Verdict = 'FAIL'; Text = 'Missing S4 H-red hostile AGREE before implementation-exit / merge claim' }
        [ordered]@{ Id = 'B3'; Surface = 'B'; Verdict = 'PASS'; Text = 'MCP-only storage; no TODO.yaml or session-log file edits' }
        [ordered]@{ Id = 'B4'; Surface = 'B'; Verdict = 'PASS'; Text = 'PowerShell only; no Python' }
        [ordered]@{ Id = 'B5'; Surface = 'B'; Verdict = 'PASS'; Text = 'Honesty: stated A claims match artifacts' }
        [ordered]@{ Id = 'B6'; Surface = 'B'; Verdict = 'PASS'; Text = 'add-profile executed; 18 files' }
        [ordered]@{ Id = 'C1'; Surface = 'C'; Verdict = 'PASS'; Text = 'FR/TR/TEST TRANSCRIPT-SEARCH-001 exist in MCP store' }
        [ordered]@{ Id = 'C2'; Surface = 'C'; Verdict = 'PASS'; Text = 'Structured AC nonempty and testable' }
        [ordered]@{ Id = 'C3'; Surface = 'C'; Verdict = 'PASS'; Text = '1:1 mapping FR to TR to TEST' }
        [ordered]@{ Id = 'C4'; Surface = 'C'; Verdict = 'PASS'; Text = 'Three named tests cover the three FR AC' }
        [ordered]@{ Id = 'D1'; Surface = 'D'; Verdict = 'PASS'; Text = 'S4 product DoD: adapter + inline JSONL coverage tests on HEAD' }
        [ordered]@{ Id = 'D2'; Surface = 'D'; Verdict = 'FAIL'; Text = 'H-red then H-green merge protocol not satisfied' }
        [ordered]@{ Id = 'D3'; Surface = 'D'; Verdict = 'PASS'; Text = 'PLAN-TRIAGELEFTOVER-001 remains Done=false' }
    )
    ReceiptMarkdown = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T233252Z.md'
    ReceiptJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T233252Z.json'
}
# Recount PASS/FAIL from Claims (D3 included; B1-B6 + A + C + D)
$pass = @($obj.Claims | Where-Object { $_.Verdict -eq 'PASS' }).Count
$fail = @($obj.Claims | Where-Object { $_.Verdict -eq 'FAIL' }).Count
$unknown = @($obj.Claims | Where-Object { $_.Verdict -eq 'UNKNOWN' }).Count
$obj.Counts.PASS = $pass
$obj.Counts.FAIL = $fail
$obj.Counts.UNKNOWN = $unknown
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T233252Z.json'
($obj | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $path -Encoding utf8
Write-Output ('WROTE ' + $path)
Write-Output ('PASS=' + $pass + ' FAIL=' + $fail + ' UNKNOWN=' + $unknown)
Get-Item -LiteralPath $path | Select-Object FullName, Length, LastWriteTimeUtc | Format-List
