using DimonSmart.TinyBenchmark.Utils;

namespace DimonSmart.TinyBenchmark;

/// <summary>
/// Represents the data and parameters for benchmarking.
/// </summary>
public class BenchmarkData
{
    /// <summary>
    /// Gets or sets the number of iterations used to determine typical execution times
    /// during initialization related to the benchmark duration limit.
    /// </summary>
    public int BenchmarkDurationLimitInitIterations { get; internal set; } = 100;

    /// <summary>
    /// Gets or sets the maximum allowed duration for the entire test run.
    /// </summary>
    public TimeSpan? BenchmarkDurationLimit { get; internal set; }

    /// <summary>
    /// Gets or sets the minimum number of function executions required during the benchmarking phase.
    /// </summary>
    public int MinFunctionExecutionCount { get; internal set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of function executions allowed during the benchmarking phase.
    /// </summary>
    public int? MaxFunctionExecutionCount { get; internal set; } = 10000;

    /// <summary>
    /// Gets or sets the method for obtaining the benchmark result from a collection of execution times.
    /// </summary>
    public Func<IEnumerable<TimeSpan>, TimeSpan> GetResult { get; internal set; } = TimeSpanUtils.Percentile50;

    /// <summary>
    /// Gets the results of method execution.
    /// </summary>
    public IList<MethodExecutionResults> Results { get; internal set; } = new List<MethodExecutionResults>();
}