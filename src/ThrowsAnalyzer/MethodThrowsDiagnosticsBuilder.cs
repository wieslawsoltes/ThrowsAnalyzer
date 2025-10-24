using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer
{
    public static class MethodThrowsDiagnosticsBuilder
    {
        public const string DiagnosticId = "THROWS001";
        private const string Category = "Usage";

        private static readonly LocalizableString Title = "Method contains throw statement";
        private static readonly LocalizableString MessageFormat = "Method '{0}' contains throw statement(s)";
        private static readonly LocalizableString Description = "Detects methods that contain throw statements.";

        public static readonly DiagnosticDescriptor MethodContainsThrowStatement = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);
    }
}
