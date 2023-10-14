using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public interface ITinyBenchmarkRunner
{
    ITinyBenchmarkRunner Reset();
    ITinyBenchmarkRunner Run();
    ITinyBenchmarkRunner SaveRawResultsData();

    IGraphExporter WithGraphExporter();
}