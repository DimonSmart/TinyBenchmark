using System.Reflection;
using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmark;

public static class AttributeUtility
{
    public static IReadOnlyCollection<Type> GetClassesUnderTest()
    {
        var classes = new List<Type>();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var loadedAssembly in loadedAssemblies)
        {
            classes.AddRange(GetClassesWithMethodsMarkedWithAttribute<TinyBenchmarkAttribute>(loadedAssembly));
        }

        var selectedClasses = classes.Where(type => type.GetCustomAttribute<TinyBenchmarkOnlyThisClassAttribute>() != null).ToList();

        return selectedClasses.Any() ? selectedClasses.AsReadOnly() : classes.AsReadOnly();
    }

    public static IReadOnlyCollection<Type> GetClassesWithMethodsMarkedWithAttribute<T>(Assembly assembly)
        where T : Attribute
    {
        var classTypes = assembly.GetTypes();
        var classesWithBenchmarkMethods = new List<Type>();

        foreach (var classType in classTypes)
        {
            var benchmarkMethods = classType.GetMethods()
                .Where(method => Attribute.IsDefined(method, typeof(T), inherit: true))
                .ToList();

            if (benchmarkMethods.Any())
            {
                classesWithBenchmarkMethods.Add(classType);
            }
        }

        return classesWithBenchmarkMethods;
    }

    public static IReadOnlyCollection<MethodInfo> GetMethodsWithTinyBenchmarkAttribute(Type classType)
    {
        var benchmarkMethods = classType.GetMethods()
            .Where(method => Attribute.IsDefined(method, typeof(TinyBenchmarkAttribute), inherit: true))
            .ToList();

        return benchmarkMethods;
    }

    public static PropertyInfo? FindClassUnderTestParameterProperty(Type classType)
    {
        var properties = classType.GetProperties();
        PropertyInfo? parameterProperty = null;

        foreach (var property in properties)
        {
            if (Attribute.IsDefined(property, typeof(TinyBenchmarkParameterAttribute)))
            {
                if (parameterProperty != null)
                {
                    throw new Exception("Multiple properties marked with TinyBenchmarkParameter found.");
                }

                parameterProperty = property;
            }
        }

        return parameterProperty;
    }

    public static object[]? GetParametersFromAttribute(PropertyInfo? property)
    {
        return property?.GetCustomAttribute<TinyBenchmarkParameterAttribute>()?.Values;
    }

    internal static IReadOnlyCollection<BenchmarkMethodDeclaration> GetBenchmarkMethodDeclarations(Type classType)
    {
        var declarations = GetMethodsWithTinyBenchmarkAttribute(classType)
            .Select(CreateBenchmarkMethodDeclaration)
            .ToList();

        ValidateParameterDeclaration(classType, declarations);
        return declarations;
    }

    private static BenchmarkMethodDeclaration CreateBenchmarkMethodDeclaration(MethodInfo method)
    {
        ValidateMethodSignature(method);

        var strategyAttributes = method.GetCustomAttributes<TinyBenchmarkAttribute>(inherit: true)
            .Select(attribute => attribute switch
            {
                TinyBenchmarkFastAttribute fast => (BenchmarkStrategy.Fast, fast.RequestedSampleCount),
                TinyBenchmarkRegularAttribute regular => (BenchmarkStrategy.Regular, regular.RequestedSampleCount),
                TinyBenchmarkLongRunningAttribute longRunning => (BenchmarkStrategy.LongRunning, longRunning.RequestedSampleCount),
                _ => ((BenchmarkStrategy?)null, (int?)null)
            })
            .Where(strategy => strategy.Item1.HasValue)
            .ToList();

        if (strategyAttributes.Count > 1)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' declares multiple strategies.");
        }

        if (strategyAttributes.Count == 0)
        {
            return new BenchmarkMethodDeclaration(method, null, null);
        }

        var (strategy, requestedSampleCount) = strategyAttributes[0];
        if (requestedSampleCount <= 0)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' has a non-positive requested sample count.");
        }

        return new BenchmarkMethodDeclaration(method, strategy, requestedSampleCount);
    }

    private static void ValidateMethodSignature(MethodInfo method)
    {
        if (method.ContainsGenericParameters)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' cannot be generic.");
        }

        if (typeof(Task).IsAssignableFrom(method.ReturnType) || method.ReturnType == typeof(ValueTask))
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' must be synchronous.");
        }

        var parameters = method.GetParameters();
        if (parameters.Length > 1 || parameters.Any(parameter => parameter.ParameterType.IsByRef || parameter.IsOut))
        {
            throw new InvalidOperationException(
                $"Benchmark method '{method.Name}' must have no parameters or one value parameter.");
        }
    }

    private static void ValidateParameterDeclaration(
        Type classType,
        IReadOnlyCollection<BenchmarkMethodDeclaration> declarations)
    {
        var parameterProperties = classType.GetProperties()
            .Where(property => Attribute.IsDefined(property, typeof(TinyBenchmarkParameterAttribute), inherit: true))
            .ToList();

        if (parameterProperties.Count > 1)
        {
            throw new InvalidOperationException("Multiple properties marked with TinyBenchmarkParameter found.");
        }

        var parameterizedMethods = declarations.Where(declaration => declaration.Method.GetParameters().Length == 1).ToList();
        if (parameterProperties.Count == 0)
        {
            if (parameterizedMethods.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Benchmark class '{classType.Name}' has a method parameter without a TinyBenchmarkParameter declaration.");
            }

            return;
        }

        if (parameterizedMethods.Count != declarations.Count)
        {
            throw new InvalidOperationException(
                $"Benchmark class '{classType.Name}' must use its TinyBenchmarkParameter declaration for every benchmark method.");
        }

        var values = GetParametersFromAttribute(parameterProperties[0]);
        if (values is not { Length: > 0 })
        {
            throw new InvalidOperationException("TinyBenchmarkParameter must declare at least one value.");
        }

        foreach (var method in parameterizedMethods)
        {
            var parameterType = method.Method.GetParameters()[0].ParameterType;
            if (values.Any(value => value is null || !parameterType.IsInstanceOfType(value)))
            {
                throw new InvalidOperationException(
                    $"Values declared by TinyBenchmarkParameter are incompatible with '{method.Method.Name}'.");
            }
        }
    }
}
