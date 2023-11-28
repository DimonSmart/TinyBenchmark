namespace DimonSmart.TinyBenchmark.Attributes;

/// <summary>
/// Attribute to be applied to a single field within a class, representing a range parameter for functions under benchmark.
/// </summary>
/// <remarks>
/// This attribute is derived from <see cref="TinyBenchmarkParameterAttribute"/> and is specifically designed for defining
/// a range of values as a parameter for benchmarked functions. It allows you to specify a range using the 'from', 'to', and
/// optional 'step' parameters, creating a sequence of values within the specified range.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class TinyBenchmarkRangeParameterAttribute : TinyBenchmarkParameterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TinyBenchmarkRangeParameterAttribute"/> class with the specified range.
    /// </summary>
    /// <param name="from">The starting value of the range (inclusive).</param>
    /// <param name="to">The ending value of the range (exclusive).</param>
    /// <param name="step">The optional step size between values in the range (default is 1).</param>
    public TinyBenchmarkRangeParameterAttribute(int from, int to, int step = 1)
    {
        var values = new List<object>(to - from);
        for (var i = from; i < to; i += step) values.Add(i);

        Values = values.ToArray();
    }
}