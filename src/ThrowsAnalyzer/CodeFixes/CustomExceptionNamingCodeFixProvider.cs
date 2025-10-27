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
using Microsoft.CodeAnalysis.Rename;

namespace ThrowsAnalyzer.CodeFixes
{
    /// <summary>
    /// Code fix provider for THROWS028: Custom Exception Naming Convention.
    /// Renames exception types to end with "Exception" suffix.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CustomExceptionNamingCodeFixProvider))]
    [Shared]
    public class CustomExceptionNamingCodeFixProvider : ThrowsAnalyzerCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("THROWS028");

        protected override string Title => "Rename to follow naming convention";

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the class declaration
            var classDecl = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return;

            var currentName = classDecl.Identifier.Text;
            var newName = currentName.EndsWith("Exception") ? currentName : currentName + "Exception";

            // Register code fix
            context.RegisterCodeFix(
                CreateCodeAction(
                    $"Rename to '{newName}'",
                    c => RenameExceptionTypeAsync(context.Document, classDecl, newName, c),
                    nameof(RenameExceptionTypeAsync)),
                diagnostic);
        }

        private async Task<Solution> RenameExceptionTypeAsync(
            Document document,
            ClassDeclarationSyntax classDecl,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document.Project.Solution;

            var typeSymbol = semanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
            if (typeSymbol == null)
                return document.Project.Solution;

            // Use Roslyn's rename API to rename the symbol throughout the solution
            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(
                solution,
                typeSymbol,
                newName,
                solution.Options,
                cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
