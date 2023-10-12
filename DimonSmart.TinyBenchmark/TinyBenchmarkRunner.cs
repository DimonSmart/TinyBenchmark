using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner
{
    public int MaxFunctionRunTImeMilliseconds { get; private set; } = 1000;
   
    public IReadOnlyCollection<MethodExecutionResults> Results { get; private set; }
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
        var results = new List<MethodExecutionResults>();
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

                    var times = MeasureAndRunAction(methodExecutionInfo.Action, MaxFunctionRunTImeMilliseconds);
                    var result = new MethodExecutionResults(methodExecutionInfo, times);
                    results.Add(result);
                }
            }
        }
    }

    public static List<TimeSpan> MeasureAndRunAction(Action action, int maxTotalMilliseconds, int minExecutionCount = 1)
    {
        var firstRunTime = MeasureExecutionTime(action);
        var maxIterations = (int)(maxTotalMilliseconds / firstRunTime.TotalMilliseconds);

        maxIterations = Math.Max(maxIterations, minExecutionCount);
        var executionTimes = new List<TimeSpan>(maxIterations + 1);
        executionTimes.Add(firstRunTime);
        PrepareRun();

        for (var i = 0; i < maxIterations; i++)
        {
            executionTimes.Add(MeasureExecutionTime(action));
        }

        return executionTimes;
    }

    private static void PrepareRun()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static TimeSpan MeasureExecutionTime(Action action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
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
        return new MethodExecutionInfo (classUnderTestType,  methodUnderTest, parameterAttributeValue, action);
    }
}