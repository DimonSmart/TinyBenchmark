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
}