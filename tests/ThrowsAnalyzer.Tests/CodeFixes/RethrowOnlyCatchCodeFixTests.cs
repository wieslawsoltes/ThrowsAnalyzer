using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class RethrowOnlyCatchCodeFixTests
{
    [TestMethod]
    public async Task RethrowOnlyCatch_Removed()
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
                    catch (InvalidOperationException)
                    {
                        throw;
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
                    DoSomething();
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task RethrowOnlyCatch_WithOtherCatches_OnlyTargetRemoved()
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
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex);
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
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
                        Console.WriteLine(ex);
                    }
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task RethrowOnlyCatch_WithFinally_CatchRemovedFinallyKept()
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
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    finally
                    {
                        Cleanup();
                    }
                }

                void DoSomething() { }
                void Cleanup() { }
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
                    finally
                    {
                        Cleanup();
                    }
                }

                void DoSomething() { }
                void Cleanup() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task RethrowOnlyCatch_WithCommentsInside_Removed()
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
                    catch (InvalidOperationException)
                    {
                        // Just rethrow
                        throw;
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
                    DoSomething();
                }

                void DoSomething() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task RethrowOnlyCatch_MultipleStatements_Unwrapped()
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
                        DoSomethingElse();
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                }

                void DoSomething() { }
                void DoSomethingElse() { }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    {
                        DoSomething();
                        DoSomethingElse();
                    }
                }

                void DoSomething() { }
                void DoSomethingElse() { }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<CatchClauseOrderingAnalyzer, RethrowOnlyCatchCodeFixProvider>(
            source, expected);
    }
}
