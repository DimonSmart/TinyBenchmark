namespace DimonSmart.TinyBenchmark.Utils;

public static class TimeSpanUtils
{
    public static TimeSpan CalculatePercentile(this IEnumerable<TimeSpan> times, double percentile)
    {
        if (percentile is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 100.");
        }

        var sortedList = times.OrderBy(ts => ts).ToList();

        if (sortedList.Count == 0)
        {
            throw new InvalidOperationException("No times provided");
        }

        var index = (int)Math.Ceiling(percentile / 100.0 * (sortedList.Count - 1));
        return sortedList[index];
    }

    public static TimeSpan CalculatePercentile<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector, double percentile)
    {
        return source.Select(selector).CalculatePercentile(percentile);
    }

    public static TimeSpan BestResult<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector)
    {
        return source.Select(selector).Min();
    }
}