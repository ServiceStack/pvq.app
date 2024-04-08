using ServiceStack.Text;

namespace MyApp.Data;

public static class StringExtensions
{
    static bool IsUserNameChar(char c) => c == '-' || (char.IsLetterOrDigit(c) && char.IsLower(c)); 
    
    public static List<string> FindUserNameMentions(this string text)
    {
        var to = new List<string>();
        var s = text.AsSpan();
        s.AdvancePastChar('@');
        while (s.Length > 0)
        {
            var i = 0;
            while (IsUserNameChar(s[i]))
            {
                if (++i >= s.Length)
                    break;
            }
            var candidate = i > 2 ? s[..i].ToString() : "";
            if (candidate.Length > 1)
            {
                to.Add(candidate);
            }
            s = s.Advance(i).AdvancePastChar('@');
        }
        return to;
    }
    
    public static string ToHumanReadable(this int? number) => number == null ? "0" : number.Value.ToHumanReadable();
    public static string ToHumanReadable(this int number)
    {
        if (number >= 1_000_000_000)
            return (number / 1_000_000_000D).ToString("0.#") + "b";
        if (number >= 1_000_000)
            return (number / 1_000_000D).ToString("0.#") + "m";
        if (number >= 1_000)
            return (number / 1_000D).ToString("0.#") + "k";
        return number.ToString("#,0");
    }    
    
    public static string TimeAgo(this TimeSpan duration)
    {
        if (duration.TotalDays > 365)
        {
            int years = (int)(duration.TotalDays / 365);
            int months = (int)((duration.TotalDays % 365) / 30);
            return $"{years} {(years > 1 ? "years" : "year")}" +
                   (months == 0 ? "" : $", {months} {(months > 1 ? "months" : "month")} ago");
        }
        if (duration.TotalDays > 30)
        {
            int months = (int)(duration.TotalDays / 30);
            int days = (int)(duration.TotalDays % 30);
            return $"{months} {(months > 1 ? "months" : "month")}" +
                   (days == 0 ? "" : $", {days} {(days > 1 ? "days" : "day")} ago");
        }
        if (duration.TotalDays >= 1)
        {
            int days = (int)duration.TotalDays;
            return $"{days} {(days > 1 ? "days" : "day")} ago";
        }
        if (duration.TotalHours >= 1)
        {
            int hours = (int)duration.TotalHours;
            return $"{hours} {(hours > 1 ? "hours" : "hour")} ago";
        }
        if (duration.TotalMinutes >= 1)
        {
            int minutes = (int)duration.TotalMinutes;
            return $"{minutes} {(minutes > 1 ? "minutes" : "minute")} ago";
        }
        int seconds = (int)duration.TotalSeconds;
        return $"{seconds} {(seconds > 1 ? "seconds" : "second")} ago";
    }
}