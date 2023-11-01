using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DimonSmart.TinyBenchmark;

public sealed class TimeSpanToMillisecondsConverter : DefaultTypeConverter
{
    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.TotalNanoseconds.ToString(CultureInfo.InvariantCulture);
        }

        return base.ConvertToString(value, row, memberMapData);
    }
}