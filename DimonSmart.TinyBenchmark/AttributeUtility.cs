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

        return classes;
    }

    public static IReadOnlyCollection<Type> GetClassesWithMethodsMarkedWithAttribute<T>(Assembly assembly)
        where T : Attribute
    {
        var classTypes = assembly.GetTypes();
        var classesWithBenchmarkMethods = new List<Type>();

        foreach (var classType in classTypes)
        {
            var benchmarkMethods = classType.GetMethods()
                .Where(method => Attribute.IsDefined(method, typeof(TinyBenchmarkAttribute)))
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
            .Where(method => Attribute.IsDefined(method, typeof(TinyBenchmarkAttribute)))
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
}