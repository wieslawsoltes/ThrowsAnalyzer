using System.CommandLine;
using ThrowsAnalyzer.Cli.Commands;

namespace ThrowsAnalyzer.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("ThrowsAnalyzer CLI - Analyze projects for exception handling diagnostics")
        {
            AnalyzeCommand.Create()
        };

        return await rootCommand.InvokeAsync(args);
    }
}
