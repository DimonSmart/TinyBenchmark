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
                                            
# Usage Example
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
                .ExportAllRawGraph(AscendingTimes)
                .ExportAllFunctionsCompareGraph();
    }
}


```

## Result examples

### Function Comparison Graph:
This graph shows the performance comparison of your functions. The X-axis represents
```csharp
[TinyBenchmarkParameter(1, 5, 10, 15, 20, 25)],
```
and the Y-axis represents function execution time. All functions are labeled with their names.
![Function comparision](https://raw.githubusercontent.com/DimonSmart/TinyBenchmark/master/DimonSmart.TinyBenchmark/Docs/Images/Compare-ExampleClassTest.png)

### Raw Execution Times
This graph is useful for ensuring that your functions provide stable values over time.
Slightly fluctuated results are okay, but if you see something completely different,
it could indicate the influence of parallel running processes on your measurement process.
![Function comparision](https://raw.githubusercontent.com/DimonSmart/TinyBenchmark/master/DimonSmart.TinyBenchmark/Docs/Images/Raw-ExampleClassTest-Function1-1-Unordered.png)

### Raw Results (Ordered)
This graph also represents raw timing values but sorted.
A typical graph shape should resemble an almost horizontal line with a sharp rise at the end.
This sharp end corresponds to longer execution times, and the shorter this rising part, the better.
![Function comparision](https://raw.githubusercontent.com/DimonSmart/TinyBenchmark/master/DimonSmart.TinyBenchmark/Docs/Images/Raw-ExampleClassTest-Function1-1-Ascending.png)

## Attributes

### [TinyBenchmarkOnlyThisClassAttribute]

The `TinyBenchmarkOnlyThisClassAttribute` is designed for classes within the TinyBenchmark library. Applying this attribute to a class allows developers to streamline the test development process. It enables the easy exclusion of all benchmarks within a class except for the one currently under active development. By doing so, developers can maintain a clean and focused Git changes list, concentrating changes exclusively on the actively developed test.

### [TinyBenchmarkAttribute]

The `TinyBenchmarkAttribute` is utilized for marking methods within benchmark classes. When applied, this attribute signals that the marked methods should be included in the benchmarking process. This attribute is crucial for pinpointing and measuring the performance of specific functions during benchmarking.

### [TinyBenchmarkParameterAttribute]

The `TinyBenchmarkParameterAttribute` is used to designate parameters for functions under benchmark. Applied to properties within benchmark classes, this attribute allows developers to provide specific values for parameters. This flexibility facilitates the variation of inputs during benchmarking, enabling the measurement of performance under different scenarios.

### [TinyBenchmarkRangeParameterAttribute]

The `TinyBenchmarkRangeParameterAttribute` is specifically tailored for defining range parameters for functions under benchmark. Applied to properties within benchmark classes, this attribute simplifies the specification of a range of values for parameters. This enhances flexibility in benchmarking scenarios, allowing developers to conduct tests across a specified range of inputs.

#### Usage Example

Consider the following example with a hypothetical benchmark class:

```csharp
[TinyBenchmarkOnlyThisClass]
public class MyBenchmarkClass
{
    // Initialization logic here...

    [TinyBenchmarkRangeParameter(1, 60, 1)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void MyBenchmarkedMethod(int parameter)
    {
        // Benchmark logic for MyBenchmarkedMethod...
    }

    // Additional benchmark methods...
}
```

In this example, the `MyBenchmarkClass` class demonstrates the application of various TinyBenchmark attributes. These attributes contribute to a focused and effective benchmarking environment, enabling developers to streamline test development and measure performance with precision.