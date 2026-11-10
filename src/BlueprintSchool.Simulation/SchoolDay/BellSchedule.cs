using BlueprintSchool.Simulation.Data.Models;

namespace BlueprintSchool.Simulation.SchoolDay;

/// <summary>Where the school day is at a moment: the period now, and the one before and after.</summary>
public sealed record BellPosition(BellPeriodDefinition? Current, BellPeriodDefinition? Next, double Progress);

/// <summary>
/// The school day, period by period, from the gates opening to the end of after-school clubs. Lessons
/// run on weekdays only; at weekends nothing is on. Times are the same every school day.
/// </summary>
public sealed class BellSchedule
{
    public BellSchedule(IEnumerable<BellPeriodDefinition> periods)
    {
        Periods = periods.OrderBy(p => p.StartTime).ToList();
    }

    /// <summary>The periods of a school day, in order.</summary>
    public IReadOnlyList<BellPeriodDefinition> Periods { get; }

    /// <summary>The periods that are lessons, in order.</summary>
    public IEnumerable<BellPeriodDefinition> Lessons => Periods.Where(p => p.Kind == BellPeriodKind.Lesson);

    /// <summary>When the school day starts (the gates open) and ends (after-school clubs finish).</summary>
    public TimeSpan DayStart => Periods.Count > 0 ? Periods[0].StartTime : TimeSpan.Zero;

    public TimeSpan DayEnd => Periods.Count > 0 ? Periods[^1].EndTime : TimeSpan.Zero;

    /// <summary>Monday to Friday. Holidays come with the school calendar later.</summary>
    public static bool IsSchoolDay(DateTime date) => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    /// <summary>The period at <paramref name="now"/>, or null outside the school day.</summary>
    public BellPeriodDefinition? At(DateTime now)
    {
        if (!IsSchoolDay(now))
        {
            return null;
        }

        var t = now.TimeOfDay;
        return Periods.FirstOrDefault(p => t >= p.StartTime && t < p.EndTime);
    }

    /// <summary>The period now, the next one today (if any) and how far through the current one we are (0..1).</summary>
    public BellPosition Position(DateTime now)
    {
        var current = At(now);
        var t = now.TimeOfDay;
        var next = IsSchoolDay(now) ? Periods.FirstOrDefault(p => p.StartTime > t) : null;
        var progress = current is null || current.Length <= TimeSpan.Zero ? 0 : (t - current.StartTime) / current.Length;
        return new BellPosition(current, next, Math.Clamp(progress, 0, 1));
    }

    /// <summary>
    /// The period whose bell rang in the tick that moved the clock from <paramref name="before"/> to
    /// <paramref name="now"/>, if one did.
    /// </summary>
    public BellPeriodDefinition? BellBetween(DateTime before, DateTime now)
    {
        if (now <= before || !IsSchoolDay(now))
        {
            return null;
        }

        foreach (var p in Periods.Where(p => p.Bell))
        {
            var ring = now.Date + p.StartTime;
            if (ring > before && ring <= now)
            {
                return p;
            }
        }

        return null;
    }

    /// <summary>Problems with the day's shape: gaps or overlaps between periods, or no lessons at all.</summary>
    public IReadOnlyList<string> Problems()
    {
        var problems = new List<string>();
        if (!Lessons.Any())
        {
            problems.Add("The school day has no lessons.");
        }

        for (var i = 1; i < Periods.Count; i++)
        {
            var (a, b) = (Periods[i - 1], Periods[i]);
            if (b.StartTime != a.EndTime)
            {
                problems.Add(b.StartTime < a.EndTime
                    ? $"{b.Name} starts at {b.Start}, before {a.Name} ends at {a.End}."
                    : $"There is a gap between {a.Name} (ends {a.End}) and {b.Name} (starts {b.Start}).");
            }
        }

        return problems;
    }
}

/// <summary>Raised when the school bell rings at the start of a period. The bell for after school is the end of the day.</summary>
public sealed record BellRang(DateTime Time, BellPeriodDefinition Period) : Core.IGameEvent
{
    public bool EndOfDay => Period.Kind == BellPeriodKind.AfterSchool;
}
