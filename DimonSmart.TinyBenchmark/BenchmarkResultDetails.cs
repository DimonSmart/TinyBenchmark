namespace DimonSmart.TinyBenchmark;

/// <summary>The phases included in a benchmark run's monotonic budget.</summary>
public sealed record BenchmarkPhaseTiming(
    TimeSpan Preparation,
    TimeSpan Warmup,
    TimeSpan Calibration,
    TimeSpan Measurement)
{
    public TimeSpan Total => Preparation + Warmup + Calibration + Measurement;
}

/// <summary>Explains an incomplete run or a permitted budget overrun.</summary>
public sealed record BenchmarkRunDiagnostics(
    string? IncompleteReason,
    TimeSpan? Budget,
    TimeSpan? BudgetOverrun)
{
    public bool IsIncomplete => !string.IsNullOrWhiteSpace(IncompleteReason);
    public bool HasBudgetOverrun => BudgetOverrun.GetValueOrDefault() > TimeSpan.Zero;
    /// <summary>Explains a permitted deadline overrun without marking a completed quota as incomplete.</summary>
    public string? OverrunReason { get; init; }
}

/// <summary>A snapshot of measurement options that were actually applied to a run.</summary>
public sealed record EffectiveMeasurementSettings(
    BenchmarkSampleRetentionMode RetentionMode,
    TimeSpan? RunBudget,
    bool AllocationMeasurementRequested);
