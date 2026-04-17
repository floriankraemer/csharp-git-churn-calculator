using System.Xml;

namespace GitChurnCalculator.Services;

/// <summary>
/// Auto-detects the coverage XML format by peeking at the root element,
/// then delegates to the appropriate parser (Cobertura or VS coverage).
/// </summary>
public sealed class AutoDetectCoverageParser : ICoverageParser
{
    public Dictionary<string, double> MapToTrackedFiles(
        Dictionary<string, double> coverageByPath,
        IReadOnlyList<string> trackedGitRelativePaths) =>
        CoveragePathMatcher.MapToGitFiles(coverageByPath, trackedGitRelativePaths);

    public Dictionary<string, double> Parse(string coverageFilePath)
    {
        var parser = CreateParserFor(coverageFilePath);
        return parser.Parse(coverageFilePath);
    }

    public static ICoverageParser CreateParserFor(string coverageFilePath)
    {
        var rootName = PeekRootElementName(coverageFilePath);

        return rootName switch
        {
            "coverage" => new CoberturaXmlParser(),
            "results" => new VsCoverageXmlParser(),
            _ => throw new InvalidOperationException(
                $"Unrecognized coverage XML format (root element: '{rootName}'). " +
                "Expected Cobertura (<coverage>) or Visual Studio coverage (<results>)."),
        };
    }

    private static string PeekRootElementName(string filePath)
    {
        using var reader = XmlReader.Create(filePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
                return reader.Name;
        }

        throw new InvalidOperationException("Coverage XML file contains no elements.");
    }
}
