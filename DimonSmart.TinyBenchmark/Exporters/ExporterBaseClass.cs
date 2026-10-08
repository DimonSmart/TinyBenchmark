namespace DimonSmart.TinyBenchmark.Exporters;

public class ExporterBaseClass : ResultProcessor
{
    public const string ResultsFolder = "TinyBenchmark";
    private readonly HashSet<string> _createdDirectories = new(StringComparer.OrdinalIgnoreCase);
    public ExporterBaseClass(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data, BenchmarkRunResult runResult) : base(tinyBenchmarkRunner, data, runResult) { }
    protected IEnumerable<IGrouping<string, BenchmarkCaseResult>> CasesByClass() => RunResult.Cases.GroupBy(@case => @case.Identity.ClassName);
    protected static string DisplayClassName(string className) => className.Split('.').Last();
    protected void RequireCompleteRawSamples()
    {
        if (RunResult.Cases.Any(@case => @case.RetentionMode != BenchmarkSampleRetentionMode.AllRaw || @case.Samples.Count != @case.CompletedSampleCount))
            throw new InvalidOperationException("RAW export requires complete RAW samples for every case in the run. This run contains summary-only or mixed retention results.");
    }
    protected string CreateResultFolderPathAndFileName(string template, string className, string? subSubFolder = null)
    {
        var displayName = DisplayClassName(className);
        var fileName = template.Replace("{className}", displayName, StringComparison.OrdinalIgnoreCase);
        var resultFolder = Data.ResultSubfolders ? Path.Combine(ResultsFolder, displayName) : ResultsFolder;
        if (!string.IsNullOrWhiteSpace(subSubFolder)) resultFolder = Path.Combine(resultFolder, subSubFolder);
        if (_createdDirectories.Add(resultFolder)) Directory.CreateDirectory(resultFolder);
        return Path.Combine(resultFolder, fileName);
    }
}
