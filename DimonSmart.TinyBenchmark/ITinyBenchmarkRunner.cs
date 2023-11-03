using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    /// <summary>
    /// Try to limit benchmarking time to time specified
    /// Note. If you export all raw results - in case of small and fast functions
    /// it can add significant amount of time to the total test execution time
    /// If you mix long and short functions the total execution count will be proportionally corrected
    /// to satisfy this limit
    /// </summary>
    /// <param name="time">Maximum benchmarking time</param>
    /// <param name="benchmarkDurationLimitInitIterations">
    /// How many times each function should be executed for calculate limits to satisfy
    /// MaxRunExecutionTime
    /// </param>
    /// <returns></returns>
    ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time, int benchmarkDurationLimitInitIterations = 100);

    ITinyBenchmarkRunner WinMinMaxFunctionExecutionCount(int min, int? max = null);

    /// <summary>
    /// Run benchmark
    /// </summary>
    /// <returns></returns>
    IResultProcessor Run();
}