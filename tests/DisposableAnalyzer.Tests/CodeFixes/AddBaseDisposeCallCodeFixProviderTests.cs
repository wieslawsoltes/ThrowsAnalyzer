using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableBaseCallAnalyzer,
    DisposableAnalyzer.CodeFixes.AddBaseDisposeCallCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class AddBaseDisposeCallCodeFixProviderTests
{
    [Fact]
    public async Task AddBaseDisposeCall_InDisposeMethod()
    {
        var code = @"
using System;
using System.IO;

class BaseClass : IDisposable
{
    public virtual void Dispose()
    {
        // Base disposal
    }
}

class DerivedClass : BaseClass
{
    private FileStream _stream;

    public override void Dispose()
    {
        _stream?.Dispose();
        // Missing base.Dispose()
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class BaseClass : IDisposable
{
    public virtual void Dispose()
    {
        // Base disposal
    }
}

class DerivedClass : BaseClass
{
    private FileStream _stream;

    public override void Dispose()
    {
        _stream?.Dispose();
        // Missing base.Dispose()
        base.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableBaseCall)
            .WithLocation(17, 26)
            .WithArguments("DerivedClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task AddBaseDisposeCall_InDisposeBoolMethod()
    {
        var code = @"
using System;
using System.IO;

class BaseClass : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Base disposal
    }
}

class DerivedClass : BaseClass
{
    private FileStream _stream;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
        // Missing base.Dispose(disposing)
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class BaseClass : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Base disposal
    }
}

class DerivedClass : BaseClass
{
    private FileStream _stream;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
        // Missing base.Dispose(disposing)
        base.Dispose(disposing);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableBaseCall)
            .WithLocation(23, 29)
            .WithArguments("DerivedClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
