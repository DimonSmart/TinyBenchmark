using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public class BenchmarkData
{
    public Func<IEnumerable<TimeSpan>, TimeSpan> GetResult { get; internal set; } = TimeSpanUtils.Get50Percentile;
    public int MaxFunctionRunTImeMilliseconds { get; internal set; } = 2000;

    public int MinFunctionExecutionCount { get; internal set; } = 20;

    public int MaxFunctionExecutionCount { get; internal set; } = 500;

    public IList<MethodExecutionResults> Results { get; } = new List<MethodExecutionResults>();
}