using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableNotImplementedAnalyzer,
    DisposableAnalyzer.CodeFixes.ImplementIDisposableCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class ImplementIDisposableCodeFixProviderTests
{
    [Fact]
    public async Task ImplementIDisposable_SingleField()
    {
        var code = @"
using System.IO;

class TestClass
{
    private FileStream _stream;
}";

        var fixedCode = @"
using System.IO;

class TestClass
: System.IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(4, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_MultipleFields()
    {
        var code = @"
using System.IO;

class TestClass
{
    private FileStream _stream1;
    private FileStream _stream2;
}";

        var fixedCode = @"
using System.IO;

class TestClass
: System.IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(4, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_WithExistingBaseClass()
    {
        var code = @"
using System.IO;

class BaseClass { }

class TestClass : BaseClass
{
    private FileStream _stream;
}";

        var fixedCode = @"
using System.IO;

class BaseClass { }

class TestClass : BaseClass, System.IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(6, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_WithExistingInterface()
    {
        var code = @"
using System;
using System.IO;

interface ICustom { }

class TestClass : ICustom
{
    private FileStream _stream;
}";

        var fixedCode = @"
using System;
using System.IO;

interface ICustom { }

class TestClass : ICustom, System.IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(7, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_IgnoresStaticFields()
    {
        var code = @"
using System.IO;

class TestClass
{
    private FileStream _instanceStream;
    private static FileStream _staticStream;
}";

        var fixedCode = @"
using System.IO;

class TestClass
: System.IDisposable
{
    private FileStream _instanceStream;
    private static FileStream _staticStream;

    public void Dispose()
    {
        _instanceStream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(4, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_ForStruct()
    {
        var code = @"
using System.IO;

struct TestStruct
{
    private FileStream _stream;
}";

        var fixedCode = @"
using System.IO;

struct TestStruct
: System.IDisposable
{
    private FileStream _stream;

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(4, 8)
            .WithArguments("TestStruct");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task ImplementIDisposable_WithExistingMethods()
    {
        var code = @"
using System.IO;

class TestClass
{
    private FileStream _stream;

    public void DoSomething()
    {
        _stream?.ReadByte();
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
: System.IDisposable
{
    private FileStream _stream;

    public void DoSomething()
    {
        _stream?.ReadByte();
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
            .WithLocation(4, 7)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: This test was commented out because Lazy<T> does not implement IDisposable
    // The DisposableNotImplementedAnalyzer correctly does not flag this case
    // [Fact]
    // public async Task ImplementIDisposable_WithGenericDisposableType()
    // {
    //     var code = @"
    // using System;
    // using System.IO;
    //
    // class TestClass
    // {
    //     private Lazy<FileStream> _lazyStream;
    // }";
    //
    //     var fixedCode = @"
    // using System;
    // using System.IO;
    //
    // class TestClass
    // : System.IDisposable
    // {
    //     private Lazy<FileStream> _lazyStream;
    //
    //     public void Dispose()
    //     {
    //         _lazyStream?.Dispose();
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableNotImplemented)
    //         .WithLocation(5, 7)
    //         .WithArguments("TestClass");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    // }
}
