// FR-MCP-REPL-003: Command Namespace Parity - GraphRAG command structures
// TR-MCP-REPL-001: YAML Envelope Protocol - GraphRAG command envelope data models
// TR-MCP-REPL-004: Command Registry and Dispatcher - GraphRAG command shapes
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - GraphRAG command namespace shapes
// FR-MCP-078: Ad-hoc text ingestion REPL commands
// FR-MCP-079: Entity and relationship CRUD REPL commands
// FR-MCP-080: Document management REPL commands

namespace McpServer.Repl.Core;

/// <summary>
/// Defines YAML command shapes for the <c>workflow.graphrag.*</c> namespace.
/// All commands follow the REPL protocol request envelope structure with method-specific parameters.
/// </summary>
/// <remarks>
/// <para>
/// Command methods in this namespace:
/// <list type="bullet">
/// <item><c>workflow.graphrag.status</c> — Get GraphRAG status</item>
/// <item><c>workflow.graphrag.index</c> — Trigger GraphRAG indexing</item>
/// <item><c>workflow.graphrag.query</c> — Run a GraphRAG query</item>
/// <item><c>workflow.graphrag.ingest</c> — Ingest raw text into corpus</item>
/// <item><c>workflow.graphrag.documents.list</c> — List documents in corpus</item>
/// <item><c>workflow.graphrag.documents.chunks</c> — Get chunks for a document</item>
/// <item><c>workflow.graphrag.documents.delete</c> — Delete a document</item>
/// <item><c>workflow.graphrag.entities.create</c> — Create a graph entity</item>
/// <item><c>workflow.graphrag.entities.list</c> — List graph entities</item>
/// <item><c>workflow.graphrag.entities.get</c> — Get a graph entity by ID</item>
/// <item><c>workflow.graphrag.entities.update</c> — Update a graph entity</item>
/// <item><c>workflow.graphrag.entities.delete</c> — Delete a graph entity</item>
/// <item><c>workflow.graphrag.relationships.create</c> — Create a graph relationship</item>
/// <item><c>workflow.graphrag.relationships.list</c> — List graph relationships</item>
/// <item><c>workflow.graphrag.relationships.get</c> — Get a graph relationship by ID</item>
/// <item><c>workflow.graphrag.relationships.update</c> — Update a graph relationship</item>
/// <item><c>workflow.graphrag.relationships.delete</c> — Delete a graph relationship</item>
/// </list>
/// </para>
/// </remarks>
public static class GraphRagCommandShapes
{
    /// <summary>
    /// The namespace prefix for all GraphRAG workflow commands.
    /// </summary>
    public const string MethodNamespace = "workflow.graphrag";

    /// <summary>
    /// Command method for getting GraphRAG status.
    /// Method: <c>workflow.graphrag.status</c>
    /// </summary>
    public const string StatusMethod = "workflow.graphrag.status";

    /// <summary>
    /// Command method for triggering GraphRAG indexing.
    /// Method: <c>workflow.graphrag.index</c>
    /// </summary>
    public const string IndexMethod = "workflow.graphrag.index";

    /// <summary>
    /// Command method for running a GraphRAG query.
    /// Method: <c>workflow.graphrag.query</c>
    /// </summary>
    public const string QueryMethod = "workflow.graphrag.query";

    /// <summary>
    /// Command method for ingesting raw text into the corpus.
    /// Method: <c>workflow.graphrag.ingest</c>
    /// </summary>
    public const string IngestMethod = "workflow.graphrag.ingest";

    /// <summary>
    /// Command method for listing documents in the corpus.
    /// Method: <c>workflow.graphrag.documents.list</c>
    /// </summary>
    public const string DocumentsListMethod = "workflow.graphrag.documents.list";

    /// <summary>
    /// Command method for retrieving chunks of a specific document.
    /// Method: <c>workflow.graphrag.documents.chunks</c>
    /// </summary>
    public const string DocumentsChunksMethod = "workflow.graphrag.documents.chunks";

    /// <summary>
    /// Command method for deleting a document from the corpus.
    /// Method: <c>workflow.graphrag.documents.delete</c>
    /// </summary>
    public const string DocumentsDeleteMethod = "workflow.graphrag.documents.delete";

    /// <summary>
    /// Command method for creating a graph entity.
    /// Method: <c>workflow.graphrag.entities.create</c>
    /// </summary>
    public const string EntitiesCreateMethod = "workflow.graphrag.entities.create";

    /// <summary>
    /// Command method for listing graph entities.
    /// Method: <c>workflow.graphrag.entities.list</c>
    /// </summary>
    public const string EntitiesListMethod = "workflow.graphrag.entities.list";

    /// <summary>
    /// Command method for retrieving a graph entity by ID.
    /// Method: <c>workflow.graphrag.entities.get</c>
    /// </summary>
    public const string EntitiesGetMethod = "workflow.graphrag.entities.get";

    /// <summary>
    /// Command method for updating a graph entity.
    /// Method: <c>workflow.graphrag.entities.update</c>
    /// </summary>
    public const string EntitiesUpdateMethod = "workflow.graphrag.entities.update";

    /// <summary>
    /// Command method for deleting a graph entity.
    /// Method: <c>workflow.graphrag.entities.delete</c>
    /// </summary>
    public const string EntitiesDeleteMethod = "workflow.graphrag.entities.delete";

    /// <summary>
    /// Command method for creating a graph relationship.
    /// Method: <c>workflow.graphrag.relationships.create</c>
    /// </summary>
    public const string RelationshipsCreateMethod = "workflow.graphrag.relationships.create";

    /// <summary>
    /// Command method for listing graph relationships.
    /// Method: <c>workflow.graphrag.relationships.list</c>
    /// </summary>
    public const string RelationshipsListMethod = "workflow.graphrag.relationships.list";

    /// <summary>
    /// Command method for retrieving a graph relationship by ID.
    /// Method: <c>workflow.graphrag.relationships.get</c>
    /// </summary>
    public const string RelationshipsGetMethod = "workflow.graphrag.relationships.get";

    /// <summary>
    /// Command method for updating a graph relationship.
    /// Method: <c>workflow.graphrag.relationships.update</c>
    /// </summary>
    public const string RelationshipsUpdateMethod = "workflow.graphrag.relationships.update";

    /// <summary>
    /// Command method for deleting a graph relationship.
    /// Method: <c>workflow.graphrag.relationships.delete</c>
    /// </summary>
    public const string RelationshipsDeleteMethod = "workflow.graphrag.relationships.delete";
}
