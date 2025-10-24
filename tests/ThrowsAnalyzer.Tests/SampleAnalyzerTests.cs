using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace ThrowsAnalyzer.Tests;

[TestClass]
public class SampleAnalyzerTests
{
    [TestMethod]
    public async Task MethodWithLowercaseName_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void myMethod()
                {
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("myMethod"));
    }

    [TestMethod]
    public async Task MethodWithUppercaseName_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MyMethod()
                {
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();

        if (compilation == null)
        {
            throw new InvalidOperationException("Compilation failed");
        }

        var analyzer = new SampleAnalyzer();
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