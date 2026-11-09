namespace RaceDayWeb.Helpers;

// Small formatting helpers used by the views (display only, no business rules).
public static class DisplayHelpers
{
    // 1 -> "1st", 2 -> "2nd", 11 -> "11th", 47 -> "47th"
    public static string Ordinal(int n)
    {
        int lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string FinishTimeText(TimeSpan? t) =>
        t.HasValue ? $"{(int)t.Value.TotalHours:00}:{t.Value.Minutes:00}:{t.Value.Seconds:00}" : "-";

    public static string PositionText(int? position, int total)
    {
        if (!position.HasValue) return "-";
        return total >= position.Value && total > 0
            ? $"{Ordinal(position.Value)} out of {total}"
            : Ordinal(position.Value);
    }

    // Colour coding for enrolment status badges (styles live in site.css)
    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "status-confirmed",   // green
        "Pending"   => "status-pending",     // amber
        "Cancelled" => "status-cancelled",   // red
        _           => "status-other"
    };
}
