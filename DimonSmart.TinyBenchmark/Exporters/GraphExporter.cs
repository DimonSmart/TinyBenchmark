using ScottPlot;
using static DimonSmart.TinyBenchmark.SortDirection;

namespace DimonSmart.TinyBenchmark.Exporters;

public class GraphExporter : ExporterBaseClass, IGraphExporter
{
    private readonly BenchmarkData _data;

    private readonly ITinyBenchmarkRunner _tinyBenchmarkRunner;

    public GraphExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data)
    {
        _tinyBenchmarkRunner = tinyBenchmarkRunner;
        _data = data;
        BeforeExport();
    }

    public SortDirection SortTimesDirection { get; set; } = Unordered;

    public int Width { get; private set; } = 800;
    public int Height { get; private set; } = 600;
    public string FileNameTemplate { get; set; } = "{ClassName}-{MethodName}-{Parameter}-{Sorted}.png";

    IGraphExporter IGraphExporter.GraphSize(int width, int height)
    {
        return GraphSize(width, height);
    }

    public IGraphExporter SetFileNameTemplate(string fileNameTemplate)
    {
        FileNameTemplate = fileNameTemplate;
        return this;
    }

    public IGraphExporter SortTimes(SortDirection direction)
    {
        SortTimesDirection = direction;
        return this;
    }

    public ITinyBenchmarkRunner Back()
    {
        return _tinyBenchmarkRunner;
    }

    public IGraphExporter ExportAllRawGraph()
    {
        foreach (var methodExecutionResult in _data.Results)
        {
            ExportRawGraph(methodExecutionResult);
        }

        return this;
    }

    public IGraphExporter ExportRawGraph(string className, string methodName, object? parameter)
    {
        var classes = _data
            .Results
            .Where(c => c.Method.ClassType.Name == className)
            .ToList();
        if (!classes.Any())
        {
            throw new ArgumentException("Class with name specified not found in results set", nameof(className));
        }

        var methods = classes
            .Where(f => f.Method.MethodInfo.Name == methodName)
            .ToList();
        if (!methods.Any())
        {
            throw new ArgumentException("Method with name specified not found in results set", nameof(methodName));
        }

        var parametrizedMethod = methods
            .Where(f => f.Method.Parameter == null && f.Method.Parameter == null ||
                        f.Method.Parameter.Equals(parameter));

        var executionResults = parametrizedMethod.Single();
        ExportRawGraph(executionResults);
        return this;
    }

    private GraphExporter GraphSize(int width, int height)
    {
        Width = width;
        Height = height;
        return this;
    }

    public IGraphExporter ExportRawGraph(MethodExecutionResults rmExecutionResults)
    {
        var className = rmExecutionResults.Method.ClassType.Name;
        var methodName = rmExecutionResults.Method.MethodInfo.Name;
        var parameter = rmExecutionResults.Method.Parameter;
        var dataX = rmExecutionResults.Times.Select((time, index) => (double)(index + 1)).ToArray();
        var dataY = rmExecutionResults.Times.Select(t => t.TotalMicroseconds).ToArray();
        if (SortTimesDirection == Ascending)
        {
            dataY = dataY.OrderBy(t => t).ToArray();
        }

        if (SortTimesDirection == Descending)
        {
            dataY = dataY.OrderByDescending(t => t).ToArray();
        }

        var myPlot = new Plot(Width, Height);
        myPlot.XLabel("Run number");
        myPlot.YLabel("Time, μs");
        myPlot.Title($"Raw data. {className}.{methodName}({rmExecutionResults.Method.Parameter})");
        myPlot.AddScatter(dataX, dataY);

        var fileName =
            SubstituteFilenameTemplate(FileNameTemplate, className, methodName, parameter, SortTimesDirection);
        myPlot.SaveFig(fileName);
        return this;
    }

    private string SubstituteFilenameTemplate(string template, string className, string methodName, object? parameter,
        SortDirection sorted)
    {
        var fileName = template.Replace("{methodName}", methodName, StringComparison.OrdinalIgnoreCase)
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase)
            .Replace("{parameter}", parameter?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{sorted}", sorted.ToString(), StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }
}