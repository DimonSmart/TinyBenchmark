using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class ModernExecutionTests
{
    [Fact]
    public void FastUsesRequestedBatchesAndActualOperationCount()
    {
        FastBenchmark.Calls = 0;
        var result = TinyBenchmarkRunner.Create().Run(typeof(FastBenchmark)).RunResult.Cases.Single();

        Assert.Equal(12, FastBenchmark.Calls);
        Assert.Equal(3, result.CompletedSampleCount);
        Assert.Equal(12, result.MeasuredInvocationCount);
        Assert.All(result.Samples, sample => Assert.Equal(4, sample.OperationCount));
    }

    [Fact]
    public void RegularWarmupIsNotUsefulSample()
    {
        RegularBenchmark.Calls = 0;
        var result = TinyBenchmarkRunner.Create().WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw).Run(typeof(RegularBenchmark)).RunResult.Cases.Single();

        Assert.Equal(5, RegularBenchmark.Calls);
        Assert.Equal(2, result.CompletedSampleCount);
        Assert.Equal(3, result.PreparationInvocationCount);
        Assert.Equal(2, result.MeasuredInvocationCount);
    }

    [Fact]
    public void RegularUsesSmallDefaultWarmupAndExplicitZeroDisablesIt()
    {
        DefaultRegularBenchmark.Calls = 0;
        DisabledRegularWarmupBenchmark.Calls = 0;

        var result = TinyBenchmarkRunner.Create().Run(typeof(DefaultRegularBenchmark), typeof(DisabledRegularWarmupBenchmark)).RunResult;

        Assert.Equal(3, DefaultRegularBenchmark.Calls);
        Assert.Equal(1, result.Cases.Single(@case => @case.CaseId.Contains(nameof(DefaultRegularBenchmark))).PreparationInvocationCount);
        Assert.Equal(2, DisabledRegularWarmupBenchmark.Calls);
        Assert.Equal(0, result.Cases.Single(@case => @case.CaseId.Contains(nameof(DisabledRegularWarmupBenchmark))).PreparationInvocationCount);
    }

    [Fact]
    public void AdaptiveFastCalibrationIsPreparationAndUsesTheSharedBudget()
    {
        AdaptiveFastBenchmark.Calls = 0;
        var time = new ControlledTimeSource();
        var runner = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, null, TimeSpan.FromMilliseconds(1)))
            .WithMaxRunExecutionTime(TimeSpan.FromMilliseconds(1), 0)
            .WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw);

        var result = runner.Run(typeof(AdaptiveFastBenchmark)).RunResult;
        var benchmark = result.Cases.Single();

        Assert.Equal(1, AdaptiveFastBenchmark.Calls);
        Assert.Equal(1, benchmark.PreparationInvocationCount);
        Assert.Equal(0, benchmark.CompletedSampleCount);
        Assert.Empty(benchmark.Samples);
        Assert.Equal(TimeSpan.FromMilliseconds(1), result.PhaseTiming.Calibration);
        Assert.Equal(BenchmarkExecutionStatus.BudgetExhausted, benchmark.ExecutionStatus);
    }

    [Fact]
    public void AdaptiveFastCalibrationSelectsAUsefulBatchWithoutChangingSampleQuota()
    {
        AdaptiveFastBenchmark.Calls = 0;
        var time = new ControlledTimeSource();
        var result = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, null, TimeSpan.FromMilliseconds(0.5)))
            .WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw)
            .Run(typeof(AdaptiveFastBenchmark)).RunResult.Cases.Single();

        Assert.Equal(3, result.PreparationInvocationCount);
        Assert.Equal(2, result.CompletedSampleCount);
        Assert.All(result.Samples, sample => Assert.Equal(2, sample.OperationCount));
        Assert.Equal(7, AdaptiveFastBenchmark.Calls);
    }

    [Fact]
    public void MemoryBenchmarkingCapturesAllocationOnUsefulCallsWithoutExtraInvocations()
    {
        AllocationBenchmark.Calls = 0;

        var result = TinyBenchmarkRunner.Create().WithMemoryBenchmarking().WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw)
            .Run(typeof(AllocationBenchmark)).RunResult.Cases.Single();

        Assert.Equal(2, AllocationBenchmark.Calls);
        Assert.All(result.Samples, sample => Assert.NotNull(sample.AllocatedBytes));
        Assert.NotNull(result.AllocatedBytes);
    }

    [Fact]
    public void FirstLongRunningUsefulSampleIsCold()
    {
        var result = TinyBenchmarkRunner.Create().WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw)
            .Run(typeof(LongBenchmark)).RunResult.Cases.Single();

        Assert.True(result.Samples[0].IsColdStart);
        Assert.All(result.Samples.Skip(1), sample => Assert.False(sample.IsColdStart));
    }

    [Fact]
    public void LongRunningHasNoExtraCallsWhenMemoryIsEnabled()
    {
        LongBenchmark.Calls = 0;
        var result = TinyBenchmarkRunner.Create().WithMemoryBenchmarking().Run(typeof(LongBenchmark)).RunResult.Cases.Single();

        Assert.Equal(3, LongBenchmark.Calls);
        Assert.Equal(3, result.CompletedSampleCount);
        Assert.Equal(0, result.PreparationInvocationCount);
    }

    [Fact]
    public void ModernDefaultsToSummaryWhileLegacyKeepsRawUntilExplicitlyChanged()
    {
        var defaultResult = TinyBenchmarkRunner.Create().WithMinFunctionExecutionCount(1).WithMaxFunctionExecutionCount(1).Run(typeof(MixedBenchmark)).RunResult;
        Assert.Empty(defaultResult.Cases.Single(@case => @case.CaseId.EndsWith(".Modern()")).Samples);
        Assert.NotEmpty(defaultResult.Cases.Single(@case => @case.CaseId.EndsWith(".Legacy()")).Samples);

        var explicitResult = TinyBenchmarkRunner.Create().WithMinFunctionExecutionCount(1).WithMaxFunctionExecutionCount(1).WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw).Run(typeof(MixedBenchmark)).RunResult;
        Assert.All(explicitResult.Cases, @case => Assert.NotEmpty(@case.Samples));
    }

    [Theory]
    [InlineData(typeof(MaximumFastBenchmark))]
    [InlineData(typeof(MaximumRegularBenchmark))]
    [InlineData(typeof(MaximumLongRunningBenchmark))]
    public void ExplicitLegacyMaximumRejectsLargerModernQuotaBeforeAnyBenchmarkInvocation(Type benchmarkType)
    {
        MaximumQuotaBenchmark.Calls = 0;

        var exception = Assert.Throws<InvalidOperationException>(() => TinyBenchmarkRunner.Create()
            .WithMaxFunctionExecutionCount(2)
            .Run(benchmarkType));

        Assert.Contains("explicitly configured legacy maximum", exception.Message);
        Assert.Equal(0, MaximumQuotaBenchmark.Calls);
    }

    [Fact]
    public void LegacyDefaultMaximumDoesNotConstrainModernQuota()
    {
        MaximumQuotaBenchmark.Calls = 0;

        var result = TinyBenchmarkRunner.Create()
            .WithMaxRunExecutionTime(TimeSpan.Zero, 0)
            .Run(typeof(DefaultMaximumFastBenchmark)).RunResult;

        Assert.Equal(10_001, result.Cases.Single().RequestedSampleCount);
        Assert.Equal(0, MaximumQuotaBenchmark.Calls);
    }

    [Fact]
    public void PublishedResultDoesNotChangeAfterRunnerIsReconfiguredAndRunAgain()
    {
        var runner = TinyBenchmarkRunner.Create();
        var first = runner.Run(typeof(FastBenchmark)).RunResult;
        runner.WithRawSampleRetention(BenchmarkSampleRetentionMode.AllRaw).Run(typeof(FastBenchmark));

        Assert.Equal(BenchmarkSampleRetentionMode.SummaryOnly, first.Settings.RetentionMode);
        Assert.Empty(first.Cases.Single().Samples);
    }

    [Fact]
    public void SchedulerProvidesInitialCoverageAndInterleavesOptionalCases()
    {
        var log = new List<string>();
        var time = new ControlledTimeSource();
        var runner = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, log, TimeSpan.FromMilliseconds(1)));

        runner.Run(typeof(InterleavedFastBenchmark), typeof(InterleavedRegularBenchmark));

        Assert.Equal(new[] { "fast", "regular", "fast", "regular" }, log);
    }

    [Fact]
    public void SchedulerStopsOptionalSamplesAtTheSharedBudgetDeadline()
    {
        var time = new ControlledTimeSource();
        var runner = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, null, TimeSpan.FromMilliseconds(2)))
            .WithMaxRunExecutionTime(TimeSpan.FromMilliseconds(5), 0);

        var result = runner.Run(typeof(BudgetFastBenchmark), typeof(BudgetRegularBenchmark)).RunResult;

        Assert.Equal(2, result.Cases.Single(@case => @case.Strategy == BenchmarkMeasurementStrategy.Fast).CompletedSampleCount);
        Assert.Equal(1, result.Cases.Single(@case => @case.Strategy == BenchmarkMeasurementStrategy.Regular).CompletedSampleCount);
        Assert.All(result.Cases, @case => Assert.Equal(BenchmarkExecutionStatus.BudgetExhausted, @case.ExecutionStatus));
        Assert.Equal(TimeSpan.FromMilliseconds(6), result.PhaseTiming.Measurement);
        Assert.Equal(BenchmarkExecutionStatus.BudgetExhausted, result.ExecutionStatus);
    }

    [Fact]
    public void LongRunningQuotaHasPriorityAndReportsPermittedOverrun()
    {
        var time = new ControlledTimeSource();
        var runner = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, null, TimeSpan.FromMilliseconds(3)))
            .WithMaxRunExecutionTime(TimeSpan.FromMilliseconds(1), 0);

        var result = runner.Run(typeof(PriorityLongRunningBenchmark), typeof(DeferredFastBenchmark)).RunResult;
        var mandatory = result.Cases.Single(@case => @case.Strategy == BenchmarkMeasurementStrategy.LongRunning);
        var optional = result.Cases.Single(@case => @case.Strategy == BenchmarkMeasurementStrategy.Fast);

        Assert.Equal(2, mandatory.CompletedSampleCount);
        Assert.Equal(BenchmarkExecutionStatus.RequiredExecutionOverrun, mandatory.ExecutionStatus);
        Assert.Equal(TimeSpan.FromMilliseconds(8), mandatory.Diagnostics.BudgetOverrun);
        Assert.False(mandatory.Diagnostics.IsIncomplete);
        Assert.NotNull(mandatory.Diagnostics.OverrunReason);
        Assert.Equal(1, optional.CompletedSampleCount);
        Assert.Equal(BenchmarkExecutionStatus.Completed, optional.ExecutionStatus);
        Assert.Equal(BenchmarkExecutionStatus.RequiredExecutionOverrun, result.ExecutionStatus);
        Assert.Equal(TimeSpan.FromMilliseconds(8), result.Diagnostics.BudgetOverrun);
    }

    public class FastBenchmark
    {
        public static int Calls;
        [TinyBenchmarkFast(3, 4)] public void Measure() => Calls++;
    }

    public class RegularBenchmark
    {
        public static int Calls;
        [TinyBenchmarkRegular(2, 3)] public void Measure() => Calls++;
    }

    public class DefaultRegularBenchmark { public static int Calls; [TinyBenchmarkRegular(2)] public void Measure() => Calls++; }
    public class DisabledRegularWarmupBenchmark { public static int Calls; [TinyBenchmarkRegular(2, 0)] public void Measure() => Calls++; }
    public class AdaptiveFastBenchmark { public static int Calls; [TinyBenchmarkFast(2)] public void Measure() => Calls++; }
    public class AllocationBenchmark { public static int Calls; [TinyBenchmarkFast(2, 1)] public void Measure() { Calls++; _ = new byte[32]; } }

    public class LongBenchmark
    {
        public static int Calls;
        [TinyBenchmarkLongRunning(3)] public void Measure() => Calls++;
    }

    public class MixedBenchmark
    {
        [TinyBenchmarkRegular(1)] public void Modern() { }
        [TinyBenchmark] public void Legacy() { }
    }

    public static class MaximumQuotaBenchmark
    {
        public static int Calls;
    }

    public class MaximumFastBenchmark { [TinyBenchmarkFast(3, 1)] public void Measure() => MaximumQuotaBenchmark.Calls++; }
    public class MaximumRegularBenchmark { [TinyBenchmarkRegular(3, 0)] public void Measure() => MaximumQuotaBenchmark.Calls++; }
    public class MaximumLongRunningBenchmark { [TinyBenchmarkLongRunning(3)] public void Measure() => MaximumQuotaBenchmark.Calls++; }
    public class DefaultMaximumFastBenchmark { [TinyBenchmarkFast(10_001, 1)] public void Measure() => MaximumQuotaBenchmark.Calls++; }

    public class InterleavedFastBenchmark
    {
        [TinyBenchmarkFast(2, 1)] public void Measure() => InvocationLog.Current!.Add("fast");
    }

    public class InterleavedRegularBenchmark
    {
        [TinyBenchmarkRegular(2, 0)] public void Measure() => InvocationLog.Current!.Add("regular");
    }

    public class BudgetFastBenchmark { [TinyBenchmarkFast(3, 1)] public void Measure() { } }
    public class BudgetRegularBenchmark { [TinyBenchmarkRegular(3, 0)] public void Measure() { } }
    public class PriorityLongRunningBenchmark { [TinyBenchmarkLongRunning(2)] public void Measure() { } }
    public class DeferredFastBenchmark { [TinyBenchmarkFast(1, 1)] public void Measure() { } }

    private sealed class ControlledTimeSource : IBenchmarkTimeSource
    {
        public TimeSpan Now { get; private set; }
        public TimeSpan GetTimestamp() => Now;
        public void Advance(TimeSpan duration) => Now += duration;
    }

    private sealed class AdvancingExecutor : IBenchmarkInvocationExecutor
    {
        private readonly ControlledTimeSource _time;
        private readonly TimeSpan _duration;

        public AdvancingExecutor(ControlledTimeSource time, List<string>? log, TimeSpan duration)
        {
            _time = time;
            _duration = duration;
            InvocationLog.Current = log;
        }

        public void Invoke(Action invocation)
        {
            invocation();
            _time.Advance(_duration);
        }
    }

    private static class InvocationLog
    {
        public static List<string>? Current { get; set; }
    }
}
