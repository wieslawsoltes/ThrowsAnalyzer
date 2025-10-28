using System;

namespace DisposalPatterns.Examples;

/// <summary>
/// Sample project demonstrating DisposableAnalyzer diagnostics.
///
/// This project contains intentional disposal issues to showcase
/// the analyzer's capabilities. Build this project to see warnings.
///
/// Categories:
/// - 01_BasicDisposalIssues: DISP001-006 (local variables, using statements)
/// - 02_FieldDisposal: DISP002, DISP007-010 (IDisposable implementation)
/// - 03_AsyncDisposal: DISP011-013 (IAsyncDisposable patterns)
/// - 04_SpecialContexts: DISP014-018 (lambdas, iterators, parameters)
/// - 05_AntiPatterns: DISP019-020, DISP030 (finalizers, collections)
/// - 06_CrossMethodAnalysis: DISP021-025 (call graph, control flow)
/// - 07_BestPractices: DISP026-029 (design patterns, recommendations)
///
/// Build the project to see analyzer warnings:
///   dotnet build
///
/// Apply fixes in your IDE (Visual Studio, Rider, VS Code with C# extension)
/// by clicking on the light bulb icons or using quick fix shortcuts.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("DisposableAnalyzer Sample Project");
        Console.WriteLine("==================================");
        Console.WriteLine();
        Console.WriteLine("This project contains intentional disposal issues.");
        Console.WriteLine("Build the project to see DisposableAnalyzer warnings.");
        Console.WriteLine();
        Console.WriteLine("Diagnostic Categories:");
        Console.WriteLine("  DISP001-006  : Basic disposal issues");
        Console.WriteLine("  DISP007-010  : Field disposal and IDisposable");
        Console.WriteLine("  DISP011-013  : Async disposal (IAsyncDisposable)");
        Console.WriteLine("  DISP014-018  : Special contexts (lambdas, iterators)");
        Console.WriteLine("  DISP019-020  : Anti-patterns (finalizers, collections)");
        Console.WriteLine("  DISP021-025  : Cross-method analysis");
        Console.WriteLine("  DISP026-030  : Best practices and design patterns");
        Console.WriteLine();
        Console.WriteLine("See the individual example files for details.");
        Console.WriteLine();
        Console.WriteLine("Quick Start:");
        Console.WriteLine("  1. Open this project in your IDE");
        Console.WriteLine("  2. Build the project (dotnet build)");
        Console.WriteLine("  3. Review warnings in the Error List/Problems panel");
        Console.WriteLine("  4. Click on warnings to see code fixes");
        Console.WriteLine("  5. Apply fixes to resolve issues");
    }
}
