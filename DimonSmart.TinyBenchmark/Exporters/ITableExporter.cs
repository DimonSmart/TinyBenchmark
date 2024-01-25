namespace DimonSmart.TinyBenchmark.Exporters;

public interface ITableExporter : IResultProcessor
{
    ITableExporter SaveAllTablesResults();
}