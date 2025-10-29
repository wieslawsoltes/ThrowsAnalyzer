using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposalInAllPathsAnalyzer,
    DisposableAnalyzer.CodeFixes.MoveDisposalToFinallyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class MoveDisposalToFinallyCodeFixProviderTests
{
    [Fact]
    public async Task MoveDisposalToFinally_BasicCase()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        try
        {
            stream.ReadByte();
        }
        catch (Exception)
        {
            stream?.Dispose();
            throw;
        }
        stream?.Dispose();
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        try
        {
            stream.ReadByte();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            stream?.Dispose();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalInAllPaths)
            .WithSpan(9, 22, 9, 63)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task MoveDisposalToFinally_WithExistingFinally()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        try
        {
            stream.ReadByte();
        }
        finally
        {
            Console.WriteLine(""Cleanup"");
        }
        stream?.Dispose();
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        try
        {
            stream.ReadByte();
        }
        finally
        {
            Console.WriteLine(""Cleanup"");
            stream?.Dispose();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposalInAllPaths)
            .WithSpan(9, 22, 9, 63)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
