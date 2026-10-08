using ScottPlot;
using ScottPlot.TickGenerators;
using static DimonSmart.TinyBenchmark.SortTimeDirection;
using static DimonSmart.TinyBenchmark.Exporters.IGraphExporter;

namespace DimonSmart.TinyBenchmark.Exporters;

public class GraphExporter : ExporterBaseClass, IGraphExporter
{
    public GraphExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data, BenchmarkRunResult runResult) : base(tinyBenchmarkRunner, data, runResult) { }
    public string ComparisonFileNameTemplate { get; set; } = "Compare-{ClassName}.png";
    public int Width { get; private set; } = 800;
    public int Height { get; private set; } = 600;
    public string RawDataFileNameTemplate { get; set; } = "Raw-{ClassName}-{MethodName}-{Parameter}-{Sorted}.png";
    IGraphExporter IGraphExporter.GraphSize(int width, int height) => GraphSize(width, height);
    public IGraphExporter SetRawDataFileNameTemplate(string fileNameTemplate) { RawDataFileNameTemplate = fileNameTemplate; return this; }
    public IGraphExporter GraphSize(int width, int height) { Width = width; Height = height; return this; }
    public IGraphExporter ExportAllRawGraph(SortTimeDirection sortTimesDirection)
    {
        RequireCompleteRawSamples();
        foreach (var @case in RunResult.Cases) ExportRawGraph(@case, sortTimesDirection);
        return this;
    }
    public IGraphExporter ExportRawGraph(string className, string methodName, object? parameter, SortTimeDirection sortTimesDirection = UnsortedTimes)
    {
        RequireCompleteRawSamples();
        var parameterDisplay = Convert.ToString(parameter, System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var candidates = RunResult.Cases.Where(@case =>
            (string.Equals(@case.Identity.ClassName, className, StringComparison.Ordinal) || string.Equals(DisplayClassName(@case.Identity.ClassName), className, StringComparison.Ordinal)) &&
            @case.Identity.MethodName == methodName && @case.Identity.ParameterDisplay == parameterDisplay).ToArray();
        if (candidates.Length != 1) throw new ArgumentException(candidates.Length == 0 ? "The requested benchmark case was not found." : "The requested benchmark is ambiguous; use a distinct parameter or overload.");
        ExportRawGraph(candidates[0], sortTimesDirection);
        return this;
    }
    public IGraphExporter ExportAllFunctionsCompareGraph(GraphExportOption options)
    {
        foreach (var group in CasesByClass()) ExportComparison(group.Key, group, options);
        return this;
    }
    public IGraphExporter ExportAllFunctionsCompareGraph(Type classType, GraphExportOption options)
    {
        var className = classType.FullName ?? classType.Name;
        var cases = RunResult.Cases.Where(@case => @case.Identity.ClassName == className).ToArray();
        if (cases.Length == 0) throw new ArgumentException("Class with name specified not found in results set", nameof(classType));
        ExportComparison(className, cases, options);
        return this;
    }
    private void ExportComparison(string className, IEnumerable<BenchmarkCaseResult> cases, GraphExportOption options)
    {
        var materialized = cases.OrderBy(@case => @case.Identity.MethodSignature).ThenBy(@case => @case.Identity.ParameterIdentity).ToArray();
        var xs = Enumerable.Range(0, materialized.Length).Select(value => (double)value).ToArray();
        var ys = materialized.Select(@case => @case.Statistics.MedianPerOperationNanoseconds ?? double.NaN).ToArray();
        var labels = xs.Select((x, i) => new KeyValuePair<double, string>(x, $"{materialized[i].Identity.MethodSignature} [{materialized[i].Identity.ParameterDisplay}]")).ToDictionary(item => item.Key, item => item.Value);
        var plot = new Plot();
        plot.XLabel("Benchmark case");
        plot.YLabel("Time per operation (ns)");
        plot.Title(DisplayClassName(className));
        plot.Axes.Bottom.TickGenerator = new NumericAutomatic { LabelFormatter = value => labels.GetValueOrDefault(value, string.Empty) };
        var scatter = plot.Add.Scatter(xs, ys); scatter.LegendText = "Median per operation (ns)";
        plot.ShowLegend();
        plot.SavePng(CreateResultFolderPathAndFileName(ComparisonFileNameTemplate, className), Width, Height);
    }
    private void ExportRawGraph(BenchmarkCaseResult @case, SortTimeDirection direction)
    {
        var samples = @case.Samples.Select(sample => new { sample.Sequence, Value = sample.PerOperationNanoseconds }).ToArray();
        samples = direction switch { AscendingTimes => samples.OrderBy(item => item.Value).ToArray(), DescendingTimes => samples.OrderByDescending(item => item.Value).ToArray(), _ => samples };
        var plot = new Plot();
        plot.XLabel("Sample sequence"); plot.YLabel("Time per operation (ns)");
        plot.Title($"RAW {@case.Identity.MethodSignature} [{@case.Identity.ParameterDisplay}]");
        plot.Add.Scatter(samples.Select(item => (double)item.Sequence).ToArray(), samples.Select(item => item.Value).ToArray());
        var fileName = RawDataFileNameTemplate.Replace("{methodName}", @case.Identity.MethodName, StringComparison.OrdinalIgnoreCase)
            .Replace("{className}", DisplayClassName(@case.Identity.ClassName), StringComparison.OrdinalIgnoreCase)
            .Replace("{parameter}", @case.Identity.ParameterDisplay, StringComparison.OrdinalIgnoreCase)
            .Replace("{sorted}", direction.ToString(), StringComparison.OrdinalIgnoreCase);
        plot.SavePng(CreateResultFolderPathAndFileName(fileName, @case.Identity.ClassName, "RawGraphs"), Width, Height);
    }
}
