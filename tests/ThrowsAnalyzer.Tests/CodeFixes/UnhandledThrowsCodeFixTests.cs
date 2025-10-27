using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThrowsAnalyzer.CodeFixes;

namespace ThrowsAnalyzer.Tests.CodeFixes;

[TestClass]
public class UnhandledThrowsCodeFixTests
{
    [TestMethod]
    public async Task SimpleUnhandledThrow_WrappedInTryCatch()
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

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task MultipleStatementsWithThrow_AllWrappedInTryCatch()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    Console.WriteLine("Before");
                    throw new InvalidOperationException();
                    Console.WriteLine("After");
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
                        Console.WriteLine("Before");
                        throw new InvalidOperationException();
                        Console.WriteLine("After");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task MultipleThrowsOfSameType_UsesThatType()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method(bool condition)
                {
                    if (condition)
                        throw new ArgumentException();
                    else
                        throw new ArgumentException("message");
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method(bool condition)
                {
                    try
                    {
                        if (condition)
                            throw new ArgumentException();
                        else
                            throw new ArgumentException("message");
                    }
                    catch (ArgumentException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task MultipleThrowsOfDifferentTypes_UsesMostCommon()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method(int value)
                {
                    if (value < 0)
                        throw new ArgumentException();
                    if (value == 0)
                        throw new InvalidOperationException();
                    if (value > 100)
                        throw new ArgumentException();
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                void Method(int value)
                {
                    try
                    {
                        if (value < 0)
                            throw new ArgumentException();
                        if (value == 0)
                            throw new InvalidOperationException();
                        if (value > 100)
                            throw new ArgumentException();
                    }
                    catch (ArgumentException ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task NoTypedThrows_UsesGenericException()
    {
        var source = """
            using System;

            class TestClass
            {
                void Method()
                {
                    var ex = new InvalidOperationException();
                    throw ex;
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
                        var ex = new InvalidOperationException();
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        // TODO: Handle exception appropriately
                        throw;
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task UnhandledThrowInConstructor_Wrapped()
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

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task UnhandledThrowInPropertyGetter_Wrapped()
    {
        var source = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                public int Value
                {
                    get
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
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task UnhandledThrowInPropertySetter_Wrapped()
    {
        var source = """
            using System;

            class TestClass
            {
                private int _value;

                public int Value
                {
                    set
                    {
                        if (value < 0)
                            throw new ArgumentException();
                        _value = value;
                    }
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                private int _value;

                public int Value
                {
                    set
                    {
                        try
                        {
                            if (value < 0)
                                throw new ArgumentException();
                            _value = value;
                        }
                        catch (ArgumentException ex)
                        {
                            // TODO: Handle exception appropriately
                            throw;
                        }
                    }
                }
            }
            """;

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task UnhandledThrowInOperator_Wrapped()
    {
        var source = """
            using System;

            class TestClass
            {
                public static TestClass operator +(TestClass a, TestClass b)
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var expected = """
            using System;

            class TestClass
            {
                public static TestClass operator +(TestClass a, TestClass b)
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

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }

    [TestMethod]
    public async Task UnhandledThrowInLocalFunction_Wrapped()
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

        await CodeFixTestHelper.VerifyCodeFixAsync<MethodThrowsAnalyzer, UnhandledThrowsCodeFixProvider>(
            source, expected);
    }
}
