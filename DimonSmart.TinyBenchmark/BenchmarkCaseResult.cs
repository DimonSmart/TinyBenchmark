using System.Collections.ObjectModel;

namespace DimonSmart.TinyBenchmark;

/// <summary>An immutable result for one benchmark case.</summary>
public sealed class BenchmarkCaseResult
{
    public BenchmarkCaseResult(
        string caseId,
        BenchmarkMeasurementStrategy strategy,
        int requestedSampleCount,
        int completedSampleCount,
        long measuredInvocationCount,
        long preparationInvocationCount,
        BenchmarkExecutionStatus executionStatus,
        BenchmarkStatisticalQualityStatus statisticalQualityStatus,
        BenchmarkSampleRetentionMode retentionMode,
        IEnumerable<BenchmarkSample> samples,
        BenchmarkRunDiagnostics diagnostics,
        BenchmarkCaseIdentity? identity = null)
    {
        if (string.IsNullOrWhiteSpace(caseId)) throw new ArgumentException("A case identity is required.", nameof(caseId));
        if (requestedSampleCount < 0) throw new ArgumentOutOfRangeException(nameof(requestedSampleCount));
        if (completedSampleCount < 0 || completedSampleCount > requestedSampleCount) throw new ArgumentOutOfRangeException(nameof(completedSampleCount));
        if (measuredInvocationCount < 0) throw new ArgumentOutOfRangeException(nameof(measuredInvocationCount));
        if (preparationInvocationCount < 0) throw new ArgumentOutOfRangeException(nameof(preparationInvocationCount));
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var capturedSamples = samples.ToArray();
        if (capturedSamples.Length != completedSampleCount)
            throw new ArgumentException("Completed sample count must match the supplied samples.", nameof(completedSampleCount));
        if (capturedSamples.Any(sample => sample.CaseId != caseId))
            throw new ArgumentException("Every sample must belong to this case.", nameof(samples));

        CaseId = caseId;
        Identity = identity ?? new BenchmarkCaseIdentity("Unknown", caseId, caseId, "unknown", string.Empty);
        Strategy = strategy;
        RequestedSampleCount = requestedSampleCount;
        CompletedSampleCount = completedSampleCount;
        MeasuredInvocationCount = measuredInvocationCount;
        PreparationInvocationCount = preparationInvocationCount;
        ExecutionStatus = executionStatus;
        StatisticalQualityStatus = statisticalQualityStatus;
        RetentionMode = retentionMode;
        Statistics = BenchmarkStatisticsCalculator.Calculate(capturedSamples);
        AllocatedBytes = capturedSamples.Length != 0 && capturedSamples.All(sample => sample.AllocatedBytes.HasValue)
            ? capturedSamples.Sum(sample => sample.AllocatedBytes!.Value)
            : null;
        Diagnostics = diagnostics;
        Samples = retentionMode == BenchmarkSampleRetentionMode.AllRaw
            ? new ReadOnlyCollection<BenchmarkSample>(capturedSamples)
            : Array.Empty<BenchmarkSample>();
    }

    public string CaseId { get; }
    public BenchmarkCaseIdentity Identity { get; }
    public BenchmarkMeasurementStrategy Strategy { get; }
    public int RequestedSampleCount { get; }
    public int CompletedSampleCount { get; }
    public long MeasuredInvocationCount { get; }
    public long PreparationInvocationCount { get; }
    public BenchmarkExecutionStatus ExecutionStatus { get; }
    public BenchmarkStatisticalQualityStatus StatisticalQualityStatus { get; }
    public BenchmarkSampleRetentionMode RetentionMode { get; }
    public BenchmarkSampleStatistics Statistics { get; }
    /// <summary>Total current-thread allocation observed during useful samples, when measurement was available.</summary>
    public long? AllocatedBytes { get; }
    public BenchmarkRunDiagnostics Diagnostics { get; }
    public IReadOnlyList<BenchmarkSample> Samples { get; }
}
