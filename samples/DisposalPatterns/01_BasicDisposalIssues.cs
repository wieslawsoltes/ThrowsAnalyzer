using System;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates basic disposal issues (DISP001-003)
/// These examples show common mistakes with local disposable variables
/// </summary>
public class BasicDisposalIssues
{
    // DISP001: Local disposable not disposed
    public void LocalNotDisposed_Bad()
    {
        var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP001
        stream.WriteByte(42);
        // stream is never disposed
    }

    // ✓ Fixed: Using statement
    public void LocalWithUsing_Good()
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good
        stream.WriteByte(42);
    }

    // ✓ Fixed: Explicit disposal
    public void LocalWithExplicitDispose_Good()
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        try
        {
            stream.WriteByte(42);
        }
        finally
        {
            stream?.Dispose(); // ✓ Good
        }
    }

    // DISP003: Potential double disposal
    public void DoubleDispose_Bad()
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        stream.Dispose();
        stream.Dispose(); // ⚠️ DISP003 - double disposal
    }

    // ✓ Fixed: Null check prevents double disposal
    public void DoubleDisposeFixed_Good()
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        stream?.Dispose();
        stream = null;
        stream?.Dispose(); // ✓ Good - null check
    }

    // DISP004: Should use 'using' statement
    public void ShouldUseUsing_Bad()
    {
        var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP004
        stream.WriteByte(42);
        stream.Dispose(); // Manual disposal - should use 'using' instead
    }

    // ✓ Fixed: Using declaration (C# 8+)
    public void UsingDeclaration_Good()
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good
        stream.WriteByte(42);
    }

    // DISP006: Use using declaration instead of statement
    public void TraditionalUsing_Suggestion()
    {
        using (var stream = new FileStream("test.txt", FileMode.Create)) // ℹ️ DISP006 - suggest using declaration
        {
            stream.WriteByte(42);
        }
    }

    // DISP005: Using statement scope too broad
    public void UsingScopeToBroad_Bad()
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP005
        stream.WriteByte(42); // Used here

        // Stream is held open unnecessarily for all these operations
        Console.WriteLine("Processing...");
        Thread.Sleep(1000);
        Console.WriteLine("More work...");
        Thread.Sleep(1000);
        Console.WriteLine("Even more work...");
        Thread.Sleep(1000);
        Console.WriteLine("Done!");
    }

    // ✓ Fixed: Narrow using scope
    public void NarrowUsingScope_Good()
    {
        {
            using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good
            stream.WriteByte(42);
        } // Stream disposed here

        // Stream is closed before these operations
        Console.WriteLine("Processing...");
        Thread.Sleep(1000);
        Console.WriteLine("More work...");
        Thread.Sleep(1000);
        Console.WriteLine("Even more work...");
        Thread.Sleep(1000);
        Console.WriteLine("Done!");
    }
}
