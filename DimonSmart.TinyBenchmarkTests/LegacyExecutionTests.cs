using System.Reflection;
using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class LegacyExecutionTests
{
    [Fact]
    public void LegacyLimitsRejectContradictoryValuesInEitherCallOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TinyBenchmarkRunner.Create()
            .WithMaxFunctionExecutionCount(4)
            .WithMinFunctionExecutionCount(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => TinyBenchmarkRunner.Create()
            .WithMinFunctionExecutionCount(5)
            .WithMaxFunctionExecutionCount(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TinyBenchmarkRunner.Create().WithMaxFunctionExecutionCount(0));
    }

    [Fact]
    public void RunBudgetRejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TinyBenchmarkRunner.Create()
            .WithMaxRunExecutionTime(TimeSpan.FromTicks(-1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TinyBenchmarkRunner.Create()
            .WithMaxRunExecutionTime(TimeSpan.Zero, -1));
    }

    [Fact]
    public void PublicRunnerInterfaceSupportsRemovingTheTimeLimitFluently()
    {
        ITinyBenchmarkRunner runner = TinyBenchmarkRunner.Create();

        var configured = runner
            .WithMaxRunExecutionTime(TimeSpan.Zero, 0)
            .WithoutRunExecutionTimeLimit()
            .WithMinFunctionExecutionCount(1)
            .WithMaxFunctionExecutionCount(1);

        Assert.Same(runner, configured);
    }

    [Fact]
    public void LegacySamplesRespectDeadlineAndReportAnOverrun()
    {
        LegacyBudgetBenchmark.Calls = 0;
        var time = new ControlledTimeSource();
        var runner = new TinyBenchmarkRunner(time, new AdvancingExecutor(time, TimeSpan.FromMilliseconds(3)))
            .WithMaxRunExecutionTime(TimeSpan.FromMilliseconds(5), 0)
            .WithMinFunctionExecutionCount(3)
            .WithMaxFunctionExecutionCount(4);

        var result = runner.Run(typeof(LegacyBudgetBenchmark)).RunResult.Cases.Single();

        Assert.Equal(2, LegacyBudgetBenchmark.Calls);
        Assert.Equal(2, result.CompletedSampleCount);
        Assert.Equal(4, result.RequestedSampleCount);
        Assert.Equal(BenchmarkExecutionStatus.BudgetExhausted, result.ExecutionStatus);
        Assert.Equal(TimeSpan.FromMilliseconds(1), result.Diagnostics.BudgetOverrun);
        Assert.NotNull(result.Diagnostics.OverrunReason);
    }

    [Fact]
    public void LegacyRawListsGrowWithUsefulSamplesInsteadOfMillionEntryPreallocation()
    {
        var runner = TinyBenchmarkRunner.Create()
            .WithMinFunctionExecutionCount(1)
            .WithMaxFunctionExecutionCount(1);

        runner.Run(typeof(LegacyCapacityBenchmark));

        var dataField = typeof(TinyBenchmarkRunner).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var data = Assert.IsType<BenchmarkData>(dataField.GetValue(runner));
        Assert.All(data.Results, result => Assert.True(Assert.IsType<List<MethodExecutionMetrics>>(result.Numbers).Capacity < 1_000));
    }

    public sealed class LegacyBudgetBenchmark
    {
        public static int Calls;
        [TinyBenchmark] public void Measure() => Calls++;
    }

    public sealed class LegacyCapacityBenchmark
    {
        [TinyBenchmark] public void First() { }
        [TinyBenchmark] public void Second() { }
    }

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

        public AdvancingExecutor(ControlledTimeSource time, TimeSpan duration)
        {
            _time = time;
            _duration = duration;
        }

        public void Invoke(Action invocation)
        {
            invocation();
            _time.Advance(_duration);
        }
    }
}
