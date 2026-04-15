namespace GitChurnCalculator.Console.Cli;

public static class ChurnOutputWriter
{
    public static async Task WriteAsync(FileInfo? output, string text)
    {
        if (output is null)
        {
            global::System.Console.Write(text);
            return;
        }

        await File.WriteAllTextAsync(output.FullName, text);
        global::System.Console.Error.WriteLine($"Output written to: {output.FullName}");
    }
}
