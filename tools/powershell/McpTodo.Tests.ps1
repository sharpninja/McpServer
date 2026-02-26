BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'McpTodo.psm1') -Force
}

Describe 'McpTodo Module' {
    # Default mock — absorbs all HTTP calls
    BeforeAll {
        Mock Invoke-RestMethod { $null } -ModuleName McpTodo
    }

    # Reset module state between tests
    BeforeEach {
        InModuleScope McpTodo {
            $script:McpBaseUrl = $null
            $script:McpApiKey  = $null
            $script:McpHeaders = @{}
        }
    }

    # ── Initialize ────────────────────────────────────────────────────────────

    Describe 'Initialize-McpTodo' {
        It 'sets connection from explicit BaseUrl and ApiKey' {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'test-key'
            InModuleScope McpTodo {
                $script:McpBaseUrl | Should -Be 'http://test:9999'
                $script:McpApiKey  | Should -Be 'test-key'
                $script:McpHeaders['X-Api-Key']    | Should -Be 'test-key'
                $script:McpHeaders['Content-Type'] | Should -Be 'application/json'
            }
        }

        It 'trims trailing slash from BaseUrl' {
            Initialize-McpTodo -BaseUrl 'http://test:9999/' -ApiKey 'k'
            InModuleScope McpTodo { $script:McpBaseUrl | Should -Be 'http://test:9999' }
        }

        It 'parses marker file for connection details' {
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            @"
owner: test
baseUrl: http://marker:7150
apiKey: mk-123
workspace: demo
"@ | Set-Content $marker

            Initialize-McpTodo -MarkerPath $marker
            InModuleScope McpTodo {
                $script:McpBaseUrl | Should -Be 'http://marker:7150'
                $script:McpApiKey  | Should -Be 'mk-123'
            }
        }

        It 'discovers marker by walking up from current directory' {
            $sub = Join-Path $TestDrive 'x' 'y' 'z'
            New-Item $sub -ItemType Directory -Force | Out-Null
            $marker = Join-Path $TestDrive 'AGENTS-README-FIRST.yaml'
            "baseUrl: http://walk:5000`napiKey: wk" | Set-Content $marker

            Push-Location $sub
            try {
                Initialize-McpTodo
                InModuleScope McpTodo { $script:McpBaseUrl | Should -Be 'http://walk:5000' }
            } finally { Pop-Location }
        }

        It 'throws when marker file not found' {
            $isolatedDir = Join-Path ([System.IO.Path]::GetTempPath()) "pester-no-marker-$(New-Guid)"
            New-Item $isolatedDir -ItemType Directory -Force | Out-Null
            Push-Location $isolatedDir
            try {
                { Initialize-McpTodo } | Should -Throw '*not found*'
            } finally {
                Pop-Location
                Remove-Item $isolatedDir -Recurse -Force
            }
        }

        It 'calls the health endpoint' {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/health'
            }
        }
    }

    # ── Assert-Initialized guard ──────────────────────────────────────────────

    Describe 'Assert-Initialized guard' {
        It 'throws on Get-McpTodo without init' {
            { Get-McpTodo } | Should -Throw '*not initialized*'
        }

        It 'throws on New-McpTodo without init' {
            { New-McpTodo -Id 'x' -Title 't' -Section 's' -Priority low } | Should -Throw '*not initialized*'
        }

        It 'throws on Complete-McpTodo without init' {
            { Complete-McpTodo -Id 'x' -DoneSummary 'done' } | Should -Throw '*not initialized*'
        }
    }

    # ── Get-McpTodo ───────────────────────────────────────────────────────────

    Describe 'Get-McpTodo' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'lists all todos when no Id provided' {
            Mock Invoke-RestMethod { @{ items = @(
                @{ id = 'a'; title = 'Alpha' },
                @{ id = 'b'; title = 'Beta' }
            ) } } -ModuleName McpTodo -ParameterFilter { $Uri -like '*/mcp/todo' -and $Uri -notlike '*/mcp/todo/*' }

            $result = Get-McpTodo
            $result.Count | Should -Be 2
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/mcp/todo'
            }
        }

        It 'gets specific todo by Id' {
            Mock Invoke-RestMethod { @{ id = 'fix-auth'; title = 'Fix auth' } } -ModuleName McpTodo -ParameterFilter { $Uri -like '*/mcp/todo/fix-auth' }

            $result = Get-McpTodo -Id 'fix-auth'
            $result.id | Should -Be 'fix-auth'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/mcp/todo/fix-auth'
            }
        }
    }

    # ── Get-McpTodoPrompt ─────────────────────────────────────────────────────

    Describe 'Get-McpTodoPrompt' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'calls correct URL for implement prompt' {
            Get-McpTodoPrompt -Id 'fix-auth' -Type implement
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/mcp/todo/fix-auth/prompt/implement'
            }
        }

        It 'calls correct URL for plan prompt' {
            Get-McpTodoPrompt -Id 'add-cache' -Type plan
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/mcp/todo/add-cache/prompt/plan'
            }
        }

        It 'calls correct URL for status prompt' {
            Get-McpTodoPrompt -Id 'deploy' -Type status
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Uri -eq 'http://test:9999/mcp/todo/deploy/prompt/status'
            }
        }
    }

    # ── New-McpTodo ───────────────────────────────────────────────────────────

    Describe 'New-McpTodo' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'posts with required fields' {
            New-McpTodo -Id 'test-todo' -Title 'Test Todo' -Section 'Backend' -Priority high
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Method -eq 'Post' -and
                $Uri -eq 'http://test:9999/mcp/todo' -and
                $Body -like '*"id":*"test-todo"*' -and
                $Body -like '*"title":*"Test Todo"*' -and
                $Body -like '*"section":*"Backend"*' -and
                $Body -like '*"priority":*"high"*'
            }
        }

        It 'includes optional description and estimate' {
            New-McpTodo -Id 't' -Title 't' -Section 's' -Priority low `
                -Description @('Line 1', 'Line 2') -Estimate '2h'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*"estimate":*"2h"*' -and
                $Body -like '*Line 1*'
            }
        }

        It 'includes implementation tasks' {
            New-McpTodo -Id 't' -Title 't' -Section 's' -Priority medium `
                -ImplementationTasks @(
                    @{ task = 'Write tests'; done = $false },
                    @{ task = 'Implement'; done = $true }
                )
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*Write tests*' -and $Body -like '*Implement*'
            }
        }

        It 'includes dependencies' {
            New-McpTodo -Id 'api' -Title 'API' -Section 's' -Priority high `
                -DependsOn @('auth', 'db')
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*"dependsOn"*' -and $Body -like '*auth*'
            }
        }
    }

    # ── Update-McpTodo ────────────────────────────────────────────────────────

    Describe 'Update-McpTodo' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'sends PUT to the correct endpoint' {
            Update-McpTodo -Id 'fix-auth' -Title 'Updated'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Method -eq 'Put' -and $Uri -eq 'http://test:9999/mcp/todo/fix-auth'
            }
        }

        It 'only includes fields that were explicitly provided' {
            Update-McpTodo -Id 'fix-auth' -Priority critical
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*"priority":*"critical"*' -and
                $Body -notlike '*"title"*' -and
                $Body -notlike '*"section"*'
            }
        }

        It 'can update remaining text' {
            Update-McpTodo -Id 'x' -Remaining 'Need more tests'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*"remaining":*"Need more tests"*'
            }
        }

        It 'can mark as done with details' {
            Update-McpTodo -Id 'x' -Done $true -DoneSummary 'Completed' -CompletedDate '2025-01-15T00:00:00Z'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*"done":*true*' -and
                $Body -like '*"doneSummary":*"Completed"*' -and
                $Body -like '*"completedDate"*'
            }
        }
    }

    # ── Complete-McpTodo ──────────────────────────────────────────────────────

    Describe 'Complete-McpTodo' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'sends PUT with done=true, completedDate, and doneSummary' {
            Complete-McpTodo -Id 'fix-auth' -DoneSummary 'Auth fixed with JWT'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Method -eq 'Put' -and
                $Uri -eq 'http://test:9999/mcp/todo/fix-auth' -and
                $Body -like '*"done":*true*' -and
                $Body -like '*"doneSummary":*"Auth fixed with JWT"*' -and
                $Body -like '*"completedDate"*'
            }
        }

        It 'sets completedDate to UTC ISO 8601' {
            Complete-McpTodo -Id 'x' -DoneSummary 's'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -match '"completedDate":\s*"\d{4}-\d{2}-\d{2}T'
            }
        }
    }

    # ── Remove-McpTodo ────────────────────────────────────────────────────────

    Describe 'Remove-McpTodo' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'sends DELETE to the correct endpoint' {
            Remove-McpTodo -Id 'old-todo'
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Method -eq 'Delete' -and $Uri -eq 'http://test:9999/mcp/todo/old-todo'
            }
        }
    }

    # ── Add-McpTodoRequirements ───────────────────────────────────────────────

    Describe 'Add-McpTodoRequirements' {
        BeforeEach {
            Initialize-McpTodo -BaseUrl 'http://test:9999' -ApiKey 'k'
        }

        It 'posts functional requirements' {
            Add-McpTodoRequirements -Id 'api' -FunctionalRequirements @('FR-001', 'FR-002')
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Method -eq 'Post' -and
                $Uri -eq 'http://test:9999/mcp/todo/api/requirements' -and
                $Body -like '*FR-001*'
            }
        }

        It 'posts technical requirements' {
            Add-McpTodoRequirements -Id 'api' -TechnicalRequirements @('TR-010')
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*TR-010*'
            }
        }

        It 'posts both requirement types together' {
            Add-McpTodoRequirements -Id 'api' `
                -FunctionalRequirements @('FR-001') `
                -TechnicalRequirements @('TR-001')
            Should -Invoke Invoke-RestMethod -ModuleName McpTodo -ParameterFilter {
                $Body -like '*FR-001*' -and $Body -like '*TR-001*'
            }
        }
    }
}
