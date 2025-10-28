using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.CompositeDisposableRecommendedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class CompositeDisposableRecommendedAnalyzerTests
{
    [Fact]
    public async Task MultipleDisposableFieldsWithoutCompositeDisposable_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class {|#0:TestClass|} : IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;
    private FileStream _stream3;

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
        _stream3?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CompositeDisposableRecommended)
            .WithLocation(0)
            .WithArguments("TestClass", "3");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task FewDisposableFields_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithoutDisposable_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass
{
    private string _field1;
    private string _field2;
    private string _field3;
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
