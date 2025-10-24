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
    public async Task MethodWithThrowStatement_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void ThrowingMethod()
                {
                    throw new System.Exception("Error");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("ThrowingMethod"));
    }

    [TestMethod]
    public async Task MethodWithoutThrowStatement_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void SafeMethod()
                {
                    var x = 42;
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task MethodWithThrowExpression_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                int ThrowingProperty() => throw new System.Exception("Error");
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("ThrowingProperty"));
    }

    [TestMethod]
    public async Task MethodWithMultipleThrows_ShouldReportOneDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MultipleThrows(int x)
                {
                    if (x < 0)
                        throw new System.ArgumentException("Negative");
                    if (x > 100)
                        throw new System.ArgumentException("Too large");
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("MultipleThrows"));
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