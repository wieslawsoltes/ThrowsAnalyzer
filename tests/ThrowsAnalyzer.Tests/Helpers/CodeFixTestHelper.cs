using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ThrowsAnalyzer.Tests;

/// <summary>
/// Helper class for testing code fix providers.
/// </summary>
public static class CodeFixTestHelper
{
    /// <summary>
    /// Verifies that a code fix provider correctly transforms source code.
    /// </summary>
    /// <typeparam name="TAnalyzer">The analyzer type</typeparam>
    /// <typeparam name="TCodeFix">The code fix provider type</typeparam>
    /// <param name="source">The source code with the diagnostic</param>
    /// <param name="expected">The expected code after applying the fix</param>
    /// <param name="codeFixIndex">The index of the code fix to apply (when multiple fixes are available)</param>
    public static async Task VerifyCodeFixAsync<TAnalyzer, TCodeFix>(
        string source,
        string expected,
        int codeFixIndex = 0)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TAnalyzer>(source);

        if (diagnostics.Length == 0)
        {
            throw new System.Exception("No diagnostics found. Cannot test code fix without a diagnostic.");
        }

        var document = CreateDocument(source);
        var codeFix = new TCodeFix();

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await codeFix.RegisterCodeFixesAsync(context);

        if (actions.Count == 0)
        {
            throw new System.Exception("No code fixes were registered.");
        }

        if (codeFixIndex >= actions.Count)
        {
            throw new System.Exception($"Code fix index {codeFixIndex} is out of range. Only {actions.Count} fixes available.");
        }

        // Apply the code fix
        var operations = await actions[codeFixIndex].GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

        if (operation == null)
        {
            throw new System.Exception("Code fix did not produce an ApplyChangesOperation.");
        }

        var changedSolution = operation.ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id);

        if (changedDocument == null)
        {
            throw new System.Exception("Could not find changed document in solution.");
        }

        var actual = await changedDocument.GetSyntaxRootAsync();
        var actualText = actual?.ToFullString() ?? string.Empty;

        // Normalize whitespace for comparison
        var expectedNormalized = NormalizeWhitespace(expected);
        var actualNormalized = NormalizeWhitespace(actualText);

        if (expectedNormalized != actualNormalized)
        {
            throw new System.Exception($"Code fix did not produce expected result.\n\nExpected:\n{expected}\n\nActual:\n{actualText}");
        }
    }

    /// <summary>
    /// Verifies that a code fix provider offers the expected number of fixes.
    /// </summary>
    public static async Task<int> GetCodeFixCountAsync<TAnalyzer, TCodeFix>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TAnalyzer>(source);

        if (diagnostics.Length == 0)
        {
            return 0;
        }

        var document = CreateDocument(source);
        var codeFix = new TCodeFix();

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await codeFix.RegisterCodeFixesAsync(context);

        return actions.Count;
    }

    /// <summary>
    /// Verifies that no code fix is offered for the given source.
    /// </summary>
    public static async Task VerifyNoCodeFixAsync<TAnalyzer, TCodeFix>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var count = await GetCodeFixCountAsync<TAnalyzer, TCodeFix>(source);

        if (count > 0)
        {
            throw new System.Exception($"Expected no code fixes, but found {count}.");
        }
    }

    private static Document CreateDocument(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddDocument(documentId, "Test.cs", source);

        return solution.GetDocument(documentId)!;
    }

    private static string NormalizeWhitespace(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        return root.NormalizeWhitespace().ToFullString();
    }
}
