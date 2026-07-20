#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$WorkspacePath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).ProviderPath,

    [string]$OutputPath = (Join-Path (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).ProviderPath 'config\brain-slots\quad-brain-slot-assignments.yaml'),

    [string]$BaseUrl = 'http://PAYTON-LEGION2:7147',

    [string]$ApiKey = $env:MCP_API_KEY,

    [string]$BearerToken = $env:MCP_BEARER_TOKEN,

    [string]$AotEndpoint = $env:MCP_BRAIN_AOT_ENDPOINT,

    [string]$ClaudeCodeEndpoint = $env:MCP_BRAIN_CLAUDE_CODE_ENDPOINT,

    [string]$CodexEndpoint = $env:MCP_BRAIN_CODEX_ENDPOINT,

    [string]$AotCredentialReference = 'env:MCP_BRAIN_AOT_API_KEY',

    [string]$ClaudeCodeCredentialReference = 'env:MCP_BRAIN_CLAUDE_CODE_API_KEY',

    [string]$CodexCredentialReference = 'env:MCP_BRAIN_CODEX_API_KEY',

    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command ConvertTo-Yaml -ErrorAction SilentlyContinue)) {
    throw 'ConvertTo-Yaml is required. Load the workspace PowerShell YAML module before running this script.'
}

function Get-ConfiguredValue {
    param(
        [string]$Value,
        [Parameter(Mandatory)][string]$Default
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Default
    }

    return $Value
}

function ConvertTo-YamlText {
    param([Parameter(Mandatory)][object]$InputObject)

    $yaml = ConvertTo-Yaml $InputObject
    if ($yaml -is [array]) {
        return ($yaml -join [Environment]::NewLine)
    }

    return [string]$yaml
}

function New-BrainSlotAssignment {
    param(
        [Parameter(Mandatory)][string]$SlotId,
        [Parameter(Mandatory)][string]$Role,
        [Parameter(Mandatory)][string]$RoleAlias,
        [Parameter(Mandatory)][string]$DisplayName,
        [Parameter(Mandatory)][string]$AdapterName,
        [Parameter(Mandatory)][string]$ModelId,
        [Parameter(Mandatory)][string]$Endpoint,
        [Parameter(Mandatory)][string]$EndpointEnvironmentVariable,
        [Parameter(Mandatory)][string]$CredentialReference,
        [Parameter(Mandatory)][string]$PartyId,
        [Parameter(Mandatory)][string]$SystemPrompt,
        [int]$TimeoutSeconds = 180,
        [int]$MaxOutputTokens = 4096
    )

    $upsertRequest = [pscustomobject][ordered]@{
        role = $Role
        displayName = $DisplayName
        providerKind = 'OpenAICompatible'
        modelId = $ModelId
        endpoint = $Endpoint
        credentialReference = $CredentialReference
        partyId = $PartyId
        enabled = $true
        timeoutSeconds = $TimeoutSeconds
        maxOutputTokens = $MaxOutputTokens
        systemPrompt = $SystemPrompt
        orchestrationWeight = 1.0
        replaceExisting = $true
    }

    return [pscustomobject][ordered]@{
        slotId = $SlotId
        role = $Role
        roleAlias = $RoleAlias
        assignedRuntime = $AdapterName
        modelId = $ModelId
        providerKind = 'OpenAICompatible'
        endpoint = $Endpoint
        endpointEnvironmentVariable = $EndpointEnvironmentVariable
        credentialReference = $CredentialReference
        partyId = $PartyId
        enabled = $true
        replaceExisting = $true
        upsertRequest = $upsertRequest
    }
}

$aotEndpointValue = Get-ConfiguredValue -Value $AotEndpoint -Default 'http://127.0.0.1:8311/v1'
$claudeEndpointValue = Get-ConfiguredValue -Value $ClaudeCodeEndpoint -Default 'http://127.0.0.1:8312/v1'
$codexEndpointValue = Get-ConfiguredValue -Value $CodexEndpoint -Default 'http://127.0.0.1:8313/v1'

$slots = @(
    New-BrainSlotAssignment `
        -SlotId 'brain-slot-arbiter-of-truth-grok-build' `
        -Role 'ArbiterOfTruth' `
        -RoleAlias 'AoT' `
        -DisplayName 'Arbiter of Truth - Grok Build' `
        -AdapterName 'Grok Build' `
        -ModelId 'grok-build' `
        -Endpoint $aotEndpointValue `
        -EndpointEnvironmentVariable 'MCP_BRAIN_AOT_ENDPOINT' `
        -CredentialReference $AotCredentialReference `
        -PartyId 'brain-slot:arbiter-of-truth' `
        -SystemPrompt 'You are the ArbiterOfTruth brain slot. Reconcile committed role evidence, identify contradictions, require traceable support, and return the smallest defensible decision.'

    New-BrainSlotAssignment `
        -SlotId 'brain-slot-curiosity-engine-claude-code-opus-4-8' `
        -Role 'CuriosityEngine' `
        -RoleAlias 'Researcher' `
        -DisplayName 'Researcher - Claude Code CLI Opus 4.8' `
        -AdapterName 'Claude Code CLI' `
        -ModelId 'claude-code-cli-opus-4.8' `
        -Endpoint $claudeEndpointValue `
        -EndpointEnvironmentVariable 'MCP_BRAIN_CLAUDE_CODE_ENDPOINT' `
        -CredentialReference $ClaudeCodeCredentialReference `
        -PartyId 'brain-slot:curiosity-engine' `
        -SystemPrompt 'You are the CuriosityEngine researcher brain slot. Find missing evidence, challenge assumptions, surface unknowns, and label which findings are ready for GraphRAG admission after transaction commit.'

    New-BrainSlotAssignment `
        -SlotId 'brain-slot-creativity-claude-code-opus-4-8' `
        -Role 'Creativity' `
        -RoleAlias 'Creative' `
        -DisplayName 'Creativity - Claude Code CLI Opus 4.8' `
        -AdapterName 'Claude Code CLI' `
        -ModelId 'claude-code-cli-opus-4.8' `
        -Endpoint $claudeEndpointValue `
        -EndpointEnvironmentVariable 'MCP_BRAIN_CLAUDE_CODE_ENDPOINT' `
        -CredentialReference $ClaudeCodeCredentialReference `
        -PartyId 'brain-slot:creativity' `
        -SystemPrompt 'You are the Creativity brain slot. Generate alternatives, spot pattern-level opportunities, explore creative solution paths, and call out assumptions that need validation.'

    New-BrainSlotAssignment `
        -SlotId 'brain-slot-logic-codex-cli-gpt-5-5' `
        -Role 'Logic' `
        -RoleAlias 'Reasoner' `
        -DisplayName 'Logic - Codex CLI GPT-5.5' `
        -AdapterName 'Codex CLI' `
        -ModelId 'codex-cli-gpt-5.5' `
        -Endpoint $codexEndpointValue `
        -EndpointEnvironmentVariable 'MCP_BRAIN_CODEX_ENDPOINT' `
        -CredentialReference $CodexCredentialReference `
        -PartyId 'brain-slot:logic' `
        -SystemPrompt 'You are the Logic brain slot. Produce structured decomposition, deterministic checks, implementation sequencing, and risk-focused analysis.'
)

$document = [pscustomobject][ordered]@{
    version = 1
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    workspacePath = $WorkspacePath
    runtimeCompatibility = [pscustomobject][ordered]@{
        acceptedProviderKind = 'OpenAICompatible'
        reason = 'The current brain-slot runtime accepts OpenAI/OpenAICompatible providers; CLI assignments must be exposed through OpenAI-compatible local adapters.'
        requiresExecutionGate = 'Mcp:BrainSlots:ExecutionEnabled=true'
        requiresLoopbackGate = 'Mcp:BrainSlots:AllowLoopbackEndpoints=true'
    }
    appSettingsPatch = [pscustomobject][ordered]@{
        Mcp = [pscustomobject][ordered]@{
            BrainSlots = [pscustomobject][ordered]@{
                ExecutionEnabled = $true
                AllowLoopbackEndpoints = $true
                AllowedEndpointHosts = @('127.0.0.1', 'localhost')
                DefaultTimeoutSeconds = 180
                MaxTimeoutSeconds = 300
            }
        }
    }
    environment = [pscustomobject][ordered]@{
        MCP_BRAIN_AOT_ENDPOINT = $aotEndpointValue
        MCP_BRAIN_CLAUDE_CODE_ENDPOINT = $claudeEndpointValue
        MCP_BRAIN_CODEX_ENDPOINT = $codexEndpointValue
        MCP_BRAIN_AOT_API_KEY = 'set outside source control'
        MCP_BRAIN_CLAUDE_CODE_API_KEY = 'set outside source control'
        MCP_BRAIN_CODEX_API_KEY = 'set outside source control'
    }
    slots = $slots
    applyOrder = @(
        'brain-slot-arbiter-of-truth-grok-build',
        'brain-slot-curiosity-engine-claude-code-opus-4-8',
        'brain-slot-creativity-claude-code-opus-4-8',
        'brain-slot-logic-codex-cli-gpt-5-5'
    )
}

$yamlText = ConvertTo-YamlText $document
$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $yamlText -Encoding UTF8
Write-Output "Wrote $OutputPath"

if ($Apply) {
    if ([string]::IsNullOrWhiteSpace($ApiKey) -and [string]::IsNullOrWhiteSpace($BearerToken)) {
        throw '-Apply requires -ApiKey or -BearerToken. Brain-slot REST endpoints are AgentManager-protected.'
    }

    $headers = @{
        'X-Workspace-Path' = $WorkspacePath
    }
    if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
        $headers['X-Api-Key'] = $ApiKey
    }
    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        $headers['Authorization'] = "Bearer $BearerToken"
    }

    foreach ($slot in $slots) {
        $escapedSlotId = [Uri]::EscapeDataString($slot.slotId)
        $uri = ($BaseUrl.TrimEnd('/') + "/mcpserver/brain-slots/$escapedSlotId")
        $body = $slot.upsertRequest | ConvertTo-Json -Depth 8
        if ($PSCmdlet.ShouldProcess($slot.slotId, "PUT $uri")) {
            Invoke-RestMethod -Method Put -Uri $uri -Headers $headers -Body $body -ContentType 'application/json' | Out-Null
            Write-Output "Applied $($slot.slotId)"
        }
    }
}
