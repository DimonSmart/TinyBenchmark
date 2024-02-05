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
    /// Gets the results of method execution.
    /// </summary>
    public IList<MethodExecutionResults> Results { get; internal set; } = new List<MethodExecutionResults>();

    /// <summary>
    /// Some methods execute very quickly, resulting in time measurements that are consistently close to zero.
    /// To obtain more discernible results, this property allows measuring a batch of executions at once.
    /// </summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to organize test results into subfolders.
    /// When enabled, subfolders are created under the main results folder, with each subfolder named after its corresponding test class.
    /// </summary>
    /// <value>
    /// <c>true</c> to create subfolders for each test class; otherwise, <c>false</c>. Default is <c>true</c>.
    /// </value>
    public bool ResultSubfolders { get; set; } = true;
}