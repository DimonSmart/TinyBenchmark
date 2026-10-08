using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Exporters;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class ExportResultTests
{
    [Fact]
    public void RawExportRejectsSummaryOnlyAndMixedResults()
    {
        var result = Run(BenchmarkSampleRetentionMode.SummaryOnly);
        var exporter = new CsvExporter(TinyBenchmarkRunner.Create(), new BenchmarkData(), result);

        var exception = Assert.Throws<InvalidOperationException>(() => exporter.SaveAllRawResults());

        Assert.Contains("complete RAW samples", exception.Message);
    }

    [Fact]
    public void SummaryOnlyTableUsesCanonicalStatistics()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"tinybenchmark-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directory);
            var data = new BenchmarkData { ResultSubfolders = false };
            new TableExporter(TinyBenchmarkRunner.Create(), data, Run(BenchmarkSampleRetentionMode.SummaryOnly)).SaveAllTablesResults();
            var text = File.ReadAllText(Path.Combine(directory, "TinyBenchmark", "Table-Example.txt"));
            Assert.Contains("MedianPerOperationNs", text);
            Assert.Contains("1000", text);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void RawCsvUsesSequenceOrderAndExplicitNanosecondColumns()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"tinybenchmark-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directory);
            var data = new BenchmarkData { ResultSubfolders = false };
            var identity = new BenchmarkCaseIdentity("Example", "Measure", "Measure()", "none", "null");
            var samples = new[] { new BenchmarkSample(9, TimeSpan.FromTicks(20), 2, "example", false), new BenchmarkSample(2, TimeSpan.FromTicks(10), 1, "example", true) };
            var @case = new BenchmarkCaseResult("example", BenchmarkMeasurementStrategy.Fast, 2, 2, 3, 0, BenchmarkExecutionStatus.Completed,
                BenchmarkStatisticalQualityStatus.Measured, BenchmarkSampleRetentionMode.AllRaw, samples, new BenchmarkRunDiagnostics(null, null, null), identity);
            var result = new BenchmarkRunResult(new EffectiveMeasurementSettings(BenchmarkSampleRetentionMode.AllRaw, null, false), new[] { @case }, BenchmarkExecutionStatus.Completed,
                new BenchmarkPhaseTiming(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero), new BenchmarkRunDiagnostics(null, null, null));
            new CsvExporter(TinyBenchmarkRunner.Create(), data, result).LimitResultLines(1).SaveAllRawResults();
            var lines = File.ReadAllLines(Path.Combine(directory, "TinyBenchmark", "RAW-Example.csv"));
            Assert.Contains("ElapsedNs", lines[0]);
            Assert.Contains("PerOperationNs", lines[0]);
            Assert.StartsWith("2,", lines[1]);
            Assert.Equal(2, lines.Length);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(directory, true);
        }
    }

    private static BenchmarkRunResult Run(BenchmarkSampleRetentionMode retention)
    {
        var identity = new BenchmarkCaseIdentity("Example", "Measure", "Measure(System.Int32)", "System.Int32:7", "7");
        var samples = new[] { new BenchmarkSample(4, TimeSpan.FromTicks(10), 1, "example/7", false) };
        var @case = new BenchmarkCaseResult("example/7", BenchmarkMeasurementStrategy.Regular, 1, 1, 1, 0,
            BenchmarkExecutionStatus.Completed, BenchmarkStatisticalQualityStatus.SingleSample, retention, samples,
            new BenchmarkRunDiagnostics(null, null, null), identity);
        return new BenchmarkRunResult(new EffectiveMeasurementSettings(retention, null, false), new[] { @case }, BenchmarkExecutionStatus.Completed,
            new BenchmarkPhaseTiming(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero), new BenchmarkRunDiagnostics(null, null, null));
    }
}
