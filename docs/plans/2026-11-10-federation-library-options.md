# Federation: existing-solution options for operator decision

2026-11-10. Codex. Research proposal, not a dependency adoption or compatibility certification.

## Decision requested

Keep LAB-OMARCHY as hub and PAYTON-LEGION2 as first spoke. Prefer supported library facilities over custom authentication, HTTP forwarding, retry queues and synchronization. Approve or deny the candidates below before dependency adoption. Recommendations are engineering judgments; documented capabilities and remaining unknowns are distinguished below.

Operator clarification, 2026-11-10: local nodes perform the same business work as the hub, remain operational when disconnected, and exchange updates with the hub in both directions. Evaluate libraries against that intended behavior. This supersedes the earlier forwarding-first recommendation; it does not establish when the implementation diverged from the operator's original intent.

Subsequent operator decisions: branches are in the initial release; concurrent edits use Git-style merging with conflicts reviewed at the receiving node; pull requests are out of scope. The operator then asked for Git with relational storage. The most relevant new options are G (Dolt: versioned relational data), H (Fossil: distributed version control stored in SQLite), and I (literal Git via relational libgit2 object backends). These are distinct architectures, not interchangeable packages. Earlier A-F remain reference candidates; their earlier ranking is superseded by this question. Dotmim.Sync and Wolverine must demonstrate branch/history fit rather than being treated as complete solutions. Operator decision 2026-11-11: Dolt (option G) is selected as the versioned-storage engine; the other options remain reference candidates. No package is adopted or installed by this document, and engine feasibility remains gated by Ph0 B1.

## A. Reuse the existing identity integration: Duende IdentityServer

Repository evidence: `Directory.Packages.props` pins Duende.IdentityServer 7.4.7 and its AspNetIdentity/EntityFramework packages at 7.4.6. The existing host integration has client-credentials support and static clients. Its current SQL Server wiring is an application configuration fact, not evidence that Duende supports only SQL Server.

Duende documents reference tokens with introspection and operational persistence, plus EF configuration and operational stores. Reuse supported issuer, credential, persistence and revocation facilities rather than inventing another credential or revocation ledger. Application authorization still must bind the authenticated user, node and allowed workspaces. A client-credentials token by itself is not proof that a human signed in.

Approval scope: retain Duende and design federation authentication around its supported facilities. Before implementation, verify the installed v7 APIs, runtime/provider compatibility, package alignment and deployment entitlement. Choose token mode and revocation/cache behavior against the approved outage requirements. Current online documentation may describe newer versions; it does not prove those APIs exist in the pinned version.

Sources: [reference tokens](https://docs.duendesoftware.com/identityserver/tokens/reference/), [EF stores](https://docs.duendesoftware.com/identityserver/data/providers/entityframework-core/), [v7 client model](https://docs.duendesoftware.com/identityserver/reference/v7/models/client/).

## B. Replace forwarding mechanics: YARP

Microsoft's `Yarp.ReverseProxy` supplies `IHttpForwarder`, including streaming, dynamic destination selection and response forwarding. Direct forwarding does not supply retries, application routing or authorization policy. Our code would still decide the authorized hub/workspace, enforce hop limits, and protect internal headers.

Recommendation: remove YARP from the critical federation path for the clarified local-processing model. Evaluate it only if an independent proxy requirement remains after reconciliation. It does not provide database synchronization or offline business execution.

License: MIT in the upstream repository. Exact package version and dependency compatibility remain to be pinned before adoption.

Sources: [Microsoft direct-forwarding documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/direct-forwarding?view=aspnetcore-10.0), [license](https://github.com/dotnet/yarp/blob/main/LICENSE.txt).

## C. Replace durable delivery machinery: Wolverine

Wolverine documents transactional inbox/outbox persistence, EF Core integration and an HTTP messaging transport with retries and durable outbox support. The HTTP transport avoids requiring a separate broker. Its SQLite integration documents file-backed persistence; its durability documentation also covers SQL Server and PostgreSQL.

Wolverine supports CQRS-style command handling as well as durable messaging. Recommendation: a candidate for publishing committed domain changes, not a complete replication solution. It can replace delivery/retry infrastructure, but conflict resolution and state convergence still need evidence. This fit is an inference until the exact version/provider/transaction combination is tested. Do not replay a locally executed business command remotely if doing so would repeat external side effects.

Wolverine is not a turnkey database synchronizer. Domain enumeration, canonical IDs, ownership and parity checks remain application concerns. Determine whether an existing backfill needs a separate progress marker at all when durable message acceptance owns progress. Never equate inbox deduplication with globally exactly-once effects.

Before adoption prove: domain mutation plus outbox write share the intended transaction on every required provider; receiver apply plus inbox state recover after crashes; HTTP endpoints enforce node/workspace authorization; duplicates and lost acknowledgements produce no extra effects; old queue rows can be preserved and migrated without two active delivery owners. If this needs a new custom transport or durability engine, reject that integration design and compare option D.

License: MIT in upstream. Exact package versions, support policy and transitive dependencies remain to be pinned.

Sources: [durability](https://wolverinefx.io/guide/durability/index.html), [EF Core integration](https://wolverinefx.io/guide/durability/efcore/), [HTTP transport](https://wolverinefx.io/guide/http/transport.html), [SQLite persistence](https://wolverinefx.io/guide/durability/sqlite.html), [license](https://github.com/JasperFx/wolverine/blob/main/LICENSE).

## D. Replace database synchronization machinery: Dotmim.Sync

Dotmim.Sync is a .NET database-synchronization framework with web client/server components. Its current source tree includes SQL Server, SQLite, PostgreSQL, MySQL and MariaDB provider projects. This makes it a serious candidate for initial and incremental hub/spoke synchronization, potentially removing more of F3/F4 than a messaging library.

Recommendation: retain as a synchronization candidate, subject now to proof of branch/history/merge fit. Source-tree presence does not establish a released, supported PostgreSQL package or its behavior. Older documentation says PostgreSQL is future work; resolve that discrepancy from a pinned release, package metadata and tests, rather than rejecting or accepting it on stale docs. Its documented default conflict policy is ServerWins; that is a library default, not an approved product decision.

Key fit questions: can server-enforced workspace filters prevent foreign-workspace data access; can hub-owned fields remain authoritative; do mixed-provider schemas, FK graphs, tombstones and canonical identifiers match the approved data model; does direct table synchronization preserve required domain validation and audit behavior; can existing data be provisioned without destructive schema work? A need for broad bespoke providers or engine modifications would undermine the speed benefit.

License: MIT in upstream. No package installed; release/runtime/provider support remains unverified.

Sources: [project](https://github.com/Mimetis/Dotmim.Sync), [provider and web project inventory](https://github.com/Mimetis/Dotmim.Sync/tree/master/Projects), [older overview](https://dotmimsync.readthedocs.io/Overview.html), [license](https://github.com/Mimetis/Dotmim.Sync/blob/master/LICENSE).

## E. External replication engine: SymmetricDS

SymmetricDS offers heterogeneous database replication and lists SQL Server, PostgreSQL and SQLite. Its current editions page requires Java 21+, lists Community under AGPLv3 with 15-minute synchronization intervals, and commercial editions with shorter intervals. Verify connector availability and limits for the exact edition before selecting it.

This is an option if a separately operated replication service is acceptable. It could reduce in-process replication development, but adds an operational component and does not establish application authorization, ownership or domain validation. Recommendation: retain as an alternative rather than the first choice for this .NET service; exact licensing eligibility, cost, deployment and policy fit are unresolved. No license purchase or service installation is authorized.

Sources: [official editions and platform list](https://symmetricds.org/editions/), [official FAQ](https://symmetricds.org/docs/faq/).

## Common acceptance evidence before adoption

- Pin a released version, license, maintainer provenance and runtime support. Follow `docs/context/compliance-rules.md`; maintainer origin and deployment license entitlement are not yet certified by this research.
- Enumerate the existing components removed, reused or retained. Migration preserves old queue/checkpoint records and never silently discards unacknowledged operations.
- Demonstrate SQLite, SQL Server and PostgreSQL where required by the approved product scope. Actual lab-provider inventory remains a separate prerequisite.
- Prove ownership isolation, unauthenticated denial, hub-authoritative fields, duplicate delivery, lost acknowledgement, crash/restart and offline recovery using approved contract cases. Prove record/relationship parity independently of the library's own success flag.
- Measure implementation and operational burden after the compatibility evaluation. Do not claim a calendar saving without evidence.
- Prove independent branches, retained common ancestors, three-way merge behavior and review of incoming conflicts. Neither reliable message delivery nor two-way row synchronization alone proves these capabilities. Pull-request infrastructure is out of scope.

The operator can approve or deny D, choose C for domain-change replication instead, request E, or reject individual candidates. A remains the existing identity integration; B is no longer a federation prerequisite. Approval of an evaluation does not approve production adoption; the final plan records the exact chosen versions, integration boundaries and migration contract for approval. No default custom engine is authorized when an option fails.

Additional primary sources for the clarification: [Wolverine durable messaging](https://wolverinefx.io/guide/durability/), [Dotmim.Sync conflict handling](https://dotmimsync.readthedocs.io/Conflict.html). The latter is versioned 0.9.5 documentation, so verify the selected release rather than treating it as current-package proof.

## F. Evaluate actual Git for versioned workspace state

The operator identified Git's distributed model as the intended federation model and approved branches for this release. Evaluate native Git or a maintained binding for actual history/branch/merge operations. Git's three-way content merging is available through [git merge-file](https://git-scm.com/docs/git-merge-file); ancestry selection is a separate responsibility described by [git merge-base](https://git-scm.com/docs/git-merge-base). Reuse these facilities instead of implementing a bespoke merge algorithm.

This is a candidate requiring an integration design: define the branchable record representation, local database consistency/recovery, histories, relationship validation and transport authorization. Git alone does not make a successful text merge a valid domain transaction. Native Git 2.56.0 was observed on LAB-OMARCHY; cross-host/runtime and binding/license evaluation remain pending. No claim is made that Git or a .NET binding has already been selected as the storage authority. Pull requests, forge integration and automated PR review are explicitly excluded from the initial release.

## G. Dolt: a relational database with distributed version control

Dolt is a MySQL-compatible SQL database with commit, branch, merge, clone, fetch, push and pull operations exposed through its CLI and SQL facilities. Its upstream repository uses Apache-2.0. It is a database engine with Git-like semantics, not Git configured to store objects in an arbitrary existing SQL Server/PostgreSQL database.

Recommendation: strongest current functional candidate to evaluate for the clarified branchable relational-data design. It may remove the need to invent history, branch synchronization and merge infrastructure. That is an architectural fit judgment, not verified compatibility with McpServer. It would require an explicit provider/storage migration decision and an EF/schema/transaction evaluation.

Material difference requiring operator approval: Dolt merges data cell by cell and records data/schema conflicts. It uses relational primary keys for row identity and has special JSON merge behavior. This is not Git's line-based file merge algorithm. Its conflict facilities may be better suited to relational data, but do not silently substitute them for the operator's requested algorithm. The application's review requirement must prohibit unreviewed destructive conflict resolution. Operator decision 2026-11-11: the operator accepted Dolt's cell-level relational merge, with mandatory conflict review, as satisfying the Git-style merging requirement for this release.

Local copies exchange data via configurable remotes; the docs include self-hosted and SSH/HTTPS options. No DoltHub subscription or PR system is proposed for initial release. Doltgres provides a PostgreSQL-compatible alternative in the same ecosystem; compatibility does not mean it is a plugin for stock PostgreSQL. DoltLite is a SQLite-derived engine with its own storage implementation; the vendor calls it Beta. The October 2026 vendor update specifically identifies remotes and concurrency/durability as continuing focus areas. Do not assume Dolt, Doltgres and DoltLite can replicate interchangeably without checking their formats and protocols.

Sources: [Dolt repository and license](https://github.com/dolthub/dolt), [merge semantics](https://www.dolthub.com/docs/concepts/dolt/git/merge/), [conflicts](https://www.dolthub.com/docs/concepts/dolt/git/conflicts/), [remotes](https://www.dolthub.com/docs/concepts/dolt/git/remotes/), [product variants](https://www.dolthub.com/), [storage-engine distinctions](https://www.dolthub.com/blog/2026-06-17-what-people-are-saying-lwn/), [DoltLite Beta](https://www.dolthub.com/blog/2026-08-31-doltlite-beta/), [DoltLite October status](https://www.dolthub.com/blog/2026-10-12-doltlite-gets-better/).

## H. Fossil: a distributed repository stored in SQLite

Fossil stores its repository state, immutable artifacts and derived metadata in SQLite. It provides local clones, branching, merging, push and pull as an existing distributed version-control system. This directly demonstrates that relational storage can underpin the proposed model.

It is a different DVCS with Git import/export facilities, not the Git implementation or Git's exact merge engine. SQLite stores Fossil artifacts; this does not automatically version McpServer's existing business tables or supply an EF mapping. Evaluate supported artifact integration and local database consistency before selecting it. Additional built-in collaboration features do not expand the initial-release scope. License and dependency integration review remain pending.

Sources: [Fossil technical overview](https://fossil-scm.org/home/doc/trunk/www/tech_overview.wiki), [Fossil versus Git](https://fossil-scm.org/home/doc/trunk/www/fossil-v-git.wiki).

## I. libgit2 with SQL object-database backends

The libgit2 organization's `libgit2-backends` repository contains SQLite and MySQL object-database backends. This is the most literal example found of Git objects in an RDBMS. It is a collection of backend components rather than a complete ready-to-run federation platform.

Before considering adoption, verify current libgit2 API compatibility, maintenance, license, reference storage, transaction/recovery behavior and .NET binding support. An object backend alone does not prove all repository state resides in SQL or that a complete secure synchronization service exists. The repository's open PostgreSQL backend proposal is not evidence of a released supported PostgreSQL backend. Do not adopt a custom-backend development project by calling it library reuse.

Sources: [backend repository](https://github.com/libgit2/libgit2-backends), [open backend proposals](https://github.com/libgit2/libgit2-backends/pulls).
