using System;
using System.IO;

namespace DisposalPatterns.Examples;

/// <summary>
/// Demonstrates cross-method disposal analysis (DISP021-025)
/// Call graph tracking and ownership transfer patterns
/// </summary>
public class CrossMethodAnalysis
{
    // DISP021: Disposal not propagated across methods
    public void DisposalNotPropagated_Bad()
    {
        var stream = CreateStream(); // ⚠️ DISP021 - who disposes this?
        WriteData(stream);
        // stream is never disposed
    }

    // ✓ Fixed: Proper disposal after cross-method usage
    public void DisposalPropagated_Good()
    {
        var stream = CreateStream();
        try
        {
            WriteData(stream);
        }
        finally
        {
            stream?.Dispose(); // ✓ Good
        }
    }

    // ✓ Alternative: Using statement
    public void DisposalWithUsing_Good()
    {
        using var stream = CreateStream(); // ✓ Good
        WriteData(stream);
    }

    private FileStream CreateStream()
    {
        return new FileStream("test.txt", FileMode.Create);
    }

    private void WriteData(FileStream stream)
    {
        stream.WriteByte(42);
    }

    // DISP022: Disposable created but not returned
    public void HelperCreatesDisposable_Bad() // ⚠️ DISP022
    {
        var stream = new MemoryStream(); // Created but not returned
        ProcessStream(stream);
        // stream leaks - neither disposed nor returned
    }

    // ✓ Fixed: Return the disposable
    public MemoryStream HelperCreatesAndReturns_Good()
    {
        var stream = new MemoryStream();
        ProcessStream(stream);
        return stream; // ✓ Good - ownership transferred to caller
    }

    // ✓ Alternative: Dispose locally
    public void HelperCreatesAndDisposes_Good()
    {
        using var stream = new MemoryStream(); // ✓ Good - disposed locally
        ProcessStream(stream);
    }

    private void ProcessStream(Stream stream)
    {
        stream.WriteByte(42);
    }

    // DISP023: Resource leak across method boundaries
    public void MethodBoundaryLeak_Bad()
    {
        CreateAndProcessStream(); // ⚠️ DISP023 - stream created inside leaks
    }

    private void CreateAndProcessStream()
    {
        var stream = new FileStream("test.txt", FileMode.Create); // Leaks
        stream.WriteByte(42);
        // Not disposed, not returned
    }

    // ✓ Fixed: Proper disposal at method boundary
    public void MethodBoundaryFixed_Good()
    {
        CreateAndProcessStreamSafe();
    }

    private void CreateAndProcessStreamSafe()
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good
        stream.WriteByte(42);
    }

    // DISP024: Conditional ownership creates unclear disposal
    public void ConditionalOwnership_Bad(bool dispose)
    {
        var stream = new FileStream("test.txt", FileMode.Create);

        if (dispose) // ⚠️ DISP024 - conditional disposal is unclear
        {
            stream.Dispose();
        }
        // What if dispose is false? Stream leaks
    }

    // ✓ Fixed: Unconditional disposal
    public void ConditionalOwnershipFixed_Good(bool condition)
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good - always disposed

        if (condition)
        {
            stream.WriteByte(42);
        }
        else
        {
            stream.WriteByte(99);
        }
    }

    // ✓ Alternative: Finally block ensures disposal
    public void ConditionalOwnershipFinally_Good(bool condition)
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        try
        {
            if (condition)
            {
                stream.WriteByte(42);
            }
            else
            {
                stream.WriteByte(99);
            }
        }
        finally
        {
            stream?.Dispose(); // ✓ Good - always disposed
        }
    }

    // DISP025: Disposal not on all code paths
    public void NotDisposedOnAllPaths_Bad(bool condition)
    {
        var stream = new FileStream("test.txt", FileMode.Create); // ⚠️ DISP025

        if (condition)
        {
            stream.WriteByte(42);
            return; // Stream not disposed on this path!
        }

        stream.Dispose(); // Only disposed on non-return path
    }

    // ✓ Fixed: Disposal on all paths with finally
    public void DisposedOnAllPaths_Good(bool condition)
    {
        var stream = new FileStream("test.txt", FileMode.Create);
        try
        {
            if (condition)
            {
                stream.WriteByte(42);
                return;
            }

            stream.WriteByte(99);
        }
        finally
        {
            stream?.Dispose(); // ✓ Good - disposed on all paths
        }
    }

    // ✓ Alternative: Using statement
    public void DisposedOnAllPathsUsing_Good(bool condition)
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good

        if (condition)
        {
            stream.WriteByte(42);
            return;
        }

        stream.WriteByte(99);
    }

    // ✓ Complex control flow with proper disposal
    public void ComplexControlFlow_Good(int value)
    {
        using var stream = new FileStream("test.txt", FileMode.Create); // ✓ Good

        switch (value)
        {
            case 0:
                stream.WriteByte(0);
                return;

            case 1:
                stream.WriteByte(1);
                throw new InvalidOperationException();

            case 2:
                stream.WriteByte(2);
                break;

            default:
                stream.WriteByte(255);
                return;
        }

        stream.WriteByte(42);
        // Disposed on all paths: return, throw, break, fall-through
    }
}
