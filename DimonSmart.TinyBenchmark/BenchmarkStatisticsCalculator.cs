namespace DimonSmart.TinyBenchmark;

/// <summary>Calculates all benchmark sample statistics in one place.</summary>
public static class BenchmarkStatisticsCalculator
{
    public static BenchmarkSampleStatistics Calculate(IEnumerable<BenchmarkSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var materialized = samples.ToArray();
        if (materialized.Length == 0)
            return new BenchmarkSampleStatistics(0, null, null, null, null, null, null, null);

        var totalOperations = materialized.Sum(sample => sample.OperationCount);
        var totalElapsedTicks = materialized.Aggregate(0.0, (total, sample) => total + sample.Elapsed.Ticks);
        var normalized = materialized.Select(sample => sample.PerOperationNanoseconds).OrderBy(value => value).ToArray();
        var median = Percentile(normalized, 50);

        if (normalized.Length == 1)
        {
            var value = normalized[0];
            return new BenchmarkSampleStatistics(1, totalElapsedTicks * 100 / totalOperations, value, value, value, value, value, null);
        }

        var mean = totalElapsedTicks * 100 / totalOperations;
        var normalizedMean = normalized.Average();
        var variance = normalized.Average(value => Math.Pow(value - normalizedMean, 2));
        return new BenchmarkSampleStatistics(
            normalized.Length,
            mean,
            median,
            Percentile(normalized, 90),
            Percentile(normalized, 95),
            normalized[0],
            normalized[^1],
            Math.Sqrt(variance));
    }

    public static double? Percentile(IEnumerable<BenchmarkSample> samples, double percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Percentile(samples.Select(sample => sample.PerOperationNanoseconds).OrderBy(value => value).ToArray(), percentile);
    }

    private static double? Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (percentile is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentile));
        if (sortedValues.Count == 0)
            return null;

        var index = (sortedValues.Count - 1) * percentile / 100;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        return lower == upper
            ? sortedValues[lower]
            : sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (index - lower);
    }
}
