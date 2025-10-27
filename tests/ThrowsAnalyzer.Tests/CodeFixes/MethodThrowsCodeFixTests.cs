using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class MethodThrowsCodeFixTests
{
    [TestMethod]
    public async Task SimpleMethodWithThrow_WrapInTryCatch()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new InvalidOperationException();
                }
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
                        throw new InvalidOperationException();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, MethodThrowsCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task ConstructorWithThrow_WrapInTryCatch()
    {
        var source = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                public TestClass()
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, MethodThrowsCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task LocalFunctionWithThrow_WrapInTryCatch()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    void LocalFunc()
                    {
                        throw new InvalidOperationException();
                    }

                    LocalFunc();
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method()
                {
                    void LocalFunc()
                    {
                        try
                        {
                            throw new InvalidOperationException();
                        }
                        catch (InvalidOperationException ex)
                        {
                            // TODO: Handle exception appropriately
                            throw;
                        }
                    }

                    LocalFunc();
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, MethodThrowsCodeFixProvider>(
            source, expected, codeFixIndex: 0);
    }

    [TestMethod]
    public async Task OneCodeFixOffered_ForMethodWithThrow()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var fixCount = await CodeFixTestHelper.GetCodeFixCountAsync<MethodThrowsAnalyzer, MethodThrowsCodeFixProvider>(source);
        Assert.AreEqual(1, fixCount, "Should offer wrap in try-catch");
    }
}
