using System;
using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.SchoolDay;
using Godot;

namespace BlueprintSchool.UI;

/// <summary>
/// The school day as a strip in the top bar: one block per period (lessons green, break and lunch gold,
/// tutor time maroon), a marker at the time now, and the current period with the minutes left beneath.
/// </summary>
public partial class BellTimeline : Control
{
    private const float StripHeight = 10;
    private BellSchedule? _schedule;
    private DateTime _now;

    public BellTimeline()
    {
        CustomMinimumSize = new Vector2(220, 36);
        MouseFilter = MouseFilterEnum.Pass;
    }

    public void Show(BellSchedule schedule, DateTime now)
    {
        if (_schedule == schedule && _now == now)
        {
            return;
        }

        _schedule = schedule;
        _now = now;
        TooltipText = Describe(schedule, now);
        QueueRedraw();
    }

    public static Color ColourOf(BellPeriodKind kind) => kind switch
    {
        BellPeriodKind.Lesson => UiStyle.Primary,
        BellPeriodKind.Break or BellPeriodKind.Lunch => UiStyle.Gold,
        BellPeriodKind.Registration => UiStyle.Maroon,
        _ => new Color("#B8C4BD"),
    };

    /// <summary>"Period 2 · 23 min left", "Before school · P1 at 08:50", "Weekend".</summary>
    public static string Caption(BellSchedule schedule, DateTime now)
    {
        if (!BellSchedule.IsSchoolDay(now))
        {
            return "Weekend · no lessons";
        }

        var p = schedule.Position(now);
        if (p.Current is { } current)
        {
            var left = (int)Math.Ceiling((now.Date + current.EndTime - now).TotalMinutes);
            return $"{current.Name} · {left} min left";
        }

        return p.Next is { } next ? $"Before school · {next.ShortName} at {next.Start}" : "School's out";
    }

    private static string Describe(BellSchedule schedule, DateTime now)
    {
        var lines = Caption(schedule, now);
        foreach (var period in schedule.Periods)
        {
            lines += $"\n{period.Start}–{period.End}  {period.Name}";
        }

        return lines;
    }

    public override void _Draw()
    {
        if (_schedule is not { Periods.Count: > 0 } schedule)
        {
            return;
        }

        var font = GetThemeDefaultFont();
        var (start, end) = (schedule.DayStart, schedule.DayEnd);
        var span = (end - start).TotalMinutes;
        var width = Size.X;
        float X(TimeSpan t) => (float)((t - start).TotalMinutes / span) * width;
        var school = BellSchedule.IsSchoolDay(_now);
        var current = school ? schedule.At(_now) : null;

        foreach (var period in schedule.Periods)
        {
            var x0 = X(period.StartTime);
            var x1 = X(period.EndTime);
            var colour = ColourOf(period.Kind);
            if (!school)
            {
                colour = colour.Lerp(new Color("#D9E1DC"), 0.75f);
            }
            else if (period != current)
            {
                colour = colour.Lerp(Colors.White, 0.45f);
            }

            DrawRect(new Rect2(x0 + 0.5f, 2, Math.Max(1, x1 - x0 - 1), StripHeight), colour);
        }

        if (school && _now.TimeOfDay >= start && _now.TimeOfDay <= end)
        {
            var x = X(_now.TimeOfDay);
            DrawLine(new Vector2(x, 0), new Vector2(x, StripHeight + 4), UiStyle.TextDark, 2);
        }

        DrawString(font, new Vector2(0, StripHeight + 18), Caption(schedule, _now), HorizontalAlignment.Left, width, 12, UiStyle.TextMuted);
    }
}
