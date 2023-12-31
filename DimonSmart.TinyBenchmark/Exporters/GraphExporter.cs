﻿using DimonSmart.TinyBenchmark.Utils;
using ScottPlot;
using ScottPlot.Plottable;
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
        var classes = Data
            .Results.Select(r => r.Method.ClassType).Distinct();

        foreach (var cls in classes)
        {
            ExportAllFunctionsCompareGraph(cls, options);
        }

        return this;
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
        var plot = new Plot(Width, Height);
        plot.XLabel("Run number");
        plot.YLabel("Time, μs");
        plot.Title($"{classType.Name}");

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
            labelX = classRunParameters.Select((value, index) => value?.ToString() ?? "X").ToArray();
        }

        plot.XAxis.ManualTickPositions(dataX, labelX);

        foreach (var function in byFunction)
        {
            var dataY = function
                .Select(f => f.Times.CalculatePercentile(50).TotalNanoseconds)
                .ToArray();
            var scatter = plot.AddScatter(dataX, dataY, label: $"{function.Key}", lineWidth: 2);
            if (options == GraphExportOption.IncludeErrorMarks)
            {
                AddErrorMarks(function, dataY, scatter);
            }
        }

        plot.Legend();
        var fileName =
            SubstituteComparisionFilenameTemplate(ComparisionFileNameTemplate, classType.Name);
        plot.SaveFig(fileName);
        return this;

        void AddErrorMarks(IGrouping<string, MethodExecutionResults> function, double[] dataY, ScatterPlot scatter)
        {
            var dataDeltaMinus = function
                .Select(f =>
                    f.Times.CalculatePercentile(50).TotalNanoseconds -
                    f.Times.CalculatePercentile(20).TotalNanoseconds)
                .Select(f => f < 0 ? 0.0 : f)
                .ToArray();

            var dataDeltaPlus = function
                .Select(f =>
                    f.Times.CalculatePercentile(80).TotalNanoseconds -
                    f.Times.CalculatePercentile(50).TotalNanoseconds)
                .Select(f => f < 0 ? 0.0 : f)
                .ToArray();

            plot.AddErrorBars(
                dataX,
                dataY,
                xErrorsNegative: new double[dataY.Length],
                xErrorsPositive: new double[dataY.Length],
                yErrorsNegative: dataDeltaMinus,
                yErrorsPositive: dataDeltaPlus,
                color: scatter.Color
            );
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
        var dataX = rmExecutionResults.Times.Select((_, index) => (double)(index + 1)).ToArray();
        var dataY = rmExecutionResults.Times.Select(t => t.TotalNanoseconds).ToArray();
        dataY = sortTimesDirection switch
        {
            AscendingTimes => dataY.OrderBy(t => t).ToArray(),
            DescendingTimes => dataY.OrderByDescending(t => t).ToArray(),
            _ => dataY
        };

        var plot = new Plot(Width, Height);
        plot.XLabel("Run number");
        plot.YLabel("Time, μs");
        plot.Title($"Raw data. {className}.{methodName}({rmExecutionResults.Method.Parameter})");
        plot.AddScatter(dataX, dataY, label: "Raw timings");
        plot.Legend();
        var fileName =
            SubstituteFilenameTemplate(RawDataFileNameTemplate, className, methodName, parameter, sortTimesDirection);
        plot.SaveFig(fileName);
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
        return Path.Combine(ResultsFolder, fileName);
    }

    private string SubstituteComparisionFilenameTemplate(string template, string className)
    {
        var fileName = template
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }
}