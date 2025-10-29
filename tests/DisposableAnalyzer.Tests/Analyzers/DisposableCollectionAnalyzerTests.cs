using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableCollectionAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableCollectionAnalyzerTests
{
    [Fact]
    public async Task CollectionOfDisposablesNotDisposed_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass
{
    private List<FileStream> {|#0:_streams|} = new List<FileStream>();

    public void AddStream(string path)
    {
        _streams.Add(new FileStream(path, FileMode.Open));
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.DisposableCollection)
            .WithLocation(0)
            .WithArguments("_streams", "TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task CollectionOfDisposablesWithDisposal_NoDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass : IDisposable
{
    private List<FileStream> _streams = new List<FileStream>();

    public void AddStream(string path)
    {
        _streams.Add(new FileStream(path, FileMode.Open));
    }

    public void Dispose()
    {
        foreach (var stream in _streams)
        {
            stream?.Dispose();
        }
        _streams.Clear();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task CollectionOfNonDisposables_NoDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;

class TestClass
{
    private List<string> _strings = new List<string>();

    public void AddString(string value)
    {
        _strings.Add(value);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task LocalCollectionOfDisposables_NoDiagnostic()
    {
        var code = @"
using System;
using System.Collections.Generic;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        var streams = new List<FileStream>();
        streams.Add(new FileStream(""test.txt"", FileMode.Open));
        // Local collections are checked by other analyzers
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
