using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableInConstructorAnalyzer,
    DisposableAnalyzer.CodeFixes.AddExceptionSafetyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class AddExceptionSafetyCodeFixProviderTests
{
    [Fact]
    public async Task AddExceptionSafety_InConstructor()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream1;
    private FileStream _stream2;

    public TestClass(string path1, string path2)
    {
        _stream1 = new FileStream(path1, FileMode.Open);
        _stream2 = new FileStream(path2, FileMode.Open); // May throw, leaving _stream1 undisposed
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream1;
    private FileStream _stream2;

    public TestClass(string path1, string path2)
    {
        try
        {
            _stream1 = new FileStream(path1, FileMode.Open);
            _stream2 = new FileStream(path2, FileMode.Open); // May throw, leaving _stream1 undisposed
        }
        catch
        {
            _stream1?.Dispose();
            throw;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInConstructor)
            .WithLocation(10, 12)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task AddExceptionSafety_WithMultipleResources()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream1;
    private FileStream _stream2;
    private FileStream _stream3;

    public TestClass(string path1, string path2, string path3)
    {
        _stream1 = new FileStream(path1, FileMode.Open);
        _stream2 = new FileStream(path2, FileMode.Open);
        _stream3 = new FileStream(path3, FileMode.Open);
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream1;
    private FileStream _stream2;
    private FileStream _stream3;

    public TestClass(string path1, string path2, string path3)
    {
        try
        {
            _stream1 = new FileStream(path1, FileMode.Open);
            _stream2 = new FileStream(path2, FileMode.Open);
            _stream3 = new FileStream(path3, FileMode.Open);
        }
        catch
        {
            _stream1?.Dispose();
            _stream2?.Dispose();
            _stream3?.Dispose();
            throw;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInConstructor)
            .WithLocation(11, 12)
            .WithArguments("TestClass");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
