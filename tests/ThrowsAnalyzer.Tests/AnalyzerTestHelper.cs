using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ThrowsAnalyzer.Tests;

public static class AnalyzerTestHelper
{
    public static async Task<Diagnostic[]> GetDiagnosticsAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();

        if (compilation == null)
        {
            throw new InvalidOperationException("Compilation failed");
        }

        var analyzer = new TAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.ToArray();
    }

    private static Project CreateProject(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddDocument(documentId, "Test.cs", source);

        return solution.GetProject(projectId)!;
    }
}
