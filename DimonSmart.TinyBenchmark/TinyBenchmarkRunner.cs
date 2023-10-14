using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;

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

    public ITinyBenchmarkRunner Run()
    {
        var methodExecutionInfos = GetMethodExecutionInfos();

        foreach (var methodExecutionInfo in methodExecutionInfos)
        {
            var times = MeasureAndRunAction(methodExecutionInfo.Action, _data.MaxFunctionRunTImeMilliseconds);
            var result = new MethodExecutionResults(methodExecutionInfo, times);
            _data.Results.Add(result);
        }

        return this;
    }

    public ITinyBenchmarkRunner SaveRawResultsData()
    {
        var flattenedResults =
            _data.Results.SelectMany(result => result.Times,
                (result, time) =>
                    new FlatMethodExecutionResult(result.Method.ClassType.Name,
                    result.Method.MethodInfo.Name,
                    result.Method.Parameter, time))
            .OrderBy(c => c.ClassName)
            .ThenBy(f => f.MethodName)
            .ThenBy(t => t.Time);
        using var writer = new StreamWriter("MethodExecutionResults.csv");
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteRecords((IEnumerable)flattenedResults);
        return this;
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

    public List<TimeSpan> MeasureAndRunAction(Action action, int maxTotalMilliseconds)
    {
        var firstRunTime = MeasureExecutionTime(action);
        var iterations = (int)(maxTotalMilliseconds / firstRunTime.TotalMilliseconds);

        iterations = Math.Max(iterations, _data.MinFunctionExecutionCount);
        iterations = Math.Min(iterations, _data.MaxFunctionExecutionCount);
        var executionTimes = new List<TimeSpan>(iterations + 1);
        executionTimes.Add(firstRunTime);
        PrepareRun();

        for (var i = 0; i < iterations; i++) executionTimes.Add(MeasureExecutionTime(action));

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