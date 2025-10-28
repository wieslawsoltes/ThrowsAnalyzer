using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DoubleDisposeAnalyzer,
    DisposableAnalyzer.CodeFixes.RemoveDoubleDisposeCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class RemoveDoubleDisposeCodeFixProviderTests
{
    [Fact]
    public async Task RemoveRedundantDispose_BasicCase()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
        stream.Dispose(); // Remove this
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(10, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task AddNullCheckAlternative_UsesNullConditional()
    // {
    //     var code = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         stream.Dispose();
    //         stream.Dispose();
    //     }
    // }";
    //
    //     var fixedCode = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod()
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         stream.Dispose();
    //         stream?.Dispose();
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
    //         .WithLocation(10, 9)
    //         .WithArguments("stream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // Second fix: null check
    // }

    [Fact]
    public async Task RemoveRedundantDispose_InConditionalBranch()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod(bool flag)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        if (flag)
        {
            stream.Dispose();
        }
        stream.Dispose();
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    void TestMethod(bool flag)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        if (flag)
        {
            stream.Dispose();
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(13, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
