using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    DisposableAnalyzer.Analyzers.DisposableReturnedAnalyzer,
    DisposableAnalyzer.CodeFixes.DocumentDisposalOwnershipCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.CodeFixes;

public class DocumentDisposalOwnershipCodeFixProviderTests
{
    [Fact]
    public async Task DocumentDisposalOwnership_ForReturnedDisposable()
    {
        var code = @"
using System.IO;

class TestClass
{
    FileStream CreateStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    /// <summary>
    /// Creates a FileStream.
    /// </summary>
    /// <returns>A FileStream that the caller must dispose.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned resource.
    /// </remarks>
    FileStream CreateStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableReturned)
            .WithLocation(6, 16)
            .WithArguments("CreateStream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task DocumentDisposalOwnership_WithExistingSummary()
    {
        var code = @"
using System.IO;

class TestClass
{
    /// <summary>
    /// Gets a stream.
    /// </summary>
    FileStream GetStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var fixedCode = @"
using System.IO;

class TestClass
{
    /// <summary>
    /// Gets a stream.
    /// </summary>
    /// <returns>A FileStream that the caller must dispose.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned resource.
    /// </remarks>
    FileStream GetStream(string path)
    {
        return new FileStream(path, FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableReturned)
            .WithLocation(9, 16)
            .WithArguments("GetStream");

        await VerifyCS.VerifyCodeFixAsync(code, expected, fixedCode);
    }
}
