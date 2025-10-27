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
    /// Code fix provider for THROWS020: Async Synchronous Throw.
    /// Offers to move validation before async or add Task.Yield.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncSynchronousThrowCodeFixProvider))]
    [Shared]
    public class AsyncSynchronousThrowCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS020");

        protected override string Title => "Fix async synchronous throw";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the throw statement
            var throwStatement = root.FindNode(diagnosticSpan) as ThrowStatementSyntax;
            if (throwStatement == null)
                return;

            // Find the containing async method
            var method = throwStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null || method.Modifiers.All(m => !m.IsKind(SyntaxKind.AsyncKeyword)))
                return;

            // Register code fix to add Task.Yield
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Add 'await Task.Yield()' before throw",
                    c => AddTaskYieldAsync(context.Document, throwStatement, c),
                    nameof(AddTaskYieldAsync)),
                diagnostic);

            // Register code fix to use wrapper pattern
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Extract to wrapper method (recommended)",
                    c => ExtractToWrapperMethodAsync(context.Document, method, throwStatement, c),
                    nameof(ExtractToWrapperMethodAsync)),
                diagnostic);
        }

        private async Task<Document> AddTaskYieldAsync(
            Document document,
            ThrowStatementSyntax throwStatement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Create await Task.Yield() statement
            var taskYieldStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Task"),
                            SyntaxFactory.IdentifierName("Yield")))))
                .WithLeadingTrivia(SyntaxFactory.Comment("// Force async execution"))
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Find the statement containing the throw
            var containingStatement = throwStatement.FirstAncestorOrSelf<StatementSyntax>();
            if (containingStatement == null)
                return document;

            // Insert Task.Yield before the throw statement
            var block = containingStatement.Parent as BlockSyntax;
            if (block != null)
            {
                var index = block.Statements.IndexOf(containingStatement);
                var newBlock = block.WithStatements(
                    block.Statements.Insert(index, taskYieldStatement));

                var newRoot = root.ReplaceNode(block, newBlock);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        private async Task<Document> ExtractToWrapperMethodAsync(
            Document document,
            MethodDeclarationSyntax method,
            ThrowStatementSyntax throwStatement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Find all synchronous throws (throws before any await)
            var methodBody = method.Body;
            if (methodBody == null)
                return document;

            var awaitExpressions = methodBody.DescendantNodes().OfType<AwaitExpressionSyntax>().ToList();
            var throwStatements = methodBody.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();

            var synchronousThrows = throwStatements
                .Where(t => !awaitExpressions.Any() || 
                           t.SpanStart < awaitExpressions.First().SpanStart)
                .ToList();

            if (!synchronousThrows.Any())
                return document;

            // Create wrapper method (non-async) that validates and returns Task
            var wrapperMethod = CreateWrapperMethod(method, synchronousThrows);
            
            // Create internal async method
            var internalMethod = CreateInternalAsyncMethod(method, synchronousThrows);

            // Replace original method with wrapper and add internal method
            var containingClass = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass == null)
                return document;

            var newClass = containingClass.ReplaceNode(method, wrapperMethod);
            var memberIndex = newClass.Members.IndexOf(wrapperMethod);
            newClass = newClass.WithMembers(
                newClass.Members.Insert(memberIndex + 1, internalMethod));

            var newRoot = root.ReplaceNode(containingClass, newClass);
            return document.WithSyntaxRoot(newRoot);
        }

        private MethodDeclarationSyntax CreateWrapperMethod(
            MethodDeclarationSyntax originalMethod,
            System.Collections.Generic.List<ThrowStatementSyntax> synchronousThrows)
        {
            // Create validation statements (synchronous throws)
            var validationStatements = synchronousThrows.Select(t => 
                t.FirstAncestorOrSelf<StatementSyntax>()).Distinct().ToList();

            // Create return statement that calls internal method
            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(originalMethod.Identifier.Text + "Internal"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            originalMethod.ParameterList.Parameters.Select(p =>
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier)))))));

            var wrapperBody = SyntaxFactory.Block(
                validationStatements.Concat(new[] { returnStatement }));

            // Remove async modifier from wrapper
            var modifiers = originalMethod.Modifiers.Where(m => !m.IsKind(SyntaxKind.AsyncKeyword));

            return originalMethod
                .WithModifiers(SyntaxFactory.TokenList(modifiers))
                .WithBody(wrapperBody)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private MethodDeclarationSyntax CreateInternalAsyncMethod(
            MethodDeclarationSyntax originalMethod,
            System.Collections.Generic.List<ThrowStatementSyntax> synchronousThrows)
        {
            var methodBody = originalMethod.Body!;
            
            // Remove synchronous throw statements from body
            var throwsToRemove = synchronousThrows
                .Select(t => t.FirstAncestorOrSelf<StatementSyntax>())
                .Distinct()
                .ToList();

            var newBody = methodBody.RemoveNodes(throwsToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return originalMethod
                .WithIdentifier(SyntaxFactory.Identifier(originalMethod.Identifier.Text + "Internal"))
                .WithBody(newBody)
                .WithLeadingTrivia()
                .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
