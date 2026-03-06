using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.Models.Json
{
    public partial class JsonTreeNode : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _value;

        [ObservableProperty]
        private string _type;

        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>
        /// Path into the JSON tree for this node, e.g. "requests[3]" or "entries[5]".
        /// Used by search to locate and select the node.
        /// </summary>
        public string? SourcePath { get; set; }

        public ObservableCollection<JsonTreeNode> Children { get; set; } = new();

        public JsonTreeNode(string name, string value, string type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public JsonTreeNode(string name, string value, string type, string? sourcePath) : this(name, value, type)
        {
            SourcePath = sourcePath;
        }
    }
}

