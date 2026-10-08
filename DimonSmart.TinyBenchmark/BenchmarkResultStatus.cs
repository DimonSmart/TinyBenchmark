namespace DimonSmart.TinyBenchmark;

/// <summary>Describes how a benchmark case or run ended.</summary>
public enum BenchmarkExecutionStatus
{
    Completed,
    BudgetExhausted,
    RequiredExecutionOverrun,
    Skipped,
    Failed
}

/// <summary>Describes the statistical usefulness of the samples that were collected.</summary>
public enum BenchmarkStatisticalQualityStatus
{
    NoData,
    SingleSample,
    InsufficientSample,
    Measured,
    HighVariability
}

/// <summary>Controls whether a published result retains its raw samples.</summary>
public enum BenchmarkSampleRetentionMode
{
    SummaryOnly,
    AllRaw
}

/// <summary>Identifies the measurement strategy that produced a case.</summary>
public enum BenchmarkMeasurementStrategy
{
    Fast,
    Regular,
    LongRunning
}
