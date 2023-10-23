using System.Collections;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DimonSmart.TinyBenchmark.Exporters;

public class CsvExporter : ExporterBaseClass, ICsvExporter
{
    public CsvExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
    }

    public string CsvFileNameTemplate { get; set; } = "RAW-{ClassName}.csv";

    public ICsvExporter SaveRawResults()
    {
        var groupedResults =
            Data.Results.SelectMany(result => result.Times,
                    (result, time) =>
                        new FlatMethodExecutionResult(
                            result.Method.ClassType.Name,
                            result.Method.MethodInfo.Name,
                            result.Method.Parameter,
                            time))
                .GroupBy(g => g.ClassName);

        foreach (var groupedResult in groupedResults)
        {
            WriteClassResults(groupedResult.Key, groupedResult);
        }

        return this;
    }

    private void WriteClassResults(string className, IEnumerable<FlatMethodExecutionResult> groupedResult)
    {
        var data = groupedResult
            .OrderBy(f => f.MethodName)
            .ThenBy(t => t.Time);
        var fileName = SubstituteFilenameTemplate(CsvFileNameTemplate, className);
        using var stream = new FileStream(fileName, FileMode.Create);
        using var bufferedStream = new BufferedStream(stream, 1024 * 1024);
        using var writer = new StreamWriter(bufferedStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.Context.RegisterClassMap<FlatMethodExecutionResultMap>();
        csv.WriteRecords((IEnumerable)data);
    }

    private string SubstituteFilenameTemplate(string template, string className)
    {
        var fileName = template
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }
}