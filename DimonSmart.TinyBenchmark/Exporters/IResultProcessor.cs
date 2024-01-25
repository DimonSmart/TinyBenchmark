namespace DimonSmart.TinyBenchmark.Exporters;

public interface IResultProcessor
{
    IGraphExporter WithGraphExporter();
    ICsvExporter WithCsvExporter();
    ITableExporter WithTableExporter();
}