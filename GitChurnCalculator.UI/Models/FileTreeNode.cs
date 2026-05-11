using System.Collections.ObjectModel;
using System.ComponentModel;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.UI.Models;

public sealed class FileTreeNode : INotifyPropertyChanged
{
    private bool _isExpanded;

    public FileTreeNode(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }
    public string Path { get; }
    public FileTreeNode? Parent { get; private set; }
    public FileChurnResult? Result { get; private set; }
    public ObservableCollection<FileTreeNode> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;

            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public static ObservableCollection<FileTreeNode> Build(IReadOnlyList<FileChurnResult> results)
    {
        var roots = new ObservableCollection<FileTreeNode>();

        foreach (var result in results.OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            AddPath(roots, result);
        }

        return roots;
    }

    private static void AddPath(ObservableCollection<FileTreeNode> roots, FileChurnResult result)
    {
        var segments = result.FilePath
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var siblings = roots;
        var currentPath = "";
        FileTreeNode? parent = null;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            currentPath = currentPath.Length == 0 ? segment : $"{currentPath}/{segment}";

            var node = siblings.FirstOrDefault(x => string.Equals(x.Name, segment, StringComparison.Ordinal));
            if (node is null)
            {
                node = new FileTreeNode(segment, currentPath);
                node.Parent = parent;
                InsertSorted(siblings, node);
            }

            if (i == segments.Length - 1)
                node.Result = result;

            parent = node;
            siblings = node.Children;
        }
    }

    private static void InsertSorted(ObservableCollection<FileTreeNode> siblings, FileTreeNode node)
    {
        for (var i = 0; i < siblings.Count; i++)
        {
            if (string.Compare(node.Name, siblings[i].Name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                siblings.Insert(i, node);
                return;
            }
        }

        siblings.Add(node);
    }
}
