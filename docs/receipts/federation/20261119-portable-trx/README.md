# Current Phase0 TRX byte-preservation receipt

This evidence-only packet retains exact original TRX bytes for the current Task003, Task005 and Task007 mock packets. It does not add test executions, native proof, B-output acceptance, task readiness, Success or HV. Existing source and checked-out evidence files remain untouched.

The three ZIP archives contain only the nine explicitly listed TRX originals. Each original was first hashed against its existing packet's raw SHA256, then each decompressed archive entry was independently hashed back to that same value. `archive-manifest.json` records complete paths, lengths, original hashes, archive/entry hashes, decoded LF text hashes, current checked-out file hashes and original provenance. It also pins the original evidence manifests and source commits. The original operational helper remains outside tracking at the path/hash recorded in the manifest.

Existing Git attributes apply `text:auto` and `eol:lf` to TRX. A checked-out text copy can therefore have a different raw hash while preserving its decoded LF-normalized text. This packet distinguishes those facts explicitly. UTF8 decoding consumes a BOM and CRLF-to-LF normalization is used solely for text comparison; neither changes the bytes stored in the archives or replaces raw verification. Hash verification must use the ZIP entry bytes for the original raw-hash claims. No repository attribute policy was changed.

Task003 includes its paired Windows/Linux complete-definition run. Task005 includes three Windows runs and the paired Linux run. Task007 includes its existing Windows definition run and both retained operations runs; this packet makes no claim that the later SQL-port repair or its new paired Linux operation run has occurred. Those runs, if later executed, need their own receipt. Counts are retained per original run and must not be summed as independent feature or native acceptance.

No builds or tests were run, no source/assertion/configuration files changed, no files were moved/deleted, and no task state was changed for this preservation work. All originals remain retained.
