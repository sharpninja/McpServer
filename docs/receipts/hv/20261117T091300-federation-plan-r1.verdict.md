2026-11-17T10:25:01 - OverallVerdict: DISAGREE. Accuracy: 97.95. Completeness: 97.0.

{"type":"progress","completionPercentage":100,"reviewerEta":"0 minutes; review complete"}

The amendment does not clear the 98/98 gate. Phase 0 must remain stopped. DEPLOY HOLD remains active. No commit, sync, implementation, deployment, or done-state change is authorized by this verdict.

Verdict counts are 17 PASS, 4 FAIL, and 3 operational UNKNOWN. Task planning-readiness is separately 56 PASS and 11 FAIL.

## Ranked blocking findings

1. FAIL: All 11 group HV gate tasks violate the cited direct-dependency contract.

   [Plan section 15.2](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/plans/2026-11-10-federation-dolt-bdpv4.md:172) and [Task-Workflow-Architecture 3.5.12](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/architecture/Task-Workflow-Architecture.md:349) require each last-member HV gate to have a Task dependency on every other group member. Each gate currently names only the immediately preceding exit task. Across the 11 gates, 45 required direct edges are missing.

   This is a latent defect already present in `original-phase-plan.md`, not a new section 16 regression. Transitive reachability does not satisfy the explicitly stated direct-edge model.

   Minimal remedy: add every non-gate member as a direct dependency of its group gate, using the exact `miss` lists in the TaskMatrix. Do not change requirements. Recalculate the plan SHA, produce a new request and self-review receipt, render the current PDF, and rerun HV.

2. FAIL: The exact-current rendered PDF required by the plan was not supplied.

   [Plan section 14](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/plans/2026-11-10-federation-dolt-bdpv4.md:152) requires the exact current source packet, rendered PDF, and embedded-source hashes. Neither available PDF contains section 16 or current plan SHA `918E6F71CF587B85A9D27BDF40E094EDD10627D140B3BB709F8124D791527373`.

   - The 87-page R2 PDF has 12 attachments but omits section 16 and the current SHA. SHA256 `6CB9929D1554473BDB8318CB51F80DDC09B47FBFD69ECB16A3248AADCAD55692`. :codex-file-citation{path="/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/plans/exports/Federation-2026-11-10-R2.pdf" purpose="source"}
   - The 48-page PDF has no attachments and also omits the amendment. SHA256 `D8D31D3AE5AA47341487F54D9542F6D50854D070F15635130149682D9FEEB5B0`. :codex-file-citation{path="/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/plans/exports/Federation-2026-11-10.pdf" purpose="source"}

   Minimal remedy: render the corrected current packet, embed the exact source documents, record all source and attachment hashes, verify section 16 and the five process corrections are present, then launch a fresh HV round. Old R5 acceptance cannot transfer.

3. Operational rerun blocker: QBrain audit persistence and live memory loading were unavailable.

   Every normal `Invoke -ParamsObject` operation failed before dispatch because `New-McpReplParamsTransfer` could not write `/tmp/mcp-repl-params-*.yaml` in the read-only sandbox. This prevented live Global and Workspace memory reads, session bootstrap, reviewer-session creation, full-verdict persistence, readback, and supported triage submission.

   Under infrastructure rule 6, this did not reduce the substantive plan scores. It does make the required durable reviewer turn unavailable, so this response cannot be represented as a fully persisted accepted HV receipt.

4. Reviewer-process failures requiring remediation:

   - I exposed a raw marker credential in an earlier tool result after incomplete redaction. I do not repeat it here. Rotate the workspace credential and ensure any response receipt intended for commit is secret-safe.
   - Several earlier progress timestamps were estimated or reused rather than freshly measured. Later timestamps were corrected and measured, but the violations remain part of this audit.

## Verified PASS results

- Actual runtime identity was independently read from local Codex runtime state: thread `01a1ea28-b705-7cd0-bc0c-138260b30fb0`, model `gpt-5.6-sol`, effort `max`, provider `openai`, CLI `0.169.0`, source `exec`, managed read-only sandbox, approval `never`.
- HEAD and requested base both equal `911cb810fbf1e005e2cbbbc426daeea7300319a2`.
- The primary plan was read end-to-end and has the requested SHA256.
- Section 16 contains 86 unique definitions and 392 criteria. Its definitions and criteria match [plan-requirements-final-v2.json](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/federation/20261117-plan-definitions/plan-requirements-final-v2.json) exactly.
- All 16 core FR/TR/TEST triples have identical criterion IDs, text, and satisfaction values. No undefined reference was found.
- The two-host readbacks preserve record identity, metadata, unchanged criterion satisfaction, and evidence. No creates, deletes, or rekeys occurred. New or changed criteria remain unsatisfied.
- The corrected mapping model is valid: separate FR-to-TR and FR-to-TEST edges, zero key-pair edge changes on both hosts. The original flawed verifier remains retained. OMarchy `createdAt` values changed between reads, but the receipt history identifies those as read-generated adapter values, not edge-identity evidence.
- [contracts.json](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/plans/federation/contracts.json:148) now uses `Ph4/Ph7` and `Ph2/Ph3/Ph4/Ph5/Ph6/Ph7`; the TR-MCP-FEDERATION-010 and -011 contract bodies exactly equal their canonical final-v2 descriptions.
- All 67 Task IDs are unique; all states are Planning; all group ordinals are contiguous; all dependency targets resolve; the task graph is acyclic.
- All 372 named task AC citations resolve exactly. Every cited core FR is accompanied by its mapped TR and TEST. The two identity tasks retain their `MCP-FEDIDENTITY-001` link.
- The mocks-first negative-control wording, accepted patchable EF10 provider position, and current cross-machine reviewer rule are correctly propagated.
- Crash/restart/replay, concurrency, malformed inputs, both hosts, plugins, lifecycle, isolation, legacy/upgrade, security, and operator-visible diagnostics have named plan outcomes. No unrequested product scope was added.
- B1 through B12 remain Phase 0 evidence obligations. Nothing claims they are proven.
- The eight supplied TRX files all hash-match the [manifest](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/hv/20261117-federation-plan-unit-evidence/manifest.json): 5,443 executed, 5,443 passed, 0 failed, 0 skipped. This proves only the unchanged baseline at the reviewed base.
- The initial Build.Tests log-lock failure was retained and the sequential rerun passed 540/540.
- The two failsafe captures are hash-preserved by [preservation.json](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/federation/20261117-failsafe-repair/preservation.json). Their errors support quarantine as nonreplayable. They do not prove a successful replay.
- I made no repository, plan, requirement, TODO, code, test, deployment, or source change and did not build or run tests.

## Requirement and artifact flux

- Plan section 16 places all 86 final-v2 records in the reviewed source amendment. Every TaskMatrix entry is affected through its core FR/TR/TEST coverage.
- PAYTON-LEGION2 received 49 changed definitions. Every one of the 67 tasks cites at least one affected core FR or TR.
- LAB-OMARCHY received 15 changed definitions. Seven tasks are directly affected through TR-MCP-FEDERATION-010: `TASK-FEDPH4-002`, `TASK-FEDPH4-004`, `TASK-FEDPH4-005`, `TASK-FEDPH7-001`, `TASK-FEDPH7-002`, `TASK-FEDPH7-005`, and `TASK-FEDPH7-006`.
- The `contracts.json` repair affects TR-MCP-FEDERATION-010 and TR-MCP-FEDERATION-011 projections. Exact affected task lists are recorded in `InFluxByArtifact` below.
- The process-wording changes altered no QBrain requirement definition or AC.

## Infrastructure and persistence status

- Profile load: 1 eligible profile file read completely; 0 live Global memories and 0 live Workspace memories loaded because both queries failed before dispatch. These are unavailable scopes, not verified empty scopes.
- Parent session: `Codex-20261116T174245Z-plugin-session`.
- Parent request: `req-20261117T150430Z-federation-plan-hv-sync`.
- Attempted reviewer session: `Codex-20261117T161957Z-federation-plan-hv-r1`.
- Attempted reviewer request: `req-20261117T161957Z-federation-plan-hv-r1`.
- Actual reviewer session and request IDs: none created. Persistence and hash-verified readback are unavailable.
- The supported triage report failed through the same transfer-file fault.
- The live current failsafe drain and canonical linked turn could not be queried. Receipt-level preservation passed; live `quarantined=2`, `replayed=0` remains operationally UNKNOWN.
- [Request JSONL](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/hv/20261117T091300-federation-plan-r1.request.jsonl) exists with SHA256 `C13AC5741A3AA38580DB5EA66EC17AD0A835A0C6C23BB8D6DF76C7060CA15D67`.
- [Response JSONL](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/hv/20261117T091300-federation-plan-r1.response.jsonl) is the active capture and cannot have a final hash verified from inside the still-running review.
- [Verdict receipt](/home/sharpninja/github/McpServer/.worktrees/federation-plan-hv-20261117/docs/receipts/hv/20261117T091300-federation-plan-r1.verdict.md) remained zero bytes because the sandbox is read-only.

Task matrix score rubric:

- `sel=PRA+REG/OOS`: selected for plan-readiness adjudication and holistic regression; implementation success is out of scope.
- `a`: seven universal task checks, plus the direct-gate rule for gate tasks.
- `c`: the same seven universal checks plus proportional direct-gate dependency coverage.
- No `PASS` means implementation completed. Every task remains Planning/open.

=== VERDICT JSON ===
{
  "OverallVerdict": "DISAGREE",
  "Accuracy": 97.95,
  "Completeness": 97.0,
  "PassCount": 17,
  "FailCount": 4,
  "UnknownCount": 3,
  "TaskPassCount": 56,
  "TaskFailCount": 11,
  "Findings": [
    {"id":"P01","status":"PASS","substantive":true,"finding":"Actual runtime identity is GPT-5.6-SOL at Max from local Codex runtime state, not requester metadata."},
    {"id":"P02","status":"PASS","substantive":false,"finding":"One eligible profile Markdown file was read completely."},
    {"id":"P03","status":"PASS","substantive":true,"finding":"HEAD, requested base, branch, and primary plan SHA match the request."},
    {"id":"P04","status":"PASS","substantive":true,"finding":"The primary plan and required supporting source packet were read end-to-end."},
    {"id":"P05","status":"PASS","substantive":true,"finding":"Section 16 and final-v2 contain 86 unique definitions and 392 exact criteria with zero undefined references.","anchor":"docs/plans/2026-11-10-federation-dolt-bdpv4.md:454"},
    {"id":"P06","status":"PASS","substantive":true,"finding":"All 16 core FR/TR/TEST triples have identical criterion identity, text, and satisfaction propagation."},
    {"id":"P07","status":"PASS","substantive":true,"finding":"Two-host preimage/readback comparison preserves IDs, metadata, unchanged satisfaction, and evidence; changed criteria remain unsatisfied."},
    {"id":"P08","status":"PASS","substantive":true,"finding":"verification-v2 correctly models separate FR-to-TR and FR-to-TEST edges and finds zero key-pair edge changes."},
    {"id":"P09","status":"PASS","substantive":true,"finding":"contracts.json phase annotations and TR-MCP-FEDERATION-010/-011 bodies now exactly match canonical definitions."},
    {"id":"P10","status":"PASS","substantive":true,"finding":"All 67 task IDs, ordinals, Planning states, and non-gate dependency references are valid; the graph is acyclic."},
    {"id":"P11","status":"PASS","substantive":true,"finding":"All 372 named task AC citations resolve and every cited core FR carries its mapped TR and TEST."},
    {"id":"P12","status":"PASS","substantive":true,"finding":"Mocks-first negative controls, accepted patchable EF10 provider wording, and reviewer routing are correctly propagated."},
    {"id":"P13","status":"PASS","substantive":true,"finding":"The plan states outcomes for the requested hostile what-if surfaces without adding product scope."},
    {"id":"P14","status":"PASS","substantive":true,"finding":"B1-B12 remain explicitly unproven Phase 0 work and DEPLOY HOLD remains in force."},
    {"id":"P15","status":"PASS","substantive":true,"finding":"Eight supplied TRX files verify a 5443/5443, failed 0, skipped 0 baseline at the reviewed base; no implementation acceptance is inferred."},
    {"id":"P16","status":"PASS","substantive":false,"finding":"Two failsafe captures are hash-preserved and support quarantine as nonreplayable, not successful replay."},
    {"id":"P17","status":"PASS","substantive":false,"finding":"Reviewer performed no build, test, repository edit, requirement/TODO write, implementation, or deployment."},
    {"id":"F01","status":"FAIL","substantive":true,"finding":"All 11 group HV gate tasks omit direct dependencies required by the cited architecture; 45 direct edges are missing.","anchor":"docs/architecture/Task-Workflow-Architecture.md:349","remedy":"Add every non-gate member as a direct dependency of its group gate using each TaskMatrix miss list."},
    {"id":"F02","status":"FAIL","substantive":true,"finding":"No rendered PDF represents the current section 16 amendment or current plan hash.","anchor":"docs/plans/2026-11-10-federation-dolt-bdpv4.md:152","remedy":"Render and hash the corrected current packet with exact embedded sources, then launch a fresh HV request."},
    {"id":"F03","status":"FAIL","substantive":false,"finding":"Reviewer exposed a raw marker credential in an earlier tool result after incomplete redaction.","remedy":"Rotate the workspace credential and ensure committed response receipts are secret-safe."},
    {"id":"F04","status":"FAIL","substantive":false,"finding":"Several early commentary timestamps were estimated or reused rather than freshly measured.","remedy":"Use a fresh measured local sortable timestamp for every response and progress record in the rerun."},
    {"id":"U01","status":"UNKNOWN","substantive":false,"finding":"Live Global and Workspace MCP memories could not be listed because the wrapper failed before dispatch."},
    {"id":"U02","status":"UNKNOWN","substantive":false,"finding":"Dedicated reviewer session creation, complete-verdict persistence, readback, and triage submission were unavailable through the same wrapper fault."},
    {"id":"U03","status":"UNKNOWN","substantive":false,"finding":"The current live failsafe drain result and canonical linked turn could not be independently queried; receipt-level preservation is verified."}
  ],
  "TaskMatrixLegend": {
    "g":"group ID",
    "n":"group ordinal",
    "s":"source state",
    "d":"dependencies",
    "r":"FR/TR/TEST requirements",
    "sel":"PRA+REG/OOS means plan-readiness plus regression selected, implementation success out of scope",
    "v":"planning-readiness verdict only",
    "a":"task accuracy percentage under the stated rubric",
    "c":"task completeness percentage under the stated rubric",
    "miss":"required direct gate dependencies absent from the plan"
  },
  "TaskMatrix": [
    {"id":"TASK-FEDPH0-001","g":"GRP-FEDPH0-001","n":1,"s":"Planning","d":[],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-002","g":"GRP-FEDPH0-001","n":2,"s":"Planning","d":["TASK-FEDPH0-001"],"r":["FR-MCP-186","FR-MCP-177","FR-MCP-175","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-013","TEST-MCP-223","TEST-MCP-225","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-003","g":"GRP-FEDPH0-001","n":3,"s":"Planning","d":["TASK-FEDPH0-001"],"r":["FR-MCP-185","TR-MCP-FEDERATION-012","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-004","g":"GRP-FEDPH0-001","n":4,"s":"Planning","d":[],"r":["FR-MCP-184","FR-MCP-182","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TEST-MCP-230","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-005","g":"GRP-FEDPH0-001","n":5,"s":"Planning","d":[],"r":["FR-MCP-185","TR-MCP-FEDERATION-012","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-006","g":"GRP-FEDPH0-001","n":6,"s":"Planning","d":["TASK-FEDPH0-002"],"r":["FR-MCP-180","FR-MCP-186","FR-MCP-187","FR-MCP-189","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TR-MCP-FEDERATION-016","TEST-MCP-228","TEST-MCP-234","TEST-MCP-235","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-007","g":"GRP-FEDPH0-001","n":7,"s":"Planning","d":["TASK-FEDPH0-002","TASK-FEDPH0-003"],"r":["FR-MCP-179","FR-MCP-184","FR-MCP-185","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TEST-MCP-227","TEST-MCP-232","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-008","g":"GRP-FEDPH0-001","n":8,"s":"Planning","d":["TASK-FEDPH0-001","TASK-FEDPH0-002","TASK-FEDPH0-003","TASK-FEDPH0-004","TASK-FEDPH0-005","TASK-FEDPH0-006","TASK-FEDPH0-007"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH0-009","g":"GRP-FEDPH0-001","n":9,"s":"Planning","d":["TASK-FEDPH0-008"],"r":["FR-MCP-186","FR-MCP-177","FR-MCP-175","FR-MCP-185","FR-MCP-184","FR-MCP-182","FR-MCP-180","FR-MCP-187","FR-MCP-189","FR-MCP-179","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TR-MCP-FEDERATION-016","TEST-MCP-223","TEST-MCP-225","TEST-MCP-227","TEST-MCP-228","TEST-MCP-230","TEST-MCP-232","TEST-MCP-233","TEST-MCP-234","TEST-MCP-235","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":89.06,"miss":["TASK-FEDPH0-001","TASK-FEDPH0-002","TASK-FEDPH0-003","TASK-FEDPH0-004","TASK-FEDPH0-005","TASK-FEDPH0-006","TASK-FEDPH0-007"]},
    {"id":"TASK-FEDPH1-001","g":"GRP-FEDPH1-001","n":1,"s":"Planning","d":["GRP-FEDPH0-001"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH1-002","g":"GRP-FEDPH1-001","n":2,"s":"Planning","d":["TASK-FEDPH1-001"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH1-003","g":"GRP-FEDPH1-001","n":3,"s":"Planning","d":["GRP-FEDPH0-001"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH1-004","g":"GRP-FEDPH1-001","n":4,"s":"Planning","d":["TASK-FEDPH1-002","TASK-FEDPH1-003"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH1-005","g":"GRP-FEDPH1-001","n":5,"s":"Planning","d":["TASK-FEDPH1-004"],"r":["FR-MCP-186","TR-MCP-FEDERATION-013","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":90.62,"miss":["TASK-FEDPH1-001","TASK-FEDPH1-002","TASK-FEDPH1-003"]},
    {"id":"TASK-FEDPH2-001","g":"GRP-FEDPH2-001","n":1,"s":"Planning","d":["GRP-FEDPH1-001"],"r":["FR-MCP-174","FR-MCP-176","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-003","TEST-MCP-222","TEST-MCP-224"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-002","g":"GRP-FEDPH2-001","n":2,"s":"Planning","d":["TASK-FEDPH2-001"],"r":["FR-MCP-177","FR-MCP-184","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-011","TEST-MCP-225","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-003","g":"GRP-FEDPH2-001","n":3,"s":"Planning","d":["TASK-FEDPH2-002"],"r":["FR-MCP-182","TR-MCP-FEDERATION-009","TEST-MCP-230"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-004","g":"GRP-FEDPH2-001","n":4,"s":"Planning","d":["TASK-FEDPH2-002"],"r":["FR-MCP-189","TR-MCP-FEDERATION-016","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-005","g":"GRP-FEDPH2-001","n":5,"s":"Planning","d":["TASK-FEDPH2-001"],"r":["FR-MCP-185","TR-MCP-FEDERATION-012","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-006","g":"GRP-FEDPH2-001","n":6,"s":"Planning","d":["TASK-FEDPH2-002"],"r":["FR-MCP-184","FR-MCP-188","TR-MCP-AUDIT-004","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-015","TEST-MCP-232","TEST-MCP-236"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-007","g":"GRP-FEDPH2-001","n":7,"s":"Planning","d":["TASK-FEDPH2-001","TASK-FEDPH2-002","TASK-FEDPH2-003","TASK-FEDPH2-004","TASK-FEDPH2-005","TASK-FEDPH2-006"],"r":["FR-MCP-174","FR-MCP-176","FR-MCP-177","FR-MCP-184","FR-MCP-182","FR-MCP-189","FR-MCP-185","FR-MCP-188","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-222","TEST-MCP-224","TEST-MCP-225","TEST-MCP-230","TEST-MCP-232","TEST-MCP-233","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH2-008","g":"GRP-FEDPH2-001","n":8,"s":"Planning","d":["TASK-FEDPH2-007"],"r":["FR-MCP-174","FR-MCP-176","FR-MCP-177","FR-MCP-184","FR-MCP-182","FR-MCP-189","FR-MCP-185","FR-MCP-188","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-222","TEST-MCP-224","TEST-MCP-225","TEST-MCP-230","TEST-MCP-232","TEST-MCP-233","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":89.29,"miss":["TASK-FEDPH2-001","TASK-FEDPH2-002","TASK-FEDPH2-003","TASK-FEDPH2-004","TASK-FEDPH2-005","TASK-FEDPH2-006"]},
    {"id":"TASK-FEDPH3-001","g":"GRP-FEDPH3-001","n":1,"s":"Planning","d":["GRP-FEDPH2-001"],"r":["FR-MCP-187","FR-MCP-182","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-014","TEST-MCP-230","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH3-002","g":"GRP-FEDPH3-001","n":2,"s":"Planning","d":["TASK-FEDPH3-001"],"r":["FR-MCP-187","TR-MCP-FEDERATION-014","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH3-003","g":"GRP-FEDPH3-001","n":3,"s":"Planning","d":["TASK-FEDPH3-001"],"r":["FR-MCP-184","TR-MCP-FEDERATION-011","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH3-004","g":"GRP-FEDPH3-001","n":4,"s":"Planning","d":["TASK-FEDPH3-002"],"r":["FR-MCP-187","TR-MCP-FEDERATION-014","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH3-005","g":"GRP-FEDPH3-001","n":5,"s":"Planning","d":["TASK-FEDPH3-001","TASK-FEDPH3-002","TASK-FEDPH3-003","TASK-FEDPH3-004"],"r":["FR-MCP-187","FR-MCP-182","FR-MCP-184","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-014","TEST-MCP-230","TEST-MCP-232","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH3-006","g":"GRP-FEDPH3-001","n":6,"s":"Planning","d":["TASK-FEDPH3-005"],"r":["FR-MCP-187","FR-MCP-182","FR-MCP-184","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-014","TEST-MCP-230","TEST-MCP-232","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":90.0,"miss":["TASK-FEDPH3-001","TASK-FEDPH3-002","TASK-FEDPH3-003","TASK-FEDPH3-004"]},
    {"id":"TASK-FEDPH4-001","g":"GRP-FEDPH4-001","n":1,"s":"Planning","d":["GRP-FEDPH3-001"],"r":["FR-MCP-175","FR-MCP-189","FR-MCP-184","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-016","TEST-MCP-223","TEST-MCP-232","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH4-002","g":"GRP-FEDPH4-001","n":2,"s":"Planning","d":["TASK-FEDPH4-001"],"r":["FR-MCP-177","FR-MCP-183","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-010","TEST-MCP-225","TEST-MCP-231"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH4-003","g":"GRP-FEDPH4-001","n":3,"s":"Planning","d":["TASK-FEDPH4-001"],"r":["FR-MCP-175","TR-MCP-FEDERATION-002","TEST-MCP-223"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH4-004","g":"GRP-FEDPH4-001","n":4,"s":"Planning","d":["TASK-FEDPH4-001","TASK-FEDPH4-002","TASK-FEDPH4-003"],"r":["FR-MCP-175","FR-MCP-189","FR-MCP-184","FR-MCP-177","FR-MCP-183","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-016","TEST-MCP-223","TEST-MCP-225","TEST-MCP-231","TEST-MCP-232","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH4-005","g":"GRP-FEDPH4-001","n":5,"s":"Planning","d":["TASK-FEDPH4-004"],"r":["FR-MCP-175","FR-MCP-189","FR-MCP-184","FR-MCP-177","FR-MCP-183","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-016","TEST-MCP-223","TEST-MCP-225","TEST-MCP-231","TEST-MCP-232","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":90.62,"miss":["TASK-FEDPH4-001","TASK-FEDPH4-002","TASK-FEDPH4-003"]},
    {"id":"TASK-FEDPH5-001","g":"GRP-FEDPH5-001","n":1,"s":"Planning","d":["GRP-FEDPH4-001"],"r":["FR-MCP-176","FR-MCP-185","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-012","TEST-MCP-224","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-002","g":"GRP-FEDPH5-001","n":2,"s":"Planning","d":["TASK-FEDPH5-001"],"r":["FR-MCP-185","TR-MCP-FEDERATION-012","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-003","g":"GRP-FEDPH5-001","n":3,"s":"Planning","d":["TASK-FEDPH5-002"],"r":["FR-MCP-178","FR-MCP-181","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-008","TEST-MCP-226","TEST-MCP-229"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-004","g":"GRP-FEDPH5-001","n":4,"s":"Planning","d":["TASK-FEDPH5-003"],"r":["FR-MCP-179","FR-MCP-181","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TEST-MCP-227","TEST-MCP-229"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-005","g":"GRP-FEDPH5-001","n":5,"s":"Planning","d":["TASK-FEDPH5-002"],"r":["FR-MCP-184","FR-MCP-182","TR-MCP-AUDIT-004","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TEST-MCP-230","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-006","g":"GRP-FEDPH5-001","n":6,"s":"Planning","d":["TASK-FEDPH5-001","TASK-FEDPH5-002","TASK-FEDPH5-003","TASK-FEDPH5-004","TASK-FEDPH5-005"],"r":["FR-MCP-176","FR-MCP-185","FR-MCP-178","FR-MCP-181","FR-MCP-179","FR-MCP-184","FR-MCP-182","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TEST-MCP-224","TEST-MCP-226","TEST-MCP-227","TEST-MCP-229","TEST-MCP-230","TEST-MCP-232","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH5-007","g":"GRP-FEDPH5-001","n":7,"s":"Planning","d":["TASK-FEDPH5-006"],"r":["FR-MCP-176","FR-MCP-185","FR-MCP-178","FR-MCP-181","FR-MCP-179","FR-MCP-184","FR-MCP-182","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TEST-MCP-224","TEST-MCP-226","TEST-MCP-227","TEST-MCP-229","TEST-MCP-230","TEST-MCP-232","TEST-MCP-233"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":89.58,"miss":["TASK-FEDPH5-001","TASK-FEDPH5-002","TASK-FEDPH5-003","TASK-FEDPH5-004","TASK-FEDPH5-005"]},
    {"id":"TASK-FEDPH6-001","g":"GRP-FEDPH6-001","n":1,"s":"Planning","d":["GRP-FEDPH5-001"],"r":["FR-MCP-178","FR-MCP-180","FR-MCP-189","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-016","TEST-MCP-226","TEST-MCP-228","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-002","g":"GRP-FEDPH6-001","n":2,"s":"Planning","d":["TASK-FEDPH6-001"],"r":["FR-MCP-180","TR-MCP-FEDERATION-007","TEST-MCP-228"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-003","g":"GRP-FEDPH6-001","n":3,"s":"Planning","d":["TASK-FEDPH6-001"],"r":["FR-MCP-181","FR-MCP-186","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-013","TEST-MCP-229","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-004","g":"GRP-FEDPH6-001","n":4,"s":"Planning","d":["TASK-FEDPH6-002","TASK-FEDPH6-003"],"r":["FR-MCP-181","FR-MCP-179","FR-MCP-184","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-011","TEST-MCP-227","TEST-MCP-229","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-005","g":"GRP-FEDPH6-001","n":5,"s":"Planning","d":["TASK-FEDPH6-001"],"r":["FR-MCP-188","TR-MCP-FEDERATION-015","TEST-MCP-236"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-006","g":"GRP-FEDPH6-001","n":6,"s":"Planning","d":["TASK-FEDPH6-001","TASK-FEDPH6-002","TASK-FEDPH6-003","TASK-FEDPH6-004","TASK-FEDPH6-005"],"r":["FR-MCP-180","FR-MCP-181","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TEST-MCP-228","TEST-MCP-229"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6-007","g":"GRP-FEDPH6-001","n":7,"s":"Planning","d":["TASK-FEDPH6-006"],"r":["FR-MCP-178","FR-MCP-180","FR-MCP-189","FR-MCP-181","FR-MCP-186","FR-MCP-179","FR-MCP-184","FR-MCP-188","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-226","TEST-MCP-227","TEST-MCP-228","TEST-MCP-229","TEST-MCP-232","TEST-MCP-234","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":89.58,"miss":["TASK-FEDPH6-001","TASK-FEDPH6-002","TASK-FEDPH6-003","TASK-FEDPH6-004","TASK-FEDPH6-005"]},
    {"id":"TASK-FEDPH6J-001","g":"GRP-FEDPH6-002","n":1,"s":"Planning","d":["GRP-FEDPH6-001"],"r":["FR-MCP-187","TR-MCP-FEDERATION-014","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6J-002","g":"GRP-FEDPH6-002","n":2,"s":"Planning","d":["TASK-FEDPH6J-001"],"r":["FR-MCP-184","TR-MCP-FEDERATION-011","TEST-MCP-232"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6J-003","g":"GRP-FEDPH6-002","n":3,"s":"Planning","d":["TASK-FEDPH6J-001","TASK-FEDPH6J-002"],"r":["FR-MCP-187","FR-MCP-184","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-014","TEST-MCP-232","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH6J-004","g":"GRP-FEDPH6-002","n":4,"s":"Planning","d":["TASK-FEDPH6J-003"],"r":["FR-MCP-187","FR-MCP-184","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-014","TEST-MCP-232","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":91.67,"miss":["TASK-FEDPH6J-001","TASK-FEDPH6J-002"]},
    {"id":"TASK-FEDPH7-001","g":"GRP-FEDPH7-001","n":1,"s":"Planning","d":["GRP-FEDPH6-002"],"r":["FR-MCP-183","TR-MCP-FEDERATION-010","TEST-MCP-231"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH7-002","g":"GRP-FEDPH7-001","n":2,"s":"Planning","d":["TASK-FEDPH7-001"],"r":["FR-MCP-183","TR-MCP-FEDERATION-010","TEST-MCP-231"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH7-003","g":"GRP-FEDPH7-001","n":3,"s":"Planning","d":["TASK-FEDPH7-002"],"r":["FR-MCP-176","FR-MCP-182","FR-MCP-185","FR-MCP-181","FR-MCP-188","FR-MCP-189","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-224","TEST-MCP-229","TEST-MCP-230","TEST-MCP-233","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH7-004","g":"GRP-FEDPH7-001","n":4,"s":"Planning","d":["TASK-FEDPH7-002"],"r":["FR-MCP-180","TR-MCP-FEDERATION-007","TEST-MCP-228"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH7-005","g":"GRP-FEDPH7-001","n":5,"s":"Planning","d":["TASK-FEDPH7-001","TASK-FEDPH7-002","TASK-FEDPH7-003","TASK-FEDPH7-004"],"r":["FR-MCP-183","FR-MCP-176","FR-MCP-182","FR-MCP-185","FR-MCP-181","FR-MCP-188","FR-MCP-189","FR-MCP-180","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-224","TEST-MCP-228","TEST-MCP-229","TEST-MCP-230","TEST-MCP-231","TEST-MCP-233","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH7-006","g":"GRP-FEDPH7-001","n":6,"s":"Planning","d":["TASK-FEDPH7-005"],"r":["FR-MCP-183","FR-MCP-176","FR-MCP-182","FR-MCP-185","FR-MCP-181","FR-MCP-188","FR-MCP-189","FR-MCP-180","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TEST-MCP-224","TEST-MCP-228","TEST-MCP-229","TEST-MCP-230","TEST-MCP-231","TEST-MCP-233","TEST-MCP-236","TEST-MCP-237"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":90.0,"miss":["TASK-FEDPH7-001","TASK-FEDPH7-002","TASK-FEDPH7-003","TASK-FEDPH7-004"]},
    {"id":"TASK-FEDPH8-001","g":"GRP-FEDPH8-001","n":1,"s":"Planning","d":["GRP-FEDPH7-001"],"r":["FR-MCP-178","FR-MCP-186","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-013","TEST-MCP-226","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-002","g":"GRP-FEDPH8-001","n":2,"s":"Planning","d":["GRP-FEDPH7-001"],"r":["FR-MCP-186","FR-MCP-179","FR-MCP-185","FR-MCP-184","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TEST-MCP-227","TEST-MCP-232","TEST-MCP-233","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-003","g":"GRP-FEDPH8-001","n":3,"s":"Planning","d":["TASK-FEDPH8-001","TASK-FEDPH8-002"],"r":["FR-MCP-181","FR-MCP-174","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-008","TEST-MCP-222","TEST-MCP-229"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-004","g":"GRP-FEDPH8-001","n":4,"s":"Planning","d":["TASK-FEDPH8-001","TASK-FEDPH8-002"],"r":["FR-MCP-187","FR-MCP-186","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TEST-MCP-234","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-005","g":"GRP-FEDPH8-001","n":5,"s":"Planning","d":["TASK-FEDPH8-004"],"r":["FR-MCP-187","TR-MCP-FEDERATION-014","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-006","g":"GRP-FEDPH8-001","n":6,"s":"Planning","d":["TASK-FEDPH8-001","TASK-FEDPH8-002","TASK-FEDPH8-003","TASK-FEDPH8-004","TASK-FEDPH8-005"],"r":["FR-MCP-178","FR-MCP-186","FR-MCP-179","FR-MCP-185","FR-MCP-184","FR-MCP-181","FR-MCP-174","FR-MCP-187","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TEST-MCP-222","TEST-MCP-226","TEST-MCP-227","TEST-MCP-229","TEST-MCP-232","TEST-MCP-233","TEST-MCP-234","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDPH8-007","g":"GRP-FEDPH8-001","n":7,"s":"Planning","d":["TASK-FEDPH8-006"],"r":["FR-MCP-178","FR-MCP-186","FR-MCP-179","FR-MCP-185","FR-MCP-184","FR-MCP-181","FR-MCP-174","FR-MCP-187","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TEST-MCP-222","TEST-MCP-226","TEST-MCP-227","TEST-MCP-229","TEST-MCP-232","TEST-MCP-233","TEST-MCP-234","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":89.58,"miss":["TASK-FEDPH8-001","TASK-FEDPH8-002","TASK-FEDPH8-003","TASK-FEDPH8-004","TASK-FEDPH8-005"]},
    {"id":"TASK-FEDCUT-001","g":"GRP-FEDCUT-001","n":1,"s":"Planning","d":["GRP-FEDPH8-001"],"r":["FR-MCP-186","FR-MCP-184","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-013","TEST-MCP-232","TEST-MCP-234"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDCUT-002","g":"GRP-FEDCUT-001","n":2,"s":"Planning","d":["TASK-FEDCUT-001"],"r":["FR-MCP-178","FR-MCP-187","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-014","TEST-MCP-226","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"PASS","a":100,"c":100},
    {"id":"TASK-FEDCUT-003","g":"GRP-FEDCUT-001","n":3,"s":"Planning","d":["TASK-FEDCUT-002"],"r":["FR-MCP-186","FR-MCP-184","FR-MCP-178","FR-MCP-187","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TEST-MCP-226","TEST-MCP-232","TEST-MCP-234","TEST-MCP-235"],"sel":"PRA+REG/OOS","v":"FAIL","a":87.5,"c":93.75,"miss":["TASK-FEDCUT-001"]}
  ],
  "Regressions": [
    {"id":"R01","status":"FAIL","finding":"The current amendment is not represented in any rendered PDF, creating source-to-render drift."},
    {"id":"R02","status":"LATENT_DEFECT","finding":"The 11 HV-gate direct-dependency defects existed in original-phase-plan.md and were missed by prior R5; they were not introduced by section 16."},
    {"id":"R03","status":"PASS","finding":"Outside section 16, the plan prefix differs from the original only at the five declared process corrections."},
    {"id":"R04","status":"PASS","finding":"contracts.json changes are limited to the declared two phase annotations, canonical TR-MCP-FEDERATION-010 outcomes/errors, and EOF cleanup."},
    {"id":"R05","status":"PASS","finding":"No old R5 acceptance was transferred to the amendment."}
  ],
  "InfrastructureIssues": [
    {"id":"I01","status":"UNKNOWN","issue":"Global and Workspace memory.list calls failed before MCP dispatch because the wrapper could not write its /tmp transfer YAML."},
    {"id":"I02","status":"UNKNOWN","issue":"Session bootstrap/open, verdict persistence, history readback, and triage report failed through the same wrapper path."},
    {"id":"I03","status":"UNKNOWN","issue":"Live remote failsafe drain and linked canonical turn could not be independently read."},
    {"id":"I04","status":"FAIL","issue":"A marker credential appeared in an earlier tool result; rotate it and keep retained receipts secret-safe."},
    {"id":"I05","status":"FAIL","issue":"Several early commentary timestamps were not freshly measured."},
    {"id":"I06","status":"LIMIT","issue":"The active response JSONL has no stable final hash until this response finishes."}
  ],
  "InFluxByArtifact": [
    {
      "artifact":"plan section 16",
      "requirementCount":86,
      "requirements":["FR-MCP-103","FR-MCP-125","FR-MCP-174","FR-MCP-175","FR-MCP-176","FR-MCP-177","FR-MCP-178","FR-MCP-179","FR-MCP-180","FR-MCP-181","FR-MCP-182","FR-MCP-183","FR-MCP-184","FR-MCP-185","FR-MCP-186","FR-MCP-187","FR-MCP-188","FR-MCP-189","FR-MCP-AUDIT-001","FR-MCP-FED-BACKFILL-001","FR-MCP-FED-ROUTE-001","FR-MCP-FED-WSSYNC-001","FR-MCP-MEMORY-008","TR-MCP-AUDIT-004","TR-MCP-FED-001","TR-MCP-FED-BACKFILL-001","TR-MCP-FED-MEMORY-001","TR-MCP-FED-ROUTE-001","TR-MCP-FED-WSSYNC-001","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","TR-MCP-TXNCOMPAT-001","TEST-MCP-136","TEST-MCP-222","TEST-MCP-223","TEST-MCP-224","TEST-MCP-225","TEST-MCP-226","TEST-MCP-227","TEST-MCP-228","TEST-MCP-229","TEST-MCP-230","TEST-MCP-231","TEST-MCP-232","TEST-MCP-233","TEST-MCP-234","TEST-MCP-235","TEST-MCP-236","TEST-MCP-237","TEST-MCP-AUDIT-003","TEST-MCP-FED-BACKFILL-001","TEST-MCP-FED-ROUTE-001","TEST-MCP-FED-WSSYNC-001","TEST-MCP-MEMORY-FED-001","FR-MCP-077","FR-MCP-173","FR-MCP-SESSIONLOGCTX-001","FR-MCP-SESSIONLOGSAN-001","TEST-MCP-AUDIT-001","TEST-MCP-AUDIT-002","TEST-MCP-FED-PATH-001","TEST-MCP-PATHNORM-001","TEST-MCP-SESSIONLOG-FED-001","TEST-MCP-SESSIONLOGSAN-001","TEST-MCP-SESSIONLOGSAN-002","TR-MCP-AUDIT-002","TR-MCP-AUDIT-003","TR-MCP-FED-PATH-001","TR-MCP-FED-SESSIONLOG-001","TR-MCP-PATHNORM-001","TR-MCP-SESSIONLOGSAN-001","TR-MCP-SESSIONLOGSAN-002"],
      "affectedTasks":"all 67 TaskMatrix entries"
    },
    {
      "artifact":"PAYTON-LEGION2 QBrain definition write",
      "requirementCount":49,
      "requirements":["FR-MCP-174","FR-MCP-175","FR-MCP-176","FR-MCP-177","FR-MCP-178","FR-MCP-179","FR-MCP-180","FR-MCP-181","FR-MCP-182","FR-MCP-183","FR-MCP-184","FR-MCP-185","FR-MCP-186","FR-MCP-187","FR-MCP-188","FR-MCP-189","FR-MCP-FED-ROUTE-001","TR-MCP-FED-ROUTE-001","TR-MCP-FEDERATION-001","TR-MCP-FEDERATION-002","TR-MCP-FEDERATION-003","TR-MCP-FEDERATION-004","TR-MCP-FEDERATION-005","TR-MCP-FEDERATION-006","TR-MCP-FEDERATION-007","TR-MCP-FEDERATION-008","TR-MCP-FEDERATION-009","TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-011","TR-MCP-FEDERATION-012","TR-MCP-FEDERATION-013","TR-MCP-FEDERATION-014","TR-MCP-FEDERATION-015","TR-MCP-FEDERATION-016","FR-MCP-077","FR-MCP-173","FR-MCP-SESSIONLOGCTX-001","TEST-MCP-AUDIT-001","TEST-MCP-AUDIT-002","TEST-MCP-FED-PATH-001","TEST-MCP-PATHNORM-001","TEST-MCP-SESSIONLOGSAN-002","TR-MCP-AUDIT-002","TR-MCP-AUDIT-003","TR-MCP-FED-PATH-001","TR-MCP-FED-SESSIONLOG-001","TR-MCP-PATHNORM-001","TR-MCP-SESSIONLOGSAN-001","TR-MCP-SESSIONLOGSAN-002"],
      "affectedTasks":"all 67 TaskMatrix entries"
    },
    {
      "artifact":"LAB-OMARCHY QBrain definition write",
      "requirementCount":15,
      "requirements":["TR-MCP-FEDERATION-010","FR-MCP-077","FR-MCP-173","FR-MCP-SESSIONLOGCTX-001","TEST-MCP-AUDIT-001","TEST-MCP-AUDIT-002","TEST-MCP-FED-PATH-001","TEST-MCP-PATHNORM-001","TEST-MCP-SESSIONLOGSAN-001","TEST-MCP-SESSIONLOGSAN-002","TR-MCP-AUDIT-002","TR-MCP-AUDIT-003","TR-MCP-FED-PATH-001","TR-MCP-SESSIONLOGSAN-001","TR-MCP-SESSIONLOGSAN-002"],
      "affectedTasks":["TASK-FEDPH4-002","TASK-FEDPH4-004","TASK-FEDPH4-005","TASK-FEDPH7-001","TASK-FEDPH7-002","TASK-FEDPH7-005","TASK-FEDPH7-006"]
    },
    {
      "artifact":"contracts.json",
      "requirements":["TR-MCP-FEDERATION-010","TR-MCP-FEDERATION-011"],
      "taskImpactByRequirement":{
        "TR-MCP-FEDERATION-010":["TASK-FEDPH4-002","TASK-FEDPH4-004","TASK-FEDPH4-005","TASK-FEDPH7-001","TASK-FEDPH7-002","TASK-FEDPH7-005","TASK-FEDPH7-006"],
        "TR-MCP-FEDERATION-011":["TASK-FEDPH0-004","TASK-FEDPH0-007","TASK-FEDPH0-009","TASK-FEDPH2-002","TASK-FEDPH2-006","TASK-FEDPH2-007","TASK-FEDPH2-008","TASK-FEDPH3-003","TASK-FEDPH3-005","TASK-FEDPH3-006","TASK-FEDPH4-001","TASK-FEDPH4-004","TASK-FEDPH4-005","TASK-FEDPH5-005","TASK-FEDPH5-006","TASK-FEDPH5-007","TASK-FEDPH6-004","TASK-FEDPH6-007","TASK-FEDPH6J-002","TASK-FEDPH6J-003","TASK-FEDPH6J-004","TASK-FEDPH8-002","TASK-FEDPH8-006","TASK-FEDPH8-007","TASK-FEDCUT-001","TASK-FEDCUT-003"]
      }
    }
  ],
  "ReviewerSessionId": null,
  "ReviewerRequestId": null,
  "AttemptedReviewerSessionId": "Codex-20261117T161957Z-federation-plan-hv-r1",
  "AttemptedReviewerRequestId": "req-20261117T161957Z-federation-plan-hv-r1",
  "ParentSessionId": "Codex-20261116T174245Z-plugin-session",
  "ParentRequestId": "req-20261117T150430Z-federation-plan-hv-sync",
  "RuntimeIdentity": {
    "threadId":"01a1ea28-b705-7cd0-bc0c-138260b30fb0",
    "model":"gpt-5.6-sol",
    "effort":"max",
    "provider":"openai",
    "source":"exec",
    "cliVersion":"0.169.0",
    "sandbox":"managed read-only",
    "approvalMode":"never",
    "head":"911cb810fbf1e005e2cbbbc426daeea7300319a2",
    "branch":"codex/federation-plan-hv-20261117"
  },
  "ProfileLoad": {
    "eligibleProfileFilesRead":1,
    "globalMemoriesLoaded":0,
    "workspaceMemoriesLoaded":0,
    "status":"INCOMPLETE",
    "reason":"Both live memory list calls failed before dispatch; zero is a loaded count, not a verified empty scope."
  },
  "PersistenceReadbackStatus":"UNAVAILABLE",
  "ReceiptPaths":[
    "docs/receipts/hv/20261117T091300-federation-plan-r1.request.jsonl",
    "docs/receipts/hv/20261117T091300-federation-plan-r1.response.jsonl",
    "docs/receipts/hv/20261117T091300-federation-plan-r1.verdict.md",
    "docs/receipts/hv/20261117T091300-federation-plan-r1.selfreview.json",
    "docs/receipts/hv/20261117-federation-plan-unit-evidence/manifest.json",
    "docs/receipts/federation/20261117-plan-definitions/plan-requirements-final-v2.json",
    "docs/receipts/federation/20261117-plan-definitions/verification-v2.json",
    "docs/receipts/federation/20261117-plan-definitions/mapping-check-correction.json",
    "docs/receipts/federation/20261117-failsafe-repair/preservation.json"
  ]
}