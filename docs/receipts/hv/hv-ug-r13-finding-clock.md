# R13-F1: the voice-session reuse unit test still depends on live expiry

Classification: implementer edit. Source inspection only; no test or build was run by this reviewer.

At the reviewed head, the untagged unit fact `VoiceConversationServiceTests.CreateSessionAsync_SameAgentAndWorkspace_ReusesExistingIdleSession` creates a service, creates a session twice, and asserts equal session IDs (tests/QBrainAi.Support.Mcp.Tests/Services/VoiceConversationServiceTests.cs:18-39). Its fixture configures a real fifteen-minute idle timeout and supplies no clock or scheduler (same file:226-273).

The production constructor arms a real recurring sixty-second timer (src/QBrainAi.Services/Services/VoiceConversationService.cs:68). Session creation records `DateTimeOffset.UtcNow` (78, 141-155). The timer calls `CleanupIdleSessionsAsync`, which reads another `DateTimeOffset.UtcNow`, compares the recorded timestamp against the configured timeout, and deletes stale idle sessions (1846-1900). The second creation searches the remaining sessions and otherwise allocates a new ID (89-117, 133). `DeleteSessionAsync` removes the session before its asynchronous cleanup (424).

A scheduling pause between the two creations, or a forward wall-clock adjustment followed by a timer callback during that pause, therefore changes the asserted result: the first session can be removed and the second creation returns a different ID. This is a reachable production lifecycle decision that the unit test does not control. It is not a hang guard whose only effect is to fail a hung test.

The R12-F2 sibling sweep explicitly excludes this path on the assertion that the decision is never reached (docs/receipts/hv/unitgreen-r13-prelaunch.clock-sweep.md:279; its explanation assumes the first sixty-second tick is not reached). There is no source-enforced exclusion. The retained passing unit TRX on both hosts establish that this schedule did not defeat those runs, not that the test is independent of elapsed or wall-clock time.

Affected requirement: TR-MCP-USECASE-018 AC-006, including its 2026-11-05 append covering age, expiry, and staleness decisions inside called production code. Affected claims include UG3, UG47 and the R12-F2 sibling-closure portion of UG13; their propagation into completion and disclosure claims will be stated in the complete verdict.

Change needed: make the checked voice-session lifecycle independent of real time in unit tests through a scripted clock and a test-controlled cleanup trigger or scheduler, while retaining the production defaults. Update the sibling classification and completion evidence. Apply the same correction to the unit siblings that construct this service and assert a session remains available.

Proposed strengthening, named `scripted-background-expiry`: an age, expiry, or staleness decision that can change a unit test's asserted lifecycle result, including a background cleanup callback, uses scripted recorded and reference times and test-controlled scheduling. The same input trace gives the same result regardless of wall-clock adjustment or real elapsed time.

Proposed tests:

- `VoiceSessionReuse_DoesNotDependOnWallClockOrCleanupScheduling`: two creates for the same idle session reuse its ID when the scripted age remains eligible, with cleanup explicitly interleaved and no live timer required.
- `IdleCleanup_UsesScriptedAgeBoundary`: retained sessions immediately before and at the configured strict expiry threshold, and removal immediately after it, proved using fixed recorded times and a scripted reference time.
- Negative controls for a live-clock reader, an uncontrolled cleanup trigger, and an inclusive comparison where the contract requires a strict comparison must be rejected by the same assertions before binding production.

This finding does not assert that the supplied gate runs failed. Their recorded voice-session cases passed.
