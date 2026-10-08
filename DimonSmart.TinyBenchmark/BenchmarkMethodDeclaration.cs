using System.Reflection;

namespace DimonSmart.TinyBenchmark;

internal enum BenchmarkStrategy
{
    Fast,
    Regular,
    LongRunning
}

internal sealed record BenchmarkMethodDeclaration(
    MethodInfo Method,
    BenchmarkStrategy? Strategy,
    int? RequestedSampleCount);
