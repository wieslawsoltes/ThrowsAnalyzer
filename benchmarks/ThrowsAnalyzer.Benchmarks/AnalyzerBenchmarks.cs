using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ThrowsAnalyzer.Detection;

namespace ThrowsAnalyzer.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class AnalyzerBenchmarks
{
    private SyntaxTree _smallFileSyntaxTree = null!;
    private SyntaxTree _mediumFileSyntaxTree = null!;
    private SyntaxTree _largeFileSyntaxTree = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small file: 10 methods
        _smallFileSyntaxTree = CSharpSyntaxTree.ParseText(GenerateCodeFile(10));

        // Medium file: 100 methods
        _mediumFileSyntaxTree = CSharpSyntaxTree.ParseText(GenerateCodeFile(100));

        // Large file: 1000 methods
        _largeFileSyntaxTree = CSharpSyntaxTree.ParseText(GenerateCodeFile(1000));
    }

    private string GenerateCodeFile(int methodCount)
    {
        var methods = new List<string>();
        for (int i = 0; i < methodCount; i++)
        {
            methods.Add($@"
    void Method{i}()
    {{
        throw new InvalidOperationException();
    }}");
        }

        return $@"
using System;

namespace TestNamespace
{{
    class TestClass
    {{
        {string.Join("\n", methods)}
    }}
}}";
    }

    [Benchmark]
    public void ThrowStatementDetector_SmallFile()
    {
        var root = _smallFileSyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasThrow = ThrowStatementDetector.ContainsThrowStatement(method);
        }
    }

    [Benchmark]
    public void ThrowStatementDetector_MediumFile()
    {
        var root = _mediumFileSyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasThrow = ThrowStatementDetector.ContainsThrowStatement(method);
        }
    }

    [Benchmark]
    public void ThrowStatementDetector_LargeFile()
    {
        var root = _largeFileSyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasThrow = ThrowStatementDetector.ContainsThrowStatement(method);
        }
    }

    [Benchmark]
    public void TryCatchDetector_SmallFile()
    {
        var root = _smallFileSyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasTryCatch = TryCatchDetector.ContainsTryCatchBlock(method);
        }
    }

    [Benchmark]
    public void TryCatchDetector_MediumFile()
    {
        var root = _mediumFileSyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasTryCatch = TryCatchDetector.ContainsTryCatchBlock(method);
        }
    }

    [Benchmark]
    public void ParseAndAnalyze_SmallFile()
    {
        var code = GenerateCodeFile(10);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasThrow = ThrowStatementDetector.ContainsThrowStatement(method);
            var hasTryCatch = TryCatchDetector.ContainsTryCatchBlock(method);
        }
    }

    [Benchmark]
    public void ParseAndAnalyze_MediumFile()
    {
        var code = GenerateCodeFile(100);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var methods = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var hasThrow = ThrowStatementDetector.ContainsThrowStatement(method);
            var hasTryCatch = TryCatchDetector.ContainsTryCatchBlock(method);
        }
    }
}
