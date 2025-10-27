using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS022: Unobserved Task Exception.
    /// Offers to add await, assign to variable, or add continuation.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnobservedTaskExceptionCodeFixProvider))]
    [Shared]
    public class UnobservedTaskExceptionCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS022");

        protected override string Title => "Observe Task exception";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation expression
            var invocation = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation == null)
                return;

            // Register code fix to add await
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Add 'await'",
                    c => AddAwaitAsync(context.Document, invocation, c),
                    nameof(AddAwaitAsync)),
                diagnostic);

            // Register code fix to assign to variable
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Assign to variable for later observation",
                    c => AssignToVariableAsync(context.Document, invocation, c),
                    nameof(AssignToVariableAsync)),
                diagnostic);

            // Register code fix to add continuation
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Add error handling continuation",
                    c => AddContinuationAsync(context.Document, invocation, c),
                    nameof(AddContinuationAsync)),
                diagnostic);
        }

        private async Task<Document> AddAwaitAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Create await expression
            var awaitExpression = SyntaxFactory.AwaitExpression(invocation)
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Also need to make containing method async if it isn't
            var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null && !containingMethod.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
            {
                var newModifiers = containingMethod.Modifiers.Add(
                    SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                var newMethod = containingMethod.WithModifiers(newModifiers);

                // If return type is void, change to Task
                if (containingMethod.ReturnType is PredefinedTypeSyntax predefined &&
                    predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    newMethod = newMethod.WithReturnType(SyntaxFactory.IdentifierName("Task"));
                }

                var tempRoot = root.ReplaceNode(containingMethod, newMethod);
                root = tempRoot;
                invocation = root.FindNode(invocation.Span).FirstAncestorOrSelf<InvocationExpressionSyntax>()!;
            }

            var newRoot = root.ReplaceNode(invocation, awaitExpression);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AssignToVariableAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statement == null)
                return document;

            // Create variable declaration
            var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier("task"))
                        .WithInitializer(
                            SyntaxFactory.EqualsValueClause(invocation)))))
                .WithLeadingTrivia(SyntaxFactory.Comment("// Task can be awaited or observed later"))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(statement, variableDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddContinuationAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var statement = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statement == null)
                return document;

            // Use simple string replacement approach for continuation
            var continuationCode = $@"{invocation.ToFullString()}.ContinueWith(t =>
{{
    if (t.IsFaulted)
    {{
        // TODO: Log error
        Console.WriteLine($""Error: {{t.Exception}}"");
    }}
}})";

            var newStatement = SyntaxFactory.ParseStatement(continuationCode)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(statement, newStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
