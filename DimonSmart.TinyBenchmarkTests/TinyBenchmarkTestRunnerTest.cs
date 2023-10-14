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
            .OrderTimes()
            .ExportAllRawGraph()
            .OrderTimes(SortDirection.Unordered)
            .ExportAllRawGraph();


        // .ExportRawGraph(nameof(ExampleClassTest), nameof(ExampleClassTest.Function2), 10);
    }
}