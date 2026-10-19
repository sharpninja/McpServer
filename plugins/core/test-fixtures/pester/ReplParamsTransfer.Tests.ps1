#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# Params never cross a process boundary on the command line (triage-report-821fe2da58284b4ea21604cb2da2fccb).
# On Windows a child command line is limited to 32,767 characters; Invoke-McpPlugin.ps1 passed the whole params
# YAML to repl-invoke.ps1 as -ParamsYaml, so a 194 KB workflow.requirements.updateBatch failed with
# "The filename or extension is too long". Contract:
#   C1 a launcher hands params to repl-invoke.ps1 through a file (-ParamsPath), never as argument text;
#   C2 the file carries the params text exactly (UTF-8, no BOM, CRLF and non-ASCII preserved) and is removed after use;
#   C3 empty params produce no -ParamsPath argument and no file;
#   C4 repl-invoke.ps1 -ParamsPath delivers the same params to the REPL as -ParamsYaml would, at any size;
#   C5 the wrapper (Invoke-McpPlugin.ps1 -Command Invoke -ParamsPath) carries a payload above 32,767 characters;
#   C6 no synced core file launches repl-invoke.ps1 with -ParamsYaml on a child command line (static lint);
#   C7 repl-invoke.ps1 rejects -ParamsYaml together with -ParamsPath, and a missing params file.
# BDPv4: Tag MockGate proves each assertion accepts a correct mock and rejects every corrupt mock before any
# production code exists; Tag Binding runs the same assertions against the real helper, script and wrapper.

BeforeAll {
    $script:CoreRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    . (Join-Path $PSScriptRoot '..\PluginTestFixtures.ps1')
    $script:Work = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-params-transfer-' + [guid]::NewGuid().ToString('N'))
    [void][System.IO.Directory]::CreateDirectory($script:Work)
    $script:Utf8 = [System.Text.UTF8Encoding]::new($false)
    $script:MaxArgumentText = 4096

    # A payload above the Windows limit, with CRLF, LF, quotes, a YAML-significant colon and non-ASCII text.
    $chunk = "line: ""quoted"" value`r`nsecond line`nunicode: " + [string][char]0x00E9 + [char]0x4E2D + " end`n"
    $script:BigText = -join (1..([int][Math]::Ceiling(200000 / $chunk.Length)) | ForEach-Object { $chunk })

    function Assert-NoParamsOnCommandLine {
        param([object[]]$Arguments, [string]$ParamsText)
        $args2 = @($Arguments | ForEach-Object { [string]$_ })
        if ($args2 -contains '-ParamsYaml') { throw 'C1: -ParamsYaml passed on the command line' }
        foreach ($a in $args2) {
            if ($a.Length -gt $script:MaxArgumentText) { throw "C1: argument of $($a.Length) characters on the command line" }
            if ($ParamsText.Length -gt 0 -and $a.Contains($ParamsText)) { throw 'C1: params text found in an argument' }
        }
        if ($ParamsText.Length -gt 0) {
            $i = [Array]::IndexOf($args2, '-ParamsPath')
            if ($i -lt 0 -or $i -ge $args2.Count - 1) { throw 'C1: no -ParamsPath argument for non-empty params' }
        }
    }

    function Assert-ParamsFileRoundTrip {
        param([string]$Path, [string]$Expected)
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "C2: params file missing: $Path" }
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw 'C2: params file has a BOM' }
        $want = $script:Utf8.GetBytes($Expected)
        if ($bytes.Length -ne $want.Length) { throw "C2: params file has $($bytes.Length) bytes, expected $($want.Length)" }
        for ($i = 0; $i -lt $want.Length; $i++) { if ($bytes[$i] -ne $want[$i]) { throw "C2: params file differs at byte $i" } }
    }

    function Assert-TransferRemoved {
        param([string]$Path)
        if ($Path -and (Test-Path -LiteralPath $Path)) { throw "C2: params file not removed: $Path" }
    }

    function Assert-EmptyTransfer {
        param($Transfer)
        if (@($Transfer.Arguments).Count -ne 0) { throw 'C3: arguments produced for empty params' }
        if ($Transfer.Path) { throw 'C3: params file produced for empty params' }
    }

    function Assert-EnvelopeCarry {
        param([string]$EnvelopeLog, [string]$Key, [string]$Expected)
        if (-not (Test-Path -LiteralPath $EnvelopeLog)) { throw 'C4: the REPL received no envelope' }
        $text = [System.IO.File]::ReadAllText($EnvelopeLog, $script:Utf8).Trim()
        $env = $text | ConvertFrom-Json -Depth 30
        $params = $env.payload.params
        if ($null -eq $params -or -not $params.PSObject.Properties[$Key]) { throw "C4: envelope params lack '$Key'" }
        if ([string]$params.$Key -cne $Expected) { throw "C4: envelope '$Key' differs (got $(([string]$params.$Key).Length) characters, want $($Expected.Length))" }
    }

    function Get-ParamsCommandLineLaunch {
        <# C6: lines that put -ParamsYaml into a child process argument list. #>
        param([string[]]$Lines)
        $hits = [System.Collections.Generic.List[string]]::new()
        for ($i = 0; $i -lt $Lines.Count; $i++) {
            $l = $Lines[$i]
            if ($l -notmatch '-ParamsYaml') { continue }
            if ($l -match 'ArgumentList\.Add|-Arguments\s+@\(|foreach\s*\(\s*\$\w+\s+in\s+@\(.*''-File''|Start-Process') { $hits.Add("$($i + 1): $($l.Trim())") }
        }
        return $hits.ToArray()
    }

    # Fake qbrain-ai-repl: logs the stdin envelope and answers with a result.
    $fakeBody = @(
        '[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false); [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)'
        '$stdin = [Console]::In.ReadToEnd(); if ($stdin.Length -gt 0 -and [int][char]$stdin[0] -eq 0xFEFF) { $stdin = $stdin.Substring(1) }'
        'if ($env:PT_REPL_LOG) { [System.IO.File]::WriteAllText($env:PT_REPL_LOG, $stdin, [System.Text.UTF8Encoding]::new($false)) }'
        '[Console]::Out.Write((@(''type: result'', ''payload:'', ''  result:'', ''    ok: true'') -join [Environment]::NewLine) + [Environment]::NewLine)'
        'exit 0'
    ) -join [Environment]::NewLine
    $script:FakeRepl = Write-TestPwshExecutable -Directory (Join-Path $script:Work 'bin') -Name 'fake-repl' -Body $fakeBody

    function ConvertTo-ParamsYaml {
        param([string]$Key, [string]$Value)
        if (-not (Get-Command ConvertTo-Yaml -ErrorAction SilentlyContinue)) { Import-Module powershell-yaml -ErrorAction Stop }
        return (ConvertTo-Yaml -Data ([ordered]@{ $Key = $Value }))
    }

    function Invoke-ChildPwsh {
        param([string]$Script, [string[]]$Arguments, [string]$Name)
        $root = Join-Path $script:Work $Name
        $ws = Join-Path $root 'ws'; $cache = Join-Path $root 'cache'; $fs = Join-Path $root 'failsafe'
        foreach ($d in $ws, $cache, $fs) { [void][System.IO.Directory]::CreateDirectory($d) }
        $log = Join-Path $root 'repl-envelope.json'
        $psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Command pwsh -ErrorAction Stop).Source)
        foreach ($a in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $Script) + $Arguments) { $psi.ArgumentList.Add($a) }
        $psi.WorkingDirectory = $ws
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        foreach ($n in @('MCP_PLUGIN_REPL_LOG', 'MCP_PLUGIN_REPL_RESPONSE', 'MCP_PLUGIN_PERSIST_LOG', 'MCP_HOOK_DEADLINE_UTC_MS')) { [void]$psi.Environment.Remove($n) }
        $psi.Environment['MCP_CACHE_DIR_OVERRIDE'] = $cache
        $psi.Environment['MCPSERVER_FAILSAFE_DIR'] = $fs
        $psi.Environment['MCP_WORKSPACE_PATH'] = $ws
        $psi.Environment['MCPSERVER_WORKSPACE_PATH'] = $ws
        $psi.Environment['MCP_PLUGIN_HOST'] = 'claude-code'
        $psi.Environment['MCP_AGENT_NAME'] = 'ClaudeCode'
        $psi.Environment['MCP_REPL_EXECUTABLE'] = $script:FakeRepl
        $psi.Environment['MCP_FAILSAFE_DRAIN_DISABLED'] = '1'
        $psi.Environment['PT_REPL_LOG'] = $log
        $p = [System.Diagnostics.Process]::Start($psi)
        $o = $p.StandardOutput.ReadToEndAsync(); $e = $p.StandardError.ReadToEndAsync()
        if (-not $p.WaitForExit(120000)) { try { $p.Kill($true) } catch { Write-Verbose "kill failed: $_" }; throw "timed out: $Name" }
        [pscustomobject]@{ ExitCode = $p.ExitCode; Stdout = $o.Result; Stderr = $e.Result; EnvelopeLog = $log }
    }
}

AfterAll {
    if ($script:Work -and (Test-Path -LiteralPath $script:Work)) { Remove-Item -LiteralPath $script:Work -Recurse -Force -ErrorAction SilentlyContinue }
}

Describe 'Params transfer assertions discriminate (mock gate)' -Tag MockGate {
    BeforeAll {
        $script:MockOk = {
            param([string]$ParamsText)
            if ([string]::IsNullOrEmpty($ParamsText)) { return [pscustomobject]@{ Arguments = @(); Path = $null } }
            $p = Join-Path $script:Work ('mock-' + [guid]::NewGuid().ToString('N') + '.yaml')
            [System.IO.File]::WriteAllText($p, $ParamsText, $script:Utf8)
            [pscustomobject]@{ Arguments = @('-ParamsPath', $p); Path = $p }
        }
    }
    It 'C1 accepts a -ParamsPath transfer' { $t = & $script:MockOk $script:BigText; { Assert-NoParamsOnCommandLine $t.Arguments $script:BigText } | Should -Not -Throw }
    It 'C1 rejects -ParamsYaml with the text' { { Assert-NoParamsOnCommandLine @('-ParamsYaml', $script:BigText) $script:BigText } | Should -Throw '*C1*' }
    It 'C1 rejects the text under another switch name' { { Assert-NoParamsOnCommandLine @('-Params', $script:BigText) $script:BigText } | Should -Throw '*C1*' }
    It 'C1 rejects a short payload passed inline' { { Assert-NoParamsOnCommandLine @('-Params', 'k: v') 'k: v' } | Should -Throw '*C1*' }
    It 'C1 rejects non-empty params without -ParamsPath' { { Assert-NoParamsOnCommandLine @('-Method', 'm') 'k: v' } | Should -Throw '*C1*' }
    It 'C2 accepts an exact file' { $t = & $script:MockOk $script:BigText; { Assert-ParamsFileRoundTrip $t.Path $script:BigText } | Should -Not -Throw }
    It 'C2 rejects a BOM' { $p = Join-Path $script:Work 'bom.yaml'; [System.IO.File]::WriteAllText($p, $script:BigText, [System.Text.UTF8Encoding]::new($true)); { Assert-ParamsFileRoundTrip $p $script:BigText } | Should -Throw '*C2*' }
    It 'C2 rejects normalized line endings' { $p = Join-Path $script:Work 'lf.yaml'; [System.IO.File]::WriteAllText($p, ($script:BigText -replace "`r`n", "`n"), $script:Utf8); { Assert-ParamsFileRoundTrip $p $script:BigText } | Should -Throw '*C2*' }
    It 'C2 rejects truncation' { $p = Join-Path $script:Work 'trunc.yaml'; [System.IO.File]::WriteAllText($p, $script:BigText.Substring(0, 32767), $script:Utf8); { Assert-ParamsFileRoundTrip $p $script:BigText } | Should -Throw '*C2*' }
    It 'C2 rejects a lossy encoding' { $p = Join-Path $script:Work 'ascii.yaml'; [System.IO.File]::WriteAllText($p, $script:BigText, [System.Text.Encoding]::ASCII); { Assert-ParamsFileRoundTrip $p $script:BigText } | Should -Throw '*C2*' }
    It 'C2 rejects a leftover file' { $t = & $script:MockOk 'k: v'; { Assert-TransferRemoved $t.Path } | Should -Throw '*C2*' }
    It 'C2 accepts a removed file' { $t = & $script:MockOk 'k: v'; Remove-Item -LiteralPath $t.Path; { Assert-TransferRemoved $t.Path } | Should -Not -Throw }
    It 'C3 accepts no transfer for empty params' { { Assert-EmptyTransfer (& $script:MockOk '') } | Should -Not -Throw }
    It 'C3 rejects a file for empty params' { { Assert-EmptyTransfer ([pscustomobject]@{ Arguments = @('-ParamsPath', 'x'); Path = 'x' }) } | Should -Throw '*C3*' }
    It 'C4 accepts an envelope carrying the value' {
        $log = Join-Path $script:Work 'env-ok.json'; [System.IO.File]::WriteAllText($log, (@{ payload = @{ params = @{ blob = $script:BigText } } } | ConvertTo-Json -Depth 5), $script:Utf8)
        { Assert-EnvelopeCarry $log 'blob' $script:BigText } | Should -Not -Throw
    }
    It 'C4 rejects a truncated envelope value' {
        $log = Join-Path $script:Work 'env-bad.json'; [System.IO.File]::WriteAllText($log, (@{ payload = @{ params = @{ blob = $script:BigText.Substring(0, 1000) } } } | ConvertTo-Json -Depth 5), $script:Utf8)
        { Assert-EnvelopeCarry $log 'blob' $script:BigText } | Should -Throw '*C4*'
    }
    It 'C4 rejects a missing envelope' { { Assert-EnvelopeCarry (Join-Path $script:Work 'none.json') 'blob' 'x' } | Should -Throw '*C4*' }
    It 'C6 lint finds <Name>' -ForEach @(
        @{ Name = 'wrapper -Arguments'; Line = "Invoke-PluginPowerShellScript -ScriptPath `$x -Arguments @('-Method', `$m, '-ParamsYaml', (`$t ?? ''))" }
        @{ Name = 'ArgumentList loop'; Line = "foreach (`$argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', `$repl, '-Method', `$Method, '-ParamsYaml', `$ParamsYaml)) {" }
        @{ Name = 'ArgumentList.Add'; Line = "`$psi.ArgumentList.Add('-ParamsYaml')" }
    ) { $h = @(Get-ParamsCommandLineLaunch -Lines @('x', $Line)); $h.Count | Should -Be 1; $h[0] | Should -BeLike '2: *' }
    It 'C6 lint ignores in-process calls and file transfer' {
        @(Get-ParamsCommandLineLaunch -Lines @("& `$repl -Method `$m -ParamsYaml `$y", "Invoke-ReplRaw -Method `$m -ParamsYaml `$y", "-Arguments @('-Method', `$m) + `$t.Arguments")).Count | Should -Be 0
    }
}

Describe 'Params transfer bound to the real core (binding)' -Tag Binding {
    BeforeAll {
        $script:Staged = Write-TestStagedPlugin -RepoRoot $script:CoreRepoRoot -Root (Join-Path $script:Work 'staged')
        . (Join-Path $script:Staged 'lib/repl-params-transfer.ps1')
        $script:Repl = Join-Path $script:Staged 'lib/repl-invoke.ps1'
        $script:Wrapper = Join-Path $script:Staged 'lib/Invoke-McpPlugin.ps1'
    }
    It 'C1 C2 the helper writes an exact file and keeps the text off the command line' {
        $t = New-McpReplParamsTransfer -ParamsText $script:BigText
        try {
            Assert-NoParamsOnCommandLine $t.Arguments $script:BigText
            Assert-ParamsFileRoundTrip $t.Path $script:BigText
        }
        finally { Remove-McpReplParamsTransfer -Transfer $t }
        Assert-TransferRemoved $t.Path
    }
    It 'C3 empty params produce no transfer' { Assert-EmptyTransfer (New-McpReplParamsTransfer -ParamsText '') }
    It 'C4 repl-invoke.ps1 -ParamsPath delivers a 200 KB value through a real process' {
        $yaml = ConvertTo-ParamsYaml -Key 'blob' -Value $script:BigText
        $file = Join-Path $script:Work 'c4.yaml'; [System.IO.File]::WriteAllText($file, $yaml, $script:Utf8)
        $r = Invoke-ChildPwsh -Script $script:Repl -Arguments @('-Method', 'client.Health.GetAsync', '-ParamsPath', $file) -Name 'c4'
        $r.ExitCode | Should -Be 0 -Because $r.Stderr
        # Expected value: what the YAML library parses from the params text (YAML normalizes CRLF in scalars), the same value -ParamsYaml would deliver.
        Assert-EnvelopeCarry $r.EnvelopeLog 'blob' ([string](ConvertFrom-Yaml $yaml).blob)
    }
    It 'C4 -ParamsPath and -ParamsYaml deliver the same params' {
        $yaml = ConvertTo-ParamsYaml -Key 'small' -Value 'same value: "x"'
        $file = Join-Path $script:Work 'c4b.yaml'; [System.IO.File]::WriteAllText($file, $yaml, $script:Utf8)
        $a = Invoke-ChildPwsh -Script $script:Repl -Arguments @('-Method', 'client.Health.GetAsync', '-ParamsPath', $file) -Name 'c4-path'
        $b = Invoke-ChildPwsh -Script $script:Repl -Arguments @('-Method', 'client.Health.GetAsync', '-ParamsYaml', $yaml) -Name 'c4-inline'
        $a.ExitCode | Should -Be 0; $b.ExitCode | Should -Be 0
        Assert-EnvelopeCarry $a.EnvelopeLog 'small' 'same value: "x"'
        Assert-EnvelopeCarry $b.EnvelopeLog 'small' 'same value: "x"'
    }
    It 'C5 the wrapper carries a payload above 32,767 characters' {
        $yaml = ConvertTo-ParamsYaml -Key 'blob' -Value $script:BigText
        $yaml.Length | Should -BeGreaterThan 32767
        $file = Join-Path $script:Work 'c5.yaml'; [System.IO.File]::WriteAllText($file, $yaml, $script:Utf8)
        $r = Invoke-ChildPwsh -Script $script:Wrapper -Arguments @('-Command', 'Invoke', '-Method', 'client.Health.GetAsync', '-ParamsPath', $file, '-PluginRoot', $script:Staged) -Name 'c5'
        $r.ExitCode | Should -Be 0 -Because ($r.Stderr + $r.Stdout)
        Assert-EnvelopeCarry $r.EnvelopeLog 'blob' ([string](ConvertFrom-Yaml $yaml).blob)
        @(Get-ChildItem -LiteralPath ([System.IO.Path]::GetTempPath()) -Filter 'mcp-repl-params-*' -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-2) }).Count | Should -Be 0 -Because 'the wrapper removes its params file'
    }
    It 'C6 no synced core file launches repl-invoke.ps1 with -ParamsYaml on a command line' {
        $hits = foreach ($f in Get-ChildItem -LiteralPath (Join-Path $script:Staged 'lib') -Recurse -File -Include '*.ps1', '*.psm1') {
            foreach ($h in (Get-ParamsCommandLineLaunch -Lines ([System.IO.File]::ReadAllLines($f.FullName)))) { "$($f.Name):$h" }
        }
        @($hits) | Should -BeNullOrEmpty
    }
    It 'C7 repl-invoke.ps1 rejects both params sources' {
        $file = Join-Path $script:Work 'c7.yaml'; [System.IO.File]::WriteAllText($file, 'a: 1', $script:Utf8)
        $r = Invoke-ChildPwsh -Script $script:Repl -Arguments @('-Method', 'client.Health.GetAsync', '-ParamsPath', $file, '-ParamsYaml', 'a: 1') -Name 'c7-both'
        $r.ExitCode | Should -Not -Be 0
        $r.Stderr | Should -Match 'ParamsPath'
    }
    It 'C7 repl-invoke.ps1 rejects a missing params file' {
        $r = Invoke-ChildPwsh -Script $script:Repl -Arguments @('-Method', 'client.Health.GetAsync', '-ParamsPath', (Join-Path $script:Work 'missing.yaml')) -Name 'c7-missing'
        $r.ExitCode | Should -Not -Be 0
        $r.Stderr | Should -Match 'ParamsPath'
    }
}
