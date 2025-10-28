using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Analysis.Patterns.Async;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Analysis.Patterns.Async
{
    public class AsyncMethodDetectorTests
    {
        private static Compilation CreateCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
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
        public void IsAsyncMethod_WithAsyncModifier_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task MethodAsync()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "MethodAsync");

            // Act
            var result = AsyncMethodDetector.IsAsyncMethod(method);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAsyncMethod_WithoutAsyncModifier_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    Task Method()
    {
        return Task.CompletedTask;
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.IsAsyncMethod(method);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ReturnsTask_WithTaskReturnType_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    Task Method()
    {
        return Task.CompletedTask;
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.ReturnsTask(method, compilation);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTask_WithTaskOfTReturnType_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    Task<int> Method()
    {
        return Task.FromResult(42);
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.ReturnsTask(method, compilation);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTask_WithVoidReturnType_ReturnsFalse()
        {
            // Arrange
            var code = @"
class C
{
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.ReturnsTask(method, compilation);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAsyncVoid_WithAsyncVoidMethod_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async void Method()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.IsAsyncVoid(method, compilation);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAsyncVoid_WithAsyncTaskMethod_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var method = GetMethodSymbol(compilation, "Method");

            // Act
            var result = AsyncMethodDetector.IsAsyncVoid(method, compilation);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetFirstAwaitExpression_WithAwaitStatement_ReturnsAwait()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
        await Task.Delay(200);
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
            var result = AsyncMethodDetector.GetFirstAwaitExpression(body);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void GetFirstAwaitExpression_WithoutAwait_ReturnsNull()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    Task Method()
    {
        return Task.CompletedTask;
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
            var result = AsyncMethodDetector.GetFirstAwaitExpression(body);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetAllAwaitExpressions_WithMultipleAwaits_ReturnsAll()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
        await Task.Delay(200);
        await Task.Delay(300);
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
            var result = AsyncMethodDetector.GetAllAwaitExpressions(body).ToList();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void IsBeforeFirstAwait_WithNodeBeforeAwait_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        int x = 42;
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var body = methodDecl.Body;
            var assignment = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>()
                .First();

            // Act
            var result = AsyncMethodDetector.IsBeforeFirstAwait(assignment, body);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsBeforeFirstAwait_WithNodeAfterAwait_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
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
            var assignment = body.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>()
                .First();

            // Act
            var result = AsyncMethodDetector.IsBeforeFirstAwait(assignment, body);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasAsyncModifier_WithAsyncMethod_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();

            // Act
            var result = AsyncMethodDetector.HasAsyncModifier(methodDecl);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasAsyncModifier_WithoutAsyncModifier_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    Task Method()
    {
        return Task.CompletedTask;
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();

            // Act
            var result = AsyncMethodDetector.HasAsyncModifier(methodDecl);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetAsyncMethodInfo_WithAsyncMethod_ReturnsCompleteInfo()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async Task Method()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = model.GetDeclaredSymbol(methodDecl);

            // Act
            var result = AsyncMethodDetector.GetAsyncMethodInfo(method, methodDecl, model);

            // Assert
            result.Should().NotBeNull();
            result.IsAsync.Should().BeTrue();
            result.ReturnsTask.Should().BeTrue();
            result.IsAsyncVoid.Should().BeFalse();
            result.HasAwaitExpressions.Should().BeTrue();
            result.FirstAwaitExpression.Should().NotBeNull();
        }

        [Fact]
        public void GetAsyncMethodInfo_WithAsyncVoid_DetectsAsyncVoid()
        {
            // Arrange
            var code = @"
using System.Threading.Tasks;
class C
{
    async void Method()
    {
        await Task.Delay(100);
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var methodDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                .First();
            var method = model.GetDeclaredSymbol(methodDecl);

            // Act
            var result = AsyncMethodDetector.GetAsyncMethodInfo(method, methodDecl, model);

            // Assert
            result.IsAsyncVoid.Should().BeTrue();
        }
    }
}
