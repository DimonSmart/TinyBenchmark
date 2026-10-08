using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DimonSmart.TinyBenchmark.Exporters;

public class CsvExporter : ExporterBaseClass, ICsvExporter
{
    public CsvExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data, BenchmarkRunResult runResult) : base(tinyBenchmarkRunner, data, runResult) { }
    public string CsvFileNameTemplate { get; set; } = "RAW-{ClassName}.csv";
    private int _limit = int.MaxValue;
    public ICsvExporter LimitResultLines(int limit) { if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit)); _limit = limit; return this; }
    public ICsvExporter SaveAllRawResults()
    {
        RequireCompleteRawSamples();
        foreach (var group in CasesByClass()) WriteClassResults(group.Key, group);
        return this;
    }
    private void WriteClassResults(string className, IEnumerable<BenchmarkCaseResult> cases)
    {
        var records = cases.SelectMany(@case => @case.Samples.Select(sample => new RawCsvRecord(sample.Sequence, @case.CaseId,
            @case.Identity.ClassName, @case.Identity.MethodSignature, @case.Identity.ParameterIdentity, @case.Identity.ParameterDisplay,
            sample.Elapsed.TotalNanoseconds, sample.OperationCount, sample.PerOperationNanoseconds, sample.IsColdStart)))
            .OrderBy(record => record.Sequence).ToArray();
        var fileName = CreateResultFolderPathAndFileName(CsvFileNameTemplate, className);
        using var writer = new StreamWriter(fileName);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteRecords(records.Take(_limit));
    }
    private sealed record RawCsvRecord(long Sequence, string CaseId, string ClassName, string MethodSignature,
        string ParameterIdentity, string ParameterDisplay, double ElapsedNs, long Operations, double PerOperationNs, bool IsColdStart);
}
