---
name: edit-file-usage
description: Use when a coding agent needs to change part of an existing repository file through the QBAgent edit_file tool, applying a targeted unique-string replacement instead of rewriting the whole file with write_file.
license: MIT
---

# edit_file Usage

The QBAgent `edit_file` tool performs a surgical, in-place change to a single file by
replacing an exact `oldString` with a `newString`. Edits are not raw filesystem writes:
they route through the MCP Server `RepoFileService` (interface `IRepoFileService`,
`src/McpServer.Services/Services/RepoFileService.cs`) and are wrapped by
`TransactionGatedRepoFileService` (`src/McpServer.Support.Mcp/Services/TransactionGatedRepoFileService.cs`),
which enforces the path allowlist (FR-SUPPORT-010, TR-PLANNED-013) and transactional
rollback compensation via `IRepoFileCompensation` (TR-MCP-TXN-001).

## When to Use

- Changing a known region of an existing file: one function, one config key, a few lines.
- Renaming or retargeting a symbol where you can pin a unique surrounding context.
- Any edit where most of the file should stay byte-for-byte unchanged.

## When Not to Use

- Creating a brand new file, or replacing essentially all of a file's content: use
  `write_file` (backed by `RepoFileService.WriteAsync`) instead.
- The target path is outside the workspace allowlist: the write will be rejected.
- You cannot read the file first to obtain an exact `oldString`. Never guess at content.

## Inputs

- `filePath` (or `relativePath`): path to the existing file, relative to the repo root.
- `oldString`: exact text to find, including original indentation and line breaks.
- `newString`: replacement text. Must differ from `oldString`.
- `replaceAll` (optional, default false): replace every occurrence instead of one.
- `expectedOccurrences` (optional): assert the number of matches before applying; the
  edit fails if the actual count differs. Use this as a guard when `replaceAll` is true.

## Critical Rules

- Read the file first. `oldString` must match the on-disk bytes exactly, including
  leading whitespace. Strip any line-number or `cat -n` prefixes before matching.
- `oldString` must be unique within the file unless `replaceAll` is set. A non-unique
  `oldString` without `replaceAll` is an error; widen the surrounding context to a unique
  span rather than forcing it.
- `oldString` and `newString` must differ. A no-op edit is an error.
- Prefer `edit_file` over `write_file` for partial changes. Rewriting a whole file
  discards unrelated content, defeats the diff, and risks clobbering concurrent edits.
- Edits are transactional. On a failed turn transaction, `IRepoFileCompensation` restores
  the prior file state, so a rejected edit leaves the file unchanged. Do not add your own
  manual rollback logic.
- Do not chase path-safety or rollback yourself: the server owns both. Treat a rejected
  edit as a signal to fix the path or the `oldString`, not to retry blindly.

## Workflow

1. Read the target file (or the relevant slice) to capture exact current content.
2. Choose the smallest `oldString` that is still unique, including enough surrounding
   context (adjacent lines, indentation) to disambiguate.
3. Construct `newString` with identical indentation style and trailing whitespace rules.
4. If the same change must apply to many identical spans, set `replaceAll: true` and pin
   `expectedOccurrences` to the count you verified in step 1.
5. Call `edit_file` with `filePath`, `oldString`, `newString`, and any flags.
6. Inspect the result. A non-success return means the path was disallowed, the match was
   absent, ambiguous, or the count mismatched. Re-read and refine; do not retry unchanged.
7. For multiple edits to the same file, apply them as separate targeted `edit_file` calls,
   re-reading if earlier edits shifted later context.

## Validation Checklist

- File already exists and its path is inside the workspace allowlist.
- `oldString` copied from a fresh read, indentation preserved, no line-number prefix.
- `oldString` is unique, or `replaceAll` plus a verified `expectedOccurrences` is set.
- `newString` differs from `oldString` and keeps surrounding style consistent.
- For a full rewrite or new file, you switched to `write_file` instead.

## Common Pitfalls

- Including the `cat -n` line-number/tab prefix in `oldString`: it will never match.
- Trailing-whitespace or tab-versus-space mismatches that make an exact string fail.
- Picking an `oldString` that appears more than once and omitting `replaceAll`.
- Using `write_file` to tweak a few lines, silently dropping the rest of the file.
- Re-running an identical failed edit instead of re-reading and adjusting context.
- Assuming a rejected edit left a partial write: the transactional rollback guarantees the
  file is untouched, so diagnose and correct rather than clean up.
