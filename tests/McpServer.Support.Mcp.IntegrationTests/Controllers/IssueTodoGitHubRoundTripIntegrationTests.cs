using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.IntegrationTests;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-095: Exercises the end-to-end ISSUE round-trip across the real HTTP controllers, the YAML TODO
/// store, and a fake GitHub backing service. The fixture starts with a create request that uses
/// <c>ISSUE-NEW</c>, then verifies GitHub-origin comments import into the TODO note without changing the
/// TODO description, verifies TODO-origin priority/comment changes flow back to GitHub, and finally verifies
/// that an externally closed GitHub issue syncs back as a closed TODO. Validates FR-MCP-071 and TR-MCP-GH-007.
/// </summary>
public sealed class IssueTodoGitHubRoundTripIntegrationTests
    : IClassFixture<IssueTodoGitHubRoundTripIntegrationTests.RoundTripWebFactory>, IDisposable
{
    private const string ExternalGitHubComment = "External GitHub follow-up from the issue thread.";
    private const string LocalTodoComment = "Local TODO follow-up comment from MCP.";

    private readonly HttpClient _client;
    private readonly RoundTripWebFactory _factory;

    /// <summary>
    /// Initializes the integration fixture using a temporary workspace-specific TODO file and a singleton fake
    /// GitHub service so each run can assert both MCP-side and GitHub-side state transitions deterministically.
    /// </summary>
    /// <param name="factory">Factory that hosts the real ASP.NET application with fake GitHub integration.</param>
    public IssueTodoGitHubRoundTripIntegrationTests(RoundTripWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    /// <summary>
    /// Disposes the test client created for the integration run. Workspace and fake GitHub cleanup are handled
    /// by the factory's isolated temporary directory and in-memory state.
    /// </summary>
    public void Dispose() => _client.Dispose();

    /// <summary>
    /// TEST-MCP-095: Verifies the full ISSUE create/sync/import/export/close lifecycle using a real HTTP host,
    /// a temporary YAML TODO store, and a fake GitHub implementation. The scenario uses a description-bearing
    /// <c>ISSUE-NEW</c> create request so the test can prove GitHub comment imports avoid description churn,
    /// then appends a TODO note comment so the resulting GitHub comment body can be asserted explicitly.
    /// Validates FR-MCP-071 and TR-MCP-GH-007.
    /// </summary>
    [Fact]
    public async Task IssueNew_RoundTripsGitHubCommentsPriorityAndClosure()
    {
        var createResponse = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/todo", UriKind.Relative),
            new
            {
                id = TodoCreationService.NewGitHubIssueTodoId,
                title = "Round-trip ISSUE integration test",
                section = "issues",
                priority = "low",
                description = new[] { "Original description that must remain unchanged." }
            }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createResult = await createResponse.Content.ReadFromJsonAsync<TodoMutationResultDto>().ConfigureAwait(true);
        Assert.NotNull(createResult);
        Assert.True(createResult.Success);
        Assert.NotNull(createResult.Item);
        Assert.StartsWith("ISSUE-", createResult.Item.Id, StringComparison.Ordinal);

        var todoId = createResult.Item.Id!;
        var issueNumber = int.Parse(todoId["ISSUE-".Length..], CultureInfo.InvariantCulture);

        await PostWithoutBodyAsync($"/mcpserver/gh/issues/{issueNumber}/sync?direction=to-github").ConfigureAwait(true);

        var initialIssue = await GetFromJsonAsync<GitHubIssueDto>($"/mcpserver/gh/issues/{issueNumber}").ConfigureAwait(true);
        Assert.Equal(issueNumber, initialIssue.Number);
        Assert.Equal("Round-trip ISSUE integration test", initialIssue.Title);
        Assert.Contains(initialIssue.Labels, label => string.Equals(label.Name, "priority: LOW", StringComparison.Ordinal));

        _factory.GitHub.AddExternalComment(issueNumber, "github-user", ExternalGitHubComment);

        await PostWithoutBodyAsync($"/mcpserver/gh/issues/{issueNumber}/sync?direction=from-github").ConfigureAwait(true);

        var afterGitHubImport = await GetFromJsonAsync<FlatTodoItemDto>($"/mcpserver/todo/{todoId}").ConfigureAwait(true);
        Assert.NotNull(afterGitHubImport.Description);
        Assert.Equal(["Original description that must remain unchanged."], afterGitHubImport.Description);
        Assert.NotNull(afterGitHubImport.Note);
        Assert.Contains(ExternalGitHubComment, afterGitHubImport.Note, StringComparison.Ordinal);
        Assert.Contains("<!-- BEGIN MCP GITHUB COMMENTS -->", afterGitHubImport.Note, StringComparison.Ordinal);

        var updatedNote = afterGitHubImport.Note + Environment.NewLine + Environment.NewLine + LocalTodoComment;
        var todoUpdateResponse = await _client.PutAsJsonAsync(
            new Uri($"/mcpserver/todo/{todoId}", UriKind.Relative),
            new
            {
                priority = "high",
                note = updatedNote
            }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, todoUpdateResponse.StatusCode);

        await PostWithoutBodyAsync($"/mcpserver/gh/issues/{issueNumber}/sync?direction=to-github").ConfigureAwait(true);

        var afterTodoExport = await GetFromJsonAsync<GitHubIssueDto>($"/mcpserver/gh/issues/{issueNumber}").ConfigureAwait(true);
        Assert.Contains(afterTodoExport.Labels, label => string.Equals(label.Name, "priority: HIGH", StringComparison.Ordinal));
        Assert.DoesNotContain(afterTodoExport.Labels, label => string.Equals(label.Name, "priority: LOW", StringComparison.Ordinal));
        Assert.Contains(afterTodoExport.Comments, comment => comment.Author == "mcp-server" && comment.Body.Contains(LocalTodoComment, StringComparison.Ordinal));

        _factory.GitHub.CloseExternally(issueNumber, "completed");

        await PostWithoutBodyAsync($"/mcpserver/gh/issues/{issueNumber}/sync?direction=from-github").ConfigureAwait(true);

        var afterGitHubClose = await GetFromJsonAsync<FlatTodoItemDto>($"/mcpserver/todo/{todoId}").ConfigureAwait(true);
        Assert.True(afterGitHubClose.Done);
        Assert.NotNull(afterGitHubClose.Note);
        Assert.Contains(LocalTodoComment, afterGitHubClose.Note, StringComparison.Ordinal);
    }

    private async Task PostWithoutBodyAsync(string relativeUri)
    {
        var response = await _client.PostAsync(new Uri(relativeUri, UriKind.Relative), content: null).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> GetFromJsonAsync<T>(string relativeUri)
    {
        var response = await _client.GetAsync(new Uri(relativeUri, UriKind.Relative)).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        var value = await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(true);
        return value ?? throw new InvalidOperationException($"Expected {typeof(T).Name} payload from '{relativeUri}'.");
    }

    private sealed record TodoMutationResultDto(bool Success, string? Error, FlatTodoItemDto? Item);

    private sealed record FlatTodoItemDto(
        string? Id,
        string? Title,
        string? Section,
        string? Priority,
        bool Done,
        string? Note,
        string[]? Description);

    private sealed record GitHubIssueDto(
        int Number,
        string Title,
        string? Body,
        string? State,
        string? Url,
        GitHubLabelDto[] Labels,
        string[] Assignees,
        string? Milestone,
        string? CreatedAt,
        string? UpdatedAt,
        string? ClosedAt,
        string? Author,
        GitHubCommentDto[] Comments);

    private sealed record GitHubLabelDto(string Name, string? Color, string? Description);

    private sealed record GitHubCommentDto(string? Author, string Body, string? CreatedAt);

    /// <summary>
    /// Hosts the real MCP API against an isolated temporary workspace and replaces the production GitHub CLI
    /// wrapper with an in-memory fake so the integration test can simulate external GitHub comments and issue
    /// closure without talking to the network. This keeps controller, service, and YAML persistence behavior real
    /// while making GitHub state deterministic for TEST-MCP-095.
    /// </summary>
    public sealed class RoundTripWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(
            Path.GetTempPath(),
            "mcp-issue-roundtrip-tests-" + Guid.NewGuid().ToString("N")[..8]);

        /// <summary>Gets the singleton fake GitHub service registered for the hosted application.</summary>
        internal FakeGitHubCliService GitHub => Services.GetRequiredService<FakeGitHubCliService>();

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            var databasePath = Path.Combine(_tempDir, "mcp.db");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "TODO.yaml"), SeedYaml);

            builder.UseEnvironment("Test");
            builder.UseContentRoot(CustomWebApplicationFactory.ResolveContentRoot());
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataFolder"] = _tempDir,
                    ["Mcp:DataSource"] = databasePath,
                    ["Mcp:Database:Provider"] = "sqlite",
                    ["Mcp:Database:Sqlite:DataSource"] = databasePath,
                    ["Mcp:UseInMemoryDatabaseForTests"] = "false",
                    ["Mcp:RepoRoot"] = _tempDir,
                    ["Mcp:TodoFilePath"] = "docs/Project/TODO.yaml",
                    ["Mcp:TodoStorage:Provider"] = "sqlite",
                    ["Mcp:TodoStorage:SqliteDataSource"] = "mcp.db",
                    ["Mcp:Workspaces:0:WorkspacePath"] = _tempDir,
                    ["Mcp:Workspaces:0:Name"] = Path.GetFileName(_tempDir),
                    ["Mcp:Workspaces:0:TodoPath"] = "docs/Project/TODO.yaml",
                    ["Mcp:Workspaces:0:IsPrimary"] = "true",
                    ["Mcp:Workspaces:0:IsEnabled"] = "true"
                });
            });
            builder.ConfigureServices(services =>
            {
                ConfigureTestDatabase(services, databasePath);
                services.RemoveAll<IWorkspaceProjectionWriter>();
                services.AddSingleton<IWorkspaceProjectionWriter, NoOpWorkspaceProjectionWriter>();
                services.RemoveAll<IGitHubCliService>();
                services.AddSingleton<FakeGitHubCliService>();
                services.AddSingleton<IGitHubCliService>(provider => provider.GetRequiredService<FakeGitHubCliService>());
                services.AddHostedService<TestDatabaseInitializer>();
            });
        }

        private static void ConfigureTestDatabase(IServiceCollection services, string databasePath)
        {
            var connectionString = $"Data Source={databasePath}";
            var providerOptions = McpDatabaseProviderFactory.CreateOptions("sqlite", connectionString);

            services.RemoveAll<McpDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<McpDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<McpDbContext>>();
            services.RemoveAll<McpDatabaseProviderOptions>();
            services.RemoveAll<McpDatabaseRuntimeOptions>();
            services.AddSingleton(providerOptions);
            services.AddSingleton(new McpDatabaseRuntimeOptions(
                providerOptions,
                new McpDatabaseEncryptionOptions(
                    enabled: false,
                    sqliteKey: null,
                    sqliteSeeToolPath: null,
                    postgreSqlKeyProvider: null,
                    postgreSqlPrincipalKey: null,
                    sqlServerCertificateName: null,
                    sqlServerDatabaseEncryptionKeyName: null)));
            services.AddDbContext<McpDbContext>(options =>
            {
                McpDatabaseProviderFactory.Configure(options, providerOptions);
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Disposes the hosted application and removes the temporary workspace tree used for this test run.
        /// </summary>
        private new void Dispose()
        {
            base.Dispose();
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private const string SeedYaml = """
            issues:
              high-priority: []
              medium-priority: []
              low-priority: []
            """;

        private sealed class TestDatabaseInitializer : IHostedService
        {
            private readonly IServiceProvider _services;

            public TestDatabaseInitializer(IServiceProvider services)
            {
                _services = services;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
                await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }

    internal sealed class FakeGitHubCliService : IGitHubCliService
    {
        private readonly object _gate = new();
        private readonly Dictionary<int, FakeIssueState> _issues = [];
        private int _nextIssueNumber = 1;

        public Task<GitHubIssueListResult> ListIssuesAsync(string? state, int limit, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var issues = _issues.Values
                    .Where(issue => MatchesState(issue.State, state))
                    .OrderBy(issue => issue.Number)
                    .Take(limit)
                    .Select(issue => new GitHubIssueItem(issue.Number, issue.Title, issue.Url, issue.State.ToLowerInvariant()))
                    .ToArray();

                return Task.FromResult(new GitHubIssueListResult(true, null, issues));
            }
        }

        public Task<GitHubPullListResult> ListPullsAsync(string? state, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult(new GitHubPullListResult(true, null, Array.Empty<GitHubPullItem>()));

        public Task<GitHubCreateIssueResult> CreateIssueAsync(string title, string? body, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var issueNumber = _nextIssueNumber++;
                var now = UtcNow();
                _issues[issueNumber] = new FakeIssueState
                {
                    Number = issueNumber,
                    Title = title,
                    Body = body,
                    State = "OPEN",
                    Url = BuildUrl(issueNumber),
                    CreatedAt = now,
                    UpdatedAt = now,
                    Author = "mcp-server"
                };

                return Task.FromResult(new GitHubCreateIssueResult(true, issueNumber, BuildUrl(issueNumber), null));
            }
        }

        public Task<GitHubCommentResult> CommentOnIssueAsync(string issueId, string body, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(issueId, NumberStyles.None, CultureInfo.InvariantCulture, out var issueNumber))
                return Task.FromResult(new GitHubCommentResult(false, $"Cannot parse issue id '{issueId}'."));

            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    return Task.FromResult(new GitHubCommentResult(false, $"Issue #{issueNumber} not found."));

                issue.Comments.Add(new FakeCommentState("mcp-server", body, UtcNow()));
                issue.UpdatedAt = UtcNow();
                return Task.FromResult(new GitHubCommentResult(true, null));
            }
        }

        public Task<GitHubCommentResult> CommentOnPullAsync(string prId, string body, CancellationToken cancellationToken = default)
            => Task.FromResult(new GitHubCommentResult(false, "Pull request comments are not used in this test."));

        public Task<GitHubIssueDetailResult> GetIssueAsync(int issueNumber, CancellationToken ct = default)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    return Task.FromResult(new GitHubIssueDetailResult(false, null, $"Issue #{issueNumber} not found."));

                return Task.FromResult(new GitHubIssueDetailResult(true, issue.ToDetail(), null));
            }
        }

        public Task<GitHubMutationResult> UpdateIssueAsync(int issueNumber, GitHubIssueUpdateRequest request, CancellationToken ct = default)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    return Task.FromResult(new GitHubMutationResult(false, null, $"Issue #{issueNumber} not found."));

                if (request.Title is not null)
                    issue.Title = request.Title;
                if (request.Body is not null)
                    issue.Body = request.Body;
                if (request.AddLabels is not null)
                {
                    foreach (var label in request.AddLabels)
                    {
                        if (!issue.Labels.Contains(label, StringComparer.Ordinal))
                            issue.Labels.Add(label);
                    }
                }

                if (request.RemoveLabels is not null)
                {
                    foreach (var label in request.RemoveLabels)
                        issue.Labels.RemoveAll(existing => string.Equals(existing, label, StringComparison.Ordinal));
                }

                issue.UpdatedAt = UtcNow();
                return Task.FromResult(new GitHubMutationResult(true, issue.Url, null));
            }
        }

        public Task<GitHubMutationResult> CloseIssueAsync(int issueNumber, string? reason = null, CancellationToken ct = default)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    return Task.FromResult(new GitHubMutationResult(false, null, $"Issue #{issueNumber} not found."));

                issue.State = "CLOSED";
                issue.ClosedAt = UtcNow();
                issue.UpdatedAt = UtcNow();
                return Task.FromResult(new GitHubMutationResult(true, issue.Url, null));
            }
        }

        public Task<GitHubMutationResult> ReopenIssueAsync(int issueNumber, CancellationToken ct = default)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    return Task.FromResult(new GitHubMutationResult(false, null, $"Issue #{issueNumber} not found."));

                issue.State = "OPEN";
                issue.ClosedAt = null;
                issue.UpdatedAt = UtcNow();
                return Task.FromResult(new GitHubMutationResult(true, issue.Url, null));
            }
        }

        public Task<GitHubLabelsResult> ListIssueLabelsAsync(CancellationToken ct = default)
        {
            IReadOnlyList<GitHubLabel> labels =
            [
                new("priority: LOW", null, null),
                new("priority: MEDIUM", null, null),
                new("priority: HIGH", null, null)
            ];

            return Task.FromResult(new GitHubLabelsResult(true, labels, null));
        }

        public Task<GitHubWorkflowRunListResult> ListWorkflowRunsAsync(GitHubWorkflowRunQuery query, CancellationToken ct = default)
            => Task.FromResult(new GitHubWorkflowRunListResult(true, Array.Empty<GitHubWorkflowRunItem>(), null));

        public Task<GitHubWorkflowRunDetailResult> GetWorkflowRunAsync(long runId, CancellationToken ct = default)
            => Task.FromResult(new GitHubWorkflowRunDetailResult(false, null, "Workflow runs are not used in this test."));

        public Task<GitHubMutationResult> RerunWorkflowRunAsync(long runId, CancellationToken ct = default)
            => Task.FromResult(new GitHubMutationResult(false, null, "Workflow runs are not used in this test."));

        public Task<GitHubMutationResult> CancelWorkflowRunAsync(long runId, CancellationToken ct = default)
            => Task.FromResult(new GitHubMutationResult(false, null, "Workflow runs are not used in this test."));

        public void AddExternalComment(int issueNumber, string author, string body)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    throw new InvalidOperationException($"Issue #{issueNumber} not found.");

                issue.Comments.Add(new FakeCommentState(author, body, UtcNow()));
                issue.UpdatedAt = UtcNow();
            }
        }

        public void CloseExternally(int issueNumber, string? reason)
        {
            lock (_gate)
            {
                if (!_issues.TryGetValue(issueNumber, out var issue))
                    throw new InvalidOperationException($"Issue #{issueNumber} not found.");

                issue.State = "CLOSED";
                issue.ClosedAt = UtcNow();
                issue.UpdatedAt = UtcNow();
                if (!string.IsNullOrWhiteSpace(reason))
                    issue.CloseReason = reason;
            }
        }

        private static bool MatchesState(string actualState, string? requestedState)
        {
            if (string.IsNullOrWhiteSpace(requestedState) || string.Equals(requestedState, "all", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(actualState, requestedState, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildUrl(int issueNumber) => $"https://github.com/test/issues/{issueNumber}";

        private static string UtcNow() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        private sealed class FakeIssueState
        {
            public int Number { get; init; }

            public string Title { get; set; } = string.Empty;

            public string? Body { get; set; }

            public string State { get; set; } = "OPEN";

            public string Url { get; init; } = string.Empty;

            public string CreatedAt { get; init; } = string.Empty;

            public string UpdatedAt { get; set; } = string.Empty;

            public string? ClosedAt { get; set; }

            public string? CloseReason { get; set; }

            public string? Author { get; init; }

            public List<string> Labels { get; } = [];

            public List<string> Assignees { get; } = [];

            public List<FakeCommentState> Comments { get; } = [];

            public GitHubIssueDetail ToDetail()
            {
                return new GitHubIssueDetail(
                    Number,
                    Title,
                    Body,
                    State,
                    Url,
                    Labels.Select(label => new GitHubLabel(label, null, null)).ToArray(),
                    Assignees.ToArray(),
                    null,
                    CreatedAt,
                    UpdatedAt,
                    ClosedAt,
                    Author,
                    Comments.Select(comment => new GitHubIssueComment(comment.Author, comment.Body, comment.CreatedAt)).ToArray());
            }
        }

        private sealed record FakeCommentState(string? Author, string Body, string CreatedAt);
    }
}
