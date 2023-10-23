using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public class BenchmarkData
{
    public int WarmUpCount { get; internal set; } = 100;
    public TimeSpan? MaxRunExecutionTime { get; internal set; }
    public int MinFunctionExecutionCount { get; internal set; } = 100;
    public int? MaxFunctionExecutionCount { get; internal set; } = null;
    public Func<IEnumerable<TimeSpan>, TimeSpan> GetResult { get; internal set; } = TimeSpanUtils.Percentile50;
    public IList<MethodExecutionResults> Results { get; internal set; } = new List<MethodExecutionResults>();
}