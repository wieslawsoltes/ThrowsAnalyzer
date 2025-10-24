using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ThrowsAnalyzer.Tests;

[TestClass]
public class TryCatchDetectorTests
{
    [TestMethod]
    public void MethodWithTryCatch_ShouldDetectTryCatch()
    {
        var code = """
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

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);

        Assert.IsTrue(result, "Should detect try/catch block");
    }

    [TestMethod]
    public void MethodWithoutTryCatch_ShouldNotDetectTryCatch()
    {
        var code = """
            class TestClass
            {
                void MethodWithoutTry()
                {
                    var x = 42;
                }
            }
            """;

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);

        Assert.IsFalse(result, "Should not detect try/catch block");
    }

    [TestMethod]
    public void MethodWithMultipleTryCatch_ShouldDetectAll()
    {
        var code = """
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

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);
        var tryBlocks = TryCatchDetector.GetTryCatchBlocks(method).ToList();

        Assert.IsTrue(result, "Should detect try/catch blocks");
        Assert.AreEqual(2, tryBlocks.Count, "Should detect exactly 2 try/catch blocks");
    }

    [TestMethod]
    public void MethodWithNestedTryCatch_ShouldDetectNested()
    {
        var code = """
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

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);
        var tryBlocks = TryCatchDetector.GetTryCatchBlocks(method).ToList();

        Assert.IsTrue(result, "Should detect try/catch blocks");
        Assert.AreEqual(2, tryBlocks.Count, "Should detect both nested try/catch blocks");
    }

    [TestMethod]
    public void MethodWithTryFinally_ShouldDetect()
    {
        var code = """
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

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);

        Assert.IsTrue(result, "Should detect try/finally block");
    }

    [TestMethod]
    public void MethodWithTryCatchFinally_ShouldDetect()
    {
        var code = """
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

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);
        var tryBlocks = TryCatchDetector.GetTryCatchBlocks(method).ToList();

        Assert.IsTrue(result, "Should detect try/catch/finally block");
        Assert.AreEqual(1, tryBlocks.Count, "Should detect one try block");
    }

    [TestMethod]
    public void ExpressionBodiedMethod_ShouldNotDetectTryCatch()
    {
        var code = """
            class TestClass
            {
                int ExpressionMethod() => 42;
            }
            """;

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);

        Assert.IsFalse(result, "Expression-bodied methods cannot have try/catch");
    }

    [TestMethod]
    public void EmptyMethod_ShouldNotDetectTryCatch()
    {
        var code = """
            class TestClass
            {
                void EmptyMethod()
                {
                }
            }
            """;

        var method = AnalyzerTestHelper.GetMethodFromCode(code);
        var result = TryCatchDetector.HasTryCatchBlocks(method);

        Assert.IsFalse(result, "Empty method should not have try/catch");
    }
}
