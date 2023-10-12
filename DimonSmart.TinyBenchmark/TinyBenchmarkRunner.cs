using System.Diagnostics;
using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner
{
    private TinyBenchmarkRunner()
    {
    }

    public int MaxFunctionRunTImeMilliseconds { get; } = 1000;

    public IReadOnlyCollection<MethodExecutionResults> Results { get; }

    public static TinyBenchmarkRunner Create()
    {
        return new TinyBenchmarkRunner();
    }

    public void Run()
    {
        var results = new List<MethodExecutionResults>();
        var methodExecutionInfos = GetMethodExecutionInfos();

        foreach (var methodExecutionInfo in methodExecutionInfos)
        {
            var times = MeasureAndRunAction(methodExecutionInfo.Action, MaxFunctionRunTImeMilliseconds);
            var result = new MethodExecutionResults(methodExecutionInfo, times);
            results.Add(result);
        }
    }

    private static List<MethodExecutionInfo> GetMethodExecutionInfos()
    {
        var classesUnderTest = AttributeUtility.GetClassesUnderTest();
        var executionInfos = new List<MethodExecutionInfo>();
        foreach (var classUnderTestType in classesUnderTest)
        {
            var classUnderTestProperty = AttributeUtility.FindClassUnderTestParameterProperty(classUnderTestType);
            var parameterAttribute = AttributeUtility.GetParameterAttributeFromProperty(classUnderTestProperty);
            var methodsUnderTest = AttributeUtility.GetMethodsWithTinyBenchmarkAttribute(classUnderTestType);
            var classForTest = Activator.CreateInstance(classUnderTestType);

            foreach (var methodUnderTest in methodsUnderTest)
            {
                executionInfos.AddRange(
                    GetMethodExecutionInfos(
                        parameterAttribute, methodUnderTest, classForTest, classUnderTestType));
            }
        }
        return executionInfos;
    }

    public static List<TimeSpan> MeasureAndRunAction(Action action, int maxTotalMilliseconds, int minExecutionCount = 1)
    {
        var firstRunTime = MeasureExecutionTime(action);
        var maxIterations = (int)(maxTotalMilliseconds / firstRunTime.TotalMilliseconds);

        maxIterations = Math.Max(maxIterations, minExecutionCount);
        var executionTimes = new List<TimeSpan>(maxIterations + 1);
        executionTimes.Add(firstRunTime);
        PrepareRun();

        for (var i = 0; i < maxIterations; i++) executionTimes.Add(MeasureExecutionTime(action));

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
        var stopwatch = Stopwatch.StartNew();
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
        return new MethodExecutionInfo(classUnderTestType, methodUnderTest, parameterAttributeValue, action);
    }
}