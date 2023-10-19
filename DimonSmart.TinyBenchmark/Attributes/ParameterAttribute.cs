namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ParameterAttribute : Attribute
{
    public ParameterAttribute(params object[] values)
    {
        Values = values;
    }

    public object[] Values { get; set; }
}