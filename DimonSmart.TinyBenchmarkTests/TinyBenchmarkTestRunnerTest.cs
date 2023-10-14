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
            .WithRunCountLimits(10, 1000)
            .Run()
            .SaveRawResultsData()
            .WithGraphExporter()
            .SortTimes()
            .ExportAllRawGraph();


        // .ExportRawGraph(nameof(ExampleClassTest), nameof(ExampleClassTest.Function2), 10);
    }
}