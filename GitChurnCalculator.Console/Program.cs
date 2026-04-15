using System.CommandLine;
using GitChurnCalculator.Console.Cli;

var app = new ChurnAnalysisApp();
var rootCommand = ChurnCliRootCommand.Create(app);

return await rootCommand.InvokeAsync(args);
