using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableFactoryPatternAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableFactoryPatternAnalyzerTests
{
    [Fact]
    public async Task FactoryMethodCreatingDisposableWithoutDocumentation_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public FileStream {|#0:CreateStream|}()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableFactoryPattern)
            .WithLocation(0)
            .WithArguments("CreateStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task FactoryMethodWithDocumentation_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    /// <summary>
    /// Creates a new file stream.
    /// </summary>
    /// <returns>A FileStream that must be disposed by the caller.</returns>
    public FileStream CreateStream()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task MethodReturningNonDisposable_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass
{
    public string CreateString()
    {
        return ""test"";
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task FactoryMethodReturningWrappedDisposable_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public IDisposable CreateResource()
    {
        return new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
