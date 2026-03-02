BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'McpSession.psm1') -Force
}

Describe 'McpSession Module' {
    # Default mock — absorbs all HTTP calls
    BeforeAll {
        Mock Invoke-RestMethod { $null } -ModuleName McpSession
    }

    # Reset module state between tests
    BeforeEach {
        InModuleScope McpSession {
            $script:McpBaseUrl = $null
            $script:McpApiKey  = $null
            $script:McpHeaders = @{}
        }
    }

    # ── Initialize ────────────────────────────────────────────────────────────

    Describe 'Initialize-McpSession' {
        It 'sets connection from explicit BaseUrl and ApiKey' {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'test-key'
            InModuleScope McpSession {
                $script:McpBaseUrl | Should -Be 'http://test:9999'
                $script:McpApiKey  | Should -Be 'test-key'
                $script:McpHeaders['X-Api-Key'] | Should -Be 'test-key'
                $script:McpHeaders['Content-Type'] | Should -Be 'application/json'
            }
        }

        It 'trims trailing slash from BaseUrl' {
            Initialize-McpSession -BaseUrl 'http://test:9999/' -ApiKey 'k'
            InModuleScope McpSession { $script:McpBaseUrl | Should -Be 'http://test:9999' }
        }

        It 'parses marker file for baseUrl and apiKey' {
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            @"
owner: test
baseUrl: http://marker-host:7150
apiKey: marker-key-456
workspace: demo
"@ | Set-Content $marker

            Initialize-McpSession -MarkerPath $marker
            InModuleScope McpSession {
                $script:McpBaseUrl | Should -Be 'http://marker-host:7150'
                $script:McpApiKey  | Should -Be 'marker-key-456'
            }
        }

        It 'discovers marker by walking up from current directory' {
            $sub = Join-Path $TestDrive 'a' 'b' 'c'
            New-Item $sub -ItemType Directory -Force | Out-Null
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            "baseUrl: http://walk:1234`napiKey: walk-key" | Set-Content $marker

            Push-Location $sub
            try {
                Initialize-McpSession
                InModuleScope McpSession { $script:McpBaseUrl | Should -Be 'http://walk:1234' }
            } finally { Pop-Location }
        }

        It 'throws when marker file not found and no explicit params' {
            # Use a temp dir outside TestDrive to avoid walk-up finding markers from other tests
            $isolatedDir = Join-Path ([System.IO.Path]::GetTempPath()) "pester-no-marker-$(New-Guid)"
            New-Item $isolatedDir -ItemType Directory -Force | Out-Null
            Push-Location $isolatedDir
            try {
                { Initialize-McpSession } | Should -Throw '*not found*'
            } finally {
                Pop-Location
                Remove-Item $isolatedDir -Recurse -Force
            }
        }

        It 'calls the health endpoint' {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/health'
            }
        }
    }

    # ── Assert-Initialized guard ──────────────────────────────────────────────

    Describe 'Assert-Initialized guard' {
        It 'throws on New-McpSessionLog without init' {
            { New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm' } | Should -Throw '*not initialized*'
        }

        It 'throws on Get-McpSessionLog without init' {
            { Get-McpSessionLog } | Should -Throw '*not initialized*'
        }

        It 'throws on Send-McpDialog without init' {
            $fake = [PSCustomObject]@{ sourceType = 'T'; sessionId = 's' }
            { Send-McpDialog -Session $fake -RequestId 'r' -Content 'c' } | Should -Throw '*not initialized*'
        }
    }

    # ── New-McpSessionLog ─────────────────────────────────────────────────────

    Describe 'New-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'returns session with correct properties' {
            $s = New-McpSessionLog -SourceType 'TestAgent' -Title 'Test session' -Model 'gpt-4'
            $s.sourceType | Should -Be 'TestAgent'
            $s.title      | Should -Be 'Test session'
            $s.model      | Should -Be 'gpt-4'
            $s.status     | Should -Be 'in_progress'
        }

        It 'initializes empty entries list' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $s.entries.GetType().Name | Should -BeLike 'List*'
            $s.entries.Count | Should -Be 0
        }

        It 'auto-generates sessionId with source prefix' {
            $s = New-McpSessionLog -SourceType 'Copilot' -Title 't' -Model 'm'
            $s.sessionId | Should -BeLike 'Copilot-*'
            $s.sessionId.Length | Should -BeGreaterThan 10
        }

        It 'uses explicit sessionId when provided' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 'my-sess-42' -Title 't' -Model 'm'
            $s.sessionId | Should -Be 'my-sess-42'
        }

        It 'sets started and lastUpdated to UTC ISO 8601' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $s.started     | Should -Match '^\d{4}-\d{2}-\d{2}T'
            $s.lastUpdated | Should -Match '^\d{4}-\d{2}-\d{2}T'
        }

        It 'pushes to server on creation' {
            New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }
    }

    # ── Add-McpSessionEntry ───────────────────────────────────────────────────

    Describe 'Add-McpSessionEntry' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'adds entry to session entries list' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'Fix bug' -QueryText 'Fix the auth bug'
            $s.entries.Count | Should -Be 1
            $e.queryTitle    | Should -Be 'Fix bug'
            $e.queryText     | Should -Be 'Fix the auth bug'
        }

        It 'defaults status to in_progress' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q'
            $e.status | Should -Be 'in_progress'
        }

        It 'auto-generates sequential requestIds' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e1 = Add-McpSessionEntry -Session $s -QueryTitle 'First' -QueryText 'q1' -NoPush
            $e2 = Add-McpSessionEntry -Session $s -QueryTitle 'Second' -QueryText 'q2' -NoPush
            $e3 = Add-McpSessionEntry -Session $s -QueryTitle 'Third' -QueryText 'q3' -NoPush
            $e1.requestId | Should -Be 'req-001'
            $e2.requestId | Should -Be 'req-002'
            $e3.requestId | Should -Be 'req-003'
        }

        It 'inherits model from session' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'claude-sonnet'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $e.model | Should -Be 'claude-sonnet'
        }

        It 'accepts explicit model override' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'default'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -Model 'override' -NoPush
            $e.model | Should -Be 'override'
        }

        It 'initializes empty mutable collections' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $e.designDecisions.Count        | Should -Be 0
            $e.requirementsDiscovered.Count | Should -Be 0
            $e.filesModified.Count          | Should -Be 0
            $e.blockers.Count               | Should -Be 0
            $e.actions.Count                | Should -Be 0
            $e.processingDialog.Count       | Should -Be 0
        }

        It 'adds entry locally without extra push when -NoPush is set' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $originalUpdated = $s.lastUpdated
            Add-McpSessionEntry -Session $s -QueryTitle 'NoPush test' -QueryText 'test' -NoPush
            # Entry was added to in-memory list
            $s.entries.Count | Should -Be 1
            $s.entries[0].queryTitle | Should -Be 'NoPush test'
            # lastUpdated should NOT have been bumped (Update-McpSessionLog was not called)
            $s.lastUpdated | Should -Be $originalUpdated
        }
    }

    # ── Set-McpSessionEntry ───────────────────────────────────────────────────

    Describe 'Set-McpSessionEntry' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'updates response field' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionEntry -Entry $e -Response 'All done!' -NoPush
            $e.response | Should -Be 'All done!'
        }

        It 'updates status field' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionEntry -Entry $e -Status completed -NoPush
            $e.status | Should -Be 'completed'
        }

        It 'appends to filesModified' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionEntry -Entry $e -FilesModified @('a.cs', 'b.cs') -NoPush
            Set-McpSessionEntry -Entry $e -FilesModified @('c.cs') -NoPush
            $e.filesModified.Count | Should -Be 3
            $e.filesModified[2]    | Should -Be 'c.cs'
        }

        It 'appends to designDecisions' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Set-McpSessionEntry -Entry $e -DesignDecisions @('Use JWT', 'Skip caching') -NoPush
            $e.designDecisions.Count | Should -Be 2
        }

        It 'pushes to server when Session is provided and NoPush is not set' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            Mock Invoke-RestMethod { $null } -ModuleName McpSession
            Set-McpSessionEntry -Entry $e -Session $s -Response 'done'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }
    }

    # ── Add-McpAction ─────────────────────────────────────────────────────────

    Describe 'Add-McpAction' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'adds action with auto-incrementing order' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a1 = Add-McpAction -Entry $e -Description 'Created file' -Type create -FilePath 'new.cs'
            $a2 = Add-McpAction -Entry $e -Description 'Edited file' -Type edit -FilePath 'old.cs'
            $a3 = Add-McpAction -Entry $e -Description 'Committed' -Type commit
            $e.actions.Count | Should -Be 3
            $a1.order | Should -Be 1
            $a2.order | Should -Be 2
            $a3.order | Should -Be 3
        }

        It 'sets correct type and description' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Entry $e -Description 'Deleted unused file' -Type delete -FilePath 'old.txt'
            $a.description | Should -Be 'Deleted unused file'
            $a.type        | Should -Be 'delete'
            $a.filePath    | Should -Be 'old.txt'
        }

        It 'defaults status to completed' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Entry $e -Description 'test' -Type edit
            $a.status | Should -Be 'completed'
        }

        It 'accepts explicit status' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Entry $e -Description 'WIP' -Type edit -Status in_progress
            $a.status | Should -Be 'in_progress'
        }

        It 'defaults filePath to empty string' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $e = Add-McpSessionEntry -Session $s -QueryTitle 'q' -QueryText 'q' -NoPush
            $a = Add-McpAction -Entry $e -Description 'Design choice' -Type design_decision
            $a.filePath | Should -Be ''
        }
    }

    # ── Update-McpSessionLog ──────────────────────────────────────────────────

    Describe 'Update-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'updates lastUpdated timestamp' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            $before = $s.lastUpdated
            Start-Sleep -Milliseconds 100
            Update-McpSessionLog -Session $s
            $s.lastUpdated | Should -Not -Be $before
        }

        It 'updates status when provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Update-McpSessionLog -Session $s -Status completed
            $s.status | Should -Be 'completed'
        }

        It 'preserves status when not provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Update-McpSessionLog -Session $s
            $s.status | Should -Be 'in_progress'
        }

        It 'updates title when provided' {
            $s = New-McpSessionLog -SourceType 'T' -Title 'Old' -Model 'm'
            Update-McpSessionLog -Session $s -Title 'New Title'
            $s.title | Should -Be 'New Title'
        }

        It 'pushes to server' {
            $s = New-McpSessionLog -SourceType 'T' -Title 't' -Model 'm'
            Mock Invoke-RestMethod { $null } -ModuleName McpSession
            Update-McpSessionLog -Session $s
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Method -eq 'Post' -and $Uri -like '*/mcpserver/sessionlog'
            }
        }
    }

    # ── Get-McpSessionLog ─────────────────────────────────────────────────────

    Describe 'Get-McpSessionLog' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'uses default limit=5 and offset=0' {
            Get-McpSessionLog
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog?limit=5&offset=0'
            }
        }

        It 'passes custom limit and offset' {
            Get-McpSessionLog -Limit 20 -Offset 10
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog?limit=20&offset=10'
            }
        }
    }

    # ── Send-McpDialog ────────────────────────────────────────────────────────

    Describe 'Send-McpDialog' {
        BeforeEach {
            Initialize-McpSession -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'posts to the correct dialog endpoint' {
            $s = New-McpSessionLog -SourceType 'Agent' -SessionId 'sess-42' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'req-001' -Content 'Thinking...'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Uri -eq 'http://test:9999/mcpserver/sessionlog/Agent/sess-42/req-001/dialog' -and
                $Method -eq 'Post'
            }
        }

        It 'defaults role to model and category to reasoning' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 's1' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'r1' -Content 'test'
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Body -like '*"role":*"model"*' -and $Body -like '*"category":*"reasoning"*'
            }
        }

        It 'accepts custom role and category' {
            $s = New-McpSessionLog -SourceType 'A' -SessionId 's1' -Title 't' -Model 'm'
            Send-McpDialog -Session $s -RequestId 'r1' -Content 'Result' -Role tool -Category tool_result
            Should -Invoke Invoke-RestMethod -ModuleName McpSession -ParameterFilter {
                $Body -like '*"role":*"tool"*' -and $Body -like '*"category":*"tool_result"*'
            }
        }
    }
}
