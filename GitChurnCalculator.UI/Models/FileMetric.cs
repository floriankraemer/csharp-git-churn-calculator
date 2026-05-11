namespace GitChurnCalculator.UI.Models;

public sealed class FileMetric
{
    public FileMetric(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }
}
