using DimonSmart.TinyBenchmark.Utils;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using static DimonSmart.TinyBenchmark.SortTimeDirection;
using static DimonSmart.TinyBenchmark.Exporters.IGraphExporter;

namespace DimonSmart.TinyBenchmark.Exporters;

public class GraphExporter : ExporterBaseClass, IGraphExporter
{
    private readonly ITinyBenchmarkRunner _tinyBenchmarkRunner;

    public GraphExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) : base(tinyBenchmarkRunner, data)
    {
        _tinyBenchmarkRunner = tinyBenchmarkRunner;
    }

    public string ComparisonFileNameTemplate { get; set; } = "Compare-{ClassName}.png";
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

    public IGraphExporter ExportAllRawGraph(SortTimeDirection sortTimesDirection)
    {
        foreach (var methodExecutionResult in Data.Results)
        {
            ExportRawGraph(methodExecutionResult, sortTimesDirection);
        }

        return this;
    }

    public IGraphExporter ExportRawGraph(string className, string methodName, object? parameter,
        SortTimeDirection sortTimesDirection = UnsortedTimes)
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
            .Where(mer => (mer.Method.Parameter == null && parameter == null) ||
                          (parameter != null && parameter.Equals(mer.Method.Parameter)));

        var executionResults = parametrizedMethod.SingleOrDefault();
        if (executionResults == null)
        {
            throw new ArgumentException(
                $"Benchmark with class:{className}, Method:{methodName} and Parameter:{(parameter == null ? "NULL" : parameter.ToString())}");
        }

        ExportRawGraph(executionResults, sortTimesDirection);
        return this;
    }

    public IGraphExporter ExportAllFunctionsCompareGraph(GraphExportOption options)
    {
        DoExportByClass(options);
        return this;
    }

    protected override void ExportOneClass(Type type, object? options)
    {
        if (options is not GraphExportOption graphOptions)
        {
            throw new ArgumentException("Invalid options type", nameof(options));
        }
        ExportAllFunctionsCompareGraph(type, graphOptions);
    }

    public IGraphExporter ExportAllFunctionsCompareGraph(Type classType, GraphExportOption options)
    {
        var classFunctions = Data
            .Results
            .Where(c => c.Method.ClassType == classType)
            .ToList();
        var byFunction = classFunctions
            .GroupBy(g => g.Method.MethodInfo.Name, v => v).ToList();

        var classRunParameters = byFunction.First()
            .Select(f => f.Method.Parameter)
            .ToList();
        var plot = new Plot();
        plot.XLabel("Run number");
        plot.YLabel("Time, μs");
        plot.Title($"{classType.Name}");

        var axisX = classRunParameters
            .Select((value, index) => new { Key = index, Value = value })
            .ToDictionary(pair => (double)pair.Key, pair => pair.Value?.ToString() ?? "X");
        var labelX = axisX.Keys.ToArray();

        plot.Axes.Bottom.TickGenerator = new NumericAutomatic
        {
            LabelFormatter = d => axisX.GetValueOrDefault(d, "!")
        };

        foreach (var function in byFunction)
        {
            var dataY = function
                .Select(f => f.Numbers.CalculatePercentile(i => i.PureMethodTime, 50).TotalNanoseconds/Data.BatchSize)
                .ToArray();
            var scatter = plot.Add.Scatter(labelX, dataY);
            scatter.LineStyle.Width = 2;
            scatter.Label = $"{function.Key}";
            if (options == GraphExportOption.IncludeErrorMarks)
            {
                AddErrorMarks(function, dataY, scatter);
            }
        }

        plot.ShowLegend();
        var fileName = CreateResultFolderPathAndFileName(ComparisonFileNameTemplate, classType.Name);
        plot.SavePng(fileName, Width, Height);
        return this;

        void AddErrorMarks(IGrouping<string, MethodExecutionResults> function, double[] dataY, Scatter scatter)
        {
            var dataDeltaMinus = function
               .Select(f =>
                   f.Numbers.CalculatePercentile(i => i.PureMethodTime, 50).TotalNanoseconds -
                   f.Numbers.CalculatePercentile(i => i.PureMethodTime, 30).TotalNanoseconds)
               .Select(f => f < 0 ? 0.0 : f / Data.BatchSize)
               .ToArray();

            var dataDeltaPlus = function
                .Select(f =>
                    f.Numbers.CalculatePercentile(i => i.PureMethodTime, 70).TotalNanoseconds -
                    f.Numbers.CalculatePercentile(i => i.PureMethodTime, 50).TotalNanoseconds)
                .Select(f => f < 0 ? 0.0 : f/Data.BatchSize)
                .ToArray();

            plot.Add.Plottable(new ErrorBar(
                xs: labelX,
                ys: dataY,
                xErrorsNegative: new double[dataY.Length],
                xErrorsPositive: new double[dataY.Length],
                yErrorsNegative: dataDeltaMinus,
                yErrorsPositive: dataDeltaPlus)
            {
                Color = scatter.Color
            });
        }
    }

    public IGraphExporter GraphSize(int width, int height)
    {
        Width = width;
        Height = height;
        return this;
    }

    public IGraphExporter ExportRawGraph(MethodExecutionResults rmExecutionResults,
        SortTimeDirection sortTimesDirection)
    {
        var className = rmExecutionResults.Method.ClassType.Name;
        var methodName = rmExecutionResults.Method.MethodInfo.Name;
        var parameter = rmExecutionResults.Method.Parameter;
        var dataX = rmExecutionResults.Numbers.Select((_, index) => (double)(index + 1)).ToArray();
        var dataY = rmExecutionResults.Numbers.Select(t => t.PureMethodTime.TotalNanoseconds).ToArray();
        dataY = sortTimesDirection switch
        {
            AscendingTimes => dataY.OrderBy(t => t).ToArray(),
            DescendingTimes => dataY.OrderByDescending(t => t).ToArray(),
            _ => dataY
        };

        var plot = new Plot();
        plot.XLabel("Run number");
        plot.YLabel("Time, μs");
        plot.Title($"Raw data. {className}.{methodName}({rmExecutionResults.Method.Parameter})");
        plot.Add.Scatter(dataX, dataY);
        plot.ShowLegend();
        var fileName =
            SubstituteFilenameTemplate(RawDataFileNameTemplate, className, methodName, parameter, sortTimesDirection);
        plot.SavePng(fileName, Width, Height);
        return this;
    }

    private string SubstituteFilenameTemplate(string template, string className, string methodName, object? parameter,
        SortTimeDirection sorted)
    {
        var fileName = template
            .Replace("{methodName}", methodName, StringComparison.OrdinalIgnoreCase)
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase)
            .Replace("{parameter}", parameter?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{sorted}", sorted.ToString(), StringComparison.OrdinalIgnoreCase);

        return CreateResultFolderPathAndFileName(fileName, className, "RawGraphs");
    }
}