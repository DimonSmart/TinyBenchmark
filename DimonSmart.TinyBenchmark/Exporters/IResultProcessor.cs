namespace DimonSmart.TinyBenchmark.Exporters;

public interface IResultProcessor
{
    /// <summary>The immutable result snapshot published by this run.</summary>
    BenchmarkRunResult RunResult { get; }

    IGraphExporter WithGraphExporter();
    ICsvExporter WithCsvExporter();
    ITableExporter WithTableExporter();
}
