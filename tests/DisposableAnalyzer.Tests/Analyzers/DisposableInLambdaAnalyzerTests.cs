using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableInLambdaAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableInLambdaAnalyzerTests
{
    [Fact]
    public async Task DisposableCreatedInLambdaNotDisposed_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;
using System.Linq;

class TestClass
{
    public void TestMethod()
    {
        var result = Enumerable.Range(0, 10)
            .Select(x => {|#0:new FileStream($""file{x}.txt"", FileMode.Open)|})
            .ToList();
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInLambda)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableCreatedInLambdaAndReturned_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public Func<FileStream> CreateStreamFactory()
    {
        return () => new FileStream(""test.txt"", FileMode.Open);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableUsedWithUsingInLambda_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        Action action = () =>
        {
            using (var stream = new FileStream(""test.txt"", FileMode.Open))
            {
                // Use stream
            }
        };
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposablePassedToMethodInLambda_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        Action action = () =>
        {
            ProcessStream(new FileStream(""test.txt"", FileMode.Open));
        };
    }

    private void ProcessStream(FileStream stream)
    {
        using (stream)
        {
            // Use stream
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
