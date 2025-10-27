using BenchmarkDotNet.Running;

namespace ThrowsAnalyzer.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<AnalyzerBenchmarks>();
    }
}
