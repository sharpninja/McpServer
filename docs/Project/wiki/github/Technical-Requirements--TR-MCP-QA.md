# TR-MCP-QA

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-QA-001

**QA Entity Tenancy** — `QuestionEntity`, `AnswerEntity`, `CommentEntity` use composite PK `(WorkspaceId, Id)` plus global query filter (mirrors TR-MCP-MT-003).
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-090, FR-MCP-093; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-115, TEST-MCP-125
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-002

**QA Question Tags JSON** — Tags stored as `TagsJson` string column on `QuestionEntity` (no separate Tag table), serialized via the same pattern as `TodoItemEntity.DescriptionJson`.
**Covered by:** FR: FR-MCP-085, FR-MCP-088; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-113
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-003

**QA Provider Migrations** — Q&A storage works on all three providers: SQLite, PostgreSQL, SQL Server. One migration per provider project.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-093; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-004

**QA Denormalized Vote Counters** — `VoteCount` is an `int` column on Question and Answer; vote endpoints use atomic `UPDATE ... SET VoteCount = VoteCount + @delta` via raw SQL or EF interceptor pattern. Voter identity is NOT stored on the Question/Answer row itself - it is captured per vote in the audit history (TR-MCP-QA-019 records `Actor` on every `vote_up` / `vote_down` audit row), preserving full provenance without bloating the hot read path.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-005

**QA Accepted Answer Storage** — `AcceptedAnswerId` is a nullable string FK on `QuestionEntity`; accepting writes `AcceptedAnswerId` + `AcceptedAt`; un-accepting clears both.
**Covered by:** FR: FR-MCP-087; TEST: TEST-MCP-112, TEST-MCP-116
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-006

**QA Service Shape** — `EfQaService` follows the `EfTodoService` shape: `IServiceScopeFactory` for scoped DbContext access, `IWriteAuditLog` for audit, optional `IChangeEventBus` for events, internal `SemaphoreSlim` for write serialization.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-087, FR-MCP-090; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-112, TEST-MCP-116, TEST-MCP-115, TEST-MCP-125
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-007

**QA REST Surface** — `QaController` routes at `/mcpserver/qa`; `WorkspaceResolutionMiddleware.WorkspaceIndependentPrefixes` adds `"/mcpserver/qa"` (joining `/mcpserver/todo` and `/mcpserver/sessionlog`). Auth is enforced by existing `WorkspaceAuthMiddleware`.
**Covered by:** FR: FR-MCP-085, FR-MCP-086, FR-MCP-088, FR-MCP-094; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128, TEST-MCP-113, TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-008

**QA Search Indexing** — Q&A search integration: `IQaSearchIndexer` writes a `ContextDocumentEntity` (`SourceType = "qa-question"` or `"qa-answer"`, `SourceKey = $"qa/{kind}/{id}"`) and a `ContextChunkEntity` (`Content = Title + "\n\n" + Body + "\n\nTags: " + tags`) on create/update, and removes both on delete. Existing FTS5 triggers and `EmbeddingService` handle the rest; no new FTS virtual table.
**Covered by:** FR: FR-MCP-085, FR-MCP-086; TEST: TEST-MCP-110, TEST-MCP-117, TEST-MCP-119, TEST-MCP-111, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-009

**QA Author Resolver** — `IQaAuthorResolver` reads `HttpContext.User.FindFirst("sub")`, the `X-Api-Key` header via `WorkspaceTokenService`, and a request-body `author` field, applying the precedence in FR-MCP-QA-008.
**Covered by:** FR: FR-MCP-092; TEST: TEST-MCP-118
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-010

**QA MCP STDIO Tools** — MCP STDIO tools live on the existing `FwhMcpTools` class (file `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`); each Q&A tool accepts optional `workspacePath` and calls the existing `ApplyWorkspaceOverride` helper.
**Covered by:** FR: FR-MCP-094; TEST: TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-011

**QA FAQ Query Projection** — FAQ projection executes as a single EF query: `Questions.Where(q => q.AcceptedAnswerId != null).Include(q => q.AcceptedAnswer).OrderByDescending(q => q.AcceptedAnswer!.VoteCount).ThenByDescending(q => q.VoteCount).Take(limit)`.
**Covered by:** FR: FR-MCP-087, FR-MCP-091; TEST: TEST-MCP-112, TEST-MCP-116, TEST-MCP-128
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-012

**QA Typed Client** — `QaClient` ships in `McpServer.Client` (NuGet `SharpNinja.McpServer.Client`); wired into `McpServerClient.Qa` via `McpServerClientFactory`.
**Covered by:** FR: FR-MCP-094; TEST: TEST-MCP-120, TEST-MCP-121
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-013

**QA XML Documentation** — XML docs on every new public type and member (CS1591 enforced). Test classes cite TR-PLANNED-013 plus the FR/TR/TEST IDs they validate.
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-014

**QA REPL Workflow** — REPL exposure: `IQaWorkflow` in `McpServer.Repl.Core` wraps `McpServerClient.Qa`; `QaWorkflow` registered as singleton in `McpServer.Repl.Core/ServiceCollectionExtensions.cs`; `QaCommandShapes` defines `MethodNamespace = "workflow.qa"` and per-method constants; `ReplCommandDispatcher` constructor takes `IQaWorkflow` and switches on `workflow.qa.*` methods.
**Covered by:** FR: FR-MCP-095; TEST: TEST-MCP-122, TEST-MCP-123
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-015

**QA PowerShell Module** — PowerShell module `tools/powershell/McpQa.psm1` reads `AGENTS-README-FIRST.yaml` via existing `Find-McpMarkerFile`/`ConvertFrom-McpMarkerContent` helpers, exports `Get-McpQuestion`, `Search-McpQuestion`, `New-McpQuestion`, `Set-McpQuestion`, `Remove-McpQuestion`, `Add-McpAnswer`, `Approve-McpAnswer`, `Add-McpQaVote`, `Add-McpQaComment`, `Get-McpFaq`.
**Covered by:** FR: FR-MCP-095; TEST: TEST-MCP-122, TEST-MCP-123
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-016

**QA Plugin Skill** — Plugin skills: each sibling plugin repo adds `skills/qa/SKILL.md` with the standard YAML frontmatter (`name`, `description` with trigger phrases, `version`). Body documents the `workflow.qa.*` command namespace, request envelope shape, response shape, and explicitly positions Q&A as a workspace knowledge source (read FAQ first; if no accepted answer matches, ask; if a peer answers and accepts, future agents inherit the answer via FAQ + hybrid search).
**Covered by:** FR: FR-MCP-097; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-017

**QA Documentation Surface** — Documentation: a new `docs/context/qa-schema.md` defines the on-demand schema reference (entities, IDs, FAQ projection); `docs/USER-GUIDE.md`, `docs/CLIENT-INTEGRATION.md`, `docs/context/api-capabilities.md`, `docs/REPL-USER-GUIDE.md`, `docs/REPL-AGENT-GUIDE.md`, `docs/FAQ.md`, `AGENTS.md`, and the root `README.md` are updated to surface the subsystem; `CLAUDE.md` `Context Loading by Task Type` adds a Q&A row.
**Covered by:** FR: FR-MCP-096; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-018

**QA Audit Storage** — Audit storage: `QaAuditHistoryEntity` (composite PK `(WorkspaceId, Id)`, columns `EntityKind` enum `question`/`answer`/`comment`, `EntityId`, `Action` enum `create`/`update`/`delete`/`accept`/`unaccept`/`vote_up`/`vote_down`/`comment_add`/`comment_delete`, `Version` int, `Actor`, `SnapshotJson`, `CreatedAt`) lives in `src/McpServer.Storage/Entities/` with the same global query filter as Q&A entities. Composite index on `(EntityKind, EntityId, Version)`.
**Covered by:** FR: FR-MCP-089, FR-MCP-090, FR-MCP-098; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135, TEST-MCP-115, TEST-MCP-125, TEST-MCP-126
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-019

**QA Audit Emission** — Audit emission: `EfQaService` injects `IWriteAuditLog` and calls it before persisting each mutation, capturing the pre-mutation snapshot (post-mutation for create) and computing `Version = MAX(Version) + 1` for that `(EntityKind, EntityId)` pair (mirrors `EfTodoService` audit pattern at line ~374).
**Covered by:** FR: FR-MCP-089, FR-MCP-090, FR-MCP-098; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135, TEST-MCP-115, TEST-MCP-125, TEST-MCP-126
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-020

**QA Audit Query** — Audit query: `IQaService.GetAuditAsync` returns `QaAuditQueryResult` with `TotalCount` + paged `QaAuditEntryDto` rows; filterable by `entityKind`, `entityId`, `action`, `from`, `to`, `actor`. `EfQaService.GetAuditAsync` uses an EF query mirroring `EfTodoService.GetAuditAsync`.
**Covered by:** FR: FR-MCP-098; TEST: TEST-MCP-125, TEST-MCP-126, TEST-MCP-127
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-021

**QA Audit Surfaces** — Audit surfaces: `QaController` adds `[HttpGet("questions/{id}/audit")]`, `[HttpGet("answers/{id}/audit")]`, `[HttpGet("comments/{id}/audit")]`, and `[HttpGet("audit")]`; MCP tool `qa_get_audit`; client `QaClient.GetAuditAsync`; REPL `workflow.qa.audit`; PowerShell `Get-McpQaAudit`; plugin skill documents audit access as a knowledge-provenance tool.
**Covered by:** FR: FR-MCP-098; TEST: TEST-MCP-125, TEST-MCP-126, TEST-MCP-127
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-022

**QA Answer Sources JSON** — Answer payload supports a structured `sources[]` array (each entry: `{ url, title?, fetchedAt, contentHash? }`) so web-search-derived answers carry their citations. Stored on `AnswerEntity` as `SourcesJson` (nullable string column). DTO `AnswerDto.Sources`, request `CreateAnswerRequest.Sources`. FAQ projection surfaces sources alongside the accepted answer.
**Covered by:** FR: FR-MCP-086, FR-MCP-091, FR-MCP-099; TEST: TEST-MCP-111, TEST-MCP-117, TEST-MCP-128, TEST-MCP-116, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-023

**QA Mandatory Web Capture Skill Rule** — Plugin skill mandate: every `skills/qa/SKILL.md` opens with a mandatory rule block, formatted as a callout (`> [!IMPORTANT]`), stating: "Any time you invoke an internet search, web fetch, browser MCP, or any external HTTP retrieval to help answer a workspace question, you MUST create a Q&A entry capturing the question, the synthesized answer, and the source URLs in the `sources[]` array. If you verified the answer, accept it. This is required, not optional." Skill also documents the exact `workflow.qa.create` + `workflow.qa.createAnswer` + `workflow.qa.accept` sequence and the `sources[]` schema.
**Covered by:** FR: FR-MCP-097, FR-MCP-099; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-024

**QA Companion Web Skill Cross References** — Companion skills in each plugin that perform web research (`web-research`, `browser`, `search`, or equivalents already shipped in each plugin) get an inline "Required follow-up" section pointing back at the Q&A skill, so the mandate is visible from both directions. Implementation step: enumerate each sibling plugin's existing skills and add the cross-reference where applicable.
**Covered by:** FR: FR-MCP-097, FR-MCP-099; TEST: TEST-MCP-124, TEST-MCP-129
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-025

**QA Close and Duplicate Storage** — Close / duplicate storage: extend `QuestionEntity` with `IsClosed` (bool), `ClosedAt` (DateTimeOffset?), `ClosedByActor` (string?), `CloseReason` (enum string `closed`/`duplicate`/`off-topic`/`resolved`/`other`), `DuplicateOfQuestionId` (nullable, with a self-referencing FK + index). FAQ projection filters out closed questions unless `?includeClosed=true`. Search indexer emits a `closed` flag in the `ContextChunkEntity.Metadata` JSON so hybrid-search consumers can filter.
**Covered by:** FR: FR-MCP-100; TEST: TEST-MCP-130
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-026

**QA Close and Duplicate Surfaces** — Close / duplicate endpoints: `POST /mcpserver/qa/questions/{id}/close` (body `{ reason, duplicateOfQuestionId? }`), `POST /mcpserver/qa/questions/{id}/reopen`, both writing audit rows with action `close` / `reopen` / `mark_duplicate`. Surface in MCP tool (`qa_close_question`, `qa_reopen_question`), client (`CloseQuestionAsync`, `ReopenQuestionAsync`), REPL (`workflow.qa.close`, `workflow.qa.reopen`), PowerShell (`Close-McpQuestion`, `Open-McpQuestion`), and skill body.
**Covered by:** FR: FR-MCP-100; TEST: TEST-MCP-130
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-027

**QA Body Rendering** — Sanitization pipeline: add NuGet packages `Markdig` (markdown -> HTML) and `Ganss.Xss` to `Directory.Packages.props` (central package management). New service `IQaBodyRenderer` (impl `QaBodyRenderer`) renders + sanitizes using a strict allow-list: tags `p, h1, h2, h3, ul, ol, li, code, pre, strong, em, a, blockquote, table, thead, tbody, tr, th, td, hr, br, img`; attributes `href, src, alt, title, class` (with `class` restricted to a set of code-highlight classes); URLs limited to `http`, `https`, `mailto`; force `rel="nofollow noopener"` on all `<a>`; drop `script`, `iframe`, `object`, `embed`, `form`, all `on*` attributes, and `javascript:` URLs. Renderer is called by `EfQaService` on every Create / Update for Question, Answer, and Comment, populating sibling columns `TitleHtml?` (Question only), `BodyHtml`, plus answer/comment equivalents. Audit snapshots also store the rendered HTML so historical views are safe.
**Covered by:** FR: FR-MCP-101; TEST: TEST-MCP-131
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-028

**QA Sanitization Tests** — Sanitization tests: `tests/McpServer.Support.Mcp.Tests/Services/QaBodyRendererTests.cs` covers a canonical XSS-payload corpus (script tags, `<img onerror>`, `javascript:` href, data URLs, nested HTML in markdown, comment-out attacks, html entities). Same corpus runs against the live controller via `tests/McpServer.Qa.Validation/ErrorTests/SanitizationTests.cs`.
**Covered by:** FR: FR-MCP-101; TEST: TEST-MCP-131
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-029

**QA FAQ Wiki Generation Target** — FAQ wiki page generation: add a Nuke build target (e.g. `BuildFaqWikiPage`) in `build/Build.cs` (or whatever the existing target file is) that POSTs `GET /mcpserver/qa/faq?limit=500&includeSources=true` to a configured workspace (env-var-driven endpoint + API key, mirroring existing wiki publication patterns), formats the response into Markdown, writes `docs/Project/wiki/azure/FAQ.md` and `docs/Project/wiki/github/FAQ.md`, and updates `Home.md`, `_Sidebar.md`, and `.order` entries to list the FAQ page. Target is wired into the existing publication target so `./build.ps1` rebuilds the FAQ page alongside the requirements wiki.
**Covered by:** FR: FR-MCP-102; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-030

**QA FAQ Wiki Snapshot Tests** — FAQ wiki content tests: a `tests/Build.Tests/FaqWikiPageTests.cs` test invokes `BuildFaqWikiPage` against a fixture FAQ JSON payload, snapshot-compares the generated Markdown to a checked-in expected output to catch unintended formatting changes, and asserts the wiki index files reference the new page.
**Covered by:** FR: FR-MCP-102; TEST: TEST-MCP-132
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-031

**QA Voter History** — Voter-history endpoints (derived from audit): `GET /mcpserver/qa/questions/{id}/voters` and `GET /mcpserver/qa/answers/{id}/voters` return the audit rows for that entity filtered to `Action IN ('vote_up','vote_down','vote_change','vote_revoke')`, projected as `{ actor, action, createdAt }` with paging. Same surface exposed through MCP (`qa_get_voters`), client (`QaClient.GetVotersAsync`), REPL (`workflow.qa.voters`), and PowerShell (`Get-McpQaVoters`). The plugin skill documents this endpoint as the canonical way to answer "who voted on X". A companion `GET .../votes` endpoint returns the current per-voter state (one row per active voter) from `QaVoteEntity` for "what is each voter's current position" queries.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-032

**QA Vote State Storage** — Per-voter state storage: new `QaVoteEntity` (composite PK `(WorkspaceId, Id)`; columns `EntityKind` (`question`/`answer`), `EntityId`, `VoterActor`, `VoteValue` (`1` or `-1`), `CreatedAt`, `UpdatedAt`). Unique index `(WorkspaceId, EntityKind, EntityId, VoterActor)` enforces one-vote-per-user-per-entity at the database layer (prevents race conditions even under concurrent vote calls). Global query filter on `WorkspaceId` matches other Q&A entities.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-033

**QA Vote State Machine** — Vote state machine: `EfQaService.VoteAsync(entityKind, entityId, delta, actor)` runs in a single transaction that (a) looks up the existing `QaVoteEntity` row, (b) applies one of `no-op` (same vote already exists), `apply` (no existing vote -> insert + counter +/- 1), `change` (opposite vote exists -> update row + counter +/- 2), or `revoke` (delta is 0 and a vote exists -> delete row + counter -/+ 1), (c) writes the corresponding audit row with action `vote_up` / `vote_down` / `vote_change` / `vote_revoke` (no audit row on `no-op`), (d) updates the Question/Answer counter via atomic `UPDATE`. Returns the resulting state so callers know which branch ran.
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

## TR-MCP-QA-034

**QA Vote Audit Actions** — Vote audit-action enum extends to: `vote_up`, `vote_down`, `vote_change`, `vote_revoke`. Migration adds the new values to the audit action enum check constraint (where one exists - SQLite stores as string, SqlServer / Postgres via check constraint).
**Covered by:** FR: FR-MCP-089; TEST: TEST-MCP-114, TEST-MCP-127, TEST-MCP-133, TEST-MCP-134, TEST-MCP-135
**Status:** pending
Scope: layer-1+

