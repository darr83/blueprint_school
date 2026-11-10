using BlueprintSchool.Simulation.Core;
using BlueprintSchool.Simulation.Data.Models;
using BlueprintSchool.Simulation.SchoolDay;

namespace BlueprintSchool.Simulation.Simulation;

public enum AnnouncementKind
{
    /// <summary>Atmosphere: the morning welcome, lunch, buses, the fire alarm test and the like.</summary>
    General,
}

/// <summary>
/// A PA announcement. <see cref="Clips"/> are the recorded lines to play in order (after the chime);
/// <see cref="Text"/> is what is said, for the notification feed and subtitles.
/// </summary>
public sealed record Announcement(AnnouncementKind Kind, string Text, IReadOnlyList<string> Clips) : IGameEvent;

/// <summary>PA announcements at set points in the school day, timed from the bell schedule.</summary>
public sealed partial class SchoolSimulation
{
    private DateTime _lastGeneralAnnouncement = DateTime.MinValue;

    /// <summary>The day's announcements, by time of day, on the given weekday. Exposed for tests.</summary>
    public IEnumerable<(TimeSpan At, string Clip, string Text)> AnnouncementsFor(DayOfWeek day)
    {
        if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            yield break;
        }

        var bells = Database.Bells;
        TimeSpan? Start(BellPeriodKind kind) => bells.Periods.FirstOrDefault(p => p.Kind == kind)?.StartTime;
        TimeSpan? End(BellPeriodKind kind) => bells.Periods.FirstOrDefault(p => p.Kind == kind)?.EndTime;
        var m = TimeSpan.FromMinutes(1);

        if (Start(BellPeriodKind.Registration) is { } reg)
        {
            yield return day == DayOfWeek.Monday
                ? (reg - (5 * m), "gen_no_smoking", "This is a no smoking site, including the car park and entrances.")
                : (reg - (5 * m), "gen_morning", "Good morning everyone. Please make your way to your tutor rooms.");
        }

        if (day == DayOfWeek.Wednesday)
        {
            yield return (new TimeSpan(10, 0, 0), "gen_fire_test", "This is a test of the fire alarm system. No action is required.");
        }

        if (Start(BellPeriodKind.Break) is { } breakStart && day == DayOfWeek.Tuesday)
        {
            yield return (breakStart + (2 * m), "gen_walk_corridors", "Please remember to walk, not run, in the corridors.");
        }

        if (Start(BellPeriodKind.Lunch) is { } lunch)
        {
            yield return (lunch + (2 * m), "gen_lunch_open", "The canteen is now open for lunch. Year seven, you may go first.");
            yield return day == DayOfWeek.Friday
                ? (lunch + (10 * m), "gen_lost_property", "Lost property is kept at the school office. Please collect anything with your name on.")
                : (lunch + (10 * m), "gen_library_lunch", "The library is open at lunchtime for quiet reading and homework.");
        }

        if (End(BellPeriodKind.Lunch) is { } lunchEnd)
        {
            yield return (lunchEnd - (5 * m), "gen_end_of_lunch", "Lunch is nearly over. Please make your way to your next lesson.");
        }

        if (Start(BellPeriodKind.AfterSchool) is { } after)
        {
            yield return (after + (5 * m), "gen_buses", "The school buses are waiting at the front. Please make your way there now.");
            yield return (after + (10 * m), "gen_clubs", "After-school clubs are starting. Please go straight to your club room.");
        }
    }

    /// <summary>Plays the announcement due this minute, if there is one.</summary>
    private void UpdateGeneralAnnouncements()
    {
        if (Now.Second != 0 || Now - _lastGeneralAnnouncement < TimeSpan.FromMinutes(1) || !BellSchedule.IsSchoolDay(Now))
        {
            return;
        }

        var minute = new TimeSpan(Now.Hour, Now.Minute, 0);
        foreach (var (at, clip, text) in AnnouncementsFor(Now.DayOfWeek))
        {
            if (at == minute)
            {
                _lastGeneralAnnouncement = Now;
                Events.Publish(new Announcement(AnnouncementKind.General, text, new[] { clip }));
                return;
            }
        }
    }
}
