namespace DimonSmart.TinyBenchmark;

internal class BenchmarkData
{
    public int MaxFunctionRunTImeMilliseconds { get; private set; } = 1000;

    public int MinFunctionExecutionCount { get; private set; } = 1;

    public int MaxFunctionExecutionCount { get; private set; } = 1000;

    public IList<MethodExecutionResults> Results { get; } = new List<MethodExecutionResults>();
}