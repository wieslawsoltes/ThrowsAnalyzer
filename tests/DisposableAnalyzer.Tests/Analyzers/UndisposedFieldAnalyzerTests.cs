using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    DisposableAnalyzer.Analyzers.UndisposedFieldAnalyzer,
    Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace DisposableAnalyzer.Tests.Analyzers;

public class UndisposedFieldAnalyzerTests
{
    [Fact]
    public async Task DisposableFieldNotDisposed_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream {|#0:_stream|};

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        // Missing disposal
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedField)
            .WithLocation(0)
            .WithArguments("_stream", "TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task DisposableFieldProperlyDisposed_NoDiagnostic()
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
    public async Task MultipleDisposableFields_AllNotDisposed_ReportsMultipleDiagnostics()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream {|#0:_stream1|};
    private FileStream {|#1:_stream2|};

    public TestClass()
    {
        _stream1 = new FileStream(""test1.txt"", FileMode.Open);
        _stream2 = new FileStream(""test2.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        // Missing disposal
    }
}";

        var expected1 = VerifyCS.Diagnostic(DiagnosticIds.UndisposedField)
            .WithLocation(0)
            .WithArguments("_stream1", "TestClass");

        var expected2 = VerifyCS.Diagnostic(DiagnosticIds.UndisposedField)
            .WithLocation(1)
            .WithArguments("_stream2", "TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected1, expected2);
    }

    [Fact]
    public async Task DisposableFieldWithDisposeBoolPattern_ProperlyDisposed_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private FileStream _stream;
    private bool _disposed;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _stream?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task StaticDisposableField_DoesNotRequireDisposal_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private static FileStream _stream = new FileStream(""test.txt"", FileMode.Open);
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NonDisposableField_NoDiagnostic()
    {
        var code = @"
using System;

class TestClass : IDisposable
{
    private int _value;

    public void Dispose()
    {
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task DisposableFieldInNonDisposableClass_NoDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass
{
    private FileStream _stream;

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }
}";

        // Analyzer only checks classes implementing IDisposable
        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task ReadonlyDisposableFieldNotDisposed_ReportsDiagnostic()
    {
        var code = @"
using System;
using System.IO;

class TestClass : IDisposable
{
    private readonly FileStream {|#0:_stream|};

    public TestClass()
    {
        _stream = new FileStream(""test.txt"", FileMode.Open);
    }

    public void Dispose()
    {
        // Missing disposal
    }
}";

        var expected = VerifyCS.Diagnostic(DiagnosticIds.UndisposedField)
            .WithLocation(0)
            .WithArguments("_stream", "TestClass");

        await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }
}
