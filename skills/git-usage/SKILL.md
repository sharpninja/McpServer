---
name: git-usage
description: Use this skill whenever an agent runs the QBAgent git tool in the McpServer workspace so version-control actions stay safe, push only to the Azure DevOps origin when explicitly asked, and commit messages keep the required Co-Authored-By trailer.
license: MIT
---

# Git Usage (QBAgent git tool)

Guidance for driving the QBAgent git tool safely inside the McpServer repository. The tool exposes a fixed set of subcommands. This skill defines which are read-only, which mutate the working tree, and which touch a remote, plus the project-specific source-control rules that override default git habits.

## When to Use

- You are about to inspect repository state (status, diff, log, branch).
- You are staging, committing, switching branches, or resetting work via the QBAgent git tool.
- You need to push committed work and want to do it correctly (origin only, opt-in).
- You are unsure whether an action is read-only or destructive.

## When Not to Use

- You only need MCP TODO or session-log operations: use the MCP Server plugin/workflow methods, not git.
- You are asked to operate on GitHub (PRs, issues, the `github` remote) without explicit, specific instruction to do so. Stop and confirm first.
- You want to bypass the QBAgent git tool with raw shell git for a destructive action. Prefer the tool's subcommands so the action is auditable.

## Inputs

- `subcommand`: one of `status`, `diff`, `log`, `branch`, `add`, `commit`, `checkout`, `push`, `reset`.
- Subcommand arguments, for example: paths for `add`, a message for `commit`, a branch name for `checkout`/`branch`, a ref or mode for `reset`, a remote/branch for `push`.
- The active workspace repository (the McpServer working tree). Use absolute paths when a path is required.

## Subcommand Reference

Read-only (safe to run any time to gather context):

- `status`: show the working-tree and staged state. Run this first before any mutation.
- `diff`: show unstaged or staged changes. Use to confirm exactly what will be committed.
- `log`: show recent commit history. Use to verify the base commit and recent messages.
- `branch`: list branches or show the current branch.

Working-tree mutations (local only, no network):

- `add`: stage specific paths. Prefer explicit paths over staging everything so unrelated changes are not swept in.
- `commit`: create a new commit from staged changes. Always supply a message ending with the Co-Authored-By trailer (see Critical Rules). Prefer a new commit over amending an existing one.
- `checkout`: switch branches or restore paths. Switching branches with uncommitted changes can move or block work; check `status` first. Restoring a path discards local edits, so confirm intent.
- `reset`: move HEAD or unstage. `reset` (mixed/soft) is recoverable; a hard reset discards uncommitted work and is destructive. Treat any hard reset as a last resort and confirm before running it.

Remote action (network, opt-in):

- `push`: publish commits to a remote. This is the only subcommand that contacts a remote and the only one gated behind explicit user intent.

## Critical Rules

1. Azure DevOps `origin` is the single source of truth. Every `push` targets `origin` only. GitHub is a downstream mirror that is synced on demand, never by you by default.
2. Never `push` to the `github` remote, and never invoke GitHub PR tooling, unless the user explicitly and specifically asks for it. When the user says "sync", "push", or "create PR" without naming a target, that means Azure DevOps `origin`.
3. `push` is opt-in. Do not push as a side effect of committing. Push only when the user asked to push, sync, or checkpoint. Commit locally otherwise and report that work is committed but unpushed.
4. Prefer a new commit over amending. Do not rewrite published history. Avoid amend and history-rewriting resets unless the user explicitly requests them.
5. Every commit message must end with this trailer as its final line:
   `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
6. Do not skip hooks or signing. Do not pass `--no-verify`, `--no-gpg-sign`, or equivalent bypasses unless the user explicitly asks. If a hook fails, fix the root cause instead of bypassing it.
7. Branch before committing on a protected branch. The default branch is `main`. If you are on `main` and the user has not said to commit directly to it, create or switch to a working branch first.
8. No em-dashes anywhere in commit messages, branch names, or any text this tool writes.
9. The McpServer tree may be edited by other agents concurrently. Re-run `status` (and re-check file modification times) immediately before staging or committing so you do not capture or clobber another agent's changes.

## Workflow

1. Inspect: run `status` to see what changed, then `diff` to read the exact edits. Run `log` and `branch` to confirm the current branch and base commit.
2. Confirm branch safety: if the current branch is `main` (the default branch) and the user did not authorize committing to it, use `checkout` to move to a working branch first.
3. Stage deliberately: run `add` with explicit paths for the files that belong in this change. Avoid staging unrelated or unexpected files surfaced by another concurrent agent.
4. Re-verify staged content: run `diff` on the staged set to make sure only intended changes are included.
5. Commit: run `commit` with a clear, present-tense message. End the message with the exact Co-Authored-By trailer from Critical Rules. Create a new commit; do not amend.
6. Decide on push: only if the user explicitly asked to push, sync, or checkpoint, run `push` to `origin`. Otherwise stop and report that the commit is local and unpushed.
7. Push correctly when authorized: `push` to `origin` (Azure DevOps). Never substitute the `github` remote. If the user named a different target, confirm it is what they want before pushing.
8. Report: state the branch, the new commit, which files were committed, and whether the work was pushed.

## Validation Checklist

- Did you run `status` and `diff` before staging and again before committing?
- Are only the intended paths staged, with no unrelated or foreign edits swept in?
- Is the current branch appropriate (not committing directly to `main` without authorization)?
- Does the commit message end with the exact `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` trailer?
- Is the commit a new commit (not an amend or a history rewrite)?
- If you pushed, did you push to `origin` only, and did the user explicitly ask for a push?
- Did you avoid the `github` remote and GitHub PR tooling unless explicitly requested?
- Are there any em-dashes in the message or branch name? There must be none.

## Common Pitfalls

- Pushing automatically after a commit. Push is a separate, opt-in step; committing never implies pushing.
- Pushing to `github` because a default remote or muscle memory points there. Always target `origin`.
- Treating "sync" or "create PR" as a GitHub action. Without an explicit target, these mean Azure DevOps `origin`.
- Forgetting the Co-Authored-By trailer, or placing it above other text instead of on the final line.
- Amending or hard-resetting to "tidy up" and silently dropping work. Prefer additive new commits; confirm before any destructive reset.
- Staging everything (`add` with no paths or a broad glob) and committing another concurrent agent's in-flight edits.
- Bypassing a failing hook with `--no-verify` instead of fixing the underlying problem.

## YAML Mutation Rule

When YAML must be changed, deserialize the complete document into an object, mutate the object, serialize the object, and save the result. Do not append YAML snippets, replace YAML lines, remove YAML lines, or build YAML payloads as strings. For PowerShell work, use `plugins/core/lib-ps/yaml-object-mutation.ps1` and call `Set-McpYamlObjectValue` or `Update-McpYamlObject`.
