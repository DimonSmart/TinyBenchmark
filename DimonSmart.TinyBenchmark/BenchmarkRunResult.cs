using System.Collections.ObjectModel;

namespace DimonSmart.TinyBenchmark;

/// <summary>An immutable, independent snapshot published by a benchmark run.</summary>
public sealed class BenchmarkRunResult
{
    public BenchmarkRunResult(
        EffectiveMeasurementSettings settings,
        IEnumerable<BenchmarkCaseResult> cases,
        BenchmarkExecutionStatus executionStatus,
        BenchmarkPhaseTiming phaseTiming,
        BenchmarkRunDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(phaseTiming);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Settings = settings;
        Cases = new ReadOnlyCollection<BenchmarkCaseResult>(cases.ToArray());
        ExecutionStatus = executionStatus;
        PhaseTiming = phaseTiming;
        Diagnostics = diagnostics;
    }

    public EffectiveMeasurementSettings Settings { get; }
    public IReadOnlyList<BenchmarkCaseResult> Cases { get; }
    public BenchmarkExecutionStatus ExecutionStatus { get; }
    public BenchmarkPhaseTiming PhaseTiming { get; }
    public BenchmarkRunDiagnostics Diagnostics { get; }
    public int RequestedSampleCount => Cases.Sum(@case => @case.RequestedSampleCount);
    public int CompletedSampleCount => Cases.Sum(@case => @case.CompletedSampleCount);
    public long MeasuredInvocationCount => Cases.Sum(@case => @case.MeasuredInvocationCount);
    public long PreparationInvocationCount => Cases.Sum(@case => @case.PreparationInvocationCount);
}
