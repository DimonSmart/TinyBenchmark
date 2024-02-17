using static DimonSmart.TinyBenchmark.SortTimeDirection;

namespace DimonSmart.TinyBenchmark.Exporters;

public interface IGraphExporter : IResultProcessor
{
    public enum GraphExportOption
    {
        None,
        IncludeErrorMarks
    }

    int Width { get; }
    int Height { get; }
    string RawDataFileNameTemplate { get; }
    IGraphExporter GraphSize(int width, int height);
    IGraphExporter SetRawDataFileNameTemplate(string fileNameTemplate);
    IGraphExporter ExportAllRawGraph(SortTimeDirection sortTimesDirection = UnsortedTimes);

    IGraphExporter ExportRawGraph(string className, string methodName, object? parameter,
        SortTimeDirection sortTimesDirection);

    IGraphExporter ExportAllFunctionsCompareGraph(Type classType, GraphExportOption options = GraphExportOption.None);
    IGraphExporter ExportAllFunctionsCompareGraph(GraphExportOption options = GraphExportOption.None);
}