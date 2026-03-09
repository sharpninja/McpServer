using McpServer.AgentFramework.SampleHost;

using var serviceProvider = SampleHostPreviewFactory.BuildServiceProvider();
var preview = SampleHostPreviewFactory.CreatePreview(serviceProvider);

Console.WriteLine(preview.ToDisplayText());
