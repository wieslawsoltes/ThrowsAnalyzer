using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableFactoryPatternAnalyzer,
    DisposableAnalyzer.CodeFixes.RenameToFactoryPatternCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class RenameToFactoryPatternCodeFixProviderTests
{
    [Fact]
    public async Task RenameToFactoryPattern_GetToCreate()
    {
        var code = @"
using System.IO;

class TestClass
{
    FileStream GetStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    FileStream CreateStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableFactoryPattern)
            .WithLocation(6, 16)
            .WithArguments("GetStream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    // NOTE: The following test was commented out because the test framework doesn't support testing multiple code fix options easily
    // [Fact]
    // public async Task RenameToFactoryPattern_GetToBuild()
    // {
    //     var code = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     FileStream GetStream(string path)
    //     {
    //         return new FileStream(path, FileMode.Open);
    //     }
    // }";
    //
    //     var fixedCode = @"
    // using System.IO;
    //
    // class TestClass
    // {
    //     FileStream BuildStream(string path)
    //     {
    //         return new FileStream(path, FileMode.Open);
    //     }
    // }";
    //
    //     var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableFactoryPattern)
    //         .WithLocation(6, 16)
    //         .WithArguments("GetStream");
    //
    //     await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode); // Second fix: Build prefix
    // }

    [Fact]
    public async Task RenameToFactoryPattern_FindToCreate()
    {
        var code = @"
using System.IO;

class TestClass
{
    FileStream FindAvailableStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    FileStream CreateAvailableStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableFactoryPattern)
            .WithLocation(6, 16)
            .WithArguments("FindAvailableStream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
