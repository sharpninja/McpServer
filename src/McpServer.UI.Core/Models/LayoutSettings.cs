using Avalonia.Controls;

namespace McpServer.UI.Core.Models;

public class LayoutSettings
{
    public GridLengthDto LandscapeLeftColWidth { get; set; } = new(300, GridUnitType.Pixel);
    public GridLengthDto LandscapeHistoryRowHeight { get; set; } = new(150, GridUnitType.Pixel);

    public GridLengthDto PortraitTreeRowHeight { get; set; } = new(1, GridUnitType.Star);
    public GridLengthDto PortraitViewerRowHeight { get; set; } = new(1, GridUnitType.Star);
    public GridLengthDto PortraitHistoryRowHeight { get; set; } = new(150, GridUnitType.Pixel);

    /// <summary>JSON viewer: search index row height (row 2). Default 200px; tree row is the only * so it fills the rest.</summary>
    public GridLengthDto JsonViewerSearchIndexRowHeight { get; set; } = new(200, GridUnitType.Pixel);
    /// <summary>JSON viewer: tree row (row 4) is always * and not persisted.</summary>
    public GridLengthDto JsonViewerTreeRowHeight { get; set; } = new(1, GridUnitType.Star);

    // Window State Persistence
    public double WindowWidth { get; set; } = 1000;
    public double WindowHeight { get; set; } = 800;
    public double WindowX { get; set; } = 100;
    public double WindowY { get; set; } = 100;
    public WindowState WindowState { get; set; } = WindowState.Normal;

    // Chat Window State
    public double ChatWindowWidth { get; set; } = 500;
    public double ChatWindowHeight { get; set; } = 550;
    public double ChatWindowX { get; set; }
    public double ChatWindowY { get; set; }

    /// <summary>Chat window: row height for template picker (row 1). Splitter below it. Default 1*.</summary>
    public GridLengthDto ChatTemplatePickerRowHeight { get; set; } = new(1, GridUnitType.Star);

    // Todo editor splitter (default: 1/3 list, 2/3 editor)
    public GridLengthDto TodoEditorLandscapeListWidth { get; set; } = new(1, GridUnitType.Star);
    public GridLengthDto TodoEditorPortraitListHeight { get; set; } = new(1, GridUnitType.Star);

    // Workspace editor splitter (default: 1/3 list, 2/3 editor)
    public GridLengthDto WorkspaceEditorLandscapeListWidth { get; set; } = new(1, GridUnitType.Star);
    public GridLengthDto WorkspaceEditorPortraitListHeight { get; set; } = new(1, GridUnitType.Star);

    /// <summary>True if the chat window was open when the app was last closed; reopen it on next launch.</summary>
    public bool ChatWindowWasOpen { get; set; }

    /// <summary>Index of the last selected tab in MainTabControl.</summary>
    public int SelectedTabIndex { get; set; }
}

public class GridLengthDto
{
    public double Value { get; set; }
    public GridUnitType UnitType { get; set; }

    public GridLengthDto() { }

    public GridLengthDto(double value, GridUnitType unitType)
    {
        Value = value;
        UnitType = unitType;
    }

    public GridLength ToGridLength()
    {
        // When loading: Star with value > 20 was likely saved as pixel-by-mistake; apply as Pixel.
        if (UnitType == GridUnitType.Star && Value > 20)
            return new GridLength(Value, GridUnitType.Pixel);
        return new GridLength(Value, UnitType);
    }

    /// <summary>Converts a GridLength to DTO. Normalizes Avalonia quirk where GridSplitter can store pixel height as Star (e.g. 211.2 Star) so we persist as Pixel for correct restore.</summary>
    public static GridLengthDto FromGridLength(GridLength length)
    {
        if (length.GridUnitType == GridUnitType.Star && length.Value > 20)
        {
            // Star weights are typically 1, 2, 3. Large values are pixel heights stored as Star by the splitter.
            return new GridLengthDto(length.Value, GridUnitType.Pixel);
        }
        return new GridLengthDto(length.Value, length.GridUnitType);
    }
}

