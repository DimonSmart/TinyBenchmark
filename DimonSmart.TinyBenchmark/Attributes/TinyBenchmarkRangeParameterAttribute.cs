namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class TinyBenchmarkRangeParameterAttribute : TinyBenchmarkParameterAttribute
{
    public TinyBenchmarkRangeParameterAttribute(int from, int to, int step = 1)
    {
        var values = new List<object>(to - from);
        for (var i = from; i < to; i += step) values.Add(i);

        Values = values.ToArray();
    }
}