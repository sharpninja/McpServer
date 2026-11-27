# TEST-MCP-BUGTRIAGE

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-BUGTRIAGE

### TEST-MCP-BUGTRIAGE-042

Grok CLI one-shot argument construction forwards an explicitly configured model. GrokCliAgentExecutionStrategyTests.BuildGrokArgumentList_ConfiguredModel_IncludesModelFlag verifies a real model name (for example grok-4.3) emits --model before --output-format in the one-shot argument list (validates TR-MCP-TRIAGE-003 runner invocation construction).


### TEST-MCP-BUGTRIAGE-043

Grok CLI startup-rejection guards for the triage research runner (validates TR-MCP-TRIAGE-003). GrokCliAgentExecutionStrategyTests verify: the sentinel model value auto (any casing) or an empty model omits --model entirely so the CLI picks its default, because current Grok CLIs reject --model auto with "unknown model id" and the runner substitutes auto for unset tier models (BuildGrokArgumentList_AutoOrEmptyModel_OmitsModelFlag, 5 cases); effort flags are pinned to high because current Grok CLIs reject max with "unknown effort level" (BuildGrokArgumentList_ContainsExpectedFlagsInOrder). Evidence 2026-07-14: both rejections reproduced from captured run stderr; fixes deployed in 1.4.15/1.4.16; live research run for triage-group-27f5ecfe4c926fde completed exit 0 at 20:08Z, first successful run since 2026-07-07.
