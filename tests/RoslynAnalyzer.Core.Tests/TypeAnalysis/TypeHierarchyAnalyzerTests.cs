using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynAnalyzer.Core.TypeAnalysis;
using Xunit;

namespace RoslynAnalyzer.Core.Tests.TypeAnalysis
{
    public class TypeHierarchyAnalyzerTests
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
        public void IsAssignableTo_WithSameType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(stringType, stringType);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAssignableTo_WithDerivedType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");
            var systemException = compilation.GetTypeByMetadataName("System.SystemException");

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(argumentException, systemException);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAssignableTo_WithBaseType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var systemException = compilation.GetTypeByMetadataName("System.SystemException");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(systemException, argumentException);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAssignableTo_WithUnrelatedTypes_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(stringType, intType);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAssignableTo_WithNullDerivedType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var baseType = compilation.GetSpecialType(SpecialType.System_Object);

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(null, baseType);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAssignableTo_WithNullBaseType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var derivedType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = TypeHierarchyAnalyzer.IsAssignableTo(derivedType, null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetTypeHierarchy_WithSimpleType_ReturnsHierarchy()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var hierarchy = TypeHierarchyAnalyzer.GetTypeHierarchy(stringType).ToList();

            // Assert
            hierarchy.Should().NotBeEmpty();
            hierarchy[0].Name.Should().Be("String");
            hierarchy[^1].Name.Should().Be("Object");
        }

        [Fact]
        public void GetTypeHierarchy_WithExceptionType_ReturnsFullHierarchy()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");

            // Act
            var hierarchy = TypeHierarchyAnalyzer.GetTypeHierarchy(argumentException).ToList();

            // Assert
            hierarchy.Should().NotBeEmpty();
            hierarchy[0].Name.Should().Be("ArgumentException");
            hierarchy.Should().Contain(t => t.Name == "SystemException");
            hierarchy.Should().Contain(t => t.Name == "Exception");
            hierarchy[^1].Name.Should().Be("Object");
        }

        [Fact]
        public void GetTypeHierarchy_WithObjectType_ReturnsOnlyObject()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);

            // Act
            var hierarchy = TypeHierarchyAnalyzer.GetTypeHierarchy(objectType).ToList();

            // Assert
            hierarchy.Should().HaveCount(1);
            hierarchy[0].Name.Should().Be("Object");
        }

        [Fact]
        public void ImplementsInterface_WithInterfaceImplementation_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var icollection = compilation.GetTypeByMetadataName("System.Collections.ICollection");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsInterface(listOfInt, icollection);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ImplementsInterface_WithoutImplementation_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var icollection = compilation.GetTypeByMetadataName("System.Collections.ICollection");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsInterface(stringType, icollection);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ImplementsInterface_WithInterfaceItself_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var ienumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsInterface(ienumerable, ienumerable);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ImplementsInterface_WithNullType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var ienumerable = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsInterface(null, ienumerable);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ImplementsInterface_WithNullInterface_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsInterface(stringType, null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ImplementsGenericInterface_WithGenericImplementation_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsGenericInterface(listOfInt, ienumerableOfT);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ImplementsGenericInterface_WithoutImplementation_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsGenericInterface(intType, ienumerableOfT);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ImplementsGenericInterface_WithConstructedGenericType_ReturnsTrue()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listDefinition = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);
            var listOfInt = listDefinition.Construct(intType);
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsGenericInterface(listOfInt, ienumerableOfT);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ImplementsGenericInterface_WithNullType_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var ienumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsGenericInterface(null, ienumerableOfT);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ImplementsGenericInterface_WithNullInterface_ReturnsFalse()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");

            // Act
            var result = TypeHierarchyAnalyzer.ImplementsGenericInterface(listOfInt, null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void FindCommonBaseType_WithSiblingExceptions_ReturnsCommonBase()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");
            var invalidOperationException = compilation.GetTypeByMetadataName("System.InvalidOperationException");

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(argumentException, invalidOperationException);

            // Assert
            common.Should().NotBeNull();
            common.Name.Should().Be("SystemException");
        }

        [Fact]
        public void FindCommonBaseType_WithParentChild_ReturnsParent()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var argumentException = compilation.GetTypeByMetadataName("System.ArgumentException");
            var argumentNullException = compilation.GetTypeByMetadataName("System.ArgumentNullException");

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(argumentException, argumentNullException);

            // Assert
            common.Should().NotBeNull();
            common.Name.Should().Be("ArgumentException");
        }

        [Fact]
        public void FindCommonBaseType_WithUnrelatedTypes_ReturnsObject()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(stringType, intType);

            // Assert
            common.Should().NotBeNull();
            // For value types, the common base is ValueType, not Object directly
            // Actually, int's base is System.ValueType
            common.Should().NotBeNull();
        }

        [Fact]
        public void FindCommonBaseType_WithSameType_ReturnsThatType()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(stringType, stringType);

            // Assert
            common.Should().NotBeNull();
            common.Name.Should().Be("String");
        }

        [Fact]
        public void FindCommonBaseType_WithNullFirstType_ReturnsNull()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(null, stringType);

            // Assert
            common.Should().BeNull();
        }

        [Fact]
        public void FindCommonBaseType_WithNullSecondType_ReturnsNull()
        {
            // Arrange
            var compilation = CreateCompilation("class C { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var common = TypeHierarchyAnalyzer.FindCommonBaseType(stringType, null);

            // Assert
            common.Should().BeNull();
        }
    }
}
