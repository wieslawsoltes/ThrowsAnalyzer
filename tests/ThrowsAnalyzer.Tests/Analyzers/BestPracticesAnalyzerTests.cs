using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers;

[TestClass]
public class BestPracticesAnalyzerTests
{
    #region THROWS027 Tests (Exception Control Flow)

    [TestMethod]
    public async Task THROWS027_ExceptionForControlFlow_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    catch (InvalidOperationException)
                    {
                        // Exception used for control flow
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionControlFlowAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS027"));
    }

    [TestMethod]
    public async Task THROWS027_ExceptionPropagates_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionControlFlowAnalyzer>(testCode);

        var throws027 = diagnostics.Where(d => d.Id == "THROWS027").ToArray();
        Assert.AreEqual(0, throws027.Length);
    }

    [TestMethod]
    public async Task THROWS027_CatchAndRethrow_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoWork();
                    }
                    catch (InvalidOperationException)
                    {
                        // Log error
                        throw; // Rethrow
                    }
                }

                void DoWork() => throw new InvalidOperationException();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionControlFlowAnalyzer>(testCode);

        var throws027 = diagnostics.Where(d => d.Id == "THROWS027").ToArray();
        Assert.AreEqual(0, throws027.Length);
    }

    #endregion

    #region THROWS028 Tests (Custom Exception Naming)

    [TestMethod]
    public async Task THROWS028_CustomExceptionBadNaming_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                class InvalidState : Exception
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CustomExceptionNamingAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS028"));
    }

    [TestMethod]
    public async Task THROWS028_CustomExceptionGoodNaming_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                class InvalidStateException : Exception
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CustomExceptionNamingAnalyzer>(testCode);

        var throws028 = diagnostics.Where(d => d.Id == "THROWS028").ToArray();
        Assert.AreEqual(0, throws028.Length);
    }

    [TestMethod]
    public async Task THROWS028_NonExceptionClass_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                class InvalidState
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CustomExceptionNamingAnalyzer>(testCode);

        var throws028 = diagnostics.Where(d => d.Id == "THROWS028").ToArray();
        Assert.AreEqual(0, throws028.Length);
    }

    #endregion

    #region THROWS029 Tests (Exception In Hot Path)

    [TestMethod]
    public async Task THROWS029_ThrowInForLoop_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i == 5)
                            throw new InvalidOperationException();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionInHotPathAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS029"));
    }

    [TestMethod]
    public async Task THROWS029_ThrowInForeachLoop_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;
            using System.Collections.Generic;

            class TestClass
            {
                void Method()
                {
                    var items = new List<int> { 1, 2, 3 };
                    foreach (var item in items)
                    {
                        if (item < 0)
                            throw new InvalidOperationException();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionInHotPathAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS029"));
    }

    [TestMethod]
    public async Task THROWS029_ThrowInWhileLoop_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    int i = 0;
                    while (i < 10)
                    {
                        if (i == 5)
                            throw new InvalidOperationException();
                        i++;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionInHotPathAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS029"));
    }

    [TestMethod]
    public async Task THROWS029_ThrowOutsideLoop_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ExceptionInHotPathAnalyzer>(testCode);

        var throws029 = diagnostics.Where(d => d.Id == "THROWS029").ToArray();
        Assert.AreEqual(0, throws029.Length);
    }

    #endregion

    #region THROWS030 Tests (Result Pattern Suggestion)

    [TestMethod]
    public async Task THROWS030_ValidationMethodThrows_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void ValidateInput(string input)
                {
                    if (string.IsNullOrEmpty(input))
                        throw new ArgumentException("Input cannot be empty");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ResultPatternSuggestionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Length >= 1);
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS030"));
    }

    [TestMethod]
    public async Task THROWS030_ParseMethodThrows_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                int ParseValue(string input)
                {
                    if (string.IsNullOrEmpty(input))
                        throw new FormatException("Invalid format");

                    return int.Parse(input);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ResultPatternSuggestionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS030"));
    }

    [TestMethod]
    public async Task THROWS030_NonValidationMethod_ShouldNotReport()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void DoWork()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ResultPatternSuggestionAnalyzer>(testCode);

        var throws030 = diagnostics.Where(d => d.Id == "THROWS030").ToArray();
        Assert.AreEqual(0, throws030.Length);
    }

    [TestMethod]
    public async Task THROWS030_CheckMethodThrows_ShouldReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void CheckState(int value)
                {
                    if (value < 0)
                        throw new InvalidOperationException("Invalid state");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<ResultPatternSuggestionAnalyzer>(testCode);

        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS030"));
    }

    #endregion
}
