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
}