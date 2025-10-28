using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Analysis.CallGraph
{
    public class CallGraphBuilderTests
    {
        private static Compilation CreateCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        }

        private static IMethodSymbol GetMethodSymbol(Compilation compilation, string methodName)
        {
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == methodName);

            return model.GetDeclaredSymbol(methodDecl)!;
        }

        [Fact]
        public async Task BuildAsync_WithSimpleMethodCall_CreatesEdge()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Caller()
    {
        Callee();
    }

    void Callee()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildAsync();

            // Assert
            graph.NodeCount.Should().Be(2);
            graph.EdgeCount.Should().Be(1);

            var caller = GetMethodSymbol(compilation, "Caller");
            var callerNode = graph.GetOrAddNode(caller);
            callerNode.Callees.Should().HaveCount(1);
        }

        [Fact]
        public async Task BuildAsync_WithMultipleMethodCalls_CreatesMultipleEdges()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1()
    {
        Method2();
        Method3();
    }

    void Method2()
    {
        Method3();
    }

    void Method3()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildAsync();

            // Assert
            graph.NodeCount.Should().Be(3);
            graph.EdgeCount.Should().Be(3); // Method1->Method2, Method1->Method3, Method2->Method3

            var method1 = GetMethodSymbol(compilation, "Method1");
            var method1Node = graph.GetOrAddNode(method1);
            method1Node.Callees.Should().HaveCount(2);

            var method2 = GetMethodSymbol(compilation, "Method2");
            var method2Node = graph.GetOrAddNode(method2);
            method2Node.Callees.Should().HaveCount(1);
        }

        [Fact]
        public async Task BuildAsync_WithConstructorCall_CreatesEdge()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method()
    {
        var obj = new OtherClass();
    }
}

class OtherClass
{
    public OtherClass()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildAsync();

            // Assert
            graph.NodeCount.Should().Be(2);
            graph.EdgeCount.Should().Be(1);

            var method = GetMethodSymbol(compilation, "Method");
            var methodNode = graph.GetOrAddNode(method);
            methodNode.Callees.Should().HaveCount(1);
            methodNode.Callees[0].Target.Method.MethodKind.Should().Be(MethodKind.Constructor);
        }

        [Fact]
        public async Task BuildAsync_WithLocalFunction_IncludesLocalFunction()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method()
    {
        LocalFunc();

        void LocalFunc()
        {
        }
    }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildAsync();

            // Assert
            graph.NodeCount.Should().Be(2); // Method and LocalFunc
            graph.EdgeCount.Should().Be(1); // Method -> LocalFunc

            var method = GetMethodSymbol(compilation, "Method");
            var methodNode = graph.GetOrAddNode(method);
            methodNode.Callees.Should().HaveCount(1);
        }

        [Fact]
        public async Task BuildAsync_WithExpressionBodiedMethod_AnalyzesExpression()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Caller() => Callee();

    void Callee()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildAsync();

            // Assert
            graph.NodeCount.Should().Be(2);
            graph.EdgeCount.Should().Be(1);

            var caller = GetMethodSymbol(compilation, "Caller");
            var callerNode = graph.GetOrAddNode(caller);
            callerNode.Callees.Should().HaveCount(1);
        }

        [Fact]
        public async Task BuildForMethodAsync_WithSpecificMethod_OnlyAnalyzesMethod()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1()
    {
        Method2();
    }

    void Method2()
    {
        Method3();
    }

    void Method3()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var method1 = GetMethodSymbol(compilation, "Method1");
            var builder = new CallGraphBuilder(compilation);

            // Act
            var graph = await builder.BuildForMethodAsync(method1);

            // Assert
            // Should only contain Method1 and its direct callee (Method2)
            graph.NodeCount.Should().Be(2);
            graph.EdgeCount.Should().Be(1);

            var method1Node = graph.GetOrAddNode(method1);
            method1Node.Callees.Should().HaveCount(1);
        }

        [Fact]
        public async Task BuildAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method3(); }
    void Method3() { Method4(); }
    void Method4() { Method5(); }
    void Method5() { }
}";
            var compilation = CreateCompilation(code);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately
            var builder = new CallGraphBuilder(compilation, cts.Token);

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await builder.BuildAsync();
            });
        }

        [Fact]
        public void GetTransitiveCallees_WithLinearChain_ReturnsAllCallees()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method3(); }
    void Method3() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method1 = GetMethodSymbol(compilation, "Method1");

            // Act
            var callees = CallGraphBuilder.GetTransitiveCallees(graph, method1, maxDepth: 10).ToList();

            // Assert
            callees.Should().HaveCount(2); // Method2 and Method3
            callees.Select(m => m.Name).Should().Contain(new[] { "Method2", "Method3" });
        }

        [Fact]
        public void GetTransitiveCallees_WithCycle_HandlesCycle()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method1(); }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method1 = GetMethodSymbol(compilation, "Method1");

            // Act
            var callees = CallGraphBuilder.GetTransitiveCallees(graph, method1, maxDepth: 10).ToList();

            // Assert
            // Should detect cycle and not loop infinitely
            callees.Should().NotBeEmpty();
            callees.Should().Contain(m => m.Name == "Method2");
        }

        [Fact]
        public void GetTransitiveCallees_WithMaxDepth_RespectsLimit()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method3(); }
    void Method3() { Method4(); }
    void Method4() { Method5(); }
    void Method5() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method1 = GetMethodSymbol(compilation, "Method1");

            // Act
            var callees = CallGraphBuilder.GetTransitiveCallees(graph, method1, maxDepth: 2).ToList();

            // Assert
            // With maxDepth=2, should only traverse Method1->Method2->Method3
            callees.Count.Should().BeLessThanOrEqualTo(3);
        }

        [Fact]
        public void GetTransitiveCallers_WithLinearChain_ReturnsAllCallers()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method3(); }
    void Method3() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method3 = GetMethodSymbol(compilation, "Method3");

            // Act
            var callers = CallGraphBuilder.GetTransitiveCallers(graph, method3, maxDepth: 10).ToList();

            // Assert
            callers.Should().HaveCount(2); // Method2 and Method1
            callers.Select(m => m.Name).Should().Contain(new[] { "Method1", "Method2" });
        }

        [Fact]
        public void GetTransitiveCallers_WithCycle_HandlesCycle()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method1(); }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method2 = GetMethodSymbol(compilation, "Method2");

            // Act
            var callers = CallGraphBuilder.GetTransitiveCallers(graph, method2, maxDepth: 10).ToList();

            // Assert
            // Should detect cycle and not loop infinitely
            callers.Should().NotBeEmpty();
            callers.Should().Contain(m => m.Name == "Method1");
        }

        [Fact]
        public void GetTransitiveCallers_WithMaxDepth_RespectsLimit()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { Method3(); }
    void Method3() { Method4(); }
    void Method4() { Method5(); }
    void Method5() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            var method5 = GetMethodSymbol(compilation, "Method5");

            // Act
            var callers = CallGraphBuilder.GetTransitiveCallers(graph, method5, maxDepth: 2).ToList();

            // Assert
            // With maxDepth=2, should only traverse Method5->Method4->Method3
            callers.Count.Should().BeLessThanOrEqualTo(3);
        }

        [Fact]
        public void GetTransitiveCallees_WithNonExistentMethod_ReturnsEmpty()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            // Create a method symbol not in the graph
            var otherCompilation = CreateCompilation(@"
class OtherClass
{
    void OtherMethod() { }
}");
            var otherMethod = GetMethodSymbol(otherCompilation, "OtherMethod");

            // Act
            var callees = CallGraphBuilder.GetTransitiveCallees(graph, otherMethod, maxDepth: 10).ToList();

            // Assert
            callees.Should().BeEmpty();
        }

        [Fact]
        public void GetTransitiveCallers_WithNonExistentMethod_ReturnsEmpty()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var builder = new CallGraphBuilder(compilation);
            var graph = builder.BuildAsync().Result;

            // Create a method symbol not in the graph
            var otherCompilation = CreateCompilation(@"
class OtherClass
{
    void OtherMethod() { }
}");
            var otherMethod = GetMethodSymbol(otherCompilation, "OtherMethod");

            // Act
            var callers = CallGraphBuilder.GetTransitiveCallers(graph, otherMethod, maxDepth: 10).ToList();

            // Assert
            callers.Should().BeEmpty();
        }
    }
}
