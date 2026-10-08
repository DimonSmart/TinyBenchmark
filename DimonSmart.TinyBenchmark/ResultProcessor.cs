using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public class ResultProcessor : IResultProcessor
{
    protected readonly BenchmarkData Data;
    protected readonly ITinyBenchmarkRunner TinyBenchmarkRunner;

    public ResultProcessor(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data, BenchmarkRunResult runResult)
    {
        TinyBenchmarkRunner = tinyBenchmarkRunner;
        Data = data;
        RunResult = runResult;
    }

    public BenchmarkRunResult RunResult { get; }

    public IGraphExporter WithGraphExporter()
    {
        return new GraphExporter(TinyBenchmarkRunner, Data, RunResult);
    }

    public ICsvExporter WithCsvExporter()
    {
        return new CsvExporter(TinyBenchmarkRunner, Data, RunResult);
    }

    public ITableExporter WithTableExporter()
    {
        return new TableExporter(TinyBenchmarkRunner, Data, RunResult);
    }
}
