using System.Xml;

namespace GitChurnCalculator.Services;

/// <summary>
/// Parses Visual Studio coverage XML files (root element &lt;results&gt;) that contain
/// per-module &lt;function&gt;/&lt;range&gt; data with &lt;source_files&gt; mapping tables.
/// Uses streaming XmlReader to handle large files (&gt;50 MB).
/// </summary>
public sealed class VsCoverageXmlParser : ICoverageParser
{
    public Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths) =>
        CoveragePathMatcher.MapToGitFiles(coverageByPath, trackedGitRelativePaths);

    public Dictionary<string, double> Parse(string coverageFilePath)
    {
        // Per-file line tracking across all modules:
        // filePath -> (coveredLines, allLines)
        var fileLines = new Dictionary<string, (HashSet<int> Covered, HashSet<int> All)>(StringComparer.OrdinalIgnoreCase);

        using var reader = XmlReader.Create(coverageFilePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "module")
                continue;

            ParseModule(reader, fileLines);
        }

        // Convert to coverage percentages
        var coverage = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (filePath, (covered, all)) in fileLines)
        {
            if (all.Count == 0)
                continue;

            var percent = (double)covered.Count / all.Count * 100.0;
            coverage[filePath] = percent;
        }

        return coverage;
    }

    private static void ParseModule(XmlReader reader, Dictionary<string, (HashSet<int> Covered, HashSet<int> All)> fileLines)
    {
        // Collect ranges first (source_id -> list of (line, covered))
        var rangesBySourceId = new Dictionary<string, List<(int Line, bool Covered)>>(StringComparer.Ordinal);

        // source_id -> file path (populated when we hit <source_files>)
        var sourceIdToPath = new Dictionary<string, string>(StringComparer.Ordinal);

        // We must read the entire <module> subtree. Track depth to know when we exit.
        if (reader.IsEmptyElement)
            return;

        var moduleDepth = reader.Depth;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == moduleDepth)
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "range":
                    ParseRange(reader, rangesBySourceId);
                    break;
                case "source_file":
                    ParseSourceFile(reader, sourceIdToPath);
                    break;
            }
        }

        // Now merge ranges into the global fileLines dictionary using the source_id -> path mapping
        foreach (var (sourceId, ranges) in rangesBySourceId)
        {
            if (!sourceIdToPath.TryGetValue(sourceId, out var filePath))
                continue;

            if (!fileLines.TryGetValue(filePath, out var entry))
            {
                entry = (new HashSet<int>(), new HashSet<int>());
                fileLines[filePath] = entry;
            }

            foreach (var (line, covered) in ranges)
            {
                entry.All.Add(line);
                if (covered)
                    entry.Covered.Add(line);
            }
        }
    }

    private static void ParseRange(XmlReader reader, Dictionary<string, List<(int Line, bool Covered)>> rangesBySourceId)
    {
        var sourceId = reader.GetAttribute("source_id");
        var startLineStr = reader.GetAttribute("start_line");
        var endLineStr = reader.GetAttribute("end_line");
        var coveredStr = reader.GetAttribute("covered");

        if (sourceId is null || startLineStr is null || coveredStr is null)
            return;

        if (!int.TryParse(startLineStr, out var startLine))
            return;

        var endLine = startLine;
        if (endLineStr is not null)
            int.TryParse(endLineStr, out endLine);

        var isCovered = coveredStr.Equals("yes", StringComparison.OrdinalIgnoreCase);

        if (!rangesBySourceId.TryGetValue(sourceId, out var list))
        {
            list = [];
            rangesBySourceId[sourceId] = list;
        }

        for (var line = startLine; line <= endLine; line++)
            list.Add((line, isCovered));
    }

    private static void ParseSourceFile(XmlReader reader, Dictionary<string, string> sourceIdToPath)
    {
        var id = reader.GetAttribute("id");
        var path = reader.GetAttribute("path");

        if (id is not null && path is not null)
            sourceIdToPath[id] = path;
    }
}
