# MCP-HANDOFF-001 validation receipt 20260816T185657Z

Source log: `docs/receipts/mcp-handoff-001-validation-20260816T185657Z.log`

MCP-HANDOFF-001 remains not done. No commit was created.

## Command exit codes

- Client.Tests: exit 0
- Support.Mcp.Tests: exit 1
- Repl.Core.Tests: exit 0
- Support.Mcp.IntegrationTests: exit 1
- Repl.IntegrationTests: exit 1
- Compile: exit 0
- build.ps1 Test: exit -1 (Nuke Test failed because Support.Mcp.Tests had failures)
- ValidateTraceability: exit 0 (Traceability validation passed)
- SyncAgentPlugins: exit 0 (core integrity OK on staged plus plugin roots)

## Counts from Passed! / Failed! lines

- Client.Tests: Failed 0, Passed 281, Skipped 0, Total 281
- Support.Mcp.Tests: Failed 3, Passed 1886, Skipped 0, Total 1889
- Repl.Core.Tests: Failed 0, Passed 823, Skipped 0, Total 823
- Support.Mcp.IntegrationTests: Failed 5, Passed 259, Skipped 0, Total 264
- Repl.IntegrationTests: Failed 1, Passed 180, Skipped 0, Total 181
- Focused Handoff filter (earlier same session): Client 5/5, Support.Mcp Handoff 31/31, Repl.Core Handoff 1/1, all Skipped 0
- Nuke Test Support.Mcp.Tests slice: Failed 2, Passed 1849, Skipped 0, Total 1851 (filter Category!=AiReview and Category!=Integration)

## Unrelated failures (not Handoff tests)

- SessionLogSanitizerTimeoutTests.SanitizeSessionLog_WhenOneFieldTimesOut_ContinuesSanitizingOtherFields
- WorkspacePolicyServiceTests.ApplyAsync_ValidDirective_UpdatesWorkspaceAndLogsPolicyChange
- TranscriptMcpStdioHostTests.SessionLogNormalizePath_ThroughStdioHost_ResolvesToolGraphAndWritesArtifacts
- QuadBrainLiveEndpointIntegrationTests planFile omitted BadRequest
- QuadBrainOllamaEndpointIntegrationTests (4 failures, planFile/todoId BadRequest)
- Iteration3IntegrationTests.TodoWorkflow_Query_ReturnsItems (YamlDotNet multiline scalar)

## Requirement receipts

Plugin `workflow.requirements.getFr/getTr/getTest/listMappings` on 2026-08-16T18:57Z-18:59Z returned all of:

- FR-HANDOFF-001 through FR-HANDOFF-007 with structured acceptanceCriteria
- TR-HANDOFF-CONTRACT-001, SECURITY-001, AGENT-001, VALIDATE-001, MODES-001, TODO-001, AUDIT-001, SURFACE-001
- TEST-HANDOFF-001 through TEST-HANDOFF-007
- Mappings matching the requested FR -> TR -> TEST graph

Generated projection: `docs/Project/TR-per-FR-Mapping.md` lines 5-11.
