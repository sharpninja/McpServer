#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts'
$stamp = '20260818T155200Z'
$mdPath = Join-Path $outDir ("hostile-validator-{0}.md" -f $stamp)
$jsonPath = Join-Path $outDir ("hostile-validator-{0}.json" -f $stamp)

$receipt = [ordered]@{
    TimestampUtc = '2026-08-18T15:52:00Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = $workspace
    WorkClass = 1
    WorkClassLabel = 'project requirement work; MCP-PRODUCTS-001 Phase 4 / H4-red only'
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
    accuracyRating = 96
    completenessRating = 94
    sessionId = 'GrokCode-20260818T154849Z-h4-red-products'
    requestId = 'req-20260818T154849Z-001-hostile-h4-red-products'
    turnId = 41778
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    plugin = [ordered]@{
        root = 'F:\GitHub\mcpserver-grok-plugin'
        pluginJsonVersion = '1.93.0'
        versionFile = '1.93.0'
        registryExactName = 'mcpserver-grok-plugin'
    }
    markerSignature = $true
    healthNonceThisReview = 'h4red16b22029a9ac4e448821f779bc8'
    healthNonceEchoed = $true
    healthStatus = 'Healthy'
    healthStorageThisReview = 'reachable'
    healthVersion = '1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e'
    contextFilter = [ordered]@{
        command = 'dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~ProductRequirementContextTests'
        listUtcStart = '2026-08-18T15:49:30.1176911Z'
        listUtcEnd = '2026-08-18T15:49:40.9585660Z'
        listExitCode = 0
        listNamedCount = 4
        utcStart = '2026-08-18T15:49:41.0144485Z'
        utcEnd = '2026-08-18T15:49:55.1946726Z'
        exitCode = 1
        failed = 4
        passed = 0
        skipped = 0
        total = 4
        compiled = $true
        compileErrorLineCount = 0
        notImplementedLineCount = 8
        rerunUtcStart = '2026-08-18T15:51:41.0954167Z'
        rerunUtcEnd = '2026-08-18T15:51:53.4370769Z'
        rerunExitCode = 1
        rerunSummary = 'Failed!  - Failed:     4, Passed:     0, Skipped:     0, Total:     4, Duration: 2 s - McpServer.Support.Mcp.Tests.dll (net10.0)'
        namedCases = @(
            'HandleAsync_Member_IncludesSiblingFrBodyAndOrigin'
            'HandleAsync_Member_DoesNotIncludeSiblingSourceFiles'
            'HandleAsync_Outsider_DoesNotIncludeSiblingFr'
            'HandleAsync_ProductRequirementsSource_ReturnsOnlyRequirementChunks'
        )
    }
    testFile = [ordered]@{
        path = 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
        exists = $true
        lastWriteUtc = '2026-08-18T15:44:47.4140744Z'
        bytes = 7147
        skipAttrCount = 0
    }
    handlerStub = [ordered]@{
        path = 'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs'
        exists = $true
        lastWriteUtc = '2026-08-18T15:44:27.9008226Z'
        bytes = 1904
        failureNotImplementedCount = 1
        failureLine = 'return Task.FromResult(Result<IReadOnlyList<ProductRequirementChunkDto>>.Failure("not implemented"));'
        srcCtxHitCount = 8
        srcOnlyThisFile = $true
    }
    todo = [ordered]@{
        id = 'MCP-PRODUCTS-001'
        done = $false
        completedDate = $null
        doneSummary = $null
        implementationTaskCount = 5
        implementationTasksAllDoneFalse = $true
        remaining = 'Phase 0 requirements created in store and exported. H0 hostile required before Phase 1. No product implementation started.'
    }
    requirements = [ordered]@{
        frTotal = 277
        trTotal = 406
        testTotal = 422
        mappingTotal = 277
        fr005Status = 'pending'
        fr005AcSatisfied = $false
        trCtx001Status = 'pending'
        test006Status = 'pending'
        mappingFr005 = [ordered]@{
            trIds = @('TR-MCP-PRODUCT-CTX-001')
            testIds = @('TEST-MCP-PRODUCT-006')
        }
    }
    planSha256 = 'E233F9E34BCA0A7176284FB0DE0E11BF2A186D04F479CF7C8E2CC089F72FB805'
    goalPlanSha256 = '0D73B5C6B754DEC494F4EAB445AD2A6EEB73D2F2923260366E07D4A9351FD92C'
    priorAgree = [ordered]@{
        h3Green = 'docs/receipts/hostile-validator-20260818T154000Z.md'
        h3GreenSha256 = '234EEFAEC6C5E4DAB18B36A7C23D7F7C7B73B6BA6C62401A83B594E15D5AC56E'
    }
    surfaces = [ordered]@{
        A = [ordered]@{
            A1 = 'PASS'
            A2 = 'PASS'
            A3 = 'PASS'
            A4 = 'PASS'
        }
        B = [ordered]@{
            B1 = 'PASS'
            B2 = 'PASS'
            B3 = 'PASS'
            B4 = 'PASS'
            B5 = 'PASS'
            B6 = 'PASS'
        }
        C = [ordered]@{
            C1 = 'PASS'
            C2 = 'PASS'
            C3 = 'PASS'
            C4 = 'PASS'
            C5 = 'PASS'
        }
        D = [ordered]@{
            D1 = 'PASS'
            D2 = 'PASS'
        }
    }
    failList = @()
    unknownList = @(
        'Full ./build.ps1 Test not run. Not required to exit H4-red.'
        'HybridSearch/pack wiring not present and not claimed. H4-green surface.'
    )
    passCount = 17
    failCount = 0
    unknownCount = 2
}

$json = $receipt | ConvertTo-Json -Depth 10
Set-Content -LiteralPath $jsonPath -Value $json -Encoding utf8

$md = @'
# Hostile Validator Receipt

TimestampUtc: 2026-08-18T15:52:00Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 1 (project requirement work; MCP-PRODUCTS-001 Phase 4 / H4-red only). Not MCP-PRODUCTS-001 done. Not Phase 4 green. Not Phase 5. Not full ./build.ps1 Test.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Files: PROFILE.md, user-payton-byrd.md, accuracy-first-verify-sources.md, approve-before-execute.md, philosophical-dialogue-mode.md, log-decisions-as-conclusions.md, session-turn-title-summary.md, never-skip-explicit-actions.md, adversarial-review-global.md, bring-the-receipts.md, hostile-on-goal-state.md, hostile-ops-vs-requirements.md, hostile-phase-gates.md, lab-authorization.md, no-attitude-honesty-tell.md, no-python-lab.md, no-shortcuts-precision-over-convenience.md, requirement-change-plan-first.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.93.0; .version 1.93.0). Tool registry GET /mcpserver/tools/search?keyword=mcpserver-grok-plugin HTTP 200; exact name mcpserver-grok-plugin is present.
Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Marker signature: Test-MarkerSignature True (pwsh, MarkerFile param, marker-resolver.ps1)
Health (this review): nonce h4red16b22029a9ac4e448821f779bc8 echoed exactly; status Healthy; version 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e; storage=reachable
SessionId: GrokCode-20260818T154849Z-h4-red-products
RequestId: req-20260818T154849Z-001-hostile-h4-red-products
turnId: 41778
planFile: docs/plans/mcp-products-001.md
todoId: MCP-PRODUCTS-001
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently listed and ran FullyQualifiedName~ProductRequirementContextTests, read ProductRequirementContextTests.cs and GetProductRequirementContextQuery.cs, grepped src for product-requirements / GetProductRequirementContext / ProductRequirementChunkDto, queried todo_get plus FR/TR/TEST/mappings through native MCP tools, and re-read the approved plan plus H3-green 20260818T154000Z. Implementer chat was not trusted.

This review did not implement product features. This review wrote only this receipt pair, collector scripts under docs/receipts/_hv-h4-red-*, and the MCP review turn.

Accuracy rating: 96/100. Named cases, stub Failure("not implemented"), independent Failed 4 Passed 0 Skipped 0, compile success, and TODO Done=false were re-verified on this pass.
Completeness rating: 94/100. Surfaces A-D and the named H4-red attacks were evaluated. Did not run the full unit suite (H4-red gate is the named context tests shown red). Did not require HybridSearch/pack wiring (that is H4-green).

## Classification

Class 1. Phase 4 red context tests for MCP-PRODUCTS-001 (FR-MCP-PRODUCT-005, TR-MCP-PRODUCT-CTX-001, TEST-MCP-PRODUCT-006). Surface C applies. Byrd v4 is scored at this H4-red gate: AC-covering context tests must exist and fail for the right reason against a stub, not compile errors, and not a working implementation.

Prior H3-green AGREE: docs/receipts/hostile-validator-20260818T154000Z.md
Prior H3-red AGREE: docs/receipts/hostile-validator-20260818T152430Z.md
Prior H2-green AGREE: docs/receipts/hostile-validator-20260818T150200Z.md
Prior H2-red AGREE: docs/receipts/hostile-validator-20260818T144836Z.md
Prior H1-green AGREE: docs/receipts/hostile-validator-20260818T143053Z.md
Prior H1-red AGREE: docs/receipts/hostile-validator-20260818T140630Z.md
H0 AGREE: docs/receipts/hostile-validator-20260818T132341Z.md

## Claims reviewed

### A Requested

A1. tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs exists with named cases: member includes sibling FR body + origin; member does not include sibling .cs chunks; outsider does not include sibling FR; source type product-requirements returns only those chunks.
Verdict: PASS
Evidence: File LastWriteUtc 2026-08-18T15:44:47.4140744Z Bytes=7147. FACT_COUNT=4 SKIP_ATTR_COUNT=0. Independent --list-tests 2026-08-18T15:49:30.1176911Z to 15:49:40.9585660Z EXIT=0 listed exactly:
- HandleAsync_Member_IncludesSiblingFrBodyAndOrigin (SIBLING-FR-BODY-UNIQUE + OriginWorkspaceId == Sibling + SourceType product-requirements)
- HandleAsync_Member_DoesNotIncludeSiblingSourceFiles (DoesNotContain class Secret / Secret.cs)
- HandleAsync_Outsider_DoesNotIncludeSiblingFr
- HandleAsync_ProductRequirementsSource_ReturnsOnlyRequirementChunks (Assert.All SourceType == product-requirements)
Plan H4-red names the first three. The fourth maps to FR-MCP-PRODUCT-005 ac-3 / TEST-MCP-PRODUCT-006 source-type filter. After H3-green (15:40:00Z), which recorded this file as ABSENT.

A2. GetProductRequirementContextQueryHandler returns Failure("not implemented").
Verdict: PASS
Evidence: src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs LastWriteUtc 2026-08-18T15:44:27.9008226Z Bytes=1904. Line 43: return Task.FromResult(Result<IReadOnlyList<ProductRequirementChunkDto>>.Failure("not implemented")); FAILURE_NOT_IMPLEMENTED_COUNT=1. Handler takes no McpDbContext. Grep ProductRequirementChunkDto / GetProductRequirementContextQueryHandler in *.cs: only this stub and the four test constructions. src product-requirements / GetProductRequirementContext hits are confined to this file (SRC_CTX_HIT_COUNT=8). No HybridSearch/pack wiring.

A3. `dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~ProductRequirementContextTests` Failed 4 Passed 0 Skipped 0, error "not implemented", compiled.
Verdict: PASS
Evidence: Independent run 2026-08-18T15:49:41.0144485Z to 15:49:55.1946726Z compiled McpServer.Support.Mcp.Tests.dll; COMPILE_ERROR_LINE_COUNT=0; Total tests: 4 Failed: 4; NOT_IMPLEMENTED_LINE_COUNT=8; all four failures Error Message: not implemented at Assert.True(result.IsSuccess, result.Error). Independent default-logger rerun 2026-08-18T15:51:41.0954167Z to 15:51:53.4370769Z: Failed!  - Failed:     4, Passed:     0, Skipped:     0, Total:     4, Duration: 2 s - McpServer.Support.Mcp.Tests.dll (net10.0) RUN2_EXIT=1. Failures are after CreateProduct/AddMember succeed (error is the stub message, not a create failure).

A4. MCP-PRODUCTS-001 Done=false.
Verdict: PASS
Evidence: Native todo_get via /mcp-transport. Id=MCP-PRODUCTS-001 Done=false CompletedDate=null DoneSummary=null. Five ImplementationTasks all Done=false. Remaining still says "H0 hostile required before Phase 1. No product implementation started." (stale remaining note, not a done-state lie). Implementer did not claim Phase 4 green, TODO done, or full suite.

### B Workspace rules

B1-honesty. Claims match artifacts.
Verdict: PASS
Rule: AGENTS.md honesty; profile accuracy-first-verify-sources.
Evidence: Named cases, stub text, Failed 4 Passed 0 Skipped 0, compile, and Done=false re-checked. Implementer did not claim TODO done, Phase 4 green, or full suite. Honesty notes (scored, not ignored): TODO Remaining text is stale; tests call the CQRS query handler rather than HybridSearchService pack (plan Phase 4 red file is ProductRequirementContextTests; H4-green is pack/search contribution). Neither is a done-state lie.

B2-receipts. Machine-verifiable evidence re-run.
Verdict: PASS
Rule: Always bring the receipts.
Evidence: This review's --list-tests, detailed run, default-logger rerun, file reads, greps, todo_get, requirements_list, Test-MarkerSignature, health nonce, tool registry search, tools/list (106 tools).

B3-MCP-only storage.
Verdict: PASS
Rule: MCP is the only interface to TODO/session/requirements.
Evidence: TODO, requirements, and session log used native sessionlog_*/todo_*/requirements_* via Streamable HTTP /mcp-transport after initialize. This review did not read or write docs/todo.yaml or session-log storage files.

B4-lab PowerShell / no Python.
Verdict: PASS
Rule: no-python-lab; pwsh.exe only.
Evidence: pwsh.exe -NoProfile path for signature, health, inventory, test runs, MCP transport client, and JSON serialize. No python/py invocation.

B5-look-before-delete.
Verdict: PASS
Evidence: No deletes. Review-only plus receipt and collector-script create.

B6-Byrd v4 phase-order at H4-red.
Verdict: PASS
Rule: hostile-phase-gates; tests covering AC shown red before implementation; score at the inter-phase gate.
Evidence: Prior H3-green AGREE exists (20260818T154000Z) and recorded ProductRequirementContextTests ABSENT. Phase 4 test/stub LastWriteUtc 15:44:27Z and 15:44:47Z are after that gate. Tests are red for Failure("not implemented"), not compile errors. Query handler is a stub only. Full ./build.ps1 Test is the H5-done gate, not H4-red.

### C Requirements

C1. FR/TR/TEST exist for this work.
Verdict: PASS
Evidence: Native requirements_list. FR_TOTAL=277 TR_TOTAL=406 TEST_TOTAL=422. PRODUCT subset present including FR-MCP-PRODUCT-005, TR-MCP-PRODUCT-CTX-001, TEST-MCP-PRODUCT-006.

C2. Structured AC exist on FR-005 and remain unsatisfied (correct; this is red, TODO is not done).
Verdict: PASS
Evidence: FR-MCP-PRODUCT-005 Status=pending. ac-1 pack for a member includes sibling FR body isSatisfied=false. ac-2 pack does not include sibling source file chunks isSatisfied=false. ac-3 source type product-requirements filters to those chunks isSatisfied=false. TR-MCP-PRODUCT-CTX-001 Status=pending, structured AC array empty, body AC: member pack includes sibling FR text; sibling .cs files are absent; product-requirements filter returns only those chunks. TEST-MCP-PRODUCT-006 Status=pending, structured AC empty, Condition names those three behaviors and File: ProductRequirementContextTests.

C3. Phase 4 AC-covering tests exist and are red (H4-red bar).
Verdict: PASS
Evidence:
- FR-005 ac-1 / TR-CTX member FR + origin: HandleAsync_Member_IncludesSiblingFrBodyAndOrigin (red, not implemented)
- FR-005 ac-2 / TEST-006 no sibling .cs: HandleAsync_Member_DoesNotIncludeSiblingSourceFiles (red, not implemented)
- FR-005 ac-3 / TEST-006 source type filter: HandleAsync_ProductRequirementsSource_ReturnsOnlyRequirementChunks (red, not implemented)
- Plan H4-red outsider isolation: HandleAsync_Outsider_DoesNotIncludeSiblingFr (red, not implemented)
Tests seed a sibling ContextDocument/chunk (class Secret) plus sibling FR body so the later green can prove CQRS share helper vs sibling ContextDocument rows.

C4. Mappings FR to TR/TEST exist.
Verdict: PASS
Evidence: requirements_list type=mapping: FR-MCP-PRODUCT-005 -> TR-MCP-PRODUCT-CTX-001 -> TEST-MCP-PRODUCT-006. Matches the approved plan set.

C5. New product context behavior has FR/TR/TEST. Implementer did not mark FR/TR/TEST completed.
Verdict: PASS
Evidence: Store IDs remain pending. TODO still links FR-001..005 and the five TRs. Status fields were not flipped to completed. isSatisfied remains false.

### D Plan holistically

D1. H4-red checkpoint is complete. Full MCP-PRODUCTS-001 DoD is not claimed.
Verdict: PASS
Evidence: Approved plan docs/plans/mcp-products-001.md SHA256 E233F9E34BCA0A7176284FB0DE0E11BF2A186D04F479CF7C8E2CC089F72FB805 (unchanged since H0/H1/H2/H3). H4-red attack text: "member requirement chunks vs no sibling source files." Named cases exist and fail on the stub. Phase 4 green (pack/search contribution; product-requirements source; no file leak; gate green) is not claimed.
Session goal plan.md SHA256 0D73B5C6B754DEC494F4EAB445AD2A6EEB73D2F2923260366E07D4A9351FD92C is unchanged. It still says MCP-PRODUCTS-001 must stay Done=false until H5-done AGREE.

D2. Did not start Phase 4 green implementation or mark the TODO done.
Verdict: PASS
Evidence: A2 and A4. Handler is Failure("not implemented"). No search/pack contribution.

## H4-red named attacks

- Member requirement chunks: PASS (named test asserts sibling FR body + origin; currently red on stub)
- No sibling source files: PASS (named test asserts absence of class Secret / Secret.cs; currently red on stub)
- Non-member isolation: PASS (named outsider test; currently red on stub)
- Tests fail for the right reason: PASS (all four Error Message: not implemented; compiled; Passed 0 Skipped 0)

## Explicit FAIL list

None.

## UNKNOWN / unevaluated

- Full `./build.ps1 Test` not run. Not required to exit H4-red.
- HybridSearch/pack wiring not present and not claimed. That is the H4-green surface.

## Session-log persistence proof

Native MCP tools over POST http://PAYTON-LEGION2:7147/mcp-transport (initialize, tools/list, tools/call). Agent GrokCode, workspace F:\GitHub\McpServer. tools/list unique name count 106 including sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_complete_turn, sessionlog_query, todo_get, requirements_list.

- initialize protocolVersion 2025-03-26 HTTP 200
- sessionlog_open GrokCode-20260818T154849Z-h4-red-products created=true
- sessionlog_begin_turn requestId req-20260818T154849Z-001-hostile-h4-red-products turnId=41778 status=in_progress
- sessionlog_dialog / replace_section / complete_turn and query proof are appended after this receipt write (see collector _hv-h4-red-mcp3.ps1 / _hv-h4-red-query-proof.json)

## Files written by this review

- docs/receipts/hostile-validator-20260818T155200Z.md
- docs/receipts/hostile-validator-20260818T155200Z.json
'@

Set-Content -LiteralPath $mdPath -Value $md -Encoding utf8
Write-Output ('WROTE_MD=' + $mdPath)
Write-Output ('WROTE_JSON=' + $jsonPath)
Write-Output ('MD_BYTES=' + (Get-Item -LiteralPath $mdPath).Length)
Write-Output ('JSON_BYTES=' + (Get-Item -LiteralPath $jsonPath).Length)
Write-Output ('MD_SHA256=' + (Get-FileHash -LiteralPath $mdPath -Algorithm SHA256).Hash)
Write-Output ('JSON_SHA256=' + (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash)
