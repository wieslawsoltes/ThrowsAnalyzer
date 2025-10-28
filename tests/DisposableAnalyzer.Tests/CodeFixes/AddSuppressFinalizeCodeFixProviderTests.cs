using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.SuppressFinalizerPerformanceAnalyzer,
    DisposableAnalyzer.CodeFixes.AddSuppressFinalizeCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class AddSuppressFinalizeCodeFixProviderTests
{
    [Fact]
    public async Task AddSuppressFinalize_WhenFinalizerExists()
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
        // Missing GC.SuppressFinalize(this)
    }

    ~TestClass()
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
        _stream?.Dispose();
        // Missing GC.SuppressFinalize(this)
        GC.SuppressFinalize(this);
    }

    ~TestClass()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableAnalyzer.Analyzers.SuppressFinalizerPerformanceAnalyzer.MissingCallRule)
            .WithLocation(9, 17)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task RemoveSuppressFinalize_WhenNoFinalizer()
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
        GC.SuppressFinalize(this); // No finalizer exists
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
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableAnalyzer.Analyzers.SuppressFinalizerPerformanceAnalyzer.UnnecessaryCallRule)
            .WithLocation(9, 17)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task AddSuppressFinalize_InDisposeBoolPattern()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        Dispose(true);
        // Missing GC.SuppressFinalize(this)
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
    }

    ~TestClass()
    {
        Dispose(false);
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
        // Missing GC.SuppressFinalize(this)
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
    }

    ~TestClass()
    {
        Dispose(false);
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableAnalyzer.Analyzers.SuppressFinalizerPerformanceAnalyzer.MissingCallRule)
            .WithLocation(9, 17)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
