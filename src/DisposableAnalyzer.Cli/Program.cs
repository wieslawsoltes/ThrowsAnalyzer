using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace DisposableAnalyzer.Cli;

/// <summary>
/// Entry point for the DisposableAnalyzer CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("DisposableAnalyzer - IDisposable pattern analysis tool for C# projects");

        // TODO: Add commands here
        // var analyzeCommand = new Command("analyze", "Analyze a project or solution for disposal issues");
        // rootCommand.AddCommand(analyzeCommand);

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("DisposableAnalyzer CLI");
            Console.WriteLine("Run 'disposable-analyzer --help' for usage information.");
            Console.WriteLine();
            Console.WriteLine("Commands will be implemented in future phases.");
        });

        return await rootCommand.InvokeAsync(args);
    }
}
