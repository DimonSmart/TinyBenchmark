using System.Collections;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DimonSmart.TinyBenchmark.Exporters;

public class CsvExporter : ResultProcessor, ICsvExporter
{
    public CsvExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
    }

    public ICsvExporter SaveRawResultsDataAsCsv()
    {
        var flattenedResults =
            Data.Results.SelectMany(result => result.Times,
                    (result, time) =>
                        new FlatMethodExecutionResult(
                            result.Method.ClassType.Name,
                            result.Method.MethodInfo.Name,
                            result.Method.Parameter,
                            time))
                .OrderBy(c => c.ClassName)
                .ThenBy(f => f.MethodName)
                .ThenBy(t => t.Time);
        using var writer = new StreamWriter("MethodExecutionResults.csv");
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.Context.RegisterClassMap<FlatMethodExecutionResultMap>();
        csv.WriteRecords((IEnumerable)flattenedResults);
        return this;
    }
}