namespace DimonSmart.TinyBenchmark.Exporters;

public interface ICsvExporter : IResultProcessor
{
    ICsvExporter LimitResultLines(int limit);
    ICsvExporter SaveAllRawResults();
}