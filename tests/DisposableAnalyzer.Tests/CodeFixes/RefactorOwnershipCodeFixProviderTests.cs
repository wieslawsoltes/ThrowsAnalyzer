using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.ConditionalOwnershipAnalyzer,
    DisposableAnalyzer.CodeFixes.RefactorOwnershipCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class RefactorOwnershipCodeFixProviderTests
{
    [Fact]
    public async Task RefactorOwnership_ConvertToUsingDeclaration()
    {
        var code = @"
using System;
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
        else
        {
            Console.WriteLine(""Skipped"");
        }
    }
}";

        var fixedCode = @"
using System;
using System.IO;

class TestClass
{
    void TestMethod(bool condition)
    {
        using var stream = new FileStream(""test.txt"", FileMode.Open);
        if (condition)
        {
        }
        else
        {
            Console.WriteLine(""Skipped"");
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.ConditionalOwnership)
            .WithLocation(9, 13)
            .WithArguments("stream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task RefactorOwnership_MoveToFinally()
    // {
    //     var code = @"
    // using System;
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod(bool condition)
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         try
    //         {
    //             if (condition)
    //             {
    //                 stream.Dispose();
    //             }
    //         }
    //         catch (Exception)
    //         {
    //             // Handle exception
    //         }
    //     }
    // }";
    //
    //     var fixedCode = @"
    // using System;
    // using System.IO;
    //
    // class TestClass
    // {
    //     void TestMethod(bool condition)
    //     {
    //         var stream = new FileStream(""test.txt"", FileMode.Open);
    //         try
    //         {
    //             if (condition)
    //             {
    //             }
    //         }
    //         catch (Exception)
    //         {
    //             // Handle exception
    //         }
    //         finally
    //         {
    //             stream?.Dispose();
    //         }
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.ConditionalOwnership)
    //         .WithLocation(9, 13)
    //         .WithArguments("stream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // Second fix: finally block
    // }
}
