using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using DisposableAnalyzer.Analyzers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.DisposableStructAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class DisposableStructAnalyzerTests
{
    [Fact]
    public async Task LargeDisposableStruct_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

struct {|#0:LargeStruct|} : IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;
    private FileStream _stream3;
    private byte[] _buffer;
    private long _value1;
    private long _value2;

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
        _stream3?.Dispose();
    }
}";

        // Expecting BoxingWarningRule for any disposable struct
        var expected = VerifyCS.Diagnostic(DisposableStructAnalyzer.BoxingWarningRule)
            .WithLocation(0)
            .WithArguments("LargeStruct");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task SmallDisposableStruct_ReportsBoxingWarning()
    {
        var code = @"
using System;

struct {|#0:SmallStruct|} : IDisposable
{
    private int _value;

    public void Dispose()
    {
    }
}";

        var expected = VerifyCS.Diagnostic(DisposableStructAnalyzer.BoxingWarningRule)
            .WithLocation(0)
            .WithArguments("SmallStruct");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task StructWithoutDisposable_NoDiagnostic()
    {
        var code = @"
struct TestStruct
{
    private int _value1;
    private long _value2;
    private byte[] _buffer;
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableClassWithManyFields_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream1;
    private FileStream _stream2;
    private FileStream _stream3;
    private byte[] _buffer;

    public void Dispose()
    {
        _stream1?.Dispose();
        _stream2?.Dispose();
        _stream3?.Dispose();
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
