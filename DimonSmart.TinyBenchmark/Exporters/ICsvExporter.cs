namespace DimonSmart.TinyBenchmark.Exporters;

public interface ICsvExporter : IResultProcessor
{
    ICsvExporter SaveAllRawResults(int limit = int.MaxValue);
}