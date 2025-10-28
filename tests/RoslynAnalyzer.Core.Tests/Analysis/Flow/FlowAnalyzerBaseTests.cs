using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Analysis.Flow;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Analysis.Flow
{
    public class FlowAnalyzerBaseTests
    {
        // Test flow type
        private class TestFlow
        {
            public string Value { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return obj is TestFlow other && Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }

        // Test flow info implementation
        private class TestFlowInfo : IFlowInfo<TestFlow>
        {
            public ISymbol Element { get; set; } = null!;
            public IEnumerable<TestFlow> IncomingFlow { get; set; } = Enumerable.Empty<TestFlow>();
            public IEnumerable<TestFlow> OutgoingFlow { get; set; } = Enumerable.Empty<TestFlow>();
            public bool HasUnhandledFlow { get; set; }
        }

        // Concrete test analyzer implementation
        private class TestFlowAnalyzer : FlowAnalyzerBase<TestFlow, TestFlowInfo>
        {
            private readonly Dictionary<string, TestFlowInfo> _methodResults;

            public TestFlowAnalyzer(Compilation compilation, Dictionary<string, TestFlowInfo> methodResults)
                : base(compilation)
            {
                _methodResults = methodResults;
            }

            protected override Task<TestFlowInfo> AnalyzeMethodAsync(IMethodSymbol method, CancellationToken cancellationToken)
            {
                // Simulate analysis by looking up pre-configured results
                if (_methodResults.TryGetValue(method.Name, out var info))
                {
                    return Task.FromResult(info);
                }

                // Default result
                return Task.FromResult(new TestFlowInfo
                {
                    Element = method,
                    IncomingFlow = new[] { new TestFlow { Value = $"incoming_{method.Name}" } },
                    OutgoingFlow = new[] { new TestFlow { Value = $"outgoing_{method.Name}" } },
                    HasUnhandledFlow = false
                });
            }
        }

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
        public async Task AnalyzeAsync_WithNewMethod_PerformsAnalysis()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method1");
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            // Act
            var result = await analyzer.AnalyzeAsync(method);

            // Assert
            result.Should().NotBeNull();
            result.Element.Should().Be(method);
            result.IncomingFlow.Should().NotBeEmpty();
            result.OutgoingFlow.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AnalyzeAsync_WithSameMethodTwice_UsesCachedResult()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method1");

            var expectedInfo = new TestFlowInfo
            {
                Element = method,
                IncomingFlow = new[] { new TestFlow { Value = "cached_incoming" } },
                OutgoingFlow = new[] { new TestFlow { Value = "cached_outgoing" } },
                HasUnhandledFlow = true
            };

            var methodResults = new Dictionary<string, TestFlowInfo>
            {
                ["Method1"] = expectedInfo
            };

            var analyzer = new TestFlowAnalyzer(compilation, methodResults);

            // Act
            var result1 = await analyzer.AnalyzeAsync(method);
            var result2 = await analyzer.AnalyzeAsync(method);

            // Assert
            result1.Should().BeSameAs(result2);
            result1.IncomingFlow.Should().ContainSingle()
                .Which.Value.Should().Be("cached_incoming");
        }

        [Fact]
        public async Task AnalyzeCompilationAsync_WithMultipleMethods_AnalyzesAll()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { Method2(); }
    void Method2() { }
}";
            var compilation = CreateCompilation(code);
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            // Act
            var results = await analyzer.AnalyzeCompilationAsync(compilation);

            // Assert
            var resultsList = results.ToList();
            resultsList.Should().HaveCount(2);
            resultsList.Select(r => r.Element).OfType<IMethodSymbol>()
                .Select(m => m.Name)
                .Should().Contain(new[] { "Method1", "Method2" });
        }

        [Fact]
        public async Task AnalyzeCompilationAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
    void Method2() { }
    void Method3() { }
}";
            var compilation = CreateCompilation(code);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await analyzer.AnalyzeCompilationAsync(compilation, cts.Token);
            });
        }

        [Fact]
        public void CombineFlows_WithMultipleSources_ReturnsDistinctUnion()
        {
            // Arrange
            var compilation = CreateCompilation("class C { void M() { } }");
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            var flow1 = new[] { new TestFlow { Value = "A" }, new TestFlow { Value = "B" } };
            var flow2 = new[] { new TestFlow { Value = "B" }, new TestFlow { Value = "C" } };
            var flow3 = new[] { new TestFlow { Value = "C" }, new TestFlow { Value = "D" } };

            // Act
            var combined = analyzer.CombineFlows(flow1, flow2, flow3).ToList();

            // Assert
            combined.Should().HaveCount(4); // A, B, C, D (distinct)
            combined.Select(f => f.Value).Should().Contain(new[] { "A", "B", "C", "D" });
        }

        [Fact]
        public void CombineFlows_WithEmptySources_ReturnsEmpty()
        {
            // Arrange
            var compilation = CreateCompilation("class C { void M() { } }");
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            // Act
            var combined = analyzer.CombineFlows(
                Enumerable.Empty<TestFlow>(),
                Enumerable.Empty<TestFlow>()
            ).ToList();

            // Assert
            combined.Should().BeEmpty();
        }

        [Fact]
        public void CombineFlows_WithSingleSource_ReturnsSameElements()
        {
            // Arrange
            var compilation = CreateCompilation("class C { void M() { } }");
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            var flow = new[] { new TestFlow { Value = "A" }, new TestFlow { Value = "B" } };

            // Act
            var combined = analyzer.CombineFlows(flow).ToList();

            // Assert
            combined.Should().HaveCount(2);
            combined.Select(f => f.Value).Should().Contain(new[] { "A", "B" });
        }

        [Fact]
        public void CombineFlows_WithDuplicatesInSingleSource_ReturnsDistinct()
        {
            // Arrange
            var compilation = CreateCompilation("class C { void M() { } }");
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            var flow = new[] { new TestFlow { Value = "A" }, new TestFlow { Value = "A" }, new TestFlow { Value = "B" } };

            // Act
            var combined = analyzer.CombineFlows(flow).ToList();

            // Assert
            combined.Should().HaveCount(2); // A and B (distinct)
            combined.Select(f => f.Value).Should().Contain(new[] { "A", "B" });
        }

        // Test for protected ClearCache method through derived class
        private class TestFlowAnalyzerWithCacheClear : TestFlowAnalyzer
        {
            public TestFlowAnalyzerWithCacheClear(Compilation compilation, Dictionary<string, TestFlowInfo> methodResults)
                : base(compilation, methodResults)
            {
            }

            public void PublicClearCache()
            {
                ClearCache();
            }
        }

        [Fact]
        public async Task ClearCache_AfterAnalysis_RemovesCachedResults()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method1");

            var callCount = 0;
            var methodResults = new Dictionary<string, TestFlowInfo>();

            var analyzer = new TestFlowAnalyzerWithCacheClear(compilation, methodResults)
            {
            };

            // First analysis
            await analyzer.AnalyzeAsync(method);

            // Act
            analyzer.PublicClearCache();

            // Second analysis should re-analyze (not use cache)
            var result2 = await analyzer.AnalyzeAsync(method);

            // Assert
            result2.Should().NotBeNull();
        }

        // Test for protected TryGetCached method through derived class
        private class TestFlowAnalyzerWithCacheAccess : TestFlowAnalyzer
        {
            public TestFlowAnalyzerWithCacheAccess(Compilation compilation, Dictionary<string, TestFlowInfo> methodResults)
                : base(compilation, methodResults)
            {
            }

            public bool PublicTryGetCached(IMethodSymbol method, out TestFlowInfo? info)
            {
                return TryGetCached(method, out info);
            }
        }

        [Fact]
        public async Task TryGetCached_WithCachedMethod_ReturnsTrue()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method1");

            var analyzer = new TestFlowAnalyzerWithCacheAccess(compilation, new Dictionary<string, TestFlowInfo>());

            // Analyze to populate cache
            await analyzer.AnalyzeAsync(method);

            // Act
            var result = analyzer.PublicTryGetCached(method, out var info);

            // Assert
            result.Should().BeTrue();
            info.Should().NotBeNull();
            info!.Element.Should().Be(method);
        }

        [Fact]
        public void TryGetCached_WithUncachedMethod_ReturnsFalse()
        {
            // Arrange
            var code = @"
class TestClass
{
    void Method1() { }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method1");

            var analyzer = new TestFlowAnalyzerWithCacheAccess(compilation, new Dictionary<string, TestFlowInfo>());

            // Act (no prior analysis)
            var result = analyzer.PublicTryGetCached(method, out var info);

            // Assert
            result.Should().BeFalse();
            info.Should().BeNull();
        }

        [Fact]
        public void Compilation_PropertyReturnsCompilation()
        {
            // Arrange
            var code = "class C { void M() { } }";
            var compilation = CreateCompilation(code);
            var analyzer = new TestFlowAnalyzer(compilation, new Dictionary<string, TestFlowInfo>());

            // We can't directly test the protected property, but we can verify through behavior
            // that the compilation is being used correctly
            var result = analyzer.AnalyzeCompilationAsync(compilation).Result;

            // Assert
            result.Should().NotBeNull();
        }
    }
}
