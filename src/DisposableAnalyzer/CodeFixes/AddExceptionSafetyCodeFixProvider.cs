using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace DisposableAnalyzer.CodeFixes;

/// <summary>
/// Code fix that adds try-finally blocks for exception safety in constructors.
/// Fixes DISP018: DisposableInConstructor
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddExceptionSafetyCodeFixProvider))]
[Shared]
public class AddExceptionSafetyCodeFixProvider : CodeFixProvider
{
    private const string Title = "Wrap in try-finally for exception safety";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.DisposableInConstructor);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var constructor = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();

        if (constructor?.Body == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddTryCatchAsync(context.Document, constructor, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private async Task<Document> AddTryCatchAsync(
        Document document,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var assignedFields = new List<string>();
        var seen = new HashSet<string>();

        foreach (var statement in constructor.Body.Statements)
        {
            var fieldName = TryGetDisposableFieldName(statement, semanticModel, cancellationToken);
            if (string.IsNullOrEmpty(fieldName))
            {
                continue;
            }

            if (seen.Add(fieldName))
            {
                assignedFields.Add(fieldName);
            }
        }

        if (assignedFields.Count == 0)
        {
            return document;
        }

        var tryStatement = SyntaxFactory.TryStatement(
            SyntaxFactory.Block(constructor.Body.Statements),
            SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause()
                    .WithBlock(CreateCatchBlock(assignedFields))),
            null)
        .WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);

        var newBody = constructor.Body.WithStatements(
            SyntaxFactory.SingletonList<StatementSyntax>(tryStatement));

        var newConstructor = constructor.WithBody(newBody)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(constructor, newConstructor);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string? TryGetDisposableFieldName(
        StatementSyntax statement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var operation = semanticModel.GetOperation(statement, cancellationToken);
        if (operation is not IExpressionStatementOperation expressionStatement)
        {
            return null;
        }

        if (expressionStatement.Operation is not ISimpleAssignmentOperation assignment)
        {
            return null;
        }

        if (assignment.Target is not IFieldReferenceOperation fieldReference)
        {
            return null;
        }

        if (!IsDisposable(fieldReference.Field.Type))
        {
            return null;
        }

        return fieldReference.Field.Name;
    }

    private static BlockSyntax CreateCatchBlock(IEnumerable<string?> fieldNames)
    {
        var disposeStatements = fieldNames
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => CreateDisposeStatement(name!))
            .ToList();

        disposeStatements.Add(
            SyntaxFactory.ThrowStatement()
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation));

        return SyntaxFactory.Block(disposeStatements)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static StatementSyntax CreateDisposeStatement(string fieldName)
    {
        var identifier = SyntaxFactory.IdentifierName(fieldName);
        var disposeInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("Dispose")),
            SyntaxFactory.ArgumentList());

        var expression = SyntaxFactory.ConditionalAccessExpression(identifier, disposeInvocation);

        return SyntaxFactory.ExpressionStatement(expression)
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static bool IsDisposable(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_IDisposable ||
               type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable);
    }
}
