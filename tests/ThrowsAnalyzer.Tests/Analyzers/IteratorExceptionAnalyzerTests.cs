using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers;

[TestClass]
public class IteratorExceptionAnalyzerTests
{
    #region THROWS023 Tests (Iterator Deferred Exception)

    [TestMethod]
    public async Task THROWS023_ThrowAfterYield_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    throw new InvalidOperationException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS023"));
    }

    [TestMethod]
    public async Task THROWS023_ThrowBeforeYield_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    throw new InvalidOperationException();
                    yield return 1;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        var throws023 = diagnostics.Where(d => d.Id == "THROWS023").ToArray();
        Assert.AreEqual(0, throws023.Length);
    }

    [TestMethod]
    public async Task THROWS023_ThrowBetweenYields_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    throw new InvalidOperationException();
                    yield return 2;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS023"));
    }

    [TestMethod]
    public async Task THROWS023_NonIteratorMethod_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    throw new InvalidOperationException();
                    return new List<int> { 1, 2, 3 };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        var throws023 = diagnostics.Where(d => d.Id == "THROWS023").ToArray();
        Assert.AreEqual(0, throws023.Length);
    }

    [TestMethod]
    public async Task THROWS023_MultipleThrowsAfterYield_ShouldReportMultipleDiagnostics()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    throw new InvalidOperationException();
                    yield return 2;
                    throw new ArgumentException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        var throws023 = diagnostics.Where(d => d.Id == "THROWS023").ToArray();
        Assert.AreEqual(2, throws023.Length);
    }

    [TestMethod]
    public async Task THROWS023_ThrowExpressionAfterYield_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    yield return 1;
                    var x = true ? throw new InvalidOperationException() : 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS023"));
    }

    [TestMethod]
    public async Task THROWS023_LocalFunctionIterator_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                void Method()
                {
                    IEnumerable<int> LocalIterator()
                    {
                        yield return 1;
                        throw new InvalidOperationException();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorDeferredExceptionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS023"));
    }

    #endregion

    #region THROWS024 Tests (Iterator Try-Finally)

    [TestMethod]
    public async Task THROWS024_TryFinallyWithYield_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        yield return 1;
                    }
                    finally
                    {
                        Console.WriteLine("Cleanup");
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS024"));
    }

    [TestMethod]
    public async Task THROWS024_TryFinallyNoYield_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        Console.WriteLine("Work");
                    }
                    finally
                    {
                        Console.WriteLine("Cleanup");
                    }
                    yield return 1;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        var throws024 = diagnostics.Where(d => d.Id == "THROWS024").ToArray();
        Assert.AreEqual(0, throws024.Length);
    }

    [TestMethod]
    public async Task THROWS024_NonIteratorTryFinally_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        Console.WriteLine("Work");
                    }
                    finally
                    {
                        Console.WriteLine("Cleanup");
                    }
                    return new List<int> { 1, 2, 3 };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        var throws024 = diagnostics.Where(d => d.Id == "THROWS024").ToArray();
        Assert.AreEqual(0, throws024.Length);
    }

    [TestMethod]
    public async Task THROWS024_NestedTryFinallyWithYield_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        try
                        {
                            yield return 1;
                        }
                        finally
                        {
                            Console.WriteLine("Inner cleanup");
                        }
                    }
                    finally
                    {
                        Console.WriteLine("Outer cleanup");
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        var throws024 = diagnostics.Where(d => d.Id == "THROWS024").ToArray();
        // Should report for both nested try-finally blocks
        Assert.IsTrue(throws024.Length >= 2);
    }

    [TestMethod]
    public async Task THROWS024_TryCatchFinallyWithYield_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                IEnumerable<int> Method()
                {
                    try
                    {
                        yield return 1;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error");
                    }
                    finally
                    {
                        Console.WriteLine("Cleanup");
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS024"));
    }

    [TestMethod]
    public async Task THROWS024_LocalFunctionIteratorTryFinally_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                void Method()
                {
                    IEnumerable<int> LocalIterator()
                    {
                        try
                        {
                            yield return 1;
                        }
                        finally
                        {
                            Console.WriteLine("Cleanup");
                        }
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<IteratorTryFinallyAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS024"));
    }

    #endregion
}
