namespace DimonSmart.TinyBenchmark;

/// <summary>Canonical, display-independent identity of a benchmark case.</summary>
public sealed record BenchmarkCaseIdentity
{
    public BenchmarkCaseIdentity(string className, string methodName, string methodSignature, string parameterIdentity, string parameterDisplay)
    {
        if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException("A class name is required.", nameof(className));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("A method name is required.", nameof(methodName));
        if (string.IsNullOrWhiteSpace(methodSignature)) throw new ArgumentException("A method signature is required.", nameof(methodSignature));
        if (string.IsNullOrWhiteSpace(parameterIdentity)) throw new ArgumentException("A parameter identity is required.", nameof(parameterIdentity));
        ClassName = className; MethodName = methodName; MethodSignature = methodSignature;
        ParameterIdentity = parameterIdentity; ParameterDisplay = parameterDisplay;
    }
    public string ClassName { get; }
    public string MethodName { get; }
    public string MethodSignature { get; }
    public string ParameterIdentity { get; }
    public string ParameterDisplay { get; }
}
