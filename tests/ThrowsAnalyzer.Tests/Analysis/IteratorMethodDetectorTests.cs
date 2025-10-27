using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using ThrowsAnalyzer.Analysis;

namespace ThrowsAnalyzer.Tests.Analysis;

[TestClass]
public class IteratorMethodDetectorTests
{
    [TestMethod]
    public async Task IsIteratorMethod_YieldReturnMethod_ReturnsTrue()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    yield return 2;
                }
            }
            """;

        var (_, method, methodNode) = await GetCompilationAndMethod(source, "Method");
        Assert.IsTrue(IteratorMethodDetector.IsIteratorMethod(method, methodNode));
    }

    [TestMethod]
    public async Task IsIteratorMethod_YieldBreakMethod_ReturnsTrue()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield break;
                }
            }
            """;

        var (_, method, methodNode) = await GetCompilationAndMethod(source, "Method");
        Assert.IsTrue(IteratorMethodDetector.IsIteratorMethod(method, methodNode));
    }

    [TestMethod]
    public async Task IsIteratorMethod_NonIteratorMethod_ReturnsFalse()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    return new List<int> { 1, 2, 3 };
                }
            }
            """;

        var (_, method, methodNode) = await GetCompilationAndMethod(source, "Method");
        Assert.IsFalse(IteratorMethodDetector.IsIteratorMethod(method, methodNode));
    }

    [TestMethod]
    public async Task ReturnsEnumerable_IEnumerableMethod_ReturnsTrue()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                }
            }
            """;

        var (compilation, method, _) = await GetCompilationAndMethod(source, "Method");
        Assert.IsTrue(IteratorMethodDetector.ReturnsEnumerable(method, compilation));
    }

    [TestMethod]
    public async Task ReturnsEnumerable_IEnumeratorMethod_ReturnsTrue()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerator<int> Method()
                {
                    yield return 1;
                }
            }
            """;

        var (compilation, method, _) = await GetCompilationAndMethod(source, "Method");
        Assert.IsTrue(IteratorMethodDetector.ReturnsEnumerable(method, compilation));
    }

    [TestMethod]
    public async Task ReturnsEnumerable_NonEnumerableMethod_ReturnsFalse()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                int Method()
                {
                    return 42;
                }
            }
            """;

        var (compilation, method, _) = await GetCompilationAndMethod(source, "Method");
        Assert.IsFalse(IteratorMethodDetector.ReturnsEnumerable(method, compilation));
    }

    [TestMethod]
    public async Task GetYieldReturnStatements_HasYields_ReturnsStatements()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    yield return 2;
                    yield break;
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var yieldReturns = IteratorMethodDetector.GetYieldReturnStatements(body).ToList();

        Assert.AreEqual(2, yieldReturns.Count);
    }

    [TestMethod]
    public async Task GetYieldBreakStatements_HasYieldBreak_ReturnsStatements()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    if (true)
                        yield break;
                    yield return 1;
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var yieldBreaks = IteratorMethodDetector.GetYieldBreakStatements(body).ToList();

        Assert.AreEqual(1, yieldBreaks.Count);
    }

    [TestMethod]
    public async Task IsThrowBeforeFirstYield_ThrowBeforeYield_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    throw new InvalidOperationException();
                    yield return 1;
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var throwStmt = body.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
            .First();

        Assert.IsTrue(IteratorMethodDetector.IsThrowBeforeFirstYield(throwStmt, body));
    }

    [TestMethod]
    public async Task IsThrowBeforeFirstYield_ThrowAfterYield_ReturnsFalse()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    throw new InvalidOperationException();
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var throwStmt = body.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
            .First();

        Assert.IsFalse(IteratorMethodDetector.IsThrowBeforeFirstYield(throwStmt, body));
    }

    [TestMethod]
    public async Task IsThrowBeforeFirstYield_NoYield_ReturnsTrue()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var throwStmt = body.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax>()
            .First();

        // All throws are before first yield if there's no yield
        Assert.IsTrue(IteratorMethodDetector.IsThrowBeforeFirstYield(throwStmt, body));
    }

    [TestMethod]
    public async Task HasYieldInTryBlock_YieldInTry_ReturnsTrue()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        yield return 1;
                    }
                    finally
                    {
                        // Cleanup
                    }
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var tryStmt = body.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TryStatementSyntax>()
            .First();

        Assert.IsTrue(IteratorMethodDetector.HasYieldInTryBlock(tryStmt));
    }

    [TestMethod]
    public async Task HasYieldInTryBlock_NoYieldInTry_ReturnsFalse()
    {
        var source = """
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        // No yield
                    }
                    finally
                    {
                        // Cleanup
                    }
                    yield return 1;
                }
            }
            """;

        var (_, _, methodNode) = await GetCompilationAndMethod(source, "Method");
        var body = IteratorMethodDetector.GetMethodBody(methodNode);
        var tryStmt = body.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TryStatementSyntax>()
            .First();

        Assert.IsFalse(IteratorMethodDetector.HasYieldInTryBlock(tryStmt));
    }

    [TestMethod]
    public async Task GetIteratorMethodInfo_IteratorMethod_ReturnsCompleteInfo()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    yield return 2;
                    yield break;
                    throw new Exception();
                }
            }
            """;

        var (compilation, method, methodNode) = await GetCompilationAndMethod(source, "Method");
        var info = IteratorMethodDetector.GetIteratorMethodInfo(method, methodNode, compilation);

        Assert.IsTrue(info.IsIterator);
        Assert.IsTrue(info.ReturnsEnumerable);
        Assert.AreEqual(2, info.YieldReturnCount);
        Assert.AreEqual(1, info.YieldBreakCount);
        Assert.AreEqual(1, info.ThrowCount);
    }

    private static async Task<(Compilation, IMethodSymbol, Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)>
        GetCompilationAndMethod(string source, string methodName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var root = await syntaxTree.GetRootAsync();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var methodDecl = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == methodName);

        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);

        return (compilation, methodSymbol, methodDecl);
    }
}
