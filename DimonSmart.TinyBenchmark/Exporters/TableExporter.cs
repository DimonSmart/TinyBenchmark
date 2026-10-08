using System.Globalization;
using System.Text;

namespace DimonSmart.TinyBenchmark.Exporters;

public class TableExporter : ExporterBaseClass, ITableExporter
{
    public TableExporter(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data, BenchmarkRunResult runResult) : base(tinyBenchmarkRunner, data, runResult) { }
    public string TableFileNameTemplate { get; set; } = "Table-{ClassName}.txt";
    public ITableExporter SaveAllTablesResults()
    {
        foreach (var group in CasesByClass())
        {
            var text = new StringBuilder("MethodSignature | CaseId | Parameter | MedianPerOperationNs | Status" + Environment.NewLine);
            foreach (var @case in group.OrderBy(item => item.Identity.MethodSignature).ThenBy(item => item.Identity.ParameterIdentity))
            {
                var value = @case.Statistics.MedianPerOperationNanoseconds?.ToString("R", CultureInfo.InvariantCulture) ?? "N/A";
                text.AppendLine($"{@case.Identity.MethodSignature} | {@case.CaseId} | {@case.Identity.ParameterDisplay} | {value} | {@case.ExecutionStatus}");
            }
            File.WriteAllText(CreateResultFolderPathAndFileName(TableFileNameTemplate, group.Key), text.ToString());
        }
        return this;
    }
}
