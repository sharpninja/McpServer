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
that gate independently. Phase 1 amends architecture sections 3.1, 3.2, 3.3
(C8/C9), 3.4, 3.5.2, 3.5.3, 3.5.6, 4.1, 4.2, 4.4, 5.1, 5.3, 5.4,
6, 7, 8 and Addendum A,
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
to Grok Bot through base A9 (do not duplicate its work). Copies are checksum-protected and never
hand-edited. Packaged text must satisfy `PluginPowerShellOnly.Tests.ps1` content
and path restrictions; design documentation is not copied as runtime instructions.

## 3. Configuration and status

Validated `EngineeringPolicyOptions`, canonical `QBrainAi:EngineeringPolicy`
projected onto `Mcp:EngineeringPolicy`:

- `GuidanceEnabled`: instance bool, default true. Effective workspace guidance is
  this value AND NOT its signed `EngineeringGuidanceSuppressed` setting; section18
  restricts suppression to Evaluation grader review workspaces.
- `EnforcementEnabled`: instance bool, default false for compatible deployment.
- `EvaluationGateRequired`: instance bool, default false for local evaluation;
  operator sets true on staging/production. Section20.2 defines activation checks.
- `HookReadTimeoutMs`: int, default 100, range 10..250.
- `MaxInstructionBytes`: int, default 8192, range 1024..8192.
- `StructuredAcceptanceCriteriaEnabled`: instance bool, default false; operator
  configuration only. Non-Plain create/conversion/edit while false is 409
  `acceptance_criterion_structured_disabled`; reads and explicit conversion to
  Plain remain available. An unrelated patch may preserve untouched structured
  criteria. Enable only after compatible clients/renderers ship.

Use IOptionsMonitor. Invalid startup fails validation; invalid reload retains last
valid settings and emits a diagnostic. Section 13 defines persisted generation
and audit. Enforcement reads current settings each completion; hook delivery
reloads at the next host session, Stateless at the next package build. Guidance
disable does not disable enforcement. Validate MaxInstructionBytes against the
largest canonical baseline+activity+BDPv4 rendering; insufficient size rejects
startup/reload, never truncates. Before first status, plugin limits are 100 ms/8192 bytes.

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
`diagnostics` (ordered code/message pairs), `hookReadTimeoutMs`,
`maxInstructionBytes`, `structuredAcceptanceCriteriaEnabled`, `evaluationGateRequired`,
`engineeringGuidanceSuppressed` (workspace bool), `graderContext` (section19.2),
`activationState` (Active|Dormant|HeldInactive), `buildArtifactSha256` (nullable
until a release manifest is installed). Section21 defines their semantics.
`guidanceEnabled` is the effective workspace value, not the unadjusted instance setting.

`mcp-status.ps1.engineeringPolicy` adds `packagedVersion`, `packagedSha256`, `host`,
`hostSessionKey`, `lastRequestId`, `deliveryOutcome` (Delivered/Duplicate/Disabled/
Unavailable/Unsupported), `deliveredSections`, `deliveredBytes`, `limitations`,
`lastDeliveredUtc`. Unknown server selection stays unknown, never None. Cache the
last verified descriptor only within its host session. Without one deliver the
packaged baseline/activity guidance plus an unavailable-selection diagnostic; do
not claim BDP section delivery. Client version/hash mismatch is advisory; gated
completion compares evidence policy hash to current server hash. Server policy
never derives from client status. Section14.1 defines refusal precedence.

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
Before StructuredAcceptanceCriteriaEnabled=true, deploy compatible clients/renderers.
Legacy full replacement that omits format on an existing structured criterion is
rejected (HTTP400 ClassifiedError `acceptance_criterion_format_required`) rather than resetting it to
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
sections every time guidance is enabled, and BDPv4 only when selected. Evaluation
grader suppression follows section18, including Stateless context. No prompt-time requestClass or
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
shorter. No ordinary prompt-hook network fetch. SessionStart status read, or the
single first-ingress read on hosts without SessionStart, has a fixed 250 ms bound
within the total hook deadline; timeout uses unknown selection/default limits,
not a second blocking read. Cache for that host session. Failure emits short Unavailable diagnostic in the host
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
no shell interpolation/arbitrary arguments. Idempotency key+payload hash; jobs
allocate per evidence revision/gate/platform/server attempt, with workspace-wide
failure memory (section15). Parse original TRX/Pester reports and exit code;
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
section 4. ReviewRefsJson contains append-only EngineeringReviewAttestation IDs
(section 13), not mutable latest-wins base reviews. Validate active-layer refs,
no free-text coverage. Historical turns have
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
  RunEngineeringValidationCommand; GateId/Platform/idempotency key only;
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
Reuse existing TODO transitions. When enforcement is off or process is None,
effective class equals declared class and opening baselines add no obligations.
For enforced BDPv4, effective Code is declared Code OR any opening
to current snapshot change outside docs/**/*.md, docs/**/*.txt, README.md,
CHANGELOG.md, LICENSE, LICENSE.md (root-anchored for the four filenames);
renames check both ends, mode/executable changes
always Code. Linked Code slices remain Code even without source changes. Missing
opening capture conservatively derives Code for every completed target, including
declared Docs/Chore; section 13's operator baseline command repairs it. Do not
invent an old hash. Docs/Chore cannot downgrade derived Code. Pass effective class
to base ReviewValidation/CompletionPolicy too; Unnecessary is invalid for derived
Code. Keep declared class separately for audit, store EffectiveRequestClass.
Only enforced BDPv4 effective Code requires these additional stages:

- Red/Progress: active Implementation or TestDesign TODO, newly named tests fail for expected
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
names gate; later review updates the base latest-wins projection and appends a
separate EngineeringReviewAttestation, preserving engineering history (section 13).
TODO done/complete requires current SliceExit reference (or approved test-design
exit); Progress cannot bypass final gate. Server-controlled administrative import/
correction exemptions audited, never agent-supplied origins.

Extend CompletionGate inside TurnWritePipeline. PrepareEngineeringCompletion
does Git/artifact reads and verification outside SerializableWrite and persists
preflight dependency version. Inside DB-only transaction, EngineeringCompletionPolicy
runs after CompletionPolicy, checks verified receipt/evidence revisions, snapshot
equality, current policy/process generation, AC/inventory revisions and review
refs. CAS mismatch aborts the attempt and reruns from preflight reads outside the
transaction, at most four whole attempts, then 409 conflict/retryable true; no
filesystem/Git/process/GraphRAG I/O in transaction. Section 13 fixes retry details.
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
false persisted completion. Stop blocks a delivered in-progress turn using those
items, preserving base queued/degraded/unsupported warning exceptions. Engineering
refusal itself is never reduced to a warning. Clear-session obtains valid
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

Ownership/order: this plan's StorageSchema first (engineering-table three-provider
migrations only), SharedComponents second (hash/codec/schema contracts). The
Format column/Plain backfill/DTO is a base StorageSchema/SharedComponents
prerequisite per15.5, not a duplicate migration here. Then:

1. PluginCorePowerShell and BuildTooling own assets/resolver/delivery/status and
   generation/integrity respectively; coordinate shared files, independent owners
   may proceed in parallel only where approved by base process.
2. RequirementsAndAcceptanceCriteria implements AC format validation, rendering, adapters,
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
- G2 cumulative current/prior unit suite: `./build.ps1 Test --test-run-id <job-id> --gate-started-at-utc <utc>`.
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
approval. Only this document's external plan-readiness loop substitutes Claude
Code claude-opus-5-5/max (confidence >=98 also); future phase/slice arrows retain
the base Codex profile unless separately overridden by signed scoped configuration.
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

Independent condition-blind grader under section18's signed context isolation:
correctness=correct exercised outcomes/total;
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
binary rollback with additive tables retained, but refuse rollback to any binary
older than format support while any non-Plain criterion exists in rows or TODO
state. BuildTooling's UpdateService guard (section14.6) scans both stores with writes quiesced and blocks
the rollout; enforcement off alone does not make downgrade safe. Schema
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

## 13. Round 2 contract completion

These details resolve R2-01 through R2-12; they are normative parts of the design,
not implementation choices to defer. Phase 1 propagates them to the architecture
sections listed in section 1 and QB-AGENT-001 C8/C9 CONTRACT entries. All new
entities below follow section 7's workspace-scoped FKs, sanitized inert dump,
local-only federation and Phase 4 migration rules.

### 13.1 Administrative authority, generations and opening baseline

Use a separate operator management listener bound to loopback, HTTPS with client
certificate authentication and policy `EngineeringPolicyAdmin`. Its pinned public
certificate identities are installed by the operator outside the checkout and
unavailable to workspace agents. Workspace API keys and replicated/imported
principals never satisfy this policy. These administration routes are **not**
exposed through MCP, REPL, ordinary typed agent clients or the public workspace
listener. A compromised operator/server administrator remains outside the threat
boundary. Management DTOs reject caller-supplied approvedBy/timestamps/generation.

`ConfigurationReconciliationService` is the audit writer for configuration files.
Operator deployment supplies a signed change manifest (workspace IDs, effective
settings, operator certificate ID, reason, deployment ID); signature and canonical
settings hash must match before applying a changed protected setting. No-op reload
and restart need no new signature or generation. Unattributed changed files are
rejected, retaining previous settings. In one DB transaction it updates
`EngineeringWorkspaceState` and appends `EngineeringPolicyAudit`. Fields:

- WorkspaceState PK WorkspaceId; Generation bigint default 1, effective policy
  hash, EnforcementEnabled, DevelopmentProcess, ActiveInventoryRevision nullable,
  CertifiedSnapshotId nullable, Revision bigint default 1, LastAppliedDeploymentId nullable.
- PolicyAudit PK (WorkspaceId, AuditId UUID); Kind, OperatorCertificateId,
  DeploymentId nullable, Reason, OldValuesJson, NewValuesJson, CreatedUtc, Generation.

Generation is persisted, monotonic and per authoritative workspace. Increment
only for an effective policy hash, EnforcementEnabled or DevelopmentProcess
change, not restart/no-op/guidance-limit changes. Inventory/AC changes have their
own revision/digests. Multiple owner instances serialize the same row; LocalProxy
reads owner state and never allocates a local generation. Status reports only a
committed generation. Completion's DB CAS compares this row. Restart compares
effective fingerprint with persisted values; it cannot reset Generation to 1.

MintSessionTurnCommand (SessionTurnLogging owner) copies CertifiedSnapshotId to
new turn fields OpeningSnapshotId (nullable FK) and OpeningSnapshotState
(Captured/Unavailable, default Unavailable), OpeningSnapshotBoundUtc nullable, and
EffectiveRequestClass (nullable). Do this DB-only within mint/first-persist; no
snapshot capture or added hook I/O. This is the **prior certified snapshot**, not
a claim to have captured the filesystem at the prompt's arrival. Only the explicit
baseline operation advances the certified pointer, with full-delta attestation and
history; Green/SliceExit never advances it implicitly (section 14).
Older baselines conservatively include other sessions' edits. Never subtract
another session's paths by trusting filesModified. Shared-checkout edits can
therefore force Code/extra evidence on a Docs turn; use isolated worktrees to
avoid this attribution ambiguity. If no certified baseline exists, all completed
targets derive Code and require a baseline; failed/canceled remain possible.

Operator-only `POST /admin/engineering-policy/{workspaceId}/baseline`, management
client `EngineeringPolicyAdmin.RecordBaselineAsync`, script
`tools/powershell/Set-EngineeringBaseline.ps1`, accepts TurnRefs (explicit agent,
session, request tuples, empty for future turns only), Reason, ReviewedDiffSha256,
HvAttestationId and ExpectedWorkspaceRevision. Server captures current snapshot
outside the DB write; operator's reviewed diff is HEAD-to-snapshot plus any prior
certified-to-current difference. It verifies the approved review attestation names
that diff/snapshot and records who accepted the baseline. Transaction updates the
certified pointer and only listed Unavailable opening references, appending an
audit; it never rewrites already captured openings or creates passing validations.
403 admin_authorization_required; 400 validation_error missing reason/review;
409 engineering_baseline_conflict for CAS/changed diff, snapshot errors as below.
The operation explicitly authorizes adopting existing changes as a baseline and
is never callable by an agent holding only the workspace key.

Snapshot capture has a 30-second wall bound, 100,000 entries, 2 GiB total and
64 MiB/file limits. Budget exhaustion returns final 409
engineering_snapshot_unavailable with reason capture_budget, no partial snapshot.
Jobs may request a fresh capture later. No mint waits for it. Double-capture
disagreement is 409 engineering_snapshot_changed, retryable true. Explicitly
extend C9/ErrorCodes to this one code; plugin treats it as outage/pending for a
later drain, never hot-retries. Preflight CAS/DB dependency races use the base
four-attempt reread rule (whole preflight and transaction, maximum 120 seconds),
then 409 conflict/retryable true. Each transaction delegate remains DB-only;
neither retry type creates a successful completion receipt or partial bulk write.

### 13.2 Operator-held validation inventory and runner isolation

`EngineeringValidationInventory`: PK (WorkspaceId, InventoryRevision bigint),
CanonicalJson, Digest, ApprovedBy, ApprovedUtc, HvAttestationId, CreatedUtc.
Append-only, active revision on WorkspaceState. Canonical JSON is ordinal-key,
compact UTF-8 with no BOM; digest is SHA-256 of these exact bytes. Schema:
schemaVersion=1; gates array sorted GateId, each GateId, Argv string array,
WorkingDirectory relative path, Platforms array, Stages array, ExpectedProjects
string array, ExpectedTests string array, ExpectedCount integer, AcLinks array,
ParserId, TimeoutSeconds (1..3600), ExpectedRedTests array; TodoTypes array of
{TodoId, Kind:Implementation|TestDesign, RequiredGateIds, RequiredClaims}.
No duplicate IDs/count mismatch/path escape. ExpectedRedTests entries contain
TestId, ExceptionType, AssertionMessageSha256 and StackFramePrefix. Positive
expected RED means every new failing result exactly matches that approved tuple;
any additional failure, compile exit or infrastructure exception is Fail/Unknown.
Prior cumulative tests must still pass without failures/skips.

GateId is one of Compile,G1,G2,G3Migration,G3Plugin,G3Support,G3Validation,G3Repl,
G3Acid,G4,G5Config,G5Traceability,G5UseCaseCoverage,G6,PowerShellOnly,BuildInfrastructure,MockOracles;
Platform is LinuxX64 or WindowsX64. ParserId is DotnetTrxV1, PesterNUnitV1,
TraceabilityBaselineV1, UseCaseCoverageV1 or ProcessExitV1. Compile/config use ProcessExitV1;
G5Traceability uses section 14's structured parser, not an exit-code exception.
A test gate cannot select ProcessExitV1. Pester emits original NUnit XML;
server parser counts all expected IDs and treats inconclusive/not-run as skipped.
G1/G2 and migration command vectors include `--test-run-id <server-job-id>` and
`--gate-started-at-utc <server-start-utc>`; these two typed placeholders are the
only runner substitutions, and cannot change command/scope. TestRunId is required
for G2 TRX/inventory generation at the inspected baseline.

Operator-only `POST /admin/engineering-policy/{workspaceId}/inventories` accepts
ExpectedWorkspaceRevision, canonical inventory payload, Reason, HvAttestationId.
It validates schema/paths/approved test IDs, hashes it, records authenticated
operator/time and atomically activates the next revision. Error 400
engineering_inventory_invalid, 409 engineering_inventory_conflict, 403 as above.
Typed management `ApproveInventoryAsync`; no agent tool or REPL setter. Runner
loads only that active DB revision, never an inventory from the editable snapshot.
New test/command additions require a new approved revision before satisfying an
exit. Approval review may assess a proposed inventory using a non-gating dry run;
only operator approval can activate it. Agent Create/Run cannot alter TodoTypes.

Runner broker is the server service account; executable test code runs as separate
non-admin `qbrain-validation` with no operator/producer credentials, workspace API
keys, server config/marker, SSH keys, user home mounts, host socket or ambient
secret environment. Allowlisted environment is PATH and approved nonsecret test
settings; per-job ephemeral data/cache/output directories, read-only source bundle
and controlled writable build outputs. Default no network; database suites get
only their isolated fixture network/ephemeral DB credentials. Host/container
isolation and secret-denial probes must pass before enabling this runner. Parser
and DB receipt writes run outside that sandbox; test code cannot mark Verified.
Each job enforces inventory timeout, then kills its own process tree and records
Unknown/timeout. Concurrency is one job per workspace to avoid fixture collisions.

Windows native executable is pwsh.exe; Linux native executable is pwsh, reached
through this workspace's required pwsh.exe compatibility entry. Provision and
verify that entry's executable target/hash before Linux gates; do not attempt to
execute a Windows binary there. All operator examples/shell invocations retain
the workspace's pwsh.exe rule; no alternate scripting shell is introduced.

### 13.3 Append-only review attestation and gate applicability

Base turn reviews are latest-wins projections, not immutable history. Add
`EngineeringReviewAttestation` PK (WorkspaceId, AttestationId UUID): TurnId
nullable (inventory/baseline review may be workspace-scoped), SubjectKind
(Slice/Inventory/Baseline/Evaluation), SubjectDigest, SnapshotId nullable, PolicySha256,
RequirementsDigest, InventoryDigest, ReviewerIdentity, Model, Effort, ReceiptSha256,
ArtifactRefsJson, ScoresJson, ClaimsJson, MappedVerdict, TrustLevel=OperatorAttested,
AttestedBy, AttestedUtc, SupersedesAttestationIdsJson nullable, CreatedUtc. Append only;
no update/delete API. Server captures dependencies rather than accepting hashes
from the agent; for proposed inventory/baseline subject it computes their digest.
Uniqueness by (WorkspaceId, ReceiptSha256, SubjectKind, SubjectDigest); indexes
turn/snapshot/subject. Store score lexemes under base lossless rules.

Operator-only `POST /admin/engineering-policy/{workspaceId}/review-attestations`,
management `AttestReviewAsync`, accepts SubjectKind, EvidenceId/Revision or proposed
inventory/baseline subject, raw reviewer receipt artifact, reviewer identity/model/
effort, Reason, SupersedesAttestationIds optional array. Server hashes/parses receipt bytes, checks scores,
claim list/verdict, configured reviewer profile and subject digest, and stamps
operator/dependencies. Raw artifacts are outside checkout. 400 review_* for base
validation, 409 engineering_review_stale, 403 for agent credentials. Read sanitized
attestation by existing engineering evidence read, no agent creation tool.

Bootstrap is not circular: proposed Inventory/Baseline attestations need no turn
or active evidence revision. Their subject is the server-computed proposed
inventory digest or captured snapshot/diff digest; absent nonapplicable dependency
digests use SHA-256 of canonical empty array, not caller values. Their required
claims are Scope, Completeness, Safety and Provenance, all PASS at .98 floors.
Approve inventory first, then attest/adopt a baseline; Slice attestations always
require active inventory and all real dependency digests. Reviewer profile is
the approved base Codex gpt-6-astra/max unless an operator-signed scoped override
exists (this plan-readiness loop's Claude override is not a production default).
Completion rechecks attestation model/effort/claims against the current profile.

Evidence creation allocates the next turn-local Revision in SerializableWrite;
each EvidenceId is immutable and belongs to exactly one revision. A changed
coverage/stage/dependency set creates a new revision/ID; agent APIs cannot update
or relabel an already verified receipt. Base four-attempt uniqueness retry applies.

Gate checks exact snapshot/AC/policy/inventory digests, receipt hash/existence,
all required claim IDs once with PASS, no FAIL/UNKNOWN, model/effort profile,
scores >=.98, authenticated operator attestation and correct subject. Agent-written
hostileReview Pass alone never satisfies it. Server verifies receipt integrity and
the operator's authorization; it cannot independently prove the remote model's
reasoning or independence. **Operator-attested reviewer identity/independence is
an explicit residual trust assumption**, not labeled cryptographically verified.
Operator approval at every slice exit already exists in BDPv4. Base review
projection can still be updated; prior engineering attestations remain unchanged.
This amends architecture 3.4/5.1/5.4 and gives pending-HV backfill real history.

Applicability is the following exhaustive ordered decision list (no table):

1. Trusted Import/Replication/ServerInternal keep base exemptions and inert data.
2. Agent target canceled/cancelled: no terminal or engineering gate. Target failed:
   base requestClass only. Other nonterminal writes: no completion gate.
3. Agent completed/Replace-completed always uses base CompletionPolicy with
   server effective class (equal to declared class when off/None). When enforcement is off or process None, add no
   engineering items, regardless of guidance/asset mismatch. Base review rules
   still apply. Purpose/stage carry no new obligations in this branch.
4. Enforcement on + BDPv4 + effective Docs/Chore: base items only. Effective class
   is server-derived using opening baseline even for declared Docs/Chore; absent
   baseline conservatively makes it Code. Chore can never exempt derived Code.
5. Enforcement on + BDPv4 + effective Code completed: engineeringPolicy,
   engineeringEvidence, engineeringSnapshot, engineeringValidation and
   engineeringCoverage required for every accepted Progress stage. Red coverage
   covers all current-increment ACs with named tests/expected outcomes, not a
   claim they pass; MocksGreen/Green apply section 8. engineeringHostileReview is
   additionally required for SliceExit and approved TestDesign exit. Purpose
   SliceExit with Red, or MocksGreen on Implementation, is invalid and reports
   engineeringValidation. Missing/unapproved TodoType reports engineeringEvidence.
6. In enforced BDPv4, Agent TODO done=true or terminal success requires final
   evidence only if TodoId is in active TodoTypes or was carried by a completed
   effective-Code turn. The latter without TodoType reports engineeringEvidence.
   All other TODOs (including ordinary docs/triage tasks) keep existing rules.
   TODOs have no invented declared class or opening snapshot; section 14 supplies
   the complete applicability cases.

HeldInactive under21.2 adds engineeringPolicy/obtain_activation_evaluation to
exactly rules5-6's Agent targets through normal HTTP400, never a completion409.
Other branches remain unchanged. Remedy codes respectively: engineeringPolicy=refresh_policy
(obtain_activation_evaluation instead when HeldInactive);
engineeringEvidence=record_evidence (approve_todo_type if missing/unapproved TodoType);
engineeringSnapshot=refresh_snapshot_or_baseline;
engineeringValidation=run_approved_inventory; engineeringCoverage=link_ac_tests;
engineeringHostileReview=obtain_attested_hv. Keep section 8's token order and
base missing-item order. Missing baseline itself reports engineeringSnapshot even
if other applicable items are also absent. None/off never require descriptor parity.

### 13.4 Delivery ledger, probes, ownership and interface details

Add turn-state policyDeliveries map keyed stable requestId/nonce, with each value
{requestId, ingressNonce, policyVersion, policySha256, sections[], bytes,
state:Reserved|Delivered|Disabled|Unavailable, reservedUtc, deliveredUtc:null|utc,
diagnosticCode:null|string}. PluginCorePowerShell functions Reserve-PolicyDelivery,
Commit-PolicyDelivery and Rebind-PolicyDelivery are the only writers under
turn-open.lock using object-first YAML mutation. Alias rebind changes map key
atomically; duplicate checks inspect original/aliased key. Duplicate is an event
outcome on the existing entry, not a stored state. Commit-PolicyDelivery commits
a text-free Disabled entry whenever effective guidance is false, including
baseline-condition runs and suppressed graders; bytes=0, sections=[], deliveredUtc=null.
Terminal Disabled entries dedupe/rebind like Delivered; tool-loop dispatch rechecks
the same bound entry. No policy text stored.
Retain compact delivered/alias tombstones for the lifetime of the host session's
durable identity, including ended/resumed sessions; do not time-prune them. Explicit
administrative session deletion ends that identity; a new identity starts fresh.
Reserved/unavailable rows remain retryable until successful delivery or terminal
request. Terminated requests are never reinjected by late replay. Host output
delivery means successful adapter emission, not proof the model obeyed guidance.

HostPluginAdoption owns new P0.2 consumption probes; PluginCorePowerShell owns
their shared harness/fixtures. For each of nine rows, a test-only policy descriptor
contains a random canary visible only through the chosen context channel; deliver
an otherwise identical prompt asking the host model to return the policy canary,
capture outgoing context where the host exposes it and the echoed result. No
canary in user text/environment/tool result. Probe initial/duplicate/restored
delivery and record actual host API/version/configuration/raw envelope; wrong or
absent canary fails support. This explicitly adds to base P0.2/Addendum A; no
claim that Read/Grep fixture tests alone establish native support.

File boundaries: TurnReviews owns new `src/QBrainAi.Services/EngineeringPolicy/`
(resolver/runner/inventory/attestation/freshness handlers) and matching service
tests. StorageSchema owns entities/McpDbContext/migrations. SharedComponents owns
shared DTOs and canonical codec. SessionTurnLogging alone changes existing
`SessionLogService`, TurnWritePipeline, mint and turn DTO stamps, calling new
interfaces. TurnReviews retains CompletionGate ownership. Public contracts and
controller/MCP/client stubs are SharedComponents Phase 2 shared-file work;
ReplAndAgents owns Phase 4 adapter behavior only. RequirementsAndAcceptanceCriteria changes RequirementsDatabaseDocumentService,
TodoExecutionService, EfTodoService and YAML TODO service adapters; coordinator
serializes these existing-service edits with SessionTurnLogging, not two agents
editing the same file. BuildTooling owns Nuke targets/infrastructure tests.

`EngineeringTodoExitPolicy` is a **server service guard**, called before every
done/complete/terminal-success mutation in TODO services, execution planning/status,
REST/MCP/REPL, YAML/SQLite and batch/import boundaries. Agent origin cannot bypass
it by changing a status alias or replacing an item; whole batch is refused before
file/DB save, code 400 validation_error, message token todo_engineering_exit_gate,
ordered missingItems and affectedTodoIds. Trusted origins are stamped internally
and audited. ReplAndAgents merely carries this refusal. Add path inventory and
bypass tests before activation, including YAML rollback and both batch backends.

All new schema strings: WorkspaceId 1024, Agent 128, SessionId/RequestId 256,
TodoId/GateId/claim IDs 128, UUID IDs 36, SHA-256 digests 64 lowercase hex,
Git HEAD 40 or 64 lowercase hex, policy version 32, enums 32, reasons/messages
4096, certificate/producer IDs 128. JSON columns are provider TEXT/nvarchar(max),
max 8 MiB per manifest, 1 MiB per inventory/evidence/attestation; enforce byte
limits before DB save. Timestamps UTC datetimeoffset; unmarked fields non-null,
nullable fields explicitly listed above. Artifact count <=256 and total <=1 GiB
per job; oversize is Unknown/artifact_budget, never partial Pass.

Canonical JSON shapes: CommandJson={executableId,argv:[]};
ScopeJson={projectPaths:[],testIds:[],filter:string|null};
ArtifactsJson=[{artifactId:sha256,kind:Trx|Pester|Stdout|Stderr|HvReceipt|DeliveryLedger|EvalProtocol|EvalRunManifest|EvalResult|EvalBlindGrades|EvalUnblindingMap|EvalAggregate,
sha256,byteLength}]; ReviewRefsJson=[attestationId];
CoverageJson=[{requirementId,acId,revision,testId,gateId,assertionKey}].
ManifestJson={headSha,entries:[{path,mode,length,sha256,indexBlob,indexMode,deleted}]};
deleted entries length/sha256=null, untracked index fields=null. No arbitrary URL
or filesystem artifact refs. Public completion fields `engineeringEvidenceId`
and `engineeringEvidenceRevision` (nullable string/int64) propagate unchanged
through bulk/PATCH/PUT/tool/client/REPL. The same pair is added to every TODO
terminal-success request and batch item; status aliases go through that DTO/guard.
An agent omitting the pair receives the guard's missing engineeringEvidence item.
Response stamp is server-only.

Plain assertionKey is acId; Gerkin is `<acId>#gerkin:<stepId>` for each effective
Then/Then-And/Then-But assertion; EARS is `<acId>#ears:<property>` for every
present semantic property (system,response,trigger,state,feature). Schema version/
pattern/format are digest dependencies, not separate test assertions. Root doc
names in effective-class exclusions are anchored `/README.md`, `/CHANGELOG.md`,
`/LICENSE`, `/LICENSE.md`; nested counterparts are Code unless under docs globs.

REPL ClientMutationPolicy: add engineeringEvidence to service-gated namespaces;
CreateAsync/RunValidationAsync writes are transactional enqueue/store commands,
never the long-running runner itself. GetAsync and engineeringPolicy.GetStatusAsync
are read-allowlisted, available while degraded. No admin methods registered.
Honor transaction_degraded/mutation_not_transactional and base service-gated
read/write coexistence amendment. Structured AC writes use existing requirements/
TODO mutator policies; extend golden/tool DTO tests for format, not raw REST bypass.

### 13.5 Evaluation placement and artifact lifecycle

Run each 72-run evaluation after Phase4 integration gates and identical pre-run
protocol freeze in every declared target store under21.3, before those targets'
staging/production activation. Each target independently records, grades, locks
and attests the same captured behavioral runs; no inert import promotes evidence.
Failure or invalid evaluation blocks staging/production activation; fix under RED/
green process, bump policy version/hash for any instruction change and rerun all
72, plus affected deterministic gates. At most one infrastructure rerun per paired
case (both conditions); a second infra failure is Invalid and blocks activation,
not silently removed. Full rerun after repair is a new versioned evaluation record.

Grader is separate Claude Code claude-opus-5-5/max session, read-only, fixed CLI
version, never the behavioral session/coordinator. Evaluation harness strips
condition labels, randomizes anonymous run IDs with a preregistered recorded seed,
normalizes artifact metadata that reveals package version, and gives grader task,
rubric and observable outcomes plus the neutral operator context defined in18.
Suppress engineering guidance for every grader prompt. Preserve reversible mapping outside grader
packet permanently; section19.1 locks blind grades and computes comparison on the
server, with no post-unblinding grader session. Some behavior
may reveal condition; record that limitation, do not claim perfect blinding.
Operator attests this evaluator receipt using the same review contract.

All active validation/HV artifacts, including interim receipts, stay in the
server-owned artifact store **outside the source checkout until completion**.
Base docs/receipts is a post-completion audit export only for this integration;
amend engineering review receipt-root handling in architecture 5.1/5.4 accordingly.
Do not create untracked JSON receipts under docs before a gate: they would change
the full snapshot and conservatively derive Code. An export is a later documented
source change needing its own completion, not retroactively part of the validated
snapshot. No broad exclusion hides source or receipt mutation from the manifest.

Round 2 actual result: DISAGREE, confidence 30, accuracy .86/completeness .72,
3 PASS/9 FAIL; exact receipt saved under claude-round-2. This section resolves its
twelve findings for the next independent review; no agreement is assumed.

## 14. Round 3 contract completion

The following rules resolve EP-R3-01 through EP-R3-14. They are part of the
approved-design candidate, to be propagated into the Phase 1 architecture and
Phase 2 contracts, not postponed product decisions.

### 14.1 Attempts, TODO applicability and refusal precedence

Attempt is server-assigned, never accepted in RunValidation input. For each
(evidence revision, GateId, Platform), allow one initial attempt and at most one
replacement for an infrastructure/producer/timeout Unknown. The latter retains
the first row with SupersededInfrastructureUnknown=true and SupersededByReceiptId;
it is the only exception to the all-finished-attempts rule. Every other finished
attempt must be Pass, or ExpectedRed for Red/Progress, and no job may remain
Queued/Running. Fail is never retried on the same evidence/failure tuple. A new
revision after Fail requires either a changed source snapshot or a changed
operator-approved inventory revision with independent HV and documented remediation;
unchanged snapshot+inventory revisions cannot erase failure. Independent review checks
the remediation, not just the fact a hash changed. A second Unknown blocks the
obligation. Once satisfied, repeated Run returns the existing receipt, not another
trial. Allocate attempt ordinal and idempotency result atomically; concurrent
requests cannot mint extra trials. Errors 409 engineering_validation_failed or
engineering_validation_retry_exhausted, final; raw failed attempts remain audit.
Tests include fail-then-pass refusal, different idempotency keys, concurrent runs,
one Unknown replacement, second Unknown, unchanged snapshot and RED totals.

TODO applicability is independent of any invented TODO requestClass:

- Off/None or trusted non-Agent origin: existing TODO completion rules only.
- Enforced BDPv4, TodoId in active TodoTypes: Implementation requires Green/
  SliceExit; TestDesign requires MocksGreen or Green with phase HV/approval.
- Enforced BDPv4, TodoId absent there but referenced by any completed turn whose
  persisted EffectiveRequestClass is Code: block with engineeringEvidence,
  remedy approve_todo_type; historical turns without that field are not inferred
  to be Code. A current Code turn cannot finish without the approved type.
- All other TODOs, including ordinary Docs and triage tasks, retain existing
  completion rules. Their acceptance criteria still use section 4's format rules.

Matrix tests exercise each branch through every backend, replace/batch/status
alias and transport, including a previously Code-linked TODO after removal from
the active inventory. Do not turn closing an incidental triage task into SliceExit.

Completion endpoints report policy/evidence defects only as ordered HTTP400
session_turn_completion_gate items. Evidence/current **server** policy hash
mismatch is engineeringPolicy; client package hash/status is advisory. Stale/
missing source is engineeringSnapshot, validation/coverage/HV as already named.
No 409 engineering_evidence_stale or engineering_policy_incompatible escapes a
completion endpoint; those belong to evidence/validation/admin operations.
Capture races during completion retry preflight, then use base 409 conflict;
other unavailable capture becomes the missing snapshot gate item. Evidence reads
return 200 with freshness findings, not a write's refusal. Extend golden fixtures.

Drain checks the complete returned missingItems: any engineering item makes that
attempt final/cleared with event+refusal audit retained, even if base items are
also missing. Base-only refusals retain base retry/quarantine rules. This is an
explicit FR-MCP-REPL-011/FAILSAFE/C8 amendment, not a global quarantine change.
The separate QB-AGENT-001 retained-mandatory-rejection obligation remains in
force; clearing a queue entry never clears its governed audit obligation.

### 14.2 Canonical digests and stored normal form

Use exact definitions below, ordinal JSON keys and compact UTF-8, no timestamps
unless explicitly part of a semantic configuration/profile. Arrays have the
specified order. All hash-sensitive engineering JSON columns bypass **only**
LineSanitizer's dash rewrite; they still receive secret-safe outbound projection
and ordinary access control. Raw source paths/test IDs/manifest bytes must survive
unchanged. Explicitly amend architecture 3.1/3.5.2's every-string normal-form rule
for ManifestJson, CanonicalJson, CommandJson, ScopeJson, ArtifactsJson, CoverageJson,
ReviewRefsJson, ScoresJson, ClaimsJson, completion-stamp JSON and signed payloads.
Do not globally turn off sanitization or hash sanitized outbound display text.

Structured ACs first decode JSON with duplicate-key rejection, replace U+2013 and
U+2014 with '-' in every string value, then validate and canonicalize. This covers
escaped and literal dashes equally. Plain new writes retain existing normalizer;
the migration itself updates Format only and never rewrites legacy Text. Forbid
U+2013/U+2014 in canonical policy asset/manifest text at build. Thus Stateless
policy text and descriptor hashes survive base storage normalization unchanged.
Tests cover dash-containing AC prose, escaped JSON, paths and test IDs; raw
artifact hash stability; no path collisions; package equality and Plain migration.

Digest inventory (bullets replace tables under workspace formatting rules):

- Criterion revision: SHA-256 of {id,format,text}; structured text is the canonical
  JSON **string** stored in Text, Plain text is stored normal form. Exclude
  isSatisfied, evidence, timestamps and audit notes. CoverageJson.revision is this
  digest, never a model-supplied ordinal.
- Effective Requirement hash: {kind,id,title,description,criteria:[{id,format,text}]},
  criteria in stored ordinal order; exclude status, satisfaction, evidence, notes,
  timestamps. This explicitly amends the base Requirement hash to include IDs
  and Format; any structured change or format-only conversion stales dependents.
  Delete/supersession/effective-layer removal is a separate freshness failure.
- In-scope ACs: union of AcLinks in the approved TodoType.RequiredGateIds, each
  AcLink={requirementKind,requirementId,acId,assertionKeys:[]}. Resolve every ID in
  the current effective layer; any missing/inactive ref blocks, never silently
  reduces denominator. This defines 'active/current-increment' for all stages.
- RequirementsDigest: {layerKey,realizesLinksDigest,requirements:[{kind,id,recordDigest,
  criteria:[{acId,ordinal,revision}]}]}, only those in-scope requirements, sorted
  ordinal by kind/id; criteria retain stored order. Additional applicable base
  AgentContext requirements keep its independent retrieval rules.
  realizesLinksDigest hashes catalog-scope (workspaceId,useCaseId,frId,relation)
  Realizes link tuples sorted by IDs, excluding timestamps. Mapping changes stale
  engineering coverage without changing the base Requirement hash's field set.
- Coverage digest: canonical CoverageJson sorted requirementId/acId/assertionKey/
  gateId/testId, unique tuples; exclude status/evidence comments. Every in-scope
  assertion key must map to an expected test of its approved gate.
- Slice SubjectDigest: {schemaVersion:1,workspaceId,todoId,evidenceId,
  evidenceRevision,stage,purpose,snapshotId,policySha256,requirementsDigest,
  inventoryDigest,reviewProfileDigest,coverageDigest,baselineDeltaDigest}.
  Server computes it; Evidence and Attestation gain ReviewProfileDigest.
- Diff representation: {oldSnapshotId:null|id,newSnapshotId,oldHeadSha:null|sha,
  newHeadSha,headToCurrent:[],priorToCurrent:[]}; each list consists of changed
  {repositoryId,path,before:null|entry,after:null|entry}, sorted repo/path.
  Entries use the exact manifest fields, with no textual diff formatting or
  timestamps. first list compares committed HEAD tree to captured bytes; second
  compares prior certified (or opening for Slice) snapshot to current. Hash of
  this JSON is ReviewedDiffSha256/baselineDeltaDigest. Raw before/after bundles
  accompany the review so hashes alone are not presented as reviewable source.
- Baseline subject: {workspaceId,oldSnapshotId:null|id,newSnapshotId,
  reviewedDiffSha256}; inventory subject is the proposed CanonicalJson digest.
- Evaluation subject: {protocolId,protocolDigest,policySha256,buildArtifactSha256,
  settingsDigest,anonymousRunManifestDigest,resultArtifactsDigest}; all fixed by
  harness before attestation. Exclude grader/operator timestamps and run labels.

InventoryDigest continues to hash the complete approved canonical inventory.
isSatisfied/evidence updates never stale a receipt; requirement text, format,
layer, coverage, inventory, profile or policy changes do. Test these independently.

### 14.3 Profiles, receipts and evaluation

Signed configuration contains ReviewerProfiles keyed ProfileId with SubjectKind,
Agent, Model, Effort, RequiredClaims (unique IDs), AccuracyFloor and CompletenessFloor
(both .98..1). Defaults: Slice = Codex/gpt-6-astra/max with base mandatory claims
plus EP1..EP6 and Scope/Completeness/Safety/Provenance; Inventory/Baseline = same identity with Scope/Completeness/Safety/
Provenance; Evaluation = ClaudeCode/claude-opus-5-5/max with EV1 FrozenProtocol,
EV2 CorrectnessCompleteness, EV3 Boundaries, EV4 InfrastructureAccounting.
Profiles are operator-signed, schema-validated, and cannot omit their mandatory
claims or lower floors. Exact base claim IDs are copied into the Phase 1 approved
profile artifact, not invented at runtime.

ReviewerOverrides entries: WorkspaceId, SubjectKind, ProfileId, ExpiresUtc,
Reason, OperatorId, signed DeploymentId; require future expiry and a profile with
the same mandatory claims/floors. Effective profile is the single unexpired exact
scope match; overlapping overrides reject reload. Otherwise default. Profile digest
includes complete effective profile and override scope/expiry. Completion compares
current digest; expiry requires new HV evidence. The human-authorized Claude
substitution in this chat concerns **external review of this plan only**, outside
the future runtime attestation store; no production override is asserted deployed.

Future attested reviewer receipt schemaVersion=1 requires ReviewedSubjectDigest,
ReviewerIdentity, Agent, Model, Effort, Scores {Accuracy,Completeness} as original
decimal text in [0,1], Claims [{Id,Verdict:PASS|FAIL|UNKNOWN}], OverallVerdict
AGREE|DISAGREE and ArtifactRefs. Reject percent-scale 98, duplicate/missing claims,
wrong subject/model/effort, malformed lexemes or inconsistent verdict. Operator
certificate identifies the attester, not the reviewer. Server captures subject
dependencies, parses raw receipt, verifies artifact hashes/CLI metadata where
supplied and applies effective profile. Remote identity/independence remains
operator-attested as stated above; an agent-authored Pass cannot create an
attestation. SubjectKind Evaluation uses its own profile and subject, removing
the impossible attempt to attest it as a Codex Slice.

Pin evaluation conditions to the **same 1.122.0 build/artifacts**, same server
settings/inventory/models/permissions: baseline GuidanceEnabled=false, integrated
GuidanceEnabled=true. This is an ablation of guidance, not comparison to an older
binary. Report as such. Each condition has 36 valid runs. Pool ratios:
sum(correct outcomes)/sum(required outcomes), sum(met ACs)/sum(required ACs)
over those runs; positive denominators predetermined by frozen fixtures. Do not
average per-task percentages or drop failed runs. Invalid paired reruns follow
section 13.5; any unresolved Invalid blocks evaluation/activation. Condition-blind
grader's claims/receipt scores and server-computed aggregate results are checked
under the Evaluation profile with section19.1's distinct meanings and bindings.

### 14.4 Producers and native validation handshake

Operator management POST `/admin/engineering-policy/{workspaceId}/producers`
upserts by ProducerInstanceId with Platform, BaseUrl (HTTPS, literal IP, no query/
fragment), ServerCertificateSha256, Enabled, ExpectedRevision and Reason. New
EngineeringValidationProducer table: composite workspace/producer PK, those
fields, Revision bigint default1, ApprovedBy/ApprovedUtc; URL max2048, pins64hex,
other lengths per section13. Unique active producer per platform/workspace.
Only EngineeringPolicyAdmin accepts it; 403 for workspace key, 400
engineering_producer_invalid, 409 engineering_producer_conflict. Pins/enablement
changes append administrative audit; producer registration is never agent-facing.

Authoritative broker dispatches mTLS POST `/internal/engineering-validation/jobs`
to that registered producer. Body {schemaVersion:1,jobId,ownerInstanceId,workspaceId,
evidenceId,revision,gateId,attempt,platform,snapshotId,bundleSha256,inventoryDigest,
inventoryCanonicalJson,deadlineUtc}. Producer authenticates owner client certificate
against its protected TrustedOwners map, not a caller URL. Idempotent same job/hash
returns202/200; changed payload with same ID gives409 validation_job_conflict.
Producer pulls `/internal/engineering-validation/bundles/{bundleSha256}` from the
preconfigured owner URL using its client certificate; owner authorizes that job/
workspace/producer and grants only the named immutable bundle. Secure extraction
rejects traversal/links/duplicate paths/oversize; recompute bundle+manifest hashes.
Copying from network source mounts is not a native validation fallback.

Producer enforces approved gate/platform/argv, secret-free sandbox and timeout,
then exposes mTLS GET `/internal/engineering-validation/jobs/{jobId}` and
`/internal/engineering-validation/jobs/{jobId}/artifacts/{sha256}` to that owner.
Owner rechecks producer pin, job/snapshot/inventory, raw counts/hashes before
Verified. Producer-provided Pass alone is insufficient. No arbitrary callback
URLs, credentials in job bodies, workspace keys or producer self-registration.

Connect5s, dispatch15s, poll5s, artifact-read30s, total job deadline inventory
TimeoutSeconds+60 (maximum3660). Before durable enqueue, no configured/enabled
producer returns503 backend_unavailable. After enqueue, dispatch/transport/timeout
failure becomes Unknown with diagnostic; eligible for the single infra retry.
Certificate/payload/hash mismatch is Unknown producer_untrusted, not an eligible
automatic retry; operator remediation required. Never switch producer/platform or
execute an arbitrary local fallback. Tests cover replay, pins/rotation, forged
receipt/bundle, byte fidelity across platforms, outage, budget and raw revalidation.

The bundle includes all approved SourceInputs, not just McpServer: inventory
SourceInputs=[repositoryId] references operator-configured local SourceRepositories
map (Id, absolute local Path). Host-adapter gates name all nine plugin IDs; missing
source returns Unknown. SourceSnapshot manifest additionally carries ordered
inputRepositories [{repositoryId,headSha,entries:[]}], included in SnapshotId and
effective-Code diff. Capture each by section6's algorithm, with the total existing
budget. Materialization retains required HEAD/tag/index Git objects in a hashed
object pack, synthesizes credential-free local Git config and overlays captured
bytes. Never copy .git credentials/hooks. Generated distribution test trees are
outputs outside immutable input bundles. Native producers receive identical inputs,
not whichever revision happens to be in their own source checkout.

### 14.5 Typed configuration, signatures and schema implementation

Extend EngineeringPolicyOptions with the following operator-protected keys.
Defaults permit guidance and read access, not execution or administration. Unknown
keys/types reject validation; all security/root/producer changes take effect only
after restart, semantic policy/process switches through the committed reconciliation
transaction. No uncommitted IOptionsMonitor value participates in a gate.

- RunnerEnabled bool=false; disabled RunValidation is409 engineering_runner_disabled.
  Enabling enforcement requires RunnerEnabled, approved inventory/baseline and
  provisioned trusted producers for its required platforms, plus20.2's current
  Evaluation attestation when EvaluationGateRequired is true.
- ArtifactRoot/WorkRoot: absolute paths outside all source roots, respectively
  `/var/lib/qbrainai/engineering/artifacts` and `/var/lib/qbrainai/engineering/work`
  on Linux, `C:/ProgramData/QBrainAi/engineering/artifacts` and
  `C:/ProgramData/QBrainAi/engineering/work` on
  Windows. Operator provisions ACLs; sandbox cannot read broker artifacts/config.
- SandboxAccount string=`qbrain-validation`; PowerShellExecutable defaults
  `C:/Program Files/PowerShell/7/pwsh.exe` on Windows and `/usr/local/bin/pwsh.exe`
  on Linux (operator-provisioned entry to native pwsh). Require a canonical absolute
  executable path, never a shell command string.
  Runner startup validates account isolation, executable hash and output mounts.
- DependencyCacheRoot defaults `/var/cache/qbrainai/engineering/dependencies` or
  `C:/ProgramData/QBrainAi/engineering/dependencies`: operator-provisioned offline
  SDK/tool/module/package mirror, outside source/user homes, read-only to sandbox.
  Inventory DependencyCacheSha256 pins its manifest and required package hashes;
  verify before each job. Use per-job writable overlays and credential-free local
  feed configuration; no network restore or silent dependency installation. Missing
  pinned dependencies make the job Unknown/dependency_unavailable. P0 provisions
  .NET/Git/PowerShell/Pester/build tools needed by the frozen commands.
- CaptureTimeoutSeconds int=30 (1..30), MaxSnapshotEntries=100000 (1..100000),
  MaxSourceBytes=2147483648 (1..that value), MaxSourceFileBytes=67108864,
  MaxArtifactsPerJob=256, MaxArtifactBytesPerJob=1073741824; all positive integers
  no larger than stated defaults. MaxConcurrentJobsPerWorkspace=1 (only value).
- ConnectTimeoutSeconds=5, DispatchTimeoutSeconds=15, PollIntervalSeconds=5,
  ArtifactReadTimeoutSeconds=30, TransferGraceSeconds=60; positive integers with
  stated values as maxima. Gate TimeoutSeconds remains inventory-owned 1..3600.
- AdminEnabled=false; AdminListenUrl=`https://127.0.0.1:7448`, loopback HTTPS only.
  InternalValidationEnabled=false; InternalListenUrl=`https://127.0.0.1:7449`,
  HTTPS literal IP only, operator explicitly binds a private interface for remote
  producers. Bind/port conflicts fail feature startup, never fall back to HTTP.
- CertificateStoreLocation=`CurrentUser`, CertificateStoreName=`My` (OS X509
  store of broker, not sandbox); AdminServerCertificateId,
  InternalServerCertificateId, ClientCertificateId nullable SHA-256 cert IDs.
  OperatorCertificateIds=[] and TrustedOwners=[] by default. Each TrustedOwners
  item contains InstanceId, BaseUrl, ServerCertificateSha256,
  ClientCertificateSha256, WorkspaceIds[]. InstanceId is a configured UUID.
  IDs/pins contain public certificate hashes, never secret key material.
  InstanceId UUID defaults null and is required when internal validation or runner
  is enabled. Byte budgets are Int64, counts/timeouts Int32, threshold floors
  decimal, flags bool; enum/string fields retain the stated closed sets/limits.
- SourceRepositories={}: map WorkspaceId to extra source root arrays, each unique
  Id, DirectoryName and canonical absolute local Git root Path, no overlap/case collision or
  network build root. Implicit primary Id `workspace` always resolves to registered
  workspace path; overrides are forbidden. Inventory SourceInputs is a nonempty
  ordered set including `workspace` plus configured extras. Producer pins
  live in the approved DB table; the enabled producer set is read-only to agents.
- ReviewerProfiles/ReviewerOverrides: typed schemas of14.3; defaults above.
  Overrides must be signed, scoped, nonoverlapping and expiring. Floors cannot
  be reduced. StructuredAcceptanceCriteriaEnabled and original four guidance/
  enforcement/read/byte settings remain exactly as section3 defines.

Absent certificate IDs are valid only for disabled listeners/runner. Enabled TLS
requires accessible private keys in broker OS store, correct EKU, current validity
and exact configured SHA-256 public-certificate pin; peer pins are checked both
ways, not replaced by DNS trust. No key paths/passwords in job bodies or audit.
Expired/invalid pins block the feature. Agent status exposes capability/diagnostic
codes, not credentials. Runtime storage/budget/timeout options use the startup
snapshot; restart applies changes. Delivery flags/limits refresh as section3;
structured-write enablement takes effect after signed config reconciliation.

Protected-setting set is **all** EngineeringPolicyOptions keys enumerated in
sections3/14.3/14.5 plus workspace DevelopmentProcess and section18's
EngineeringGuidanceSuppressed/EvaluationGraderContext. Producer/inventory/baseline/
attestation management operations use mTLS and the same operator audit. Signed
change manifest v1 = {schemaVersion:1,keyId,algorithm:"PS256",payload,signatureBase64}.
Payload = {deploymentId UUID,issuedUtc,reason,operatorId,workspaceIds:[],
validUntilUtc,expectedWorkspaceRevisions:{...},buildArtifactSha256:null|hash,
evaluationAbandonment:null|{protocolId,protocolSha256},
effectiveSettings:{...complete protected settings...}}. Validity is at most24h;
reject expired new manifests, future issue time beyond5min or stale expected
revisions before applying. Canonicalize keys ordinally,
compact UTF-8, preserve array order, no BOM; sign payload bytes with RSA-PSS/SHA256,
minimum3072-bit operator key in OS store. Verify signature against pinned operator
public certificate, current validity, unique deploymentId/payload hash, expected
workspace set and canonical config equality. Reject altered/reused deploymentId
with a different hash. Retrying identical manifest is idempotent.
`tools/powershell/Sign-EngineeringPolicyChange.ps1` uses OS certificate APIs, no
private material on command line/stdout. Store SignedManifestJson unchanged beside
PolicyAudit; audit display strings may use base normal form. Initial built-in
defaults are recorded as SystemBootstrap with null DeploymentId; nondefault
protected settings require a signed manifest even on first startup.

ConfigurationReconciliationService commits WorkspaceState.EffectiveSettingsJson
and audit/generation before publishing an effective options snapshot. Gates read
those committed DB fields; hot reload never observes partially applied settings.
No-op/restart compares stored semantic values, retaining generation. All canonical
signed/config/profile JSON fields join the dash-rewrite exemption. Schema tests
exercise signatures, replay, rotation, expiry, invalid limits and no-op restart.

No JSON Schema package is currently referenced in the inspected projects. Use
existing System.Text.Json plus `AcceptanceCriterionSchemaValidator`, a local
validator for the exact subset used by the two embedded schemas: type, required,
properties, additionalProperties, const, enum, length/item limits, items, pattern,
allOf, if/then/else and not. Reject unsupported schema keywords at startup rather
than pretending to support all draft2020-12. $schema/$id are metadata only; no
remote resolution. Compile the two checked-in schema files as EmbeddedResource;
build verifies their SHA-256 and service/client schema version1 parity. Validate
duplicates/depth/bytes/normal form before schema, then Gerkin phase/unique-ID and
EARS semantic rules. Conformance fixtures cover every used keyword, each EARS
branch, negative/round-trip cases and schema drift. No new dependency or separate
AC payload column. Status adds acceptanceCriteriaFormatVersion=1 and schema hashes;
missing capability on an older server is0, and new clients refuse non-Plain writes
to it with server_capability_unsupported.

### 14.6 Traceability, rollout census and migration defaults

Add TraceabilityBaselineV1 parser and inventory AllowedBaselineFindings (sorted
{code,requirementId,relatedIds:[]} tuples frozen from P0). BuildTooling extends
ValidateTraceability to emit a JSON artifact {schemaVersion:1,complete:true,
findings:[{code,requirementId,relatedIds:[],message}],catalogIds:[]} before exit.
Parser verifies report/job hash and complete flag; malformed/missing is Unknown.
Strict exit0 or exit1 passes only if no finding names a current catalog ID and
every remaining finding is in the approved P0 baseline. Any other exit is failure.
Disappearing baseline findings are fine; new findings are not. Ordinary mapping
validation must exit0. Add GateId G5UseCaseCoverage and ParserId UseCaseCoverageV1:
the existing store-backed UseCases.GetCoverageAsync report must contain none of
this plan's catalog FRs in functionalRequirementsWithoutRealizesUseCase. Freeze
catalog IDs in inventory, not the global SQLite-only strict-use-case switch.
This implements the base G5 exception without labeling failed tests passing;
all test gates still require zero failures/skips. Red/Progress explicitly applies
to approved TestDesign TODOs as well as Implementation TODOs.

Inventory gates gain ExecutionKind=Process|BrokerQuery. Only G5UseCaseCoverage
uses BrokerQuery, QueryId=UseCasesCoverage and argv=[]; all other gates use Process.
Broker invokes the existing typed UseCases.GetCoverageAsync outside transactions,
emits the complete raw JSON report and link digest, and verifies it with
UseCaseCoverageV1. It uses the base G5 store-query exception, not sandbox credentials
or an arbitrary query supplied by an agent. This store check is owner-platform
only; native process gates still run on each required OS. Concurrent link changes
invalidate RequirementsDigest at preflight. Test missing report, missing Realizes
link, catalog filtering, stale link digest and unavailable authoritative store.

BuildTooling owns `build/Build.UpdateService.cs` rollback guard. Add --rollback
bool, used with --skip-build/--skip-version-bump/--publish-source. Before replacing
format-aware binaries: stop the target service and all registered instances that
write its stores, verify processes/listeners stopped, and run current format-aware
binary `--ac-format-census --read-only --report <path>` without starting the host.
RequirementsAndAcceptanceCriteria owns that offline CLI/codec; it scans every
configured relational provider and current TODO execution-state/rollback-state
backend, treating missing legacy Format as Plain. Result schema1: complete,
scannedStores[], plainCount, nonPlainCount, unknownCount, sourceManifestHashes.
Never rewrite files/data. Non-Plain count>0 refuses with
rollback_ac_format_incompatible; malformed/unavailable stores/unknown formats or
incomplete census refuse rollback_ac_format_census_failed. If any writer cannot
be quiesced, rollback_quiesce_failed. Shared-store multi-host deployments require
operator quiescence of every owning instance; no automatic remote source build.
On refusal no binary swap occurs; restart original format-aware service. Guard
is in the installed update workflow, not an endpoint the old binary must enforce.
Only after a complete zero-nonPlain census may old binary be installed. Down
migration uses the same census/backup rule. Keep backups inert; restoring any
structured state requires a format-aware binary and the same compatibility check.

Migration defaults: historical OpeningSnapshotId/BoundUtc/EffectiveRequestClass/
EngineeringCompletionStampJson are null, OpeningSnapshotState=Unavailable.
New old-binary turn inserts receive those DB defaults. WorkspaceState's
LastAppliedDeploymentId is nullable default null; PolicyAudit.DeploymentId is
nullable for non-deployment administration. No timestamp fabricated for history.
New tables do not require old binaries to insert rows. Test prior-binary inserts
after Up, old-binary rollback with all Plain, refusal with one structured row or
TODO state, malformed/unreachable state, census read-only behavior and Up/Down/Up
on all three providers. Infrastructure orchestration tests remain Build.Tests;
FR behavior evidence belongs to runtime/migration tests under the base test rule.

### 14.7 Pointer history, delivery and ownership amendments

Add EngineeringBaselineHistory: (WorkspaceId, HistoryId UUID), SnapshotId,
PreviousSnapshotId nullable, ReviewedDiffSha256, HvAttestationId, ValidFromUtc,
ValidToUtc nullable, OperatorId, AuditId; append-only interval history (close prior
interval and insert next in one transaction), with only one open interval.
Only RecordBaseline operation advances pointer, deterministically after full
server-computed prior-to-current and HEAD-to-current delta attestation and CAS.
Green/SliceExit does not adopt unrelated edits or move it. No matching full-delta
attestation means refusal, never partial adoption. Bind opening reference to the
history interval in force at immutable ingress binding.boundAtUtc, including
late failsafe mint, not the pointer current at drain. Missing/untrusted ingress
time/history yields Unavailable and conservative Code; never guess. Live mint
uses the base trusted ingress binding, not an agent-selected clock. Preserve
history/snapshots across restart, end/resume and recovery. Tests include another
session editing before/after baseline approval, delayed mint across pointer move,
missing history and false future ingress timestamps.

Contended turn-open.lock does not wait: deliveryOutcome=Unavailable,
diagnostic policy_delivery_contended, no Delivered tombstone/instruction claim;
preserve the base turn-open diagnostic and stateful exit behavior. Reserved/
Unavailable rows retry on next hook invocation for the same delivered request or
the adapter's next pre-model-dispatch, within existing read/deadline bounds.
Successful retry commits the one emission. For direct request adapters (Cline v2,
OpenCode), **every outgoing model request** includes canonical policy unless the
same hash/sections already occur in that request's actual context; ledger records
first emission only, never removes later context. Hook hosts rely on their native
context retention within a prompt and next-prompt restoration; P0.2 records any
in-loop restoration limitation. Stateless mandatory item stays on all tool-loop
requests while earlier-request history is excluded. Test contention, retry,
crash-reserved, duplicate emission, tool-loop requests and restored context.

Explicit architecture8.3 ownership amendment: create
RequirementsAndAcceptanceCriteria context owning existing requirement/TODO service
changes, schema/codec integration, census and server TODO guard. Merge it after
SharedComponents/SessionTurnLogging and before TurnReviews, consuming Phase2 seams;
feature flags remain off until all implementations land. TurnReviews retains
CompletionGate and new engineering services. SessionTurnLogging owns pipeline/
mint/stamps and calls that policy. SharedComponents Phase2 owns public DTO/client/
controller/MCP stubs/shared signatures; ReplAndAgents Phase4 owns adapter behavior
only. Cross-owner edits require an approved Phase2 amendment, no parallel edits of
the same file. Add these ownership rules and new context position to base8.3.
C8/C9/normal-form/public-contract changes cannot cross into implementation until
QB-AGENT-001 acknowledges the CONTRACT entry or the operator explicitly resolves
it. This is a dependency, not approval inferred from committing this document.

Round3: DISAGREE, confidence38, accuracy.88/completeness.79, 2PASS/10FAIL;
14 findings. Exact output is retained under claude-round-3. Sections14.1–14.7 and
the corrected earlier clauses are the author resolutions awaiting re-review.

## 15. Round 4 contract completion

These nine resolutions complete EP-R4-01 through EP-R4-09. They amend the base
HV protocol and contracts explicitly; no implementation or phase approval is
inferred from this document's external readiness review.

### 15.1 Independent review workspace and baseline cadence

For engineering-gated source workspaces, HV/grader sessions run in a **separate
registered review workspace**, DevelopmentProcess=None, over a read-only
materialization of the exact source bundle. Its writable workspace envelope holds
only registration/marker/session metadata; reviewed files are a read-only `source/`
child. Receipts go only to the broker artifact store outside source. Operator
registers this workspace/configuration and launches the reviewer there; the review
brief binds original WorkspaceId, snapshot/subject digests and allowed artifact
scope. Reviewer records/completes its own Chore turn in that review workspace under
base rules; it cannot mutate source, create validations or attest its own verdict.
Global/operator profile and scoped approved review context still apply to ordinary
HV workspaces. Evaluation graders instead use section18's neutral context and
signed guidance suppression. No source workspace marker/API key is added to the provider packet.
Review tool allowlist includes own-workspace session-log MCP operations solely
for its audit lifecycle; read-only restrictions apply to reviewed source. Local
registration/hooks bootstrap with redacted trust diagnostics, never exposing
marker keys to the provider. No source-workspace/admin/requirements/TODO mutator
is available to the reviewer. Explicitly amend the
base-plan HV protocol's launch directory, docs/receipts/hv location and reviewer
turn rule for these workspaces. Do not execute the reviewer inside the dirty
engineering-gated worktree. Ordinary source-agent turns remain gated as before.

After operator approval of a SliceExit, the operator invokes RecordBaseline to
adopt that slice snapshot before starting the next slice. It may reuse that Slice
AGREE attestation **only** when oldSnapshotId is the current certified pointer,
newSnapshotId equals the slice snapshot, and its baselineDeltaDigest equals the
server's ReviewedDiffSha256. Slice profile includes Scope, Completeness, Safety and Provenance
in addition to its existing mandatory claims. CAS/full-delta/history checks still
run; any extra shared-checkout change or different pointer requires a new Baseline
HV. Nothing moves the pointer automatically. Same rule applies to approved
TestDesign exits. Tests: reviewer Chore turn completes without source evidence;
Docs after adoption stays Docs; unrelated edits cannot piggyback on Slice AGREE;
attestation/pointer change refuses adoption. Before new gates exist, external
packet-only plan readiness remains a review record, not a formal phase exit.

### 15.2 Attestation lookup, ownership and failure memory

ReviewRefsJson contains prerequisite attestations only, never the current Slice
attestation. Evidence can therefore be created before its HV. At completion,
server recomputes SubjectDigest from the referenced immutable EvidenceId/Revision
and current profile/dependencies, then looks up attestations by workspace/subject/
profile. Newest non-superseded attestation must be AGREE and **no** non-superseded
DISAGREE may exist. Superseding is an operator-only append referencing prior IDs
for that same subject/profile, with Reason and audit; validate cycles/foreign IDs.
Newest order is server AttestedUtc then AttestationId, never caller timestamp.
An old AGREE cannot be selected around a later unresolved DISAGREE.

Turn completion requires evidence whose complete turn FK/root tuple is exactly
the completing turn. TODO done requires evidence for that TodoId from a completed
coordinating turn and repeats preflight current-snapshot equality plus DB-only
policy/generation/profile/requirement/inventory/coverage/attestation checks. It does
not trust the previous turn's completion stamp alone. Apply the same bounded CAS/
retry and atomic backend-save rules. Tests cover circular/prerequisite refs,
multiple/superseded verdicts, foreign-turn evidence and edits between turn close
and TODO done.

Workspace-wide failure memory is keyed (WorkspaceId, SnapshotId, GateId, Platform,
InventoryDigest). A Fail in **any** evidence/turn blocks a new Pass for that tuple;
RunValidation returns409 engineering_validation_failed, completion reports
engineeringValidation. New turn/revision/Progress versus SliceExit cannot clear
it. Changed snapshot or independently HV-approved changed inventory is required
after Fail; the failure-memory tuple is authoritative. Receipt gains indexed
SnapshotId/InventoryDigest, SupersededInfrastructureUnknown bool defaultfalse,
SupersededByReceiptId nullable UUID/composite workspace FK; section14's one-infra-
retry exception is unchanged. All turn-child evidence/receipt rows carry the base
immutable Agent/SessionId/RequestId roots plus parent evidence ID/revision; these
common root fields are inherited in every wire/job/storage projection, not omitted
by the shorter field inventories above. Workspace-scoped non-turn attestations
have nullable turn roots; Slice attestations require them. Test cross-turn failure
memory and uniqueness under concurrent retries.

### 15.3 Materialization and one normative inventory shape

SourceRepositories.DirectoryName is required, one path component, exact repository
name. Primary implicit `workspace` has DirectoryName=`McpServer`. Host directories
are exactly mcpserver-claude-code-plugin, mcpserver-claude-cowork-plugin,
mcpserver-cline-plugin, mcpserver-cline-v2-plugin, mcpserver-codex-plugin,
mcpserver-copilot-plugin, mcpserver-grok-plugin, mcpserver-grok-bot-plugin and
mcpserver-opencode-plugin. Reject duplicate/case-colliding/traversing names.

Job layout: `<WorkRoot>/<jobId>/inputs/<DirectoryName>/` for each source, and
separate outputs/cache under that job. Inventory WorkingDirectory is relative to
the primary `inputs/McpServer` repository, canonical default `.`. G6 and
PowerShellOnly run from that root with literal `--agent-plugin-parent ..`; the
isolated parent is thus `inputs`, not a substituted arbitrary path. Required
working outputs are private overlays as already specified. Only the existing
TestRunId/GateStartedAtUtc placeholders are substituted. Discovery/layout/hash-
preservation tests run on both OSes, proving all nine repositories are selected.

Normative inventory v1 field placement is below (illustrative values, not active
test claims). JSON names are lowerCamel; earlier PascalCase denotes DTO members.
All shown keys required; queryId is null for Process, UseCasesCoverage for
BrokerQuery. No extra keys. Arrays follow the ordering rules after the example.

```json
{
  "schemaVersion": 1,
  "sourceInputs": ["workspace"],
  "dependencyCacheSha256": "<64-lowercase-hex>",
  "catalogIds": ["<frozen-requirement-id>"],
  "allowedBaselineFindings": [
    {"code": "<baseline-code>", "requirementId": "<id>", "relatedIds": []}
  ],
  "gates": [
    {
      "gateId": "G2",
      "executionKind": "Process",
      "queryId": null,
      "argv": ["./build.ps1", "Test", "--test-run-id", "<server-job-id>", "--gate-started-at-utc", "<server-start-utc>"],
      "workingDirectory": ".",
      "platforms": ["LinuxX64"],
      "stages": ["Red", "MocksGreen", "Green"],
      "expectedProjects": ["<project-path>"],
      "expectedTests": ["<test-id>"],
      "expectedCount": 1,
      "acLinks": [
        {"requirementKind": "FR", "requirementId": "<id>", "acId": "<ac-id>", "assertionKeys": ["<assertion-key>"]}
      ],
      "parserId": "DotnetTrxV1",
      "timeoutSeconds": 3600,
      "expectedRedTests": [
        {"testId": "<test-id>", "exceptionType": "<type>", "assertionMessageSha256": "<64-lowercase-hex>", "stackFramePrefix": "<prefix>"}
      ]
    }
  ],
  "todoTypes": [
    {"todoId": "<todo-id>", "kind": "Implementation", "requiredGateIds": ["G2"], "requiredClaims": ["EP1"]}
  ]
}
```

Canonical object keys sort ordinally for hashing. Sort sourceInputs/catalogIds/
projects/test IDs/claim IDs/assertion keys/relatedIds ordinally; gates by gateId,
todoTypes by todoId, baseline findings by code/requirementId/relatedIds, AcLinks by
kind/requirementId/acId, ExpectedRedTests by testId. Platform/Stage enum arrays use
their declared order. argv preserves execution order. Real inventory substitutes
verified facts, covers all required cumulative gates/claims and all nine source
IDs when host gates apply; placeholders/unknown IDs are refused. No approval of
the example is implied. Reviewed operator approval cannot weaken fixed gate/floor
requirements. Missing/unapproved TodoType uses engineeringEvidence with remedy
approve_todo_type; all other missing evidence uses record_evidence.

### 15.4 Replay, signed-manifest channel and status selection

SessionLogMintTurnRequest gains optional `ingressReplay` object
{hostSessionKey,keySource,provisionalRequestId,boundAtUtc}, supplied only by the
drain from the captured C4 binding. No object means live mint uses server receive
UTC. Any top-level boundAtUtc is400 validation_error; the object identifies the
replay path and must match original nonce/session/provisional binding.
Replay accepts captured times in [session openedUtc, server now+5seconds], bound
to original nonce/session/request recovery envelope. Outside that window, missing
or invalid time makes OpeningSnapshotState=Unavailable; do not select a newer
pointer or fabricate an arrival. Extend arch4.2, mint client/REPL shim and golden
contract. Tests: live clock, valid delayed replay across pointer advance, rejected
future/before-session timestamps and no history.

Signed configuration channel: `engineering-policy-change.json` beside the
instance's canonical protected configuration file, same operator-only directory
ACL. Signing script writes an atomic temporary file/rename, then operator atomically
updates canonical configuration and requests reload/restart. Reconciliation reads
both at startup/reload; mismatch/partial deployment retains previous effective
settings and reports engineering_config_manifest_mismatch. Missing manifest for
nondefault change, invalid signature, expired new manifest or stale revision are
final config rejection codes (engineering_config_manifest_required/invalid/expired/
conflict). On initial startup with nondefault protected settings, reject feature
activation until valid; never silently weaken an enabled enforcement request.
Keep accepted manifest beside configuration and archive its unchanged bytes/hash
with audit; restart/no-op/idempotent deployment ID reuse uses stored approval,
not a new signature. New change overwrites channel atomically, preserving audited
history; cleanup never removes the last accepted manifest. No secret key in file.

Plugin status uses the invoking host's base binding (explicit host session key,
otherwise sole live session inference). If no invocation can select a single
host session, return `hostSessions` array keyed/sorted by hostSessionKey with each
row's host/lastRequestId/delivery fields, and no guessed top-level session fields.
Server policy descriptor stays workspace-scoped. Test two live sessions and
ended/resumed sessions; status never changes binding or delivery state.

### 15.5 Evaluation isolation, hash evolution and owners

Run baseline/integrated on two identically provisioned isolated instances and
source/fixture copies, differing only in their signed GuidanceEnabled values.
Require fresh host sessions and successful descriptor reads before every run.
A run is infrastructure-invalid if **any prompt** ledger outcome is not Disabled
for baseline or Delivered/Duplicate for integrated, including timeout fallback
that would otherwise inject packaged guidance into baseline. Use the single
paired rerun; second invalid blocks activation. Keep all contaminated evidence.

Choose the pre-persistence amendment option for Requirement hashes: fold kind/
criterion IDs/Format into the authoritative base hash contract **before Phase C
implements any hash writer or persists a hash**. Fold the additive Format column/
Plain default/backfill and DTO extension into base StorageSchema/SharedComponents
before those writers compile/deploy. Structured writes remain disabled. Policy
plugins still ship at1.122.0 after the base rollout; this foundational amendment
must not wait until that release. No historical Trusted response is silently
rehash-staled and no per-record hash-version columns are needed in this path.
Phase1/StorageSchema verifies absence of existing PhaseC package/response/evidence/
relationship hashes in every target store; migration preflight repeats it. If
any legacy hash exists, block this rollout without modifying data and require a
separately reviewed compatibility migration before proceeding; do not assume empty
from source inspection alone. Tests: seeded legacy state blocks without mutation;
empty stores write the amended hash; format-only changes stale dependencies;
satisfaction/evidence metadata does not. This is an explicit base PhaseC ordering
dependency, not a runtime choice left to implementers.

Ownership additions: TurnReviews owns admin/internal listener behavior, producer
transport and ConfigurationReconciliationService in its engineering module.
DESIGN-MERGEDPLAN-001 owns composition-root/listener/reconciliation surface changes
and coordinates shared files; SharedComponents Phase2 owns public signatures and
embedded docs/schemas/resources. PluginCorePowerShell owns signing/baseline helper
scripts under tools/powershell, BuildTooling calls those contracts and owns update/
census orchestration. RequirementsAndAcceptanceCriteria owns runtime codec/census
behavior. No context edits another's files without the Phase2 amendment/ACK rule.

Round4: DISAGREE, confidence44, accuracy.91/completeness.82, 3PASS/9FAIL;
nine findings. Exact output saved under claude-round-4. Current resolutions await
the next independent review under the unchanged12-claim/.98/confidence98 contract.

## 16. Round 5 contract completion

### 16.1 Review materialization and receipt emission

Add operator-only POST
`/admin/engineering-policy/{workspaceId}/review-materializations`, mTLS policy
EngineeringPolicyAdmin, client `CreateReviewMaterializationAsync`. Input:
{subjectKind,subjectRef,reviewWorkspaceId}; references are Slice evidenceId/revision,
Inventory proposed canonical inventory payload, Baseline capture request (no
preexisting snapshot required), or Evaluation harness-manifest artifact ID created
by17.1. Resolve the
subject using14.2's formulas, not a caller's claimed digest. No arbitrary path/URL.
ReviewWorkspaceId must be registered with process None on this owner, outside
all source roots, with empty materialization destinations (or identical ready
materialization for an idempotent repeat). A different existing subject refuses.
An Evaluation grader workspace must also satisfy section18; suppression is invalid
for Slice/Inventory/Baseline materializations.

TurnReviews behavior reuses14.4's bundle verifier/materializer to write exact
read-only `source/` and, where a before/after comparison is required, `before/`
under that review workspace. Preserve15.3's repository directory layout. Recompute
bundle/manifest/file hashes before Ready, enforce OS read-only access to reviewed
bytes, and write `review-materialization.json` containing {schemaVersion:1,
materializationId,subjectKind,subjectDigest,snapshotIds:[],bundleSha256s:[],
reviewedDiffSha256:null|hash,reviewProfileDigest}. Known-live-credential scan failure (17.2) refuses
materialization, never silently redacts bytes and retains their old digest.
No automatic provider upload: operator launches the reviewer with the approved
materialization/brief. This explicitly replaces the base rule that the implementing
agent launches the reviewer in its worktree. The reviewer emits its receipt only
in the captured final response/VERDICT block; section20.1's explicit capture
operation stores it as an artifact. The reviewer does not write receipt files in source or artifact storage.
These launch/emission rules join15.1's base HV protocol amendment.

EngineeringReviewMaterialization: composite WorkspaceId/MaterializationId UUID
PK; SubjectKind, SubjectDigest, ReviewWorkspaceId, ManifestSha256, ManifestJson,
State Ready|Invalid, CreatedUtc, CreatedByOperatorId. Snapshot/bundle refs live in
the immutable manifest; lengths/JSON limits/dump/local-only rules inherit13.
TurnReviews owns handler/record behavior; StorageSchema owns entity/migration;
DESIGN owns management surface with SharedComponents Phase2 DTOs.
400 validation_error for shape/ref kind;403 admin_authorization_required;
404 not_found for workspace/subject;409 review_materialization_conflict/stale/
hash_mismatch/sensitive for conflicting destination, changed subject, failed
integrity or secret scan. No partial Ready row. Standard503 for unavailable store.

Brief and attested receipt must carry MaterializationId. ArtifactRefsJson shape
is {materializationId,manifestSha256,artifacts:[standard artifact refs]}; the raw
receipt's ArtifactRefs uses the same shape. AttestReview resolves the stored
materialization, verifies Ready/current subject/profile and manifest hash against
the receipt and dependencies, and refuses mismatch409 engineering_review_stale.
Current plan-readiness packets before these runtime gates exist are explicitly
outside this future attestation protocol, not claimed as deployed materializations.
Tests: byte equality, before/after manifests, read-only enforcement, stale/missing/
foreign materialization, mismatch refusal, idempotent destination and final-response
receipt capture. Known-live source/review credentials are refused; pattern-only
findings remain visible for operator inspection under17.2, without byte redaction.

### 16.2 Format-support boundary and migration ownership

This feature's StorageSchema creates engineering tables only. Base StorageSchema/
SharedComponents owns the single folded Format column/Plain backfill/DTO extension
before PhaseC hashing (15.5); do not create a second AC migration in this plan.
Base binaries with only those folded pieces are **not format-aware**: capability
acceptanceCriteriaFormatVersion is0 or absent and non-Plain API writes are refused
until the complete codec/validation/rendering implementation is installed.
Every use of 'format support' or 'format-aware' in11/14.6 means server capability
acceptanceCriteriaFormatVersion>=1, not presence of a column, enum or DTO.
Build emits server-capabilities.json from the same compiled capability constant
the status query reports; UpdateService reads validated target package metadata,
treating absent/invalid capability as0. Its current-capability>=1 census refuses
rollback to **any** target below1 while relational or TODO state has non-Plain
data. Test the base-with-column/DTO-but-capability0 target, a genuinely capable
target, no duplicate migration, Plain backfill once, and capability/status/package
parity. Existing census/quiesce/refusal/no-swap rules remain intact.

### 16.3 Replay object and attestation consistency

The optional ingressReplay object (15.4), not an implicit live/drain distinction,
identifies replay. It carries exactly hostSessionKey,keySource,provisionalRequestId,
boundAtUtc from C4. Validate all against nonce/session recovery binding; absent
object is live server time. Top-level boundAtUtc is always400 validation_error,
including through tools/REPL. Update mint DTO, arch4.2, shim and golden fixtures;
test live, matching replay, forged/foreign envelope and trust-window failures.

SupersedesAttestationIdsJson is nullable TEXT JSON. Null means no supersession;
non-null is an array of1..N unique prior UUIDs under the common JSON size bound,
same workspace/subject/profile, with cycle/foreign-ID checks in the append
transaction. No singular SupersedesAttestationId exists. Multiple DISAGREEs can
therefore be resolved by one genuine newer attestation without changing history.
Baseline reuse validates the Slice AGREE under the **current Slice profile**, whose
mandatory set includes Scope, Completeness, Safety and Provenance plus base/EP
claims, then checks15.1's exact old pointer/new snapshot/diff equality. It does not
pretend a Slice SubjectDigest is the Baseline SubjectDigest. Failure memory's
tuple is authoritative: changed snapshot **or** independently HV-approved changed
inventory plus documented remediation permits a new evidence revision; unchanged
tuple across turns remains blocked. Test multiple supersession, unresolved verdict,
Slice reuse claim/profile mismatch, approved inventory correction and unapproved
or semantically unchanged inventory attempts. The one-infrastructure-rerun rule
does not change.

Round5: DISAGREE, confidence74, accuracy.95/completeness.93; four findings.
Exact continued-review output is retained under claude-round-5. This revision
addresses R5-01..04 for the next independent review, without changing thresholds.

## 17. Round 6 contract completion

### 17.1 Producing baseline, inventory and evaluation review inputs

Baseline and Inventory review-materializations **capture** the registered checkout
and inputRepositories using6/14.4: double capture, canonical hashes, budgets and
persisted EngineeringSourceSnapshot row. They do not require an agent evidence
revision or prior baseline attestation. For Baseline, oldSnapshotId is the current
certified pointer or null; compute14.2's full diff and ReviewedDiffSha256 and put
them in review-materialization.json. For Inventory, capture the proposed
inventory's SourceInputs as context, explicitly outside its canonical inventory
digest. This supports initial Inventory review before a certified baseline exists.
RecordBaseline recaptures on adoption: SnapshotId and complete diff must reproduce
the reviewed values, or409 engineering_baseline_conflict; do not silently adopt a
later checkout. Owners: TurnReviews behavior and DESIGN surface, as16.1.

Add operator-only POST
`/admin/engineering-policy/{workspaceId}/evaluation-artifacts`, EngineeringPolicyAdmin
mTLS, client `StoreEvaluationArtifactsAsync`.
Request DTO: {operation:FreezeProtocol|RecordResults|LockBlindGrades|AbandonProtocol,protocolId,
protocol:null|protocolJson,protocolSha256:null|hash,
anonymousRunManifest:null|manifestJson,results:[],unblindingMap:null|mapJson,
materializationId:null|uuid,capturedResponseArtifactId:null|sha256,reason:null|string,
signedChangeManifest:null|manifestJson,
freezeReceipts:[]}. Result entries are
{runId,resultJson,observableArtifactIds:[],capture:{cliResultUtf8Base64,
sessionExportUtf8Base64,deliveryLedgerUtf8Base64,launch,evaluationInstanceId}}.
Capture validation and pre-run target-store freeze use21.3. Stored results add
server-populated captureArtifactIds for audit-only artifacts under22; these are
not grading observables. FreezeProtocol requires protocol only,
returns protocolArtifactId; its protocol includes the preregistered map SHA-256
under19.1. RecordResults requires its stored hash, complete anonymous manifest/
results and matching unblindingMap, with no replacement protocol. LockBlindGrades
uses the stored captured response and materialization under19.1/21.1.
AbandonProtocol uses21.1's signed administrative audit. Unused mode fields
must be null/empty. The embedded protocol/run/
result documents follow the frozen fields/rubric of9/10; opaque existing artifact
IDs reference raw observations, never caller paths/URLs. It content-addresses the frozen
protocol, anonymous run manifest and result payloads as EvalProtocol,
EvalRunManifest and EvalResult. Protocol-only requests freeze protocolId/hash before
runs; later requests must reference that immutable protocol hash and cannot replace
its bytes under the same ID. Result/manifest requests validate the frozen task/run/
condition/rerun/model/build/settings schema of9/10/14/15, store result artifacts,
then a harness manifest referencing protocol/results and anonymous run records.
Return harnessManifestArtifactId (the EvalRunManifest SHA-256) when that complete
packet exists. Standard size/count/hash/403/400/404/409 and unavailable-store503
rules apply; incomplete packets cannot receive Evaluation attestation.

Evaluation materialization source/ contains section18's **blinded grading projection**
of that anonymized artifact packet, not an invented repository snapshot. The full
frozen protocol, anonymous run manifest, results and referenced observables remain
immutable in the broker store; the full protocol and unblinding map are withheld
from the grader permanently; only the server uses the map after the grade lock
under19.1. All referenced bytes/hashes are checked; SubjectDigest
uses14.2's evaluation formula. Evaluation attestation is recorded in the source
workspace **being activated**, while the grader's Chore turn belongs to its separate
review workspace. TurnReviews owns storage/handler behavior; DESIGN the management
surface, SharedComponents the DTOs. No agent-facing upload/approval tool is added.
Tests: clean bootstrap with no evidence/baseline, Inventory context capture,
Baseline mutation-before-adoption refusal, frozen protocol immutability, complete
evaluation packet creation/materialization, missing results and correct attestation
workspace. These operations supply the references used by16.1.

### 17.2 Exact live-credential refusal and fixture-safe findings

The refusing materialization scan uses **exact matches** against the owner's known
live credential values: active source and review workspace marker/API keys, plus
other already-configured live secret values from the owner's credential providers.
Obtain this set in broker memory from existing trusted auth/secret services;
never discover/read unrelated credential files, place secrets in manifests or
audit their values/hashes. Empty values are ignored. A match refuses409
review_materialization_sensitive, reporting path and rule ID only, never the
matched value. The broker snapshots/rechecks the active credential-catalog revision
before Ready so a rotation cannot silently invalidate the scan.

Reuse base SecretRedactor's **pattern** rules only for non-blocking findings in
review-materialization.json: secretScanFindings=[{path,ruleId}]. Do not redact or
change reviewed bytes, and do not refuse merely because a fake redaction fixture
resembles a credential. This mirrors the base receipt check against current marker
keys. Ready certifies integrity and this known-live check, not exhaustive detection
of every possible unknown secret; operator inspects the approved packet/findings
before launching any provider review. No automatic upload is introduced. Tests:
fixture-bearing repo succeeds with findings and identical bytes; a known live key
is refused without disclosure; key rotation reruns/refuses; no credential-file
discovery or secret value is recorded in output/audit.

Round6: DISAGREE, confidence80, accuracy.97/completeness.95; two findings.
Exact output retained under claude-round-6. These R6-01/R6-02 resolutions await
independent confirmation under unchanged claims and thresholds.

## 18. Round 7 evaluation-grader isolation

This resolves R7-01 and explicitly amends the base required-profile/memory/context
injection contract **only for Evaluation grader review workspaces**. It does not
change source-agent guidance or ordinary Slice/Inventory/Baseline HV context.

Add operator-protected workspace keys beside DevelopmentProcess:

- `EngineeringGuidanceSuppressed`: bool=false. True requires process None and
  `EvaluationGraderContext.subjectKind=Evaluation`. No agent setter or per-turn
  switch. Apply through the signed configuration/reconciliation of14.5/15.4.
- `EvaluationGraderContext`: nullable object, default null. When suppression is
  true, require {subjectKind:"Evaluation",operatorProfileText,allowedMemoryIds:[],
  contextSha256}. Profile text is UTF-8, at most8192 bytes. Memory IDs are unique;
  an empty list is valid. Startup/reload validate configuration shape only.
  Materialization, launch and dispatch validate existence in effective scope and
  the context hash; a missing/deleted memory yields409 evaluation_grader_context_invalid,
  never a server startup failure. contextSha256 hashes
  canonical JSON of the other three fields plus the exact selected memory texts
  in Global-ID then Workspace-ID order. Operator signs the full configuration and
  attests that the profile/memories describe neither the evaluated policy nor its
  conditions, fixtures or treatment mapping. A changed selected memory invalidates
  that hash and requires renewed operator approval before another grader prompt.

Registration/configuration may declare this intended subject before materialization
exists. CreateReviewMaterialization for Evaluation requires these settings and
binds their hash into review-materialization.json as graderContextSha256. Suppressed
workspaces refuse other subject kinds. Launch requires a Ready Evaluation
materialization whose context hash still matches; no grader session starts against
an unmaterialized or mismatched subject. Invalid configuration shape fails startup/reload using
section3's last-valid configuration rule; materialization/launch mismatches are409
evaluation_grader_context_invalid. None plus suppression never disables the
ordinary session-audit lifecycle or grants source/administrative mutation access.

GetStatus returns engineeringGuidanceSuppressed=true and guidanceEnabled=false
for this workspace even if instance GuidanceEnabled=true. Hooks emit no engineering
baseline/activity/BDPv4 text; Stateless excludes its mandatory EngineeringPolicy
item. Every grader prompt's delivery ledger records Disabled, including tool-loop
dispatch, restored/compacted context and any absorbed prompt. Tool-loop dispatch
rechecks its bound human request's Disabled entry without inventing another
arrival; an absorbed human prompt has its own entry. Require a successful
current descriptor read, consume its graderContext under19.2 and verify the signed context before launch and dispatch;
descriptor unavailable/stale or context verification failure aborts grading before
model dispatch, never falls back to packaged policy. Ordinary HV workspaces retain
their existing default-on guidance and profile behavior.

Each grading session starts with fresh, history-free context. It never resumes an
implementation, behavioral-run or HV conversation. During quality grading, allowed
task content is the blinded task, frozen scoring rubric and observable outcomes,
plus the signed neutral operator profile and selected raw memories above. Preserve
selected memories verbatim and in effective order; do not summarize or change the
authoritative store. This narrowly scoped override replaces automatic global/
workspace profile, required-memory and other policy-bearing project-context
autoload for the grader. Host system/developer instructions, security boundaries
and audit tools still apply. If a mandatory host context cannot be isolated from
the policy, conditions or fixture provenance, setup is infrastructure-invalid;
do not claim a blind grade from that host.

The materializer creates a separate, content-addressed grading projection with its
own manifest/hash, preserving raw source artifacts unchanged in the broker store.
Only neutral task/rubric fields from the frozen protocol, anonymous run IDs and
observable outcomes are readable by the grader, through22's strict allowlist.
Raw capture/ledger/launch artifacts are never grading observables. The treatment assignment, package metadata and full protocol remain outside
all grader-readable roots and tool/context sources. Record the projection hash and
source artifact hashes in the materialization; server recomputes SubjectDigest from
the full immutable evaluation inputs as14.2 requires. Section19.1 defines the
immutable blind-grade lock and deterministic server aggregate audit; the grader
never receives the mapping or a post-unblinding session. This does not claim behavior itself can never
reveal a condition; retain13.5's stated limitation.

Grading is infrastructure-invalid unless **every grader prompt ledger outcome is
Disabled** and the neutral-context/projection checks pass. Preserve contaminated
artifacts. Behavioral infrastructure failures retain the single paired-rerun rule;
grading-only failures use21.1's single invalid-only regrade of unchanged results,
with no behavioral rerun. A second failure in either applicable budget blocks
activation. TurnReviews owns launch/materialization checks,
SharedComponents the typed settings/status/hash DTOs, plugin core and each adapter
the suppression/context path; DESIGN owns the signed operator configuration.

Add a canary fixture whose unique policy token is present in the canonical policy
and ordinary profile/autoload context but absent from task/rubric/observables.
Capture actual grader model-request context across all9 adapters, including
Stateless, tool loops, restored context and descriptor failure. Assert that the
canary never reaches any grader request, the permitted neutral profile/memories
remain exact, and each dispatched prompt records Disabled. Also test non-Evaluation
suppression refusal, context-memory hash change, missing materialization, unavailable
descriptor refusing dispatch, locked-score immutability, and unchanged ordinary HV
guidance. These tests are planned gates, not claimed executed checks.

Round7: DISAGREE, confidence86, accuracy.97/completeness.97; one finding.
Exact output is retained under claude-round-7. This resolution awaits independent
confirmation under the unchanged12-claim/.98/confidence98 acceptance contract.

## 19. Round 8 grade locking and delivery contracts

### 19.1 Single blind grader and deterministic aggregation

Per21.1, capture intake permits at most one distinct capture per grading attempt,
with invalid-only retry and server-minted session binding; no capture selection.

Choose one blind grading session and **no post-unblinding model audit**. The
grader's captured final response contains the standard14.3 Evaluation receipt
and `blindGrades:[{anonymousRunId,correctOutcomes,requiredOutcomes,metAcs,
requiredAcs,violations:[]}]`. Counts are integers, denominators positive and equal
to the frozen fixture rubric, numerators between zero and their denominators.
Require exactly one entry per accepted anonymous run, no duplicates/missing/foreign
IDs. Violations use the frozen10 boundary categories and include evidence refs.
EV1..EV4 are judged blind: the projection includes the frozen protocol commitment/
freeze time, neutral rubric/projection provenance, outcomes and22's neutral
infrastructure summaries. EV4 uses those summaries; only the server reads raw
ledger evidence. No treatment labels or full protocol reach the grader.
The receipt's Scores remain the reviewer's accuracy/completeness assessment of its
grading, subject to the unchanged profile floors; they are **not** the per-condition
behavioral outcome ratios. Those ratios are exclusively server-computed below.

FreezeProtocol commits `unblindingMapSha256` before runs. The map is canonical JSON
{schemaVersion:1,protocolId,assignments:[{anonymousRunId,fixtureId,repeat,condition,
pairedCaseId,attempt}]}, sorted ordinally by anonymousRunId. Condition is
Baseline|Integrated, repeat1..3, attempt0|1. Preallocate unique IDs for all primary
and possible single paired-retry runs using the frozen seed; commit the map hash
before any observations. No outcomes or later acceptance decisions belong in it.
RecordResults validates/stores that exact map as EvalUnblindingMap in broker-only
storage; a hash mismatch refuses409 evaluation_map_mismatch. It is never mounted,
returned to grader tools or included in grader status/context. The harness manifest
references its opaque hash. Validate accepted run/attempt selection from the frozen
paired retry rules and all retained ledger/results; no caller-selected omission.
There must be36 valid accepted runs per condition and no unresolved Invalid case.

LockBlindGrades on the existing17.1 management endpoint requires protocolId/hash,
materializationId and the broker-stored capturedResponseArtifactId from16.1.
Resolve that capture's bound launch/materialization, CLI identity and exact final
response; never accept replacement caller-authored grade JSON. Verify Ready subject,
projection/context hashes, complete per-run grade shape/counts and every grader
prompt Disabled. Validate the standard blind receipt/profile/claims as above.
Atomically store canonical parsed grades as EvalBlindGrades bound to materializationId,
projectionSha256, subjectDigest and capturedResponseArtifactId. This immutable
artifact plus its durable lock binding is the **grade lock**, not an agent claim
that a score was signed. Same inputs are idempotent; any different capture/grades
for an already locked materialization refuse409 evaluation_grades_locked.

After that lock, the server alone reads the withheld map, joins accepted run IDs,
and recomputes the pooled numerator/denominator sums and exact rational ratios
per condition under14.3. Compare by integer cross-multiplication against98/100
and baseline; do not round to satisfy a floor. It stores EvalAggregate containing
protocol/map/grades/projection/result-manifest hashes, per-condition sums/ratios,
all violations, infrastructure accounting and threshold verdict. Any integrated
ratio below.98, below baseline, nonzero prohibited violation or unresolved Invalid
produces a failing aggregate. LockBlindGrades returns blindGradesArtifactId and
aggregateArtifactId; it does not itself create an AGREE attestation.

Add EngineeringEvaluationGradeLock, composite WorkspaceId/MaterializationId PK
and FK to EngineeringReviewMaterialization, with ProtocolId, ProtocolSha256,
SubjectDigest, ProjectionSha256, CapturedResponseArtifactId, BlindGradesArtifactId,
AggregateArtifactId, LockedUtc, CreatedByOperatorId. UUID/hash lengths, immutable
append semantics, JSON limits, local-only dump rules and transaction handling
inherit13/16; no in-place grade/map edit or delete route. The lock/artifacts are
committed together or not at all. StorageSchema owns its Phase4 migration;
TurnReviews owns validation, storage and recomputation; SharedComponents the DTOs.
Standard management shape/auth/notfound/store errors apply; inconsistent rubric,
run set or receipt is400 validation_error, stale bindings409 engineering_review_stale.

For SubjectKind Evaluation, AttestReview resolves the grade lock by the raw blind
receipt's MaterializationId and adds server-derived refs for EvalBlindGrades,
EvalUnblindingMap and EvalAggregate to the stored attestation's ArtifactRefsJson.
The raw receipt remains unchanged and carries only its original known inputs;
neither grader nor caller must predict hashes of later-created artifacts. Server
resolves those added refs from the immutable lock, never caller substitutions, and
recomputes the aggregate from immutable grades/map/results, checks exact equality
with the stored artifact and enforces both the blind receipt/profile floors and
the behavioral thresholds above. It verifies current evaluation subject inputs,
not a caller's aggregate claim. Activation repeats these checks against the
attested artifact bindings; any mismatch/failing aggregate blocks activation.
The operator attests the blind review and server result together. No second
materialization or post-unblinding grader session is needed. Behavioral infrastructure
retry uses a new immutable results manifest/materialization and retains old records;
grading-only retry uses21.1's unchanged results/new workspace. Neither overwrites
grades or increases its applicable single-retry cap.

Tests: map commitment/withholding, exact accepted run set and denominators,
captured-response binding, atomic lock/idempotence/overwrite refusal, deterministic
aggregation equality, below-floor/noninferior/violation refusal, altered artifact
refusal at attestation/activation, and one paired retry retaining prior artifacts.

### 19.2 Explicit context read channel and Disabled evidence

For suppressed workspaces only, the existing authenticated status read adds
`graderContext:{operatorProfileText,allowedMemoryIds,memoryTexts:[{id,scope,text}],
contextSha256}`. For all other workspaces graderContext=null. Read only from that
workspace's committed signed configuration and effective selected raw memories;
order Global-ID then Workspace-ID. Existing workspace authorization applies;
no admin credentials, map or full protocol are returned. Materialization/launch/
dispatch re-resolve selected memories and verify18's hash. Missing memory or
hash mismatch returns409 evaluation_grader_context_invalid with no context;
server startup/reload only validates shape, so memory deletion does not prevent
server startup. Plugin core/adapters consume and hash-check this exact status
payload before constructing the fresh neutral model context; no inferred profile
or hidden file channel. Stateless uses the same response and suppression rule.

Section13.4's Disabled state is durable per bound human request. Commit-PolicyDelivery
writes it under turn-open.lock for any effective-guidance-false workspace; Duplicate
events and tool dispatch retain/recheck that same Disabled state. Baseline runs
and graders therefore have actual evidence for every prompt, without claiming
delivery of text. Reserved/Unavailable is not Disabled and invalidates evaluation.
Add tests for Disabled creation/dedupe/alias/restore/tool-loop evidence, neutral
status round-trip/injection, selected memory deletion after startup returning409,
and unchanged ordinary-workspace context. These are planned implementation tests.

Round8: DISAGREE, confidence84, accuracy.97/completeness.96; two findings.
Exact output is retained under claude-round-8. R8-01/R8-02 are submitted for the
next independent review under the unchanged twelve claims and score floors.

## 20. Round 9 capture and activation interfaces

### 20.1 Immutable review capture and ledger verification

Add operator-only POST
`/admin/engineering-policy/{workspaceId}/review-materializations/{materializationId}/captures`,
mTLS EngineeringPolicyAdmin; client `RecordReviewCaptureAsync`. WorkspaceId is
the original subject workspace; the stored materialization selects the separate
review workspace. Input:
{cliResultUtf8Base64,sessionExportUtf8Base64,deliveryLedgerUtf8Base64,
launch:{agent,model,effort,cliVersion,providerSessionId,mcpSessionId,hostSessionKey,
arguments:[],startedAtUtc,finishedAtUtc}}. Inputs are exact captured bytes, never
paths/URLs. Session export contains the complete reviewer conversation/event stream
and ingress/request bindings; ledger bytes are a JSON export of that host session's
policyDeliveries entries and alias bindings. Respect existing artifact/count/byte
budgets and exact known-live credential refusal; do not silently redact captures.

TurnReviews parses the raw CLI result/events and session export, extracts the exact
final response/VERDICT, and verifies agent/model/version/session/launch-time
consistency with launch metadata, materialization and effective review profile.
Require successful CLI termination and exactly one final response. CLI arguments
must specify the required effort. Where a CLI does not independently report actual
effort, retain requested effort and mark actual effort unexposed/operator-attested;
do not invent telemetry. Identity/independence remains operator-attested under13.3;
untrusted agent upload cannot invoke this endpoint or create a trusted capture.

This capture record supplies the previously implicit launch binding: operator
certifies the declared launch/session metadata with the capture, and the server
crosschecks its own registered review workspace and minted MCP session/turns.
Reject a foreign workspace/session, wrong profile/CLI identity, missing final
response or mismatched request bindings. For an Evaluation capture, enumerate
**all server-minted grader turns in that mcpSessionId** and require one matching
Disabled ledger entry for each request, using stored ingress aliases where needed.
Reject missing, foreign or conflicting entries and Reserved/Delivered/Unavailable;
verify zero bytes/empty sections and signed neutral-context hash. Session export
must account for every human ingress and tool-loop dispatch and their bound request;
unlogged prompt/dispatch or missing complete export invalidates grading. Tool loops
recheck the same Disabled entry, not a new arrival. Crosschecking is not a claim of
cryptographic remote-host execution proof; the remaining capture honesty is the
explicit operator-attested boundary. Tests must not label forged agent data trusted.

Store exact CLI bytes, session export and extracted final response as separate
HvReceipt artifacts, the exported ledger as DeliveryLedger, all content-addressed.
Add EngineeringReviewCapture, composite WorkspaceId/CaptureId UUID PK, FK
WorkspaceId/MaterializationId, with ReviewWorkspaceId, McpSessionId,
ProviderSessionId, HostSessionKey, LaunchJson, CliArtifactId, SessionExportArtifactId,
DeliveryLedgerArtifactId, CapturedResponseArtifactId, CaptureDigest, CreatedUtc,
CreatedByOperatorId. CaptureDigest hashes canonical launch metadata and artifact
hashes plus materializationId/manifest hash; store append-only with a uniqueness
index on WorkspaceId/MaterializationId/CaptureDigest. Same bytes/bindings return
the same record. Section21.1 additionally permits only one distinct capture per
attempt. Refusal records GradingInvalid only before acceptance; after acceptance
section24 applies. No multiple pre-lock captures.
Return {captureId,capturedResponseArtifactId}; lookup by
that artifact must identify exactly one matching materialization binding, otherwise
refuse ambiguity. LockBlindGrades resolves this record and repeats the identity,
server-turn coverage and Disabled checks; raw receipt upload is not a substitute.

TurnReviews owns behavior/parsers, DESIGN the management surface, SharedComponents
the DTO/client, StorageSchema the Phase4 capture migration. Standard size limits,
400 validation_error,403 admin_authorization_required,404 not_found and503 store
errors apply. Capture identity/ledger/session inconsistencies are409
review_capture_invalid; unsupported CLI capture format is409
review_capture_unsupported. No partial trusted capture. Missing/forged/mixed-state
ledger, unlogged ingress, mismatched identity/materialization, exact extraction,
idempotence, locked-capture refusal and operator-only authorization are test cases.

### 20.2 Defined staging and production activation gate

EvaluationGateRequired is a protected signed EngineeringPolicyOptions bool under
14.5/15.4. Defaults false only support local development/evaluation instances;
staging/production provisioning sets true. Status exposes the effective value.
On those instances ConfigurationReconciliationService defines **activation** as:

- false-to-true EnforcementEnabled: validate every currently registered BDPv4
  source workspace; with none, enforcement is dormant and grants no BDP activation;
- None-to-BDPv4 selection for a source workspace: validate that workspace;
- policy SHA-256 or deployed build artifact SHA-256 changes while a workspace is
  BDPv4 and enforcement enabled: validate it before applying the deployment.

For each affected workspace require a current non-superseded Evaluation AGREE
attestation under the current Evaluation profile, no unresolved DISAGREE, whose
policySha256/buildArtifactSha256 equal the proposed deployed values. Deployed build
hash comes from the operator-approved build artifact manifest, never a caller's
status claim. Re-run19.1's lock/ref/aggregate/threshold checks. Missing, stale,
mismatched or failing evaluation refuses the configuration/deployment with409
`engineering_activation_evaluation_required`, audits the reason/attestation ID,
and retains the previous committed settings. No semantic setting becomes visible
before this check succeeds. Existing Runner/inventory/baseline preconditions still
apply. Enabling EvaluationGateRequired on an already active configuration runs
the same check, preventing an unchecked first signed deployment.

Startup/restart also verifies this invariant before allowing gated BDP work; a
persisted active selection with a changed build/policy or invalid evaluation is
HeldInactive under21.2 with engineering_activation_evaluation_required, never silently
grandfathered. Status/administrative recovery remain available; do not claim a
completed activation. An explicitly signed operator change can disable enforcement
or select None for recovery under the existing audited procedure. This does not
allow an agent to lower EvaluationGateRequired or alter the deployment manifest.

Publishing/installing the default-on1.122.0 plugins to staging/production is also
an operator release-checklist step citing that workspace's matching Evaluation
attestation ID; the server cannot enforce copying external plugin files. State this
procedural boundary explicitly. Local isolated evaluation instances keep the gate
false while producing the evidence, so evaluation bootstrap is not circular.
ConfigurationReconciliationService owns the invariant, DESIGN the deployment/
release-checklist integration, TurnReviews the evaluation verification query.
Test successful activation, missing/stale/failed aggregate refusal with unchanged
settings, None-to-BDP selection, enforcement enablement, gate enablement on active
settings, policy/build replacement, restart holding stale activation inactive,
and the recorded plugin-release attestation reference.

Round9: DISAGREE, confidence86, accuracy.97/completeness.97; R9-01/R9-02.
Exact output is retained under claude-round-9. These interface corrections await
independent review; no product implementation or activation is claimed.

## 21. Round 10 evaluation lifecycle completion

### 21.1 Bounded grading attempts and selection prevention

Per WorkspaceId/Evaluation SubjectDigest, allow GradingAttempt0, and Attempt1
**only** after server-recorded GradingInvalid for0 and before any grade lock.
Each attempt uses a new registered review workspace, one Ready materialization,
exactly one server-minted MCP session created since Ready, and at most one distinct
capture. Section23 also finalizes the protocol's complete results packet on any
attempt allocation, preventing a fresh subject from resetting these limits.
CreateReviewMaterialization first validates22's projection; only then
does it atomically commit Ready and allocate the attempt. Projection refusal
consumes no grading attempt. Uniqueness
and the attempt counter prevent concurrent alternatives. Bind the first MCP session
creation in that review workspace to it. A second session or distinct capture is
refused409 review_capture_invalid: before acceptance it records GradingInvalid;
after acceptance it records24's sticky violation without granting a retry.
Identical capture replay is idempotent, not another grading. No regrade follows
an accepted capture except a bound-capture content failure under24.

Add EngineeringEvaluationGradingAttempt with composite WorkspaceId/SubjectDigest/
AttemptNo PK, AttemptNo0|1, unique ReviewWorkspaceId and MaterializationId,
McpSessionId nullable, CaptureId nullable, State Reserved|GradingInvalid|Locked,
InvalidReasonJson, CreatedUtc, InvalidUtc nullable. Materialization/capture FKs
are in the subject store. MCP session binding is verified through the owner's
registered review-store catalog, not a cross-database FK or caller URL. Only server
materialization/session/capture/lock handlers write it under the same serialized
subject transaction. StorageSchema owns its Phase4 migration, TurnReviews the
state machine, SharedComponents the DTOs; no agent mutation tool exists. The
session-start guard reserves the candidate MCP ID in the subject store before
creating the review-store session; repeat of that ID is idempotent, a different
ID invalidates an attempt only before capture acceptance; after acceptance24's
post-acceptance violation rule applies. These are not a distributed transaction: failed or
unknown review-session creation records GradingInvalid, never retries another ID.
Capture verifies the actual review-store session set against that reservation.

Before capture acceptance, GradingInvalid means CLI failure; not exactly one final response; missing or
non-Disabled ledger entry; context/projection mismatch; any refused authenticated
capture for that attempt; or a400 from LockBlindGrades. The handler records the
invalid state/reason durably before returning its refusal. Operator reports a crash
or absent final output through captures with the failed/empty CLI export; it is
an invalid grading record, not an accepted capture. Unauthorized or foreign-ID
requests cannot invalidate another attempt. A refused capture stores only safe
diagnostics when raw bytes fail credential checks. Grading-only failure retries
the **same immutable behavioral results**, with a fresh session/workspace, once;
it does not consume or grant a behavioral paired rerun. Attempt1 invalid blocks
activation for that workspace/policy/build, including after protocol abandonment.
After acceptance, only24's bound-capture content failure can grant the one regrade.

Add a unique index EngineeringEvaluationGradeLock(WorkspaceId,SubjectDigest).
At most one lock exists per subject; it binds that attempt's sole capture.
Evaluation AttestReview must use a raw receipt artifact **equal to the lock's
CapturedResponseArtifactId**, not another receipt with matching claims. Aggregate
infrastructure accounting lists every grading attempt, session and capture,
including invalid attempts and reasons. A later extra session/capture records
the post-acceptance violation under24 without editing the immutable lock or
converting an accepted capture to GradingInvalid; it invalidates activation and
cannot produce a replacement lock. Activation rechecks current attempt accounting.

For the same workspace/policySha256/buildArtifactSha256, activation also refuses
if **any** lock has a failing behavioral threshold verdict, if two grading attempts
were invalid, if24's post-acceptance violation exists, if an accepted grading
capture has no lock under23, or if any frozen
protocol has no lock and no explicit abandonment.
A successful lock under a new protocol cannot hide an earlier failing lock for
the same policy/build. Repair requires changed policy/build and a new evaluation,
not selective receipt supersession. Existing same-subject DISAGREE rules still apply.

AbandonProtocol on17.1's operator-only endpoint requires protocolId/hash, reason
and signedChangeManifest under14.5's PS256 verifier, with evaluationAbandonment
bound to that exact protocol and otherwise-current settings/build hash. Verify
the operator/certificate/expiry/deploymentId as usual; this operation does not
apply new settings. Sign-EngineeringPolicyChange supports that typed payload.
It writes PolicyAudit
Kind EvaluationAbandoned with protocol/workspace/policy/build, operator, UTC and
reason; show it in status diagnostics and all later aggregate accounting. An
abandonment is not a lock or passing grade. Refuse abandoning a locked protocol
or one with any accepted grading capture under23;
it cannot erase a failing lock or two-invalid-grading block. Unfinished protocol
abandonment permits a new fully disclosed evaluation only under those constraints.

Tests: concurrent attempt allocation, second-session/distinct-capture refusal,
invalid persistence despite400/409, identical retry, crash/empty response, one
invalid-only regrade with identical results, second invalid blocking, unique lock,
receipt/lock equality, aggregate attempt completeness, late capture invalidation,
failed-lock selection refusal and visible signed abandonment with no failure erasure.

### 21.2 HeldInactive gates and exact build identity

Committed enforcement/process settings stay unchanged during a hold. Status is
Dormant when enforcement is off or process None; HeldInactive when enforced BDPv4
requires but lacks valid activation evidence; Active otherwise with respect to
this evaluation check. Existing inventory/runner/baseline checks still apply.
For every Agent target that13.3 rules5-6 would gate, HeldInactive causes the
completion gate and EngineeringTodoExitPolicy to refuse through their **normal
HTTP400 contracts**, adding engineeringPolicy with remedy
`obtain_activation_evaluation`. It takes precedence over refresh_policy for that
item, occurs once in the normal engineering-item order, and does not suppress
other normally applicable missing items. No completion409 or fallback to base rules.
Base-only targets, failed/canceled targets, trusted origins, guidance delivery,
evidence create/run/read, status and administrative recovery remain unchanged.
Amend the C8 golden contract, remedy enum/list, Get-CloseRemedy fixtures and drain
handling to recognize this remedy like the other engineering items. CompletionGate/
TurnReviews own the gate/query; HostPluginAdoption owns the drain/fixture update.

BuildArtifactManifest v1 is canonical JSON
{schemaVersion:1,releaseVersion,serverPackageSha256,
plugins:[{id,version,packageSha256}]}. Exactly nine unique IDs: ClaudeCode,
ClaudeCowork,Cline,ClineV2,Codex,Copilot,Grok,GrokBot,OpenCode, sorted ordinally.
Versions are exact package versions and hashes SHA-256 of exact distributable
package bytes. Apply14.5's canonical UTF-8/no-BOM/key-order rules. BuildTooling's
release build emits it **after** all packages are built; the manifest is outside
the hashed server/plugin packages, avoiding a self-hash cycle. buildArtifactSha256
is SHA-256 of those canonical manifest bytes. The signed change-manifest payload
includes this value; null is allowed only for local non-release guidance with
EvaluationGateRequired=false, never for freezing an evaluation or gated activation.

UpdateService verifies the installed server package against serverPackageSha256
and installs the manifest beside the binary. Retain the immutable installed
package for verification; verify installed packaged payload files against its
entries before activation/startup, rather than equating a ZIP hash with one binary.
Reconciliation requires installed manifest digest = signed change-manifest value
= attested buildArtifactSha256; mismatch refuses or holds activation as20.2/above.
The harness reads/verifies the same manifest/digest for both isolated conditions.
Plugin package entries are enforced by the operator release checklist, not by a
claim that the server can inspect remote host installations.

Every changed server or plugin package changes this build identity and requires
a fresh72-run evaluation before affected staging/production activation, even if
the policy text is unchanged. Only targets installing the same exact manifest
share runs; a different server package/OS manifest requires its own evaluation.
BuildTooling owns canonical emission and release-checklist metadata, UpdateService
installed verification, ConfigurationReconciliationService signed equality.
Tests: all9 IDs/order/version/hash/canonicalization, no self-hash, tampered installed
package/payload/manifest, harness digest equality, server-only patch staleness,
HeldInactive normal400/remedy/order/TODO/drain behavior and unchanged exemptions.

### 21.3 Behavioral captures and pre-run target-store freeze

Each RecordResults run entry carries its exact CLI result, session export,
delivery ledger and launch bytes through17.1's capture object plus
evaluationInstanceId. Apply20.1's parsers, identity checks, budgets, exact credential
refusal and400/409 errors; store HvReceipt/DeliveryLedger artifacts in that run's
server-populated captureArtifactIds, never observableArtifactIds or a grading
projection. Inline launch/capture metadata is audit-only too. Validate every captured human ingress
and tool-loop request binding against15.5: baseline Disabled; integrated
Delivered/Duplicate with a Delivered bound entry. Reserved/Unavailable, absent
bindings, incomplete exports or contradictory states refuse409 review_capture_invalid
and durably record that run as infrastructure-invalid before returning409 under
the single paired-rerun rule. Retain safe raw captures/artifact IDs, or diagnostics
only when credential refusal forbids storage. A later complete RecordResults may
reference those server-recorded Invalid attempts as retained history alongside
valid paired retries; it must not reclassify them as accepted runs or erase them.
Reject newly claimed valid runs with invalid captures. Accepted-run selection uses
the server's immutable attempt states, not caller assertions of validity.
Binding exports to the isolated instance's minted turns is **operator-attested**;
the target server cannot query a foreign store's private turn rows or claim stronger
remote execution proof. Record the instance identity, claimed remote session/turn
IDs and that trust limitation. Harness-authored counts alone are not valid captures.

FreezeProtocol's frozen protocol includes sorted unique targetWorkspaceIds and
the build manifest/map commitments. Before the first behavioral run, the operator
harness must call FreezeProtocol through **each target store's authenticated
management surface** and receive the identical protocol hash. Return/store a
freeze receipt {targetWorkspaceId,protocolId,protocolSha256,frozenUtc}; the harness
records all receipts before dispatching any run. RecordResults requires that set,
the current target's own immutable freeze row, matching target IDs/hash and
pre-run ordering evidence; another store's receipt/order is operator-attested.
No raw database write, inert federation import or post-run backdated freeze counts.
A target lacking the pre-run freeze requires a new evaluation; it cannot adopt
earlier runs by merely importing their result artifacts.

Provision each target's format-aware server/management API before freezing; an
existing enforced BDP workspace stays HeldInactive until evidence is complete.
Local isolated behavioral instances keep EvaluationGateRequired=false. Admin/
status/evidence paths remain available during a hold, avoiding bootstrap circularity.
After shared runs, **each target** records the identical raw captures through its
own RecordResults, creates its own projection and bounded grading attempts, locks
and attests locally. Staging's lock/attestation is not production evidence by
import. Differences between independent target gradings remain visible; each
target must meet its own gates. The72 behavioral runs may be shared only by the
pre-frozen target set and exact build manifest; per-target grading is additional
accounted review work, never silently omitted or selected across stores.

TurnReviews owns capture validation/local lifecycle, DESIGN the authenticated
multi-store harness orchestration/target registration, SharedComponents the capture/
freeze DTOs, BuildTooling the pinned harness inputs. Tests: contamination in any
prompt/tool loop, missing/foreign capture, declared remote trust, same captures/
hashes in two stores, all freezes before dispatch, mismatched/late/missing target
freeze refusal, independent target locks/attestations, and held production setup.

Round10: DISAGREE, confidence81, accuracy.96/completeness.95; three findings.
Exact output is retained under claude-round-10. These R10-01/R10-02/R10-03
resolutions await independent review under the unchanged acceptance contract.

## 22. Round 11 blinded projection allowlist

Behavioral capture artifacts (raw CLI result, session export, DeliveryLedger and
launch metadata) are validation/audit evidence **only**. RecordResults stores
their refs in server-populated per-run captureArtifactIds, distinct from outcomes.
They are never mounted, injected or exposed through grader tools or any grading
projection, even if also referenced elsewhere. Raw captures remain unchanged in
the broker store for server checks/operator audit, not for EV4 model inspection.

The grading projection is an explicit allowlist, not serialization of a whole
result, protocol or artifact reference collection. Allow only:

- neutral frozen task/rubric fields and opaque frozen-protocol commitment/freeze
  time needed for EV1, with no treatment/configuration metadata;
- anonymous run IDs and resultJson's frozen outcome fields: final diff, fixture
  check results and final response; never condition, settings, model, build or
  ledger fields, capture objects or arbitrary unknown fields;
- artifacts classified Outcome by the frozen protocol with an explicit payload
  schema/field allowlist for those outcome types; no raw CLI/session/ledger/launch
  artifact qualifies, regardless of its kind or a caller's label;
- per-run infrastructure summary
  {anonymousRunId,validity:Valid|Invalid,reasonClass,retryUsed:bool}, with reasonClass
  None|ExecutionFailure|CaptureInvalid|ContextInvalid|RetryLimit. Use neutral classes
  common to both conditions, not detailed failure codes that disclose state.

EV4 judges only this neutral infrastructure accounting. The server alone verifies
actual delivery states and remote-capture bindings under21.3. Include no ledger
states, policy hashes, delivered sections/bytes, hook/system context, status or
diagnostic fields. Freeze each projected outcome schema/field set in the protocol;
extract schema-valid fields into separately hashed projection artifacts with source
provenance retained only in the broker audit record. Do not silently redact or
rewrite permitted behavioral outcomes. The materialization exposes only the
projection manifest/artifacts, not audit-only refs or raw source records.

Before Ready and **before grading-attempt allocation**, TurnReviews validates
the complete projected field/artifact set against that allowlist and rejects any
non-allowlisted field, artifact or capture-role overlap with409
evaluation_projection_invalid. No Ready row/session or consumed grading attempt
results. This validates actual projected data, not a caller's declaration that it
was sanitized. SharedComponents owns the projection DTO/schema, TurnReviews the
extractor/ref-role checks, DESIGN the frozen task/outcome/neutral-summary schemas.

Extend18's controlled canary fixture: its token must be present in the integrated
raw CLI/session capture, but absent from every projection artifact and every
grader model request. Use fixture outcomes that do not echo the token, so this
tests hidden-context leakage. Exercise raw capture accidentally included in
observable refs, extra result/config fields, ledger/status/launch injection,
artifact mislabeled as Outcome and the before-Ready/no-attempt refusal. Confirm
valid neutral outcomes/summary reach the grader, while raw ledgers remain available
to server validation. Natural behavioral differences may still reveal a condition;
retain13.5's limitation and never claim perfect blinding.

Round11: DISAGREE, confidence90, accuracy.97/completeness.97; sole R11-01.
Exact output is retained under claude-round-11. This allowlist resolution awaits
independent review under the unchanged12-claim/.98/confidence98 contract.

## 23. Round 12 protocol result finality

Per WorkspaceId/protocolId, once **any grading attempt is allocated** for a
results subject, that complete RecordResults packet is final. A different later
complete packet, including any changed accepted-run EvalResult/resultJson bytes,
is refused409 evaluation_results_locked; an identical repeat is idempotent.
Before any attempt exists, a corrected complete packet may supersede its prior
packet, but retain both immutable manifests/results and their supersession in
protocol and aggregate accounting. Use the existing RecordResults reason field
to explain a correction. No packet/accepted-run mutation occurs in place.

Persist the finalized subject digest and results-manifest hash in the existing
durable workspace/protocol binding. RecordResults correction and grading-attempt
allocation serialize on that same binding: allocation atomically binds the current
complete packet; a racing correction either precedes allocation and is disclosed,
or receives evaluation_results_locked. GradingInvalid does not unfinalize results
or permit a new subject under the same protocol. A new protocol remains subject
to21.1's failed-lock, invalid-attempt and unfinished-evaluation checks.

AbandonProtocol is refused409 evaluation_results_locked once **any grading
attempt for that protocol holds an accepted capture**. That captured grading must
reach LockBlindGrades or server-recorded **content** GradingInvalid under24; it cannot disappear through
abandonment. Retain all captures/states in the accounting, including an invalid
attempt's earlier accepted intake. An accepted capture whose attempt is still
pending adjudication cannot be treated as an abandoned or absent grading.

Activation for the same workspace/policy/build additionally refuses with20.2's
engineering_activation_evaluation_required while any grading attempt holds an
accepted capture without a lock. Check all protocol subjects, not only the chosen
AGREE attestation. Existing21.1 failure/invalid and attestation-equality rules remain
in force; a new result subject cannot hide an accepted unfavorable grading.

TurnReviews owns finalization/capture/activation queries, StorageSchema the
protocol-binding fields/serialization token, SharedComponents the409 contract.
Tests: different packet after allocation refused, identical replay idempotent,
accepted-run bytes immutable, correction before any attempt allowed and disclosed,
allocation/correction race, abandonment after accepted capture refused, and an
unlocked accepted capture blocking activation despite another matching AGREE.

Round12: DISAGREE, confidence90, accuracy.97/completeness.97; sole R12-01.
Exact output is retained under claude-round-12. This finality correction awaits
independent review under the unchanged acceptance contract.

## 24. Round 13 accepted-capture protection

The post-lock protection applies **from capture acceptance**, superseding broader
pre-acceptance invalidation wording in20.1/21.1. After a capture is accepted, an
authenticated second session or distinct re-capture is refused and recorded in
accounting as PostAcceptanceViolation. It does **not** convert that accepted
attempt to GradingInvalid, grants no Attempt1 and refuses activation for the same
workspace/policy/build. Identical idempotent replay remains permitted. Unauthorized
or foreign-ID traffic cannot create this violation for another attempt.

After acceptance, only LockBlindGrades' server-side **content validation of that
bound immutable capture** may record GradingInvalid: inconsistent frozen rubric,
run set or receipt. A request-shape400, authorization error, not-found, stale-binding
or store error never does. Failed caller parameters cannot manufacture a content
failure: resolve the exact stored accepted capture/profile/subject before running
the content validator; it examines those captured bytes, not replacement caller
grades. Preserve the400 validation_error contract but record a content-invalid
reason only for that validator's result. No regrade follows an accepted capture
except this content failure, and never when PostAcceptanceViolation exists.

EngineeringEvaluationGradingAttempt adds
CaptureValidationState None|Accepted|ContentRejected and
PostAcceptanceViolation bool=false. CaptureId remains immutable history. Intake
acceptance sets Accepted. A genuine content failure sets ContentRejected and
GradingInvalid, retaining the original capture/reason; it may allocate the single
Attempt1 if all other21.1 conditions pass. For23's pending accepted-capture check,
Accepted means the active validation state, not a historically accepted capture
subsequently ContentRejected. Thus an actual malformed grading can be regraded
once, while a valid low grading or a malformed caller request cannot be discarded.
Aggregate accounting includes both historical intake and content rejection.
Activation checks all attempts for sticky PostAcceptanceViolation in addition to
existing failed-lock/two-invalid/unfinished/pending-capture checks.

Aborting a grader before successful termination can look identical to a crash.
That remains an explicitly operator-attested residual: record observed termination,
available session/capture evidence and every attempt; do not claim that the server
proves the cause or prevents strategic pre-termination aborts. The single retry
limit and disclosed accounting still apply. This does not relax accepted-capture
protection or permit an undisclosed new subject/results packet.

TurnReviews owns the validator/state/activation checks, StorageSchema the typed
attempt fields, SharedComponents the error/state DTOs. Tests: second session and
distinct capture after acceptance set the sticky violation/no Attempt1; malformed
request/auth/notfound/stale/store errors never grant a retry; genuine captured
content400 permits the single regrade and clears only the pending active-acceptance
check; valid low grades cannot be invalidated by caller input; historical intake/
rejection and observed pre-termination failure remain disclosed.

Round13: DISAGREE, confidence94, accuracy.98/completeness.98; sole Low R13-01.
Exact output is retained under claude-round-13. This correction awaits independent
review under the unchanged all12-PASS/.98/confidence98 acceptance contract.
