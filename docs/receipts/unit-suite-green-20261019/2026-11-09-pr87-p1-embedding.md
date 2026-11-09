# PR87 P1 embedding slice receipt

Author: Codex. Execution window: 2026-11-09T09:59:29-06:00 through 2026-11-09T10:31:31-06:00.
Requirements: FR-MCP-004 `embedding-vector-contract`; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001.

## Source identity

The slice was uncommitted during its prescribed mock, binding, Compile and cross-host gates. Linux ran in the PR87
worktree whose recorded HEAD was `c8b231c13164956ac98d6234fb2c6fb459c0cefb`. Windows ran from detached code
baseline `03eea74797827aacf7030ac577cc19d46ebee5d2`. The exact six source files were hash-pinned before transfer and
reverified on Windows:

- `EmbeddingModelAssertionContract.cs`: `1E4441344C2EF0AF1DF1A605FC3A1E97FDCB582302A79E14772A75D2E392C268`
- `EmbeddingModelAssertionContractTests.cs`: `E2D08E6D483A605AA7235B71269984EC28335322890D58DC95872F2672E811CA`
- `QBrainAi.Support.Mcp.EnvironmentIntegrationTests.csproj`: `73042F104B706C1DFA50332A8697DC6726053E91CD9E701C37EA035289B97815`
- integration `EmbeddingServiceTests.cs`: `7D371EDC355EA485F0710D0FC77184DFDD6633B2C8B690F8C1E3575C0F3C2550`
- `EmbeddingModelFixture.cs`: `DBD2D81CD293A0A7FAF52638C36729476807C162202383D021AC8E4F7A56A8CE`
- `EmbeddingModelFixtureTests.cs`: `10433EBE3D782AE9DD6AFC1D1BB0FDB9E5368A870DB61C52E6A325746EB2E8EE`

## Chronology and outcomes

- Mock gate `ug-r15-embedding-mock-20261109T095929` ran before the real fixture or binding changed. The 12 theory
  rows passed with zero failures and zero not-executed: four positive responses, four independently wrong responses,
  and four unavailable responses. TRX SHA-256:
  `F1E679FA1309D3C4C414E2C012555FE5DF9CABE67F3CEC7D98EA9DDAB7F3E504`.
- First real binding `ug-r15-embedding-binding-20261109T1008` executed 6 integration cases: the two isolated
  missing/corrupt controls passed and the four original model cases failed at required fixture availability, with no
  skip or early-success return. Retained integration TRX SHA-256:
  `3BB476BB2B6F9DAC9CFFAA980F04195CB735DF76FFF5C66F5A36B7EB5519F05E`.
- Diagnostic binding `ug-r15-embedding-bindingdiag-20261109T1013` preserved the product load exception. It proved
  Microsoft.ML.OnnxRuntime 1.24.1 requested `onnxruntime.dll` while the portable Linux output contained
  `runtimes/linux-x64/native/libonnxruntime.so`. Counts remained 2 passed and 4 failed, with zero not-executed.
  Retained integration TRX SHA-256:
  `9F941CD6CD41911C19AB685D16A4079A5A3964723D467E1A28EA50B883B7000E`.
- A test-assembly module resolver was added so the corrupt-model control and positive fixture bind the packaged
  native asset before xUnit order can affect ONNX initialization. No product source or public API changed.
- Corrected Linux binding `ug-r15-embedding-bindingnative-20261109T1015` passed unit 19/19 and integration 6/6,
  with zero failed and zero not-executed. Unit TRX SHA-256:
  `2651FE7CA665309F6F1BEB94519718727A38B630B742AAE01EA99997D2FAA33D`. Integration TRX SHA-256:
  `80DF5A23D3441485CEE61E362EFA6E87E6F782615E9F57C3F17821A6B82FA9B3`.
- Nuke Compile `ug-r15-embedding-compile-20261109T1017` passed in 2:51 with zero warnings and zero errors.
  Retained log SHA-256: `21C29DD2AADA05F7AAE9FB161A7F4AAAF15BAC423C9BAB71B1B46599FF37B883`.
  Retained metadata SHA-256: `D5E854AD1199550155749B384F79888A42D397C9077081978B7A5F8B0D73AFA1`.
- Windows binding `ug-r15-embedding-binding-win-20261109T1021` ran on PAYTON-LEGION2 as `kingd` after a WhatIf
  and exact HEAD/file-hash checks. Nuke exited 0 and passed unit 19/19 plus integration 6/6, with zero failed and zero
  not-executed. Unit TRX SHA-256:
  `C9D11DBE88863CC8B99F79EE61B511C85D5CE3C8292B80ACDB5EB7CBCBB83872`. Integration TRX SHA-256:
  `33FA433D2796931D8AFF195FB8942E50FB5901E055943718FAE73C0E64AAE3D7`.

The first Windows SSH receipt command returned 1 only after the successful Nuke run because its post-run formatter
dereferenced absent unit-test stdout. It did not change the Nuke exit or either TRX. A corrected analyzed read-only
probe independently read back HEAD, all six source hashes, Nuke success, both TRX hashes/counts and model output;
its local receipt SHA-256 is `9B3F02DF1C9E60AE00208FB411F036E11799077A1B35CD0EA4930D26B075A300`.
No test rerun was substituted for the successful Windows gate.

## Model fixture evidence

Both hosts recorded the same artifacts in every original model-backed case:

- Model source: `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx`
- Model length: `90405214`
- Model SHA-256: `6FD5D72FE4589F189F8EBC006442DBB529BB7CE38F8082112682524616046452`
- Vocabulary source: `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt`
- Vocabulary length: `231508`
- Vocabulary SHA-256: `07ECED375CEC144D27C900241F3E339478DEC958F92FDDBC551F295C992038A3`

The asynchronous fixture streams both artifacts into one unique temporary directory, supplies the explicit model
path, disables product auto-download, requires `IsAvailable`, disposes the service and deletes only its owned root.
The missing/corrupt harness uses separate unique roots and confirms that `RequireAvailable` throws. The user's default
model/cache directory is neither read nor written by this fixture.

After copying both Windows TRX originals into this receipt tree, the six staged checkout changes and six temporary
upload copies were removed only after exact hash verification. Windows retained its TRX evidence and returned to its
pre-existing state: detached `03eea74797827aacf7030ac577cc19d46ebee5d2` with only `.nuke/build.schema.json`
modified.
