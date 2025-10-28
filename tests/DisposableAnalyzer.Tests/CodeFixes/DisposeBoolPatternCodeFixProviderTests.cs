using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposeBoolPatternAnalyzer,
    DisposableAnalyzer.CodeFixes.DisposeBoolPatternCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class DisposeBoolPatternCodeFixProviderTests
{
    [Fact]
    public async Task ImplementDisposeBoolPattern_BasicCase()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _stream?.Dispose();
        }
        // Dispose unmanaged resources
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableAnalyzer.Analyzers.DisposeBoolPatternAnalyzer.MissingDisposeBoolRule)
            .WithLocation(9, 17)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementDisposeBoolPattern_WithFinalizer()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;
    private IntPtr _unmanagedResource;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;
    private IntPtr _unmanagedResource;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            _stream?.Dispose();
        }
        // Dispose unmanaged resources
    }

    ~TestClass()
    {
        Dispose(false);
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableAnalyzer.Analyzers.DisposeBoolPatternAnalyzer.MissingDisposeBoolRule)
            .WithLocation(10, 17)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
