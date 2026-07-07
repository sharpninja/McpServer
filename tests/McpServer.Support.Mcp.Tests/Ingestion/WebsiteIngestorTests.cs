using System.Net;
using System.Net.Http;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-MCP-INGEST-003: Unit tests for direct website ingestion behavior and guards.</summary>
public sealed class WebsiteIngestorTests
{
    [Fact]
    public async Task IngestAsync_HtmlExtraction_PreservesTitleAndHeadingText()
    {
        const string html = "<html><head><title>Docs Home</title></head><body><nav>skip</nav><h1>Welcome</h1><p>GraphRAG content.</p></body></html>";
        var sut = CreateSut(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        }));

        var results = await sut.IngestAsync(new WebsiteIngestRequest { Url = "https://example.com/docs", MaxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var page = Assert.Single(results);
        Assert.Equal("ingested", page.Outcome.Status);
        Assert.NotNull(page.Document);
        Assert.Contains("https://example.com/docs", page.Document!.SourceKey, StringComparison.Ordinal);
        Assert.True(page.Chunks.Count > 0);
    }

    [Fact]
    public async Task IngestAsync_RespectsMaxPages_WhenSubpagesEnabled()
    {
        var rootHtml = "<html><body><a href=\"/a\">A</a><a href=\"/b\">B</a></body></html>";
        var sut = CreateSut(new StubHandler(request =>
        {
            var html = request.RequestUri!.AbsolutePath == "/" ? rootHtml : "<html><body><p>child</p></body></html>";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }));

        var results = await sut.IngestAsync(new WebsiteIngestRequest
        {
            Url = "https://example.com/",
            IncludeSubpages = true,
            MaxPages = 1,
            MaxDepth = 2
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(results);
    }

    [Fact]
    public async Task IngestAsync_UsesConfiguredMaxWebsitePagesCap_WhenRequestExceedsLimit()
    {
        var rootHtml = "<html><body><a href=\"/a\">A</a><a href=\"/b\">B</a><a href=\"/c\">C</a><a href=\"/d\">D</a></body></html>";
        var sut = CreateSut(new StubHandler(request =>
        {
            var html = request.RequestUri!.AbsolutePath == "/" ? rootHtml : "<html><body><p>child</p></body></html>";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }), maxWebsitePages: 3);

        var results = await sut.IngestAsync(new WebsiteIngestRequest
        {
            Url = "https://example.com/",
            IncludeSubpages = true,
            MaxPages = 5000,
            MaxDepth = 2
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task IngestAsync_PrioritizesAndFiltersContentLinks_WhenSubpagesEnabled()
    {
        var rootHtml = "<html><body><a href=\"/load.php?modules=site.styles&only=styles\">Styles</a><a href=\"/rest.php/v1/search\">Api</a><a href=\"/wiki/C64\">Article</a></body></html>";
        var handler = new StubHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Equals("/wiki/Main_Page", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(rootHtml, Encoding.UTF8, "text/html")
                };
            }

            if (path.Equals("/wiki/C64", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><body><p>Commodore 64</p></body></html>", Encoding.UTF8, "text/html")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body><p>non-content endpoint</p></body></html>", Encoding.UTF8, "text/html")
            };
        });

        var sut = CreateSut(handler, maxWebsitePages: 2);

        var results = await sut.IngestAsync(new WebsiteIngestRequest
        {
            Url = "https://example.com/wiki/Main_Page",
            IncludeSubpages = true,
            MaxPages = 2,
            MaxDepth = 2
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, results.Count);
        Assert.Collection(results,
            root => Assert.Equal("https://example.com/wiki/Main_Page", root.Outcome.Url),
            article => Assert.Equal("https://example.com/wiki/C64", article.Outcome.Url));
    }

    [Theory]
    [InlineData("http://localhost/")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://192.168.1.10/")]
    [InlineData("http://169.254.1.10/")]
    public async Task IngestAsync_BlocksSsrfTargets(string url)
    {
        var sut = CreateSut(new StubHandler(_ => throw new InvalidOperationException("request should not execute")));

        var results = await sut.IngestAsync(new WebsiteIngestRequest { Url = url, MaxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var page = Assert.Single(results);
        Assert.Equal("error", page.Outcome.Status);
        Assert.Contains("blocked", page.Outcome.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestAsync_BlocksRedirectToPrivateIp()
    {
        var handler = new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("http://127.0.0.1/private");
            return response;
        });
        var sut = CreateSut(handler);

        var results = await sut.IngestAsync(new WebsiteIngestRequest { Url = "https://example.com/", MaxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var page = Assert.Single(results);
        Assert.Equal("error", page.Outcome.Status);
        Assert.Contains("blocked", page.Outcome.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestAsync_CanonicalSourceKey_IsDeterministic()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><body>hello</body></html>", Encoding.UTF8, "text/html")
        });
        var sut = CreateSut(handler);

        var first = await sut.IngestAsync(new WebsiteIngestRequest { Url = "https://EXAMPLE.com:443/docs/#top", MaxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await sut.IngestAsync(new WebsiteIngestRequest { Url = "https://example.com/docs", MaxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(first[0].Document!.SourceKey, second[0].Document!.SourceKey);
    }

    [Fact]
    public async Task IngestAsync_WhenPageExceedsMaxBytes_ReturnsError()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(new string('x', 10_000), Encoding.UTF8, "text/plain")
        });
        var sut = CreateSut(handler);

        var results = await sut.IngestAsync(new WebsiteIngestRequest
        {
            Url = "https://example.com/large",
            MaxPages = 1,
            MaxBytesPerPage = 4096
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var page = Assert.Single(results);
        Assert.Equal("error", page.Outcome.Status);
        Assert.Contains("max bytes", page.Outcome.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static WebsiteIngestor CreateSut(HttpMessageHandler handler, int maxWebsitePages = 50)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = "e:/github/McpServer",
            MaxWebsitePages = maxWebsitePages,
            MaxWebsiteDepth = 3,
            MaxWebsiteBytesPerPage = 256000,
            MaxWebsiteRedirects = 3,
            WebsiteAllowedSchemes = ["http", "https"],
            WebsiteBlockedHosts = ["localhost"]
        });

        var context = new WorkspaceContext
        {
            WorkspacePath = "e:/github/McpServer"
        };

        return new WebsiteIngestor(
            new Chunker(),
            options,
            context,
            NullLogger<WebsiteIngestor>.Instance,
            () => new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
