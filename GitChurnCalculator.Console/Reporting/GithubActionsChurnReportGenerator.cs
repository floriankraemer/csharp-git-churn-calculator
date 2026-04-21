using System.Text;
using GitChurnCalculator.Models;

namespace GitChurnCalculator.Console.Reporting;

public sealed class GithubActionsChurnReportGenerator : IChurnReportGenerator
{
    public string Generate(IReadOnlyList<FileChurnResult> results, string subtitle)
    {
        _ = subtitle;
        var sb = new StringBuilder();
        foreach (var r in results)
        {
            var kind = ChurnCiSeverity.GithubCommandKind(r.ChurnRiskScore);
            var path = ChurnCiEncoding.NormalizeFilePath(r.FilePath);
            var title = $"Churn risk {r.ChurnRiskScore:F4}";
            var message = ChurnCiEncoding.EncodeWorkflowCommandMessage(ChurnCiSeverity.BuildMessage(r));
            sb.Append("::").Append(kind)
                .Append(" file=").Append(path)
                .Append(",line=1,title=").Append(ChurnCiEncoding.EncodeWorkflowCommandMessage(title))
                .Append("::").Append(message).AppendLine();
        }

        return sb.ToString();
    }
}
