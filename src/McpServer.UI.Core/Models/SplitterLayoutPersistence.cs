using System;
using Avalonia.Controls;

namespace McpServer.UI.Core.Models;

/// <summary>Shared helpers for applying and saving grid row/column sizes that are controlled by GridSplitters.</summary>
public static class SplitterLayoutPersistence
{
    public static GridLength Resolve(GridLengthDto? storedLength, GridLength fallback)
        => storedLength?.ToGridLength() ?? fallback;

    public static bool TryApplyRowHeight(
        Grid? grid,
        int rowIndex,
        GridLengthDto? storedLength,
        GridLength fallback,
        Func<GridLength, GridLength>? coerce = null)
    {
        if (grid?.RowDefinitions == null || rowIndex < 0 || rowIndex >= grid.RowDefinitions.Count)
            return false;

        var length = Resolve(storedLength, fallback);
        if (coerce != null)
            length = coerce(length);

        grid.RowDefinitions[rowIndex].Height = length;
        return true;
    }

    public static bool TryApplyColumnWidth(
        Grid? grid,
        int columnIndex,
        GridLengthDto? storedLength,
        GridLength fallback,
        Func<GridLength, GridLength>? coerce = null)
    {
        if (grid?.ColumnDefinitions == null || columnIndex < 0 || columnIndex >= grid.ColumnDefinitions.Count)
            return false;

        var length = Resolve(storedLength, fallback);
        if (coerce != null)
            length = coerce(length);

        grid.ColumnDefinitions[columnIndex].Width = length;
        return true;
    }

    public static bool TryCaptureRowHeight(Grid? grid, int rowIndex, out GridLengthDto? storedLength)
    {
        storedLength = null;
        if (grid?.RowDefinitions == null || rowIndex < 0 || rowIndex >= grid.RowDefinitions.Count)
            return false;

        storedLength = GridLengthDto.FromGridLength(grid.RowDefinitions[rowIndex].Height);
        return true;
    }

    public static bool TryCaptureColumnWidth(Grid? grid, int columnIndex, out GridLengthDto? storedLength)
    {
        storedLength = null;
        if (grid?.ColumnDefinitions == null || columnIndex < 0 || columnIndex >= grid.ColumnDefinitions.Count)
            return false;

        storedLength = GridLengthDto.FromGridLength(grid.ColumnDefinitions[columnIndex].Width);
        return true;
    }
}

