using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Constructors;

[TestClass]
public class TryCatchAnalyzer_ConstructorTests
{
    [TestMethod]
    public async Task ConstructorWithTryCatch_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(string value)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(value))
                            throw new System.ArgumentException("Invalid value");
                    }
                    catch (System.ArgumentException)
                    {
                        // Handle validation error
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ConstructorWithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(int value)
                {
                    Value = value;
                }

                public int Value { get; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
