namespace DimonSmart.TinyBenchmark.Exporters;

public interface IGraphExporter : IResultProcessor
{
    int Width { get; }
    int Height { get; }
    string RawDataFileNameTemplate { get; }
    IGraphExporter GraphSize(int width, int height);
    IGraphExporter SetRawDataFileNameTemplate(string fileNameTemplate);
    IGraphExporter ExportAllRawGraph(SortTimeDirection sortTimesDirection = SortTimeDirection.UnsortedTimes);

    IGraphExporter ExportRawGraph(string className, string methodName, object? parameter,
        SortTimeDirection sortTimesDirection);

    IGraphExporter ExportAllFunctionsCompareGraph(Type classType);
    IGraphExporter ExportAllFunctionsCompareGraph();
}