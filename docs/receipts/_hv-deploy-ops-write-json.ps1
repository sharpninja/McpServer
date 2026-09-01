#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$obj = [ordered]@{
    TimestampUtc = '2026-08-18T18:14:30Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 2
    WorkClassLabel = 'user-directed lab/ops (redeploy service, install REPL, sync plugins)'
    addProfile = [ordered]@{
        executed = $true
        profileFileCountRead = 18
        excluded = @('add-profile.grok.md')
        files = @(
            'PROFILE.md'
            'user-payton-byrd.md'
            'accuracy-first-verify-sources.md'
            'approve-before-execute.md'
            'philosophical-dialogue-mode.md'
            'log-decisions-as-conclusions.md'
            'session-turn-title-summary.md'
            'never-skip-explicit-actions.md'
            'adversarial-review-global.md'
            'bring-the-receipts.md'
            'hostile-on-goal-state.md'
            'hostile-ops-vs-requirements.md'
            'hostile-phase-gates.md'
            'lab-authorization.md'
            'no-attitude-honesty-tell.md'
            'no-python-lab.md'
            'no-shortcuts-precision-over-convenience.md'
            'requirement-change-plan-first.md'
        )
    }
    OverallVerdict = 'AGREE'
    accuracyRating = 98
    completenessRating = 97
    sessionId = 'GrokCode-20260818T181311Z-deploy-ops'
    requestId = 'req-20260818T181311Z-001-hostile-deploy-ops'
    turnId = 41850
    planFile = 'None'
    todoId = 'None'
    plugin = [ordered]@{
        root = 'F:\GitHub\mcpserver-grok-plugin'
        versionFile = '1.94.0'
        pluginJsonVersion = '1.94.0'
        cacheVersion = '1.94.0'
        coreVersion = '298c5fde'
        cachePath = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
    }
    markerSignature = $true
    markerSignatureValue = 'DAB0AC6970CA8AF6D864E6057AAB3C4C788DF2AECFD0BBC6DDEB0AF4959840D3'
    markerStartedAt = '2026-08-18T18:02:40.9427094+00:00'
    healthNonceThisReview = 'hv-deploy-ops-f76c604e7e204730b8b9d92725d14ac9'
    healthNonceEchoed = $true
    healthStatus = 'Healthy'
    healthStorageThisReview = 'reachable'
    healthVersion = '1.4.26+298c5fde3d1438ff7741ebec82ced796b207433e'
    serviceStatus = 'Running'
    deployment = [ordered]@{
        path = 'C:\ProgramData\McpServer\.mcpservice-deployment.json'
        generatedBy = 'build/Build.UpdateService.cs'
        generatedUtc = '2026-08-18T18:02:20.3911263Z'
        operation = 'update'
    }
    updateServiceLog = [ordered]@{
        path = 'F:\GitHub\McpServer\.nuke\temp\build.2026-08-18_13-00-49.log'
        wsHealthLine = 'WSHealth: OK (38/38)'
        deploymentVersion = '1.4.26'
        healthVersion = '1.4.26+298c5fde3d1438ff7741ebec82ced796b207433e'
        targetSucceeded = $true
    }
    workspaceHealth = [ordered]@{
        checked = 38
        enabled = 38
        disabled = 0
        healthy = 38
        liveFormula = 'enabled workspaces + shared /health Healthy'
    }
    replVersion = '1.4.26+298c5fde3d1438ff7741ebec82ced796b207433e'
    swagger = [ordered]@{
        pathCount = 264
        hasProducts = $true
        effectiveGetParams = @('layerKey', 'productScope')
        productsGetStatus = 200
        productsGetBody = '[]'
        effectiveGetStatus = 200
        effectiveHasProductScope = $true
    }
    pluginVersions = [ordered]@{
        grok = '1.94.0'
        claudeCode = '1.94.0'
        claudeCowork = '1.94.0'
        cline = '1.94.0'
        clineV2 = '1.94.0'
        codex = '1.94.0'
        copilot = '1.94.0'
        opencode = '1.94.0'
    }
    implementerReceipts = [ordered]@{
        installRepl = 'docs/receipts/_deploy-install-repl-20260818T180300Z.txt'
        syncPlugins = 'docs/receipts/_deploy-sync-plugins-20260818T180600Z.txt'
        updateServiceTxtMislabeled = 'docs/receipts/_deploy-update-service-20260818T180100Z.txt'
        updateServiceTxtIsSyncAgentPlugins = $true
        doNotFailMissingUpdateServiceTxt = $true
    }
    claims = @(
        [ordered]@{ id = 'A1'; surface = 'A'; verdict = 'PASS'; summary = 'UpdateService 1.4.26+298c5fde Running Healthy nonce match WSHealth 38/38' }
        [ordered]@{ id = 'A2'; surface = 'A'; verdict = 'PASS'; summary = 'InstallReplTool mcpserver-repl 1.4.26+298c5fde' }
        [ordered]@{ id = 'A3'; surface = 'A'; verdict = 'PASS'; summary = 'SyncAgentPlugins plugins 1.94.0 core 298c5fde grok cache refreshed' }
        [ordered]@{ id = 'A4'; surface = 'A'; verdict = 'PASS'; summary = 'swagger /mcpserver/products and GET 200 [] plus effective productScope' }
        [ordered]@{ id = 'B1'; surface = 'B'; verdict = 'PASS'; summary = 'Byrd N/A class 2' }
        [ordered]@{ id = 'B2'; surface = 'B'; verdict = 'PASS'; summary = 'Receipts re-verified' }
        [ordered]@{ id = 'B3'; surface = 'B'; verdict = 'PASS'; summary = 'MCP-only storage; TODOs not flipped' }
        [ordered]@{ id = 'B4'; surface = 'B'; verdict = 'PASS'; summary = 'pwsh only; no Python' }
        [ordered]@{ id = 'B5'; surface = 'B'; verdict = 'PASS'; summary = 'Honesty; mislabeled update-service txt not used as proof' }
        [ordered]@{ id = 'C'; surface = 'C'; verdict = 'N/A'; summary = 'Class 2 ops; FR/TR not required' }
        [ordered]@{ id = 'D'; surface = 'D'; verdict = 'N/A'; summary = 'planFile None; no plan-step claimed' }
    )
    failList = @()
    unknownSurfaces = @()
    passCount = 9
    failCount = 0
    unknownCount = 0
    naCount = 2
}

$json = $obj | ConvertTo-Json -Depth 8
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T181430Z.json'
[System.IO.File]::WriteAllText($path, $json)
Write-Output ("WROTE=" + $path + " LEN=" + $json.Length)
