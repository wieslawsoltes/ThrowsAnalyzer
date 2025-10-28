using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableInIteratorAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableInIteratorAnalyzerTests
{
    [Fact]
    public async Task DisposableCreatedInIteratorNotYielded_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass
{
    public IEnumerable<int> GetNumbers()
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInIterator)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableUsedWithUsingInIterator_NoDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass
{
    public IEnumerable<string> ReadLines()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NonIteratorMethod_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void RegularMethod()
    {
        var stream = new FileStream(""test.txt"", FileMode.Open);
        stream.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task AsyncIteratorWithDisposable_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

class TestClass
{
    public async IAsyncEnumerable<int> GetNumbersAsync()
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(100);
            yield return i;
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInIterator)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }
}
