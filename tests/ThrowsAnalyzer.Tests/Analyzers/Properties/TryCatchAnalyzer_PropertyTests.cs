using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Properties;

[TestClass]
public class TryCatchAnalyzer_PropertyTests
{
    [TestMethod]
    public async Task PropertySetterWithTryCatch_ShouldReportDiagnostic()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS003", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task AutoPropertyWithoutTryCatch_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public string Name { get; set; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<TryCatchAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
