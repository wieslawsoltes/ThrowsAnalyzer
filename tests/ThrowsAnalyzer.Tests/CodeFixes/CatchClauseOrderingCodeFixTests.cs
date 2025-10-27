using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class CatchClauseOrderingCodeFixTests
{
    [TestMethod]
    public async Task TwoCatches_WrongOrder_Reordered()
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
                        Console.WriteLine("General");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
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
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("General");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, CatchClauseOrderingCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task ThreeCatches_WrongOrder_Reordered()
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
                        Console.WriteLine("General");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("Mid");
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine("Specific");
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
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("Mid");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("General");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, CatchClauseOrderingCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task GeneralCatch_MovedToEnd()
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
                        Console.WriteLine("General");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
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
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    catch
                    {
                        Console.WriteLine("General");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, CatchClauseOrderingCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task CatchesWithComments_PreserveComments()
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
                    // Handle all exceptions
                    catch (Exception ex)
                    {
                        Console.WriteLine("General");
                    }
                    // Handle arguments
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
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
                    // Handle arguments
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine("Specific");
                    }
                    // Handle all exceptions
                    catch (Exception ex)
                    {
                        Console.WriteLine("General");
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, CatchClauseOrderingCodeFixProvider>(
            source, expected);
    }
}
