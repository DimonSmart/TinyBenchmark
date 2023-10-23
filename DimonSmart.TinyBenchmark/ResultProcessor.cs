using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public class ResultProcessor : IResultProcessor
{
    protected readonly BenchmarkData Data;
    protected readonly ITinyBenchmarkRunner TinyBenchmarkRunner;


    public ResultProcessor(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data)
    {
        TinyBenchmarkRunner = tinyBenchmarkRunner;
        Data = data;
    }

    public IGraphExporter WithGraphExporter()
    {
        return new GraphExporter(TinyBenchmarkRunner, Data);
    }

    public ICsvExporter WithCsvExporter()
    {
        return new CsvExporter(TinyBenchmarkRunner, Data);
    }
}