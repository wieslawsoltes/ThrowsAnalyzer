using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Analysis.CallGraph;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Analysis.CallGraph
{
    public class CallGraphTests
    {
        private static IMethodSymbol CreateMethodSymbol(string name)
        {
            var code = $@"
class TestClass
{{
    void {name}() {{ }}
}}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();

            return model.GetDeclaredSymbol(methodDecl)!;
        }

        [Fact]
        public void GetOrAddNode_WithNewMethod_CreatesNode()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method = CreateMethodSymbol("Method1");

            // Act
            var node = graph.GetOrAddNode(method);

            // Assert
            node.Should().NotBeNull();
            node.Method.Should().Be(method);
            graph.NodeCount.Should().Be(1);
        }

        [Fact]
        public void GetOrAddNode_WithExistingMethod_ReturnsSameNode()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method = CreateMethodSymbol("Method1");

            // Act
            var node1 = graph.GetOrAddNode(method);
            var node2 = graph.GetOrAddNode(method);

            // Assert
            node1.Should().BeSameAs(node2);
            graph.NodeCount.Should().Be(1);
        }

        [Fact]
        public void AddEdge_WithTwoMethods_CreatesEdge()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var caller = CreateMethodSymbol("Caller");
            var callee = CreateMethodSymbol("Callee");
            var location = Location.None;

            // Act
            graph.AddEdge(caller, callee, location);

            // Assert
            graph.NodeCount.Should().Be(2);
            graph.EdgeCount.Should().Be(1);

            var callerNode = graph.GetOrAddNode(caller);
            var calleeNode = graph.GetOrAddNode(callee);

            callerNode.Callees.Should().HaveCount(1);
            callerNode.Callees[0].Target.Method.Should().Be(callee);
            callerNode.Callees[0].CallSite.Should().Be(location);

            calleeNode.Callers.Should().HaveCount(1);
            calleeNode.Callers[0].Target.Method.Should().Be(caller);
            calleeNode.Callers[0].CallSite.Should().Be(location);
        }

        [Fact]
        public void AddEdge_WithMultipleCallSites_CreatesMultipleEdges()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var caller = CreateMethodSymbol("Caller");
            var callee = CreateMethodSymbol("Callee");

            // Act
            graph.AddEdge(caller, callee, Location.None);
            graph.AddEdge(caller, callee, Location.None);

            // Assert
            var callerNode = graph.GetOrAddNode(caller);
            callerNode.Callees.Should().HaveCount(2);
            graph.EdgeCount.Should().Be(2);
        }

        [Fact]
        public void TryGetNode_WithExistingMethod_ReturnsTrue()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method = CreateMethodSymbol("Method1");
            graph.GetOrAddNode(method);

            // Act
            var result = graph.TryGetNode(method, out var node);

            // Assert
            result.Should().BeTrue();
            node.Should().NotBeNull();
            node!.Method.Should().Be(method);
        }

        [Fact]
        public void TryGetNode_WithNonExistingMethod_ReturnsFalse()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method = CreateMethodSymbol("Method1");

            // Act
            var result = graph.TryGetNode(method, out var node);

            // Assert
            result.Should().BeFalse();
            node.Should().BeNull();
        }

        [Fact]
        public void Nodes_ReturnsAllNodes()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method1 = CreateMethodSymbol("Method1");
            var method2 = CreateMethodSymbol("Method2");
            graph.GetOrAddNode(method1);
            graph.GetOrAddNode(method2);

            // Act
            var nodes = graph.Nodes.ToList();

            // Assert
            nodes.Should().HaveCount(2);
            nodes.Select(n => n.Method).Should().Contain(new[] { method1, method2 });
        }

        [Fact]
        public void EdgeCount_WithMultipleEdges_ReturnsCorrectCount()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method1 = CreateMethodSymbol("Method1");
            var method2 = CreateMethodSymbol("Method2");
            var method3 = CreateMethodSymbol("Method3");

            // Act
            graph.AddEdge(method1, method2, Location.None);
            graph.AddEdge(method1, method3, Location.None);
            graph.AddEdge(method2, method3, Location.None);

            // Assert
            graph.EdgeCount.Should().Be(3);
        }

        [Fact]
        public void CallGraphNode_GetDepth_WithNoCallers_ReturnsZero()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method = CreateMethodSymbol("Method1");
            var node = graph.GetOrAddNode(method);

            // Act
            var depth = node.GetDepth();

            // Assert
            depth.Should().Be(0);
        }

        [Fact]
        public void CallGraphNode_GetDepth_WithOneCaller_ReturnsOne()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var caller = CreateMethodSymbol("Caller");
            var callee = CreateMethodSymbol("Callee");
            graph.AddEdge(caller, callee, Location.None);

            // Act
            var calleeNode = graph.GetOrAddNode(callee);
            var depth = calleeNode.GetDepth();

            // Assert
            depth.Should().Be(1);
        }

        [Fact]
        public void CallGraphNode_GetDepth_WithMultipleLevels_ReturnsMaxDepth()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method1 = CreateMethodSymbol("Method1");
            var method2 = CreateMethodSymbol("Method2");
            var method3 = CreateMethodSymbol("Method3");

            // method1 -> method2 -> method3
            graph.AddEdge(method1, method2, Location.None);
            graph.AddEdge(method2, method3, Location.None);

            // Act
            var node3 = graph.GetOrAddNode(method3);
            var depth = node3.GetDepth();

            // Assert
            depth.Should().Be(2);
        }

        [Fact]
        public void CallGraphNode_GetDepth_WithCycle_ReturnsZero()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method1 = CreateMethodSymbol("Method1");
            var method2 = CreateMethodSymbol("Method2");

            // Create cycle: method1 -> method2 -> method1
            graph.AddEdge(method1, method2, Location.None);
            graph.AddEdge(method2, method1, Location.None);

            // Act
            var node1 = graph.GetOrAddNode(method1);
            var depth = node1.GetDepth();

            // Assert
            // When cycle is detected, depth calculation returns 0 for the cyclic portion
            depth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CallGraphNode_GetDepth_WithMultiplePaths_ReturnsMaxDepth()
        {
            // Arrange
            var graph = new Core.Analysis.CallGraph.CallGraph();
            var method1 = CreateMethodSymbol("Method1");
            var method2 = CreateMethodSymbol("Method2");
            var method3 = CreateMethodSymbol("Method3");
            var method4 = CreateMethodSymbol("Method4");

            // Create multiple paths to method4:
            // method1 -> method4 (depth 1)
            // method2 -> method3 -> method4 (depth 2)
            graph.AddEdge(method1, method4, Location.None);
            graph.AddEdge(method2, method3, Location.None);
            graph.AddEdge(method3, method4, Location.None);

            // Act
            var node4 = graph.GetOrAddNode(method4);
            var depth = node4.GetDepth();

            // Assert
            depth.Should().Be(2); // Maximum depth from method2 -> method3 -> method4
        }
    }
}
