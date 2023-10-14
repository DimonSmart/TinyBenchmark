using static DimonSmart.TinyBenchmark.SortDirection;

namespace DimonSmart.TinyBenchmark.Exporters;

public interface IGraphExporter
{
    int Width { get; }
    int Height { get; }
    string FileNameTemplate { get; }
    IGraphExporter GraphSize(int width, int height);
    IGraphExporter SetFileNameTemplate(string fileNameTemplate);
    IGraphExporter OrderTimes(SortDirection direction = Ascending);
    ITinyBenchmarkRunner Back();
    IGraphExporter ExportAllRawGraph();
    IGraphExporter ExportRawGraph(string className, string methodName, object? parameter);
}