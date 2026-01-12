using System.Text.RegularExpressions;

namespace ChatBot;

public static class Utils
{
    public static string RequireEnv(this WebApplicationBuilder builder, string key)
    {
        var v = builder.Configuration["Keys:" + key];
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {key}");
        return v!;
    }

    public static string ToUrlSafeId(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var s = title!.Trim();
        s = new string(s.Where(c => c <= 127).ToArray());
        s = Regex.Replace(s, @"[^\w\-]+", "_");
        s = Regex.Replace(s, "_{2,}", "_");
        s = s.Trim('_');

        if (string.IsNullOrEmpty(s))
            return Uri.EscapeDataString(title);

        return s;
    }
}
