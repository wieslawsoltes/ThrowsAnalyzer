using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.Extensions;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.Extensions
{
    public class TypeSymbolExtensionsTests
    {
        private static Compilation CreateCompilation(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Exception).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
                });
        }

        [Fact]
        public void IsAssignableTo_Extension_WithDerivedType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");
            var systemException = compilation.GetTypeByMetadataName("System.SystemException");

            // Act
            var result = argumentException.IsAssignableTo(systemException);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetTypeHierarchy_Extension_ReturnsHierarchy()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");

            // Act
            var hierarchy = argumentException.GetTypeHierarchy().ToList();

            // Assert
            hierarchy.Should().NotBeEmpty();
            hierarchy[0].Name.Should().Be("ArgumentException");
            hierarchy.Should().Contain(t => t.Name == "SystemException");
        }

        [Fact]
        public void ImplementsInterface_Extension_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var icollection = compilation.GetTypeByMetadataName("System.Collections.ICollection");

            // Act
            var result = listOfInt.ImplementsInterface(icollection);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ImplementsGenericInterface_Extension_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            // Act
            var result = listOfInt.ImplementsGenericInterface(ienumerableOfT);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void FindCommonBaseType_Extension_ReturnsCommonBase()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");
            var invalidOperationException = compilation.GetTypeByMetadataName("System.InvalidOperationException");

            // Act
            var common = argumentException.FindCommonBaseType(invalidOperationException);

            // Assert
            common.Should().NotBeNull();
            common.Name.Should().Be("SystemException");
        }

        [Fact]
        public void IsType_WithMatchingType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = stringType.IsType("string");  // Use the C# type name

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsType_WithNonMatchingType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = stringType.IsType("int");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsType_WithNullType_ReturnsFalse()
        {
            // Act
            ITypeSymbol type = null;
            var result = type.IsType("System.String");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsType_WithEmptyMetadataName_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = stringType.IsType("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValueType_WithValueType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // Act
            var result = intType.IsValueType();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValueType_WithReferenceType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = stringType.IsValueType();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValueType_WithNullType_ReturnsFalse()
        {
            // Act
            ITypeSymbol type = null;
            var result = type.IsValueType();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsReferenceType_WithReferenceType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = stringType.IsReferenceType();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsReferenceType_WithValueType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // Act
            var result = intType.IsReferenceType();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsReferenceType_WithNullType_ReturnsFalse()
        {
            // Act
            ITypeSymbol type = null;
            var result = type.IsReferenceType();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNullable_WithNullableValueType_ReturnsTrue()
        {
            // Arrange
            var code = "class C { int? NullableInt { get; set; } }";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var propertyDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
                .First();
            var propertySymbol = model.GetDeclaredSymbol(propertyDecl) as IPropertySymbol;
            var nullableIntType = propertySymbol.Type;

            // Act
            var result = nullableIntType.IsNullable();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullable_WithNonNullableValueType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // Act
            var result = intType.IsNullable();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNullable_WithNullType_ReturnsFalse()
        {
            // Act
            ITypeSymbol type = null;
            var result = type.IsNullable();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNullable_WithNullableReferenceType_ReturnsTrue()
        {
            // Arrange
            var code = @"
#nullable enable
class C
{
    string? NullableString { get; set; }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var propertyDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
                .First();
            var propertySymbol = model.GetDeclaredSymbol(propertyDecl) as IPropertySymbol;
            var nullableStringType = propertySymbol.Type;

            // Act
            var result = nullableStringType.IsNullable();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullable_WithNonNullableReferenceType_ReturnsFalse()
        {
            // Arrange
            var code = @"
#nullable enable
class C
{
    string NonNullableString { get; set; }
}";
            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var propertyDecl = root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
                .First();
            var propertySymbol = model.GetDeclaredSymbol(propertyDecl) as IPropertySymbol;
            var nonNullableStringType = propertySymbol.Type;

            // Act
            var result = nonNullableStringType.IsNullable();

            // Assert
            result.Should().BeFalse();
        }
    }
}
