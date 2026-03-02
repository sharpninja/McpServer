using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace McpServer.VsExtension.McpTodo;

internal static class FilterMruStorage
{
    private const int MaxCount = 10;
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = false };

    private static string GetFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpServer.VsExtension.McpTodo");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "filter-mru.json");
    }

    public static List<string> Load()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path)) return new List<string>();
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void Save(IList<string> items)
    {
        try
        {
            var path = GetFilePath();
            var list = (items ?? new List<string>()).Take(MaxCount).ToList();
            var json = JsonSerializer.Serialize(list, s_jsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // best-effort
        }
    }
}
