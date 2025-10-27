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
    /// Code fix provider for THROWS029: Exception in Hot Path.
    /// Offers to move validation outside loop or convert to Try pattern.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionInHotPathCodeFixProvider))]
    [Shared]
    public class ExceptionInHotPathCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS029");

        protected override string Title => "Fix exception in hot path";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the throw statement
            var throwStatement = root.FindNode(diagnosticSpan) as ThrowStatementSyntax 
                ?? root.FindNode(diagnosticSpan).FirstAncestorOrSelf<ThrowStatementSyntax>();
            
            if (throwStatement == null)
                return;

            // Find the containing loop
            var loop = FindContainingLoop(throwStatement);
            if (loop == null)
                return;

            // Register code fix to move validation outside loop
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Move validation before loop",
                    c => MoveValidationBeforeLoopAsync(context.Document, throwStatement, loop, c),
                    nameof(MoveValidationBeforeLoopAsync)),
                diagnostic);

            // Register code fix to use return value instead
            context.RegisterCodeFix(
                CreateCodeAction(
                    "Use return value instead of exception",
                    c => UseReturnValueAsync(context.Document, throwStatement, loop, c),
                    nameof(UseReturnValueAsync)),
                diagnostic);
        }

        private StatementSyntax? FindContainingLoop(SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is ForStatementSyntax or 
                    ForEachStatementSyntax or 
                    WhileStatementSyntax or 
                    DoStatementSyntax)
                {
                    return (StatementSyntax)current;
                }

                // Stop at method boundary
                if (current is MethodDeclarationSyntax or 
                    LocalFunctionStatementSyntax)
                {
                    break;
                }

                current = current.Parent;
            }
            return null;
        }

        private async Task<Document> MoveValidationBeforeLoopAsync(
            Document document,
            ThrowStatementSyntax throwStatement,
            StatementSyntax loop,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Extract the condition that causes the throw
            var throwCondition = ExtractThrowCondition(throwStatement);
            if (throwCondition == null)
                return document;

            // Create validation statement before loop
            // This is a simplified implementation - would need more sophisticated logic for real scenarios
            var validationComment = SyntaxFactory.Comment("// TODO: Implement validation before loop");
            var validationStatement = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.IdentifierName("ValidateInputs"))
                .WithLeadingTrivia(validationComment);

            // Find block containing the loop
            var block = loop.Parent as BlockSyntax;
            if (block != null)
            {
                var loopIndex = block.Statements.IndexOf(loop);
                var newBlock = block.WithStatements(
                    block.Statements.Insert(loopIndex, validationStatement));

                var newRoot = root.ReplaceNode(block, newBlock);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        private async Task<Document> UseReturnValueAsync(
            Document document,
            ThrowStatementSyntax throwStatement,
            StatementSyntax loop,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Replace throw with return false (simplified)
            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.FalseLiteralExpression))
                .WithLeadingTrivia(SyntaxFactory.Comment("// TODO: Consider using bool return value"))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(throwStatement, returnStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        private ExpressionSyntax? ExtractThrowCondition(ThrowStatementSyntax throwStatement)
        {
            // Try to find the if statement that contains the throw
            var ifStatement = throwStatement.FirstAncestorOrSelf<IfStatementSyntax>();
            return ifStatement?.Condition;
        }
    }
}
