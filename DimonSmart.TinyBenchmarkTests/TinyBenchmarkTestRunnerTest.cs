using DimonSmart.TinyBenchmark;
using Xunit;
using Xunit.Abstractions;
using static DimonSmart.TinyBenchmark.Exporters.IGraphExporter.GraphExportOption;
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
            .WithMaxRunExecutionTime(TimeSpan.FromSeconds(180))
            .WinMinMaxFunctionExecutionCount(1000, 100000)
            .Run()
            .WithCsvExporter()
            .SaveAllRawResults(50)
            .WithGraphExporter()
            .ExportAllRawGraph(AscendingTimes)
            .ExportAllRawGraph()
            .ExportAllFunctionsCompareGraph(IncludeErrorMarks);
    }
}