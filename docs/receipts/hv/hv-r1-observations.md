<!-- SPDX-License-Identifier: Apache-2.0 -->
# Round 1 working observations

These are independently observed candidate defects, not the final verdict. The review continues through the entire artifact and cited sources.

- Amendment lines 1395 and 154 promise a flush on elapsed OutputFlushInterval even without another line. StreamReviewerOutput lines 1425-1437 has only line, heartbeat, and process-exit events. It checks elapsed time only when a line arrives. A below-threshold line followed by silence remains buffered until another line or exit. This contradicts the requested complete activity diagram and may fail the gated live-line case.
- UpdateHvReviewerCommand lines 632 and 653 promises a new NONCE after configuration changes. HvWorkerHost lines 1372-1390 requests NONCE only at startup; the ordinary loop sweeps, files, and claims. ClaimHvRunCommand line 941 requires ProbeRequestedUtc, which Update does not set. A running host can leave a changed reviewer Unverified indefinitely.
- V-6 line 1283 checks only the compact hv_verdict record in the reviewer turn. R-24 and P-06 require the entire result there. This check admits an audit turn with no assessments, holistic evaluation, findings, or detailed verdict prose.
- R-28 line 170 says Canceled refuses cancellation with 409. Handler lines 787 and 805 says Canceled returns 200 unchanged. This is a public response-contract inconsistency.
- RemoveHvEvidenceCommand lines 859 and 873 stamps RemovedBy and RemovalReason on chunks. HvRunOutputChunk line 492 defines neither field. GetHvRequestQuery and StreamHvRunOutputQuery also have no removed-payload branch although completed runs keep manifest counts after removal.
- Probe failures appear to dispatch request-specific HvRetryDecision unconditionally in FailHvRunCommand (lines 1045-1067), whereas the policy explicitly applies to review runs only (line 1346). Need check all downstream acceptance criteria.
- Finalization sets member turns Final before writing linked response grades (lines 1100 and 1139-1144). HvTurnFreeze allows the special origin only for Pending turns of its own request (line 1262). Need inspect the foundation's response and child mutation rules before concluding this is a deadlock.

Review process facts: no substantive project files changed; only review scripts and evidence under docs/receipts/hv, and the validator's isolated plugin cache and own session turn. No other reviewer launched. No TODO or requirement mutated.
