using DimonSmart.TinyBenchmark;
using Xunit;
using Xunit.Abstractions;

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
            .Create()
            .WithMaxRunExecutionTime(TimeSpan.FromSeconds(60))
            .WithBestTimeAsResult()
            .Run()
            // .WithCsvExporter()
            // .SaveRawResults()
            .WithGraphExporter()
            // .ExportAllRawGraph()
            // .OrderTimes()
            // .ExportAllRawGraph()
            .ExportAllFunctionsCompareGraph();
    }
}