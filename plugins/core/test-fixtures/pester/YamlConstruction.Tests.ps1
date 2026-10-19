#Requires -Version 7.0

# Workspace rule (marker "YAML Mutation Rule" and operator profile 6.1): JSON and YAML payloads are built
# from native objects and serialized; never handwritten as string literals, joined lines or here-strings.
# This suite proves every YAML producer in lib, hooks and skills goes through the serializer, and a static
# lint keeps hand-built YAML from coming back. Mock gate first (fixtures and negative controls), then the
# production functions bound to the same assertions.

Describe 'Plugin YAML producers build objects and serialize' {
    BeforeAll {
        $script:CoreRepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).ProviderPath
        . (Join-Path $PSScriptRoot '..\PluginTestFixtures.ps1')
        # Ported from the plugin repos (7991235): run against a staged plugin synced from plugins/core.
        $script:PluginRoot = Write-TestStagedPlugin -RepoRoot $script:CoreRepoRoot -Root (Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-staged-yaml-' + [guid]::NewGuid().ToString('N')))
        . (Join-Path $script:PluginRoot 'lib/yaml-object-mutation.ps1')
        . (Join-Path $script:PluginRoot 'lib/plan-todo-map.ps1')
        Import-Module (Join-Path $script:PluginRoot 'lib/McpPluginShim.psm1') -Force
        Import-McpYamlSerializer
        $script:TestRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('mcp-yaml-construction-' + [guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($script:TestRoot)

        function Assert-ParsesToMapping {
            param([Parameter(Mandatory)][string]$Path)
            $parsed = ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText($Path)) -Ordered
            $parsed | Should -BeOfType [System.Collections.Specialized.OrderedDictionary]
            return $parsed
        }
    }

    AfterAll {
        if (Test-Path -LiteralPath $script:TestRoot) { Remove-Item -LiteralPath $script:TestRoot -Recurse -Force -ErrorAction SilentlyContinue }
    }

    Context 'mock gate: fixtures prove the assertions and show why hand-built YAML breaks' {
        It 'a serialized fixture with a colon-bearing value round-trips exactly' {
            $fixture = [ordered]@{ status = 'verified'; sessionId = 'Agent-20261017T000000Z-a: b'; lastUpdated = '2026-10-17T13:00:00Z' }
            $parsed = ConvertFrom-Yaml -Yaml (ConvertTo-McpYamlText -Document $fixture) -Ordered
            $parsed['sessionId'] | Should -Be 'Agent-20261017T000000Z-a: b'
            $parsed['status'] | Should -Be 'verified'
        }

        It 'the same value hand-joined into YAML lines does not parse (negative control)' {
            $handwritten = @("status: verified", "sessionId: Agent-20261017T000000Z-a: b", "lastUpdated: 2026-10-17T13:00:00Z") -join "`n"
            { ConvertFrom-Yaml -Yaml $handwritten -Ordered } | Should -Throw
        }

        It 'a hand-built pending record with a nested colon value loses or breaks the params (negative control)' {
            $handwritten = "id: `"001`"`nmethod: workflow.x`nparams:`n  note: a: b`nretryCount: 0"
            { ConvertFrom-Yaml -Yaml $handwritten -Ordered } | Should -Throw
        }
    }

    Context 'repl-invoke session-state writer' {
        BeforeAll {
            . (Join-Path $script:PluginRoot 'lib/repl-invoke.ps1')
        }

        It 'writes session-state.yaml as a serialized mapping' {
            $cacheDir = Join-Path $script:TestRoot 'session'
            [void][System.IO.Directory]::CreateDirectory($cacheDir)
            $path = Write-ReplStickySessionState -CacheDir $cacheDir -SessionId 'ClaudeCode-20261017T130000Z-plugin-session'
            $parsed = Assert-ParsesToMapping -Path $path
            $parsed['status'] | Should -Be 'verified'
            $parsed['sessionId'] | Should -Be 'ClaudeCode-20261017T130000Z-plugin-session'
            ([string]$parsed['lastUpdated']) | Should -Match '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$'
        }
    }

    Context 'shim failsafe record ToYaml' {
        It 'serializes method, label, timestamp and params as a nested mapping' {
            $paramsYaml = ConvertTo-McpYamlText -Document ([ordered]@{ sessionId = 'S-1'; note = 'a: b'; items = @(1, 2) })
            $record = New-McpPluginFailsafeRecord -Method client.SessionLog.UpsertTurnAsync -Label session_upsertTurn -Timestamp 20261017T130000Z -ParamsYaml $paramsYaml
            $parsed = ConvertFrom-Yaml -Yaml $record.ToYaml() -Ordered
            $parsed['method'] | Should -Be 'client.SessionLog.UpsertTurnAsync'
            $parsed['label'] | Should -Be 'session_upsertTurn'
            $parsed['timestamp'] | Should -Be '20261017T130000Z'
            $parsed['params']['note'] | Should -Be 'a: b'
            @($parsed['params']['items']).Count | Should -Be 2
        }

        It 'maps an empty params body to an empty mapping' {
            $record = New-McpPluginFailsafeRecord -Method client.Health.GetAsync -Label health -Timestamp 20261017T130000Z -ParamsYaml '{}'
            $parsed = ConvertFrom-Yaml -Yaml $record.ToYaml() -Ordered
            $parsed['params'] | Should -BeOfType [System.Collections.IDictionary]
            $parsed['params'].Count | Should -Be 0
        }
    }

    Context 'cache-manager pending records' {
        BeforeAll {
            $script:SavedCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
            $script:SavedFlushRepl = $env:MCP_CACHE_FLUSH_REPL
            $script:CacheRoot = Join-Path $script:TestRoot 'cache'
            [void][System.IO.Directory]::CreateDirectory($script:CacheRoot)
            $env:MCP_CACHE_DIR_OVERRIDE = $script:CacheRoot
            $script:CacheManager = Join-Path $script:PluginRoot 'lib/cache-manager.ps1'
            $script:Capture = Join-Path $script:TestRoot 'flush-capture.yaml'
            $script:StubOk = Join-Path $script:TestRoot 'stub-ok.ps1'
            [System.IO.File]::WriteAllText($script:StubOk, "param([string]`$Method, [string]`$ParamsYaml = '')`n[System.IO.File]::WriteAllText('$($script:Capture)', `"method: `$Method`n---`n`$ParamsYaml`")`n")
            $script:StubFail = Join-Path $script:TestRoot 'stub-fail.ps1'
            [System.IO.File]::WriteAllText($script:StubFail, "param([string]`$Method, [string]`$ParamsYaml = '')`nthrow 'replay failed'`n")
        }

        AfterAll {
            if ($null -ne $script:SavedCacheOverride) { $env:MCP_CACHE_DIR_OVERRIDE = $script:SavedCacheOverride } else { Remove-Item Env:MCP_CACHE_DIR_OVERRIDE -ErrorAction SilentlyContinue }
            if ($null -ne $script:SavedFlushRepl) { $env:MCP_CACHE_FLUSH_REPL = $script:SavedFlushRepl } else { Remove-Item Env:MCP_CACHE_FLUSH_REPL -ErrorAction SilentlyContinue }
        }

        It 'writes a pending record whose params survive as a nested object' {
            $paramsYaml = ConvertTo-McpYamlText -Document ([ordered]@{ dialogItems = @([ordered]@{ content = 'Decision: a: b'; category = 'decision' }) })
            $path = & $script:CacheManager -Action write -Method 'workflow.sessionlog.appendDialog' -ParamsYaml $paramsYaml
            Test-Path -LiteralPath $path | Should -BeTrue
            $parsed = Assert-ParsesToMapping -Path $path
            $parsed['method'] | Should -Be 'workflow.sessionlog.appendDialog'
            [int]$parsed['retryCount'] | Should -Be 0
            $parsed['params']['dialogItems'][0]['content'] | Should -Be 'Decision: a: b'
        }

        It 'replays the same params object on flush and clears the record' {
            $env:MCP_CACHE_FLUSH_REPL = $script:StubOk
            $result = & $script:CacheManager -Action flush
            $result | Should -Match 'flushed=1 failed=0 pending=0'
            $captured = [System.IO.File]::ReadAllText($script:Capture)
            $docs = @(ConvertFrom-Yaml -Yaml $captured -AllDocuments -Ordered)
            $docs[0]['method'] | Should -Be 'workflow.sessionlog.appendDialog'
            $docs[1]['dialogItems'][0]['content'] | Should -Be 'Decision: a: b'
        }

        It 'increments retryCount through the object on a failed replay and keeps the file parseable' {
            $null = & $script:CacheManager -Action write -Method 'workflow.todo.list' -ParamsYaml ''
            $env:MCP_CACHE_FLUSH_REPL = $script:StubFail
            $result = & $script:CacheManager -Action flush
            $result | Should -Match 'flushed=0 failed=1 pending=1'
            $file = Get-ChildItem -Path (Join-Path $script:CacheRoot 'pending') -Filter '*.yaml' | Select-Object -First 1
            $parsed = Assert-ParsesToMapping -Path $file.FullName
            [int]$parsed['retryCount'] | Should -Be 1
            $parsed['params'].Count | Should -Be 0
        }
    }

    Context 'plan-to-TODO map' {
        It 'writes and reads the map as an object, including a Windows path with backslashes' {
            $mapPath = Join-Path $script:TestRoot 'plan-todo-map.yaml'
            $null = Write-PlanTodoMapEntry -MapPath $mapPath -PlanFile 'F:\GitHub\vice-sharp\docs\plans\PLAN-X: draft.md' -TodoId 'PLAN-X-001'
            $parsed = Assert-ParsesToMapping -Path $mapPath
            @($parsed['entries']).Count | Should -Be 1
            $last = Get-LastPlanTodoMapEntryFromFile -MapPath $mapPath
            $last.planFile | Should -Be 'F:\GitHub\vice-sharp\docs\plans\PLAN-X: draft.md'
            $last.todoId | Should -Be 'PLAN-X-001'
            Find-PlanTodoIdInMap -MapPath $mapPath -PlanFile 'F:\GitHub\vice-sharp\docs\plans\PLAN-X: draft.md' | Should -Be 'PLAN-X-001'
            Find-PlanTodoIdInMap -MapPath $mapPath -PlanFile 'other.md' | Should -BeNullOrEmpty
        }

        It 'still reads a legacy map written as plain lines' {
            $legacy = Join-Path $script:TestRoot 'legacy-map.yaml'
            [System.IO.File]::WriteAllText($legacy, "entries:`n  - planFile: C:\plans\old.md`n    todoId: PLAN-OLD-001`n")
            (Get-LastPlanTodoMapEntryFromFile -MapPath $legacy).todoId | Should -Be 'PLAN-OLD-001'
        }

        It 'returns nothing for a missing map' {
            Get-LastPlanTodoMapEntryFromFile -MapPath (Join-Path $script:TestRoot 'absent.yaml') | Should -BeNullOrEmpty
        }
    }

    Context 'REPL diagnostic log entries' {
        It 'appends entries that read back as YAML documents with method and params text' {
            $log = Join-Path $script:TestRoot 'repl-log.txt'
            Write-McpReplLogEntry -Path $log -Method 'workflow.todo.list' -ParamsYaml ''
            Write-McpReplLogEntry -Path $log -Method 'workflow.sessionlog.appendDialog' -ParamsYaml "dialogItems:`r`n- content: 'a: b'`r`n"
            $docs = @(ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText($log)) -AllDocuments -Ordered | Where-Object { $null -ne $_ })
            $docs.Count | Should -Be 2
            $docs[0]['method'] | Should -Be 'workflow.todo.list'
            $docs[1]['method'] | Should -Be 'workflow.sessionlog.appendDialog'
            $docs[1]['params'] | Should -Be "dialogItems:`n- content: 'a: b'`n"
        }
    }

    Context 'static lint: no hand-built YAML remains in lib, hooks or skills' {
        BeforeAll {
            # A hand-built YAML line is a bare string literal shaped like "key: value" or 'key: value' on
            # its own line (the joined-array style), a literal block marker 'params: |', or a here-string
            # line beginning a YAML mapping. Regex and match patterns are excluded by their metacharacters.
            $script:LintPattern = '^\s*(?:["''])(?:[A-Za-z_][A-Za-z0-9_-]*:\s[^"''\\^*+?()\[\]|]*|[A-Za-z_][A-Za-z0-9_-]*:\s*\|)(?:["''])\s*$'
            function Get-HandBuiltYamlLine {
                param([Parameter(Mandatory)][string]$Root)
                $hits = @()
                foreach ($dir in @('lib', 'hooks', 'skills')) {
                    $path = Join-Path $Root $dir
                    if (-not (Test-Path -LiteralPath $path)) { continue }
                    foreach ($file in Get-ChildItem -Path $path -Recurse -Include *.ps1, *.psm1 -File) {
                        $lines = [System.IO.File]::ReadAllLines($file.FullName)
                        for ($i = 0; $i -lt $lines.Length; $i++) {
                            $line = $lines[$i]
                            if ($line -match '^\s*#') { continue }
                            if ($line -match $script:LintPattern) { $hits += ('{0}:{1}: {2}' -f $file.FullName.Substring($Root.Length + 1), ($i + 1), $line.Trim()) }
                        }
                    }
                }
                return $hits
            }
        }

        It 'the lint detects the old joined-array style (negative control)' {
            $sample = Join-Path $script:TestRoot 'lint-sample'
            [void][System.IO.Directory]::CreateDirectory((Join-Path $sample 'lib'))
            [System.IO.File]::WriteAllText((Join-Path $sample 'lib/bad.ps1'), "`$entry = @(`n    `"method: `$Method`"`n    'params: |'`n    '---'`n) -join `"`n`"`n")
            $hits = @(Get-HandBuiltYamlLine -Root $sample)
            $hits.Count | Should -Be 2
        }

        It 'the lint ignores regex patterns and comments' {
            $sample = Join-Path $script:TestRoot 'lint-clean'
            [void][System.IO.Directory]::CreateDirectory((Join-Path $sample 'lib'))
            [System.IO.File]::WriteAllText((Join-Path $sample 'lib/ok.ps1'), "# 'type: error'`nif (`$x -match '^method:\s*(.+)$') { }`n`$y = 'plain text'`n")
            @(Get-HandBuiltYamlLine -Root $sample).Count | Should -Be 0
        }

        It 'finds no hand-built YAML lines in the plugin' {
            $hits = @(Get-HandBuiltYamlLine -Root $script:PluginRoot)
            ($hits -join "`n") | Should -BeNullOrEmpty
        }
    }
}
