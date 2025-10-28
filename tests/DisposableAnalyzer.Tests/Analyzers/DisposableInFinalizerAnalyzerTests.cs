using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using DisposableAnalyzer.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableInFinalizerAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableInFinalizerAnalyzerTests
{
    [Fact]
    public async Task FinalizerWithoutDisposeCall_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    {|#0:~TestClass|}()
    {
        // Should call Dispose(false)
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableInFinalizerAnalyzer.MissingDisposeCallRule)
            .WithLocation(0);

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task FinalizerDisposesFields_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    ~TestClass()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task FinalizerInNonDisposableClass_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass
{
    ~TestClass()
    {
        // Cleanup unmanaged resources - no IDisposable, so no warning needed
        Console.WriteLine(""Finalizing"");
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
