# Projected problems before hostile validation round 6 (IMPL-UNITGREEN-001), 2026-10-21

Prepared by ClaudeCode after the holistic read of plan sections 1 to 19, the round 6 diff (9933796c, c8418301) and the what-if pass of section 19.5. Each item says what a hostile reviewer could raise and what stands behind it. Nothing below is a known failing gate: every gate at c8418301 is green with receipts.

## Fixed before this list (found while preparing round 6)

- R6-P1, gate v3 lacked some keys of the moved tests. The chosen option says each moved test's keys go into the hostile gate; v3 carried the four probe names but not Mcp__Port, QBrainAi__Port or the HostileReviewWorker settings. Gate v4 (126 variables) adds them: ug-r6-hostile-wide-3, 4558/0/0 at the docs head 44e9d810 (code-identical to c8418301).
- R6-P2, wording. TODO task 22 and the brief said "IsReachable and its seven callers"; there are seven scan roots checked by four call sites. Both corrected (TODO read back 14:24:02).

## Declared, with evidence (a reviewer may still weigh them)

- R6-P3, the sweep's list is not discovered by a test. The writer list in ProcessEnvironmentCollectionTests and the 13 moves come from the one-time search (receipt ug-r6-env-sweep.txt); a new environment-writing class must be added by hand. A discovery test would be a mechanical enforcer, which the operator ruled out at 07:12:22 ("It is a workspace rule, not something that needs to be mechanically enforced.").
- R6-P4, one reader outside the collection. HostileReviewWorker_InvalidOptions_FailStartup reads the whole environment through a default host builder, so it is an integration test, but it writes nothing and its assertion depends on no variable, so it is not in the writers' collection. The criteria serialize writers; writers run alone, so a reader cannot see a transient write.
- R6-P5, judged cases. NgrokTunnelProviderEnvironmentTests (6 cases) stay unit tests: their ProcessStartInfo assertions read only values the stub service or options set. The CliBrainSlotStrategyTests recording spawner strips inherited keys from its copies. The receipt states each judgment.
- R6-P6, a variable that cannot be scripted. BrainSlotCredentialResolverTests' environment case makes up a random variable name per run, so no hostile gate can pre-set it; it is an integration test that sets and clears its own variable.
- R6-P7, a writer outside the gates. QBrainAi.PluginIntegration.Tests has one environment writer (PLUGIN_ROOT_OVERRIDE, restored), already in its serialized PluginSessionLog collection; that project runs only under PluginSessionLogIntegration, which is not a gate of this PR, and it is unchanged.
- R6-P8, gate v4's head. v4 ran at 44e9d810, a docs-only commit after the last code head c8418301 (`git diff --name-only c8418301..44e9d810` lists only docs/), like the round 5 negative control at a code-identical head. Every other gate ran at c8418301.
- R6-P9, the reachability boundary. The profile or home itself is trusted and never checked, so a linked home is still scanned (the strengthened criteria say so). A root that is not below the boundary, or whose path form differs from the listing's (for example a relative profile path), is unreachable and falls back; the profile comes from the platform as an absolute path.
- R6-P10, the round 5 evidence file. hv-ug-r5-test-results.json (98.19 MB) is in branch history; removing it needs a force push, which is forbidden. Every later commit is size-checked (round 6 receipts: 0.2 MB).
- R6-P11, my process slips (all disclosed in the self-review): the round 5 launch ran before add-profile after the compaction; two Bash ls commands; the round 5 receipts committed without a size check; one log copy without its dry run; commit B and a restore issued in one parallel batch (the restore ran after the commit); my round 5 question recommended no AC change.
- R6-P12, carried: the race cause stays an inference, flagged as a risk ("Then flag the risk and roll on."); the TR-MCP-SESSIONLOG-006 store drift (1 criterion here, 8 in the export, triage 647e35bb); the runs without a receipt are failed, not evidence; the scanner stays out of scope by the 06:06:46 override (IMPL-SCANNERALIAS-001 open, no work scheduled); the requirements wiki export's Azure-related failure is information, not a gate.

## Round 5's own receipt gap

- R6-P13, null ETA. Every round 5 progress entry before 100% had etaSeconds null. The round 6 brief requires a non-negative number in every entry and says null is not allowed.

## What happens next if you confirm

- Commit the round 6 prelaunch records (brief, self-review, this list, the gate v4 receipt and the plan's v4 lines; docs only, signed, normal push), publish PR body r7, then launch round 6: Codex gpt-6-astra max, gate unitgreen-r6, unbounded review time.
