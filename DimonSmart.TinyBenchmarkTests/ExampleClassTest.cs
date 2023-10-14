using System.Diagnostics;
using DimonSmart.TinyBenchmark;

namespace DimonSmart.TinyBenchmarkTests;

public class ExampleClassTest
{
    [TinyBenchmarkParameter(1, 10, 20, 100)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void Function1(int parameter)
    {
        Debug.WriteLine($"{nameof(Function1)}:{parameter}");
        var guid = Guid.NewGuid();
    }

    [TinyBenchmark]
    public void Function2(int parameter)
    {
        Debug.WriteLine($"{nameof(Function2)}:{parameter}");
        for (var i = 0; i < parameter; i++)
        {
            var guid = Guid.NewGuid();
        }
    }
}