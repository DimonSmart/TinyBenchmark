using ScottPlot;
using static DimonSmart.TinyBenchmark.SortDirection;

namespace DimonSmart.TinyBenchmark.Exporters;

public class GraphExporter : ExporterBaseClass, IGraphExporter
{
    private readonly ITinyBenchmarkRunner _tinyBenchmarkRunner;

    public GraphExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) : base(tinyBenchmarkRunner, data)
    {
        _tinyBenchmarkRunner = tinyBenchmarkRunner;
        BeforeExport();
    }

    public SortDirection SortTimesDirection { get; set; } = Unordered;
    public string ComparisionFileNameTemplate { get; set; } = "Compare-{ClassName}.png";
    public int Width { get; private set; } = 800;
    public int Height { get; private set; } = 600;
    public string RawDataFileNameTemplate { get; set; } = "Raw-{ClassName}-{MethodName}-{Parameter}-{Sorted}.png";

    IGraphExporter IGraphExporter.GraphSize(int width, int height)
    {
        return GraphSize(width, height);
    }

    public IGraphExporter SetRawDataFileNameTemplate(string fileNameTemplate)
    {
        RawDataFileNameTemplate = fileNameTemplate;
        return this;
    }

    public IGraphExporter OrderTimes(SortDirection direction)
    {
        SortTimesDirection = direction;
        return this;
    }

    public IGraphExporter ExportAllRawGraph()
    {
        foreach (var methodExecutionResult in Data.Results)
        {
            ExportRawGraph(methodExecutionResult);
        }

        return this;
    }

    public IGraphExporter ExportRawGraph(string className, string methodName, object? parameter)
    {
        var classes = Data
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
            .Where(f => (f.Method.Parameter == null && f.Method.Parameter == null) ||
                        f.Method.Parameter.Equals(parameter));

        var executionResults = parametrizedMethod.Single();
        ExportRawGraph(executionResults);
        return this;
    }

    public IGraphExporter ExportAllFunctionsCompareGraph(string className)
    {
        var classFunctions = Data
            .Results
            .Where(c => c.Method.ClassType.Name == className)
            .ToList();
        var byFunction = classFunctions
            .GroupBy(g => g.Method.MethodInfo.Name, v => v).ToList();

        var classRunParameters = byFunction.First()
            .Select(f => f.Method.Parameter)
            //.Distinct()
            .ToList();
        var plot = new Plot(Width, Height);
        plot.XLabel("Run number");

        double[] dataX;
        string[] labelX;
        if (classRunParameters.Count == 0)
        {
            dataX = new double[] { 1 };
            labelX = new[] { "1" };
        }
        else
        {
            dataX = classRunParameters.Select((value, index) => (double)index).ToArray();
            labelX = classRunParameters.Select((value, index) => value?.ToString()).ToArray();
        }

        plot.XAxis.ManualTickPositions(dataX, labelX);

        foreach (var function in byFunction)
        {
            var dataY = function
                .Select(f => TimeSpanUtils.Get50Percentile(f.Times).TotalMicroseconds).ToArray();
            plot.AddScatter(dataX, dataY, label: $"{function.Key}");
        }

        plot.Legend();
        var fileName = SubstituteComparisionFilenameTemplate(ComparisionFileNameTemplate, className);
        plot.SaveFig(fileName);
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
        var dataX = rmExecutionResults.Times.Select((_, index) => (double)(index + 1)).ToArray();
        var dataY = rmExecutionResults.Times.Select(t => t.TotalMicroseconds).ToArray();
        dataY = SortTimesDirection switch
        {
            Ascending => dataY.OrderBy(t => t).ToArray(),
            Descending => dataY.OrderByDescending(t => t).ToArray(),
            _ => dataY
        };

        var plot = new Plot(Width, Height);
        plot.XLabel("Run number");
        plot.YLabel("Time, μs");
        plot.Title($"Raw data. {className}.{methodName}({rmExecutionResults.Method.Parameter})");
        plot.AddScatter(dataX, dataY, label: "Raw timings");
        plot.Legend();
        var fileName =
            SubstituteFilenameTemplate(RawDataFileNameTemplate, className, methodName, parameter, SortTimesDirection);
        plot.SaveFig(fileName);
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

    private string SubstituteComparisionFilenameTemplate(string template, string className)
    {
        var fileName = template.Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }
}