namespace DimonSmart.TinyBenchmark.Attributes;

/// <summary>
/// Attribute to be applied to benchmark classes, indicating that only benchmarks in the marked class should be considered
/// during testing. This attribute is useful for focusing changes on a specific test during development without marking
/// all other tests as ignored. It helps keep the Git changes list clean and focused on the active test.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TinyBenchmarkOnlyThisClassAttribute : Attribute
{
}