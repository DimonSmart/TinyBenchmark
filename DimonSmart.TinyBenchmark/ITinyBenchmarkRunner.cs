using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    ITinyBenchmarkRunner Reset();
    IResultProcessor Run();
}