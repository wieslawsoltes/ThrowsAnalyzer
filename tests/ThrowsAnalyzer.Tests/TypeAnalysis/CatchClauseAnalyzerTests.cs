using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Tests.TypeAnalysis;

[TestClass]
public class CatchClauseAnalyzerTests
{
    #region GetCatchClauses Tests

    [TestMethod]
    public async Task GetCatchClauses_SingleTypedCatch_ReturnsCorrectInfo()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (ArgumentException ex) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var catches = CatchClauseAnalyzer.GetCatchClauses(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, catches.Count);
        Assert.IsNotNull(catches[0].ExceptionType);
        Assert.AreEqual("System.ArgumentException", catches[0].ExceptionTypeName);
        Assert.IsFalse(catches[0].IsGeneralCatch);
        Assert.IsFalse(catches[0].HasFilter);
    }

    [TestMethod]
    public async Task GetCatchClauses_GeneralCatch_ReturnsCorrectInfo()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var catches = CatchClauseAnalyzer.GetCatchClauses(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, catches.Count);
        Assert.IsTrue(catches[0].IsGeneralCatch);
        Assert.IsFalse(catches[0].HasFilter);
    }

    [TestMethod]
    public async Task GetCatchClauses_CatchWithFilter_ReturnsCorrectInfo()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception ex) when (ex.Message != null) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var catches = CatchClauseAnalyzer.GetCatchClauses(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, catches.Count);
        Assert.IsTrue(catches[0].HasFilter);
        Assert.IsNotNull(catches[0].Filter);
    }

    [TestMethod]
    public async Task GetCatchClauses_MultipleCatches_ReturnsAll()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var catches = CatchClauseAnalyzer.GetCatchClauses(tryStatement, semanticModel).ToList();

        Assert.AreEqual(3, catches.Count);
        Assert.AreEqual("System.ArgumentException", catches[0].ExceptionTypeName);
        Assert.AreEqual("System.InvalidOperationException", catches[1].ExceptionTypeName);
        Assert.AreEqual("System.Exception", catches[2].ExceptionTypeName);
    }

    #endregion

    #region DetectOrderingIssues Tests

    [TestMethod]
    public async Task DetectOrderingIssues_CorrectOrder_ReturnsNoIssues()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (ArgumentException) { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var issues = CatchClauseAnalyzer.DetectOrderingIssues(tryStatement, semanticModel).ToList();

        Assert.AreEqual(0, issues.Count);
    }

    [TestMethod]
    public async Task DetectOrderingIssues_ExceptionBeforeArgumentException_ReportsIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception) { }
                    catch (ArgumentException) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var issues = CatchClauseAnalyzer.DetectOrderingIssues(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, issues.Count);
        Assert.AreEqual("System.ArgumentException", issues[0].UnreachableClause.ExceptionTypeName);
        Assert.AreEqual("System.Exception", issues[0].MaskedByClause.ExceptionTypeName);
    }

    [TestMethod]
    public async Task DetectOrderingIssues_GeneralCatchFirst_MasksAllOthers()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var issues = CatchClauseAnalyzer.DetectOrderingIssues(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, issues.Count);
        Assert.IsTrue(issues[0].MaskedByClause.IsGeneralCatch);
    }

    [TestMethod]
    public async Task DetectOrderingIssues_WithFilters_SkipsCheck()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception ex) when (ex.Message == "A") { }
                    catch (ArgumentException) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var issues = CatchClauseAnalyzer.DetectOrderingIssues(tryStatement, semanticModel).ToList();

        // Filters change reachability, so no issue should be reported
        Assert.AreEqual(0, issues.Count);
    }

    [TestMethod]
    public async Task DetectOrderingIssues_MultipleUnreachable_ReportsAll()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception) { }
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var issues = CatchClauseAnalyzer.DetectOrderingIssues(tryStatement, semanticModel).ToList();

        Assert.AreEqual(2, issues.Count);
    }

    #endregion

    #region DetectEmptyCatches Tests

    [TestMethod]
    public async Task DetectEmptyCatches_EmptyBlock_ReportsIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var emptyCatches = CatchClauseAnalyzer.DetectEmptyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, emptyCatches.Count);
    }

    [TestMethod]
    public async Task DetectEmptyCatches_BlockWithStatements_ReportsNoIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var emptyCatches = CatchClauseAnalyzer.DetectEmptyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(0, emptyCatches.Count);
    }

    [TestMethod]
    public async Task DetectEmptyCatches_MultipleEmpty_ReportsAll()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var emptyCatches = CatchClauseAnalyzer.DetectEmptyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(2, emptyCatches.Count);
    }

    #endregion

    #region DetectRethrowOnlyCatches Tests

    [TestMethod]
    public async Task DetectRethrowOnlyCatches_OnlyBareRethrow_ReportsIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var rethrowOnly = CatchClauseAnalyzer.DetectRethrowOnlyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, rethrowOnly.Count);
    }

    [TestMethod]
    public async Task DetectRethrowOnlyCatches_RethrowWithLogging_ReportsNoIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var rethrowOnly = CatchClauseAnalyzer.DetectRethrowOnlyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(0, rethrowOnly.Count);
    }

    [TestMethod]
    public async Task DetectRethrowOnlyCatches_EmptyBlock_ReportsNoIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var rethrowOnly = CatchClauseAnalyzer.DetectRethrowOnlyCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(0, rethrowOnly.Count);
    }

    #endregion

    #region DetectOverlyBroadCatches Tests

    [TestMethod]
    public async Task DetectOverlyBroadCatches_CatchException_ReportsIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var broadCatches = CatchClauseAnalyzer.DetectOverlyBroadCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(1, broadCatches.Count);
        Assert.AreEqual("System.Exception", broadCatches[0].ExceptionTypeName);
    }

    [TestMethod]
    public async Task DetectOverlyBroadCatches_CatchSpecificException_ReportsNoIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch (ArgumentException) { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var broadCatches = CatchClauseAnalyzer.DetectOverlyBroadCatches(tryStatement, semanticModel).ToList();

        Assert.AreEqual(0, broadCatches.Count);
    }

    [TestMethod]
    public async Task DetectOverlyBroadCatches_GeneralCatch_ReportsIssue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch { }
                }
            }
            """;

        var (semanticModel, tryStatement) = await GetTryStatementNode(source);
        var broadCatches = CatchClauseAnalyzer.DetectOverlyBroadCatches(tryStatement, semanticModel).ToList();

        // General catch resolves to System.Exception, which is considered overly broad
        Assert.AreEqual(1, broadCatches.Count);
    }

    #endregion

    // Helper method
    private static async Task<(SemanticModel, TryStatementSyntax)> GetTryStatementNode(string source, int tryIndex = 0)
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();
        var tree = compilation!.SyntaxTrees.First();
        var root = await tree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(tree);

        var tryStatement = root.DescendantNodes()
            .OfType<TryStatementSyntax>()
            .ElementAt(tryIndex);

        return (semanticModel, tryStatement);
    }

    private static Project CreateProject(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddDocument(documentId, "Test.cs", source);

        return solution.GetProject(projectId)!;
    }
}
