namespace DimonSmart.TinyBenchmark.Exporters;

public class ExporterBaseClass : ResultProcessor
{
    public const string ResultsFolder = "TinyBenchmark";
    private readonly HashSet<string> _createdDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public ExporterBaseClass(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
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
                               numbers.PureMethodTime))
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

    protected string CreateResultFolderPathAndFileName(string template, string className, string? subSubFolder = null)
    {
        var fileName = template.Replace("{className}", className, StringComparison.OrdinalIgnoreCase);
        var resultFolder = Data.ResultSubfolders ? Path.Combine(ResultsFolder, className) : ResultsFolder;
        if (!string.IsNullOrWhiteSpace(subSubFolder))
        {
            resultFolder = Path.Combine(resultFolder, subSubFolder);
        }

        if (!_createdDirectories.Contains(resultFolder))
        {
            Directory.CreateDirectory(resultFolder);
            _createdDirectories.Add(resultFolder);
        }

        return Path.Combine(resultFolder, fileName);
    }
}