// Global using directives to match the implicit usings provided by Microsoft.NET.Sdk.Web
// Required because McpServer.GraphRag uses Microsoft.NET.Sdk (not Web SDK) but the
// source files were originally written for a Web SDK project.
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Options;
