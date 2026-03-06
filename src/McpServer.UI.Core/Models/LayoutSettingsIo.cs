using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Models;

/// <summary>Load/save LayoutSettings from the app's settings file (shared by main and chat window).</summary>
public static class LayoutSettingsIo
{
    private static readonly ILogger _logger = AppLogService.Instance.CreateLogger("LayoutSettings");
    private const string SettingsFileName = "layout_settings.json";

    public static string GetSettingsFilePath()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "McpServerManager");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, SettingsFileName);
    }

    public static LayoutSettings? Load()
    {
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
                return null;
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<LayoutSettings>(json);
            if (settings?.ChatTemplatePickerRowHeight != null)
            {
                var dto = settings.ChatTemplatePickerRowHeight;
                if (dto.UnitType == GridUnitType.Star && dto.Value > 20)
                    settings.ChatTemplatePickerRowHeight = new GridLengthDto(dto.Value, GridUnitType.Pixel);
            }
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LayoutSettings] Failed to load settings");
            return null;
        }
    }

    public static void Save(LayoutSettings settings)
    {
        try
        {
            var path = GetSettingsFilePath();
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(path, json);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[LayoutSettings] Failed to save settings"); }
    }
}


