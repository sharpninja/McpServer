using System.Net.Http;
using Nuke.Common;
using Serilog;

partial class Build
{
    [Parameter("MCP server base URL for smoke tests")]
    readonly string BaseUrl = "http://localhost:7147";

    [Parameter("MCP server API key for smoke tests")]
    readonly string ApiKey;

    [Parameter("Workspace path for GraphRAG smoke test")]
    readonly string WorkspacePath;

    [Parameter("GraphRAG query for smoke test")]
    readonly string GraphRagQuery = "authentication flow";

    /// <summary>GraphRAG smoke test: status → index → query endpoints.</summary>
    public Target TestGraphRagSmoke => _ => _
        .DependsOn(Compile)
        .Requires(() => ApiKey)
        .Executes(async () =>
        {
            using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

            // Step 1: Status
            Log.Information("Step 1: Checking GraphRAG status...");
            var statusResponse = await http.GetAsync("/mcpserver/graphrag/status");
            statusResponse.EnsureSuccessStatusCode();
            var statusBody = await statusResponse.Content.ReadAsStringAsync();
            Log.Information("Status: {Body}", statusBody);

            // Step 2: Index
            Log.Information("Step 2: Triggering GraphRAG index...");
            var indexUri = string.IsNullOrWhiteSpace(WorkspacePath)
                ? "/mcpserver/graphrag/index"
                : $"/mcpserver/graphrag/index?workspacePath={Uri.EscapeDataString(WorkspacePath)}";
            var indexResponse = await http.PostAsync(indexUri, null);
            indexResponse.EnsureSuccessStatusCode();
            var indexBody = await indexResponse.Content.ReadAsStringAsync();
            Log.Information("Index: {Body}", indexBody);

            // Step 3: Query
            Log.Information("Step 3: Querying GraphRAG...");
            var queryUri = $"/mcpserver/graphrag/query?q={Uri.EscapeDataString(GraphRagQuery)}";
            var queryResponse = await http.GetAsync(queryUri);
            queryResponse.EnsureSuccessStatusCode();
            var queryBody = await queryResponse.Content.ReadAsStringAsync();
            Log.Information("Query: {Body}", queryBody);

            Log.Information("GraphRAG smoke test passed.");
        });
}
