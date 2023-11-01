using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    ITinyBenchmarkRunner Reset();

    ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time);

    IResultProcessor Run();
}