using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Helpers;

namespace RoslynAnalyzer.Core.Tests.Helpers;

public class DiagnosticHelpersTests
{
    [Fact]
    public void GetMemberLocation_WithMethod_ReturnsIdentifierLocation()
    {
        // Arrange
        var code = "class C { void MyMethod() { } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(method);

        // Assert
        location.Should().NotBeNull();
        location.SourceSpan.Should().NotBe(default);
        // The location should be for the identifier "MyMethod"
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("MyMethod");
    }

    [Fact]
    public void GetMemberLocation_WithConstructor_ReturnsIdentifierLocation()
    {
        // Arrange
        var code = "class MyClass { MyClass() { } }";
        var constructor = GetFirstNode<ConstructorDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(constructor);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("MyClass");
    }

    [Fact]
    public void GetMemberLocation_WithDestructor_ReturnsIdentifierLocation()
    {
        // Arrange
        var code = "class MyClass { ~MyClass() { } }";
        var destructor = GetFirstNode<DestructorDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(destructor);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("MyClass");
    }

    [Fact]
    public void GetMemberLocation_WithProperty_ReturnsIdentifierLocation()
    {
        // Arrange
        var code = "class C { int MyProperty { get; set; } }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(property);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("MyProperty");
    }

    [Fact]
    public void GetMemberLocation_WithAccessor_ReturnsKeywordLocation()
    {
        // Arrange
        var code = "class C { int P { get { return 1; } } }";
        var accessor = GetFirstNode<AccessorDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(accessor);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("get");
    }

    [Fact]
    public void GetMemberLocation_WithOperator_ReturnsOperatorTokenLocation()
    {
        // Arrange
        var code = "class C { public static C operator +(C a, C b) => a; }";
        var op = GetFirstNode<OperatorDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(op);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("+");
    }

    [Fact]
    public void GetMemberLocation_WithConversionOperator_ReturnsTypeLocation()
    {
        // Arrange
        var code = "class C { public static explicit operator int(C c) => 0; }";
        var convOp = GetFirstNode<ConversionOperatorDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(convOp);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("int");
    }

    [Fact]
    public void GetMemberLocation_WithLocalFunction_ReturnsIdentifierLocation()
    {
        // Arrange
        var code = "class C { void M() { void LocalFunc() { } } }";
        var localFunction = GetFirstNode<LocalFunctionStatementSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(localFunction);

        // Assert
        location.Should().NotBeNull();
        var text = location.SourceTree?.GetText().ToString(location.SourceSpan);
        text.Should().Be("LocalFunc");
    }

    [Fact]
    public void GetMemberLocation_WithUnknownNode_ReturnsNodeLocation()
    {
        // Arrange
        var code = "class C { }";
        var classDecl = GetFirstNode<ClassDeclarationSyntax>(code);

        // Act
        var location = DiagnosticHelpers.GetMemberLocation(classDecl);

        // Assert
        location.Should().NotBeNull();
        location.Should().Be(classDecl.GetLocation());
    }

    private static T GetFirstNode<T>(string code) where T : CSharpSyntaxNode
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        return root.DescendantNodes().OfType<T>().First();
    }
}
