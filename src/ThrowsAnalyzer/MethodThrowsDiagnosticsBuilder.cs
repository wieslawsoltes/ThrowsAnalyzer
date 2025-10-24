using Microsoft.CodeAnalysis;

namespace ThrowsAnalyzer
{
    public static class MethodThrowsDiagnosticsBuilder
    {
        // THROWS001: Method contains throw statement
        public const string DiagnosticId001 = "THROWS001";
        private const string Category = "Usage";

        private static readonly LocalizableString Title001 = "Method contains throw statement";
        private static readonly LocalizableString MessageFormat001 = "Method '{0}' contains throw statement(s)";
        private static readonly LocalizableString Description001 = "Detects methods that contain throw statements.";

        public static readonly DiagnosticDescriptor MethodContainsThrowStatement = new DiagnosticDescriptor(
            DiagnosticId001,
            Title001,
            MessageFormat001,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description001);

        // THROWS002: Method contains unhandled throw statement
        public const string DiagnosticId002 = "THROWS002";

        private static readonly LocalizableString Title002 = "Method contains unhandled throw statement";
        private static readonly LocalizableString MessageFormat002 = "Method '{0}' contains throw statement(s) without try/catch handling";
        private static readonly LocalizableString Description002 = "Detects methods that contain throw statements not wrapped in try/catch blocks.";

        public static readonly DiagnosticDescriptor MethodContainsUnhandledThrow = new DiagnosticDescriptor(
            DiagnosticId002,
            Title002,
            MessageFormat002,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description002);
    }
}
