// Global using directives to match the implicit usings provided by Microsoft.NET.Sdk.Web
// Required because McpServer.Services uses Microsoft.NET.Sdk (not Web SDK) but the
// source files were originally written for a Web SDK project.
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
