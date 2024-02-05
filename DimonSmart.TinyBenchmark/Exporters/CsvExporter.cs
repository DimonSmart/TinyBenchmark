using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DimonSmart.TinyBenchmark.Utils;

namespace DimonSmart.TinyBenchmark.Exporters;

public class CsvExporter : ExporterBaseClass, ICsvExporter
{
    public CsvExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
    }
    public string CsvFileNameTemplate { get; set; } = "RAW-{ClassName}.csv";
    private int _limit = int.MaxValue;

    public ICsvExporter LimitResultLines(int limit)
    {
        _limit = limit;
        return this;
    }

    public ICsvExporter SaveAllRawResults()
    {
        DoRawExportByClass();
        return this;
    }
    public override void WriteClassResults(string className, IGrouping<string, FlatMethodExecutionResult> groupedResult)
    {
        var groupedByMethod = groupedResult
            .OrderBy(f => f.MethodName)
            .ThenBy(t => t.Time)
            .GroupBy(r => r.MethodName);

        var limitedData = new List<FlatMethodExecutionResult>();

        foreach (var methodGroup in groupedByMethod)
        {
            limitedData.AddRange(methodGroup.ToList().LimitProportionally(_limit));
        }

        var fileName = CreateResultFolderPathAndFileName(CsvFileNameTemplate, className);
        using var stream = new FileStream(fileName, FileMode.Create);
        using var bufferedStream = new BufferedStream(stream, 1024 * 1024);
        using var writer = new StreamWriter(bufferedStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.Context.RegisterClassMap<FlatMethodExecutionResultMap>();
        csv.WriteRecords(limitedData);
    }
}