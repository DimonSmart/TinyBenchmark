using CsvHelper.Configuration;

namespace DimonSmart.TinyBenchmark;

public sealed class FlatMethodExecutionResultMap : ClassMap<FlatMethodExecutionResult>
{
    public FlatMethodExecutionResultMap()
    {
        Map(x => x.ClassName);
        Map(x => x.MethodName);
        Map(x => x.Parameter);
        Map(x => x.Time);
    }
}