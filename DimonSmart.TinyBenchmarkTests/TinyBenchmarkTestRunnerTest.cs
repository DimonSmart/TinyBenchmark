using DimonSmart.TinyBenchmark;
using Xunit;
using Xunit.Abstractions;
using static DimonSmart.TinyBenchmark.SortTimeDirection;

namespace DimonSmart.TinyBenchmarkTests;

public class TinyBenchmarkTestRunnerTest
{
    private readonly ITestOutputHelper _output;

    public TinyBenchmarkTestRunnerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunAllBenchmarks()
    {
        TinyBenchmarkRunner
            .Create(_output.WriteLine)
            .WinMinMaxFunctionExecutionCount(100, 10000)
            .WithMaxRunExecutionTime(TimeSpan.FromSeconds(60))
            // .WithBestTimeAsResult()
            .Run()
        .WithCsvExporter()
        .SaveRawResults()
        .WithGraphExporter()
        .ExportAllRawGraph(AscendingTimes)
        .ExportAllRawGraph()
        .ExportAllFunctionsCompareGraph();
    }
}