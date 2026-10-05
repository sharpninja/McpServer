# Hostile validation prompt — QBrain.AI rebrand plan (Phase 0)

Stamp: 20261005T154611Z
Required reviewer: Codex
Required model: gpt-6-astra
Required reasoning effort: xhigh
Operator shorthand: astra-6-xhigh
Host required for the live run: PAYTON-LEGION2
Subject: `docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md`
Cloud-recorded SHA-256 of that file before this prompt was added: `5ba31fc11d32ba9ce7c0e19f99a30136f4c85d14de706b352d4f12e46431ccaf`
This prompt is the claim list. It is not a verdict.

You are the hostile reviewer. Read the plan file in the workspace you were given. Do not rename namespaces, packages, or repositories. Do not mark Phase 1 approved. Do not mark the plan done. Do not edit the plan. Write your verdict into a new receipt next to this file, and keep the Codex request and response jsonl. Prove model and effort from your own rollout `turn_context`: both `payload.model` / `payload.effort` and `collaboration_mode.settings` must show `gpt-6-astra` and `xhigh`. If they do not, OverallVerdict is DISAGREE.

## Claims

Score each claim PASS, FAIL, or UNKNOWN. UNKNOWN is not a pass. A FAIL forces OverallVerdict DISAGREE. Do not emit AGREE to be helpful.

- C0. The plan status line is still Draft pending approval, and the plan says Phase 1 is not approved.
- C1. Phases 0, 1, 2, 3, 4, 4-L, and 5 each have their own hostile-validation checkpoint, and each checkpoint names model `gpt-6-astra` and effort `xhigh`.
- C2. Operator shorthand `astra-6-xhigh` is defined as that model and effort. The plan says a different model cannot mark a phase done.
- C3. A phase acceptance list is not enough to mark the phase done. Done requires OverallVerdict AGREE on a receipt from that model and effort.
- C4. Branding and product identity are FR-MCP-QBRAIN-001 through FR-MCP-QBRAIN-005. Those records do not themselves rename a namespace, package, or repository.
- C5. Renames and migration mechanics are TR-MCP-QBRAIN-001 through TR-MCP-QBRAIN-008. The trace map names which TRs implement which FRs.
- C6. The locked tokens remain: display `QBrain.AI`, PascalCase `QBrainAi`, lowercase single token `qbrainai`, kebab `qbrain-ai`, NuGet prefix `SharpNinja.QBrainAi.`.
- C7. The plan leaves `/mcp-transport`, Model Context Protocol wording, QuadBrain, the `qbagent` command, the `QBAgent` segment, existing requirement IDs, `mcp.db`, `.mcpServer`, `McpDbContext`, `PAYTON-LEGION2`, and `LAB-OMARCHY` in place for the reasons the plan states.
- C8. Aliases for routes, config, environment variables, and old package ids last through 1.x and stop at 2.0.
- C9. The cloud receipt `docs/receipts/hv/20261005T154611Z-qbrain-ai-rebrand-plan.receipt.md` is a NOT RUN blocker. It is not an AGREE. Do not treat it as this review.

## Out of scope for this review

Do not fail the plan because Phase 1 code has not been renamed. That work is not approved. Do not fail it because this prompt was not executed on Legion before the cloud receipt existed. Score whether the plan text matches the claims.

## Receipt template

Copy this shape into the verdict file. Replace the placeholders. Do not leave NOT RUN if you actually ran.

- TimestampUtc: `<utc>`
- ValidatorIdentity: Codex
- Model: gpt-6-astra
- Effort: xhigh
- Proof: `<rollout jsonl path and turn_context timestamp>`
- SubjectSha256: `<hash you computed>`
- OverallVerdict: AGREE or DISAGREE
- Accuracy: `<0-100>`
- Completeness: `<0-100>`
- PASS / FAIL / UNKNOWN counts
- One line per claim id with PASS, FAIL, or UNKNOWN and the plan heading you used
