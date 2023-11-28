namespace DimonSmart.TinyBenchmark.Attributes;

/// <summary>
/// Attribute to be applied to a single field within a class, representing a parameter for functions under benchmark.
/// </summary>
/// <remarks>
/// Use this attribute to mark a field within a benchmark class that represents a parameter for the benchmarked functions.
/// This attribute should be applied only once per class to designate the parameter for benchmarking. It allows you to provide
/// specific values for this parameter, facilitating the variation of inputs during benchmarking to measure performance
/// under different scenarios.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class TinyBenchmarkParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TinyBenchmarkParameterAttribute"/> class with the specified values.
    /// </summary>
    /// <param name="values">The values to be associated with the benchmark parameter.</param>
    public TinyBenchmarkParameterAttribute(params object[] values)
    {
        Values = values;
    }

    public object[] Values { get; set; }
}