using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableBaseCallAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableBaseCallAnalyzerTests
{
    [Fact]
    public async Task DerivedClassWithoutBaseCall_ReportsDiagnostic()
    {
        var code = @"
using System;

class BaseClass : IDisposable
{
    public virtual void Dispose()
    {
    }
}

class DerivedClass : BaseClass
{
    public override void {|#0:Dispose|}()
    {
        // Missing base.Dispose()
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableBaseCall)
            .WithLocation(0)
            .WithArguments("DerivedClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DerivedClassWithBaseCall_NoDiagnostic()
    {
        var code = @"
using System;

class BaseClass : IDisposable
{
    public virtual void Dispose()
    {
    }
}

class DerivedClass : BaseClass
{
    public override void Dispose()
    {
        base.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposeBoolWithoutBaseCall_ReportsDiagnostic()
    {
        var code = @"
using System;

class BaseClass : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}

class DerivedClass : BaseClass
{
    protected override void {|#0:Dispose|}(bool disposing)
    {
        // Missing base.Dispose(disposing)
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableBaseCall)
            .WithLocation(0)
            .WithArguments("DerivedClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposeBoolWithBaseCall_NoDiagnostic()
    {
        var code = @"
using System;

class BaseClass : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}

class DerivedClass : BaseClass
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ClassWithoutBaseDisposable_NoDiagnostic()
    {
        var code = @"
using System;

class BaseClass
{
}

class DerivedClass : BaseClass, IDisposable
{
    public void Dispose()
    {
        // No base.Dispose() needed - base doesn't implement IDisposable
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ExplicitInterfaceImplementation_NoDiagnostic()
    {
        var code = @"
using System;

class BaseClass : IDisposable
{
    void IDisposable.Dispose()
    {
    }
}

class DerivedClass : BaseClass, IDisposable
{
    void IDisposable.Dispose()
    {
        ((IDisposable)this).Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
