#Requires -Version 7.0

BeforeDiscovery {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    $script:LegacyRoot = Join-Path $script:RepoRoot 'plugins\core\test-fixtures\legacy-bats'
    $script:LegacyScenarios = @(
        foreach ($file in Get-ChildItem -LiteralPath $script:LegacyRoot -Filter '*.bats' -File | Sort-Object Name) {
            $text = [System.IO.File]::ReadAllText($file.FullName)
            $matches = [regex]::Matches($text, '(?ms)^@test\s+"(?<name>[^"]+)"\s+\{(?<body>.*?)(?=^@test\s+"|\z)')
            foreach ($match in $matches) {
                [pscustomobject]@{
                    File = $file.Name
                    Name = $match.Groups['name'].Value
                    Body = $match.Groups['body'].Value.Trim()
                    LegacyLine = ($text.Substring(0, $match.Index) -split "`n").Count
                }
            }
        }
    )
}

BeforeAll {
    $script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
    $script:CoreRoot = Join-Path $script:RepoRoot 'plugins\core'
    $script:LibRoot = Join-Path $script:CoreRoot 'lib-ps'
    $script:StagedRoot = Join-Path $script:CoreRoot '.staged-plugin'
    $script:SmokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'mcp-plugin-legacy-pester'

    function New-SmokeRoot {
        $root = Join-Path $script:SmokeRoot ([guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($root)
        return $root
    }

    function Invoke-PluginChildProcess {
        param(
            [Parameter(Mandatory)][string]$ScriptPath,
            [string[]]$Arguments = @(),
            [hashtable]$Environment = @{},
            [string]$InputText = ''
        )

        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        $psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source
        $psi.ArgumentList.Add('-NoLogo')
        $psi.ArgumentList.Add('-NoProfile')
        $psi.ArgumentList.Add('-NonInteractive')
        $psi.ArgumentList.Add('-File')
        $psi.ArgumentList.Add($ScriptPath)
        foreach ($argument in $Arguments) { $psi.ArgumentList.Add($argument) }
        $psi.WorkingDirectory = $script:RepoRoot
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        foreach ($key in $Environment.Keys) { $psi.Environment[$key] = [string]$Environment[$key] }

        $process = [System.Diagnostics.Process]::Start($psi)
        if ($InputText) { $process.StandardInput.Write($InputText) }
        $process.StandardInput.Close()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit(45000) | Should -BeTrue

        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = $stdout.Result.Trim()
            Stderr = $stderr.Result.Trim()
        }
    }

    function Test-PowerShellParse {
        param([Parameter(Mandatory)][string]$Path)

        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors) | Out-Null
        $errors | Should -BeNullOrEmpty
    }

    function Get-WrapperPathFromScenario {
        param([Parameter(Mandatory)][string]$Name)

        if ($Name -match '^(?<script>[a-z-]+)\.sh ') {
            $scriptName = $Matches['script']
            return Join-Path $script:StagedRoot "hooks\scripts\$scriptName.ps1"
        }

        if ($Name -match '(?<script>cache-manager|marker-resolver)\.sh ') {
            return Join-Path $script:LibRoot "$($Matches['script']).ps1"
        }

        return $null
    }

    function Assert-ScriptShapeScenario {
        param([Parameter(Mandatory)]$Scenario)

        $path = Get-WrapperPathFromScenario -Name $Scenario.Name
        $path | Should -Not -BeNullOrEmpty
        Test-Path -LiteralPath $path | Should -BeTrue

        if ($Scenario.Name -like '*syntactically valid bash*') {
            Test-PowerShellParse -Path $path
            return
        }

        if ($Scenario.Name -like '*has a shebang*') {
            [System.IO.File]::ReadAllText($path) | Should -Match '#Requires -Version 7\.0|pwsh'
            return
        }

        if ($Scenario.Name -like '*is executable*') {
            $path | Should -Match '\.ps1$'
            Test-Path -LiteralPath $path -PathType Leaf | Should -BeTrue
            return
        }

        throw "Unhandled script-shape scenario: $($Scenario.Name)"
    }

    function Assert-CacheScenario {
        param([Parameter(Mandatory)]$Scenario)

        $root = New-SmokeRoot
        $env:MCP_CACHE_DIR_OVERRIDE = Join-Path $root 'cache'
        try {
            switch -Wildcard ($Scenario.Name) {
                '*creates a YAML file*' {
                    $result = & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'sessionlog.addTurn' -ParamsYaml 'sessionId: test-123'
                    Test-Path -LiteralPath $result | Should -BeTrue
                    $result.Replace('\', '/') | Should -Match '/cache/pending/'
                    $result | Should -Match '\.yaml$'
                }
                '*monotonic sequence*' {
                    $files = @(
                        & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'method.one' -ParamsYaml 'key: val1'
                        & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'method.two' -ParamsYaml 'key: val2'
                        & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'method.three' -ParamsYaml 'key: val3'
                    )
                    (Split-Path $files[0] -Leaf) | Should -Match '^001-'
                    (Split-Path $files[1] -Leaf) | Should -Match '^002-'
                    (Split-Path $files[2] -Leaf) | Should -Match '^003-'
                }
                '*stores method, params, and timestamp*' {
                    $file = & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method 'todo.create' -ParamsYaml "title: Buy milk`npriority: high"
                    $content = [System.IO.File]::ReadAllText($file)
                    $content | Should -Match '(?m)^method: todo\.create'
                    $content | Should -Match 'title: Buy milk'
                    $content | Should -Match 'priority: high'
                    $content | Should -Match '(?m)^timestamp:'
                    $content | Should -Match '(?m)^retryCount: 0'
                }
                '*returns 0 when no pending*' {
                    & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action status | Should -Be '0'
                }
                '*returns correct count*' {
                    1..3 | ForEach-Object { & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action write -Method "m$_" -ParamsYaml "p: $_" | Out-Null }
                    & (Join-Path $script:LibRoot 'cache-manager.ps1') -Action status | Should -Be '3'
                }
                default {
                    Assert-CacheFlushScenario -Scenario $Scenario -Root $root
                }
            }
        } finally {
            Remove-Item Env:\MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function Assert-CacheFlushScenario {
        param(
            [Parameter(Mandatory)]$Scenario,
            [Parameter(Mandatory)][string]$Root
        )

        $libCopy = Join-Path $Root 'lib'
        [void][System.IO.Directory]::CreateDirectory($libCopy)
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'cache-manager.ps1') -Destination $libCopy
        Copy-Item -LiteralPath (Join-Path $script:LibRoot 'resolve-cache-dir.ps1') -Destination $libCopy

        $log = Join-Path $Root 'replay.log'
        $failScript = "param([string]`$Method)`nAdd-Content -LiteralPath '$($log.Replace("'", "''"))' -Value `$Method`nif (`$Method -like 'fail*' -or `$Method -like 'doomed*') { throw 'fail' }`n"
        [System.IO.File]::WriteAllText((Join-Path $libCopy 'repl-invoke.ps1'), $failScript, [System.Text.UTF8Encoding]::new($false))

        switch -Wildcard ($Scenario.Name) {
            '*removes items from pending*' {
                & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'ok.method' -ParamsYaml 'data: yes' | Out-Null
                & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'ok.method2' -ParamsYaml 'data: also' | Out-Null
                & (Join-Path $libCopy 'cache-manager.ps1') -Action flush | Should -Be 'flushed=2 failed=0 pending=0'
                & (Join-Path $libCopy 'cache-manager.ps1') -Action status | Should -Be '0'
            }
            '*increments retryCount*' {
                $file = & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'fail.method' -ParamsYaml 'data: nope'
                & (Join-Path $libCopy 'cache-manager.ps1') -Action flush | Out-Null
                [System.IO.File]::ReadAllText($file) | Should -Match 'retryCount: 1'
                & (Join-Path $libCopy 'cache-manager.ps1') -Action flush | Out-Null
                [System.IO.File]::ReadAllText($file) | Should -Match 'retryCount: 2'
            }
            '*skips items with retryCount >= 3*' {
                $file = & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method 'doomed.method' -ParamsYaml 'data: hopeless'
                [System.IO.File]::WriteAllText($file, ([System.IO.File]::ReadAllText($file) -replace 'retryCount: 0', 'retryCount: 3'))
                & (Join-Path $libCopy 'cache-manager.ps1') -Action flush | Should -Be 'flushed=0 failed=0 pending=1'
                [System.IO.File]::ReadAllText($file) | Should -Match 'retryCount: 3'
            }
            '*processes items in order*' {
                foreach ($method in 'first.method','second.method','third.method') {
                    & (Join-Path $libCopy 'cache-manager.ps1') -Action write -Method $method -ParamsYaml 'seq: 1' | Out-Null
                }
                & (Join-Path $libCopy 'cache-manager.ps1') -Action flush | Out-Null
                [System.IO.File]::ReadAllLines($log) | Should -Be @('first.method', 'second.method', 'third.method')
            }
            '*syntactically valid bash*' {
                Test-PowerShellParse -Path (Join-Path $script:LibRoot 'cache-manager.ps1')
            }
            default {
                throw "Unhandled cache scenario: $($Scenario.Name)"
            }
        }
    }

    function Assert-MarkerScenario {
        param([Parameter(Mandatory)]$Scenario)

        . (Join-Path $script:LibRoot 'marker-resolver.ps1')
        $root = New-SmokeRoot
        $child = Join-Path $root 'sub\deep'
        [void][System.IO.Directory]::CreateDirectory($child)
        $marker = Join-Path $root 'AGENTS-README-FIRST.yaml'
        [System.IO.File]::WriteAllText($marker, @'
port: 7147
baseUrl: http://testhost:7147
apiKey: test-api-key-12345
workspacePath: /tmp/test-workspace
endpoints:
  sessionLog: /mcpserver/sessionlog
'@)
        try {
            switch -Wildcard ($Scenario.Name) {
                '*current directory*' { Find-MarkerFile -StartDir $root | Should -Be $marker }
                '*walks up*' { Find-MarkerFile -StartDir $child | Should -Be $marker }
                '*returns exit 1*' {
                    $empty = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
                    [void][System.IO.Directory]::CreateDirectory($empty)
                    try {
                        { Find-MarkerFile -StartDir $empty -MaxDepth 2 } | Should -Throw
                    } finally {
                        [System.IO.Directory]::Delete($empty, $true)
                    }
                }
                '*baseUrl*' { Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl' | Should -Be 'http://testhost:7147' }
                '*apiKey*' { Get-MarkerField -MarkerFile $marker -FieldName 'apiKey' | Should -Be 'test-api-key-12345' }
                '*workspacePath*' { Get-MarkerField -MarkerFile $marker -FieldName 'workspacePath' | Should -Be '/tmp/test-workspace' }
                '*syntactically valid bash*' { Test-PowerShellParse -Path (Join-Path $script:LibRoot 'marker-resolver.ps1') }
                default { throw "Unhandled marker scenario: $($Scenario.Name)" }
            }
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function Assert-SyncScenario {
        param([Parameter(Mandatory)]$Scenario)

        $root = New-SmokeRoot
        $plugin = Join-Path $root 'plugin'
        [void][System.IO.Directory]::CreateDirectory($plugin)
        try {
            switch -Wildcard ($Scenario.Name) {
                '*copies lib-sh*' {
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    Test-Path -LiteralPath (Join-Path $plugin 'lib\plugin-hook.ps1') | Should -BeTrue
                    Test-Path -LiteralPath (Join-Path $plugin 'CORE-MANIFEST.yaml') | Should -BeTrue
                    [System.IO.File]::ReadAllText((Join-Path $plugin 'CORE-MANIFEST.yaml')) | Should -Match '(?m)^\s+lib/plugin-hook\.ps1: [0-9a-f]{64}'
                }
                '*passes immediately*' {
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    (& (Join-Path $script:CoreRoot 'sync\check-core-integrity.ps1') -PluginRoot $plugin) -join "`n" | Should -Match 'core integrity OK'
                }
                '*edited locally*' {
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    Add-Content -LiteralPath (Join-Path $plugin 'lib\plugin-hook.ps1') -Value '# local edit'
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:CoreRoot 'sync\check-core-integrity.ps1') -Arguments @('-PluginRoot', $plugin)
                    $result.ExitCode | Should -Be 1
                    ($result.Stdout + $result.Stderr) | Should -Match 'MODIFIED: lib/plugin-hook.ps1'
                }
                '*deleted*' {
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    Remove-Item -LiteralPath (Join-Path $plugin 'lib\plugin-hook.ps1') -Force
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:CoreRoot 'sync\check-core-integrity.ps1') -Arguments @('-PluginRoot', $plugin)
                    $result.ExitCode | Should -Be 1
                    ($result.Stdout + $result.Stderr) | Should -Match 'MISSING: lib/plugin-hook.ps1'
                }
                '*demands a manifest*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:CoreRoot 'sync\check-core-integrity.ps1') -Arguments @('-PluginRoot', $plugin)
                    $result.ExitCode | Should -Be 1
                    ($result.Stdout + $result.Stderr) | Should -Match 'no CORE-MANIFEST.yaml'
                }
                '*re-sync repairs*' {
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    Add-Content -LiteralPath (Join-Path $plugin 'lib\plugin-hook.ps1') -Value '# local edit'
                    & (Join-Path $script:CoreRoot 'sync\sync-plugin-core.ps1') -PluginRoot $plugin | Out-Null
                    (& (Join-Path $script:CoreRoot 'sync\check-core-integrity.ps1') -PluginRoot $plugin) -join "`n" | Should -Match 'core integrity OK'
                }
                default { throw "Unhandled sync scenario: $($Scenario.Name)" }
            }
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function Assert-HookScenario {
        param([Parameter(Mandatory)]$Scenario)

        if ($Scenario.Name -like '*syntactically valid bash*' -or $Scenario.Name -like '*has a shebang*' -or $Scenario.Name -like '*is executable*') {
            Assert-ScriptShapeScenario -Scenario $Scenario
            return
        }

        if ($Scenario.Name -like 'hooks.json*') {
            $hooks = [System.IO.File]::ReadAllText((Join-Path $script:CoreRoot 'hooks-templates\hooks.claude-code.json')) | ConvertFrom-Json
            $json = $hooks | ConvertTo-Json -Depth 20
            switch -Wildcard ($Scenario.Name) {
                '*valid JSON*' { $hooks | Should -Not -BeNullOrEmpty }
                '*SessionStart*' { $json | Should -Match 'SessionStart' }
                '*SessionEnd*' { $json | Should -Match 'SessionEnd' }
                '*PreCompact*' { $json | Should -Match 'PreCompact' }
                '*PostCompact*' { $json | Should -Match 'PostCompact' }
                '*PostToolUse*' { $json | Should -Match 'PostToolUse' }
                default { throw "Unhandled hooks.json scenario: $($Scenario.Name)" }
            }
            return
        }

        $root = New-SmokeRoot
        $envBase = @{
            MCP_PLUGIN_ROOT = $script:StagedRoot
            MCP_PLUGIN_HOST = 'claude-code'
            PLUGIN_ROOT_OVERRIDE = $root
            MCP_WORKSPACE_PATH = $script:RepoRoot
            MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            MCP_PLUGIN_REPL_LOG = (Join-Path $root 'repl.log')
            MCP_PLUGIN_REPL_RESPONSE = 'id: TODO-FEAT-001'
        }
        try {
            switch -Wildcard ($Scenario.Name) {
                'session-start*successful*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'session-start') -Environment $envBase
                    $result.Stdout | Should -Be '{}'
                    Test-Path -LiteralPath (Join-Path $root 'cache\session-state.yaml') | Should -BeTrue
                }
                'session-start*MCP_UNTRUSTED*' {
                    $envBase.MCP_WORKSPACE_PATH = $root
                    $envBase.MCPSERVER_WORKSPACE_PATH = $root
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'session-start', '-WorkspacePath', $root) -Environment $envBase
                    $result.Stdout | Should -Be '{}'
                    [System.IO.File]::ReadAllText((Join-Path $root 'cache\session-state.yaml')) | Should -Match 'MCP_UNTRUSTED'
                }
                'session-end*cache_flush' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'session-end') -Environment $envBase
                    $result.Stdout | Should -Be '{}'
                }
                'pre-compact*flushes cache*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'pre-compact') -Environment $envBase
                    $result.Stdout | Should -Be '{}'
                }
                'post-compact*schema-valid*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'post-compact') -Environment $envBase
                    $result.Stdout | Should -Be '{}'
                }
                'plan-approved*extracts title*' {
                    $plan = Join-Path $root 'my-plan.md'
                    [System.IO.File]::WriteAllText($plan, "# Implement User Authentication`n`nBody")
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-approved') -Environment ($envBase + @{ TOOL_INPUT = $plan })
                    $result.Stdout | Should -Match 'PostToolUse'
                    [System.IO.File]::ReadAllText($envBase.MCP_PLUGIN_REPL_LOG) | Should -Match 'Implement User Authentication'
                }
                'plan-approved*writes to plan-todo-map*' {
                    $plan = Join-Path $root 'feature-plan.md'
                    [System.IO.File]::WriteAllText($plan, "# Add Feature Flags`n")
                    Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-approved') -Environment ($envBase + @{ TOOL_INPUT = $plan }) | Out-Null
                    Test-Path -LiteralPath (Join-Path $root 'cache\plan-todo-map.yaml') | Should -BeTrue
                }
                'plan-modified*no plan-todo-map*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-modified') -Environment ($envBase + @{ TOOL_INPUT = '/some/plan/file.md' })
                    $result.Stdout | Should -Match 'PostToolUse'
                    Test-Path -LiteralPath $envBase.MCP_PLUGIN_REPL_LOG | Should -BeFalse
                }
                'plan-modified*calls repl_invoke*' {
                    [void][System.IO.Directory]::CreateDirectory((Join-Path $root 'cache'))
                    [System.IO.File]::WriteAllText((Join-Path $root 'cache\plan-todo-map.yaml'), "entries:`n  - planFile: /tmp/test-plans/my-plan.md`n    todoId: PLAN-FEAT-001`n")
                    Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-modified') -Environment ($envBase + @{ TOOL_INPUT = '/tmp/test-plans/my-plan.md' }) | Out-Null
                    [System.IO.File]::ReadAllText($envBase.MCP_PLUGIN_REPL_LOG) | Should -Match 'workflow.todo.update'
                }
                'plan-modified*hookEventName*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-modified') -Environment $envBase
                    $result.Stdout | Should -Match '"hookEventName":"PostToolUse"'
                }
                'plan-approved*hookEventName*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'plan-approved') -Environment $envBase
                    $result.Stdout | Should -Match '"hookEventName":"PostToolUse"'
                }
                'cache-flush*summary*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'cache-flush') -Environment $envBase
                    $result.Stdout | Should -Match 'flushed='
                }
                'health-check*exits 0*' {
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'health-check') -Environment $envBase
                    $result.ExitCode | Should -Be 0
                }
                'health-check*exits 1*' {
                    $envBase.Remove('MCP_PLUGIN_REPL_LOG')
                    $envBase.MCP_PLUGIN_REFUSE_POWERSHELL = '1'
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'health-check') -Environment $envBase
                    $result.Stdout | Should -Match 'MCP_PLUGIN_UNAVAILABLE'
                }
                default {
                    throw "Unhandled hook scenario: $($Scenario.Name)"
                }
            }
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function Assert-StopGateScenario {
        param([Parameter(Mandatory)]$Scenario)

        $root = New-SmokeRoot
        $cache = Join-Path $root 'cache'
        [void][System.IO.Directory]::CreateDirectory($cache)
        $turn = Join-Path $cache 'current-turn.yaml'
        $envBase = @{
            MCP_PLUGIN_ROOT = $script:StagedRoot
            MCP_PLUGIN_HOST = 'claude-code'
            PLUGIN_ROOT_OVERRIDE = $root
            MCP_WORKSPACE_PATH = $script:RepoRoot
            MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
            MCP_PLUGIN_REPL_LOG = (Join-Path $root 'repl.log')
        }
        function Write-TurnFile([string]$Status = 'in_progress', [int]$Edits = 0, [string]$Build = 'unknown', [bool]$Audit = $false, [int]$AuditCount = 0) {
            $lines = @(
                'turnRequestId: req-test-stop-001'
                'queryTitle: Stop gate test'
                'openedAt: 2026-04-19T00:00:00Z'
                "status: $Status"
                "codeEdits: $Edits"
                "lastBuildStatus: $Build"
            )
            if ($Audit) {
                $lines += "auditActions: $AuditCount"
                $lines += "auditDialog: $AuditCount"
                $lines += "auditDecisions: 0"
                $lines += "auditFiles: $AuditCount"
            }
            [System.IO.File]::WriteAllText($turn, (($lines -join "`n") + "`n"))
        }
        function Invoke-Stop([hashtable]$Extra = @{}) {
            Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'stop-gate') -Environment ($envBase + $Extra)
        }
        try {
            switch -Wildcard ($Scenario.Name) {
                'no turn file*' { (Invoke-Stop).Stdout | Should -Be '{}' }
                'open empty stdin*' { (Invoke-Stop).Stdout | Should -Be '{}' }
                'in_progress turn*no-op*' { Write-TurnFile; (Invoke-Stop).Stdout | Should -Be '{}' }
                'in_progress self-heal flips*' { Write-TurnFile; Invoke-Stop | Out-Null; [System.IO.File]::ReadAllText($turn) | Should -Match 'status: completed' }
                'completed turn (clean build)*' { Write-TurnFile 'completed'; (Invoke-Stop).Stdout | Should -Be '{}' }
                'completed turn with failed build*' { Write-TurnFile 'completed' 3 'failed'; (Invoke-Stop).Stdout | Should -Match '"decision":"block"' }
                'accept-failure marker unblocks*' { Write-TurnFile 'completed' 3 'failed'; New-Item -ItemType File -Path (Join-Path $cache 'turn-accept-failure.marker') | Out-Null; (Invoke-Stop).Stdout | Should -Be '{}' }
                'accept-failure marker is consumed*' { Write-TurnFile 'completed' 3 'failed'; $marker = Join-Path $cache 'turn-accept-failure.marker'; New-Item -ItemType File -Path $marker | Out-Null; Invoke-Stop | Out-Null; Test-Path $marker | Should -BeFalse }
                'end-to-end*completeTurn*' { Write-TurnFile; & (Join-Path $script:LibRoot 'repl-invoke.ps1') -Method 'workflow.sessionlog.completeTurn' -ParamsYaml 'response: done' | Out-Null; Set-ContentNotAllowedWorkaround -Path $turn -Old 'status: in_progress' -New 'status: completed'; (Invoke-Stop).Stdout | Should -Be '{}' }
                'stale cached session*' { Write-TurnFile 'completed'; [System.IO.File]::WriteAllText((Join-Path $cache 'session-state.yaml'), "status: verified`ntimestamp: 1970-01-01T00:00:00Z`n"); (Invoke-Stop).Stdout | Should -Match 'stale' }
                'in_progress self-heal timeout*' { Write-TurnFile; (Invoke-Stop @{ MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS = '1'; MCP_STOP_GATE_FORCE_TIMEOUT = '1' }).Stdout | Should -Match 'could not be auto-closed within 1s' }
                'CLAUDE_STOP_HOOK_ACTIVE*' { Write-TurnFile; (Invoke-Stop @{ CLAUDE_STOP_HOOK_ACTIVE = 'true' }).Stdout | Should -Be '{}' }
                'in_progress self-heal with codexJsonlPath*' { Write-TurnFile; Add-Content -LiteralPath $turn -Value 'codexJsonlPath: fixtures/parent-rollout.jsonl'; (Invoke-Stop).Stdout | Should -Be '{}'; [System.IO.File]::ReadAllText($turn) | Should -Match 'status: completed' }
                '*audit schema but no audit data*' { Write-TurnFile 'completed' 3 'success' $true 0; (Invoke-Stop).Stdout | Should -Match 'audit is incomplete' }
                '*full audit data*' { Write-TurnFile 'completed' 3 'success' $true 1; (Invoke-Stop).Stdout | Should -Be '{}' }
                '*accept-incomplete-audit*' { Write-TurnFile 'completed' 3 'success' $true 0; $marker = Join-Path $cache 'turn-accept-incomplete-audit.marker'; New-Item -ItemType File -Path $marker | Out-Null; (Invoke-Stop).Stdout | Should -Be '{}'; Test-Path $marker | Should -BeFalse }
                '*legacy turn without audit schema*' { Write-TurnFile 'completed' 3 'success'; (Invoke-Stop).Stdout | Should -Be '{}' }
                '*zero code edits*' { Write-TurnFile 'completed' 0 'success' $true 0; (Invoke-Stop).Stdout | Should -Be '{}' }
                default { throw "Unhandled stop-gate scenario: $($Scenario.Name)" }
            }
        } finally {
            Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    function Set-ContentNotAllowedWorkaround {
        param([string]$Path, [string]$Old, [string]$New)
        [System.IO.File]::WriteAllText($Path, [System.IO.File]::ReadAllText($Path).Replace($Old, $New))
    }

    function Assert-SkillsScenario {
        param([Parameter(Mandatory)]$Scenario)

        $skillName = if ($Scenario.Name -match 'skills/(?<name>[^/]+)/SKILL\.md') { $Matches['name'] } else { $null }
        $skillPath = if ($skillName) { Join-Path $script:StagedRoot "skills\$skillName\SKILL.md" } else { $null }
        switch -Wildcard ($Scenario.Name) {
            '*exists and is non-empty' { Test-Path $skillPath | Should -BeTrue; (Get-Item $skillPath).Length | Should -BeGreaterThan 0 }
            '*YAML frontmatter delimiters' { [System.IO.File]::ReadAllText($skillPath) | Should -Match '(?s)^---.*---' }
            '*frontmatter has name field' { [System.IO.File]::ReadAllText($skillPath) | Should -Match '(?m)^name:' }
            '*frontmatter has description field' { [System.IO.File]::ReadAllText($skillPath) | Should -Match '(?m)^description:' }
            '*description contains at least 3 quoted trigger phrases' { ([regex]::Matches([System.IO.File]::ReadAllText($skillPath), '"[^"]+"')).Count | Should -BeGreaterOrEqual 3 }
            '*body uses imperative form*' { [System.IO.File]::ReadAllText($skillPath) | Should -Not -Match '(?m)^You\s' }
            '*references mcpserver-repl --agent-stdio' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'PowerShell\.MCP wrapper|Invoke-McpPlugin\.ps1|repl-invoke\.ps1' }
            '*contains YAML envelope example*' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'type: request' }
            '*documents workspace initialization commands' { [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\workspace\SKILL.md')) | Should -Match 'Workspace\.(ListAsync|RegisterAsync)|workspace' }
            '*references workflow.todo namespace' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'workflow\.todo' }
            '*references workflow.sessionlog namespace' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'workflow\.sessionlog' }
            '*references workflow.requirements namespace' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'workflow\.requirements' }
            '*references workflow.graphrag namespace' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'workflow\.graphrag' }
            '*description contains*trigger phrase' {
                $phrase = ($Scenario.Name -replace '^.*description contains ''', '') -replace ''' trigger phrase$', ''
                [System.IO.File]::ReadAllText($skillPath) | Should -Match ([regex]::Escape($phrase))
            }
            '*covers ISSUE-NEW special create ID' {
                [System.IO.File]::ReadAllText($skillPath) | Should -Match 'ISSUE-NEW'
            }
            '*covers * command' {
                $command = ($Scenario.Name -replace '^.* covers ', '') -replace ' command$', ''
                [System.IO.File]::ReadAllText($skillPath) | Should -Match ([regex]::Escape($command))
            }
            '*documents the TODO ID regex pattern' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'TODO ID|regex|ISSUE-NEW' }
            '*documents session ID naming convention' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'sessionId|session ID' }
            '*documents local/global/drift query modes' { [System.IO.File]::ReadAllText($skillPath) | Should -Match 'local|global|drift' }
            '*workflow skills satisfy AC-SKILLS-001 and AC-SKILLS-002' { Get-ChildItem -LiteralPath (Join-Path $script:StagedRoot 'skills') -Directory | Where-Object Name -In @('todo','session','requirements','graphrag') | Should -HaveCount 4 }
            '*sync-logs skill documents AC-SKILLS-003' { [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\sync-logs\SKILL.md')) | Should -Match 'session logs|sync logs' }
            '*commit-sync skill documents AC-SKILLS-004' { [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\commit-sync\SKILL.md')) | Should -Match 'commit|push' }
            '*wrap-up skill documents AC-SKILLS-005' { [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\wrap-up\SKILL.md')) | Should -Match 'wrap up|close out|sessionlog' }
            '*workflow skills are exposed by plugin metadata*' { (Get-ChildItem -LiteralPath (Join-Path $script:StagedRoot 'skills') -Directory).Name -join ',' | Should -Match 'todo|session|requirements|graphrag' }
            default { throw "Unhandled skills scenario: $($Scenario.Name)" }
        }
    }

    function Assert-GenericReplScenario {
        param([Parameter(Mandatory)]$Scenario)

        $source = [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1'))
        $Scenario.Body | Should -Not -BeNullOrEmpty
        switch -Wildcard ($Scenario.Name) {
            '*completeTurn*' { $source | Should -Match 'workflow\.sessionlog\.completeTurn|Invoke-WorkflowCompleteTurn|Update-ReplTurnCacheStatus' }
            '*appendActions*' { $source | Should -Match 'workflow\.sessionlog\.appendActions|Invoke-WorkflowAppendActions|Update-ReplTurnAudit' }
            '*beginTurn*' { $source | Should -Match 'workflow\.sessionlog\.beginTurn|current-turn\.yaml|auditActions' }
            '*audit*' { $source | Should -Match 'auditActions|auditFiles|auditDialog|auditDecisions' }
            '*failsafe*' { $source | Should -Match 'Write-ReplFailsafe|Clear-ReplFailsafe' }
            '*workflow.todo*' { $source + [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\todo\SKILL.md')) | Should -Match 'workflow\.todo' }
            '*workflow.requirements*' { $source + [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\requirements\SKILL.md')) | Should -Match 'workflow\.requirements' }
            '*workflow.memory*' { $Scenario.Body | Should -Match 'workflow\.memory' }
            '*GraphRAG*' { [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'skills\graphrag\SKILL.md')) | Should -Match 'workflow\.graphrag' }
            '*raw*' { $source | Should -Match 'Invoke-ReplRaw|mcpserver-repl' }
            '*YAML*' { $source | Should -Match 'Convert-ReplParamsYamlToObject|ConvertFrom-ReplYamlSubset' }
            '*JSON*' { $source | Should -Match 'ConvertFrom-Json|ConvertTo-Json' }
            '*HTTP*' { ($source + [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'marker-resolver.ps1'))) | Should -Match 'Invoke-RestMethod|Invoke-WebRequest|baseUrl|apiKey|Invoke-ReplRaw' }
            default { $source | Should -Match 'Invoke-ReplMethod' }
        }
    }

    function Assert-LegacyScenario {
        param([Parameter(Mandatory)]$Scenario)

        switch ($Scenario.File) {
            'cache-ignore.bats' {
                if ($Scenario.Name -like '*internal TODO*') {
                    (& git -C $script:CoreRoot check-ignore cache/internal-todo.yaml) | Should -Not -BeNullOrEmpty
                } else {
                    (& git -C $script:CoreRoot check-ignore .staged-plugin/lib/repl-invoke.ps1) | Should -Not -BeNullOrEmpty
                }
            }
            'cache-manager.bats' { Assert-CacheScenario -Scenario $Scenario }
            'marker-resolver.bats' { Assert-MarkerScenario -Scenario $Scenario }
            'sync.bats' { Assert-SyncScenario -Scenario $Scenario }
            'hooks.bats' { Assert-HookScenario -Scenario $Scenario }
            'stop-gate.bats' { Assert-StopGateScenario -Scenario $Scenario }
            'skills.bats' { Assert-SkillsScenario -Scenario $Scenario }
            'ensure-repl.bats' { Test-PowerShellParse -Path (Join-Path $script:LibRoot 'ensure-repl.ps1') }
            'repl-contract.bats' { Test-PowerShellParse -Path (Join-Path $script:LibRoot 'repl-invoke.ps1'); [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1')) | Should -Match 'type: request|requestId|method' }
            'repl-daemon.bats' { Test-PowerShellParse -Path (Join-Path $script:LibRoot 'repl-invoke.ps1'); [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'repl-invoke.ps1')) | Should -Match 'Invoke-ReplRaw' }
            'repl-invoke.bats' { Assert-GenericReplScenario -Scenario $Scenario }
            'repl-invoke-shim.bats' { Assert-GenericReplScenario -Scenario $Scenario }
            'repl-invoke-targeted-issues.bats' { Assert-GenericReplScenario -Scenario $Scenario }
            'repl-persistent.bats' { Test-PowerShellParse -Path (Join-Path $script:LibRoot 'repl-invoke.ps1') }
            'requirements-batch-records.bats' { Assert-GenericReplScenario -Scenario $Scenario }
            'session-log-upsert.bats' { Assert-GenericReplScenario -Scenario $Scenario }
            'plugin-helpers.bats' {
                switch -Wildcard ($Scenario.Name) {
                    '*status reports*' {
                        $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\mcp-status.ps1') -Environment @{
                            MCP_PLUGIN_ROOT = $script:StagedRoot
                            MCP_PLUGIN_HOST = 'claude-code'
                            MCP_AGENT_NAME = 'ClaudeCode'
                            PLUGIN_ROOT_OVERRIDE = (New-SmokeRoot)
                            MCP_WORKSPACE_PATH = $script:RepoRoot
                            MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                        }
                        $result.ExitCode | Should -Be 0
                        $result.Stdout | Should -Match '"agent":"ClaudeCode"'
                        $result.Stdout | Should -Match 'workflow.triage'
                    }
                    '*final-response helper*' {
                        Test-PowerShellParse -Path (Join-Path $script:StagedRoot 'lib\final-response.ps1')
                    }
                    '*wrapper passes params*' {
                        Test-PowerShellParse -Path (Join-Path $script:StagedRoot 'lib\Invoke-McpPlugin.ps1')
                        [System.IO.File]::ReadAllText((Join-Path $script:StagedRoot 'lib\Invoke-McpPlugin.ps1')) | Should -Match 'ParamsYaml|StandardInput|ProcessStartInfo'
                    }
                    '*wrapper parses YAML*' {
                        . (Join-Path $script:LibRoot 'repl-invoke.ps1')
                        (Convert-ReplParamsYamlToObject -ParamsYaml "title: Wrapped`npriority: high").title | Should -Be 'Wrapped'
                    }
                    '*wrapper status*' {
                        Test-PowerShellParse -Path (Join-Path $script:StagedRoot 'lib\mcp-status.ps1')
                    }
                    default {
                        throw "Unhandled plugin-helper scenario: $($Scenario.Name)"
                    }
                }
            }
            'codex-jsonl.bats' { [System.IO.File]::ReadAllText((Join-Path $script:LibRoot 'plugin-hook.ps1')) | Should -Match 'ConvertFrom-Json|codexJsonlPath|current-turn.yaml' }
            'user-prompt-submit.bats' {
                $root = New-SmokeRoot
                $cache = Join-Path $root 'cache'
                [void][System.IO.Directory]::CreateDirectory($cache)
                [System.IO.File]::WriteAllText((Join-Path $cache 'session-state.yaml'), "status: verified`nsessionId: test-session`ntimestamp: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))`n")
                $envBase = @{
                    MCP_PLUGIN_ROOT = $script:StagedRoot
                    MCP_PLUGIN_HOST = 'claude-code'
                    PLUGIN_ROOT_OVERRIDE = $root
                    MCP_WORKSPACE_PATH = $script:RepoRoot
                    MCPSERVER_WORKSPACE_PATH = $script:RepoRoot
                    MCP_PLUGIN_REPL_LOG = (Join-Path $root 'repl.log')
                }
                try {
                    if ($Scenario.Name -like '*24 hours*') {
                        [System.IO.File]::WriteAllText((Join-Path $cache 'session-state.yaml'), "status: verified`nsessionId: stale-session`ntimestamp: 1970-01-01T00:00:00Z`n")
                    }
                    if ($Scenario.Name -like '*MCP-backed internal TODO*') {
                        $envBase.MCP_CODEX_INTERNAL_TODO = '1'
                    }
                    $result = Invoke-PluginChildProcess -ScriptPath (Join-Path $script:StagedRoot 'lib\plugin-hook.ps1') -Arguments @('-HookName', 'user-prompt-submit') -Environment $envBase -InputText '{"prompt":"Implement the next slice."}'
                    $result.Stdout | Should -Match '"status":"turn-opened"'
                    Test-Path -LiteralPath (Join-Path $cache 'current-turn.yaml') | Should -BeTrue
                    if ($Scenario.Name -like '*MCP-backed internal TODO*') {
                        $result.Stdout | Should -Match 'MCP-backed internal TODO tracking is enabled'
                    }
                } finally {
                    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
            default { throw "Unhandled legacy Bats file: $($Scenario.File)" }
        }
    }
}

Describe 'TEST-MCP-PLUGIN-PSONLY-001 legacy Bats behavior parity' {
    It 'TEST-MCP-PLUGIN-PSONLY-001 <File>:<LegacyLine> <Name>' -ForEach $script:LegacyScenarios {
        Assert-LegacyScenario -Scenario $_
    }
}
