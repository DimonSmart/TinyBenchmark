namespace DimonSmart.TinyBenchmark;

public class BenchmarkData
{
    public int MaxFunctionRunTImeMilliseconds { get; internal set; } = 5000;

    public int MinFunctionExecutionCount { get; internal set; } = 100;

    public int MaxFunctionExecutionCount { get; internal set; } = 1000;

    public IList<MethodExecutionResults> Results { get; } = new List<MethodExecutionResults>();
}