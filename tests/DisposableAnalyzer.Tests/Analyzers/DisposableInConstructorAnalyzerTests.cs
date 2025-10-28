using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableInConstructorAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableInConstructorAnalyzerTests
{
    [Fact]
    public async Task DisposableCreatedInConstructorAndStored_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableCreatedInConstructorButNotStored_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    public TestClass()
    {
        var stream = {|#0:new FileStream(""test.txt"", FileMode.Open)|};
        // stream goes out of scope without being disposed
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableInConstructor)
            .WithLocation(0)
            .WithArguments("FileStream");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableCreatedAndPassedToBase_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class BaseClass
{
    protected readonly Stream Stream;

    protected BaseClass(Stream stream)
    {
        Stream = stream;
    }
}

class DerivedClass : BaseClass
{
    public DerivedClass()
        : base(new FileStream(""test.txt"", FileMode.Open))
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableUsedWithUsingInConstructor_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private string _data;

    public TestClass()
    {
        using (var stream = new FileStream(""test.txt"", FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            _data = reader.ReadToEnd();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
