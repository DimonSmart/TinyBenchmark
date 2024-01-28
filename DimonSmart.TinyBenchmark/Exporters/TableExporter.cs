using System.Globalization;
using ConsoleTableExt;
using CsvHelper;
using CsvHelper.Configuration;
using DimonSmart.TinyBenchmark.Utils;
using static DimonSmart.TinyBenchmark.Exporters.IGraphExporter;

namespace DimonSmart.TinyBenchmark.Exporters;

public class TableExporter : ExporterBaseClass, ITableExporter
{
    public TableExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
    }
    public string TableFileNameTemplate { get; set; } = "Table-{ClassName}.txt";

    private static string SubstituteFilenameTemplate(string template, string className)
    {
        var fileName = template
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }

    public ITableExporter SaveAllTablesResults()
    {
        DoExportByClass();
        return this;
    }

    protected override void ExportOneClass(Type classType, object? options)
    {
        var classFunctions = Data
           .Results
           .Where(c => c.Method.ClassType == classType)
           .ToList();
        var functions = classFunctions
            .GroupBy(g => g.Method.MethodInfo.Name, v => v).ToList();
        var classRunParameters = functions.First()
            .Select(f => f.Method.Parameter ?? "null")
            .OrderBy(p => p)
            .ToList();

        var columns = new List<object> { "Function" };
        columns.AddRange(classRunParameters);

        var rows = new List<List<object>>();
        foreach (var function in functions)
        {
            var oneRow = new List<object>() { function.Key };
            var results = function
                .Select(f => f.Numbers.Select(n => n.PureMethodTime).CalculatePercentile(50).TotalNanoseconds)
                // Add formatting here
                .Select(f => (object)f);
            oneRow.AddRange(results);
            rows.Add(oneRow);
        }

        var table = ConsoleTableBuilder.From(() =>
        {
            return new ConsoleTableBaseData
            {
                Rows = rows,
                Column = columns
            };
        })
        .WithFormat(ConsoleTableBuilderFormat.Default)
        .Export();

        var fileName = SubstituteClassNameFilenameTemplate(TableFileNameTemplate, classType.Name);
        File.WriteAllText(fileName, table.ToString());
    }

    public override void WriteClassResults(string className, IGrouping<string, FlatMethodExecutionResult> groupedResult)
    {
        var groupedByMethod = groupedResult
            .OrderBy(f => f.MethodName)
            .ThenBy(t => t.Time)
            .GroupBy(r => r.MethodName);

        foreach (var methodGroup in groupedByMethod)
        {
            var methodName = methodGroup.Key;
            var parameters = methodGroup.GroupBy(g => g.Parameter).OrderBy(g => g.Key);



        }

        var fileName = SubstituteFilenameTemplate(TableFileNameTemplate, className);

    }
}