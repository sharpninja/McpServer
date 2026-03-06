using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.Models.Json
{
    /// <summary>
    /// Roll-up statistics and metadata for a loaded JSON log (Copilot or Cursor).
    /// </summary>
    public partial class JsonLogSummary : ObservableObject
    {
        [ObservableProperty]
        private string _schemaType = "Unknown";

        [ObservableProperty]
        private ObservableCollection<string> _summaryLines = new();

        /// <summary>
        /// Total request/entry count.
        /// </summary>
        [ObservableProperty]
        private int _totalCount;

        /// <summary>
        /// Human-readable stats (e.g. "By model: gpt-4: 5, claude: 3").
        /// </summary>
        [ObservableProperty]
        private string _statsByModel = "";

        [ObservableProperty]
        private string _statsBySuccess = "";

        [ObservableProperty]
        private string _statsCostOrTokens = "";

        /// <summary>
        /// Flat list of searchable entries (requests/entries) for indexing and search. Populated during LoadJson.
        /// </summary>
        public List<SearchableEntry> SearchIndex { get; set; } = new();
    }

    /// <summary>
    /// One searchable entry in the log (a single request or Cursor entry).
    /// Used to build a flat index for search and to jump to the corresponding tree node.
    /// </summary>
    public partial class SearchableEntry : ObservableObject
    {
        /// <summary>
        /// Request ID or similar unique key.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ListLine))]
        private string _requestId = "";

        /// <summary>
        /// Primary display line (slug, title, or first part of request text).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ListLine))]
        private string _displayText = "";

        /// <summary>
        /// Timestamp string for display and search. Must be the entry's timestamp, not the file's.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ListLine))]
        [NotifyPropertyChangedFor(nameof(SortableTimestamp))]
        [NotifyPropertyChangedFor(nameof(TimestampDisplay))]
        private string _timestamp = "";

        /// <summary>
        /// Model name for display and filter.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ListLine))]
        private string _model = "";

        /// <summary>
        /// Agent for display and filter.
        /// </summary>
        [ObservableProperty]
        private string _agent = "";

        /// <summary>
        /// 0-based index in requests[] or entries[] for this log type.
        /// </summary>
        [ObservableProperty]
        private int _entryIndex;

        /// <summary>
        /// Path into the JSON tree to locate this node, e.g. "requests[3]" or "entries[5]".
        /// </summary>
        [ObservableProperty]
        private string _sourcePath = "";

        /// <summary>
        /// Full text used for search (request body, title, slug, etc.).
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// Parsed timestamp for sorting (local time); null if unparseable.
        /// Uses the entry's timestamp only (never file timestamp). UTC and Unspecified are converted to local time.
        /// </summary>
        public DateTime? SortableTimestamp
        {
            get
            {
                var dt = GetEntryTimestamp();
                return dt.HasValue ? ToLocalTime(dt.Value) : null;
            }
        }

        /// <summary>Gets the entry's timestamp for display/sort. Prefers stored Timestamp string, then UnifiedEntry.Timestamp.</summary>
        private DateTime? GetEntryTimestamp()
        {
            if (!string.IsNullOrWhiteSpace(Timestamp))
            {
                if (DateTime.TryParse(Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                    return dt;
                if (DateTime.TryParse(Timestamp, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt2))
                    return dt2;
            }
            return UnifiedEntry?.Timestamp;
        }

        private static DateTime ToLocalTime(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Local) return dt;
            if (dt.Kind == DateTimeKind.Utc) return dt.ToLocalTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        /// <summary>
        /// Timestamp formatted for display using local short date/time. Reflects the entry's timestamp, not the file's.
        /// </summary>
        public string TimestampDisplay => SortableTimestamp.HasValue ? SortableTimestamp.Value.ToString("g") : "";

        /// <summary>
        /// Single line for list display: "requestId | slug | model | timestamp" (timestamp in local short date/time).
        /// </summary>
        public string ListLine => string.IsNullOrEmpty(TimestampDisplay)
            ? $"{RequestId} | {DisplayText} | {Model}"
            : $"{RequestId} | {DisplayText} | {Model} | {TimestampDisplay}";

        /// <summary>
        /// Reference to the unified entry object for detailed viewing.
        /// </summary>
        public UnifiedRequestEntry? UnifiedEntry { get; set; }
    }
}

