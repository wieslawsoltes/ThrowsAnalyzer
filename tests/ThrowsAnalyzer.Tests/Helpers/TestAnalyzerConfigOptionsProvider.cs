using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ThrowsAnalyzer.Tests;

/// <summary>
/// Test implementation of AnalyzerConfigOptionsProvider for testing configuration
/// </summary>
public class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions _options;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
    {
        _options = new TestAnalyzerConfigOptions(options);
    }

    public override AnalyzerConfigOptions GlobalOptions => _options;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _backing;

        public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _backing = options.ToImmutableDictionary();
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _backing.TryGetValue(key, out value!);
        }

        public override IEnumerable<string> Keys => _backing.Keys;
    }
}
