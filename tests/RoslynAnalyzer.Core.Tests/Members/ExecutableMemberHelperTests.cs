using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members;

namespace RoslynAnalyzer.Core.Tests.Members;

public class ExecutableMemberHelperTests
{
    [Fact]
    public void IsExecutableMember_WithMethod_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(method);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithConstructor_ReturnsTrue()
    {
        // Arrange
        var code = "class C { C() { } }";
        var constructor = GetFirstNode<ConstructorDeclarationSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(constructor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithExpressionBodiedProperty_ReturnsTrue()
    {
        // Arrange - expression-bodied property
        var code = "class C { int P => 42; }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(property);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithAccessor_ReturnsTrue()
    {
        // Arrange - accessor in property
        var code = "class C { int P { get { return 1; } } }";
        var accessor = GetFirstNode<AccessorDeclarationSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(accessor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithLocalFunction_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { void Local() { } } }";
        var localFunction = GetFirstNode<LocalFunctionStatementSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(localFunction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithLambda_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { var x = () => 42; } }";
        var lambda = GetFirstNode<ParenthesizedLambdaExpressionSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(lambda);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExecutableMember_WithClassDeclaration_ReturnsFalse()
    {
        // Arrange
        var code = "class C { }";
        var classDecl = GetFirstNode<ClassDeclarationSyntax>(code);

        // Act
        var result = ExecutableMemberHelper.IsExecutableMember(classDecl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetExecutableBlocks_WithBlockBodiedMethod_ReturnsBlock()
    {
        // Arrange
        var code = "class C { void M() { var x = 1; } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);

        // Act
        var blocks = ExecutableMemberHelper.GetExecutableBlocks(method).ToList();

        // Assert
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<BlockSyntax>();
    }

    [Fact]
    public void GetExecutableBlocks_WithExpressionBodiedMethod_ReturnsExpression()
    {
        // Arrange
        var code = "class C { int M() => 42; }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);

        // Act
        var blocks = ExecutableMemberHelper.GetExecutableBlocks(method).ToList();

        // Assert
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<ArrowExpressionClauseSyntax>();
    }

    [Fact]
    public void GetExecutableBlocks_WithNonExecutableMember_ReturnsEmpty()
    {
        // Arrange
        var code = "class C { }";
        var classDecl = GetFirstNode<ClassDeclarationSyntax>(code);

        // Act
        var blocks = ExecutableMemberHelper.GetExecutableBlocks(classDecl);

        // Assert
        blocks.Should().BeEmpty();
    }

    [Fact]
    public void GetMemberDisplayName_WithMethod_ReturnsMethodName()
    {
        // Arrange
        var code = "class C { void MyMethod() { } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);

        // Act
        var displayName = ExecutableMemberHelper.GetMemberDisplayName(method);

        // Assert
        displayName.Should().Be("Method 'MyMethod'");
    }

    [Fact]
    public void GetMemberDisplayName_WithConstructor_ReturnsConstructor()
    {
        // Arrange
        var code = "class MyClass { MyClass() { } }";
        var constructor = GetFirstNode<ConstructorDeclarationSyntax>(code);

        // Act
        var displayName = ExecutableMemberHelper.GetMemberDisplayName(constructor);

        // Assert
        displayName.Should().Be("Constructor 'MyClass'");
    }

    [Fact]
    public void GetMemberDisplayName_WithExpressionBodiedProperty_ReturnsPropertyName()
    {
        // Arrange
        var code = "class C { int MyProperty => 42; }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);

        // Act
        var displayName = ExecutableMemberHelper.GetMemberDisplayName(property);

        // Assert
        displayName.Should().Be("Property 'MyProperty'");
    }

    [Fact]
    public void GetMemberDisplayName_WithUnknownNode_ReturnsMember()
    {
        // Arrange
        var code = "class C { }";
        var classDecl = GetFirstNode<ClassDeclarationSyntax>(code);

        // Act
        var displayName = ExecutableMemberHelper.GetMemberDisplayName(classDecl);

        // Assert
        displayName.Should().Be("Member");
    }

    [Fact]
    public void GetAllDetectors_ReturnsAllTenDetectors()
    {
        // Act
        var detectors = ExecutableMemberHelper.GetAllDetectors();

        // Assert
        detectors.Should().HaveCount(10);
        detectors.Should().AllBeAssignableTo<IExecutableMemberDetector>();
    }

    private static T GetFirstNode<T>(string code) where T : CSharpSyntaxNode
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        return root.DescendantNodes().OfType<T>().First();
    }
}
