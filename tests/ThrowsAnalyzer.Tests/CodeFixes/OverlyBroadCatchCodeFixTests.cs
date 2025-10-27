using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class OverlyBroadCatchCodeFixTests
{
    [TestMethod]
    public async Task BroadCatch_FilterAdded()
    {
        var source = """
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

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex) when (true /* TODO: Add condition */)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, OverlyBroadCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task BroadCatchWithoutVariable_FilterAddedWithDeclaration()
    {
        var source = """
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
                        Console.WriteLine("Error");
                    }
                }

                void DoSomething() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex) when (true /* TODO: Add condition */)
                    {
                        Console.WriteLine("Error");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, OverlyBroadCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task GeneralCatch_FilterAdded()
    {
        var source = """
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

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex) when (true /* TODO: Add condition */)
                    {
                        Console.WriteLine("Error");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, OverlyBroadCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task BroadCatchWithFilter_NoCodeFixOffered()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    catch (Exception ex) when (ex.Message.Contains("test"))
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<CatchClauseOrderingAnalyzer, OverlyBroadCatchCodeFixProvider>(source);
        Assert.AreEqual(0, fixCount, "Should not offer fix when filter already exists");
    }

    [TestMethod]
    public async Task BroadCatchWithComments_PreservesComments()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    // Catch all exceptions
                    catch (Exception ex)
                    {
                        // Log the error
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    try
                    {
                        DoSomething();
                    }
                    // Catch all exceptions
                    catch (Exception ex) when (true /* TODO: Add condition */)
                    {
                        // Log the error
                        Console.WriteLine(ex.Message);
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, OverlyBroadCatchCodeFixProvider>(
            source, expected);
    }
}
