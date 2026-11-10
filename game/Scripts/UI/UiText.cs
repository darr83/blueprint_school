using System;

namespace BlueprintSchool.UI;

/// <summary>Shared wording and status colours for the interface.</summary>
public static class UiText
{
    public const string RedHex = "#E53935";
    public const string AmberHex = "#F9A825";
    public const string GreenHex = "#43A047";

    /// <summary>Amber that is readable as text on a white panel.</summary>
    public const string AmberTextHex = "#9A5B00";

    public static string Duration(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{(int)span.TotalHours} h {span.Minutes:00} m" : $"{Math.Max(0, (int)span.TotalMinutes)} m";

    /// <summary>Escapes text for a RichTextLabel with BBCode enabled.</summary>
    public static string Escape(string text) => text.Replace("[", "[lb]");
}
