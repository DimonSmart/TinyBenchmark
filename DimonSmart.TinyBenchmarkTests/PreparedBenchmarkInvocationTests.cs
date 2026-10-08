using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class PreparedBenchmarkInvocationTests
{
    [Fact]
    public void NonParameterizedBenchmarkIsInvokedThroughPreparedDelegate()
    {
        NonParameterizedBenchmark.InvocationCount = 0;

        RunOnce<NonParameterizedBenchmark>();

        Assert.Equal(1, NonParameterizedBenchmark.InvocationCount);
    }

    [Fact]
    public void ParameterizedReturnValueBenchmarkPreservesLegacyBatching()
    {
        ParameterizedReturnValueBenchmark.Total = 0;

        RunOnce<ParameterizedReturnValueBenchmark>();

        Assert.Equal(15, ParameterizedReturnValueBenchmark.Total);
    }

    [Fact]
    public void BenchmarkReturnValueIsDiscarded()
    {
        ReturnValueBenchmark.InvocationCount = 0;

        RunOnce<ReturnValueBenchmark>();

        Assert.Equal(1, ReturnValueBenchmark.InvocationCount);
    }

    [Fact]
    public void BenchmarkExceptionPropagatesWithoutReflectionWrapper()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RunOnce<ThrowingBenchmark>());

        Assert.Equal("benchmark failure", exception.Message);
    }

    private static void RunOnce<TBenchmark>()
    {
        TinyBenchmarkRunner.Create()
            .WithMinFunctionExecutionCount(1)
            .WithMaxFunctionExecutionCount(1)
            .Run(typeof(TBenchmark));
    }

    public sealed class NonParameterizedBenchmark
    {
        public static int InvocationCount { get; set; }

        [TinyBenchmark]
        public void Measure()
        {
            InvocationCount++;
        }
    }

    public sealed class ParameterizedReturnValueBenchmark
    {
        public static int Total { get; set; }

        [TinyBenchmarkParameter(3)]
        public int Parameter { get; set; }

        [TinyBenchmark]
        public int Measure(int value)
        {
            Total += value;
            return Total;
        }
    }

    public sealed class ReturnValueBenchmark
    {
        public static int InvocationCount { get; set; }

        [TinyBenchmark]
        public int Measure()
        {
            return ++InvocationCount;
        }
    }

    public sealed class ThrowingBenchmark
    {
        [TinyBenchmark]
        public void Measure()
        {
            throw new InvalidOperationException("benchmark failure");
        }
    }
}
