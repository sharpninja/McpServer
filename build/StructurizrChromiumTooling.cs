using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

/// <summary>
/// TR-MCP-NUKE-017: Real tooling boundary of the ArchitectureDiagrams target. Runs the pinned Structurizr CLI in
/// Docker on a temporary copy of the workspace (nothing is written under docs) and renders views to SVG by driving
/// headless Chromium over the DevTools protocol against the Structurizr static export: light mode, then per view
/// structurizr.diagram.changeView(key) and structurizr.diagram.exportCurrentDiagramToSVG(false), then crops each SVG
/// to its measured content with <see cref="SvgContentFitter"/> (AC-2 "the content fitted"). The workspace sets
/// "structurizr.metadata" "false", which makes the export byte-identical across runs (PLAN-C4MODEL-001 D8).
/// </summary>
/// <param name="chromiumPath">Chromium-family browser executable.</param>
sealed class StructurizrChromiumTooling(string chromiumPath) : IArchitectureDiagramTooling
{
    private static readonly TimeSpan ViewTimeout = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    public ArchitectureToolResult Validate(string workspaceDirectory)
    {
        var copy = CopyWorkspace(workspaceDirectory);
        try
        {
            return RunCli(copy, "validate", "-workspace", "workspace.dsl");
        }
        finally
        {
            Directory.Delete(copy, recursive: true);
        }
    }

    /// <inheritdoc />
    public string ExportWorkspaceJson(string workspaceDirectory)
    {
        var copy = CopyWorkspace(workspaceDirectory);
        try
        {
            var result = RunCli(copy, "export", "-workspace", "workspace.dsl", "-format", "json", "-output", "json");
            var path = Path.Combine(copy, "json", "workspace.json");
            if (result.ExitCode != 0 || !File.Exists(path))
            {
                throw new InvalidOperationException("Structurizr JSON export failed (exit " + result.ExitCode + "): " + result.Output);
            }

            return File.ReadAllText(path);
        }
        finally
        {
            Directory.Delete(copy, recursive: true);
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> RenderViews(string workspaceDirectory, IReadOnlyList<string> viewKeys)
    {
        var copy = CopyWorkspace(workspaceDirectory);
        var profile = Path.Combine(Path.GetTempPath(), "c4-chromium-" + Guid.NewGuid().ToString("N"));
        Process? browser = null;
        try
        {
            var export = RunCli(copy, "export", "-workspace", "workspace.dsl", "-format", "static", "-output", "static");
            var index = Path.Combine(copy, "static", "index.html");
            if (export.ExitCode != 0 || !File.Exists(index))
            {
                throw new InvalidOperationException("Structurizr static export failed (exit " + export.ExitCode + "): " + export.Output);
            }

            var port = FreePort();
            browser = Process.Start(new ProcessStartInfo(chromiumPath)
            {
                ArgumentList =
                {
                    "--headless=new", "--disable-gpu", "--no-sandbox", "--hide-scrollbars",
                    "--remote-debugging-port=" + port, "--user-data-dir=" + profile, "about:blank",
                },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }) ?? throw new InvalidOperationException("Could not start " + chromiumPath);
            browser.ErrorDataReceived += (_, _) => { };
            browser.OutputDataReceived += (_, _) => { };
            browser.BeginErrorReadLine();
            browser.BeginOutputReadLine();

            return RenderWithDevTools(port, new Uri(index).AbsoluteUri, viewKeys).GetAwaiter().GetResult();
        }
        finally
        {
            if (browser is { HasExited: false })
            {
                browser.Kill(entireProcessTree: true);
                browser.WaitForExit();
            }

            browser?.Dispose();
            Directory.Delete(copy, recursive: true);
            if (Directory.Exists(profile))
            {
                Directory.Delete(profile, recursive: true);
            }
        }
    }

    private static async Task<IReadOnlyDictionary<string, string>> RenderWithDevTools(int port, string indexUri, IReadOnlyList<string> viewKeys)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        string? socketUrl = null;
        for (var attempt = 0; attempt < 100 && socketUrl is null; attempt++)
        {
            try
            {
                using var targets = JsonDocument.Parse(await http.GetStringAsync(new Uri("http://127.0.0.1:" + port + "/json/list")).ConfigureAwait(false));
                socketUrl = targets.RootElement.EnumerateArray()
                    .Where(target => target.GetProperty("type").GetString() == "page")
                    .Select(target => target.GetProperty("webSocketDebuggerUrl").GetString())
                    .FirstOrDefault();
            }
            catch (HttpRequestException)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        if (socketUrl is null)
        {
            throw new InvalidOperationException("Chromium DevTools endpoint did not become available on port " + port + ".");
        }

        using var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.Zero;
        await socket.ConnectAsync(new Uri(socketUrl), CancellationToken.None).ConfigureAwait(false);
        var session = new DevToolsSession(socket);
        await session.Call("Page.enable", new Dictionary<string, object?>()).ConfigureAwait(false);
        await session.Call("Emulation.setEmulatedMedia", new Dictionary<string, object?>
        {
            ["features"] = new[] { new Dictionary<string, object?> { ["name"] = "prefers-color-scheme", ["value"] = "light" } },
        }).ConfigureAwait(false);
        await session.Call("Page.navigate", new Dictionary<string, object?> { ["url"] = indexUri }).ConfigureAwait(false);
        var loaded = false;
        for (var attempt = 0; attempt < 600 && !loaded; attempt++)
        {
            using var state = await session.Call("Runtime.evaluate", new Dictionary<string, object?>
            {
                ["expression"] = "document.readyState + '|' + location.protocol",
                ["returnByValue"] = true,
            }).ConfigureAwait(false);
            loaded = state.RootElement.TryGetProperty("result", out var stateResult)
                && stateResult.TryGetProperty("result", out var stateValue)
                && stateValue.TryGetProperty("value", out var text)
                && text.GetString() == "complete|file:";
            if (!loaded)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        if (!loaded)
        {
            throw new InvalidOperationException("The Structurizr static export did not finish loading in Chromium.");
        }

        var svgs = new Dictionary<string, string>(StringComparer.Ordinal);
        var fitter = new SvgContentFitter();
        var renumberer = new SvgIdRenumberer();
        foreach (var key in viewKeys.Distinct(StringComparer.Ordinal))
        {
            // AC-2 "the content fitted": the export's viewBox is the whole paper, so the drawn content is measured in
            // the page (union of the rendered shapes of the exported SVG at 1:1 scale) and SvgContentFitter crops to it.
            var expression = """
                new Promise((resolve, reject) => {
                  const key = __KEY__;
                  const deadline = Date.now() + __TIMEOUT__;
                  const measure = (svg) => {
                    const holder = document.createElement('div');
                    holder.style.cssText = 'position:absolute;left:0;top:0;';
                    holder.innerHTML = svg;
                    document.body.appendChild(holder);
                    try {
                      const root = holder.querySelector('svg');
                      const paper = root.viewBox.baseVal;
                      root.setAttribute('width', paper.width);
                      root.setAttribute('height', paper.height);
                      const origin = root.getBoundingClientRect();
                      let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
                      root.querySelectorAll('rect,path,text,image,ellipse,circle,polygon,polyline,line').forEach(node => {
                        const r = node.getBoundingClientRect();
                        if (r.width <= 0 && r.height <= 0) { return; }
                        minX = Math.min(minX, paper.x + r.left - origin.left);
                        minY = Math.min(minY, paper.y + r.top - origin.top);
                        maxX = Math.max(maxX, paper.x + r.right - origin.left);
                        maxY = Math.max(maxY, paper.y + r.bottom - origin.top);
                      });
                      return [minX, minY, maxX, maxY];
                    } finally {
                      holder.remove();
                    }
                  };
                  const attempt = () => {
                    if (window.structurizr && structurizr.diagram && typeof structurizr.diagram.changeView === 'function') {
                      structurizr.diagram.setDarkMode(false);
                      structurizr.diagram.changeView(key, () => {
                        try {
                          const svg = structurizr.diagram.exportCurrentDiagramToSVG(false);
                          resolve(JSON.stringify({ svg: svg, content: measure(svg) }));
                        } catch (e) { reject(String(e)); }
                      });
                    } else if (Date.now() > deadline) { reject('Structurizr viewer did not become ready'); } else { setTimeout(attempt, 100); }
                  };
                  attempt();
                })
                """
                .Replace("__KEY__", JsonSerializer.Serialize(key), StringComparison.Ordinal)
                .Replace("__TIMEOUT__", ((int)ViewTimeout.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
            using var reply = await session.Call("Runtime.evaluate", new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["awaitPromise"] = true,
                ["returnByValue"] = true,
            }).ConfigureAwait(false);
            if (reply.RootElement.TryGetProperty("result", out var result)
                && result.TryGetProperty("result", out var value)
                && value.TryGetProperty("value", out var payload)
                && payload.ValueKind == JsonValueKind.String)
            {
                using var rendered = JsonDocument.Parse(payload.GetString()!);
                var svg = rendered.RootElement.GetProperty("svg").GetString()!;
                var edges = rendered.RootElement.GetProperty("content").EnumerateArray()
                    .Select(edge => edge.ValueKind == JsonValueKind.Number ? edge.GetDouble() : double.NaN)
                    .ToArray();
                try
                {
                    // AC-2: the ids come from one page-wide counter consumed at a timing-dependent point, so each
                    // render is renumbered on its own.
                    svgs[key] = renumberer.Renumber(fitter.Fit(svg, new SvgContentBox(edges[0], edges[1], edges[2], edges[3])));
                }
                catch (ArgumentException)
                {
                    // Nothing drawn or no root viewBox: the key stays missing and the runner reports RenderFailed.
                }
            }
        }

        return svgs;
    }

    private static string CopyWorkspace(string workspaceDirectory)
    {
        var copy = Path.Combine(Path.GetTempPath(), "c4-workspace-" + Guid.NewGuid().ToString("N"));
        foreach (var source in Directory.EnumerateFiles(workspaceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(workspaceDirectory, source);
            var top = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (top is "rendered" or "static" or "json")
            {
                continue;
            }

            var target = Path.Combine(copy, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target);
        }

        return copy;
    }

    private static ArchitectureToolResult RunCli(string mountDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--rm");
        if (!OperatingSystem.IsWindows())
        {
            start.ArgumentList.Add("--user");
            start.ArgumentList.Add(ReadId("-u") + ":" + ReadId("-g"));
        }

        start.ArgumentList.Add("-v");
        start.ArgumentList.Add(mountDirectory + ":/usr/local/structurizr");
        start.ArgumentList.Add(ArchitectureDiagramPaths.StructurizrCliImage);
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start docker.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new ArchitectureToolResult(process.ExitCode, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    private static string ReadId(string flag)
    {
        using var process = Process.Start(new ProcessStartInfo("id", flag) { RedirectStandardOutput = true, UseShellExecute = false })
            ?? throw new InvalidOperationException("Could not run id " + flag);
        var value = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return value;
    }

    private static int FreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Minimal DevTools protocol client: one request at a time, events skipped.</summary>
    private sealed class DevToolsSession(ClientWebSocket socket)
    {
        private int nextId;

        public async Task<JsonDocument> Call(string method, Dictionary<string, object?> parameters)
        {
            var id = ++nextId;
            var request = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object?> { ["id"] = id, ["method"] = method, ["params"] = parameters });
            await socket.SendAsync(request, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None).ConfigureAwait(false);
            var buffer = new byte[1 << 20];
            while (true)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult received;
                do
                {
                    received = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                    message.Write(buffer, 0, received.Count);
                }
                while (!received.EndOfMessage);

                var document = JsonDocument.Parse(message.ToArray());
                if (document.RootElement.TryGetProperty("id", out var replyId) && replyId.GetInt32() == id)
                {
                    return document;
                }

                document.Dispose();
            }
        }
    }
}
