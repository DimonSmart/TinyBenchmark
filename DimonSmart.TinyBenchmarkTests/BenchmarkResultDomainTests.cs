using DimonSmart.TinyBenchmark;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class BenchmarkResultDomainTests
{
    [Fact]
    public void SampleAndPublishedCollectionsAreImmutableSnapshots()
    {
        var samples = new[] { Sample(0, 10, 1) };
        var benchmarkCase = Case(BenchmarkSampleRetentionMode.AllRaw, samples);
        var cases = new List<BenchmarkCaseResult> { benchmarkCase };
        var result = Run(cases);

        samples[0] = Sample(1, 20, 1);
        cases.Clear();

        Assert.Equal(10, benchmarkCase.Samples[0].Elapsed.Ticks);
        Assert.Single(result.Cases);
        Assert.Throws<NotSupportedException>(() => ((IList<BenchmarkSample>)benchmarkCase.Samples).Add(Sample(2, 1, 1)));
        Assert.Throws<NotSupportedException>(() => ((IList<BenchmarkCaseResult>)result.Cases).Clear());
    }

    [Fact]
    public void SummaryOnlyCalculatesStatisticsWithoutRetainingRawSamples()
    {
        var benchmarkCase = Case(BenchmarkSampleRetentionMode.SummaryOnly, new[] { Sample(0, 10, 1) });

        Assert.Empty(benchmarkCase.Samples);
        Assert.Equal(1, benchmarkCase.Statistics.SampleCount);
        Assert.Equal(1000, benchmarkCase.Statistics.MeanPerOperationNanoseconds);
    }

    [Fact]
    public void AllRawPreservesInputOrder()
    {
        var benchmarkCase = Case(BenchmarkSampleRetentionMode.AllRaw, new[] { Sample(3, 10, 1), Sample(1, 20, 1) });

        Assert.Equal(new long[] { 3, 1 }, benchmarkCase.Samples.Select(sample => sample.Sequence));
    }

    [Fact]
    public void StatisticsUseBatchWeightedMeanAndSampleNormalizedPercentiles()
    {
        var statistics = BenchmarkStatisticsCalculator.Calculate(new[] { Sample(0, 100, 10), Sample(1, 100, 1) });

        Assert.Equal(20000d / 11, statistics.MeanPerOperationNanoseconds!.Value, 8);
        Assert.Equal(5500, statistics.MedianPerOperationNanoseconds);
        Assert.Equal(1000, statistics.MinimumPerOperationNanoseconds);
        Assert.Equal(10000, statistics.MaximumPerOperationNanoseconds);
    }

    [Fact]
    public void EmptyAndSingleSampleStatisticsDoNotInventDispersion()
    {
        var empty = BenchmarkStatisticsCalculator.Calculate(Array.Empty<BenchmarkSample>());
        var single = BenchmarkStatisticsCalculator.Calculate(new[] { Sample(0, 25, 1) });

        Assert.Equal(0, empty.SampleCount);
        Assert.Null(empty.MeanPerOperationNanoseconds);
        Assert.Null(empty.StandardDeviationPerOperationNanoseconds);
        Assert.Equal(1, single.SampleCount);
        Assert.Equal(2500, single.MedianPerOperationNanoseconds);
        Assert.Null(single.StandardDeviationPerOperationNanoseconds);
    }

    private static BenchmarkSample Sample(long sequence, long ticks, long operations) =>
        new(sequence, TimeSpan.FromTicks(ticks), operations, "case", false);

    private static BenchmarkCaseResult Case(BenchmarkSampleRetentionMode retentionMode, IReadOnlyCollection<BenchmarkSample> samples) =>
        new("case", BenchmarkMeasurementStrategy.Fast, samples.Count, samples.Count, samples.Sum(sample => sample.OperationCount), 0,
            BenchmarkExecutionStatus.Completed,
            samples.Count == 0 ? BenchmarkStatisticalQualityStatus.NoData : BenchmarkStatisticalQualityStatus.SingleSample,
            retentionMode, samples, new BenchmarkRunDiagnostics(null, null, null));

    private static BenchmarkRunResult Run(IEnumerable<BenchmarkCaseResult> cases) =>
        new(new EffectiveMeasurementSettings(BenchmarkSampleRetentionMode.AllRaw, null, false), cases,
            BenchmarkExecutionStatus.Completed,
            new BenchmarkPhaseTiming(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero),
            new BenchmarkRunDiagnostics(null, null, null));
}
