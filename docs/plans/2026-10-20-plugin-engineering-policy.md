# BDPv4 Plan: Integrate Karpathy and Ponytail Across the Plugins

Status: proposed architecture amendment and implementation plan. Committing or
reviewing this document does not authorize product implementation or deployment.

## 1. Intent, authority and dependencies

Deliver one shared engineering policy combining Karpathy's decision discipline,
Ponytail's smallest complete solution and workspace-selected BDPv4, with
authoritative server enforcement of completion evidence. Guidance defaults on.
Cover Claude Code, Claude Cowork, Cline, Cline v2, Codex, Copilot, Grok, Grok Bot
and OpenCode. Shared runtime behavior is PowerShell; adapters translate events.
Acceptance criteria support the operator-selected formats `Gerkin`, `EARS` and
`Plain`; default/backfill `Plain`, with structured JSON in the existing text field.

Intent source: GraphRAG document `adhoc-ffa040f73459499ba82ad8912e4046f3`, memory
`MEMORY-PROJECT-001`, source key
`transcripts/compare-kaparthy-ponytail-bdpv4/0446942c2aac9ac4`, SHA-256
`0446942C2AAC9AC436017F51BE9F12B0F631FB233BB9F0482ACF5967267AF20C`.
The cancelled validation in the transcript conveys no approval.

Reviewed baseline: `e7ec28f90cdcd5da355c9e2e4ece2accf3dc73c9`; subsequent review
manifests pin their own commit and exact plan hash. Inspection at `5187b7b6` is
historical. `SessionLogService.ValidateTerminalTurnCompliance` exists today;
`TurnWritePipeline`, `CompletionGate`, classification/reviews and AgentContext
are planned foundations, not implemented capabilities of this baseline.

The [architecture](../architecture/SessionTurn-Review-AgentContext-Architecture.md)
and [base plan](PLAN-SESSIONTURN-SESSIONREVIEW-001.md) remain authoritative until
explicitly amended. The base plan is **NOT APPROVED**. Its approval, analyzer
merge, P0 prerequisites, turn identity, shared components, CompletionPolicy and
CompletionGate enforcement change set are dependencies. This plan cannot activate
that gate independently. Phase 1 amends architecture sections 3.5, 5.3, 6 and 8,
component/interface/data/activity/sequence diagrams, golden contracts and rollout
together, including the new acceptance-criteria contracts in section 4.

Within platform/system/developer constraints, precedence is explicit operator/user
instructions, workspace rules, selected process, then engineering guidance. Put
this order in the baseline. It grants no model authority to disable an
administrative gate; operator configuration changes are separately audited.

## 2. Canonical asset, provenance and distribution

Canonical asset `plugins/core/lib-ps/policy/engineering-policy.json` and descriptor
`engineering-policy.manifest.json` use UTF-8 without BOM, LF/final LF, two-space
indentation and ordinal property order. Initial policy version `1.0.0` uses semver;
instruction changes increment version and hash. SHA-256 covers exact canonical
asset bytes, not the manifest. Reject noncanonical encoding to avoid divergent
normalization. Asset fields: `schemaVersion:1`, `policyVersion`, ordered sections
`baseline`, `planning`, `implementation`, `review`, `bdpv4`, each with `id`, `text`,
`sourceIds`. Descriptor fields: schema/policy version, asset relative path,
lowercase hex SHA-256, ordered section IDs and source records (`id`, immutable
URL, commit, source path, declared license, notice or null, adaptation decision).

Reconciled rules: establish observable success criteria; surface consequential
ambiguity; resolve routine choices autonomously and record material assumptions;
inspect affected callers/tests/fixtures/configuration/interfaces; prefer project
capabilities, platform facilities, then installed dependencies; make the smallest
change meeting all ACs; preserve security, accessibility, validation, interfaces
and data integrity; refactor only as needed for the increment; use the selected
verification process and its cumulative gates. A self-check cannot replace BDPv4.
During requirements/planning, explicitly establish each criterion's format and
observable outcome before implementation; no forced conversion from Plain.

Pinned primary sources:

- [Karpathy guidelines](https://github.com/multica-ai/andrej-karpathy-skills/blob/2c606141936f1eeef17fa3043a72095b4765b9c2/skills/karpathy-guidelines/SKILL.md),
  commit `2c606141936f1eeef17fa3043a72095b4765b9c2`, skill blob
  `6a62d0441753157ca6ca50479e490c2948033adb`. Frontmatter declares MIT, but no
  root license notice was found. Do not invent one or vendor the text. Initial
  delivery uses original wording of the agreed rules above, with provenance;
  upstream adaptation remains excluded until notice verification/Phase 1 approval.
- [Ponytail](https://github.com/DietrichGebert/ponytail/blob/09c434ab73ca43761c5d8b0c5d341926c2fba710/skills/ponytail/SKILL.md),
  commit `09c434ab73ca43761c5d8b0c5d341926c2fba710`, skill blob
  `d64edd32265ba21cbf5270158f04e97d96925f27`;
  [MIT notice](https://github.com/DietrichGebert/ponytail/blob/09c434ab73ca43761c5d8b0c5d341926c2fba710/LICENSE),
  blob `715d483338cea4365f0d91a27799cf61226d6bcf`, Copyright (c) 2026 Dietrich
  Gebert. Preserve the complete notice for adaptations. Do not introduce personas,
  response formats or separate modes.

No runtime upstream fetch. Unresolved license text selects original wording,
never absence of policy. Server build embeds an identical canonical asset and
descriptor; resolver/status/receipts verify version/hash equality. Low-level
`sync-plugin-core.ps1` distributes them; extend build discovery/generator/fixtures
to Grok Bot, currently omitted. Generated copies are checksum-protected and never
hand-edited. Packaged text must satisfy `PluginPowerShellOnly.Tests.ps1` content
and path restrictions; design documentation is not copied as runtime instructions.

## 3. Configuration and status

Validated `EngineeringPolicyOptions`, canonical `QBrainAi:EngineeringPolicy`
projected onto `Mcp:EngineeringPolicy`:

- `GuidanceEnabled`: instance bool, default true.
- `EnforcementEnabled`: instance bool, default false for compatible deployment.
- `HookReadTimeoutMs`: int, default 100, range 10..250.
- `MaxInstructionBytes`: int, default 8192, range 1024..8192.

Use IOptionsMonitor. Invalid startup fails validation; invalid reload retains last
valid settings and emits a diagnostic. Valid reload increments policy generation;
enforcement reads current settings each completion, guidance next delivered
prompt. Disabling guidance does not disable enforcement.

Workspace `Mcp:Workspaces:<name>:DevelopmentProcess` is `None|BDPv4`, default None.
Only the operator's canonical configuration/deployment workflow can change it;
add no agent setter. Workspace API keys cannot change administrative settings.
Record operator identity/reason/old/new values in administrative audit. Register
this workspace's BDPv4 selection explicitly at rollout. No per-turn opt-out;
evaluate/stamp current selection at completion. A change to None is an explicit
administrative decision, not an inference from a prompt or agent DTO.

Read `GET /qbrainai/engineering-policy/status` (alias `/mcpserver/...`), client
`EngineeringPolicy.GetStatusAsync`, REPL `workflow.engineeringPolicy.status`, MCP
`engineering_policy_status`. JSON: `schemaVersion`, `policyVersion`, `policySha256`,
`policyGeneration`, `guidanceEnabled`, `enforcementEnabled`, `developmentProcess`,
`agentContextMode`, `assetAvailable`, `compatible`, `minimumPluginVersion`,
`diagnostics` (ordered code/message pairs).

`mcp-status.ps1.engineeringPolicy` adds `packagedVersion`, `packagedSha256`, `host`,
`hostSessionKey`, `lastRequestId`, `deliveryOutcome` (Delivered/Duplicate/Disabled/
Unavailable/Unsupported), `deliveredSections`, `deliveredBytes`, `limitations`,
`lastDeliveredUtc`. Unknown server selection stays unknown, never None. Cache the
last verified descriptor only within its host session. Without one deliver the
packaged baseline/activity guidance plus an unavailable-selection diagnostic; do
not claim BDP section delivery. Version/hash mismatch prevents gated Code
completion; server policy never derives from a client's status claims.

## 4. Acceptance-criteria formats and JSON-in-text contract

Add `Format` / JSON `format` per AcceptanceCriterion, string enum with exact values
**`Gerkin`, `EARS`, `Plain`**. `Gerkin` is the operator-requested wire spelling for
Gherkin. Do not silently rename it or infer format by parsing text. Both service
and client AcceptanceCriterion models gain the field; requirement criteria and
execution TODO criteria use the same contract. Preserve `id`, `text`,
`isSatisfied`, `evidence`; the new field describes **that criterion's text**.
Plain is the default when omitted on create or read of legacy data. An update
omitting format preserves existing format; an explicit format/text pair is
validated atomically. Unknown/null/empty enum values are rejected, not coerced.

Keep `RequirementAcceptanceCriterionEntity.Text` as the existing database text
column. Add non-null `Format` string column (max 16, default `Plain`, check values
where supported) to its criterion rows. Migration explicitly backfills **every
existing row to Plain without altering Text**, even text that resembles JSON.
Execution TODO's existing JSON state serialization in TodoExecutionService
likewise gains format with Plain default; update its state upgrade/read/write
and rollback snapshots, plus all YAML/SQLite adapters, imports/exports,
clones, snapshots, batch writes and federation projections so it round-trips.
No second payload column, flattened replacement, or automatic text conversion.
The current requirement relational row is RequirementAcceptanceCriterionEntity;
execution state has no corresponding relational AC row at this baseline. Upgrade
existing execution-state criterion objects explicitly with format Plain on the
first atomic state rewrite, preserving their text. Register that data upgrade
alongside the relational migration and test rollback snapshots. Do not invent a
replacement acceptance-criteria table for execution state.

For Gerkin/EARS, Text stores one JSON object serialized as a string by the outer
DTO, never a nested replacement for the public text property. Validate using
checked-in JSON Schema draft 2020-12 at
`docs/schemas/acceptance-criterion-gerkin-v1.schema.json` and
`docs/schemas/acceptance-criterion-ears-v1.schema.json`. Schemas below are normative
plan contracts. `$id` is a local schema identifier, never remotely fetched.
Reject duplicate JSON keys, unknown properties, unsupported schemaVersion, invalid
format/payload combinations and invalid cross-field rules with HTTP 400
validation_error, code `acceptance_criterion_format`, criterion ID and JSON pointer.
Maximum Text UTF-8 size 64 KiB, JSON depth 16; each nonempty string is trimmed for
validation but original Plain text is preserved. Canonical structured writes use
UTF-8 ordinal-key compact JSON, no BOM, no implicit natural-language conversion.

The [Cucumber reference](https://cucumber.io/docs/gherkin/reference/) supplies
Gherkin keyword semantics; [EARS author's guide](https://alistairmavin.com/ears/)
supplies EARS pattern/clause semantics. The JSON shapes and restrictions here are
local storage contracts, not upstream schemas or claims of full language support.
Gerkin v1 is a bounded Given/When/Then scenario subset, not the complete Gherkin
language: no Background, Rule, Examples, doc strings or data tables in v1. One
criterion contains one scenario; separate examples are separate criterion IDs.
Step `And` inherits the preceding phase, `But` is an assertion modifier only.
Require one or more Given steps, then one or more When steps, then one or more
Then steps; no backwards phase transition. Every step has stable unique local ID.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "urn:mcp:ac:gerkin:1",
  "type": "object",
  "additionalProperties": false,
  "required": ["schemaVersion", "scenario", "steps"],
  "properties": {
    "schemaVersion": {"const": 1},
    "scenario": {"type": "string", "minLength": 1, "maxLength": 1024},
    "steps": {
      "type": "array", "minItems": 3, "maxItems": 128,
      "items": {
        "type": "object", "additionalProperties": false,
        "required": ["id", "keyword", "text"],
        "properties": {
          "id": {"type": "string", "pattern": "^[A-Za-z][A-Za-z0-9_-]{0,63}$"},
          "keyword": {"enum": ["Given", "When", "Then", "And", "But"]},
          "text": {"type": "string", "minLength": 1, "maxLength": 4096}
        }
      }
    }
  }
}
```

EARS v1 stores structured clauses and renders standard templates. `pattern` is
Ubiquitous, EventDriven, StateDriven, UnwantedBehavior, OptionalFeature or Complex.
`system` and `response` are always required; trigger/state/feature clauses below
are required only for their patterns, disallowed otherwise. Complex requires
state AND trigger; OptionalFeature requires feature; UnwantedBehavior requires
trigger (the unwanted event). OptionalFeature+event/state combinations can be
separate criteria in v1. No free-text template that disagrees with stored clauses.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "urn:mcp:ac:ears:1",
  "type": "object", "additionalProperties": false,
  "required": ["schemaVersion", "pattern", "system", "response"],
  "properties": {
    "schemaVersion": {"const": 1},
    "pattern": {"enum": ["Ubiquitous", "EventDriven", "StateDriven", "UnwantedBehavior", "OptionalFeature", "Complex"]},
    "system": {"type": "string", "minLength": 1, "maxLength": 1024},
    "response": {"type": "string", "minLength": 1, "maxLength": 4096},
    "trigger": {"type": "string", "minLength": 1, "maxLength": 2048},
    "state": {"type": "string", "minLength": 1, "maxLength": 2048},
    "feature": {"type": "string", "minLength": 1, "maxLength": 2048}
  },
  "allOf": [
    {"if": {"properties": {"pattern": {"enum": ["EventDriven", "UnwantedBehavior", "Complex"]}}}, "then": {"required": ["trigger"]}, "else": {"not": {"required": ["trigger"]}}},
    {"if": {"properties": {"pattern": {"enum": ["StateDriven", "Complex"]}}}, "then": {"required": ["state"]}, "else": {"not": {"required": ["state"]}}},
    {"if": {"properties": {"pattern": {"const": "OptionalFeature"}}}, "then": {"required": ["feature"]}, "else": {"not": {"required": ["feature"]}}}
  ]
}
```

Render respectively: `The <system> shall <response>`; `When <trigger>, the ...`;
`While <state>, the ...`; `If <trigger>, then the ...`; `Where <feature>, the ...`;
`While <state>, when <trigger>, the ...`. Renderers HTML/Markdown-escape user
clauses; never execute steps or clauses. Plain uses current rendering unchanged.
Read/edit/export surfaces show format and human-readable structured content,
retaining the original canonical JSON for round-trip/import; malformed stored
structured text is visible as invalid raw text with a diagnostic, never Plain.

Existing clients omitting format still create Plain; modern updates preserve it.
Before structured writes are enabled, deploy all compatible clients/renderers.
Legacy full replacement that omits format on an existing structured criterion is
rejected (`acceptance_criterion_format_required`) rather than resetting it to
Plain. Explicit conversion requires new format and text, validates both, keeps
criterion identity and audit history, invalidates prior satisfaction/evidence and
linked engineering receipts. Format/text/schemaVersion participate in AC revision
digest; no format satisfies an AC automatically. Coverage keys use criterion ID;
Gerkin adds step IDs for Then assertions, EARS covers every clause and response.
Down migration refuses while any non-Plain criterion exists: operator must export
and explicitly convert first, preserving audit. No lossy automatic downgrade.

Owners: Requirements and TODO storage/services coordinate SharedComponents'
`AcceptanceCriterionCodec`/validator, typed clients/REPL/MCP and renderers.
Reuse existing create/update/batch APIs, extending the criterion DTO; no new CRUD
framework. Add `AcceptanceCriterionFormatTests`, schema positive/negative fixtures,
all six EARS patterns, Gerkin phase/ID rules, default/backfill/round-trip/update/
conversion/legacy replacement tests, three-provider Up/Down/Up with unchanged Plain
text hashes, YAML parity, rendering escaping and coverage-staleness tests.

## 5. Nine-host delivery contract

Invocation means one delivered human prompt, including absorbed mid-turn messages;
not tool/background events. Deliver baseline plus all three compact activity
sections every time, and BDPv4 only when selected. No prompt-time requestClass or
natural-language activity classifier. This also teaches format establishment.

Use base arrival identity. Dedupe `(hostSessionKey, requestId)` or stable ingress
nonce before mint, preserving aliases, in `turn-state.yaml` under `turn-open.lock`.
Reserve/commit delivery; retry reserved-but-uncommitted output after a killed hook;
mark Delivered only after successful output construction. Duplicate emits no
second policy. Preserve base indistinguishable-arrival/single-registration rules.

Planned feature-matrix rows (P0.2 proves actual delivery; exit code alone is not
proof of model context):

- Claude Code: delivered UserPromptSubmit, `hookSpecificOutput.additionalContext`,
  payload `session_id`, bridge request/q-nonce; next prompt after compact marker.
- Claude Cowork: same channel/binding, only if its P0.2 hook probe passes; otherwise
  Unsupported with explicit adapter requirement.
- Codex: trusted UserPromptSubmit, `hookSpecificOutput.additionalContext`,
  `session_id`/`turn_id`; each fresh prompt, no assumed compaction hook. Requires
  operator hook trust and base live assertion.
- Copilot: UserPromptSubmit, host result `additionalContext`, session/base cp-nonce;
  require proven consumption, single registration and equal-field refusal.
- Grok: UserPromptSubmit, `hookSpecificOutput.additionalContext`, camelCase
  `sessionId`/`promptId`; next prompt after compaction marker.
- Grok Bot: corresponding cloud prompt hook/output, same identity only after its
  own P0.2 proof; missing injection capability is Unsupported, not analogy support.
- Cline classic: native PowerShell UserPromptSubmit hook, `contextModification`,
  taskId/base cl-nonce; prove that output reaches model; base single-hook rules.
- Cline v2: per-message arrival adapter inserts policy system-context item in
  outgoing model request before dispatch, never edits user text; base task/run
  session+nonce; requires one run per message and proven injection seam.
- OpenCode: message-arrival adapter inserts policy system-context item in outgoing
  request; host session/message identity; restored requests redeliver. P0.2
  records actual API/fixture; MCP discovery alone is not delivery.

Record event/channel/restoration/dedupe/limitation for each in
`docs/AGENT-PLUGIN-FEATURE-MATRIX.md`. Failed probes block that host's supported
rollout, not silently choose different semantics. Final acceptance needs nine
passing rows. PostCompact is silent (FR-MCP-115/TR-MCP-PLUGIN-009/TEST-MCP-155);
its marker only schedules actual next-prompt text. Resume keeps base session.

Stateless package has mandatory EngineeringPolicy item beside memories/rules;
suppress separate hook text only if that request's actual package contains it.
Pilot/Off use prompt delivery. Preserve authoritative history-exclusion/refusal
prerequisites. Maximum 8192 UTF-8 instruction bytes, ceil(bytes/4) estimated tokens,
counted before optional items. Never truncate mandatory text/memories; package
overflow is existing 422 Overflow and capable hosts refuse dispatch. A host unable
to refuse follows base Mode=Off limitation.

Read/hash bound: HookReadTimeoutMs or remaining total hook deadline, whichever is
shorter. No prompt-hook network fetch; cache selection at bounded SessionStart/
first-prompt status read. Failure emits short Unavailable diagnostic in the host
envelope and failsafe audit, never Delivered; stateful hooks keep existing exit
behavior. Server completion still requires evidence, independently of injection.

## 6. Evidence trust and source snapshots

Turn-scoped evidence is independent of AgentContext Mode; do not reuse its
response-scoped, mode-gated relationships as authoritative storage.

Authoritative workspace server computes SourceSnapshot v1 from its registered
local checkout before validations and immediately before completion preflight.
No agent-supplied digest. Unavailable checkout yields Unknown/refusal. LocalProxy
forwards live to owner, never verifies a different checkout or queues success.

Snapshot: HEAD; `git ls-files -z --stage`; `git status --porcelain=v2 -z
--untracked-files=all`; every tracked path's current raw-byte SHA-256/mode/length
and index blob/mode, deleted markers, every untracked nonignored regular file.
Tracked ignored files remain included. Normalize relative path separators to /,
sort ordinally; reject non-UTF-8/non-NFC/case-colliding paths, links, submodules and
escapes (`engineering_snapshot_unsupported`). Canonical UTF-8/LF JSON manifest
digest; capture twice, disagreement gives retryable snapshot_changed, no verified
row. Include all tracked files, no agent-chosen affected subset. Ignored untracked
outputs are excluded; artifacts are outside source, never exclude tracked files.

Materialize exact captured bytes into isolated server-owned checkout outside
source, with writable output directories. Hash before/after each validation;
input mutation invalidates receipt. Native platforms use identical bundle bytes,
not checkout-dependent line endings. Policy, effective AC revisions (including
format/payload), inventory and process generation are additional dependencies.

Explicit architecture amendment: new EngineeringSnapshotResolver/Freshness,
separate from committed-only SourceResolver/IResponseStalenessRule. AgentContext
retains its committed-file semantics. Any manifest/HEAD/index/AC/policy/inventory
change stales engineering evidence for new claims. Completion certifies immutable
snapshot as of preflight, never promises a mutable tree stays unchanged. Double
capture detects ordinary concurrent edits, not a malicious same-user race after
last read; record this limitation instead of claiming a filesystem transaction.

Only server-owned `EngineeringValidationRunner` creates Verified receipts. Agent
requests named inventory gate; cannot submit trusted Pass/counts/commands/origins.
Runner executes approved fixed argument vectors via pwsh.exe in isolated snapshot,
no shell interpolation/arbitrary arguments. Idempotency key+payload hash; one job
per snapshot/gate/platform/attempt. Parse original TRX/Pester reports and exit code;
check expected projects/test IDs/counts, failures/skips, scope/time/platform and
artifact hashes. Missing/contradictory/empty output is Unknown. Raw reports and
stdout/stderr are content-addressed outside checkout, sanitized on read; uploaded
agent attachments stay Unverified and never satisfy a gate.

Native Linux/Windows release runs use operator-provisioned validation instances
on those machines, not builds against another host's mounted source. Cross-host
receipts require authenticated server-to-server retrieval from operator-pinned
producer, raw hash recheck and producer stamp; no agent registration route.
Unreachable/unregistered producer yields Unknown. Legion2 plan review uses scratch
only. Runner protects against invented reports, not dishonest executable tests
or compromised admin/storage. Existing execution isolation/access controls plus
coverage review and mutant checks remain required; state residual risk honestly.

## 7. Storage and public operations

TurnReviews owns contracts/handlers, StorageSchema registered tables/migrations,
SharedComponents hashing/error contracts. New independent entities:

- EngineeringSourceSnapshot: PK (WorkspaceId, SnapshotId=manifest SHA-256), HeadSha,
  ManifestJson, BundleSha256, CapturedUtc, ProducerInstanceId.
- EngineeringEvidence: PK (WorkspaceId, EvidenceId=server UUID), turn FK and
  immutable Agent/SessionId/RequestId, snapshot FK, Stage (Red/MocksGreen/Green),
  Purpose (Progress/SliceExit), TodoId, PolicyVersion/Sha256/Generation,
  DevelopmentProcess, RequirementsDigest, InventoryDigest, CoverageJson,
  ReviewRefsJson, CreatedUtc, Revision (long default 1).
- ValidationReceipt: PK (WorkspaceId, ReceiptId=server UUID), EvidenceId FK, GateId,
  Attempt, Platform, ProducerInstanceId, JobState (Queued/Running/Finished),
  Verification (Unverified/Verified/Unknown), Result (Pass/ExpectedRed/Fail/Unknown),
  CommandJson, ScopeJson, nullable ExitCode/Passed/Failed/Skipped/Total,
  ArtifactsJson, StartedUtc, nullable FinishedUtc/DiagnosticCode.

Composite FKs Restrict; unique evidence turn+revision and receipt evidence/gate/
platform/attempt; indexes turn, TodoId, snapshot, job state/time. Coverage entries:
requirementId/acId/revision/testId/gateId plus structured assertion refs from
section 4. Review refs name immutable existing reviews with identical dependency
digests. Validate active-layer refs, no free-text coverage. Historical turns have
no new row/backfilled Pass. Turn gains nullable EngineeringCompletionStampJson,
default null, evidence/revision/snapshot/policy/requirements/inventory/process/
generation/preflight UTC. AC format migration's Plain backfill is separate from
the prohibition on manufacturing validation evidence.

Every set: explicit COPY-SANITIZE+HISTORY+INERT wiki dump policy; exclude raw artifact
bytes. Federation domain engineering_evidence local-only: no offline queue/fanout,
LocalProxy forwards live. Copied/imported history is inert/unverified, never proof.

Phase 2 unregistered entity stubs/interfaces only, no DbSet/migrations. Phase 4
StorageSchema registers models and SQLite/SQL Server/PostgreSQL Up/Down/Up; new
verification defaults Unverified and stamps null. Binary rollback retains tables.

Operations, with /mcpserver aliases and explicit base-bound turn identities:

- POST `/qbrainai/engineering-evidence/{agent}/{sessionId}/{requestId}`:
  CreateEngineeringEvidenceCommand; Stage/Purpose/TodoId/coverage/review refs;
  server captures dependencies; client EngineeringEvidence.CreateAsync, REPL
  workflow.engineeringEvidence.create, MCP engineering_evidence_create.
- POST `/qbrainai/engineering-evidence/{evidenceId}/validations`:
  RunEngineeringValidationCommand; GateId/Platform/Attempt/idempotency key only;
  202 ReceiptId/job state; RunValidationAsync, workflow.engineeringEvidence.runValidation,
  engineering_validation_run. Queued is not Pass.
- GET `/qbrainai/engineering-evidence/{evidenceId}`: GetEngineeringEvidenceQuery,
  sanitized evidence/receipts/freshness; GetAsync, workflow.engineeringEvidence.get,
  engineering_evidence_get. Poll read for jobs.
- Internal VerifyEngineeringValidation only: runner parses/stores artifacts before
  completion transaction. No external Verified setter. Section 3 defines status.
  Existing completion DTO adds EvidenceId/Revision refs only.

ClassifiedErrorException: 400 validation_error invalid stage/coverage/command/key
reuse; 404 not_found scoped ID; 409 engineering_snapshot_changed (retryable),
engineering_snapshot_unavailable/unsupported, engineering_policy_incompatible,
engineering_evidence_stale (remediation required); 503 backend_unavailable for
producer/artifact outage. Gate rejection uses section 8's canonical contract.

## 8. Completion, progress and atomicity

Turn completion may certify Progress; slice/TODO completion is distinct SliceExit.
Reuse existing TODO transitions. Effective Code is declared Code OR any opening
to current snapshot change outside docs/**/*.md, docs/**/*.txt, README.md,
CHANGELOG.md, LICENSE, LICENSE.md; renames check both ends, mode/executable changes
always Code. Linked Code slices remain Code even without source changes. Missing
opening capture blocks Code-relevant completion until operator-reviewed baseline
evidence, not an invented old hash. Docs/Chore cannot downgrade derived Code.
Only enforced BDPv4 effective Code requires these additional stages:

- Red/Progress: active implementation TODO, newly named tests fail for expected
  assertion/behavior (not compile/infrastructure error); prior iteration suite
  passes zero failures/skips. ExpectedRed receipt, never Pass. AgentReview may
  be Other explaining RED; hostileReview Other `pending HV <gate>` is permitted.
- MocksGreen/Progress: approved mock/stub acceptance suite plus mutant/oracle
  checks and prior suite green, zero failures/skips; certifies test design only.
- Green/Progress: all slice inventory gates/active ACs green with zero failures/
  skips; pending-HV Other allowed while independent review runs, not slice exit.
- Green/SliceExit: cumulative gates/coverage and fresh independent HV Pass,
  accuracy/completeness >=0.98, all claims PASS, no FAIL/UNKNOWN. Red/MocksGreen
  cannot close implementation slices. Approved Phase 3 test-design TODO may close
  at MocksGreen only after its phase HV/approval; type fixed in approved inventory.

Explicit amendment of architecture 5.3: base CompletionPolicy still accepts
Pass/Fail/Other presence. Additional EngineeringCompletionPolicy imposes above.
Failed keeps base class-only rule, canceled/cancelled exempt. Pending-HV Other
names gate; later additive review records actual outcome, preserving history.
TODO done/complete requires current SliceExit reference (or approved test-design
exit); Progress cannot bypass final gate. Server-controlled administrative import/
correction exemptions audited, never agent-supplied origins.

Extend CompletionGate inside TurnWritePipeline. PrepareEngineeringCompletion
does Git/artifact reads and verification outside SerializableWrite and persists
preflight dependency version. Inside DB-only transaction, EngineeringCompletionPolicy
runs after CompletionPolicy, checks verified receipt/evidence revisions, snapshot
equality, current policy/process generation, AC/inventory revisions and review
refs. CAS mismatch rejects; no filesystem/Git/process/GraphRAG I/O in transaction.
Prepare all bulk dependencies before the atomic write. Artifact bytes verified
at preflight, never accepted by trusting agent paths.

Gate every Agent transition into completed and PUT/Replace carrying completed,
including already-completed Replace, using current preflight; failed uses base
checks. Additive terminal enrichment is not re-gated/stamp-mutated. Cover bulk,
PATCH/PUT, tools, typed client, REPL, recovery; QBAgent counts as Agent. Preserve
server-controlled Import/Replication/ServerInternal exemptions; reject agent origin
fields. All refusal rolls back whole write, reusable DbContext clean.

HTTP400 validation_error prefix session_turn_completion_gate; failedRequestIds,
ordered missingItems: requestClass, agentReview, hostileReview, engineeringPolicy,
engineeringEvidence, engineeringSnapshot, engineeringValidation, engineeringCoverage,
engineeringHostileReview (only applicable items, with remedy codes). Phase 1
QB-AGENT-001 C8 CONTRACT amends golden
`docs/context/session-turn-review-contract.json` and Get-CloseRemedy fixtures.
PS/client checks are advisory; display server items, never approve locally.

Failsafe engineering refusal is final for that attempted completion: preserve
event+refusal receipt, leave original turn in progress, clear attempted replay
entry, surface missing items for corrected new event; no quarantine/retry loop or
false persisted completion. Stop warns same items. Clear-session obtains valid
completion or deliberately fails/cancels under base rules, never invents evidence.
Outage/queued outcomes retain normal pending entries.

## 9. BDPv4 phases, ownership and gates

Implementation start queries effective requirements, creates MCP parent/dependent
slice TODOs and stable FR/TR/TEST records for sections 2–8/evaluation. Phase 1
updates Functional/Technical/Testing-Requirements, mapping and matrix, MCP records,
AC format schemas and all architecture diagrams/contracts. No registration claimed
by this document alone. Freeze TODO types, test/command inventory and licenses.

Phase 2: DTOs/interfaces/options/unregistered entities, PS seams/transport/schema
contracts and compiling stubs, no registered model/migrations/behavior. Phase 2
serialization/schema deliverables get actual contract tests, not skipped future
behavior placeholders. Phase 3: tests RED against stubs, green against approved
mocks plus negative/mutant oracles; cumulative prior tests green. No real-behavior
claim. Phase 4 real slices: next behavior RED, implement green, refactor, cumulative
gate before next slice, with evidence and MCP updates after meaningful changes.

Ownership/order: StorageSchema first (evidence and AC-format three-provider
migrations), SharedComponents second (hash/codec/schema contracts); then:

1. PluginCorePowerShell and BuildTooling own assets/resolver/delivery/status and
   generation/integrity respectively; coordinate shared files, independent owners
   may proceed in parallel only where approved by base process.
2. Requirements/TODO owners implement AC format validation, rendering, adapters,
   snapshots and all write paths after shared codec/schema; clients/tools follow.
3. TurnReviews and SessionTurnLogging implement evidence/runner/snapshot/gate after
   base completion/review foundations; no Phase C storage dependency.
4. ContextPackages/ResponsesAndTrust integrate mandatory policy/dedup/budgets,
   dependent on base Phase C package/response infrastructure; stateful guidance
   can proceed earlier. ReplAndAgents integrates transports and TODO-exit guard.
5. HostPluginAdoption after PluginCorePowerShell+BuildTooling: nine adapters,
   native probes/Pester/feature matrix. Documentation last: guides/receipts/rollout.

Phase 1 inventory freezes GateId, argument vector, project/test IDs/counts,
platform, stage, AC links and raw parser. Additions require reviewed inventory
revision, not agent-declared smaller scopes. Groups: EngineeringPolicyResolutionTests,
EngineeringPolicyDeliveryTests, EngineeringPolicyHostContractTests,
EngineeringEvidenceValidationTests, EngineeringCompletionGateTests,
EngineeringPolicyDistributionTests, AcceptanceCriterionFormatTests. Cover defaults/
reload/precedence, all host delivery/duplicate/crash/alias/compact/modes/overflow,
all snapshot changes and unsupported paths, forged/empty/skipped reports, staged
progress/pending HV, every writer/origin/Replace/bulk/CAS/replay/Stop/clear-session,
policy parity/notices/version and all section 4 format/migration cases.

Run natively via pwsh.exe; future flags/targets depend on base P0.1a/TR-MCP-NUKE-011,
not asserted to exist at baseline:

- Compile: `./build.ps1 Compile`.
- G1 targeted: `./build.ps1 Test --test-projects <inventory-projects> --test-filter <inventory-filter>`.
- G2 cumulative current/prior unit suite: `./build.ps1 Test`.
- G3: `./build.ps1 MigrationIntegrationTests --skip InstallTestDependencies`, all
  SQLite/SQL Server/PostgreSQL cases; `./build.ps1 PluginSessionLogIntegration`;
  explicit G1 selection of Support.Mcp.IntegrationTests, SessionLog.Validation,
  Repl.IntegrationTests, Acid.IntegrationTests with integration category enabled.
- G4 shared Pester: `./build.ps1 TestPluginCore`.
- G5: `./build.ps1 ValidateConfig` and
  `./build.ps1 ValidateTraceability --strict-tr-and-test-coverage`.
- G6: `./build.ps1 TestAgentPlugins --plugins <inventory-hosts>`; six PS hosts
  before HostPluginAdoption, all nine afterwards. Final
  `./build.ps1 ValidatePluginPowerShellOnly --agent-plugin-parent <isolated-parent>`.
- Build infrastructure exception: `dotnet test build/Build.Tests` directly when
  build tooling changes, after analyzer merge moves tests/Build.Tests there.

Phase 1 exit G5/traceability/architecture review; Phase 2 Compile/G1/G2/G4/G5/G6
and Build.Tests if needed; Phase 3 G1/G2/G4/G5/G6; each Phase 4 slice Compile and
cumulative G1–G6 plus applicable Build.Tests. Migration cases start Phase 4,
not Phase 2. Linux+Windows native evidence required for integration/rollout.
Exit gates require zero failures and zero skips; RED is intermediate evidence.

Every phase/slice arrow also requires independent HV (base named reviewer Codex
gpt-6-astra max), accuracy/completeness >=0.98, every claim PASS, and operator
approval. This plan's operator-authorized substitution is Claude Code
claude-opus-5-5/max, confidence >=98 as well; do not globally change base reviewer.
Receipts pin exact source/plan, actual reviewer configuration and limitations.

## 10. Bounded evaluation

Freeze engineering-policy-eval-v1 before results: 12 task fixtures (material
ambiguity, trivial choice, required interface, cross-caller change, RED/green,
staleness, compaction, unavailable policy, duplicate prompt, stateless overflow,
class/process downgrade, instruction conflict). Each has fixed text/snapshot/ACs;
include format establishment/round-trip in ambiguity/interface fixtures. Baseline
and integrated plugin, 3 fresh repeats per task/condition: 72 runs. Same Claude
Code claude-opus-5-5/max, fixed CLI, permissions and budgets; unexposed sampling
settings recorded unexposed. Predetermine alternating order, no selective retry;
infra-invalid paired runs retained and rerun both with recorded reason.

Independent condition-blind grader: correctness=correct exercised outcomes/total;
completeness=met ACs/total. Require integrated aggregates >=0.98 and not below
baseline, zero unauthorized scope/dependency, interface/security/data/accessibility,
precedence or fabricated-evidence violations, deterministic gates all green.
Report all per-task/repeat outcomes, rework, dependencies, change size, cost and
latency descriptively; no assumed efficiency gains/statistical generalization.
Nine-host equivalence is G4/G6 integration evidence, not this bounded model eval.

Named extra HV claims EP1 AC coverage/evidence (all formats), EP2 justified
dependencies/abstractions, EP3 affected callers/interfaces, EP4 boundaries/
precedence, EP5 receipt trust/freshness, EP6 honest measured evaluation. Retain
existing claims and 0.98 floors; all PASS. Deterministic evidence proves execution;
review assesses judgment. Neither replaces the other.

## 11. Rollout and rollback

No release SyncAgentPlugins in tests: it refreshes installed caches. Use low-level
sync into disposable isolated trees; never build/commit/push another host's mounted
source. Additive server/schema first, EnforcementEnabled=false, Mode=Off supported.
Base 1.120.0 sync and CompletionGate deploy precede this release. Reserve plugin
**1.122.0** (review used installed 1.121.0); Phase 1 checks all nine manifests/
caches for collision. Collision blocks release pending explicit amended version
and integrity revalidation, never hidden auto-bump. Compatible clients/REPL and
format-aware renderers before structured writes/enforcement; nine packages,
installed-equals-synced, trust steps and proven feature matrix before activation.

Inventory open turns at activation; missing evidence must be real, never
backfilled Pass. Missing opening snapshots need operator-reviewed baseline.
Enable isolated development, staging, then operator-approved BDPv4 workspaces.
Stamp current policy/process generation; do not reclassify historical completions.

Rollback operator sets EnforcementEnabled=false, retains records/artifacts/read
APIs; guidance disable separate. Restore compatible packages together. Prefer
binary rollback with additive tables retained; old clients cannot safely edit
structured criteria, so block their writes pending format-aware rollout. Schema
Down requires backup/approval and no non-Plain ACs (section 4). Plain backfill
does not alter text; no lossy conversion. AgentContext Mode rollback cannot disable
evidence storage. Reupgrade cannot trust stale/inert/imported receipts.

## 12. Review reconciliation

Claude round 1: DISAGREE; confidence 15, accuracy .80, completeness .52, all 12
claims failed. Exact output/transport metadata retained under
`docs/receipts/plugin-engineering-policy-hv-20261020/claude-round-1/`.
Author resolutions awaiting independent confirmation: F1 trust/class/process
(3/6/8), F2 snapshots (6), F3 DB-only/C8 (8), F4 RED/progress (8), F5 independent
storage/APIs/migrations (7), F6 nine-host channels/dedupe/budgets/restoration (5),
F7 no prompt-time classifier (5), F8 config/status (3), F9 asset/license/hash (2),
F10 inventory/gates/approvals (9), F11 replay/Replace/version/rollback (8/11), F12
bounded eval (10), F13 precedence (1), F14 baseline/approval/ownership/dependencies
(1/9). Operator additions: exact Gerkin/EARS/Plain discriminator, normative JSON
schemas inside existing Text, Plain default and explicit migration backfill (4).

Primary-source fallback used because Perplexity was unavailable. No product
build/test result or implementation approval is claimed by this plan.
