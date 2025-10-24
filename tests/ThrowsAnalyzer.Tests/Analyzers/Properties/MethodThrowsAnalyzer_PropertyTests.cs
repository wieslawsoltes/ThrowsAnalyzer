using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ThrowsAnalyzer.Tests.Analyzers.Properties;

[TestClass]
public class MethodThrowsAnalyzer_PropertyTests
{
    [TestMethod]
    public async Task PropertyGetterWithThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public string Name
                {
                    get { throw new System.NotImplementedException(); }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task PropertySetterWithThrow_ShouldReportDiagnostic()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task PropertyWithExpressionBodyThrow_ShouldReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public string Name => throw new System.NotImplementedException();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual("THROWS001", diagnostics[0].Id);
    }

    [TestMethod]
    public async Task AutoPropertyWithoutThrow_ShouldNotReportDiagnostic()
    {
        var testCode = """
            class TestClass
            {
                public string Name { get; set; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<MethodThrowsAnalyzer>(testCode);

        Assert.AreEqual(0, diagnostics.Length);
    }
}
