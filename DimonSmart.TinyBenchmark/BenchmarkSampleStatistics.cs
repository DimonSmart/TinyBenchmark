namespace DimonSmart.TinyBenchmark;

/// <summary>Statistics derived from useful samples, expressed in nanoseconds per operation.</summary>
public sealed record BenchmarkSampleStatistics(
    int SampleCount,
    double? MeanPerOperationNanoseconds,
    double? MedianPerOperationNanoseconds,
    double? Percentile90PerOperationNanoseconds,
    double? Percentile95PerOperationNanoseconds,
    double? MinimumPerOperationNanoseconds,
    double? MaximumPerOperationNanoseconds,
    double? StandardDeviationPerOperationNanoseconds);
