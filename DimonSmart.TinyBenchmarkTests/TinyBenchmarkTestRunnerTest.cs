using DimonSmart.TinyBenchmark;
using Xunit;

namespace DimonSmart.TinyBenchmarkTests;

public class TinyBenchmarkTestRunnerTest
{
    [Fact]
    public void RunAllBenchmarks()
    {
        TinyBenchmarkRunner
            .Create()
            .Run();
    }
}