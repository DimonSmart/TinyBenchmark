namespace DimonSmart.TinyBenchmark.Exporters;

public class ExporterBaseClass : ResultProcessor
{
    private bool _directoryCreated;

    public ExporterBaseClass(ITinyBenchmarkRunner tinyBenchmarkRunner, BenchmarkData data) :
        base(tinyBenchmarkRunner, data)
    {
        BeforeExport();
    }

    public string ResultsFolder { get; set; } = "TinyBenchmark";

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