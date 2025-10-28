using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalyzer.Core.Members;
using RoslynAnalyzer.Core.Helpers;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS002: Method contains unhandled throw statement.
    /// Wraps unhandled throws in try-catch blocks.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnhandledThrowsCodeFixProvider))]
    [Shared]
    public class UnhandledThrowsCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string WrapTitle = "Wrap in try-catch block";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId002);

        /// <summary>
        /// Registers code fixes for unhandled throw diagnostics.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var (document, root) = await GetDocumentAndRootAsync(context, context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            // Find the member containing unhandled throws
            var memberNode = root.FindNode(diagnostic.Location.SourceSpan);

            if (!ExecutableMemberHelper.IsExecutableMember(memberNode))
            {
                return;
            }

            // Register code fix to wrap throws in try-catch
            context.RegisterCodeFix(
                CreateCodeAction(
                    WrapTitle,
                    cancellationToken => WrapInTryCatchAsync(document, root, memberNode, cancellationToken),
                    nameof(WrapInTryCatchAsync)),
                diagnostic);
        }

        /// <summary>
        /// Wraps the member body in a try-catch block.
        /// </summary>
        private static Task<Document> WrapInTryCatchAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode memberNode,
            CancellationToken cancellationToken)
        {
            var newRoot = root;
            BlockSyntax? bodyBlock = null;
            SyntaxNode? newMember = null;

            // Get the body block based on member type
            switch (memberNode)
            {
                case MethodDeclarationSyntax method when method.Body != null:
                    bodyBlock = method.Body;
                    var wrappedBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = method.WithBody(wrappedBody);
                    break;

                case ConstructorDeclarationSyntax ctor when ctor.Body != null:
                    bodyBlock = ctor.Body;
                    var wrappedCtorBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = ctor.WithBody(wrappedCtorBody);
                    break;

                case DestructorDeclarationSyntax dtor when dtor.Body != null:
                    bodyBlock = dtor.Body;
                    var wrappedDtorBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = dtor.WithBody(wrappedDtorBody);
                    break;

                case OperatorDeclarationSyntax op when op.Body != null:
                    bodyBlock = op.Body;
                    var wrappedOpBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = op.WithBody(wrappedOpBody);
                    break;

                case ConversionOperatorDeclarationSyntax conv when conv.Body != null:
                    bodyBlock = conv.Body;
                    var wrappedConvBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = conv.WithBody(wrappedConvBody);
                    break;

                case AccessorDeclarationSyntax accessor when accessor.Body != null:
                    bodyBlock = accessor.Body;
                    var wrappedAccessorBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = accessor.WithBody(wrappedAccessorBody);
                    break;

                case LocalFunctionStatementSyntax localFunc when localFunc.Body != null:
                    bodyBlock = localFunc.Body;
                    var wrappedLocalBody = WrapBlockInTryCatch(bodyBlock);
                    newMember = localFunc.WithBody(wrappedLocalBody);
                    break;

                default:
                    return Task.FromResult(document);
            }

            if (newMember != null)
            {
                newRoot = root.ReplaceNode(memberNode, newMember);
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        /// <summary>
        /// Wraps a block in a try-catch statement.
        /// </summary>
        private static BlockSyntax WrapBlockInTryCatch(BlockSyntax block)
        {
            // Determine the most common exception type from throw statements
            var exceptionType = GetMostCommonExceptionType(block) ?? "Exception";

            // Create TODO comment trivia
            var todoComment = SyntaxFactory.Comment("// TODO: Handle exception appropriately");
            var todoTrivia = SyntaxFactory.TriviaList(todoComment, SyntaxFactory.CarriageReturnLineFeed);

            // Create bare throw statement with TODO comment
            var throwStatement = SyntaxFactory.ThrowStatement()
                .WithLeadingTrivia(todoTrivia);

            // Create catch clause
            var catchDeclaration = SyntaxFactory.CatchDeclaration(
                SyntaxFactory.IdentifierName(exceptionType),
                SyntaxFactory.Identifier("ex"));

            var catchBlock = SyntaxFactory.Block(throwStatement);

            var catchClause = SyntaxFactory.CatchClause()
                .WithDeclaration(catchDeclaration)
                .WithBlock(catchBlock);

            // Create try statement with the original block's statements
            var tryStatement = SyntaxFactory.TryStatement()
                .WithBlock(block)
                .WithCatches(SyntaxFactory.SingletonList(catchClause))
                .NormalizeWhitespace();

            // Return a new block containing the try-catch
            return SyntaxFactory.Block(tryStatement)
                .NormalizeWhitespace();
        }

        /// <summary>
        /// Gets the most commonly thrown exception type in the block, or null.
        /// </summary>
        private static string? GetMostCommonExceptionType(BlockSyntax block)
        {
            var throwStatements = block.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null);

            var exceptionTypes = new List<string>();

            foreach (var throwStmt in throwStatements)
            {
                if (throwStmt.Expression is ObjectCreationExpressionSyntax objCreation)
                {
                    var typeName = objCreation.Type.ToString();
                    exceptionTypes.Add(typeName);
                }
            }

            // Return the most common type, or null if none found
            return exceptionTypes
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()
                ?.Key;
        }
    }
}
