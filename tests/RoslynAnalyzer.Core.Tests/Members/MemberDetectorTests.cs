using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members.Detectors;

namespace RoslynAnalyzer.Core.Tests.Members;

public class MemberDetectorTests
{
    [Fact]
    public void MethodMemberDetector_SupportsMethod_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);
        var detector = new MethodMemberDetector();

        // Act
        var result = detector.SupportsNode(method);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MethodMemberDetector_GetExecutableBlocks_WithBlockBody_ReturnsBlock()
    {
        // Arrange
        var code = "class C { void M() { var x = 1; } }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);
        var detector = new MethodMemberDetector();

        // Act
        var blocks = detector.GetExecutableBlocks(method).ToList();

        // Assert
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<BlockSyntax>();
    }

    [Fact]
    public void MethodMemberDetector_GetExecutableBlocks_WithExpressionBody_ReturnsExpressionBody()
    {
        // Arrange
        var code = "class C { int M() => 42; }";
        var method = GetFirstNode<MethodDeclarationSyntax>(code);
        var detector = new MethodMemberDetector();

        // Act
        var blocks = detector.GetExecutableBlocks(method).ToList();

        // Assert
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<ArrowExpressionClauseSyntax>();
    }

    [Fact]
    public void ConstructorMemberDetector_SupportsConstructor_ReturnsTrue()
    {
        // Arrange
        var code = "class C { C() { } }";
        var constructor = GetFirstNode<ConstructorDeclarationSyntax>(code);
        var detector = new ConstructorMemberDetector();

        // Act
        var result = detector.SupportsNode(constructor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DestructorMemberDetector_SupportsDestructor_ReturnsTrue()
    {
        // Arrange
        var code = "class C { ~C() { } }";
        var destructor = GetFirstNode<DestructorDeclarationSyntax>(code);
        var detector = new DestructorMemberDetector();

        // Act
        var result = detector.SupportsNode(destructor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PropertyMemberDetector_SupportsExpressionBodiedProperty_ReturnsTrue()
    {
        // Arrange - expression-bodied property
        var code = "class C { int P => 42; }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);
        var detector = new PropertyMemberDetector();

        // Act
        var result = detector.SupportsNode(property);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PropertyMemberDetector_DoesNotSupportAccessorBasedProperty_ReturnsFalse()
    {
        // Arrange - property with accessors (not expression-bodied)
        var code = "class C { int P { get; set; } }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);
        var detector = new PropertyMemberDetector();

        // Act
        var result = detector.SupportsNode(property);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PropertyMemberDetector_GetExecutableBlocks_WithExpressionBody_ReturnsExpression()
    {
        // Arrange
        var code = "class C { int P => 42; }";
        var property = GetFirstNode<PropertyDeclarationSyntax>(code);
        var detector = new PropertyMemberDetector();

        // Act
        var blocks = detector.GetExecutableBlocks(property).ToList();

        // Assert
        blocks.Should().HaveCount(1);
    }

    [Fact]
    public void AccessorMemberDetector_SupportsGetAccessor_ReturnsTrue()
    {
        // Arrange
        var code = "class C { int P { get { return 1; } } }";
        var accessor = GetFirstNode<AccessorDeclarationSyntax>(code);
        var detector = new AccessorMemberDetector();

        // Act
        var result = detector.SupportsNode(accessor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void LocalFunctionMemberDetector_SupportsLocalFunction_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { void Local() { } } }";
        var localFunction = GetFirstNode<LocalFunctionStatementSyntax>(code);
        var detector = new LocalFunctionMemberDetector();

        // Act
        var result = detector.SupportsNode(localFunction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void LambdaMemberDetector_SupportsParenthesizedLambda_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { var x = () => 42; } }";
        var lambda = GetFirstNode<ParenthesizedLambdaExpressionSyntax>(code);
        var detector = new LambdaMemberDetector();

        // Act
        var result = detector.SupportsNode(lambda);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void LambdaMemberDetector_SupportsSimpleLambda_ReturnsTrue()
    {
        // Arrange - simple lambda without parentheses
        var code = "class C { void M() { System.Func<int, int> x = y => y * 2; } }";
        var lambda = GetFirstNode<SimpleLambdaExpressionSyntax>(code);
        var detector = new LambdaMemberDetector();

        // Act
        var result = detector.SupportsNode(lambda);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void LambdaMemberDetector_GetDisplayName_WithBlockLambda_ReturnsLambdaExpression()
    {
        // Arrange
        var code = "class C { void M() { var x = () => { return 42; }; } }";
        var lambda = GetFirstNode<ParenthesizedLambdaExpressionSyntax>(code);
        var detector = new LambdaMemberDetector();

        // Act
        var displayName = detector.GetMemberDisplayName(lambda);

        // Assert
        displayName.Should().Be("Lambda expression");
    }

    [Fact]
    public void AnonymousMethodMemberDetector_SupportsAnonymousMethod_ReturnsTrue()
    {
        // Arrange
        var code = "class C { void M() { var x = delegate { }; } }";
        var anonymousMethod = GetFirstNode<AnonymousMethodExpressionSyntax>(code);
        var detector = new AnonymousMethodMemberDetector();

        // Act
        var result = detector.SupportsNode(anonymousMethod);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void OperatorMemberDetector_SupportsOperator_ReturnsTrue()
    {
        // Arrange
        var code = "class C { public static C operator +(C a, C b) => a; }";
        var op = GetFirstNode<OperatorDeclarationSyntax>(code);
        var detector = new OperatorMemberDetector();

        // Act
        var result = detector.SupportsNode(op);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ConversionOperatorMemberDetector_SupportsConversionOperator_ReturnsTrue()
    {
        // Arrange
        var code = "class C { public static explicit operator int(C c) => 0; }";
        var convOp = GetFirstNode<ConversionOperatorDeclarationSyntax>(code);
        var detector = new ConversionOperatorMemberDetector();

        // Act
        var result = detector.SupportsNode(convOp);

        // Assert
        result.Should().BeTrue();
    }

    private static T GetFirstNode<T>(string code) where T : CSharpSyntaxNode
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        return root.DescendantNodes().OfType<T>().First();
    }
}
