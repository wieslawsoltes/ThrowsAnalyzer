using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Properties;

[TestClass]
public class UnhandledThrowsAnalyzer_PropertyTests
{
    [TestMethod]
    public async Task PropertySetterWithUnhandledThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                private string _name;
                public string Name
                {
                    get => _name;
                    set
                    {
                        if (string.IsNullOrEmpty(value))
                            throw new System.ArgumentException("Name cannot be empty");
                        _name = value;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS002", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task PropertySetterWithHandledThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                private string _name;
                public string Name
                {
                    get => _name;
                    set
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(value))
                                throw new System.ArgumentException("Name cannot be empty");
                            _name = value;
                        }
                        catch (System.ArgumentException)
                        {
                            _name = "Default";
                        }
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<UnhandledThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
