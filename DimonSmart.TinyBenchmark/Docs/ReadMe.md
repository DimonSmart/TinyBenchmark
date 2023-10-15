# TinyBenchmark
A Simple and User-Friendly Benchmarking Library

TinyBenchmark is a straightforward and user-friendly benchmarking library designed to facilitate a transparent comparison of execution times between
 two alternative functions.
With the ability to parameterize functions using a single argument, TinyBenchmark makes it easy to assess performance differences.

**Key Features:**

- **Easy to Use**: TinyBenchmark simplifies the process of benchmarking, ensuring you can quickly and effortlessly compare the execution times
 of different functions.

- **Parameterization**: Easily parametrize functions with a single argument for more versatile testing.

- **Export Options**: Benchmarking results can be exported in various formats, including:

    1. **RAW Plain CSV**: Generate raw data in CSV format for further analysis.

    2. **Aggregate Data**: Aggregate raw data for all benchmarking points or selectively for specified points of interest.

    3. **Function Comparison Graphs**: Visualize performance comparisons between functions within test classes.

TinyBenchmark is the ideal tool for gaining valuable insights into your code's performance.
Whether you're a developer looking to optimize your code or simply curious about execution times, TinyBenchmark has you covered.
                                            
# Ussage example
## Test class
```csharp
using DimonSmart.TinyBenchmark;
namespace DimonSmart.TinyBenchmarkTests;

public class ExampleClassTest
{
    [TinyBenchmarkParameter(1, 5, 10, 15, 20, 25)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void Function1(int parameter)
    {
        var guid = Guid.NewGuid();
    }

    [TinyBenchmark]
    public void Function2(int parameter)
    {
        for (var i = 0; i < parameter; i++)
        {
            var guid = Guid.NewGuid();
        }
    }
}

```

## Benchmark Runner
```csharp
public class TinyBenchmarkTestRunnerTest
{
    [Fact]
    public void RunAllBenchmarks()
    {
        TinyBenchmarkRunner
            .Create()
            .Run()
            .WithCsvExporter()
                .SaveRawResults()
            .WithGraphExporter()
                .ExportAllRawGraph()
                .OrderTimes()
                .ExportAllRawGraph()
                .ExportAllFunctionsCompareGraph(nameof(ExampleClassTest));
    }
}

```

## Result examples

### Functions comparision

![Function comparision](https://github.com/DimonSmart/TinyBenchmark/blob/main/Compare-ExampleClassTest.png?raw=true)


