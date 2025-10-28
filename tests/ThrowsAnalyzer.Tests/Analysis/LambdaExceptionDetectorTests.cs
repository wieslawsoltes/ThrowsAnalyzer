using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using ThrowsAnalyzer.Analysis;
using LambdaContext = RoslynAnalyzer.Core.Analysis.Patterns.Lambda.LambdaContext;

namespace ThrowsAnalyzer.Tests.Analysis;

[TestClass]
public class LambdaExceptionDetectorTests
{
    [TestMethod]
    public async Task GetLambdaExpressions_SimpleLambda_ReturnsLambda()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x => x > 1);
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambdas = LambdaExceptionDetector.GetLambdaExpressions(methodNode).ToList();

        Assert.AreEqual(1, lambdas.Count);
    }

    [TestMethod]
    public async Task GetLambdaExpressions_ParenthesizedLambda_ReturnsLambda()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Select((x, i) => x + i);
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambdas = LambdaExceptionDetector.GetLambdaExpressions(methodNode).ToList();

        Assert.AreEqual(1, lambdas.Count);
    }

    [TestMethod]
    public async Task HasBlockBody_BlockBodyLambda_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        return x > 1;
                    });
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsTrue(LambdaExceptionDetector.HasBlockBody(lambda));
    }

    [TestMethod]
    public async Task HasExpressionBody_ExpressionBodyLambda_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x => x > 1);
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsTrue(LambdaExceptionDetector.HasExpressionBody(lambda));
    }

    [TestMethod]
    public async Task GetThrowStatements_LambdaWithThrow_ReturnsThrow()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        if (x < 0)
                            throw new InvalidOperationException();
                        return x > 1;
                    });
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();
        var throws = LambdaExceptionDetector.GetThrowStatements(lambda).ToList();

        Assert.AreEqual(1, throws.Count);
    }

    [TestMethod]
    public async Task GetThrowExpressions_LambdaWithThrowExpression_ReturnsThrow()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Select(x => x >= 0 ? x : throw new ArgumentException());
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();
        var throws = LambdaExceptionDetector.GetThrowExpressions(lambda).ToList();

        Assert.AreEqual(1, throws.Count);
    }

    [TestMethod]
    public async Task HasTryCatch_LambdaWithTryCatch_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Select(x =>
                    {
                        try
                        {
                            return x * 2;
                        }
                        catch (Exception)
                        {
                            return 0;
                        }
                    });
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsTrue(LambdaExceptionDetector.HasTryCatch(lambda));
    }

    [TestMethod]
    public async Task IsAsyncLambda_AsyncLambda_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                void Method()
                {
                    Func<Task<int>> func = async () =>
                    {
                        await Task.Delay(1);
                        return 42;
                    };
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsTrue(LambdaExceptionDetector.IsAsyncLambda(lambda));
    }

    [TestMethod]
    public async Task IsAsyncLambda_SyncLambda_ReturnsFalse()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x => x > 1);
                }
            }
            """;

        var (_, methodNode) = await GetCompilationAndMethod(source, "Method");
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsFalse(LambdaExceptionDetector.IsAsyncLambda(lambda));
    }

    [TestMethod]
    public async Task IsEventHandlerLambda_EventAssignment_ReturnsTrue()
    {
        var source = """
            using System;

            class TestClass
            {
                event EventHandler MyEvent;

                void Method()
                {
                    MyEvent += (sender, e) => Console.WriteLine("Event");
                }
            }
            """;

        var (compilation, methodNode) = await GetCompilationAndMethod(source, "Method");
        var semanticModel = compilation.GetSemanticModel(methodNode.SyntaxTree);
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        Assert.IsTrue(LambdaExceptionDetector.IsEventHandlerLambda(lambda, semanticModel));
    }

    [TestMethod]
    public async Task GetLambdaContext_LinqQuery_ReturnsLinqQuery()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x => x > 1);
                }
            }
            """;

        var (compilation, methodNode) = await GetCompilationAndMethod(source, "Method");
        var semanticModel = compilation.GetSemanticModel(methodNode.SyntaxTree);
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        var context = LambdaExceptionDetector.GetLambdaContext(lambda, semanticModel);

        Assert.AreEqual(LambdaContext.LinqQuery, context);
    }

    [TestMethod]
    public async Task GetLambdaExceptionInfo_Lambda_ReturnsCompleteInfo()
    {
        var source = """
            using System;
            using System.Linq;

            class TestClass
            {
                void Method()
                {
                    var items = new[] { 1, 2, 3 };
                    var result = items.Where(x =>
                    {
                        if (x < 0)
                            throw new InvalidOperationException();
                        return x > 1;
                    });
                }
            }
            """;

        var (compilation, methodNode) = await GetCompilationAndMethod(source, "Method");
        var semanticModel = compilation.GetSemanticModel(methodNode.SyntaxTree);
        var lambda = LambdaExceptionDetector.GetLambdaExpressions(methodNode).First();

        var info = LambdaExceptionDetector.GetLambdaExceptionInfo(lambda, semanticModel);

        Assert.IsNotNull(info);
        Assert.IsFalse(info.IsAsync);
        Assert.IsTrue(info.HasBlockBody);
        Assert.IsFalse(info.HasExpressionBody);
        Assert.AreEqual(1, info.ThrowCount);
        Assert.IsFalse(info.HasTryCatch);
        Assert.AreEqual(LambdaContext.LinqQuery, info.Context);
    }

    private static async Task<(Compilation, Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)>
        GetCompilationAndMethod(string source, string methodName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var root = await syntaxTree.GetRootAsync();

        var methodDecl = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == methodName);

        return (compilation, methodDecl);
    }
}
