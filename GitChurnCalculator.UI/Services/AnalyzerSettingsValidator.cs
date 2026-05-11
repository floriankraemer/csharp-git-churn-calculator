using System.Text.RegularExpressions;
using GitChurnCalculator.UI.Models;

namespace GitChurnCalculator.UI.Services;

public static class AnalyzerSettingsValidator
{
    public static bool TryValidate(AppSettings settings, out string? error)
    {
        if (!TryValidateRegex(settings.IncludePattern, "Include pattern", out error))
            return false;

        if (!TryValidateRegex(settings.ExcludePattern, "Exclude pattern", out error))
            return false;

        if (!string.IsNullOrWhiteSpace(settings.CoverageFilePath) && !File.Exists(settings.CoverageFilePath))
        {
            error = $"Coverage file does not exist: {settings.CoverageFilePath}";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateRegex(string? pattern, string label, out string? error)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = null;
            return true;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant);
            error = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            error = $"{label} is not a valid regular expression: {ex.Message}";
            return false;
        }
    }
}
