# TR-GRAPHRAG-ADHOC

[Back to Technical Requirements](Technical-Requirements)

## TR-GRAPHRAG-ADHOC-001

**Ad-hoc text ingestion pipeline** — GraphRagService.IngestTextAsync shall accept raw text, chunk via Chunker (512 tokens), generate embeddings via IEmbeddingService (all-MiniLM-L6-v2, 384-dim), persist ContextDocumentEntity and ContextChunkEntity rows with workspace scoping, register vectors in IVectorIndexService, and optionally trigger IndexAsync. Document ID format: "adhoc-{Guid:N}". Content hash: SHA256.
**Covered by:** FR: FR-MCP-078
**Status:** pending
Scope: layer-1+

## TR-GRAPHRAG-ADHOC-002

**Explicit graph entity and relationship storage** — New EF Core entities GraphEntityEntity (Id, WorkspaceId, Name, EntityType, Description, Metadata JSON, timestamps) and GraphRelationshipEntity (Id, WorkspaceId, SourceEntityId FK, TargetEntityId FK, RelationshipType, Description, Weight default 1.0, Metadata JSON, timestamps) with workspace query filters, cascade delete from entity to relationships, and indexes on Name, EntityType, SourceEntityId, TargetEntityId, RelationshipType.
**Covered by:** FR: FR-MCP-079
**Status:** pending
Scope: layer-1+

## TR-GRAPHRAG-ADHOC-003

**Document lifecycle with cascade delete and vector cleanup** — DeleteDocumentAsync shall query chunk IDs for the document, call IVectorIndexService.RemoveVector for each chunk, then delete the ContextDocumentEntity (EF cascade removes chunks). RemoveVector removes the chunk from internal HNSW dictionaries making the node unreachable; full rebuild reclaims space. ListDocumentsAsync shall return paginated results with ChunkCount and TotalTokens computed via subquery.
**Covered by:** FR: FR-MCP-080
**Status:** pending
Scope: layer-1+

