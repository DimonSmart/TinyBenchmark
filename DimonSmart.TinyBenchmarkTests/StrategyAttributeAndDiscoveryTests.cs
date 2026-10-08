using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class StrategyAttributeAndDiscoveryTests
{
    [Fact]
    public void StrategyAttributesRequireValidCountsAndOptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkFastAttribute(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkFastAttribute(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkRegularAttribute(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkRegularAttribute(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkLongRunningAttribute(0));
    }

    [Fact]
    public void StrategyMarkerIsDiscoveredWhenInheritedFromAnOverride()
    {
        var methods = AttributeUtility.GetMethodsWithTinyBenchmarkAttribute(typeof(InheritedStrategyBenchmark));

        Assert.Single(methods);
        Assert.Equal(nameof(InheritedStrategyBenchmark.Measure), methods.Single().Name);
    }

    [Fact]
    public void LegacyMarkerDiscoveryRemainsAvailable()
    {
        var methods = AttributeUtility.GetMethodsWithTinyBenchmarkAttribute(typeof(LegacyBenchmark));

        Assert.Single(methods);
        Assert.Equal(nameof(LegacyBenchmark.Measure), methods.Single().Name);
    }

    [Fact]
    public void DuplicateStrategiesAreRejectedBeforeBenchmarkInvocation()
    {
        Assert.Throws<InvalidOperationException>(() => TinyBenchmarkRunner.Create().Run(typeof(DuplicateStrategyBenchmark)));
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 2, -1)]
    [InlineData(2, 1, 1)]
    public void RangeParameterRejectsInvalidRanges(int from, int to, int step)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TinyBenchmarkRangeParameterAttribute(from, to, step));
    }

    private class LegacyBenchmark
    {
        [TinyBenchmark]
        public void Measure()
        {
        }
    }

    private class BaseStrategyBenchmark
    {
        [TinyBenchmarkRegular(1)]
        public virtual void Measure()
        {
        }
    }

    private sealed class InheritedStrategyBenchmark : BaseStrategyBenchmark
    {
        public override void Measure()
        {
        }
    }

    private class DuplicateStrategyBenchmark
    {
        [TinyBenchmarkFast(1)]
        [TinyBenchmarkRegular(1)]
        public void Measure()
        {
        }
    }
}
