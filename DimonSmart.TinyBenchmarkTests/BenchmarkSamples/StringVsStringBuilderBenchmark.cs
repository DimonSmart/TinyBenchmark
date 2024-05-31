using System.Text;
using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests.BenchmarkSamples;

public class StringVsStringBuilderBenchmark : VsBenchmarkBase
{
    [TinyBenchmarkRangeParameter(1, 32, 1)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void StringConcatenation(int parameter)
    {
        var s = string.Empty;
        for (var i = 0; i < parameter; i++) s += AllNames[i];
    }

    [TinyBenchmark]
    public void StringBuilder(int parameter)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < parameter; i++) sb.Append(AllNames[i]);
        var _ = sb.ToString();
    }
}