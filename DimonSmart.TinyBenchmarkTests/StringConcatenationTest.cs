using System.Text;
using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

public class StringConcatenationTest : VsTestsBase
{
    [TinyBenchmarkRangeParameter(1, 60, 5)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void StringPlus(int parameter)
    {
        var s = string.Empty;
        for (var i = 0; i < parameter; i++) s = s + AllNames[i];
    }

    [TinyBenchmark]
    public void StringBuilder(int parameter)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < parameter; i++) sb.Append(AllNames[i]);
        var s = sb.ToString();
    }
}