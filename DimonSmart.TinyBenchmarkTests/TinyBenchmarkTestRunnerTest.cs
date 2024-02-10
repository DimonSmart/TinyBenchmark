using DimonSmart.TinyBenchmark;
using DimonSmart.TinyBenchmark.Attributes;
using DimonSmart.TinyBenchmarkTests.BenchmarkSamples;
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

    /// <summary>
    /// Full run
    /// </summary>
    [Fact]
    public void StringVsStringBuilderBenchmark()
    {
        TinyBenchmarkRunner
            .Create()
            .WithLogger(_output.WriteLine)
            .WithMaxRunExecutionTime(TimeSpan.FromSeconds(30), 100)
            .WithMinFunctionExecutionCount(100)
            .WithMaxFunctionExecutionCount(10000)
            .WithResultSubfolders(true)
            .WithMemoryBenchmarking()
            .Run(typeof(StringVsStringBuilderBenchmark))
            .WithCsvExporter()
                .LimitResultLines(50)
                .SaveAllRawResults()
            .WithTableExporter()
                .SaveAllTablesResults()
            .WithGraphExporter()
                .ExportAllRawGraph(AscendingTimes)
                .ExportAllFunctionsCompareGraph(IncludeErrorMarks);
    }
}