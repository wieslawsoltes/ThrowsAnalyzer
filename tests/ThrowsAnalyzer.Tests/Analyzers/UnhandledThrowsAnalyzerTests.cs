using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests;

[TestClass]
public class UnhandledThrowsAnalyzerTests
{
    [TestMethod]
    public async Task MethodWithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void ThrowingMethod()
                {
                    throw new System.Exception("Error");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("ThrowingMethod"));
    }

    [TestMethod]
    public async Task MethodWithHandledThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void SafeMethod()
                {
                    try
                    {
                        throw new System.Exception("Error");
                    }
                    catch (System.Exception)
                    {
                        // Handle it
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task MethodWithMixedThrows_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MixedMethod(int value)
                {
                    if (value < 0)
                        throw new System.ArgumentException("Negative"); // Unhandled

                    try
                    {
                        throw new System.Exception("Error"); // Handled
                    }
                    catch (System.Exception)
                    {
                        // Handle it
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task MethodWithoutThrows_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void SafeMethod()
                {
                    var x = 42;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task MethodWithThrowExpressionUnhandled_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                int ThrowingProperty() => throw new System.Exception("Error");
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task MethodWithAllThrowsHandled_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void CompletelyHandledMethod(int value)
                {
                    try
                    {
                        if (value < 0)
                            throw new System.ArgumentException("Negative");

                        if (value > 100)
                            throw new System.ArgumentException("Too large");
                    }
                    catch (System.ArgumentException)
                    {
                        // All throws are inside try block
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
