namespace DimonSmart.TinyBenchmark.Attributes;

/// <summary>
/// Marks a method for the legacy TinyBenchmark compatibility execution mode.
/// </summary>
/// <remarks>
/// This mode keeps the established fluent repetition and RAW-export behavior. New benchmarks should select one
/// of the explicit strategy attributes instead.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TinyBenchmarkAttribute : Attribute
{
}
