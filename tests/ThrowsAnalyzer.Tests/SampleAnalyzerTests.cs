using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests;

[TestClass]
public class SampleAnalyzerTests
{
    [TestMethod]
    public async Task MethodWithThrowStatement_ShouldReportDiagnostic()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<SampleAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("ThrowingMethod"));
    }

    [TestMethod]
    public async Task MethodWithoutThrowStatement_ShouldNotReportDiagnostic()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<SampleAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }

    [TestMethod]
    public async Task MethodWithThrowExpression_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                int ThrowingProperty() => throw new System.Exception("Error");
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<SampleAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("ThrowingProperty"));
    }

    [TestMethod]
    public async Task MethodWithMultipleThrows_ShouldReportOneDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                void MultipleThrows(int x)
                {
                    if (x < 0)
                        throw new System.ArgumentException("Negative");
                    if (x > 100)
                        throw new System.ArgumentException("Too large");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<SampleAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
        Assert.IsTrue(diagnostics[0].GetMessage().Contains("MultipleThrows"));
    }
}