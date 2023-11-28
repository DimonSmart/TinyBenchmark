namespace DimonSmart.TinyBenchmark.Attributes;

/// <summary>
/// Attribute to be applied to methods that should be benchmarked. When this attribute is applied to a method,
/// it indicates that the method is a benchmark and should be included in the benchmarking process.
/// </summary>
/// <remarks>
/// Make sure to mark all methods that you want to be part of the benchmarking process with this attribute.
/// This attribute helps the TinyBenchmark library identify and include specific methods for performance measurement.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class TinyBenchmarkAttribute : Attribute
{
}