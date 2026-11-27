# Provision TODO-backed slices

Use this procedure when Task Groups are not implemented or the active host cannot read them. It requires only the existing QBrain TODO operations. TODO-backed slices are first-class, not mirrors for native groups. Do not create or infer a Task Group or a UUID.

## 1. Reconcile read-only

1. Bind to the trusted workspace using its installed QBrain tools. Read the exact umbrella TODO and compare it with the selected plan and source revision. A successful umbrella read proves the legacy checklist is available, not that native groups are available or empty. `method_not_found`, `Unknown client`, and unavailable or incomplete queries mean **unsupported/unverified**, not zero. Only a successful workspace- and umbrella-scoped lookup through all pages with no results proves empty on a host that actually implements native reads.
2. Map each phase or genuine subdivision to a planned slice. For an unsplit legacy phase, use one inline default. A legacy checklist row supplies a charter and source anchor, not an identity. Search existing TODO records with the supported query; where a match looks plausible, read the exact ID to verify workspace, owning plan, slice scope and prerequisites. Reuse a verified match instead of creating a duplicate. Ambiguous or conflicting matches require resolution, not a guess.
3. In a separate migration ledger, propose one ordinary TODO per independently tracked slice that still lacks a binding. Record workspace, umbrella TODO ID, phase ID, source revision and anchor, exact target slice location, proposed TODO ID, title, section, priority, charter, canonical FR/TR links, existing verified prerequisites, and pending artifact edges. Allocate a stable, collision-checked uppercase ID with a three-digit suffix, such as `SLICE-WEBREQS-001`; preserve the assigned proposal on reruns. Never use `ISSUE-NEW` or a guessed issue ID. Do not treat the umbrella as a prerequisite merely to convey ownership.
4. Review the full proposal before writing. Cross-check prereqs for cycles and preserve genuine dependency edges rather than serializing otherwise independent phases. With no authorization for live TODO writes, stop here. Keep the YAML readable with unresolved `sliceRef` fields absent, and preserve deferred phase references and typed artifact edges in the ledger. A blocked YAML draft may fail root schema validation; report the exact missing paths and do not substitute sample IDs, null identities, or copied completion flags to turn it green.

## 2. Apply only when authorized

Use the installed host's documented TODO interface: the REPL tools `mcp_todo_query`, `mcp_todo_get`, and `mcp_todo_create`, or the trusted workspace's initialized `Get-McpTodo` and `New-McpTodo` helpers. Use the actual installed parameter schema; do not call raw REST, write YAML projections, or fall back to another workspace. If neither documented create route is available, report the pending records and stop.

Provision only the approved plan bindings, in prerequisite order. Re-read the exact ID and compare ownership and scope before each create; only a positively classified not-found permits a create. Authentication errors, an unavailable tool, a partial list, or a missing field in a summary are not not-found. If a proposed ID is occupied by a different record, record the collision rather than overwriting it or silently allocating a new one.

Use ordinary new TODOs with no completed status. Preserve ownership in the description, for example `QBrain plan binding: PLAN-WEBREQS-001 | PHASE-WEBREQS-001 | inline-default`, plus the verified source anchor and work boundary. Keep checks or legacy checklist content only as scoped notes or unchecked implementation tasks; never copy checked rows as proof of completion. Use the exact FR/TR IDs and include `dependsOn` only for approved, verified prerequisites. If a matching TODO already exists, leave its content and completion state unchanged unless a separate authorized reconciliation calls for an edit.

PowerShell example for one authorized binding after preflight (illustrative IDs and FK values, not values to copy):

```powershell
New-McpTodo -Id 'SLICE-WEBREQS-001' -Title 'Define the requirements catalog contract' `
  -Section 'Web Requirements' -Priority 'medium' `
  -Description @('QBrain plan binding: PLAN-WEBREQS-001 | PHASE-WEBREQS-001 | inline-default',
    'Source: <verified revision and anchor>', '<verified slice charter>')
Get-McpTodo -Id 'SLICE-WEBREQS-001'
```

Add `-FunctionalRequirements`, `-TechnicalRequirements` or `-DependsOn` using the verified values when applicable. On REPL, use the same fields via the installed `mcp_todo_create` input schema. If create times out or the outcome is uncertain, read the exact ID before any retry; continue only after authoritative readback and matching ownership. If it remains inconclusive, stop dependent writes and preserve verified results for safe resumption. Do not roll back by deleting records.

## 3. Bind and verify

Record the exact returned TODO ID and durable readback, including source binding, FR/TR references and actual prerequisites. Then populate `sliceRef: { kind: todo, todoId: "<returned ID>" }` in the v0.8 slice, using the same typed identity in explicit phase lists and all applicable artifact edges and receipt scopes. Do not rewrite historical receipts or mark anything complete. Re-parse and run the companion schema plus workspace, gate and graph checks. Report independently: identities bound, structural validity, and remaining readiness blockers such as design or storyboard evidence. A schema-valid TODO-backed plan does not bypass approval or execute work.
