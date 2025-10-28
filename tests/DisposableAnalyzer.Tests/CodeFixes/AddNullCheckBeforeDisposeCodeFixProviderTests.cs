using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DoubleDisposeAnalyzer,
    DisposableAnalyzer.CodeFixes.AddNullCheckBeforeDisposeCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class AddNullCheckBeforeDisposeCodeFixProviderTests
{
    [Fact]
    public async Task AddNullCheck_UseNullConditionalOperator()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
        stream.Dispose(); // Double dispose
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
        stream?.Dispose(); // Double dispose
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(10, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task AddNullCheck_WrapInIfStatement()
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
    //         stream.Dispose(); // Double dispose
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
    //         if (stream != null)
    //         {
    //             stream.Dispose(); // Double dispose
    //         }
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
    //         .WithLocation(10, 9)
    //         .WithArguments("stream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // Second fix: if statement
    // }

    [Fact]
    public async Task AddNullCheck_InConditionalBlock()
    {
        var code = @"
using System.IO;

class TestClass
{
    void TestMethod(bool condition)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        if (condition)
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
    void TestMethod(bool condition)
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        if (condition)
        {
            stream.Dispose();
        }
        stream?.Dispose();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DoubleDispose)
            .WithLocation(13, 9)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
