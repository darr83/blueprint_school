using System.Globalization;

namespace BlueprintSchool.Simulation.Data.Models;

/// <summary>What happens in one part of the school day.</summary>
public enum BellPeriodKind
{
    /// <summary>Pupils arrive and the gates open.</summary>
    Arrival,

    /// <summary>Morning registration with the form tutor.</summary>
    Registration,

    Lesson,

    /// <summary>Morning break.</summary>
    Break,

    Lunch,

    /// <summary>Clubs, detentions and staff meetings after the last lesson.</summary>
    AfterSchool,
}

/// <summary>
/// One part of the school day, from one bell to the next. Loaded from Data/BellSchedule; the
/// periods of a day are laid end to end, so each one ends where the next begins.
/// </summary>
public sealed class BellPeriodDefinition : IDataRecord
{
    public string Id { get; init; } = string.Empty;

    /// <summary>Full name, e.g. "Period 3" or "Lunch".</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Label for the bell timeline, e.g. "P3".</summary>
    public string ShortName { get; init; } = string.Empty;

    public BellPeriodKind Kind { get; init; }

    /// <summary>Start time, as HH:mm on the 24-hour clock.</summary>
    public string Start { get; init; } = string.Empty;

    /// <summary>End time, as HH:mm on the 24-hour clock.</summary>
    public string End { get; init; } = string.Empty;

    /// <summary>True if the bell rings when this period starts.</summary>
    public bool Bell { get; init; } = true;

    public TimeSpan StartTime => ParseTime(Start) ?? TimeSpan.Zero;

    public TimeSpan EndTime => ParseTime(End) ?? TimeSpan.Zero;

    public TimeSpan Length => EndTime - StartTime;

    public static TimeSpan? ParseTime(string? text) =>
        TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var t) && t < TimeSpan.FromDays(1) ? t : null;

    public void Validate(ValidationContext context)
    {
        context.RequireId("id", Id);
        context.RequireText("name", Name);
        context.RequireText("short_name", ShortName);
        if (ParseTime(Start) is null)
        {
            context.Error("start", $"'{Start}' is not a time. Use HH:mm, e.g. 08:50.");
        }

        if (ParseTime(End) is null)
        {
            context.Error("end", $"'{End}' is not a time. Use HH:mm, e.g. 09:45.");
        }
        else if (ParseTime(Start) is { } s && ParseTime(End) <= s)
        {
            context.Error("end", "must be after the start.");
        }
    }
}
