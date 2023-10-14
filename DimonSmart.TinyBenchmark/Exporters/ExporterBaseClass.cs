namespace DimonSmart.TinyBenchmark.Exporters;

public class ExporterBaseClass
{
    private bool _directoryCreated;
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