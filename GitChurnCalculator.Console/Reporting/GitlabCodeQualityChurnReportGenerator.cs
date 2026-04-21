using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class GitlabCodeQualityChurnReportGenerator : IChurnReportGenerator
{
    private const string CheckName = "churn/file-risk";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Generate(IReadOnlyList<FileChurnResult> results, string subtitle)
    {
        _ = subtitle;
        var issues = results.Select(r => new GitlabCodeQualityIssue
        {
            Type = "issue",
            CheckName = CheckName,
            Description = ChurnCiSeverity.BuildMessage(r),
            Categories = ["Complexity"],
            Severity = ChurnCiSeverity.GitlabSeverity(r.ChurnRiskScore),
            Fingerprint = ComputeFingerprint(r.FilePath),
            Location = new GitlabLocation
            {
                Path = ChurnCiEncoding.NormalizeFilePath(r.FilePath),
                Lines = new GitlabLines { Begin = 1, End = 1 },
            },
        }).ToList();

        return JsonSerializer.Serialize(issues, JsonOptions);
    }

    private static string ComputeFingerprint(string filePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("churn|" + filePath));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal sealed class GitlabCodeQualityIssue
{
    public string Type { get; init; } = "";

    public string CheckName { get; init; } = "";

    public string Description { get; init; } = "";

    public List<string> Categories { get; init; } = [];

    public string Severity { get; init; } = "";

    public string Fingerprint { get; init; } = "";

    public GitlabLocation Location { get; init; } = new();
}

internal sealed class GitlabLocation
{
    public string Path { get; init; } = "";

    public GitlabLines Lines { get; init; } = new();
}

internal sealed class GitlabLines
{
    public int Begin { get; init; }

    public int End { get; init; }
}
