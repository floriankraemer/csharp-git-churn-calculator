using System.Collections.ObjectModel;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.UI.Models;

public sealed class FileTreeNode
{
    public FileTreeNode(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public string Name { get; }
    public string Path { get; }
    public FileChurnResult? Result { get; private set; }
    public ObservableCollection<FileTreeNode> Children { get; } = [];

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

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            currentPath = currentPath.Length == 0 ? segment : $"{currentPath}/{segment}";

            var node = siblings.FirstOrDefault(x => string.Equals(x.Name, segment, StringComparison.Ordinal));
            if (node is null)
            {
                node = new FileTreeNode(segment, currentPath);
                InsertSorted(siblings, node);
            }

            if (i == segments.Length - 1)
                node.Result = result;

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
