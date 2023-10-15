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
            .WithRunCountLimits(20, 1000)
            .Run()
            .WithCsvExporter()
            .SaveRawResultsDataAsCsv()
            .WithGraphExporter()
            .OrderTimes()
            .ExportAllRawGraph()
            .OrderTimes(SortDirection.Unordered)
            .ExportAllRawGraph()
            .ExportAllFunctionsCompareGraph(nameof(ExampleClassTest));


        // .ExportRawGraph(nameof(ExampleClassTest), nameof(ExampleClassTest.Function2), 10);
    }
}