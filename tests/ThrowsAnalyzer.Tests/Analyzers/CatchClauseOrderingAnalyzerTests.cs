using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers;

[TestClass]
public class CatchClauseOrderingAnalyzerTests
{
    #region THROWS007 - Catch Clause Ordering Tests

    [TestMethod]
    public async Task CorrectCatchOrder_ShouldNotReportDiagnostic()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var orderingDiagnostics = diagnostics.Where(d => d.Id == "THROWS007").ToArray();
        Assert.AreEqual(0, orderingDiagnostics.Length);
    }

    [TestMethod]
    public async Task ExceptionBeforeArgumentException_ShouldReportTHROWS007()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var orderingDiagnostics = diagnostics.Where(d => d.Id == "THROWS007").ToArray();
        Assert.AreEqual(1, orderingDiagnostics.Length);
        Assert.IsTrue(orderingDiagnostics[0].GetMessage().Contains("ArgumentException"));
        Assert.IsTrue(orderingDiagnostics[0].GetMessage().Contains("Exception"));
    }

    [TestMethod]
    public async Task GeneralCatchBeforeTypedCatch_ShouldReportTHROWS007()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch
                    {
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var orderingDiagnostics = diagnostics.Where(d => d.Id == "THROWS007").ToArray();
        Assert.AreEqual(1, orderingDiagnostics.Length);
    }

    [TestMethod]
    public async Task CatchWithFilter_ShouldNotReportOrdering()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex) when (ex.Message == "A")
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var orderingDiagnostics = diagnostics.Where(d => d.Id == "THROWS007").ToArray();
        Assert.AreEqual(0, orderingDiagnostics.Length);
    }

    [TestMethod]
    public async Task MultipleUnreachableCatches_ShouldReportMultipleTHROWS007()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var orderingDiagnostics = diagnostics.Where(d => d.Id == "THROWS007").ToArray();
        Assert.AreEqual(2, orderingDiagnostics.Length);
    }

    #endregion

    #region THROWS008 - Empty Catch Block Tests

    [TestMethod]
    public async Task EmptyCatchBlock_ShouldReportTHROWS008()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var emptyDiagnostics = diagnostics.Where(d => d.Id == "THROWS008").ToArray();
        Assert.AreEqual(1, emptyDiagnostics.Length);
        Assert.IsTrue(emptyDiagnostics[0].GetMessage().Contains("Method"));
    }

    [TestMethod]
    public async Task CatchWithStatement_ShouldNotReportTHROWS008()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var emptyDiagnostics = diagnostics.Where(d => d.Id == "THROWS008").ToArray();
        Assert.AreEqual(0, emptyDiagnostics.Length);
    }

    [TestMethod]
    public async Task MultipleEmptyCatches_ShouldReportMultipleTHROWS008()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var emptyDiagnostics = diagnostics.Where(d => d.Id == "THROWS008").ToArray();
        Assert.AreEqual(2, emptyDiagnostics.Length);
    }

    [TestMethod]
    public async Task EmptyCatchInConstructor_ShouldReportTHROWS008()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var emptyDiagnostics = diagnostics.Where(d => d.Id == "THROWS008").ToArray();
        Assert.AreEqual(1, emptyDiagnostics.Length);
        Assert.IsTrue(emptyDiagnostics[0].GetMessage().Contains("Constructor"));
    }

    #endregion

    #region THROWS009 - Rethrow Only Catch Tests

    [TestMethod]
    public async Task CatchWithOnlyBareRethrow_ShouldReportTHROWS009()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var rethrowDiagnostics = diagnostics.Where(d => d.Id == "THROWS009").ToArray();
        Assert.AreEqual(1, rethrowDiagnostics.Length);
        Assert.IsTrue(rethrowDiagnostics[0].GetMessage().Contains("Method"));
    }

    [TestMethod]
    public async Task CatchWithLoggingAndRethrow_ShouldNotReportTHROWS009()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var rethrowDiagnostics = diagnostics.Where(d => d.Id == "THROWS009").ToArray();
        Assert.AreEqual(0, rethrowDiagnostics.Length);
    }

    [TestMethod]
    public async Task EmptyCatch_ShouldNotReportTHROWS009()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var rethrowDiagnostics = diagnostics.Where(d => d.Id == "THROWS009").ToArray();
        Assert.AreEqual(0, rethrowDiagnostics.Length);
    }

    [TestMethod]
    public async Task CatchWithThrowEx_ShouldNotReportTHROWS009()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        // Should report THROWS004 (anti-pattern) but not THROWS009
        var rethrowDiagnostics = diagnostics.Where(d => d.Id == "THROWS009").ToArray();
        Assert.AreEqual(0, rethrowDiagnostics.Length);
    }

    #endregion

    #region THROWS010 - Overly Broad Catch Tests

    [TestMethod]
    public async Task CatchSystemException_ShouldReportTHROWS010()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();
        Assert.AreEqual(1, broadDiagnostics.Length);
        Assert.IsTrue(broadDiagnostics[0].GetMessage().Contains("Exception"));
    }

    [TestMethod]
    public async Task CatchSpecificException_ShouldNotReportTHROWS010()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();
        Assert.AreEqual(0, broadDiagnostics.Length);
    }

    [TestMethod]
    public async Task GeneralCatch_ShouldReportTHROWS010()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch
                    {
                        Console.WriteLine("Error");
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();
        // General catch resolves to System.Exception, which is considered overly broad
        Assert.AreEqual(1, broadDiagnostics.Length);
    }

    [TestMethod]
    public async Task MultipleBroadCatches_ShouldReportMultipleTHROWS010()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method1()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                }

                void Method2()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();
        Assert.AreEqual(2, broadDiagnostics.Length);
    }

    #endregion

    #region Combined Scenarios

    [TestMethod]
    public async Task ComplexScenario_ShouldReportMultipleDiagnostics()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        // Should report:
        // - THROWS007: ArgumentException unreachable (masked by Exception)
        // - THROWS008: Empty catch block (Exception)
        // - THROWS008: Empty catch block (ArgumentException)
        // - THROWS010: Overly broad catch (Exception)
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS007"));
        Assert.AreEqual(2, diagnostics.Count(d => d.Id == "THROWS008"));
        Assert.IsTrue(diagnostics.Any(d => d.Id == "THROWS010"));
    }

    [TestMethod]
    public async Task PropertyAccessor_ShouldReportDiagnostics()
    {
        var testCode = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
                    {
                        try
                        {
                            return DoSomething();
                        }
                        catch (Exception)
                        {
                        }
                        return 0;
                    }
                }

                int DoSomething() => 42;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var emptyDiagnostics = diagnostics.Where(d => d.Id == "THROWS008").ToArray();
        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();

        Assert.AreEqual(1, emptyDiagnostics.Length);
        Assert.AreEqual(1, broadDiagnostics.Length);
    }

    [TestMethod]
    public async Task Lambda_ShouldReportDiagnostics()
    {
        var testCode = """
            using System;

            class TestClass
            {
                void Method()
                {
                    Action a = () =>
                    {
                        try
                        {
                            DoSomething();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    };
                }

                void DoSomething() { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<CatchClauseOrderingAnalyzer>(testCode);

        var rethrowDiagnostics = diagnostics.Where(d => d.Id == "THROWS009").ToArray();
        var broadDiagnostics = diagnostics.Where(d => d.Id == "THROWS010").ToArray();

        Assert.AreEqual(1, rethrowDiagnostics.Length);
        Assert.AreEqual(1, broadDiagnostics.Length);
    }

    #endregion
}
