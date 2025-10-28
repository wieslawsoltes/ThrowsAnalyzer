using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Analysis.Patterns.Iterators;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Analysis.Patterns.Iterators
{
    public class IteratorMethodDetectorTests
    {
        private static Compilation CreateCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
                });
        }

        private static IMethodSymbol GetMethodSymbol(Compilation compilation, string methodName)
        {
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == methodName);

            return model.GetDeclaredSymbol(methodDecl);
        }

        [Fact]
        public void IsIteratorMethod_WithYieldReturn_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
        yield return 2;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.IsIteratorMethod(method, methodDecl);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsIteratorMethod_WithoutYield_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        return new List<int> { 1, 2, 3 };
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.IsIteratorMethod(method, methodDecl);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsEnumerable_WithIEnumerableReturnType_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.ReturnsEnumerable(method, compilation);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsEnumerable_WithNonGenericIEnumerable_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections;
class C
{
    IEnumerable Method()
    {
        yield return 1;
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.ReturnsEnumerable(method, compilation);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsEnumerable_WithNonEnumerableReturnType_ReturnsFalse()
        {
            // Arrange
            var code = @"
class C
{
    int Method()
    {
        return 42;
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.ReturnsEnumerable(method, compilation);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetYieldReturnStatements_WithMultipleYields_ReturnsAll()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;

            // Act
            var result = IteratorMethodDetector.GetYieldReturnStatements(body).ToList();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void GetYieldBreakStatements_WithYieldBreak_ReturnsStatement()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        if (true)
            yield break;
        yield return 1;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;

            // Act
            var result = IteratorMethodDetector.GetYieldBreakStatements(body).ToList();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public void HasYieldStatements_WithYield_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;

            // Act
            var result = IteratorMethodDetector.HasYieldStatements(body);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasYieldStatements_WithoutYield_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        return new List<int>();
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;

            // Act
            var result = IteratorMethodDetector.HasYieldStatements(body);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsBeforeFirstYield_WithNodeBeforeYield_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        int x = 42;
        yield return x;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;
            var declaration = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>()
                .First();

            // Act
            var result = IteratorMethodDetector.IsBeforeFirstYield(declaration, body);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsBeforeFirstYield_WithNodeAfterYield_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
        int x = 42;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;
            var declaration = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>()
                .First();

            // Act
            var result = IteratorMethodDetector.IsBeforeFirstYield(declaration, body);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasYieldInTryBlock_WithYieldInTry_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        try
        {
            yield return 1;
        }
        finally
        {
        }
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var tryStatement = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TryStatementSyntax>()
                .First();

            // Act
            var result = IteratorMethodDetector.HasYieldInTryBlock(tryStatement);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasYieldInTryBlock_WithoutYieldInTry_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        try
        {
            int x = 42;
        }
        finally
        {
        }
        yield return 1;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var tryStatement = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TryStatementSyntax>()
                .First();

            // Act
            var result = IteratorMethodDetector.HasYieldInTryBlock(tryStatement);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetIteratorMethodInfo_WithIteratorMethod_ReturnsCompleteInfo()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        yield return 1;
        yield return 2;
        yield break;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.GetIteratorMethodInfo(method, methodDecl, compilation);

            // Assert
            result.Should().NotBeNull();
            result.IsIterator.Should().BeTrue();
            result.ReturnsEnumerable.Should().BeTrue();
            result.YieldReturnCount.Should().Be(2);
            result.YieldBreakCount.Should().Be(1);
        }

        [Fact]
        public void GetIteratorMethodInfo_WithNonIteratorMethod_ReturnsCorrectInfo()
        {
            // Arrange
            var code = @"
using System.Collections.Generic;
class C
{
    IEnumerable<int> Method()
    {
        return new List<int> { 1, 2, 3 };
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = IteratorMethodDetector.GetIteratorMethodInfo(method, methodDecl, compilation);

            // Assert
            result.IsIterator.Should().BeFalse();
            result.ReturnsEnumerable.Should().BeTrue();
            result.YieldReturnCount.Should().Be(0);
            result.YieldBreakCount.Should().Be(0);
        }
    }
}
