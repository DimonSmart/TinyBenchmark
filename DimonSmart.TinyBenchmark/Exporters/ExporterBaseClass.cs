namespace DimonSmart.TinyBenchmark.Exporters;

public class ExporterBaseClass : ResultProcessor
{
    public const string ResultsFolder = "TinyBenchmark";
    private bool _directoryCreated;

    public ExporterBaseClass(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
        BeforeExport();
    }

    protected void BeforeExport()
    {
        if (_directoryCreated)
        {
            return;
        }

        Directory.CreateDirectory(ResultsFolder);
        _directoryCreated = true;
    }


    protected void DoExportByClass(object? options = null)
    {
        var classes = Data
            .Results.Select(r => r.Method.ClassType).Distinct();

        foreach (var cls in classes)
        {
            ExportOneClass(cls, options);
        }
    }

    protected virtual void ExportOneClass(Type classType, object? options)
    {
        throw new NotImplementedException();
    }

    protected void DoRawExportByClass()
    {
        var groupedResults =
               Data.Results.SelectMany(result => result.Numbers,
                       (result, numbers) =>
                           new FlatMethodExecutionResult(
                               result.Method.ClassType.Name,
                               result.Method.MethodInfo.Name,
                               result.Method.Parameter,
                               numbers.MethodTime))
                   .GroupBy(g => g.ClassName);

        foreach (var groupedResult in groupedResults)
        {
            WriteClassResults(groupedResult.Key, groupedResult);
        }
    }
    public virtual void WriteClassResults(string key, IGrouping<string, FlatMethodExecutionResult> groupedResult)
    {
        throw new NotImplementedException();
    }

    protected string SubstituteClassNameFilenameTemplate(string template, string className)
    {
        var fileName = template
            .Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        return Path.Combine(ResultsFolder, fileName);
    }
}