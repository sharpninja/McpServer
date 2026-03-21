using McpServer.McpAgent.SampleHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

try
{
    using var cancellationSource = new CancellationTokenSource();
    SampleHostConsoleApplication? application = null;
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;

        if (application?.TryCancelActivePowerShellCommand() == true)
            return;

        cancellationSource.Cancel();
    };

    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
    builder.Configuration.AddYamlFile(
        Path.Combine(AppContext.BaseDirectory, "appsettings.yaml"),
        optional: false,
        reloadOnChange: true);

    using var serviceProvider = SampleHostAppFactory.BuildServiceProvider(builder.Configuration);
    application = SampleHostAppFactory.CreateApplication(serviceProvider);
    using (application)
    {
        return await application.RunAsync(args, cancellationSource.Token).ConfigureAwait(false);
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Canceled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Failed to start the MCP Agent sample host: {exception}");
    return 1;
}
