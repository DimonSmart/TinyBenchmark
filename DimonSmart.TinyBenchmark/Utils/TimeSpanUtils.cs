namespace DimonSmart.TinyBenchmark.Utils;

public static class TimeSpanUtils
{
    public static TimeSpan Percentile50(this IEnumerable<TimeSpan> times)
    {
        var sortedList = times.OrderBy(ts => ts).ToList();

        if (sortedList.Count == 0)
        {
            throw new InvalidOperationException("No times provided");
        }

        if (sortedList.Count % 2 == 0)
        {
            var middle1 = sortedList[sortedList.Count / 2 - 1];
            var middle2 = sortedList[sortedList.Count / 2];
            return TimeSpan.FromTicks((middle1.Ticks + middle2.Ticks) / 2);
        }

        return sortedList[sortedList.Count / 2];
    }

    public static TimeSpan BestResult(IEnumerable<TimeSpan> times)
    {
        return times.Min();
    }
}