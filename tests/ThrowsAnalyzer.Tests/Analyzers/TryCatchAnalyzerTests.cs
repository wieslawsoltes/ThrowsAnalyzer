using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests;

[TestClass]
public class TryCatchAnalyzerTests
{
    [TestMethod]
    public async Task MethodWithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MethodWithTry()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (System.Exception)
                    {
                        // Handle
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("MethodWithTry"));
    }

    [TestMethod]
    public async Task MethodWithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void SimpleMethod()
                {
                    var x = 42;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task MethodWithTryFinally_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MethodWithTryFinally()
                {
                    try
                    {
                        DoSomething();
                    }
                    finally
                    {
                        Cleanup();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task MethodWithTryCatchFinally_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MethodWithTryCatchFinally()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (System.Exception)
                    {
                        // Handle
                    }
                    finally
                    {
                        Cleanup();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task MethodWithMultipleTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MethodWithMultipleTry()
                {
                    try
                    {
                        DoFirst();
                    }
                    catch (System.Exception)
                    {
                        // Handle first
                    }

                    try
                    {
                        DoSecond();
                    }
                    catch (System.Exception)
                    {
                        // Handle second
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("MethodWithMultipleTry"));
    }

    [TestMethod]
    public async Task MethodWithNestedTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MethodWithNestedTry()
                {
                    try
                    {
                        try
                        {
                            DoInner();
                        }
                        catch (System.InvalidOperationException)
                        {
                            // Handle inner
                        }
                        DoOuter();
                    }
                    catch (System.Exception)
                    {
                        // Handle outer
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ExpressionBodiedMethod_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                int ExpressionMethod() => 42;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task EmptyMethod_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void EmptyMethod()
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
