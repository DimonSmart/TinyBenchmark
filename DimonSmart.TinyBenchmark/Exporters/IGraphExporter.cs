using static DimonSmart.TinyBenchmark.SortDirection;

namespace DimonSmart.TinyBenchmark.Exporters;

public interface IGraphExporter : IResultProcessor
{
    int Width { get; }
    int Height { get; }
    string RawDataFileNameTemplate { get; }
    IGraphExporter GraphSize(int width, int height);
    IGraphExporter SetRawDataFileNameTemplate(string fileNameTemplate);
    IGraphExporter OrderTimes(SortDirection direction = Ascending);
    IGraphExporter ExportAllRawGraph();
    IGraphExporter ExportRawGraph(string className, string methodName, object? parameter);
    IGraphExporter ExportAllFunctionsCompareGraph(Type classType);
    IGraphExporter ExportAllFunctionsCompareGraph();
}