using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Constructors;

[TestClass]
public class UnhandledThrowsAnalyzer_ConstructorTests
{
    [TestMethod]
    public async Task ConstructorWithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(string value)
                {
                    if (string.IsNullOrEmpty(value))
                        throw new System.ArgumentException("Value cannot be null or empty");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task ConstructorWithHandledThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public TestClass(string value)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(value))
                            throw new System.ArgumentException("Value cannot be null or empty");
                    }
                    catch (System.ArgumentException)
                    {
                        // Handle validation error
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
