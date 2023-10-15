using System.Diagnostics;
using System.Reflection;
using DimonSmart.TinyBenchmark.Exporters;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private BenchmarkData _data = new();

    private TinyBenchmarkRunner()
    {
    }

    public ITinyBenchmarkRunner Reset()
    {
        _data = new BenchmarkData();
        return this;
    }

    public IResultProcessor Run()
    {
        var methodExecutionInfos = GetMethodExecutionInfos();

        foreach (var methodExecutionInfo in methodExecutionInfos)
        {
            var times = MeasureAndRunAction(methodExecutionInfo.Action, _data.MaxFunctionRunTImeMilliseconds);
            var result = new MethodExecutionResults(methodExecutionInfo, times);
            _data.Results.Add(result);
        }

        return new ResultProcessor(this, _data);
    }


    public static TinyBenchmarkRunner Create()
    {
        return new TinyBenchmarkRunner();
    }

    private static List<MethodExecutionInfo> GetMethodExecutionInfos()
    {
        var classesUnderTest = AttributeUtility.GetClassesUnderTest();
        var executionInfos = new List<MethodExecutionInfo>();
        foreach (var classUnderTestType in classesUnderTest)
        {
            var classUnderTestProperty = AttributeUtility.FindClassUnderTestParameterProperty(classUnderTestType);
            var parameters = AttributeUtility.GetParametersFromAttribute(classUnderTestProperty);
            var methodsUnderTest = AttributeUtility.GetMethodsWithTinyBenchmarkAttribute(classUnderTestType);
            var classForTest = Activator.CreateInstance(classUnderTestType);

            foreach (var methodUnderTest in methodsUnderTest)
            {
                executionInfos.AddRange(
                    GetMethodExecutionInfos(parameters, methodUnderTest, classForTest, classUnderTestType));
            }
        }

        return executionInfos;
    }

    public List<TimeSpan> MeasureAndRunAction(Action action, int maxTotalMilliseconds)
    {
        var firstRunTime = MeasureExecutionTime(action);
        var iterations = (int)(maxTotalMilliseconds / firstRunTime.TotalMilliseconds);

        iterations = Math.Max(iterations, _data.MinFunctionExecutionCount);
        iterations = Math.Min(iterations, _data.MaxFunctionExecutionCount);
        var executionTimes = new List<TimeSpan>(iterations + 1);
        executionTimes.Add(firstRunTime);


        for (var i = 0; i < iterations; i++) executionTimes.Add(MeasureExecutionTime(action));

        return executionTimes;
    }

    private static void PrepareRun()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    public static TimeSpan MeasureExecutionTime(Action action)
    {
        PrepareRun();
        GC.TryStartNoGCRegion(100 * 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        GC.EndNoGCRegion();
        return stopwatch.Elapsed;
    }

    private static List<MethodExecutionInfo> GetMethodExecutionInfos(
        object[]? parameters,
        MethodInfo methodUnderTest, object? classForTest, Type classUnderTestType)
    {
        var methodExecutionInfos = new List<MethodExecutionInfo>();
        if (parameters != null)
        {
            foreach (var parameterAttributeValue in parameters)
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

    public ITinyBenchmarkRunner WithRunCountLimits(int minFunctionExecutionCount, int maxFunctionExecutionCount)
    {
        if (minFunctionExecutionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minFunctionExecutionCount), minFunctionExecutionCount,
                "Must be greater then 1");
        }

        if (minFunctionExecutionCount > maxFunctionExecutionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFunctionExecutionCount), maxFunctionExecutionCount,
                $"Must be greater then {nameof(minFunctionExecutionCount)}");
        }

        _data.MinFunctionExecutionCount = minFunctionExecutionCount;
        _data.MaxFunctionExecutionCount = maxFunctionExecutionCount;
        return this;
    }
}