using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.UsingDeclarationRecommendedAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class UsingDeclarationRecommendedAnalyzerTests
{
    [Fact]
    public async Task SimpleUsingStatementAtMethodStart_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        {|#0:using|} (var stream = new FileStream(""test.txt"", FileMode.Open))
        {
            var buffer = new byte[1024];
            stream.Read(buffer, 0, buffer.Length);
        }
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UsingDeclarationRecommended)
            .WithLocation(0)
            .WithArguments("stream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task UsingDeclarationAlreadyUsed_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using var stream = new FileStream(""test.txt"", FileMode.Open);
        var buffer = new byte[1024];
        stream.Read(buffer, 0, buffer.Length);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task UsingInConditional_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod(bool condition)
    {
        if (condition)
        {
            using (var stream = new FileStream(""test.txt"", FileMode.Open))
            {
                // Can't easily convert to using declaration
            }
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NestedUsingStatements_NoRecommendationForOuter()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            var line = reader.ReadLine();
        }
    }
}";

        // Chained using statements - both should NOT be flagged as they form a pattern
        // that's clearer as statements than declarations
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task UsingInTryCatch_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        try
        {
            using (var stream = new FileStream(""test.txt"", FileMode.Open))
            {
                // Process stream
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
