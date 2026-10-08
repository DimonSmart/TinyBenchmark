using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    /// <summary>
    /// Sets a non-negative shared time limit for benchmarking. Exporting raw results is not included in this limit.
    /// </summary>
    /// <param name="time">The maximum allowed time for benchmarking.</param>
    /// <param name="benchmarkDurationLimitInitIterations">
    /// The number of legacy preparation invocations per case before useful samples are collected.
    /// Default value is 100.
    /// </param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time, int benchmarkDurationLimitInitIterations = 100);

    /// <summary>
    /// Removes the shared time limit for benchmarking.
    /// </summary>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithoutRunExecutionTimeLimit();

    /// <summary>
    /// Sets a minimum number of useful samples for each legacy benchmark case.
    /// </summary>
    /// <param name="min">The minimum number of times each function should be executed.</param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMinFunctionExecutionCount(int min);

    /// <summary>
    /// Sets a maximum number of useful samples for each legacy benchmark case.
    /// </summary>
    /// <param name="max">The maximum number of times each function should be executed.</param>
    /// <returns>An instance of ITinyBenchmarkRunner for method chaining.</returns>
    ITinyBenchmarkRunner WithMaxFunctionExecutionCount(int max);

    /// <summary>
    /// Configures the benchmark runner to use a custom logger.
    /// </summary>
    /// <param name="writeMessage">An Action delegate that takes a string message. This delegate is used for logging purposes.</param>
    /// <returns>An instance of ITinyBenchmarkRunner configured with the specified logger, for method chaining.</returns>
    ITinyBenchmarkRunner WithLogger(Action<string> writeMessage);

    /// <summary>
    /// Configures the benchmark runner to optionally use subfolders for storing results.
    /// When enabled, results are organized into subfolders named after the test classes.
    /// </summary>
    /// <param name="resultSubfolders">
    /// A boolean value indicating whether to use subfolders. 
    /// If true, results are stored in subfolders named by test class names. 
    /// If false, all results are stored in the main folder. 
    /// Default is true.
    /// </param>
    /// <returns>An instance of ITinyBenchmarkRunner configured with the specified result organization preference.</returns>
    ITinyBenchmarkRunner WithResultSubfolders(bool resultSubfolders = true);

    /// <summary>
    /// Measure memory consumption.
    /// NOTE! Memory consumption benchmarking could be slow.
    /// </summary>
    /// <param name="benchmarkMemory"></param>
    /// <returns></returns>
    ITinyBenchmarkRunner WithMemoryBenchmarking(bool benchmarkMemory = true);

    /// <summary>Controls whether useful raw samples are retained in the published run result.</summary>
    ITinyBenchmarkRunner WithRawSampleRetention(BenchmarkSampleRetentionMode retentionMode);

    /// <summary>
    /// Executes the benchmark and returns the results.
    /// </summary>
    /// <returns>An IResultProcessor containing the benchmark results.</returns>
    IResultProcessor Run(params Type[] types);
}
