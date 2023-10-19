namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RangeParameterAttribute : ParameterAttribute
{
    public RangeParameterAttribute(int from, int to, int step = 1)
    {
        var values = new List<object>(to - from);
        for (var i = from; i < to; i++) values.Add(i);

        Values = values.ToArray();
    }
}