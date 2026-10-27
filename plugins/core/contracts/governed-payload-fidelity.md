<!-- SPDX-License-Identifier: Apache-2.0 -->
# S6 payload fidelity assertion seam

FR-QB-003/005, TR-QB-AUDIT-001, AC/TEST-QB-012/069 and canonical
C10/D12 require lossless lexical review scores and decoded text normalization.
This assertion increment covers that existing prerequisite only. It grants no
review, turn, lifecycle, transport, phase completion or acceptance.

`GovernedPayloadFidelity.Contract.ps1` supplies native JSON writer inputs through
one `RoundTrip` dependency operation in Mock and Production. The independent
model parses, normalizes decoded string values, writes JSON preserving raw
numeric tokens, then reparses the published bytes. The common runner derives
observations from those bytes. The production provider must bind the actual
supported REPL/client/cache/outbox path; no production mock fallback exists.

U+2013/U+2014 decoded string values become U+002D before capture/hash. All other
Unicode, Markdown, CR/LF, leading/trailing whitespace and literal backslash-u
text remain unchanged. Both ordinary escaped JSON and literal Unicode inputs
exercise the same assertions. Property/root identities are retained.

Score lexemes survive before numeric coercion: 19-digit and 28-digit inputs and
trailing zeros remain exact. Signed/exponent spellings and non-number JSON
values cannot satisfy the existing >=98 threshold. The 28-digit value just
below98 must remain below98; rounding to binary floating point is insufficient.
No new score upper bound is introduced by this test increment.

Deliberate dependency effects round numeric storage, drop trailing zeros, retain
decoded dashes, rewrite literal escapes, trim content, remove CR, change unrelated
Unicode, remove a field or retarget a root. These are rejected through the SAME
scenario assertions and published-byte inspection, never case-name summary
objects. Preserve every prior receipt and the current original C7/outbox tests.
