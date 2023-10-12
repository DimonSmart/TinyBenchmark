using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner
{
    private TinyBenchmarkRunner()
    {
    }

    public static TinyBenchmarkRunner Create()
    {
        return new TinyBenchmarkRunner();
    }

    public void Run()
    {
        Console.WriteLine("Running TinyBenchmarks...");
        var classesUnderTest = AttributeUtility.GetClassesUnderTest();

        foreach (var classUnderTestType in classesUnderTest)
        {
            Console.WriteLine($"Benchmarking class: {classUnderTestType.Name}");

            var classUnderTestProperty = AttributeUtility.FindClassUnderTestParameterProperty(classUnderTestType);
            var parameterAttribute = AttributeUtility.GetParameterAttributeFromProperty(classUnderTestProperty);

            var methodsUnderTest = AttributeUtility.GetMethodsWithTinyBenchmarkAttribute(classUnderTestType);

            // Instantiate the class under test
            var classForTest = Activator.CreateInstance(classUnderTestType);

            foreach (var methodUnderTest in methodsUnderTest)
            {
                Console.WriteLine($"    Benchmarking method: {methodUnderTest.Name}");
                var methodExecutionInfos = GetMethodExecutionInfos(parameterAttribute, methodUnderTest, classForTest,
                    classUnderTestType);

                foreach (var methodExecutionInfo in methodExecutionInfos)
                {
                    if (parameterAttribute != null)
                    {
                        Console.WriteLine(@$"         Argument: {methodExecutionInfo.Parameter ?? "null"}");
                    }
                    // Benchmark method execution here
                }
            }
        }
    }

    private static List<MethodExecutionInfo> GetMethodExecutionInfos(
        TinyBenchmarkParameterAttribute? parameterAttribute,
        MethodInfo methodUnderTest, object? classForTest, Type classUnderTestType)
    {
        var methodExecutionInfos = new List<MethodExecutionInfo>();
        if (parameterAttribute != null)
        {
            foreach (var parameterAttributeValue in parameterAttribute.Values)
            {
                methodExecutionInfos.Add(CreateMethodExecutionInfo(methodUnderTest, classForTest, methodExecutionInfos,
                    classUnderTestType, parameterAttributeValue));
            }
        }
        else
        {
            methodExecutionInfos.Add(CreateMethodExecutionInfo(methodUnderTest, classForTest, methodExecutionInfos,
                classUnderTestType));
        }

        return methodExecutionInfos;
    }

    private static MethodExecutionInfo CreateMethodExecutionInfo(MethodInfo methodUnderTest, object? classForTest,
        List<MethodExecutionInfo> methodExecutionInfos, Type classUnderTestType, object? parameterAttributeValue = null)
    {
        var parameters = parameterAttributeValue == null ? null : new[] { parameterAttributeValue };
        var action = () => { methodUnderTest.Invoke(classForTest, parameters); };
        return new MethodExecutionInfo
        {
            ClassType = classUnderTestType,
            MethodInfo = methodUnderTest,
            Parameter = parameterAttributeValue,
            Action = action
        };
    }
}