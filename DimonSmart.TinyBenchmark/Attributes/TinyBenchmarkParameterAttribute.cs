namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class TinyBenchmarkParameterAttribute : Attribute
{
    public TinyBenchmarkParameterAttribute(params object[] values)
    {
        Values = values;
    }

    public object[] Values { get; set; }
}