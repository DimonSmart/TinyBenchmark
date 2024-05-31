using DimonSmart.TinyBenchmark.Utils;
using System.Security.AccessControl;
using Xunit;
using Xunit.Abstractions;

namespace DimonSmart.TinyBenchmarkTests;


public class SimpleBaseClass
{   
    public virtual int GetNumber(int x) => 1;
}

public class NonSealedSimple : SimpleBaseClass
{ 
    public override int GetNumber(int x) => x * 2;
}

public sealed class SealedSimple : SimpleBaseClass
{
    public override int GetNumber(int x) => 2;
}


public class EnumerableUtilsTests
{
    private readonly ITestOutputHelper _output;

    public EnumerableUtilsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LimitProportionally_ShouldThrowOnNegativeLimit()
    {
        var source = new List<int> { 1, 2, 3 };
        Assert.Throws<ArgumentException>(() => source.LimitProportionally(-1));
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 5)]
    [InlineData(3, 2)]
    public void LimitProportionally_ShouldReturnProperCount(int sourceSize, int limit)
    {
        var source = Enumerable.Range(1, sourceSize).ToList();
        var result = source.LimitProportionally(limit);
        Assert.Equal(Math.Min(sourceSize, limit), result.Count());
    }

    [Fact]
    public void LimitProportionally_ShouldReturnAllForLimitGreaterThanSize()
    {
        var source = Enumerable.Range(1, 5).ToList();
        var result = source.LimitProportionally(10);
        Assert.Equal(source.Count, result.Count());
    }
}