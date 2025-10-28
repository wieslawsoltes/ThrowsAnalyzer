using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Configuration.Suppression;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Configuration.Suppression
{
    public class SuppressionHelperTests
    {
        private static Compilation CreateCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
                });
        }

        [Fact]
        public void IsSuppressed_WithMethodLevelSuppression_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    void Method()
    {
        Console.WriteLine();
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithoutSuppression_ReturnsFalse()
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
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsSuppressed_WithDifferentDiagnostic_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE002", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsSuppressed_WithWildcardSuppression_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE*"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithWildcardNotMatching_ReturnsFalse()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE*"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "OTHER001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsSuppressed_WithMultipleDiagnostics_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"", ""RULE002"", ""RULE003"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE002", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithTypeLevelSuppression_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

[SuppressAnalysis(""RULE001"")]
class C
{
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithShortAttributeName_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act - use short name "SuppressAnalysis" instead of full "SuppressAnalysisAttribute"
            var result = SuppressionHelper.IsSuppressed(model, methodDecl, "RULE001", "SuppressAnalysis");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithMultipleAttributeNames_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    void Method()
    {
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            // Act - try multiple attribute names
            var result = SuppressionHelper.IsSuppressed(
                model,
                methodDecl,
                "RULE001",
                "OtherAttribute",
                "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact(Skip = "Constructor attribute resolution needs investigation")]
        public void IsSuppressed_WithConstructor_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    public C()
    {
        Console.WriteLine();
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var ctorDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, ctorDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithProperty_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    [SuppressAnalysis(""RULE001"")]
    public int Property { get; set; }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var propertyDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, propertyDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithAccessor_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    public int Property
    {
        [SuppressAnalysis(""RULE001"")]
        get { return 0; }
        set { }
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var accessorDecl = tree.GetRoot()
                .DescendantNodes()
                .OfType<AccessorDeclarationSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, accessorDecl, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSuppressed_WithLocalFunction_ReturnsTrue()
        {
            // Arrange
            var code = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
class SuppressAnalysisAttribute : Attribute
{
    public SuppressAnalysisAttribute(params string[] rules) { }
}

class C
{
    void Method()
    {
        [SuppressAnalysis(""RULE001"")]
        void LocalFunction()
        {
        }
    }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var localFunc = tree.GetRoot()
                .DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .First();

            // Act
            var result = SuppressionHelper.IsSuppressed(model, localFunc, "RULE001", "SuppressAnalysisAttribute");

            // Assert
            result.Should().BeTrue();
        }
    }
}
