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
using ThrowsAnalyzer.Core;
using ThrowsAnalyzer.TypeAnalysis;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS001: Method contains throw statements but lacks documentation.
    /// Offers two options: add XML documentation or wrap in try-catch.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodThrowsCodeFixProvider))]
    [Shared]
    public class MethodThrowsCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        private const string AddDocumentationTitle = "Add XML exception documentation";
        private const string WrapInTryCatchTitle = "Wrap in try-catch block";

        /// <summary>
        /// Gets the diagnostic IDs that this code fix provider can fix.
        /// </summary>
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(MethodThrowsDiagnosticsBuilder.DiagnosticId001);

        /// <summary>
        /// Registers code fixes for method throws diagnostics.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var (document, root) = await GetDocumentAndRootAsync(context, context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            // Find the member containing throws
            var memberNode = root.FindNode(diagnostic.Location.SourceSpan);

            if (!ExecutableMemberHelper.IsExecutableMember(memberNode))
            {
                return;
            }

            // Wrap throws in try-catch block
            context.RegisterCodeFix(
                CreateCodeAction(
                    WrapInTryCatchTitle,
                    cancellationToken => WrapInTryCatchAsync(document, root, memberNode, cancellationToken),
                    nameof(WrapInTryCatchAsync)),
                diagnostic);
        }

        /// <summary>
        /// Checks if XML documentation can be added to this member.
        /// </summary>
        private static bool CanAddXmlDocumentation(SyntaxNode memberNode)
        {
            // XML documentation is only supported for certain member types
            return memberNode is MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or PropertyDeclarationSyntax
                or IndexerDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax;
        }

        /// <summary>
        /// Adds XML exception documentation to the member.
        /// </summary>
        private static async Task<Document> AddXmlDocumentationAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode memberNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return document;
            }

            // Get all exception types thrown in this member
            var exceptionTypes = GetThrownExceptionTypes(memberNode, semanticModel);

            if (exceptionTypes.Count == 0)
            {
                return document;
            }

            // Get or create XML documentation
            var newMember = AddExceptionDocumentation(memberNode, exceptionTypes);

            if (newMember == null)
            {
                return document;
            }

            var newRoot = root.ReplaceNode(memberNode, newMember);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Wraps the member body in a try-catch block.
        /// </summary>
        private static async Task<Document> WrapInTryCatchAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode memberNode,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return document;
            }

            var newRoot = root;
            BlockSyntax? bodyBlock = null;
            SyntaxNode? newMember = null;

            // Get the body block based on member type
            switch (memberNode)
            {
                case MethodDeclarationSyntax method when method.Body != null:
                    bodyBlock = method.Body;
                    var wrappedBody = WrapBlockInTryCatch(bodyBlock, semanticModel);
                    newMember = method.WithBody(wrappedBody);
                    break;

                case ConstructorDeclarationSyntax ctor when ctor.Body != null:
                    bodyBlock = ctor.Body;
                    var wrappedCtorBody = WrapBlockInTryCatch(bodyBlock, semanticModel);
                    newMember = ctor.WithBody(wrappedCtorBody);
                    break;

                case AccessorDeclarationSyntax accessor when accessor.Body != null:
                    bodyBlock = accessor.Body;
                    var wrappedAccessorBody = WrapBlockInTryCatch(bodyBlock, semanticModel);
                    newMember = accessor.WithBody(wrappedAccessorBody);
                    break;

                case LocalFunctionStatementSyntax localFunc when localFunc.Body != null:
                    bodyBlock = localFunc.Body;
                    var wrappedLocalBody = WrapBlockInTryCatch(bodyBlock, semanticModel);
                    newMember = localFunc.WithBody(wrappedLocalBody);
                    break;

                default:
                    return document;
            }

            if (newMember != null)
            {
                newRoot = root.ReplaceNode(memberNode, newMember);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Gets all exception types thrown in a member.
        /// </summary>
        private static HashSet<string> GetThrownExceptionTypes(SyntaxNode memberNode, SemanticModel semanticModel)
        {
            var exceptionTypes = new HashSet<string>();

            var throwNodes = memberNode.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null);

            foreach (var throwNode in throwNodes)
            {
                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwNode, semanticModel);
                if (exceptionType != null)
                {
                    exceptionTypes.Add(exceptionType.ToDisplayString());
                }
            }

            return exceptionTypes;
        }

        /// <summary>
        /// Adds exception documentation to a member.
        /// </summary>
        private static SyntaxNode? AddExceptionDocumentation(SyntaxNode memberNode, HashSet<string> exceptionTypes)
        {
            // Get existing documentation or create new
            var existingTrivia = memberNode.GetLeadingTrivia();
            var docComment = existingTrivia
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                  || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            SyntaxTriviaList newTrivia;

            if (docComment != default)
            {
                // Add to existing documentation
                newTrivia = AddToExistingDocumentation(existingTrivia, docComment, exceptionTypes);
            }
            else
            {
                // Create new documentation
                newTrivia = CreateNewDocumentation(memberNode, exceptionTypes);
            }

            return memberNode.WithLeadingTrivia(newTrivia);
        }

        /// <summary>
        /// Creates new XML documentation with exception tags.
        /// </summary>
        private static SyntaxTriviaList CreateNewDocumentation(SyntaxNode memberNode, HashSet<string> exceptionTypes)
        {
            var existingTrivia = memberNode.GetLeadingTrivia();
            var indentation = GetIndentation(memberNode);
            var triviaList = new List<SyntaxTrivia>();

            // Preserve any existing leading trivia (except whitespace at the end)
            foreach (var trivia in existingTrivia)
            {
                if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    triviaList.Add(trivia);
                }
            }

            // Add summary
            triviaList.Add(SyntaxFactory.Whitespace(indentation));
            triviaList.Add(SyntaxFactory.Comment("/// <summary>"));
            triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            triviaList.Add(SyntaxFactory.Whitespace(indentation));
            triviaList.Add(SyntaxFactory.Comment("/// TODO: Add method description"));
            triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            triviaList.Add(SyntaxFactory.Whitespace(indentation));
            triviaList.Add(SyntaxFactory.Comment("/// </summary>"));
            triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);

            // Add exception tags
            foreach (var exceptionType in exceptionTypes.OrderBy(t => t))
            {
                var simpleName = GetSimpleTypeName(exceptionType);
                triviaList.Add(SyntaxFactory.Whitespace(indentation));
                triviaList.Add(SyntaxFactory.Comment($"/// <exception cref=\"{simpleName}\">TODO: Describe when this exception is thrown</exception>"));
                triviaList.Add(SyntaxFactory.CarriageReturnLineFeed);
            }

            // Add final indentation for the member itself
            triviaList.Add(SyntaxFactory.Whitespace(indentation));

            return SyntaxFactory.TriviaList(triviaList);
        }

        /// <summary>
        /// Adds exception tags to existing XML documentation.
        /// </summary>
        private static SyntaxTriviaList AddToExistingDocumentation(
            SyntaxTriviaList existingTrivia,
            SyntaxTrivia docComment,
            HashSet<string> exceptionTypes)
        {
            var indentation = GetIndentationFromTrivia(existingTrivia);
            var triviaList = existingTrivia.ToList();

            // Find the index of the doc comment
            var docIndex = triviaList.IndexOf(docComment);
            if (docIndex == -1)
            {
                return existingTrivia;
            }

            // Find the insertion point (after the last line of existing doc, before the next line)
            var insertIndex = docIndex + 1;
            while (insertIndex < triviaList.Count &&
                   (triviaList[insertIndex].IsKind(SyntaxKind.EndOfLineTrivia) ||
                    triviaList[insertIndex].IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                insertIndex++;
            }

            // Add exception tags
            var exceptionTrivia = new List<SyntaxTrivia>();
            foreach (var exceptionType in exceptionTypes.OrderBy(t => t))
            {
                var simpleName = GetSimpleTypeName(exceptionType);
                exceptionTrivia.Add(SyntaxFactory.Comment($"{indentation}/// <exception cref=\"{simpleName}\">TODO: Describe when this exception is thrown</exception>"));
                exceptionTrivia.Add(SyntaxFactory.CarriageReturnLineFeed);
            }

            triviaList.InsertRange(insertIndex, exceptionTrivia);

            return SyntaxFactory.TriviaList(triviaList);
        }

        /// <summary>
        /// Wraps a block in a try-catch statement.
        /// </summary>
        private static BlockSyntax WrapBlockInTryCatch(BlockSyntax block, SemanticModel semanticModel)
        {
            // Determine the most common exception type from throw statements
            var exceptionTypeName = GetMostCommonExceptionTypeName(block, semanticModel) ?? "Exception";

            // Create TODO comment trivia
            var todoComment = SyntaxFactory.Comment("// TODO: Handle exception appropriately");
            var todoTrivia = SyntaxFactory.TriviaList(todoComment, SyntaxFactory.CarriageReturnLineFeed);

            // Create bare throw statement with TODO comment
            var throwStatement = SyntaxFactory.ThrowStatement()
                .WithLeadingTrivia(todoTrivia);

            // Create catch clause
            var catchDeclaration = SyntaxFactory.CatchDeclaration(
                SyntaxFactory.IdentifierName(exceptionTypeName),
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
        /// Gets the most commonly thrown exception type name in the block (simple name only).
        /// </summary>
        private static string? GetMostCommonExceptionTypeName(BlockSyntax block, SemanticModel semanticModel)
        {
            var throwStatements = block.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null);

            var exceptionTypeNames = new List<string>();

            foreach (var throwStmt in throwStatements)
            {
                if (throwStmt.Expression is ObjectCreationExpressionSyntax objCreation)
                {
                    // Get the simple type name from syntax (not semantic model)
                    var typeName = objCreation.Type.ToString();
                    // Remove generic parameters and namespace qualifiers
                    var simpleName = GetSimpleTypeName(typeName);
                    exceptionTypeNames.Add(simpleName);
                }
            }

            // Return the most common type name, or null if none found
            return exceptionTypeNames
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()
                ?.Key;
        }

        /// <summary>
        /// Gets the most commonly thrown exception type in the block.
        /// </summary>
        private static string? GetMostCommonExceptionType(BlockSyntax block, SemanticModel semanticModel)
        {
            var throwStatements = block.DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Where(t => t.Expression != null);

            var exceptionTypes = new List<string>();

            foreach (var throwStmt in throwStatements)
            {
                var exceptionType = ExceptionTypeAnalyzer.GetThrownExceptionType(throwStmt, semanticModel);
                if (exceptionType != null)
                {
                    exceptionTypes.Add(exceptionType.ToDisplayString());
                }
            }

            // Return the most common type, or null if none found
            return exceptionTypes
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()
                ?.Key;
        }

        /// <summary>
        /// Gets the indentation string for a syntax node.
        /// </summary>
        private static string GetIndentation(SyntaxNode node)
        {
            var leadingTrivia = node.GetLeadingTrivia();
            var whitespace = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            return whitespace.ToString();
        }

        /// <summary>
        /// Gets the indentation string from trivia list.
        /// </summary>
        private static string GetIndentationFromTrivia(SyntaxTriviaList trivia)
        {
            var whitespace = trivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            return whitespace.ToString();
        }

        /// <summary>
        /// Gets the simple type name from a fully qualified type name.
        /// </summary>
        private static string GetSimpleTypeName(string fullyQualifiedName)
        {
            var lastDot = fullyQualifiedName.LastIndexOf('.');
            return lastDot >= 0 ? fullyQualifiedName.Substring(lastDot + 1) : fullyQualifiedName;
        }
    }
}
