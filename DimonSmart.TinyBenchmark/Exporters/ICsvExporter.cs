namespace DimonSmart.TinyBenchmark.Exporters;

public interface ICsvExporter : IResultProcessor
{
    ICsvExporter SaveRawResultsDataAsCsv();
}