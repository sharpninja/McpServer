# TEST-GRAPHRAG-ADHOC

[Back to Testing Requirements](Testing-Requirements)

## TEST-GRAPHRAG-ADHOC

### TEST-GRAPHRAG-ADHOC-001

GraphEntityEntity/GraphRelationshipEntity persist with all fields, workspace isolation, cascade delete, FK validation, and RemoveVector correctness.


### TEST-GRAPHRAG-ADHOC-002

IngestTextAsync creates document + chunks, generates embeddings, registers vectors, handles empty content, defaults SourceType/SourceKey, and optionally triggers reindex.


### TEST-GRAPHRAG-ADHOC-003

ListDocumentsAsync pagination and filtering, GetDocumentChunksAsync ordering, DeleteDocumentAsync cascade and vector cleanup.


### TEST-GRAPHRAG-ADHOC-004

Create/Get/Update/List/Delete for entities and relationships with ID generation, timestamp management, FK validation, cascade behavior.


### TEST-GRAPHRAG-ADHOC-005

All CQRS command and query handlers delegate to IGraphRagService and wrap results in Result<T>.


### TEST-GRAPHRAG-ADHOC-006

All 14 controller actions return correct HTTP status codes, content types, and error responses.


### TEST-GRAPHRAG-ADHOC-007

MCP tools serialize correctly, REPL workflow delegates to ContextClient, McpAgent tool adapter exposes all 14 tools.
