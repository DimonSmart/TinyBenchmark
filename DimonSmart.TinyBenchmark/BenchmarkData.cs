namespace DimonSmart.TinyBenchmark;

public class BenchmarkData
{
    public int MaxFunctionRunTImeMilliseconds { get; private set; } = 1000;

    public int MinFunctionExecutionCount { get; internal set; } = 1;

    public int MaxFunctionExecutionCount { get; internal set; } = 1000;

    public IList<MethodExecutionResults> Results { get; } = new List<MethodExecutionResults>();
}