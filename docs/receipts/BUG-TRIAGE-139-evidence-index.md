# BUG-TRIAGE-139 append-only evidence index

This index is the correction and supersession surface for BUG-TRIAGE-139. Published historical
receipts are immutable evidence: corrections are added here or in a later receipt, never inserted
into an earlier receipt.

## Immutable historical receipt ledger

- Ninth receipt: source commit `a331557b30836e3570be1e112b8460fc58100b8a`; SHA-256 `BB920C424C2890F103C31D44B50E4E5BC510DF238F5FA082277D664AE0FD5E90`.
- Tenth receipt: source commit `5011cacca62b483ff4b93a0ae51e0c67ae182d66`; SHA-256 `3C16AFBA48C8DD3C416C35A72131720CA718FDC2A8A1BB0E0B0713CAA4CFA73A`.
- Eleventh receipt: approved-base commit `210cb223d81dac6b4045b868e4d7b2712d08b752`; SHA-256 `5B9F17C95CBD6EF471F8DC8B9D3F59CD587DE481E452A0647CAEB08F781BF1AC`.
- Twelfth receipt: source commit `799c8ac0870d91b40f32e11a2c6531f65e6422f9`; SHA-256 `3F40F3195168574A52A27030902B22F62917683ED6BB8E80415AF4B4D0622628`.
- Fourteenth receipt: reviewed candidate `f7d8a07f9a754321a5523d749bf5e713a74ab9f6`; SHA-256 `3B09DF63D5860DEC2A96E7589CB528586CBB6726130E1BFB5C3E356184FF5C33`.
- Fifteenth receipt: reviewed candidate `f410963f94d75064197ff576de3ee163a5f10f90`; SHA-256 `04BA22D37B5F29042A45B6D9A55FCE11EE23F4C33148B19BCDCE20052000F91C`.
- Sixteenth receipt: rejected candidate `3ce5a81402b46a1e106f6deff63375e073030bca`; SHA-256 `CB3D04D459ED657F7DB579F712C72552638233C9695888E2FC206C12FA828DEB`.

## Supersession records

- The ninth receipt's temporary ninth-review path was not durable. No repository-owned copy or hash
  of that missing narrative is claimed.
- The tenth receipt's statement that normal and ignore-EOL numstat were materially identical was
  disproved by the eleventh review. Later code normalized the three identified mixed-terminator
  files, but the original tenth receipt remains unchanged.
- The eleventh receipt's broad editable-text description remains historical text. Current audit code
  enumerates the exact editable patterns it enforces.
- The twelfth candidate was not approved. Its resolution statements are historical claims, not
  current proof.
- The fourteenth candidate `f7d8a07f9a754321a5523d749bf5e713a74ab9f6` was not approved. The
  independent review measured 13/20 for the external scope and 8/18 for Agent Help under its stated
  deterministic environment.
- The fifteenth candidate `f410963f94d75064197ff576de3ee163a5f10f90` was not approved. Its
  resolution statements are historical claims, not current proof.
- The sixteenth candidate `3ce5a81402b46a1e106f6deff63375e073030bca` was not approved. The exact
  independent review is
  [eighteenth-input-independent-review-3ce5a814.txt](artifacts/BUG-TRIAGE-139/reviews/eighteenth-input-independent-review-3ce5a814.txt),
  SHA-256 `B39E4AF54974655D31F31A16B461390017C39350F618FB17DB74AB92BFD2F842`.

## Canonical ownership receipts

Read-only supported plugin calls on 2026-09-05 established:

- Worktree `F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-review` has no local
  BUG-TRIAGE-139 TODO: `workflow.todo.query` request
  `req-20260905T105643Z-aff5`, totalCount `0`.
- Registered workspace `F:\GitHub\McpServer` canonically owns TODO `BUG-TRIAGE-139`:
  `workflow.todo.get` request `req-20260905T105727Z-bef9`, `done: false`.
- Registered workspace `F:\GitHub\McpServer` owns triage group
  `triage-group-0c1b4fed35c7c183`: `workflow.triage.getGroup` request
  `req-20260905T105810Z-20b4`, status `completed`, reportCount `1`, createdTodoId
  `BUG-TRIAGE-139`. The original report workspace is `F:\GitHub\NinjaTrader`; routing to the
  registered McpServer workspace is the canonical triage policy.

No worktree-local TODO or triage row is fabricated, and this remediation does not mark the
canonical TODO done.

## Current remediation pointer

The current work is recorded in `BUG-TRIAGE-139-eighteenth-remediation.md`. It remains pending a
real non-root Linux/FUSE/FIFO gate and a new independent review. The exact-final external ledger path
is fixed as
`C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-eighteenth\final-verification\exact-final-head.yaml`.
The eighteenth receipt may cite that path only after the file exists and its recorded final
commit/tree values have been replayed.

## Twentieth durable-evidence correction

- The prior eighteenth exact-final ledger is now repository-owned byte-for-byte at
  `artifacts/BUG-TRIAGE-139/eighteenth-exact-final/2EDE51B29371442419C2B74D905402240DB9E528DE7926261EEB473B4BCE9F03.yaml`.
  Its filename is its SHA-256 digest; the twentieth evidence test verifies both the bytes and a
  Git blob reachable from HEAD after the evidence commit.
- `BUG-TRIAGE-139-twentieth-evidence.md` is the append-only declaration of that provenance. It
  deliberately does not assert a checksum for itself or for the twentieth C: review report. The
  historical receipts and external eighteenth ledger are not rewritten.
