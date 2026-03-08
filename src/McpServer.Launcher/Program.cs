// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using McpServer.Launcher.Models;
using McpServer.Launcher.Services;
using McpServer.Launcher.Validation;

namespace McpServer.Launcher;

/// <summary>
/// Entry point for the desktop process launcher.
/// Accepts a JSON-serialized <see cref="ProcessLaunchRequest"/> as a command-line argument,
/// launches the process on the interactive desktop using <c>CreateProcessWithTokenW</c>,
/// and writes a JSON <see cref="ProcessLaunchResult"/> to stdout.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) 
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Main entry point.
    /// Exit codes: 0 = success, 1 = launch failure, 2 = invalid args/JSON.
    /// </summary>
    internal static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            var error = new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = "Usage: McpServer.Launcher.exe '<json>'"
            };
            Console.WriteLine(JsonSerializer.Serialize(error, s_jsonOptions));
            return 2;
        }

        ProcessLaunchRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ProcessLaunchRequest>(args[0], s_jsonOptions);
        }
        catch (JsonException ex)
        {
            var error = new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = $"Invalid JSON: {ex.Message}"
            };
            Console.WriteLine(JsonSerializer.Serialize(error, s_jsonOptions));
            return 2;
        }

        if (request is null)
        {
            var error = new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = "JSON deserialized to null."
            };
            Console.WriteLine(JsonSerializer.Serialize(error, s_jsonOptions));
            return 2;
        }

        var validationErrors = RequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            var error = new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = string.Join("; ", validationErrors)
            };
            Console.WriteLine(JsonSerializer.Serialize(error, s_jsonOptions));
            return 2;
        }

        var launcher = new ProcessLauncher();
        var result = launcher.Launch(request);
        Console.WriteLine(JsonSerializer.Serialize(result, s_jsonOptions));
        return result.Success ? 0 : 1;
    }
}
