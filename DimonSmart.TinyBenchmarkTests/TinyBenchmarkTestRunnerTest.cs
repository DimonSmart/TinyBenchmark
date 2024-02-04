using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using Xunit;
using Xunit.Abstractions;
using static DimonSmart.TinyBenchmark.Exporters.IGraphExporter.GraphExportOption;
using static DimonSmart.TinyBenchmark.SortTimeDirection;

namespace DimonSmart.TinyBenchmarkTests;

[TinyBenchmarkOnlyThisClass]
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
            .WithMaxRunExecutionTime(TimeSpan.FromSeconds(30), 100)
            .WithMinFunctionExecutionCount(500)
            .WithMaxFunctionExecutionCount(10000)
            .Run()
            .WithCsvExporter()
                .LimitResultLines(50)
                .SaveAllRawResults()
            .WithTableExporter()
                .SaveAllTablesResults()
            .WithGraphExporter()
            .ExportAllRawGraph(AscendingTimes)
            .ExportAllRawGraph()
            .ExportAllFunctionsCompareGraph(IncludeErrorMarks);
    }
}