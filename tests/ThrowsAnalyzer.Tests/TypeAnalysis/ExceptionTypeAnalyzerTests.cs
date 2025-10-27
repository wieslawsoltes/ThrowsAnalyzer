using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.Tests.TypeAnalysis;

[TestClass]
public class ExceptionTypeAnalyzerTests
{
    [TestMethod]
    public async Task GetThrownExceptionType_ObjectCreation_ReturnsCorrectType()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new ArgumentException();
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.ArgumentException", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task GetThrownExceptionType_VariableThrow_ReturnsCorrectType()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    var ex = new InvalidOperationException();
                    throw ex;
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source, 0);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.InvalidOperationException", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task GetThrownExceptionType_MethodInvocation_ReturnsCorrectType()
    {
        var source = """
            using System;

            class TestClass
            {
                Exception GetException() => new NotImplementedException();

                void Method()
                {
                    throw GetException();
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.Exception", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task GetThrownExceptionType_ConditionalExpression_ReturnsCommonType()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method(bool condition)
                {
                    throw condition ? new ArgumentException() : new ArgumentNullException();
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        // Both ArgumentException and ArgumentNullException derive from ArgumentException
        Assert.IsTrue(exceptionType.ToDisplayString().Contains("Exception"));
    }

    [TestMethod]
    public async Task GetThrownExceptionType_BareRethrow_ReturnsNull()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try { }
                    catch { throw; }
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNull(exceptionType);
    }

    [TestMethod]
    public async Task GetThrownExceptionType_ThrowExpression_ReturnsCorrectType()
    {
        var source = """
            using System;

            class TestClass
            {
                int Method(string value) => value ?? throw new ArgumentNullException();
            }
            """;

        var (semanticModel, throwNode) = await GetThrowExpressionNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.ArgumentNullException", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task GetCaughtExceptionType_TypedCatch_ReturnsCorrectType()
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

        var (semanticModel, catchClause) = await GetCatchClauseNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.ArgumentException", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task GetCaughtExceptionType_GeneralCatch_ReturnsSystemException()
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

        var (semanticModel, catchClause) = await GetCatchClauseNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetCaughtExceptionType(catchClause, semanticModel);

        Assert.IsNotNull(exceptionType);
        Assert.AreEqual("System.Exception", exceptionType.ToDisplayString());
    }

    [TestMethod]
    public async Task IsExceptionType_ArgumentException_ReturnsTrue()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new ArgumentException();
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        var isException = ExceptionTypeAnalyzer.IsExceptionType(exceptionType, semanticModel.Compilation);

        Assert.IsTrue(isException);
    }

    [TestMethod]
    public async Task IsExceptionType_String_ReturnsFalse()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    var x = "test";
                }
            }
            """;

        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();
        var stringType = compilation!.GetTypeByMetadataName("System.String");

        Assert.IsNotNull(stringType);
        var isException = ExceptionTypeAnalyzer.IsExceptionType(stringType, compilation);

        Assert.IsFalse(isException);
    }

    [TestMethod]
    public async Task IsAssignableTo_ArgumentExceptionToException_ReturnsTrue()
    {
        var source = """
            using System;

            class TestClass { }
            """;

        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();

        var argumentException = compilation!.GetTypeByMetadataName("System.ArgumentException");
        var exception = compilation.GetTypeByMetadataName("System.Exception");

        Assert.IsNotNull(argumentException);
        Assert.IsNotNull(exception);

        var isAssignable = ExceptionTypeAnalyzer.IsAssignableTo(argumentException, exception, compilation);

        Assert.IsTrue(isAssignable);
    }

    [TestMethod]
    public async Task IsAssignableTo_ExceptionToArgumentException_ReturnsFalse()
    {
        var source = """
            using System;

            class TestClass { }
            """;

        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();

        var argumentException = compilation!.GetTypeByMetadataName("System.ArgumentException");
        var exception = compilation.GetTypeByMetadataName("System.Exception");

        Assert.IsNotNull(argumentException);
        Assert.IsNotNull(exception);

        var isAssignable = ExceptionTypeAnalyzer.IsAssignableTo(exception, argumentException, compilation);

        Assert.IsFalse(isAssignable);
    }

    [TestMethod]
    public async Task GetExceptionHierarchy_ArgumentException_ReturnsCorrectChain()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new ArgumentException();
                }
            }
            """;

        var (semanticModel, throwNode) = await GetSemanticModelAndThrowNode(source);
        var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);

        Assert.IsNotNull(exceptionType);
        var hierarchy = ExceptionTypeAnalyzer.GetExceptionHierarchy(exceptionType, semanticModel.Compilation).ToList();

        Assert.IsTrue(hierarchy.Count >= 2);
        Assert.AreEqual("System.ArgumentException", hierarchy[0].ToDisplayString());
        Assert.IsTrue(hierarchy.Any(t => t.ToDisplayString() == "System.Exception"));
    }

    // Helper methods
    private static async Task<(SemanticModel, SyntaxNode)> GetSemanticModelAndThrowNode(string source, int throwIndex = 0)
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();
        var tree = compilation!.SyntaxTrees.First();
        var root = await tree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(tree);

        var throwNode = root.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .ElementAt(throwIndex);

        return (semanticModel, throwNode);
    }

    private static async Task<(SemanticModel, SyntaxNode)> GetThrowExpressionNode(string source)
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();
        var tree = compilation!.SyntaxTrees.First();
        var root = await tree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(tree);

        var throwNode = root.DescendantNodes()
            .OfType<ThrowExpressionSyntax>()
            .First();

        return (semanticModel, throwNode);
    }

    private static async Task<(SemanticModel, CatchClauseSyntax)> GetCatchClauseNode(string source, int catchIndex = 0)
    {
        var project = CreateProject(source);
        var compilation = await project.GetCompilationAsync();
        var tree = compilation!.SyntaxTrees.First();
        var root = await tree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(tree);

        var catchClause = root.DescendantNodes()
            .OfType<CatchClauseSyntax>()
            .ElementAt(catchIndex);

        return (semanticModel, catchClause);
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
