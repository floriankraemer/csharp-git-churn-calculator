using GitChurnCalculator.Services;
using Xunit;

namespace GitChurnCalculator.Tests;

public class VsCoverageXmlParserTests
{
    [Fact]
    public void Parse_BasicVsCoverageXml_ExtractsLineCoverage()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <results>
              <modules>
                <module name="MyApp.dll" path="MyApp.dll" block_coverage="50.00" line_coverage="50.00"
                        blocks_covered="5" blocks_not_covered="5" lines_covered="3" lines_partially_covered="0" lines_not_covered="3">
                  <functions>
                    <function id="1" token="0x6000001" name="DoWork()" namespace="MyApp" type_name="Worker"
                              block_coverage="50.00" line_coverage="50.00" blocks_covered="2" blocks_not_covered="2"
                              lines_covered="2" lines_partially_covered="0" lines_not_covered="2">
                      <ranges>
                        <range source_id="1" start_line="10" end_line="10" start_column="9" end_column="10" covered="yes" />
                        <range source_id="1" start_line="11" end_line="11" start_column="13" end_column="30" covered="yes" />
                        <range source_id="1" start_line="12" end_line="12" start_column="13" end_column="14" covered="no" />
                        <range source_id="1" start_line="13" end_line="13" start_column="17" end_column="26" covered="no" />
                      </ranges>
                    </function>
                    <function id="2" token="0x6000002" name="GetName()" namespace="MyApp" type_name="Helper"
                              block_coverage="100.00" line_coverage="100.00" blocks_covered="2" blocks_not_covered="0"
                              lines_covered="2" lines_partially_covered="0" lines_not_covered="0">
                      <ranges>
                        <range source_id="2" start_line="5" end_line="5" start_column="9" end_column="10" covered="yes" />
                        <range source_id="2" start_line="6" end_line="6" start_column="13" end_column="30" covered="yes" />
                      </ranges>
                    </function>
                  </functions>
                  <source_files>
                    <source_file id="1" path="C:\dev\MyApp\Worker.cs" />
                    <source_file id="2" path="C:\dev\MyApp\Helper.cs" />
                  </source_files>
                </module>
              </modules>
            </results>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = new VsCoverageXmlParser();
            var result = parser.Parse(tempFile);

            Assert.Equal(2, result.Count);

            // Worker.cs: lines 10,11 covered; 12,13 not covered = 50%
            Assert.True(result.ContainsKey(@"C:\dev\MyApp\Worker.cs"));
            Assert.Equal(50.0, result[@"C:\dev\MyApp\Worker.cs"]);

            // Helper.cs: lines 5,6 both covered = 100%
            Assert.True(result.ContainsKey(@"C:\dev\MyApp\Helper.cs"));
            Assert.Equal(100.0, result[@"C:\dev\MyApp\Helper.cs"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_MultiModule_SourceIdsAreScopedPerModule()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <results>
              <modules>
                <module name="ModuleA.dll" path="ModuleA.dll" block_coverage="100.00" line_coverage="100.00"
                        blocks_covered="2" blocks_not_covered="0" lines_covered="2" lines_partially_covered="0" lines_not_covered="0">
                  <functions>
                    <function id="1" token="0x6000001" name="A()" namespace="NS" type_name="ClassA"
                              block_coverage="100.00" line_coverage="100.00" blocks_covered="2" blocks_not_covered="0"
                              lines_covered="2" lines_partially_covered="0" lines_not_covered="0">
                      <ranges>
                        <range source_id="1" start_line="1" end_line="1" start_column="1" end_column="10" covered="yes" />
                        <range source_id="1" start_line="2" end_line="2" start_column="1" end_column="10" covered="yes" />
                      </ranges>
                    </function>
                  </functions>
                  <source_files>
                    <source_file id="1" path="C:\src\FileA.cs" />
                  </source_files>
                </module>
                <module name="ModuleB.dll" path="ModuleB.dll" block_coverage="0.00" line_coverage="0.00"
                        blocks_covered="0" blocks_not_covered="2" lines_covered="0" lines_partially_covered="0" lines_not_covered="2">
                  <functions>
                    <function id="1" token="0x6000001" name="B()" namespace="NS" type_name="ClassB"
                              block_coverage="0.00" line_coverage="0.00" blocks_covered="0" blocks_not_covered="2"
                              lines_covered="0" lines_partially_covered="0" lines_not_covered="2">
                      <ranges>
                        <range source_id="1" start_line="1" end_line="1" start_column="1" end_column="10" covered="no" />
                        <range source_id="1" start_line="2" end_line="2" start_column="1" end_column="10" covered="no" />
                      </ranges>
                    </function>
                  </functions>
                  <source_files>
                    <source_file id="1" path="C:\src\FileB.cs" />
                  </source_files>
                </module>
              </modules>
            </results>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = new VsCoverageXmlParser();
            var result = parser.Parse(tempFile);

            Assert.Equal(2, result.Count);
            // Both modules use source_id="1" but map to different files
            Assert.Equal(100.0, result[@"C:\src\FileA.cs"]);
            Assert.Equal(0.0, result[@"C:\src\FileB.cs"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_MultipleFunctionsSameFile_DeduplicatesLines()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <results>
              <modules>
                <module name="MyApp.dll" path="MyApp.dll" block_coverage="50.00" line_coverage="50.00"
                        blocks_covered="3" blocks_not_covered="3" lines_covered="2" lines_partially_covered="0" lines_not_covered="1">
                  <functions>
                    <function id="1" token="0x6000001" name="Foo()" namespace="MyApp" type_name="MyClass"
                              block_coverage="100.00" line_coverage="100.00" blocks_covered="2" blocks_not_covered="0"
                              lines_covered="2" lines_partially_covered="0" lines_not_covered="0">
                      <ranges>
                        <range source_id="1" start_line="10" end_line="10" start_column="9" end_column="10" covered="yes" />
                        <range source_id="1" start_line="11" end_line="11" start_column="13" end_column="30" covered="yes" />
                      </ranges>
                    </function>
                    <function id="2" token="0x6000002" name="Bar()" namespace="MyApp" type_name="MyClass"
                              block_coverage="0.00" line_coverage="0.00" blocks_covered="0" blocks_not_covered="2"
                              lines_covered="0" lines_partially_covered="0" lines_not_covered="1">
                      <ranges>
                        <range source_id="1" start_line="11" end_line="11" start_column="13" end_column="30" covered="no" />
                        <range source_id="1" start_line="20" end_line="20" start_column="9" end_column="10" covered="no" />
                      </ranges>
                    </function>
                  </functions>
                  <source_files>
                    <source_file id="1" path="C:\src\MyClass.cs" />
                  </source_files>
                </module>
              </modules>
            </results>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = new VsCoverageXmlParser();
            var result = parser.Parse(tempFile);

            Assert.Single(result);
            // Lines: 10 (covered), 11 (covered from Foo, not from Bar - covered wins via HashSet),
            //        20 (not covered)
            // Total 3 unique lines, 2 covered => 66.67%
            var coverage = result[@"C:\src\MyClass.cs"];
            Assert.Equal(66.67, Math.Round(coverage, 2));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_EmptyModule_ReturnsEmpty()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <results>
              <modules>
                <module name="Empty.dll" path="Empty.dll" block_coverage="0.00" line_coverage="0.00"
                        blocks_covered="0" blocks_not_covered="0" lines_covered="0" lines_partially_covered="0" lines_not_covered="0">
                  <functions />
                  <source_files />
                </module>
              </modules>
            </results>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = new VsCoverageXmlParser();
            var result = parser.Parse(tempFile);

            Assert.Empty(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void MapToTrackedFiles_MatchesAbsoluteWindowsPaths()
    {
        var coverage = new Dictionary<string, double>
        {
            [@"C:\dev\project\Services\Utils\Worker.cs"] = 75.0,
            [@"C:\dev\project\Services\Helper.cs"] = 50.0,
        };
        var gitFiles = new List<string> { "Services/Utils/Worker.cs", "Services/Helper.cs", "README.md" };

        var parser = new VsCoverageXmlParser();
        var mapped = parser.MapToTrackedFiles(coverage, gitFiles);

        Assert.Equal(2, mapped.Count);
        Assert.Equal(75.0, mapped["Services/Utils/Worker.cs"]);
        Assert.Equal(50.0, mapped["Services/Helper.cs"]);
    }
}

public class AutoDetectCoverageParserTests
{
    [Fact]
    public void CreateParserFor_CoberturaXml_ReturnsCoberturaParser()
    {
        var xml = """
            <?xml version="1.0"?>
            <coverage line-rate="0.5">
              <packages />
            </coverage>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = AutoDetectCoverageParser.CreateParserFor(tempFile);
            Assert.IsType<CoberturaXmlParser>(parser);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CreateParserFor_VsCoverageXml_ReturnsVsCoverageParser()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <results>
              <modules />
            </results>
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var parser = AutoDetectCoverageParser.CreateParserFor(tempFile);
            Assert.IsType<VsCoverageXmlParser>(parser);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CreateParserFor_UnknownFormat_Throws()
    {
        var xml = """
            <?xml version="1.0"?>
            <unknown />
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, xml);
            var ex = Assert.Throws<InvalidOperationException>(() => AutoDetectCoverageParser.CreateParserFor(tempFile));
            Assert.Contains("unknown", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
