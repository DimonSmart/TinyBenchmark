namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    ITinyBenchmarkRunner Reset();
    ITinyBenchmarkRunner Run();
    ITinyBenchmarkRunner SaveRawResultsData();
}