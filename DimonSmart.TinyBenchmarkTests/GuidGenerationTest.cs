using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

public class GuidGenerationTest
{
    [ParameterAttribute(1, 2, 3, 4, 5)] public int BenchmarkParameter { get; set; }

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