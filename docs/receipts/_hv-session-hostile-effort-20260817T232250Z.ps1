#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$sessionId = 'GrokCode-20260817T232250Z-hostile-effort'
$requestId = 'req-20260817T232250Z-001-hostile-validate-effort'
$utcNow = [DateTime]::UtcNow.ToString('o')

function Invoke-Hv {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)]$Params,
        [int]$TimeoutSeconds = 120
    )
    Write-Output ("---- METHOD {0} ----" -f $Method)
    try {
        & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds $TimeoutSeconds
        Write-Output ("EXIT_OK {0}" -f $Method)
    } catch {
        Write-Output ("EXIT_FAIL {0}: {1}" -f $Method, $_.Exception.Message)
    }
}

Invoke-Hv -Method 'workflow.sessionlog.bootstrap' -Params @{}
Invoke-Hv -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile validate Agent Help effort high'
    model = 'grok'
}
Invoke-Hv -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile validate Agent Help effort-high claims'
    queryText = 'Hostile validator: attack implementer claims that Agent Help effort is already high and that no YAML or product-code change was required.'
}
Invoke-Hv -Method 'workflow.sessionlog.appendDialog' -Params @{
    dialogItems = @(
        @{
            timestamp = $utcNow
            role = 'model'
            content = 'Classified work as class 2 user-directed ops. Surface C N/A. Surface D N/A. add-profile executed first: 18 non-skill profile files read.'
            category = 'observation'
        },
        @{
            timestamp = $utcNow
            role = 'model'
            content = 'Decision: do not write HelperEffort and do not change product code during this review. Consequence: review-only receipts plus a dedicated GrokCode hostile session. Alternatives rejected: inventing a live YAML knob, restarting McpServer, treating OneShotCli GrokHighestEffort=max as Agent Help behavior.'
            category = 'decision'
        },
        @{
            timestamp = $utcNow
            role = 'model'
            content = 'Re-read AgentHelpOptions, GrokCliAgentExecutionStrategy, live ProgramData appsettings.yaml, official xAI reasoning docs, grok --help, and the deployed exe. OverallVerdict AGREE. Fail list empty.'
            category = 'reasoning'
        }
    )
}
Invoke-Hv -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{
            order = 1
            description = 'add-profile executed; 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile'
            type = 'design_decision'
            status = 'completed'
            filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md'
        },
        @{
            order = 2
            description = 'web_reference: https://docs.x.ai/developers/model-capabilities/text/reasoning grok-4.5 low/medium/high default high; xhigh is 4.6+'
            type = 'web_reference'
            status = 'completed'
            filePath = 'https://docs.x.ai/developers/model-capabilities/text/reasoning'
        },
        @{
            order = 3
            description = 'web_reference: https://docs.x.ai/build/cli/reference documents --effort <LEVEL>; local grok --help shows --reasoning-effort with alias --effort'
            type = 'web_reference'
            status = 'completed'
            filePath = 'https://docs.x.ai/build/cli/reference'
        },
        @{
            order = 4
            description = 'Decision: class 2 ops review; OverallVerdict AGREE because A+B passed and C/D are N/A; no product edits'
            type = 'design_decision'
            status = 'completed'
            filePath = ''
        }
    )
}
Invoke-Hv -Method 'workflow.sessionlog.updateTurn' -Params @{
    interpretation = 'Operator-directed hostile validation of Agent Help effort-high claims. Class 2 ops. No product implementation and no plan-step completion claimed.'
    response = 'OverallVerdict AGREE. Receipt: docs/receipts/hostile-validator-20260817T232829Z.md'
    tags = @('hostile-validator', 'agent-help', 'effort', 'class-2', 'AGREE')
    contextList = @(
        'src/McpServer.Services/Options/AgentHelpOptions.cs',
        'src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs',
        'C:\ProgramData\McpServer\appsettings.yaml',
        'https://docs.x.ai/developers/model-capabilities/text/reasoning',
        'docs/receipts/hostile-validator-20260817T232829Z.md'
    )
}
Invoke-Hv -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = 'Hostile validation AGREE. Receipt docs/receipts/hostile-validator-20260817T232829Z.md. All applicable A+B claims PASS. C and D N/A. Fail list empty. Proved via queryHistory and client.SessionLog.QueryAsync (sessionlog_query backend).'
}

Write-Output '==== QUERY HISTORY ===='
Invoke-Hv -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = 'GrokCode'
    limit = 10
    offset = 0
}

Write-Output '==== CLIENT QUERY ASYNC (sessionlog_query backend) ===='
Invoke-Hv -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    text = 'hostile-effort'
    limit = 10
}

Write-Output 'SESSION_SCRIPT_DONE'
