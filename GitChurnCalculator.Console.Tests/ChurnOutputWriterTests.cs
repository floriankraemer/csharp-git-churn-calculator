using GitChurnCalculator.Console.Cli;
using Xunit;

namespace GitChurnCalculator.Console.Tests;

public class ChurnOutputWriterTests
{
    [Fact]
    public async Task WriteAsync_WithOutputFile_WritesContent()
    {
        var path = Path.Combine(Path.GetTempPath(), "churn-out-" + Guid.NewGuid() + ".txt");
        try
        {
            await ChurnOutputWriter.WriteAsync(new FileInfo(path), "hello");
            Assert.Equal("hello", await File.ReadAllTextAsync(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_NullOutput_WritesToConsoleOut()
    {
        var sw = new StringWriter();
        var oldOut = global::System.Console.Out;
        global::System.Console.SetOut(sw);
        try
        {
            await ChurnOutputWriter.WriteAsync(null, "xy");
            Assert.Equal("xy", sw.ToString());
        }
        finally
        {
            global::System.Console.SetOut(oldOut);
        }
    }
}
