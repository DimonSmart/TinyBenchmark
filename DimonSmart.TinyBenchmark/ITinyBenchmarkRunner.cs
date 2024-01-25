using DimonSmart.TinyBenchmark.Exporters;

public interface ITinyBenchmarkRunner
{
    /// <summary>
    /// Sets a maximum time limit for benchmarking. Note that exporting all raw results,
    /// especially for small and fast functions, can significantly increase the total execution time.
    /// If a mix of long and short functions is present, the total execution count will be
    /// proportionally adjusted to adhere to this limit.
    /// </summary>
    /// <param name="time">The maximum allowed time for benchmarking.</param>
    /// <param name="benchmarkDurationLimitInitIterations">
    /// The initial number of iterations for each function to calculate limits that satisfy
    /// the MaxRunExecutionTime constraint.
    /// Default value is 100.
    /// </param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time, int benchmarkDurationLimitInitIterations = 100);

    /// <summary>
    /// Sets a minimum execution count for each function in the benchmark.
    /// </summary>
    /// <param name="min">The minimum number of times each function should be executed.</param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMinFunctionExecutionCount(int min);

    /// <summary>
    /// Sets a maximum execution count for each function in the benchmark.
    /// </summary>
    /// <param name="max">The maximum number of times each function should be executed.</param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMaxFunctionExecutionCount(int max);

    /// <summary>
    /// Executes the benchmark and returns the results.
    /// </summary>
    /// <returns>An IResultProcessor containing the benchmark results.</returns>
    IResultProcessor Run();
}
